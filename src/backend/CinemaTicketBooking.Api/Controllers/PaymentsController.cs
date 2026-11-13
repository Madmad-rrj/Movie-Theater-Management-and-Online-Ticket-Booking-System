using CinemaTicketBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaTicketBooking.Api.Controllers;

[ApiController, Route("api/payments"), GuestCheckout]
public sealed class PaymentsController(BookingPaymentService payments) : ControllerBase
{
    [HttpGet("{paymentId}")]
    public async Task<IActionResult> Read(string paymentId, CancellationToken ct)
    {
        var id = SelectionService.Number(paymentId, "paymentId");
        var guest = HttpContext.Features.Get<DTOs.Checkout.GuestContextDto>()!;
        return Ok(new { data = await payments.ReadAsync(guest.ContextId, id, ct) });
    }
}
