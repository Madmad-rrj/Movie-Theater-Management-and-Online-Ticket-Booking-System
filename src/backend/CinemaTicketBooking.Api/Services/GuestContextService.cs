using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using CinemaTicketBooking.Api.Data;
using CinemaTicketBooking.Api.DTOs.Checkout;
using CinemaTicketBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CinemaTicketBooking.Api.Services;

public sealed class GuestContextService(
    DbContextOptions<ApplicationDbContext> dbOptions,
    IOptions<GuestCheckoutOptions> options) : IGuestContextService
{
    private const string Operation = "createGuestContext";
    private const string Scope = "bootstrap";
    private const string Target = "guest-context";
    private static readonly string Fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("POST /api/guest-context:v1:no-body")));

    public async Task<GuestBootstrapResult> BootstrapAsync(Guid requestKey, string? credential, CancellationToken cancellationToken)
    {
        var suppliedCredential = credential is null ? null : GuestCredential.Parse(credential);
        // Allocate once, outside retries, so an uncertain commit can be recovered using
        // this request's own credential without storing a recoverable secret in the DB.
        var candidateCredential = suppliedCredential ?? GuestCredential.Create();
        var candidateId = Guid.NewGuid();
        var lockKey = BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes($"{Operation}:{requestKey:D}")));

        await using var strategyContext = new ApplicationDbContext(dbOptions);
        return await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            // A fresh context prevents tracked inserts from leaking into a retry.
            await using var db = new ApplicationDbContext(dbOptions);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var acquired = await db.Database.SqlQuery<bool>(
                $"SELECT pg_try_advisory_xact_lock({lockKey}) AS \"Value\"").SingleAsync(cancellationToken);
            if (!acquired)
            {
                throw CheckoutException.InProgress();
            }

            var receipt = await db.OperationReceipts.SingleOrDefaultAsync(row =>
                row.ActorScope == Scope && row.OperationName == Operation &&
                row.TargetScope == Target && row.RequestKey == requestKey, cancellationToken);

            if (receipt is not null)
            {
                if (receipt.RequestFingerprint != Fingerprint)
                {
                    throw CheckoutException.KeyConflict();
                }
                if (receipt.ProcessingState == "processing")
                {
                    throw CheckoutException.InProgress();
                }
                // A caller-supplied key alone is never authority to recover a cookie.
                // candidateCredential also handles a commit-acknowledgement loss in
                // the same execution-strategy invocation, before any HTTP response.
                var existing = await FindIdentityAsync(db, candidateCredential, true, cancellationToken);
                if (receipt.ProcessingState != "committed" || receipt.ResultReference != existing.Id.ToString("D"))
                {
                    throw CheckoutException.KeyConflict();
                }
                await transaction.CommitAsync(cancellationToken);
                return Result(existing, candidateCredential, false, true);
            }

            CheckoutIdentity identity;
            if (suppliedCredential is not null)
            {
                identity = await FindIdentityAsync(db, suppliedCredential, true, cancellationToken);
            }
            else
            {
                var now = await DatabaseTimeAsync(db, cancellationToken);
                identity = new CheckoutIdentity
                {
                    Id = candidateId,
                    GuestCredentialHash = candidateCredential.Hash,
                    CreatedAt = now,
                    GuestExpiresAt = now.AddHours(options.Value.LifetimeHours)
                };
                db.CheckoutIdentities.Add(identity);
            }

            var committedAt = await DatabaseTimeAsync(db, cancellationToken);
            if (identity.GuestExpiresAt <= committedAt || identity.RevokedAt is not null)
            {
                throw CheckoutException.Unauthenticated();
            }
            db.OperationReceipts.Add(new OperationReceipt
            {
                Id = Guid.NewGuid(), ActorScope = Scope, OperationName = Operation,
                TargetScope = Target, RequestKey = requestKey, RequestFingerprint = Fingerprint,
                ProcessingState = "committed", ResultReference = identity.Id.ToString("D"),
                CreatedAt = committedAt, CommittedAt = committedAt
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result(identity, candidateCredential, suppliedCredential is null, false);
        });
    }

    public async Task<GuestContextDto> ResolveAsync(string? credential, CancellationToken cancellationToken)
    {
        var parsed = GuestCredential.Parse(credential);
        await using var db = new ApplicationDbContext(dbOptions);
        var identity = await FindIdentityAsync(db, parsed, false, cancellationToken);
        return new GuestContextDto(identity.Id, identity.GuestExpiresAt!.Value);
    }

    private static async Task<CheckoutIdentity> FindIdentityAsync(
        ApplicationDbContext db, GuestCredential credential, bool forUpdate, CancellationToken cancellationToken)
    {
        var query = forUpdate
            ? db.CheckoutIdentities.FromSqlInterpolated(
                $"SELECT * FROM cinema.checkout_identity WHERE guest_credential_hash = {credential.Hash} FOR UPDATE")
            : db.CheckoutIdentities.Where(identity => identity.GuestCredentialHash == credential.Hash);
        var identity = await query.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var now = await DatabaseTimeAsync(db, cancellationToken);
        if (identity is null || identity.UserId is not null || identity.RevokedAt is not null ||
            identity.GuestExpiresAt is null || identity.GuestExpiresAt <= now)
        {
            throw CheckoutException.Unauthenticated();
        }
        return identity;
    }

    private static Task<DateTime> DatabaseTimeAsync(ApplicationDbContext db, CancellationToken cancellationToken)
        => db.Database.SqlQuery<DateTime>($"SELECT clock_timestamp() AS \"Value\"").SingleAsync(cancellationToken);

    private static GuestBootstrapResult Result(CheckoutIdentity identity, GuestCredential credential, bool created, bool replayed)
        => new(new GuestContextDto(identity.Id, identity.GuestExpiresAt!.Value), credential.CookieValue, created, replayed);
}
