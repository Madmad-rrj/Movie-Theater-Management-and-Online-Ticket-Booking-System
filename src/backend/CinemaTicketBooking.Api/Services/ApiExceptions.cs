namespace CinemaTicketBooking.Api.Services;

public sealed record ApiValidationViolation(string Field, string Reason, string Message);

public class ApiRequestException : Exception
{
    public ApiRequestException(
        string code,
        string message,
        int statusCode = StatusCodes.Status400BadRequest,
        object? details = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Details = details;
    }

    public string Code { get; }
    public int StatusCode { get; }
    public object? Details { get; }
}

public sealed class NotFoundApiException : ApiRequestException
{
    public NotFoundApiException(string message, object? details = null)
        : base("NOT_FOUND", message, StatusCodes.Status404NotFound, details)
    {
    }
}

public static class ApiValidation
{
    public static ApiRequestException Error(string field, string reason, string message)
    {
        return new ApiRequestException(
            "VALIDATION_ERROR",
            message,
            StatusCodes.Status400BadRequest,
            new { violations = new[] { new ApiValidationViolation(field, reason, message) } });
    }

    public static ApiRequestException Errors(string message, IEnumerable<ApiValidationViolation> violations)
    {
        return new ApiRequestException(
            "VALIDATION_ERROR",
            message,
            StatusCodes.Status400BadRequest,
            new { violations = violations.ToArray() });
    }
}
