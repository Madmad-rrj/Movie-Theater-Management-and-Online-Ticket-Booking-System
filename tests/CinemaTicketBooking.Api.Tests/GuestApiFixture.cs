using System.Collections.Concurrent;
using System.Net;
using CinemaTicketBooking.Api.Controllers;
using CinemaTicketBooking.Api.DTOs.Checkout;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;

namespace CinemaTicketBooking.Api.Tests;

[CollectionDefinition("Guest PostgreSQL")]
public sealed class GuestPostgresCollection : ICollectionFixture<GuestApiFixture>;

public sealed class GuestApiFixture : IAsyncLifetime
{
    public string ConnectionString { get; } = Environment.GetEnvironmentVariable("CINEMA_TEST_CONNECTION")
        ?? throw new InvalidOperationException("Set CINEMA_TEST_CONNECTION to a NEW isolated local BE2 test database.");
    public GuestApiFactory App { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var connectionOptions = new NpgsqlConnectionStringBuilder(ConnectionString);
        var host = connectionOptions.Host;
        var local = host is not null && (host == "localhost" ||
            (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address)) ||
            (host.StartsWith("cinema-be2-0098-", StringComparison.Ordinal) && host.EndsWith("-postgres", StringComparison.Ordinal)));
        if (!local || connectionOptions.Database?.StartsWith("cinema_be2_test_", StringComparison.Ordinal) != true)
        {
            throw new InvalidOperationException("Tests refuse non-local hosts and databases without the cinema_be2_test_ prefix.");
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using (var check = new NpgsqlCommand("SELECT to_regnamespace('cinema') IS NOT NULL", connection))
        {
            if ((bool)(await check.ExecuteScalarAsync())!)
            {
                throw new InvalidOperationException("The test database already contains cinema. Use a fresh isolated database; tests never reset it.");
            }
        }
        foreach (var file in new[] { "database-schema.sql", "seed-data.sql" })
        {
            var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", file));
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 60 };
            await command.ExecuteNonQueryAsync();
        }
        App = new GuestApiFactory(ConnectionString);
    }

    public Task DisposeAsync()
    {
        App?.Dispose();
        NpgsqlConnection.ClearAllPools();
        return Task.CompletedTask;
    }

    public async Task<long> CountAsync(string table)
    {
        // Callers use fixed table names in test code, never request input.
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"SELECT count(*) FROM cinema.{table}", connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    public async Task ExecuteForGuestAsync(string sql, Guid id)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
    }
}

public sealed class GuestApiFactory(
    string connectionString,
    string environment = "Development",
    Dictionary<string, string?>? overrides = null,
    bool loopbackClient = true) : WebApplicationFactory<Program>
{
    public CapturedLogs Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["GuestCheckout:LifetimeHours"] = "24",
                ["BookingExpiry:Enabled"] = "false",
                ["GuestCheckout:AllowedOrigins:0"] = environment == "Development" ? "http://127.0.0.1:5173" : "https://cinema.example.test",
                ["GuestCheckout:AllowInsecureLoopbackCookies"] = (environment == "Development").ToString(),
                ["GuestCheckout:CrossSiteCookies"] = "false",
                ["GuestCheckout:BootstrapPermitLimit"] = "1000",
                ["GuestCheckout:BootstrapWindowSeconds"] = "60"
            };
        if (overrides is not null)
        {
            foreach (var item in overrides) settings[item.Key] = item.Value;
        }
        // Host settings are available while minimal Program.cs registers services.
        // ConfigureAppConfiguration is applied later, at builder.Build().
        foreach (var setting in settings) builder.UseSetting(setting.Key, setting.Value);
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter>(new TestConnectionFilter(loopbackClient));
            // These endpoints exist ONLY in the test assembly. The shipped API
            // exposes no test mutation, identity lookup, or worker-run endpoint.
            services.AddControllers().AddApplicationPart(typeof(GuestProbeController).Assembly);
        });
    }

    public HttpClient CreateBrowser(bool https = true) => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri(https ? "https://localhost" : "http://127.0.0.1"),
        HandleCookies = false, AllowAutoRedirect = false
    });

    private sealed class TestConnectionFilter(bool loopback) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, continuation) =>
            {
                context.Connection.RemoteIpAddress = loopback ? IPAddress.Loopback : IPAddress.Parse("192.0.2.25");
                return continuation(context);
            });
            next(app);
        };
    }
}

[ApiController]
[Route("api/__tests/guest")]
[GuestCheckout]
public sealed class GuestProbeController : ControllerBase
{
    [HttpGet]
    public IActionResult Read() => Ok(new { data = HttpContext.Features.Get<GuestContextDto>() });

    [HttpPost]
    public IActionResult Mutate() => Read();
}

public sealed class CapturedLogs : ILoggerProvider
{
    public ConcurrentQueue<string> Messages { get; } = new();
    public ILogger CreateLogger(string categoryName) => new CaptureLogger(Messages);
    public void Dispose() { }

    private sealed class CaptureLogger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            messages.Enqueue(formatter(state, exception));
            if (exception is not null) messages.Enqueue(exception.ToString());
        }
    }
}
