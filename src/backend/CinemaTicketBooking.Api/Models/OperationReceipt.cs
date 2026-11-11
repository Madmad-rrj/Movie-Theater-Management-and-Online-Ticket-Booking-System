namespace CinemaTicketBooking.Api.Models;

public sealed class OperationReceipt
{
    public Guid Id { get; set; }
    public string ActorScope { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public string TargetScope { get; set; } = string.Empty;
    public Guid RequestKey { get; set; }
    public string RequestFingerprint { get; set; } = string.Empty;
    public string ProcessingState { get; set; } = string.Empty;
    public string? ResultReference { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CommittedAt { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public Guid? ClaimToken { get; set; }
}
