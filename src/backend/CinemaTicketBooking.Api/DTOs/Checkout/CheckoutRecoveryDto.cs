namespace CinemaTicketBooking.Api.DTOs.Checkout;

public sealed record CheckoutPayment(string PaymentId, string BookingId, string Provider, string Status,
    DateTime? AppliedAt, string ReconciliationStatus, Money Amount, DateTime? PaymentDeadline,
    DateTime? GraceUntil, DateTime? LastVerifiedAt, DateTime ServerTime);
public sealed record BookingCancellationSummary(string RequestId, string Status, string? RefundId, bool BlocksAdmission);
public sealed record CheckoutRecoveryDto(BookingDto Booking, CheckoutPayment? Payment, string NextAction, DateTime ServerTime);
