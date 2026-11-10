namespace CinemaTicketBooking.Api.Models;

public sealed class BookingSeat
{
    public long Id { get; set; }
    public long BookingId { get; set; }
    public long ShowtimeId { get; set; }
    public long CinemaRoomId { get; set; }
    public long SeatId { get; set; }
    public string SeatLabelSnapshot { get; set; } = string.Empty;
    public string SeatTypeNameSnapshot { get; set; } = string.Empty;
    public decimal SurchargeSnapshot { get; set; }
    public string AllocationStatus { get; set; } = "held";
    public DateTimeOffset? ReleasedAt { get; set; }
    public Showtime Showtime { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
}
