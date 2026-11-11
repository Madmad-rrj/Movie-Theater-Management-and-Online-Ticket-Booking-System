namespace CinemaTicketBooking.Api.Services;

public sealed class CheckoutException(int statusCode, string code, string message, object? details = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
    public object Details { get; } = details ?? new { };

    public static CheckoutException Unauthenticated() => new(401, "UNAUTHENTICATED", "Phiên Guest không hợp lệ hoặc đã hết hạn.");
    public static CheckoutException CsrfRejected() => new(403, "CSRF_REJECTED", "Không thể xác minh nguồn yêu cầu.");
    public static CheckoutException Validation(string field, string reason, string message) => new(400, "VALIDATION_ERROR", message,
        new { violations = new[] { new { field, reason, message } } });
    public static CheckoutException KeyConflict() => new(409, "IDEMPOTENCY_KEY_CONFLICT", "Idempotency-Key đã được sử dụng cho một ngữ cảnh khác.");
    public static CheckoutException InProgress() => new(409, "OPERATION_IN_PROGRESS", "Yêu cầu đang được xử lý. Vui lòng thử lại.");
}
