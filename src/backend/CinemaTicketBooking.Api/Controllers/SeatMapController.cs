using CinemaTicketBooking.Api.DTOs;
using CinemaTicketBooking.Api.DTOs.Seats;
using CinemaTicketBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaTicketBooking.Api.Controllers;

[ApiController]
public sealed class SeatMapController(ISeatMapService seatMapService) : ControllerBase
{
    [HttpGet("api/showtimes/{showtimeId}/seats")]
    [ProducesResponseType(typeof(ApiResponse<SeatMapDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string showtimeId, CancellationToken cancellationToken)
    {
        var seatMap = await seatMapService.GetSeatMapAsync(showtimeId, cancellationToken);
        return Ok(new ApiResponse<SeatMapDto>(seatMap));
    }
}
