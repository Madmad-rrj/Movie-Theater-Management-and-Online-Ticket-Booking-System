using Microsoft.AspNetCore.Mvc;

namespace CinemaTicketBooking.Api.DTOs.Movies;

public sealed class MovieQuery
{
    [FromQuery(Name = "query")]
    public string? SearchTerm { get; init; }
    public string? GenreId { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}
