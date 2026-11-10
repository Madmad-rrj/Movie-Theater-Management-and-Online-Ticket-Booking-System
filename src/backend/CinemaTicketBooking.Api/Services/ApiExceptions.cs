namespace CinemaTicketBooking.Api.Services;

public sealed class ApiRequestException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class NotFoundApiException(string message) : Exception(message);
