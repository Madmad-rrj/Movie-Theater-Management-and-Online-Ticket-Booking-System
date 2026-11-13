using CinemaTicketBooking.Api.DTOs;
using CinemaTicketBooking.Api.DTOs.Showtimes;

namespace CinemaTicketBooking.Api.Services;

public interface IShowtimeService
{
    Task<ApiResponse<IReadOnlyList<ShowtimeDto>>> BrowseByMovieAsync(
        string movieId,
        ShowtimeQuery query,
        CancellationToken cancellationToken);

    Task<ShowtimeDto> GetPublicDetailAsync(string showtimeId, CancellationToken cancellationToken);
}
