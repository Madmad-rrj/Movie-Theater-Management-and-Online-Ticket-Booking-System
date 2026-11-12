using CinemaTicketBooking.Api.DTOs.Checkout;
using CinemaTicketBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace CinemaTicketBooking.Api.Controllers;
[ApiController, Route("api/bookings"), GuestCheckout]
public sealed class BookingsController(BookingReservationService reservations) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Reserve(ReserveRequest request, CancellationToken ct)
    {
        var keys = Request.Headers["Idempotency-Key"];
        if (keys.Count != 1 || !Guid.TryParseExact(keys[0], "D", out var key) || key == Guid.Empty)
            throw CheckoutException.Validation("Idempotency-Key", "invalid_format", "Idempotency-Key phải là UUID.");
        var guest = HttpContext.Features.Get<GuestContextDto>()!;
        var result = await reservations.ReserveAsync(guest.ContextId, key, request, HttpContext.TraceIdentifier, ct);
        if (result.Replayed) Response.Headers["Idempotency-Replayed"] = "true";
        return StatusCode(result.Replayed ? 200 : 201, new { data = result.Booking });
    }
}
