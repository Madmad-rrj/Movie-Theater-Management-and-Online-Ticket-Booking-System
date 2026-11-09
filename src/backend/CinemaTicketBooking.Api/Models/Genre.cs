namespace CinemaTicketBooking.Api.Models;

public sealed class Genre
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public long Version { get; set; }
    public ICollection<MovieGenre> MovieGenres { get; set; } = [];
}
