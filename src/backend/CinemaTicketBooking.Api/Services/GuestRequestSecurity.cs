using System.Net;
using CinemaTicketBooking.Api.DTOs.Checkout;
using Microsoft.Extensions.Options;
using HeaderNames = Microsoft.Net.Http.Headers.HeaderNames;

namespace CinemaTicketBooking.Api.Services;

public sealed class GuestRequestSecurity(IOptions<GuestCheckoutOptions> options, IHostEnvironment environment)
{
    public static void RejectUnsupportedAuthorization(HttpContext context)
    {
        // Authenticated checkout is not implemented in this Guest-only branch.
        // Never silently fall back to a cookie when a bearer credential is supplied.
        if (context.Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            throw CheckoutException.Unauthenticated();
        }
    }

    public void ValidateOrigin(HttpContext context)
    {
        var origins = context.Request.Headers.Origin;
        if (origins.Count != 1 || !options.Value.AllowedOrigins.Contains(origins[0], StringComparer.Ordinal))
        {
            throw CheckoutException.CsrfRejected();
        }
        RequireSecureTransport(context);
    }

    public void RequireSecureTransport(HttpContext context)
    {
        if (!context.Request.IsHttps && !CanUseInsecureLoopback(context))
        {
            throw new CheckoutException(403, "FORBIDDEN", "Guest checkout yêu cầu kết nối an toàn.");
        }
    }

    public void ValidateCsrf(HttpContext context, GuestContextDto guest)
    {
        ValidateOrigin(context);
        var tokens = context.Request.Headers[GuestCheckoutOptions.CsrfHeaderName];
        var credential = GuestCredential.Parse(context.Request.Cookies[GuestCheckoutOptions.CookieName]);
        if (tokens.Count != 1 || !credential.MatchesCsrfToken(guest.ContextId, tokens[0]))
        {
            throw CheckoutException.CsrfRejected();
        }
    }

    public void WriteBootstrapHeaders(HttpContext context, GuestBootstrapResult result)
    {
        RequireSecureTransport(context);
        var secure = context.Request.IsHttps || !CanUseInsecureLoopback(context);
        context.Response.Cookies.Append(GuestCheckoutOptions.CookieName, result.CookieCredential, new CookieOptions
        {
            HttpOnly = true, Secure = secure, Path = "/api",
            SameSite = options.Value.CrossSiteCookies ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = new DateTimeOffset(result.Context.ExpiresAt), IsEssential = true
        });
        context.Response.Headers[GuestCheckoutOptions.CsrfHeaderName] =
            GuestCredential.Parse(result.CookieCredential).CreateCsrfToken(result.Context.ContextId);
        if (result.Replayed)
        {
            context.Response.Headers["Idempotency-Replayed"] = "true";
        }
    }

    public void ClearInvalidCookie(HttpContext context)
    {
        if (context.Request.Cookies.ContainsKey(GuestCheckoutOptions.CookieName))
        {
            context.Response.Cookies.Delete(GuestCheckoutOptions.CookieName, new CookieOptions
            {
                Path = "/api", HttpOnly = true,
                Secure = context.Request.IsHttps || !CanUseInsecureLoopback(context),
                SameSite = options.Value.CrossSiteCookies ? SameSiteMode.None : SameSiteMode.Lax
            });
        }
    }

    private bool CanUseInsecureLoopback(HttpContext context)
        => environment.IsDevelopment() && options.Value.AllowInsecureLoopbackCookies &&
           Uri.TryCreate($"{context.Request.Scheme}://{context.Request.Host}", UriKind.Absolute, out var uri) &&
           uri.IsLoopback && context.Connection.RemoteIpAddress is { } address && IPAddress.IsLoopback(address);
}
