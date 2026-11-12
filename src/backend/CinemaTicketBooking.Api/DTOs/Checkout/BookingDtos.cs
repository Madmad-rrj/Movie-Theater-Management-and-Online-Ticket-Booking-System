using System.ComponentModel.DataAnnotations;
namespace CinemaTicketBooking.Api.DTOs.Checkout;
public sealed class ReserveRequest
{
    [Required] public Guid? SelectionId { get; init; }
    [Required] public string SelectionVersion { get; init; } = "";
    [Required] public string ShowtimeId { get; init; } = "";
    [Required] public string[] SeatIds { get; init; } = null!;
    [Required] public Quantities Quantities { get; init; } = null!;
    [Required] public BookingContact Contact { get; init; } = null!;
    [Required] public CheckoutDeclarations Declarations { get; init; } = null!;
    [Required] public PriceAcceptance PriceAcceptance { get; init; } = null!;
}
public sealed record BookingContact([Required, StringLength(150)] string FullName,
    [Required, StringLength(30)] string Phone, [Required, StringLength(256), EmailAddress] string Email);
public sealed record CheckoutDeclarations([Required] bool? TermsAccepted, [Required] bool? AgeRequirementsAcknowledged);
public sealed record AcceptedMoney([Required] decimal? Amount, [Required] string Currency);
public sealed record PriceAcceptance([Required] Guid? QuoteId, [Required] string Version, [Required] AcceptedMoney AcceptedTotal);
public sealed record BookingShowtime(string ShowtimeId, string MovieId, string MovieTitle, string CinemaId,
    string CinemaName, string RoomId, string RoomName, DateTime OfficialStartsAt, DateTime OfficialEndsAt,
    string Timezone, string Status, bool BookingAvailable, DateTime ServerTime, string Version);
public sealed record BookingDto(string BookingId, string BookingCode, string Status, BookingShowtime Showtime,
    DateTime PurchasedStartAt, IReadOnlyList<SelectionSeatSnapshot> Seats, Quantities Quantities, Quantities Admitted,
    Quantities Remaining, SelectionQuote FinancialSnapshot, BookingContact Contact, DateTime SelectionDeadline,
    DateTime? PaymentDeadline, string? ActivePaymentId, object? Cancellation, DateTime CreatedAt, DateTime? ConfirmedAt,
    string Version);
public sealed record ReservationOutcome(BookingDto Booking, bool Replayed = false);
