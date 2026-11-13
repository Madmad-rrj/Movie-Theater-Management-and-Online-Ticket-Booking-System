using System.ComponentModel.DataAnnotations;
using CinemaTicketBooking.Api.DTOs.Checkout;

namespace CinemaTicketBooking.Api.DTOs.Payments;

public sealed class InitiatePaymentRequest
{
    [Required]
    public string Provider { get; init; } = "";
}

public sealed record PaymentDto(
    string PaymentId,
    string BookingId,
    string Provider,
    string Status,
    DateTime? AppliedAt,
    string ReconciliationStatus,
    Money Amount,
    DateTime? PaymentDeadline,
    DateTime? GraceUntil,
    DateTime? LastVerifiedAt,
    DateTime ServerTime);

public sealed record PaymentInitiationDto(
    PaymentDto Payment,
    string? RedirectUrl,
    string NextAction);
