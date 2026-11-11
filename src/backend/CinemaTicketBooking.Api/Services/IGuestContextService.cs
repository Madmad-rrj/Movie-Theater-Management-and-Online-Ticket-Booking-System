using CinemaTicketBooking.Api.DTOs.Checkout;

namespace CinemaTicketBooking.Api.Services;

public interface IGuestContextService
{
    Task<GuestBootstrapResult> BootstrapAsync(Guid requestKey, string? credential, CancellationToken cancellationToken);
    Task<GuestContextDto> ResolveAsync(string? credential, CancellationToken cancellationToken);
}

// The controller must project Context into JSON; CookieCredential is transport-only.
public sealed class GuestBootstrapResult(GuestContextDto context, string cookieCredential, bool created, bool replayed)
{
    public GuestContextDto Context { get; } = context;
    [System.Text.Json.Serialization.JsonIgnore]
    public string CookieCredential { get; } = cookieCredential;
    public bool Created { get; } = created;
    public bool Replayed { get; } = replayed;
    public override string ToString() => $"Guest bootstrap {Context.ContextId:D}";
}
