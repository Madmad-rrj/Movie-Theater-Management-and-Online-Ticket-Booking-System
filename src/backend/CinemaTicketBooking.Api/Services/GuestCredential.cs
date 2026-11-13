using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace CinemaTicketBooking.Api.Services;

// Never log or serialize this type. Only the hash is persisted.
internal sealed class GuestCredential
{
    private readonly byte[] secret;

    private GuestCredential(byte[] secret)
    {
        this.secret = secret;
        CookieValue = WebEncoders.Base64UrlEncode(secret);
        Hash = Convert.ToHexString(SHA256.HashData(secret));
    }

    public string CookieValue { get; }
    public string Hash { get; }
    public static GuestCredential Create() => new(RandomNumberGenerator.GetBytes(32));

    public static GuestCredential Parse(string? value)
    {
        if (value is not { Length: 43 })
        {
            throw CheckoutException.Unauthenticated();
        }

        try
        {
            var decoded = WebEncoders.Base64UrlDecode(value);
            if (decoded.Length == 32 && WebEncoders.Base64UrlEncode(decoded) == value)
            {
                return new GuestCredential(decoded);
            }
        }
        catch (FormatException)
        {
            // A malformed cookie has the same public outcome as an expired one.
        }

        throw CheckoutException.Unauthenticated();
    }

    public string CreateCsrfToken(Guid contextId) => WebEncoders.Base64UrlEncode(CsrfBytes(contextId));

    public bool MatchesCsrfToken(Guid contextId, string? token)
    {
        if (token is not { Length: 43 })
        {
            return false;
        }

        try
        {
            return CryptographicOperations.FixedTimeEquals(CsrfBytes(contextId), WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private byte[] CsrfBytes(Guid contextId) => HMACSHA256.HashData(
        secret, Encoding.UTF8.GetBytes($"cinema-guest-csrf-v1:{contextId:D}"));

    public override string ToString() => "[redacted guest credential]";
}
