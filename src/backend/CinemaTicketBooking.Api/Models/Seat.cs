namespace CinemaTicketBooking.Api.Models;

public sealed class Seat
{
    public long Id { get; set; }
    public long CinemaRoomId { get; set; }
    public long SeatTypeId { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public short SeatNumber { get; set; }
    public int XPosition { get; set; }
    public int YPosition { get; set; }
    public string OperationalState { get; set; } = "active";
    public DateTimeOffset? RetiredAt { get; set; }
    public long Version { get; set; }
    public CinemaRoom CinemaRoom { get; set; } = null!;
    public SeatType SeatType { get; set; } = null!;
    public ICollection<BookingSeat> BookingSeats { get; set; } = [];
}
