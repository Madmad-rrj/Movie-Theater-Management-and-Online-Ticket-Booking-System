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
    public async Task<IActionResult> Browse(
        [FromQuery] MovieQuery filters,
        CancellationToken cancellationToken)
    {
        return Ok(await movieService.BrowseAsync(filters, cancellationToken));
    }

    [HttpGet("{movieId}")]
    [ProducesResponseType(typeof(ApiResponse<MovieDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(string movieId, CancellationToken cancellationToken)
    {
        var movie = await movieService.GetPublicDetailAsync(movieId, cancellationToken);
        return Ok(new ApiResponse<MovieDto>(movie));
    }
}
