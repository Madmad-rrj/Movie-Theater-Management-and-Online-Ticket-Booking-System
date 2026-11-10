using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace CinemaTicketBooking.Api.Tests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; private set; } = null!;
    public string ConnectionString => Container.GetConnectionString();
    public ApiFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Container = new PostgreSqlBuilder()
            .WithImage("postgres:16.4")
            .WithDatabase("cinema_test")
            .WithUsername("cinema_test")
            .WithPassword("cinema_test")
            .WithPortBinding(0, 5432)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready -U cinema_test -d cinema_test"))
            .Build();

        await Container.StartAsync();
        await ApplySchemaAsync();
        Factory = new ApiFactory(ConnectionString);
        Client = Factory.CreateClient();
    }

    private async Task ApplySchemaAsync()
    {
        var schemaPath = FindSchemaPath();
        var script = await File.ReadAllTextAsync(schemaPath);
        await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
        await using var cmd = dataSource.CreateCommand(script);
        cmd.CommandTimeout = 120;
        await cmd.ExecuteNonQueryAsync();
    }

    private static string FindSchemaPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "database", "database-schema.sql");
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Could not locate database/database-schema.sql from test base directory.");
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }
        if (Container is not null)
        {
            await Container.DisposeAsync();
        }
    }
}

[CollectionDefinition("Database")]
public sealed class DatabaseCollection : ICollectionFixture<PostgresFixture>
{
}
