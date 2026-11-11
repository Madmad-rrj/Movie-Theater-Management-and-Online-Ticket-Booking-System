namespace CinemaTicketBooking.Api.Models;

public sealed class CheckoutIdentity
{
    public Guid Id { get; set; }
    public long? UserId { get; set; }
    public string? GuestCredentialHash { get; set; }
    public DateTime? GuestExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long Version { get; set; } = 1;
}
