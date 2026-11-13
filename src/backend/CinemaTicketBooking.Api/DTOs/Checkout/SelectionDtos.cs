using System.ComponentModel.DataAnnotations;
namespace CinemaTicketBooking.Api.DTOs.Checkout;
public sealed record Quantities([Required] int? Adult, [Required] int? Discounted);
public sealed class CreateSelectionRequest
{
    [Required] public string ShowtimeId { get; init; } = "";
    [Required] public Quantities Quantities { get; init; } = null!;
    [Required] public string[] SeatIds { get; init; } = null!;
}
public sealed class UpdateSelectionRequest
{
    [Required] public string Version { get; init; } = "";
    [Required] public Quantities Quantities { get; init; } = null!;
    [Required] public string[] SeatIds { get; init; } = null!;
}
public sealed record Money(decimal Amount, string Currency = "VND");
public sealed record SelectionSeatSnapshot(string SeatId, string Label, string SeatTypeName, Money Surcharge);
public sealed record SelectionQuote(Guid QuoteId, string Version, Quantities Quantities, Money AdultBasePrice,
 Money DiscountedBasePrice, IReadOnlyList<SelectionSeatSnapshot> Seats, Money Total, DateTime QuotedAt);
public sealed record SelectionDto(Guid SelectionId, string ShowtimeId, Quantities Quantities, string[] SeatIds,
 SelectionQuote? Quote, DateTime SelectionDeadline, DateTime ServerTime, string State, string? BookingId, string Version);
public sealed record SelectionOutcome(SelectionDto Selection, bool Created = false, bool Replayed = false);
