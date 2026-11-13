using CinemaTicketBooking.Api.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CinemaTicketBooking.Api.Controllers;

[ApiController]
[Route("api/guest-context")]
public sealed class GuestContextController(IGuestContextService guests, GuestRequestSecurity security) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(GuestCheckoutOptions.BootstrapRateLimitPolicy)]
    public async Task<IActionResult> Bootstrap(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        GuestRequestSecurity.RejectUnsupportedAuthorization(HttpContext);
        // Bootstrap issues the first CSRF token and may recover it after a reload.
        // It requires a trusted Origin, never changes the existing Guest's expiry,
        // and never modifies that Guest's selections or bookings.
        security.ValidateOrigin(HttpContext);
        if (Request.ContentLength is > 0 || Request.Headers.ContainsKey("Transfer-Encoding") ||
            HttpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody == true)
        {
            throw CheckoutException.Validation("body", "body_not_allowed", "Guest bootstrap không nhận request body.");
        }
        var keys = Request.Headers["Idempotency-Key"];
        if (keys.Count != 1 || !Guid.TryParseExact(keys[0], "D", out var key) || key == Guid.Empty)
        {
            throw CheckoutException.Validation("Idempotency-Key", "invalid_format", "Idempotency-Key phải là một UUID hợp lệ.");
        }
        GuestBootstrapResult result;
        try
        {
            result = await guests.BootstrapAsync(key,
                Request.Cookies[GuestCheckoutOptions.CookieName], cancellationToken);
        }
        catch (CheckoutException exception) when (exception.Code == "UNAUTHENTICATED")
        {
            security.ClearInvalidCookie(HttpContext);
            throw;
        }
        security.WriteBootstrapHeaders(HttpContext, result);
        return StatusCode(result.Created ? StatusCodes.Status201Created : StatusCodes.Status200OK,
            new { data = result.Context });
    }
}
