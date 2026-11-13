namespace CinemaTicketBooking.Api.Services;

public sealed class MockPayOptions
{
    public const string SectionName = "MockPay";

    public string BaseUrl { get; set; } = "";
    public string MerchantApiKey { get; set; } = "";
    public string Provider { get; set; } = "MockPay";
    public int PaymentWindowSeconds { get; set; } = 600;
    public int GraceSeconds { get; set; } = 10;

    public bool IsValid(bool requireKey = false)
        => Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) &&
           uri.Scheme == Uri.UriSchemeHttps &&
           (!requireKey || !string.IsNullOrWhiteSpace(MerchantApiKey)) &&
           Provider == "MockPay" &&
           PaymentWindowSeconds == 600 &&
           GraceSeconds >= 0;
}
