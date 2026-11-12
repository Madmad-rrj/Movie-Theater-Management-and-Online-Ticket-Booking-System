namespace CinemaTicketBooking.Api.Models;

public sealed class Booking
{
    public long Id { get; set; }
    public Guid BookingCode { get; set; }
    public Guid CheckoutIdentityId { get; set; }
    public Guid SelectionSessionId { get; set; }
    public long ShowtimeId { get; set; }
    public long CinemaRoomId { get; set; }
    public string ContactFullName { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public DateTime TermsAcceptedAt { get; set; }
    public DateTime AgeAcknowledgedAt { get; set; }
    public DateTime PurchasedStartAt { get; set; }
    public string MovieTitleSnapshot { get; set; } = "";
    public string RoomNameSnapshot { get; set; } = "";
    public DateTime ReservationExpiresAt { get; set; }
    public int MaximumTicketsSnapshot { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public long Version { get; set; } = 1;
    public List<BookingCategory> Categories { get; set; } = [];
    public List<BookingSeat> Seats { get; set; } = [];
}
public sealed class BookingCategory
{
    public long BookingId { get; set; }
    public string CategoryCode { get; set; } = "";
    public int Quantity { get; set; }
    public decimal BasePriceSnapshot { get; set; }
    public string CategoryNameSnapshot { get; set; } = "";
    public int AdmittedQuantity { get; set; }
}
public sealed class BookingSeat
{
    public long Id { get; set; }
    public long BookingId { get; set; }
    public long ShowtimeId { get; set; }
    public long CinemaRoomId { get; set; }
    public long SeatId { get; set; }
    public string SeatLabelSnapshot { get; set; } = "";
    public string SeatTypeNameSnapshot { get; set; } = "";
    public decimal SurchargeSnapshot { get; set; }
    public string AllocationStatus { get; set; } = "held";
    public DateTime? ReleasedAt { get; set; }
}
