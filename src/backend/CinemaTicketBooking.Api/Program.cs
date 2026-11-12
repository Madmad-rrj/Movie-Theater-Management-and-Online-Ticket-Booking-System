using System.Globalization;
using System.Threading.RateLimiting;
using CinemaTicketBooking.Api.Controllers;
using CinemaTicketBooking.Api.Data;
using CinemaTicketBooking.Api.DTOs.Common;
using CinemaTicketBooking.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing ConnectionStrings__DefaultConnection. Configure it through an environment variable or approved local secret store.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(1), new[] { "40P01", "40001" })));
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IGuestContextService, GuestContextService>();
builder.Services.AddScoped<SelectionService>();
builder.Services.AddScoped<BookingReservationService>();
builder.Services.AddOptions<SelectionOptions>().BindConfiguration("Selection")
    .Validate(options => options.MaximumTickets > 0, "Selection:MaximumTickets must be positive.").ValidateOnStart();
builder.Services.AddSingleton<GuestRequestSecurity>();
builder.Services.AddOptions<GuestCheckoutOptions>()
    .Bind(builder.Configuration.GetSection(GuestCheckoutOptions.SectionName))
    .Validate(options => options.IsValid(builder.Environment),
        "GuestCheckout requires a positive lifetime/rate limit and exact trusted origins. HTTP cookies are allowed only for explicit Development loopback use.")
    .ValidateOnStart();

var guestOptions = builder.Configuration.GetSection(GuestCheckoutOptions.SectionName).Get<GuestCheckoutOptions>()
    ?? new GuestCheckoutOptions();
builder.Services.AddCors(options => options.AddPolicy("GuestCheckout", policy => policy
    .WithOrigins(guestOptions.AllowedOrigins)
    .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
    .WithHeaders("Content-Type", "Idempotency-Key", GuestCheckoutOptions.CsrfHeaderName, "Authorization")
    .WithExposedHeaders(GuestCheckoutOptions.CsrfHeaderName, "Idempotency-Replayed", "Retry-After")
    .AllowCredentials()));

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(GuestCheckoutOptions.BootstrapRateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = guestOptions.BootstrapPermitLimit,
                Window = TimeSpan.FromSeconds(guestOptions.BootstrapWindowSeconds), QueueLimit = 0
            }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }
        await context.HttpContext.Response.WriteAsJsonAsync(ApiErrorResponse.Create(
            context.HttpContext, "RATE_LIMITED", "Quá nhiều yêu cầu. Vui lòng thử lại sau."), cancellationToken);
    };
});

builder.Services.AddControllers(options => options.Filters.Add<CheckoutExceptionFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.UnmappedMemberHandling =
        System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
        new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(ApiErrorResponse.Create(
            context.HttpContext, "VALIDATION_ERROR", "Dữ liệu yêu cầu không hợp lệ.", new
            {
                violations = context.ModelState.Where(entry => entry.Value?.Errors.Count > 0)
                    .Select(entry => new { field = entry.Key, reason = "invalid", message = "Giá trị không hợp lệ." })
            })));

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var dependencyUnavailable = IsDependencyUnavailable(exception);
        context.Response.StatusCode = dependencyUnavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(context,
            dependencyUnavailable ? "DEPENDENCY_UNAVAILABLE" : "INTERNAL_ERROR",
            "Đã xảy ra lỗi khi xử lý yêu cầu."));
    });
});

app.UseCors("GuestCheckout");
app.UseRateLimiter();
app.MapControllers();

app.Run();

static bool IsDependencyUnavailable(Exception? exception) => exception is not null &&
    (exception is TimeoutException or NpgsqlException { IsTransient: true } || IsDependencyUnavailable(exception.InnerException));

public partial class Program;
