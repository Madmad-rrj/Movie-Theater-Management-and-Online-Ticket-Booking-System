using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CinemaTicketBooking.Api.Data;
using CinemaTicketBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using static CinemaTicketBooking.Api.Services.SelectionService;

namespace CinemaTicketBooking.Api.Services;

public sealed record ExpiryBatch(long Cursor, int Scanned, int Expired, int Deferred, int Busy, int Skipped, int Failed);

public sealed class BookingExpiryService(DbContextOptions<ApplicationDbContext> options,
    IOptions<BookingExpiryOptions> settings, ILogger<BookingExpiryService> logger)
{
    public async Task<ExpiryBatch> RunBatchAsync(long cursor, CancellationToken ct)
    {
        await using var scan = new ApplicationDbContext(options);
        var now = await scan.Database.SqlQuery<DateTime>($"SELECT clock_timestamp() AS \"Value\"").SingleAsync(ct);
        async Task<long[]> Candidates(long after) => await scan.Set<Booking>().AsNoTracking()
            .Where(x => x.Status == "pending" && x.ReservationExpiresAt <= now && x.Id > after)
            .OrderBy(x => x.Id).Select(x => x.Id).Take(settings.Value.BatchSize).ToArrayAsync(ct);
        var ids = await Candidates(cursor);
        if (ids.Length == 0 && cursor > 0) ids = await Candidates(0);
        var expired = 0; var deferred = 0; var busy = 0; var skipped = 0; var failed = 0;
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                switch (await TryExpireAsync(id, ct))
                {
                    case "expired": expired++; break;
                    case "deferred": deferred++; break;
                    case "busy": busy++; break;
                    default: skipped++; break;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                failed++;
                // Do not emit exception text, SQL, credentials or contact data.
                logger.LogWarning("Expiry transaction failed for booking {BookingId} ({ErrorType}); a later sweep will retry.", id, ex.GetType().Name);
            }
        }
        return new(ids.LastOrDefault(), ids.Length, expired, deferred, busy, skipped, failed);
    }

    public async Task<string> TryExpireAsync(long id, CancellationToken ct)
    {
        await using var strategy = new ApplicationDbContext(options);
        return await strategy.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            try
            {
                await using var db = new ApplicationDbContext(options);
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                var candidate = await db.Set<Booking>().AsNoTracking().Where(x => x.Id == id && x.Status == "pending")
                        .Select(x => new { x.CheckoutIdentityId, x.ShowtimeId }).SingleOrDefaultAsync(ct);
                if (candidate is null) return "skipped";
                if (!await TryLock(db, candidate.CheckoutIdentityId, ct)) return "busy";
                // Cleanup is independent of Guest lifetime/revocation, but retains
                // the same ancestor lock order as Guest operations.
                await using (var guest = Command(db, "SELECT id FROM cinema.checkout_identity WHERE id=@id FOR SHARE NOWAIT"))
                {
                    guest.Parameters.AddWithValue("id", candidate.CheckoutIdentityId);
                    await guest.ExecuteScalarAsync(ct);
                }
                await Context(db, candidate.ShowtimeId, ct, reserve: true, noWait: true);
                await using (var claim = Command(db, "SELECT id FROM cinema.booking WHERE id=@id FOR UPDATE SKIP LOCKED"))
                {
                    claim.Parameters.AddWithValue("id", id);
                    if (await claim.ExecuteScalarAsync(ct) is null) return "busy";
                }
                var booking = await BookingRelease.LoadLocked(db, candidate.CheckoutIdentityId, id, ct);
                if (booking.Status != "pending") return "skipped";
                var now = await Now(db, ct);
                if (booking.ReservationExpiresAt > now) return "skipped";
                if (!BookingRelease.AllHeld(booking) || await BookingRelease.HasOtherObligation(db, id, ct) ||
                        await PaymentProtected(db, id, now, ct)) return "deferred";
                // All decisions and writes are under the same showtime/booking/
                // payment locks. Every allocation and the audit commit together.
                now = await Now(db, ct);
                BookingRelease.Apply(booking, "expired", now);
                await db.SaveChangesAsync(ct);
                var operation = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes("booking-expiry:" + id.ToString(CultureInfo.InvariantCulture)))[..16]);
                await BookingRelease.Audit(db, operation, "booking_expired", "system", "booking-expiry", id, now, null, ct);
                await tx.CommitAsync(ct);
                return "expired";
            }
            // Npgsql treats lock-not-available as transient. Handle it inside
            // the strategy so a busy candidate is skipped without retries.
            catch (PostgresException ex) when (ex.SqlState == "55P03") { return "busy"; }
        });
    }

    private static async Task<bool> PaymentProtected(ApplicationDbContext db, long id, DateTime now, CancellationToken ct)
    {
        await using (var command = Command(db, """
            SELECT status,verified_at,applied_at,reconciliation_status,received_amount,payment_deadline,grace_until
            FROM cinema.payment WHERE booking_id=@id ORDER BY id FOR UPDATE NOWAIT
            """))
        {
            command.Parameters.AddWithValue("id", id);
            await using var r = await command.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                // Only verified definitive unsuccessful outcomes can be safe.
                // No timeout, window/grace expiry or status label proves failure.
                if (r.GetString(0) is not ("failed" or "cancelled") || r.IsDBNull(1) || !r.IsDBNull(2) ||
                    r.GetString(3) is not ("none" or "resolved") || (!r.IsDBNull(4) && r.GetDecimal(4) > 0) ||
                    (!r.IsDBNull(5) && r.GetDateTime(5) > now) || (!r.IsDBNull(6) && r.GetDateTime(6) > now))
                    return true;
            }
        }
        await using var evidence = Command(db, """
            SELECT EXISTS(SELECT 1 FROM cinema.payment_event e JOIN cinema.payment p ON p.id=e.payment_id
                          WHERE p.booking_id=@id AND e.processing_status<>'processed')
            """);
        evidence.Parameters.AddWithValue("id", id);
        return (bool)(await evidence.ExecuteScalarAsync(ct))!;
    }
}
