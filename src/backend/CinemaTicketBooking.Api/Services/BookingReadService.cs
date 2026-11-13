using System.Data;
using System.Globalization;
using CinemaTicketBooking.Api.Data;
using CinemaTicketBooking.Api.DTOs.Checkout;
using CinemaTicketBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using static CinemaTicketBooking.Api.Services.SelectionService;

namespace CinemaTicketBooking.Api.Services;

public sealed class BookingReadService(DbContextOptions<ApplicationDbContext> options)
{
    private static CheckoutException NotFound() => new(404, "NOT_FOUND", "Không tìm thấy booking.");

    public async Task<CheckoutRecoveryDto> ReadAsync(Guid owner, string bookingId, CancellationToken ct)
    {
        var id = Number(bookingId, "bookingId");
        await using var strategy = new ApplicationDbContext(options);
        return await strategy.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = new ApplicationDbContext(options);
            // A coherent snapshot across booking, quantities, allocations and payment.
            // Use the existing lock order so future coordinated writers can coexist.
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
            await Lock(db, owner, ct);
            await Authority(db, owner, ct);
            var showtime = await db.Set<Booking>().AsNoTracking()
                .Where(x => x.Id == id && x.CheckoutIdentityId == owner)
                .Select(x => (long?)x.ShowtimeId).SingleOrDefaultAsync(ct) ?? throw NotFound();
            var context = await Context(db, showtime, ct);
            await using (var command = Command(db, "SELECT id FROM cinema.booking WHERE id=@id AND checkout_identity_id=@owner FOR SHARE"))
            {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("owner", owner);
                if (await command.ExecuteScalarAsync(ct) is null) throw NotFound();
            }
            var result = await Project(db, owner, id, context, ct);
            await Authority(db, owner, ct);
            await tx.CommitAsync(ct);
            return result;
        });
    }

    internal static async Task<CheckoutRecoveryDto> Project(ApplicationDbContext db, Guid owner, long id,
        ShowtimeContext context, CancellationToken ct)
    {
        var booking = await db.Set<Booking>().AsNoTracking().Include(x => x.Categories).Include(x => x.Seats)
            .AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id && x.CheckoutIdentityId == owner, ct) ?? throw NotFound();
        var selection = await db.Set<SelectionSession>().AsNoTracking()
            .SingleAsync(x => x.Id == booking.SelectionSessionId && x.CheckoutIdentityId == owner, ct);
        var adult = booking.Categories.Single(x => x.CategoryCode == "Adult");
        var discounted = booking.Categories.Single(x => x.CategoryCode == "Discounted");
        var seats = booking.Seats.OrderBy(x => x.SeatId).Select(x => new SelectionSeatSnapshot(
            x.SeatId.ToString(CultureInfo.InvariantCulture), x.SeatLabelSnapshot, x.SeatTypeNameSnapshot,
            new(x.SurchargeSnapshot))).ToArray();
        var quote = new SelectionQuote(new Guid(Convert.FromHexString(selection.AcceptedQuoteFingerprint!)[..16]),
            selection.AcceptedQuoteFingerprint!, new(adult.Quantity, discounted.Quantity), new(adult.BasePriceSnapshot),
            new(discounted.BasePriceSnapshot), seats, new(booking.TotalAmount, booking.Currency), booking.CreatedAt);
        var payment = await Payment(db, id, ct);
        var cancellation = await Cancellation(db, id, ct);
        var now = await Now(db, ct);
        if (payment is not null) payment = payment with { ServerTime = now };
        var summary = new BookingDto(booking.Id.ToString(CultureInfo.InvariantCulture), booking.BookingCode.ToString("D"),
            booking.Status, context.ToDto(now), booking.PurchasedStartAt, seats, new(adult.Quantity, discounted.Quantity),
            new(adult.AdmittedQuantity, discounted.AdmittedQuantity),
            new(adult.Quantity - adult.AdmittedQuantity, discounted.Quantity - discounted.AdmittedQuantity), quote,
            new(booking.ContactFullName, booking.ContactPhone, booking.ContactEmail), booking.ReservationExpiresAt,
            payment?.PaymentDeadline, payment?.PaymentId, cancellation, booking.CreatedAt, booking.ConfirmedAt,
            booking.Version.ToString(CultureInfo.InvariantCulture));
        var next = await NextAction(db, booking, context, payment, cancellation, now, ct);
        return new(summary, payment, next, now);
    }

    private static async Task<CheckoutPayment?> Payment(ApplicationDbContext db, long id, CancellationToken ct)
    {
        // An unresolved financial obligation takes priority over a newer terminal
        // failure. No provider identity, raw evidence or reconciliation internals.
        await using var command = Command(db, """
            SELECT id,provider,status,applied_at,reconciliation_status,amount,currency,
                   payment_deadline,grace_until,verified_at
            FROM cinema.payment WHERE booking_id=@id
            ORDER BY CASE WHEN reconciliation_status IN ('needs_review','refund_required') THEN 0
                          WHEN status='succeeded' AND applied_at IS NULL THEN 1
                          WHEN status IN ('created','pending','unknown') THEN 2
                          WHEN applied_at IS NOT NULL THEN 3 ELSE 4 END,
                     initiation_committed_at DESC,id DESC LIMIT 1
            """);
        command.Parameters.AddWithValue("id", id);
        await using var r = await command.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return new(r.GetInt64(0).ToString(CultureInfo.InvariantCulture), id.ToString(CultureInfo.InvariantCulture),
            r.GetString(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetDateTime(3), r.GetString(4),
            new(r.GetDecimal(5), r.GetString(6)), r.IsDBNull(7) ? null : r.GetDateTime(7),
            r.IsDBNull(8) ? null : r.GetDateTime(8), r.IsDBNull(9) ? null : r.GetDateTime(9), default);
    }

    private static async Task<BookingCancellationSummary?> Cancellation(ApplicationDbContext db, long id, CancellationToken ct)
    {
        await using var command = Command(db, """
            SELECT cr.id,cr.status,r.id FROM cinema.cancellation_request cr
            LEFT JOIN cinema.refund r ON r.cancellation_request_id=cr.id
            WHERE cr.booking_id=@id AND cr.status IN ('pending_review','approved_awaiting_refund')
            ORDER BY cr.id DESC LIMIT 1
            """);
        command.Parameters.AddWithValue("id", id);
        await using var r = await command.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        var approved = r.GetString(1) == "approved_awaiting_refund";
        return new(r.GetInt64(0).ToString(CultureInfo.InvariantCulture), approved ? "approved" : "pending",
            r.IsDBNull(2) ? null : r.GetInt64(2).ToString(CultureInfo.InvariantCulture), approved);
    }

    private static async Task<string> NextAction(ApplicationDbContext db, Booking booking, ShowtimeContext context,
        CheckoutPayment? payment, BookingCancellationSummary? cancellation, DateTime now, CancellationToken ct)
    {
        if (payment?.ReconciliationStatus is "needs_review" or "refund_required" || cancellation?.BlocksAdmission == true)
            return "contactSupport";
        if (payment is not null && (payment.Status is "created" or "pending" or "unknown" ||
            (payment.Status == "succeeded" && payment.AppliedAt is null)))
            return booking.Status == "pending" ? "awaitOutcome" : "contactSupport";
        if (booking.Status is "expired" or "cancelled") return "selectAgain";
        if (booking.Status == "confirmed")
        {
            await using var command = Command(db, "SELECT EXISTS(SELECT 1 FROM cinema.ticket WHERE booking_id=@id AND status='valid')");
            command.Parameters.AddWithValue("id", booking.Id);
            return (bool)(await command.ExecuteScalarAsync(ct))! ? "viewTicket" : "contactSupport";
        }
        // An applied payment with an unconfirmed booking is an inconsistency;
        // never suggest another payment. Failed/cancelled attempts do not restart
        // checkout either: payment retry policy belongs to the payment workflow.
        if (payment is not null) return "contactSupport";
        if (booking.ReservationExpiresAt <= now || !context.Eligible || context.Start <= now) return "selectAgain";
        if (booking.Seats.Count == 0 || booking.Seats.Any(x => x.AllocationStatus != "held") || booking.TotalAmount <= 0)
            return "contactSupport";
        return "pay";
    }
}
