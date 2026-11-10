namespace CinemaTicketBooking.Api.Models;

public sealed class CinemaTicketPrice
{
    public long CinemaId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
    public Cinema Cinema { get; set; } = null!;
    public TicketCategory Category { get; set; } = null!;
}
