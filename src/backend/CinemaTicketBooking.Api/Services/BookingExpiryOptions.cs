namespace CinemaTicketBooking.Api.Services;

public sealed class BookingExpiryOptions
{
    public bool Enabled { get; set; }
    public int PollIntervalSeconds { get; set; } = 15;
    public int BatchSize { get; set; } = 50;
}
