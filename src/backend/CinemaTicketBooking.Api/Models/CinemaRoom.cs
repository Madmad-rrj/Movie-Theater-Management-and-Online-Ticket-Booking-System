namespace CinemaTicketBooking.Api.Models;

public sealed class CinemaRoom
{
    public long Id { get; set; }
    public long CinemaId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public long Version { get; set; }
    public Cinema Cinema { get; set; } = null!;
    public ICollection<Showtime> Showtimes { get; set; } = [];
    public ICollection<Seat> Seats { get; set; } = [];
}
