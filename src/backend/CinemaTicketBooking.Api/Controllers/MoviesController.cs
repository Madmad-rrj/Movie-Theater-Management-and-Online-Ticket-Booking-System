using CinemaTicketBooking.Api.DTOs;
using CinemaTicketBooking.Api.DTOs.Movies;
using CinemaTicketBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaTicketBooking.Api.Controllers;

[ApiController]
[Route("api/movies")]
public sealed class MoviesController(IMovieService movieService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MovieDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Browse(
        [FromQuery] MovieQuery filters,
        CancellationToken cancellationToken)
    {
        if (filters.Page < 1)
        {
            return BadRequest(new { error = new ApiError("VALIDATION_ERROR", "page phải lớn hơn hoặc bằng 1.") });
        }

        if (filters.PageSize is < 1 or > 100)
        {
            return BadRequest(new { error = new ApiError("VALIDATION_ERROR", "pageSize phải nằm trong khoảng 1 đến 100.") });
        }

        if (!string.IsNullOrWhiteSpace(filters.Status) &&
            filters.Status is not ("coming_soon" or "now_showing"))
        {
            return BadRequest(new { error = new ApiError("VALIDATION_ERROR", "status chỉ nhận coming_soon hoặc now_showing.") });
        }

        try
        {
            return Ok(await movieService.BrowseAsync(filters, cancellationToken));
        }
        catch (MovieQueryException exception)
        {
            return BadRequest(new { error = new ApiError(exception.Code, exception.Message) });
        }
    }

    [HttpGet("{movieId}")]
    [ProducesResponseType(typeof(ApiResponse<MovieDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(string movieId, CancellationToken cancellationToken)
    {
        try
        {
            var movie = await movieService.GetPublicDetailAsync(movieId, cancellationToken);
            return Ok(new ApiResponse<MovieDto>(movie));
        }
        catch (MovieQueryException exception)
        {
            return BadRequest(new { error = new ApiError(exception.Code, exception.Message) });
        }
        catch (MovieNotFoundException)
        {
            return NotFound(new { error = new ApiError("NOT_FOUND", "Không tìm thấy phim.") });
        }
    }
}
