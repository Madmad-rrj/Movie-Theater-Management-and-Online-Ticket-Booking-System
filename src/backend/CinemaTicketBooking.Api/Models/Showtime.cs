namespace CinemaTicketBooking.Api.Models;

public sealed class Showtime
{
    public long Id { get; set; }
    public string? SeedKey { get; set; }
    public long MovieId { get; set; }
    public long CinemaRoomId { get; set; }
    public short DurationMinutesSnapshot { get; set; }
    public short TurnaroundMinutesApplied { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset RoomAvailableAt { get; set; }
    public string Status { get; set; } = "draft";
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public long Version { get; set; }
    public Movie Movie { get; set; } = null!;
    public CinemaRoom CinemaRoom { get; set; } = null!;
    public ICollection<BookingSeat> BookingSeats { get; set; } = [];
}
