namespace CinemaTicketBooking.Api.Services;

public sealed class GuestCheckoutOptions
{
    public const string SectionName = "GuestCheckout";
    public const string CookieName = "cinema_guest";
    public const string CsrfHeaderName = "X-CSRF-Token";
    public const string BootstrapRateLimitPolicy = "guest-bootstrap";

    public int LifetimeHours { get; set; } = 24;
    public string[] AllowedOrigins { get; set; } = [];
    public bool AllowInsecureLoopbackCookies { get; set; }
    public bool CrossSiteCookies { get; set; }
    public int BootstrapPermitLimit { get; set; } = 20;
    public int BootstrapWindowSeconds { get; set; } = 60;

    public bool IsValid(IHostEnvironment environment)
    {
        if (LifetimeHours is < 1 or > 720 || BootstrapPermitLimit < 1 || BootstrapWindowSeconds < 1 ||
            AllowedOrigins.Length == 0 || (AllowInsecureLoopbackCookies && (!environment.IsDevelopment() || CrossSiteCookies)))
        {
            return false;
        }

        return AllowedOrigins.All(origin =>
            Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
            uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
            origin == uri.GetLeftPart(UriPartial.Authority) &&
            (uri.Scheme == Uri.UriSchemeHttps ||
             (AllowInsecureLoopbackCookies && uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)));
    }
}
