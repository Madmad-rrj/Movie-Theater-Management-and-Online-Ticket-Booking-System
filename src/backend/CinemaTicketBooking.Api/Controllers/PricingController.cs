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
    public async Task<IActionResult> CinemaPricing(string cinemaId, CancellationToken cancellationToken)
    {
        return Ok(await pricingService.GetCinemaPricingAsync(cinemaId, cancellationToken));
    }
}
