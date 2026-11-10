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
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BrowseByMovie(
        string movieId,
        [FromQuery] ShowtimeQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await showtimeService.BrowseByMovieAsync(movieId, query, cancellationToken));
        }
        catch (ApiRequestException ex)
        {
            return BadRequest(new { error = new ApiError(ex.Code, ex.Message) });
        }
        catch (NotFoundApiException ex)
        {
            return NotFound(new { error = new ApiError("NOT_FOUND", ex.Message) });
        }
    }

    [HttpGet("api/showtimes/{showtimeId}")]
    [ProducesResponseType(typeof(ApiResponse<ShowtimeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string showtimeId, CancellationToken cancellationToken)
    {
        try
        {
            var showtime = await showtimeService.GetPublicDetailAsync(showtimeId, cancellationToken);
            return Ok(new ApiResponse<ShowtimeDto>(showtime));
        }
        catch (ApiRequestException ex)
        {
            return BadRequest(new { error = new ApiError(ex.Code, ex.Message) });
        }
        catch (NotFoundApiException ex)
        {
            return NotFound(new { error = new ApiError("NOT_FOUND", ex.Message) });
        }
    }
}
