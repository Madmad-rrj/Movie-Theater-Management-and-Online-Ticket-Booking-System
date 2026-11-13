namespace CinemaTicketBooking.Api.DTOs.Common;

public sealed record ApiErrorResponse(ApiErrorBody Error)
{
    public static ApiErrorResponse Create(HttpContext context, string code, string message, object? details = null)
        => new(new ApiErrorBody(code, message, details ?? new { }, context.TraceIdentifier));
}

public sealed record ApiErrorBody(string Code, string Message, object Details, string TraceId);
