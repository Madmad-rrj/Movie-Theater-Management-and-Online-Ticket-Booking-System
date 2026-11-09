namespace CinemaTicketBooking.Api.Models;

public sealed class Movie
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DurationMinutes { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? PosterUrl { get; set; }
    public string? TrailerUrl { get; set; }
    public string? Language { get; set; }
    public string? AgeRating { get; set; }
    public string Status { get; set; } = string.Empty;
    public long Version { get; set; }
    public ICollection<MovieGenre> MovieGenres { get; set; } = [];
}
