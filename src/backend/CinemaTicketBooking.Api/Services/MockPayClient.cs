using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace CinemaTicketBooking.Api.Services;

public sealed class MockPayClient(HttpClient httpClient, IOptions<MockPayOptions> options)
{
    private readonly MockPayOptions settings = options.Value;

    public async Task<MockPayCreateResponse> CreateAsync(
        MockPayCreateRequest request, Guid idempotencyKey, CancellationToken ct)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments")
        {
            Content = JsonContent.Create(request)
        };
        Authorize(message);
        message.Headers.Add("Idempotency-Key", idempotencyKey.ToString("D"));
        return await SendAsync<MockPayCreateResponse>(message, ct);
    }

    public async Task<MockPayStatusResponse> GetStatusAsync(string paymentId, CancellationToken ct)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Get, $"/api/v1/payments/{Uri.EscapeDataString(paymentId)}/status");
        Authorize(message);
        return await SendAsync<MockPayStatusResponse>(message, ct);
    }

    private void Authorize(HttpRequestMessage message)
        => message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.MerchantApiKey);

    private async Task<T> SendAsync<T>(HttpRequestMessage message, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new MockPayTransportException("MockPay không thể kết nối.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new MockPayTransportException("MockPay phản hồi quá thời gian.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var status = response.StatusCode;
                await response.Content.ReadAsStringAsync(ct);
                throw new MockPayProviderException(status, "MockPay từ chối yêu cầu.");
            }

            var envelope = await response.Content.ReadFromJsonAsync<MockPayEnvelope<T>>(cancellationToken: ct);
            if (envelope is null)
                throw new MockPayProviderException(HttpStatusCode.BadGateway, "MockPay trả về dữ liệu không hợp lệ.");
            return envelope.Data;
        }
    }

}

public sealed record MockPayCreateRequest(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("amount")] long Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("expiresAt")] string ExpiresAt,
    [property: JsonPropertyName("metadata")] object Metadata);

public sealed record MockPayCreateResponse(
    string PaymentId,
    string OrderId,
    long Amount,
    string Currency,
    string Status,
    string PaymentUrl,
    DateTime CreatedAt,
    DateTime ExpiresAt);

public sealed record MockPayStatusResponse(
    string PaymentId,
    string OrderId,
    long Amount,
    string Currency,
    string Status,
    DateTime? PaidAt,
    DateTime ExpiresAt);

public sealed record MockPayEnvelope<T>(T Data);

public sealed class MockPayTransportException(string message, Exception innerException)
    : Exception(message, innerException);

public sealed class MockPayProviderException(HttpStatusCode statusCode, string message)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
