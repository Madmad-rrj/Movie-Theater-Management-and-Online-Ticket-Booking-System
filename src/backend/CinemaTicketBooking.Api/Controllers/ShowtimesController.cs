using CinemaTicketBooking.Api.DTOs;
using CinemaTicketBooking.Api.DTOs.Showtimes;
using CinemaTicketBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaTicketBooking.Api.Controllers;

[ApiController]
public sealed class ShowtimesController(IShowtimeService showtimeService) : ControllerBase
{
    [HttpGet("api/movies/{movieId}/showtimes")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShowtimeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BrowseByMovie(
        string movieId,
        [FromQuery] ShowtimeQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await showtimeService.BrowseByMovieAsync(movieId, query, cancellationToken));
    }

    [HttpGet("api/showtimes/{showtimeId}")]
    [ProducesResponseType(typeof(ApiResponse<ShowtimeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(string showtimeId, CancellationToken cancellationToken)
    {
        var showtime = await showtimeService.GetPublicDetailAsync(showtimeId, cancellationToken);
        return Ok(new ApiResponse<ShowtimeDto>(showtime));
    }
}
