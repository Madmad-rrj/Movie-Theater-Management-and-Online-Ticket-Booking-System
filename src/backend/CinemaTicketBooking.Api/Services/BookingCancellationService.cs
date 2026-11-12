using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CinemaTicketBooking.Api.Data;
using CinemaTicketBooking.Api.DTOs.Checkout;
using CinemaTicketBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using static CinemaTicketBooking.Api.Services.SelectionService;

namespace CinemaTicketBooking.Api.Services;

public sealed class BookingCancellationService(DbContextOptions<ApplicationDbContext> options)
{
    private static CheckoutException Ineligible(string reason) => new(422, "CANCELLATION_NOT_ELIGIBLE",
        "Không thể hủy booking ở trạng thái hiện tại.", new { reason });
    private sealed record Failure(string Reason);
    private static readonly JsonSerializerOptions ReceiptJson = new(JsonSerializerDefaults.Web);

    public async Task<ReservationOutcome> CancelAsync(Guid owner, string bookingId, Guid key,
        CancelPendingRequest request, string traceId, CancellationToken ct)
    {
        var id = Number(bookingId, "bookingId");
        if (request.Confirmed != true)
            throw CheckoutException.Validation("confirmed", "confirmation_required", "Phải xác nhận cảnh báo hủy booking.");
        var scope = owner.ToString("D");
        var target = "bookings/" + id.ToString(CultureInfo.InvariantCulture);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(target + ":confirmed=true")));
        var operation = Guid.NewGuid();
        await using var strategy = new ApplicationDbContext(options);
        return await strategy.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = new ApplicationDbContext(options);
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await Lock(db, owner, ct);
            await Authority(db, owner, ct);
            var showtime = await db.Set<Booking>().Where(x => x.Id == id && x.CheckoutIdentityId == owner)
                .Select(x => (long?)x.ShowtimeId).SingleOrDefaultAsync(ct)
                ?? throw new CheckoutException(404, "NOT_FOUND", "Không tìm thấy booking.");
            var context = await Context(db, showtime, ct, reserve: true);
            var booking = await BookingRelease.LoadLocked(db, owner, id, ct);
            var prior = await db.OperationReceipts.SingleOrDefaultAsync(x => x.ActorScope == scope &&
                x.OperationName == "cancelPending" && x.TargetScope == target && x.RequestKey == key, ct);
            if (prior is not null)
            {
                if (prior.RequestFingerprint != fingerprint) throw CheckoutException.KeyConflict();
                if (prior.ProcessingState == "rejected")
                    throw new CheckoutException(422, "CANCELLATION_NOT_ELIGIBLE", "Không thể hủy booking ở trạng thái hiện tại.",
                        new { reason = JsonSerializer.Deserialize<Failure>(prior.ResultReference!, ReceiptJson)!.Reason }) { Replayed = true };
                if (prior.ProcessingState != "committed") throw CheckoutException.InProgress();
                var replay = await BookingReadService.Project(db, owner, id, context, ct);
                await Authority(db, owner, ct);
                await tx.CommitAsync(ct);
                return new ReservationOutcome(replay.Booking, Replayed: true);
            }
            string? reason = booking.Status != "pending" ? "booking_not_pending" :
                await BookingRelease.HasAnyPayment(db, id, ct) ? "payment_initiation_committed" :
                !BookingRelease.AllHeld(booking) ? "allocation_state_inconsistent" :
                await BookingRelease.HasOtherObligation(db, id, ct) ? "protected_obligation" : null;
            var now = await Now(db, ct);
            if (reason is null)
            {
                BookingRelease.Apply(booking, "cancelled", now);
                await db.SaveChangesAsync(ct);
                await BookingRelease.Audit(db, operation, "booking_cancelled", "guest", scope, id, now, traceId, ct);
            }
            db.OperationReceipts.Add(new OperationReceipt
            {
                Id = operation, ActorScope = scope, OperationName = "cancelPending", TargetScope = target,
                RequestKey = key, RequestFingerprint = fingerprint,
                ProcessingState = reason is null ? "committed" : "rejected",
                ResultReference = reason is null ? id.ToString(CultureInfo.InvariantCulture) : JsonSerializer.Serialize(new Failure(reason), ReceiptJson),
                CreatedAt = now, CommittedAt = reason is null ? now : null
            });
            await db.SaveChangesAsync(ct);
            var result = reason is null ? (await BookingReadService.Project(db, owner, id, context, ct)).Booking : null;
            await Authority(db, owner, ct);
            await tx.CommitAsync(ct);
            if (reason is not null) throw Ineligible(reason);
            return new ReservationOutcome(result!);
        });
    }
}

// Shared transaction helpers. Call only after ancestor/showtime and booking locks.
internal static class BookingRelease
{
    internal static async Task<Booking> LoadLocked(ApplicationDbContext db, Guid owner, long id, CancellationToken ct)
    {
        var booking = (await db.Set<Booking>().FromSqlInterpolated(
            $"SELECT * FROM cinema.booking WHERE id={id} AND checkout_identity_id={owner} FOR UPDATE").ToListAsync(ct))
            .SingleOrDefault() ?? throw new CheckoutException(404, "NOT_FOUND", "Không tìm thấy booking.");
        await db.Entry(booking).Collection(x => x.Seats).LoadAsync(ct);
        await db.Entry(booking).Collection(x => x.Categories).LoadAsync(ct);
        return booking;
    }

    internal static bool AllHeld(Booking booking) => booking.Seats.Count > 0 &&
        booking.Seats.Count == booking.Categories.Sum(x => x.Quantity) &&
        booking.Categories.All(x => x.AdmittedQuantity == 0) && booking.Seats.All(x => x.AllocationStatus == "held");

    internal static async Task<bool> HasAnyPayment(ApplicationDbContext db, long id, CancellationToken ct)
    {
        await using var command = Command(db, "SELECT id FROM cinema.payment WHERE booking_id=@id ORDER BY id FOR UPDATE");
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct);
    }

    internal static async Task<bool> HasOtherObligation(ApplicationDbContext db, long id, CancellationToken ct)
    {
        await using var command = Command(db, """
            SELECT EXISTS(SELECT 1 FROM cinema.ticket WHERE booking_id=@id)
                OR EXISTS(SELECT 1 FROM cinema.refund WHERE booking_id=@id)
                OR EXISTS(SELECT 1 FROM cinema.cancellation_request WHERE booking_id=@id AND status IN ('pending_review','approved_awaiting_refund'))
            """);
        command.Parameters.AddWithValue("id", id);
        return (bool)(await command.ExecuteScalarAsync(ct))!;
    }

    internal static void Apply(Booking booking, string state, DateTime now)
    {
        booking.Status = state; booking.Version++;
        if (state == "cancelled") { booking.CancelledAt = now; booking.CancellationReason = "guest_cancelled_before_payment"; }
        foreach (var seat in booking.Seats) { seat.AllocationStatus = "released"; seat.ReleasedAt = now; }
    }

    internal static Task Audit(ApplicationDbContext db, Guid operation, string type, string actor, string scope,
        long id, DateTime now, string? traceId, CancellationToken ct)
        => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO cinema.audit_event(operation_identity,event_type,actor_kind,actor_scope,resource_type,resource_id,occurred_at,correlation_id)
            VALUES ({operation},{type},{actor},{scope},'booking',{id.ToString(CultureInfo.InvariantCulture)},{now},{traceId})
            """, ct);
}
