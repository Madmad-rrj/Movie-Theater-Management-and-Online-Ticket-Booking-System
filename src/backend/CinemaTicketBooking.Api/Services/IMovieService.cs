using CinemaTicketBooking.Api.DTOs.Movies;

namespace CinemaTicketBooking.Api.Services;

public interface IMovieService
{
    Task<ApiResponse<IReadOnlyList<MovieDto>>> BrowseAsync(
        MovieQuery query,
        CancellationToken cancellationToken);
}
