using CinemaTicketBooking.Api.DTOs;
using CinemaTicketBooking.Api.DTOs.Cinemas;
using CinemaTicketBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaTicketBooking.Api.Controllers;

[ApiController]
[Route("api/cinemas")]
public sealed class CinemasController(ICinemaService cinemaService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CinemaDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        return Ok(await cinemaService.ListPublicAsync(cancellationToken));
    }
}
