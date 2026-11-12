using System.Net;
using System.Text.Json;
using Xunit;

namespace CinemaTicketBooking.Api.Tests;

public sealed partial class BookingReservationTests
{
    private Task<HttpResponseMessage> Cancel(Guest guest, string id, Guid? key = null, object? body = null, bool csrf = true)
        => Send(guest, HttpMethod.Post, "api/bookings/" + id + "/cancel", body ?? new { confirmed = true }, key ?? Guid.NewGuid(), csrf);

    [Fact]
    public async Task Pending_cancellation_is_atomic_replayable_and_preserves_history()
    {
        var guest = await Bootstrap(); var selection = await Create(guest, Payload([standard, vip], 2));
        using var reserved = await Reserve(guest, Reservation(selection)); var booking = await Success(reserved);
        var id = booking.GetProperty("bookingId").GetString()!; var key = Guid.NewGuid();
        using var first = await Cancel(guest, id, key); var cancelled = await Success(first);
        Assert.Equal("cancelled", cancelled.GetProperty("status").GetString());
        Assert.Equal("2", cancelled.GetProperty("version").GetString());
        foreach (var field in new[] { "bookingId", "selectionDeadline", "financialSnapshot", "seats", "contact", "quantities", "purchasedStartAt" })
            Assert.Equal(booking.GetProperty(field).GetRawText(), cancelled.GetProperty(field).GetRawText());
        using var replay = await Cancel(guest, id, key); Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal("true", replay.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.Equal("2", (await Success(replay)).GetProperty("version").GetString());
        Assert.Equal(2, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE booking_id=@id AND allocation_status='released' AND released_at IS NOT NULL", ("id", long.Parse(id))));
        Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.audit_event WHERE resource_type='booking' AND resource_id=@id AND event_type='booking_cancelled'", ("id", id)));
        var recovery = await Recovery(guest, id); Assert.Equal("selectAgain", recovery.GetProperty("nextAction").GetString());
        Assert.Equal(0, await fixture.CountAsync("payment")); Assert.Equal(0, await fixture.CountAsync("ticket"));
    }
}
