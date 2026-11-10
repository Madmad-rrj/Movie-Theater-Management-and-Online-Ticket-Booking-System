using CinemaTicketBooking.Api.DTOs;
using CinemaTicketBooking.Api.DTOs.Pricing;
using CinemaTicketBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaTicketBooking.Api.Controllers;

[ApiController]
public sealed class PricingController(IPricingService pricingService) : ControllerBase
{
    [HttpGet("api/ticket-categories")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TicketCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken)
    {
        return Ok(await pricingService.ListTicketCategoriesAsync(cancellationToken));
    }

    [HttpGet("api/cinemas/{cinemaId}/ticket-pricing")]
    [ProducesResponseType(typeof(ApiResponse<CinemaPricingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CinemaPricing(string cinemaId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await pricingService.GetCinemaPricingAsync(cinemaId, cancellationToken));
        }
        catch (ApiRequestException ex)
        {
            if (ex.Code == "STATE_CONFLICT")
            {
                return Conflict(new { error = new ApiError(ex.Code, ex.Message) });
            }
            return BadRequest(new { error = new ApiError(ex.Code, ex.Message) });
        }
        catch (NotFoundApiException ex)
        {
            return NotFound(new { error = new ApiError("NOT_FOUND", ex.Message) });
        }
    }
}
