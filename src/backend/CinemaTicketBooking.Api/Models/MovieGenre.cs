namespace CinemaTicketBooking.Api.Models;

public sealed class MovieGenre
{
    public long MovieId { get; set; }
    public long GenreId { get; set; }
    public Movie Movie { get; set; } = null!;
    public Genre Genre { get; set; } = null!;
}
