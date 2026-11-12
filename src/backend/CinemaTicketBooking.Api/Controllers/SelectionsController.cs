using CinemaTicketBooking.Api.DTOs.Checkout;
using CinemaTicketBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace CinemaTicketBooking.Api.Controllers;
[ApiController, Route("api/selections"), GuestCheckout]
public sealed class SelectionsController(SelectionService selections) : ControllerBase
{
    private Guid Owner => HttpContext.Features.Get<GuestContextDto>()!.ContextId;
    [HttpPost]
    public async Task<IActionResult> Create(CreateSelectionRequest request, CancellationToken ct)
    {
        var keys = Request.Headers["Idempotency-Key"];
        if (keys.Count != 1 || !Guid.TryParseExact(keys[0], "D", out var key) || key == Guid.Empty)
            throw CheckoutException.Validation("Idempotency-Key", "invalid_format", "Idempotency-Key phải là UUID.");
        var result = await selections.CreateAsync(Owner, key, request, ct);
        if (result.Replayed) Response.Headers["Idempotency-Replayed"] = "true";
        return StatusCode(result.Created ? 201 : 200, new { data = result.Selection });
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> Read(string id, CancellationToken ct)
     => Ok(new { data = await selections.ReadAsync(Owner, Parse(id), ct) });
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UpdateSelectionRequest request, CancellationToken ct)
     => Ok(new { data = await selections.UpdateAsync(Owner, Parse(id), request, ct) });
    private static Guid Parse(string id) => Guid.TryParseExact(id, "D", out var value) && value != Guid.Empty ? value
     : throw CheckoutException.Validation("selectionId", "invalid_format", "selectionId phải là UUID.");
}
