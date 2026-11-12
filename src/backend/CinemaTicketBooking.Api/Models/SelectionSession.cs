namespace CinemaTicketBooking.Api.Models;
public sealed class SelectionSession
{
    public Guid Id { get; set; }
    public Guid CheckoutIdentityId { get; set; }
    public long ShowtimeId { get; set; }
    public long CinemaRoomId { get; set; }
    public DateTime StartedAt { get; set; }
    public int DurationSeconds { get; set; } = 300;
    public DateTime ExpiresAt { get; set; }
    public int AdultQuantity { get; set; }
    public int DiscountedQuantity { get; set; }
    public int MaximumTicketsSnapshot { get; set; } = 8;
    public string State { get; set; } = "active";
    public string? AcceptedQuoteFingerprint { get; set; }
    public decimal? QuotedAdultPrice { get; set; }
    public decimal? QuotedDiscountedPrice { get; set; }
    public decimal? QuotedTotal { get; set; }
    public DateTime? QuotedShowtimeStart { get; set; }
    public long Version { get; set; } = 1;
}
public sealed class SelectionSeat
{
    public Guid SelectionId { get; set; }
    public long SeatId { get; set; }
    public long CinemaRoomId { get; set; }
}
