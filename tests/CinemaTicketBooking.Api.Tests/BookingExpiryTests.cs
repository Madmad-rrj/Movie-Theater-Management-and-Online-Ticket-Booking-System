using System.Net;
using System.Text.Json;
using CinemaTicketBooking.Api.Services;
using CinemaTicketBooking.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CinemaTicketBooking.Api.Tests;

public sealed partial class BookingReservationTests
{
    private async Task<string> Expire(long id)
    {
        using var scope = fixture.App.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<BookingExpiryService>().TryExpireAsync(id, CancellationToken.None);
    }
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

    [Fact]
    public async Task Expiry_releases_all_seats_once_and_another_guest_can_reserve_them()
    {
        var guest = await Bootstrap(); var selection = await Create(guest, Payload([standard, vip], 2));
        using var reserved = await Reserve(guest, Reservation(selection)); var booking = await Success(reserved);
        var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await ElapseBooking(id);
        Assert.Equal("expired", await Expire(id)); Assert.Equal("skipped", await Expire(id));
        Assert.Equal(2, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE booking_id=@id AND allocation_status='released' AND released_at IS NOT NULL", ("id", id)));
        Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.audit_event WHERE event_type='booking_expired' AND resource_id=@id", ("id", id.ToString())));
        var other = await Bootstrap(); var next = await Create(other, Payload([standard, vip], 2));
        using var again = await Reserve(other, Reservation(next)); Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }
    [Theory]
    [InlineData("created")]
    [InlineData("pending")]
    [InlineData("unknown")]
    [InlineData("succeeded")]
    [InlineData("failed")]
    [InlineData("cancelled")]
    public async Task Cancellation_rejects_every_committed_payment_attempt_and_replays_rejection(string status)
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = booking.GetProperty("bookingId").GetString()!;
        await PaymentFixture(long.Parse(id), status); var key = Guid.NewGuid();
        using var response = await Cancel(guest, id, key);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("payment_initiation_committed", (await Json(response)).GetProperty("error").GetProperty("details").GetProperty("reason").GetString());
        using var replay = await Cancel(guest, id, key); Assert.Equal(HttpStatusCode.UnprocessableEntity, replay.StatusCode);
        Assert.Equal("true", replay.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE booking_id=@id AND allocation_status='held'", ("id", long.Parse(id))));
    }
    [Fact]
    public async Task Cancellation_requires_confirmation_csrf_idempotency_and_ownership()
    {
        var guest = await Bootstrap(); var stranger = await Bootstrap(); var booking = await Book(guest); var id = booking.GetProperty("bookingId").GetString()!;
        foreach (var body in new object[] { new { }, new { confirmed = false }, new { confirmed = (bool?)null }, new { confirmed = true, status = "cancelled" } })
        {
            using var response = await Cancel(guest, id, body: body); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using var noCsrf = await Cancel(guest, id, csrf: false); Assert.Equal(HttpStatusCode.Forbidden, noCsrf.StatusCode);
        using var noKey = await Send(guest, HttpMethod.Post, "api/bookings/" + id + "/cancel", new { confirmed = true });
        Assert.Equal(HttpStatusCode.BadRequest, noKey.StatusCode);
        using var foreign = await Cancel(stranger, id); Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        using var missing = await Cancel(guest, long.MaxValue.ToString()); Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var invalidId = await Cancel(guest, "abc"); Assert.Equal(HttpStatusCode.BadRequest, invalidId.StatusCode);
        using var noCookie = await Cancel(guest with { Cookie = "" }, id); Assert.Equal(HttpStatusCode.Unauthorized, noCookie.StatusCode);
        Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE booking_id=@id AND allocation_status='held'", ("id", long.Parse(id))));
        Assert.Equal(0, await Scalar("SELECT count(*) FROM cinema.operation_receipt WHERE operation_name='cancelPending'"));
    }
    [Fact]
    public async Task Concurrent_cancellation_with_same_key_commits_one_release_and_audit()
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = booking.GetProperty("bookingId").GetString()!; var key = Guid.NewGuid();
        var responses = await Task.WhenAll(Cancel(guest, id, key), Cancel(guest, id, key));
        try
        {
            Assert.All(responses, x => Assert.Equal(HttpStatusCode.OK, x.StatusCode));
            Assert.Single(responses.Where(x => x.Headers.Contains("Idempotency-Replayed")));
            Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.audit_event WHERE event_type='booking_cancelled' AND resource_id=@id", ("id", id)));
            using var newIntent = await Cancel(guest, id); Assert.Equal(HttpStatusCode.UnprocessableEntity, newIntent.StatusCode);
            Assert.Equal(2, await Scalar("SELECT version FROM cinema.booking WHERE id=@id", ("id", long.Parse(id))));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }
    [Fact]
    public async Task Cancellation_key_is_scoped_to_booking_and_freed_seats_can_be_reserved_again()
    {
        var guest = await Bootstrap(); var first = await Book(guest); var key = Guid.NewGuid();
        using var cancelFirst = await Cancel(guest, first.GetProperty("bookingId").GetString()!, key); Assert.Equal(HttpStatusCode.OK, cancelFirst.StatusCode);
        var second = await Book(guest); Assert.NotEqual(first.GetProperty("bookingId").GetString(), second.GetProperty("bookingId").GetString());
        using var cancelSecond = await Cancel(guest, second.GetProperty("bookingId").GetString()!, key); Assert.Equal(HttpStatusCode.OK, cancelSecond.StatusCode);
        Assert.Equal(0, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE allocation_status='held'"));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Release_failure_rolls_back_booking_allocations_receipt_and_audit(bool expiry)
    {
        var guest = await Bootstrap(); var selection = await Create(guest, Payload([standard, vip], 2));
        using var reserved = await Reserve(guest, Reservation(selection)); var booking = await Success(reserved); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        if (expiry) await ElapseBooking(id);
        await Sql($"""
            CREATE FUNCTION cinema.test_release_failure() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN IF NEW.seat_id={vip} AND NEW.allocation_status='released' THEN RAISE EXCEPTION 'test release failure' USING ERRCODE='23514'; END IF; RETURN NEW; END; $$;
            CREATE TRIGGER test_release_failure BEFORE UPDATE ON cinema.booking_seat FOR EACH ROW EXECUTE FUNCTION cinema.test_release_failure();
            """);
        try
        {
            if (expiry) await Assert.ThrowsAnyAsync<Exception>(() => Expire(id));
            else { using var response = await Cancel(guest, id.ToString()); Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode); }
            Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.booking WHERE id=@id AND status='pending' AND version=1", ("id", id)));
            Assert.Equal(2, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE booking_id=@id AND allocation_status='held' AND released_at IS NULL", ("id", id)));
            Assert.Equal(0, await Scalar("SELECT count(*) FROM cinema.operation_receipt WHERE operation_name='cancelPending'"));
            Assert.Equal(0, await Scalar("SELECT count(*) FROM cinema.audit_event WHERE resource_id=@id AND event_type IN ('booking_cancelled','booking_expired')", ("id", id.ToString())));
        }
        finally { await Sql("DROP TRIGGER test_release_failure ON cinema.booking_seat; DROP FUNCTION cinema.test_release_failure();"); }
        if (expiry) Assert.Equal("expired", await Expire(id));
        else { using var retry = await Cancel(guest, id.ToString()); Assert.Equal(HttpStatusCode.OK, retry.StatusCode); }
    }
    [Theory]
    [InlineData("created", false)]
    [InlineData("pending", true)]
    [InlineData("unknown", true)]
    [InlineData("succeeded", true)]
    [InlineData("failed", true)]
    [InlineData("cancelled", false)]
    public async Task Expiry_never_assumes_unresolved_or_unverified_payments_failed(string status, bool initiated)
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await ElapseBooking(id); await PaymentFixture(id, status, initiated: initiated);
        Assert.Equal("deferred", await Expire(id));
        Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE booking_id=@id AND allocation_status='held'", ("id", id)));
        Assert.Equal(0, await Scalar("SELECT count(*) FROM cinema.audit_event WHERE event_type='booking_expired' AND resource_id=@id", ("id", id.ToString())));
    }
    [Theory]
    [InlineData("failed")]
    [InlineData("cancelled")]
    public async Task Expiry_allows_verified_definitive_failure_only_after_governing_window(string status)
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await ElapseBooking(id); await PaymentFixture(id, status, initiated: true);
        await Sql("UPDATE cinema.payment SET verified_at=clock_timestamp() WHERE booking_id=@id", ("id", id));
        Assert.Equal("expired", await Expire(id));
        Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.payment WHERE booking_id=@id AND status=@status AND applied_at IS NULL", ("id", id), ("status", status)));
    }
    [Theory]
    [InlineData("window")]
    [InlineData("grace")]
    [InlineData("review")]
    [InlineData("refund")]
    [InlineData("collected")]
    [InlineData("event")]
    [InlineData("applied")]
    public async Task Expiry_preserves_each_payment_safety_guard(string guard)
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await ElapseBooking(id); await PaymentFixture(id, guard == "applied" ? "succeeded" : "failed", initiated: true, applied: guard == "applied");
        await Sql("UPDATE cinema.payment SET verified_at=clock_timestamp() WHERE booking_id=@id", ("id", id));
        switch (guard)
        {
            case "window":
                await Sql("WITH t AS (SELECT clock_timestamp() AS n) UPDATE cinema.payment SET initiated_at=t.n,payment_deadline=t.n+interval '10 minutes',grace_until=t.n+interval '610 seconds' FROM t WHERE booking_id=@id", ("id", id)); break;
            case "grace":
                await Sql("WITH t AS (SELECT clock_timestamp() AS n) UPDATE cinema.payment SET initiated_at=t.n-interval '605 seconds',payment_deadline=t.n-interval '5 seconds',grace_until=t.n+interval '5 seconds' FROM t WHERE booking_id=@id", ("id", id)); break;
            case "review": await Sql("UPDATE cinema.payment SET reconciliation_status='needs_review' WHERE booking_id=@id", ("id", id)); break;
            case "refund": await Sql("UPDATE cinema.payment SET reconciliation_status='refund_required' WHERE booking_id=@id", ("id", id)); break;
            case "collected": await Sql("UPDATE cinema.payment SET received_amount=amount WHERE booking_id=@id", ("id", id)); break;
            case "event":
                await Sql("INSERT INTO cinema.payment_event(payment_id,provider,provider_event_identity,source,sanitized_payload,received_at,verified_at) SELECT id,'MoMo',@event,'webhook','{}'::jsonb,clock_timestamp(),clock_timestamp() FROM cinema.payment WHERE booking_id=@id", ("id", id), ("event", Guid.NewGuid().ToString())); break;
        }
        Assert.Equal("deferred", await Expire(id));
        Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.booking WHERE id=@id AND status='pending'", ("id", id)));
    }
    [Theory]
    [InlineData("confirmed")]
    [InlineData("cancelled")]
    [InlineData("expired")]
    public async Task Expiry_and_cancellation_do_not_change_terminal_bookings(string state)
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await Sql("UPDATE cinema.booking SET status=@state,confirmed_at=CASE WHEN @state='confirmed' THEN clock_timestamp() END,cancelled_at=CASE WHEN @state='cancelled' THEN clock_timestamp() END,cancellation_reason=CASE WHEN @state='cancelled' THEN 'test' END WHERE id=@id; UPDATE cinema.booking_seat SET allocation_status=CASE WHEN @state='confirmed' THEN 'sold' ELSE 'released' END,released_at=CASE WHEN @state<>'confirmed' THEN clock_timestamp() END WHERE booking_id=@id", ("id", id), ("state", state));
        Assert.Equal("skipped", await Expire(id));
        using var response = await Cancel(guest, id.ToString()); Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(1, await Scalar("SELECT version FROM cinema.booking WHERE id=@id", ("id", id)));
    }
    [Fact]
    public async Task Expiry_works_after_guest_expiry_and_revocation_but_not_before_booking_deadline()
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        Assert.Equal("skipped", await Expire(id));
        await ElapseBooking(id);
        await Sql("UPDATE cinema.checkout_identity SET created_at=clock_timestamp()-interval '2 days',guest_expires_at=clock_timestamp()-interval '1 day',revoked_at=clock_timestamp() WHERE id=@id", ("id", owners[0]));
        Assert.Equal("expired", await Expire(id));
    }
    [Fact]
    public async Task Inconsistent_mixed_allocations_are_preserved_for_review()
    {
        var guest = await Bootstrap(); var selection = await Create(guest, Payload([standard, vip], 2));
        using var reserve = await Reserve(guest, Reservation(selection)); var booking = await Success(reserve); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await ElapseBooking(id);
        await Sql("UPDATE cinema.booking_seat SET allocation_status='sold' WHERE booking_id=@id AND seat_id=@seat", ("id", id), ("seat", standard));
        Assert.Equal("deferred", await Expire(id)); using var cancel = await Cancel(guest, id.ToString()); Assert.Equal(HttpStatusCode.UnprocessableEntity, cancel.StatusCode);
        Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE booking_id=@id AND allocation_status='held'", ("id", id)));
    }
    private async Task<ExpiryBatch> Batch(long cursor, int batchSize = 50)
    {
        using var scope = fixture.App.Services.CreateScope();
        var service = new BookingExpiryService(scope.ServiceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>(),
            Options.Create(new BookingExpiryOptions { BatchSize = batchSize }), NullLogger<BookingExpiryService>.Instance);
        return await service.RunBatchAsync(cursor, CancellationToken.None);
    }
    [Fact]
    public async Task Bounded_sweep_moves_past_protected_candidates_and_wraps_without_starvation()
    {
        var first = await Bootstrap(); var one = await Book(first); var id1 = long.Parse(one.GetProperty("bookingId").GetString()!);
        await PaymentFixture(id1, "unknown"); await ElapseBooking(id1);
        var second = await Bootstrap(); var selection = await Create(second, Payload([vip]));
        using var reserve = await Reserve(second, Reservation(selection)); var id2 = long.Parse((await Success(reserve)).GetProperty("bookingId").GetString()!); await ElapseBooking(id2);
        var a = await Batch(0, 1);
        Assert.Equal(1, a.Scanned); Assert.Equal(1, a.Deferred); Assert.Equal(id1, a.Cursor);
        var b = await Batch(a.Cursor, 1); Assert.Equal(1, b.Expired); Assert.Equal(id2, b.Cursor);
        var c = await Batch(b.Cursor, 1); Assert.Equal(id1, c.Cursor); Assert.Equal(1, c.Deferred);
    }
    [Fact]
    public async Task Worker_instances_and_cancellation_race_commit_one_terminal_transition()
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!); await ElapseBooking(id);
        var cancelTask = Cancel(guest, id.ToString()); var expireA = Expire(id); var expireB = Expire(id);
        using var cancel = await cancelTask; await Task.WhenAll(expireA, expireB);
        Assert.Contains(cancel.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.UnprocessableEntity });
        Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.booking WHERE id=@id AND status IN ('cancelled','expired') AND version=2", ("id", id)));
        Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.audit_event WHERE event_type IN ('booking_cancelled','booking_expired') AND resource_id=@id", ("id", id.ToString())));
        Assert.Equal(0, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE booking_id=@id AND allocation_status='held'", ("id", id)));
    }
    [Fact]
    public async Task Payment_initiation_commit_wins_against_waiting_cancellation_and_expiry_skips_busy_booking()
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!); await ElapseBooking(id);
        await using var connection = new NpgsqlConnection(fixture.ConnectionString); await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        await using var command = new NpgsqlCommand("SELECT id FROM cinema.showtime WHERE id=@showtime FOR UPDATE; SELECT id FROM cinema.booking WHERE id=@id FOR UPDATE; INSERT INTO cinema.payment(booking_id,provider,merchant_request_id,amount,status,created_at,initiation_committed_at) SELECT id,'MoMo',@key,total_amount,'created',clock_timestamp(),clock_timestamp() FROM cinema.booking WHERE id=@id", connection, tx);
        command.Parameters.AddWithValue("id", id); command.Parameters.AddWithValue("showtime", showtime); command.Parameters.AddWithValue("key", Guid.NewGuid());
        await command.ExecuteNonQueryAsync(); Assert.Equal("busy", await Expire(id));
        var pendingCancel = Cancel(guest, id.ToString()); await Task.Delay(100); Assert.False(pendingCancel.IsCompleted);
        await tx.CommitAsync(); using var cancelled = await pendingCancel; Assert.Equal(HttpStatusCode.UnprocessableEntity, cancelled.StatusCode);
        Assert.Equal("deferred", await Expire(id));
    }
    [Fact]
    public async Task Hosted_expiry_worker_processes_without_any_browser_request()
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!); await ElapseBooking(id);
        await using var workerApp = new GuestApiFactory(fixture.ConnectionString, overrides: new()
        {
            ["BookingExpiry:Enabled"] = "true",
            ["BookingExpiry:PollIntervalSeconds"] = "1"
        });
        using var startup = workerApp.CreateBrowser();
        var until = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < until && await Scalar("SELECT count(*) FROM cinema.booking WHERE id=@id AND status='expired'", ("id", id)) == 0)
            await Task.Delay(50);
        Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE booking_id=@id AND allocation_status='released'", ("id", id)));
    }
    [Fact]
    public async Task Guest_expiry_during_cancellation_rolls_back_release_and_receipt()
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await Sql("WITH t AS (SELECT clock_timestamp() AS n) UPDATE cinema.checkout_identity SET created_at=t.n-interval '5 minutes',guest_expires_at=t.n+interval '1 second' FROM t WHERE id=@id", ("id", owners[0]));
        await Sql("CREATE FUNCTION cinema.test_cancel_guest_expiry() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN PERFORM pg_sleep(1.2); RETURN NEW; END; $$; CREATE TRIGGER test_cancel_guest_expiry BEFORE UPDATE ON cinema.booking_seat FOR EACH ROW EXECUTE FUNCTION cinema.test_cancel_guest_expiry();");
        try
        {
            using var response = await Cancel(guest, id.ToString()); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.booking WHERE id=@id AND status='pending' AND version=1", ("id", id)));
            Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE booking_id=@id AND allocation_status='held'", ("id", id)));
            Assert.Equal(0, await Scalar("SELECT count(*) FROM cinema.operation_receipt WHERE operation_name='cancelPending'"));
            Assert.Equal(0, await Scalar("SELECT count(*) FROM cinema.audit_event WHERE event_type='booking_cancelled' AND resource_id=@id", ("id", id.ToString())));
        }
        finally { await Sql("DROP TRIGGER test_cancel_guest_expiry ON cinema.booking_seat; DROP FUNCTION cinema.test_cancel_guest_expiry();"); }
    }
    [Fact]
    public async Task Expiry_batch_isolates_failed_booking_and_continues_then_retries_safely()
    {
        var guest = await Bootstrap(); var first = await Book(guest); var id1 = long.Parse(first.GetProperty("bookingId").GetString()!); await ElapseBooking(id1);
        var other = await Bootstrap(); var selection = await Create(other, Payload([vip]));
        using var reserve = await Reserve(other, Reservation(selection)); var id2 = long.Parse((await Success(reserve)).GetProperty("bookingId").GetString()!); await ElapseBooking(id2);
        await Sql($"CREATE FUNCTION cinema.test_expiry_batch_failure() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.booking_id={id1} THEN RAISE EXCEPTION 'test failure' USING ERRCODE='23514'; END IF; RETURN NEW; END; $$; CREATE TRIGGER test_expiry_batch_failure BEFORE UPDATE ON cinema.booking_seat FOR EACH ROW EXECUTE FUNCTION cinema.test_expiry_batch_failure();");
        try
        {
            var batch = await Batch(0); Assert.Equal(2, batch.Scanned); Assert.Equal(1, batch.Failed); Assert.Equal(1, batch.Expired);
            Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.booking WHERE id=@id AND status='pending'", ("id", id1)));
            Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.booking WHERE id=@id AND status='expired'", ("id", id2)));
        }
        finally { await Sql("DROP TRIGGER test_expiry_batch_failure ON cinema.booking_seat; DROP FUNCTION cinema.test_expiry_batch_failure();"); }
        var retry = await Batch(0); Assert.Equal(1, retry.Expired); Assert.Equal(0, retry.Failed);
    }
}
