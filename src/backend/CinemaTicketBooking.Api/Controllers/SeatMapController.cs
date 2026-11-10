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
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string showtimeId, CancellationToken cancellationToken)
    {
        try
        {
            var seatMap = await seatMapService.GetSeatMapAsync(showtimeId, cancellationToken);
            return Ok(new ApiResponse<SeatMapDto>(seatMap));
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
