using CinemaTicketBooking.Api.DTOs.Checkout;
using CinemaTicketBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace CinemaTicketBooking.Api.Controllers;
[ApiController, Route("api/bookings"), GuestCheckout]
public sealed class BookingsController(BookingReservationService reservations, BookingReadService reads,
    BookingCancellationService cancellations) : ControllerBase
{
    [HttpGet("{bookingId}")]
    public async Task<IActionResult> Read(string bookingId, CancellationToken ct)
    {
        var guest = HttpContext.Features.Get<GuestContextDto>()!;
        var result = await reads.ReadAsync(guest.ContextId, bookingId, ct);
        return Ok(new { data = result.Booking });
    }

    [HttpGet("{bookingId}/checkout")]
    public async Task<IActionResult> Recover(string bookingId, CancellationToken ct)
    {
        var guest = HttpContext.Features.Get<GuestContextDto>()!;
        return Ok(new { data = await reads.ReadAsync(guest.ContextId, bookingId, ct) });
    }

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

    [HttpPost("{bookingId}/cancel")]
    public async Task<IActionResult> Cancel(string bookingId, CancelPendingRequest request, CancellationToken ct)
    {
        var keys = Request.Headers["Idempotency-Key"];
        if (keys.Count != 1 || !Guid.TryParseExact(keys[0], "D", out var key) || key == Guid.Empty)
            throw CheckoutException.Validation("Idempotency-Key", "invalid_format", "Idempotency-Key phải là UUID.");
        var guest = HttpContext.Features.Get<GuestContextDto>()!;
        var result = await cancellations.CancelAsync(guest.ContextId, bookingId, key, request, HttpContext.TraceIdentifier, ct);
        if (result.Replayed) Response.Headers["Idempotency-Replayed"] = "true";
        return Ok(new { data = result.Booking });
    }
}
