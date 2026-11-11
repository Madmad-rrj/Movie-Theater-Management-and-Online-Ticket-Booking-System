using CinemaTicketBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Api.Data;

internal static class GuestCheckoutMapping
{
    public static void ConfigureGuestCheckout(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CheckoutIdentity>(entity =>
        {
            entity.ToTable("checkout_identity");
            entity.HasKey(identity => identity.Id);
            entity.Property(identity => identity.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(identity => identity.UserId).HasColumnName("user_id");
            entity.Property(identity => identity.GuestCredentialHash).HasColumnName("guest_credential_hash");
            entity.Property(identity => identity.GuestExpiresAt).HasColumnName("guest_expires_at");
            entity.Property(identity => identity.RevokedAt).HasColumnName("revoked_at");
            entity.Property(identity => identity.CreatedAt).HasColumnName("created_at");
            entity.Property(identity => identity.Version).HasColumnName("version").IsConcurrencyToken();
            entity.HasIndex(identity => identity.GuestCredentialHash).IsUnique();
        });

        modelBuilder.Entity<OperationReceipt>(entity =>
        {
            entity.ToTable("operation_receipt");
            entity.HasKey(receipt => receipt.Id);
            entity.Property(receipt => receipt.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(receipt => receipt.ActorScope).HasColumnName("actor_scope");
            entity.Property(receipt => receipt.OperationName).HasColumnName("operation_name").HasMaxLength(80);
            entity.Property(receipt => receipt.TargetScope).HasColumnName("target_scope");
            entity.Property(receipt => receipt.RequestKey).HasColumnName("request_key");
            entity.Property(receipt => receipt.RequestFingerprint).HasColumnName("request_fingerprint");
            entity.Property(receipt => receipt.ProcessingState).HasColumnName("processing_state").HasMaxLength(16);
            entity.Property(receipt => receipt.ResultReference).HasColumnName("result_reference");
            entity.Property(receipt => receipt.CreatedAt).HasColumnName("created_at");
            entity.Property(receipt => receipt.CommittedAt).HasColumnName("committed_at");
            entity.Property(receipt => receipt.LeaseUntil).HasColumnName("lease_until");
            entity.Property(receipt => receipt.ClaimToken).HasColumnName("claim_token");
            entity.HasIndex(receipt => new
            {
                receipt.ActorScope, receipt.OperationName, receipt.TargetScope, receipt.RequestKey
            }).IsUnique();
        });
    }
}
