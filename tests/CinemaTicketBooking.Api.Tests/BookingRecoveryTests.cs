using System.Net;
using System.Text.Json;
using Npgsql;
using Xunit;

namespace CinemaTicketBooking.Api.Tests;

// Share the isolated reservation fixture and cleanup, not seeded production IDs.
public sealed partial class BookingReservationTests
{
    private async Task<JsonElement> Book(Guest guest)
    {
        var selection = await Create(guest);
        using var response = await Reserve(guest, Reservation(selection));
        return await Success(response);
    }
    private async Task<JsonElement> Recovery(Guest guest, string id)
    {
        using var response = await Send(guest, HttpMethod.Get, "api/bookings/" + id + "/checkout", csrf: false);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        return (await Json(response)).GetProperty("data");
    }
    private async Task PaymentFixture(long id, string status, string reconciliation = "none", bool initiated = false, bool applied = false)
    {
        // SQL fixtures only in the disposable test database. No gateway calls.
        await Sql("""
            WITH t AS (SELECT clock_timestamp() AS n)
            INSERT INTO cinema.payment(booking_id,provider,merchant_request_id,amount,currency,status,created_at,
                initiation_committed_at,initiated_at,payment_deadline,grace_until,reconciliation_status,
                verified_at,paid_at,received_amount,received_currency,provider_transaction_id,applied_at)
            SELECT id,'MoMo',@key,total_amount,'VND',@status,t.n,t.n,
                CASE WHEN @initiated THEN t.n-interval '11 minutes' END,
                CASE WHEN @initiated THEN t.n-interval '1 minute' END,
                CASE WHEN @initiated THEN t.n-interval '50 seconds' END,@reconciliation,
                CASE WHEN @status='succeeded' THEN t.n END,
                CASE WHEN @status='succeeded' THEN t.n END,
                CASE WHEN @status='succeeded' THEN total_amount END,
                CASE WHEN @status='succeeded' THEN 'VND' END,
                CASE WHEN @status='succeeded' THEN @transaction END,
                CASE WHEN @applied THEN t.n END
            FROM cinema.booking CROSS JOIN t WHERE id=@id
            """, ("id", id), ("key", Guid.NewGuid()), ("status", status), ("reconciliation", reconciliation),
            ("initiated", initiated), ("applied", applied), ("transaction", Guid.NewGuid().ToString()));
    }
    [Fact]
    public async Task Booking_reads_and_reloads_preserve_deadlines_snapshots_and_have_no_writes()
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = booking.GetProperty("bookingId").GetString()!;
        var receipts = await fixture.CountAsync("operation_receipt");
        var audits = await fixture.CountAsync("audit_event");
        var guestExpiry = await Scalar("SELECT extract(epoch FROM guest_expires_at)::bigint FROM cinema.checkout_identity WHERE id=@id", ("id", owners[0]));
        for (var i = 0; i < 3; i++)
        {
            using var read = await Send(guest, HttpMethod.Get, "api/bookings/" + id, csrf: false);
            Assert.Equal(HttpStatusCode.OK, read.StatusCode); Assert.True(read.Headers.CacheControl?.NoStore);
            var summary = (await Json(read)).GetProperty("data");
            var recovery = await Recovery(guest, id);
            Assert.Equal("pay", recovery.GetProperty("nextAction").GetString());
            Assert.Equal(JsonValueKind.Null, recovery.GetProperty("payment").ValueKind);
            foreach (var field in new[] { "bookingId", "bookingCode", "status", "selectionDeadline", "financialSnapshot", "seats", "quantities", "contact", "version", "createdAt", "purchasedStartAt" })
            {
                Assert.Equal(booking.GetProperty(field).GetRawText(), summary.GetProperty(field).GetRawText());
                Assert.Equal(booking.GetProperty(field).GetRawText(), recovery.GetProperty("booking").GetProperty(field).GetRawText());
            }
            Assert.Equal(recovery.GetProperty("serverTime").GetString(), recovery.GetProperty("booking").GetProperty("showtime").GetProperty("serverTime").GetString());
        }
        Assert.Equal(1, await fixture.CountAsync("booking")); Assert.Equal(1, await fixture.CountAsync("booking_seat"));
        Assert.Equal(receipts, await fixture.CountAsync("operation_receipt")); Assert.Equal(audits, await fixture.CountAsync("audit_event"));
        Assert.Equal(guestExpiry, await Scalar("SELECT extract(epoch FROM guest_expires_at)::bigint FROM cinema.checkout_identity WHERE id=@id", ("id", owners[0])));
        foreach (var table in new[] { "payment", "ticket", "notification_request" }) Assert.Equal(0, await fixture.CountAsync(table));
    }
    [Theory]
    [InlineData("")]
    [InlineData("/checkout")]
    public async Task Booking_reads_hide_foreign_and_missing_resources(string suffix)
    {
        var owner = await Bootstrap(); var stranger = await Bootstrap(); var booking = await Book(owner);
        foreach (var id in new[] { booking.GetProperty("bookingId").GetString()!, long.MaxValue.ToString() })
        {
            using var response = await Send(stranger, HttpMethod.Get, "api/bookings/" + id + suffix, csrf: false);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var error = (await Json(response)).GetProperty("error"); Assert.Equal("NOT_FOUND", error.GetProperty("code").GetString());
            Assert.Equal("{}", error.GetProperty("details").GetRawText());
            Assert.DoesNotContain("guest@example.invalid", await response.Content.ReadAsStringAsync());
        }
    }
    [Theory]
    [InlineData("", "missing")]
    [InlineData("/checkout", "missing")]
    [InlineData("", "invalid")]
    [InlineData("/checkout", "invalid")]
    [InlineData("", "expired")]
    [InlineData("/checkout", "expired")]
    [InlineData("", "revoked")]
    [InlineData("/checkout", "revoked")]
    public async Task Booking_reads_require_current_guest_proof(string suffix, string state)
    {
        var guest = await Bootstrap(); var booking = await Book(guest);
        if (state == "expired") await Sql("UPDATE cinema.checkout_identity SET created_at=clock_timestamp()-interval '2 days',guest_expires_at=clock_timestamp()-interval '1 day' WHERE id=@id", ("id", owners[0]));
        if (state == "revoked") await Sql("UPDATE cinema.checkout_identity SET revoked_at=clock_timestamp() WHERE id=@id", ("id", owners[0]));
        if (state is "missing" or "invalid") guest = guest with { Cookie = state == "missing" ? "" : "cinema_guest=invalid" };
        using var response = await Send(guest, HttpMethod.Get, "api/bookings/" + booking.GetProperty("bookingId").GetString() + suffix, csrf: false);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHENTICATED", (await Json(response)).GetProperty("error").GetProperty("code").GetString());
        Assert.DoesNotContain("guest@example.invalid", await response.Content.ReadAsStringAsync());
    }
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("9223372036854775808")]
    [InlineData("00000000-0000-4000-8000-000000000001")]
    public async Task Booking_reads_validate_decimal_string_ids(string id)
    {
        var guest = await Bootstrap();
        foreach (var suffix in new[] { "", "/checkout" })
        {
            using var response = await Send(guest, HttpMethod.Get, "api/bookings/" + id + suffix, csrf: false);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("VALIDATION_ERROR", (await Json(response)).GetProperty("error").GetProperty("code").GetString());
        }
    }
    [Fact]
    public async Task Recovery_keeps_committed_prices_and_purchase_time_when_official_data_changes()
    {
        var guest = await Bootstrap(); var booking = await Book(guest);
        await Sql("UPDATE cinema.cinema_ticket_price SET amount=amount+1000,version=version+1 WHERE cinema_id=@id AND category_code='Adult'", ("id", cinema));
        await Sql("UPDATE cinema.showtime SET starts_at=starts_at+interval '1 hour',ends_at=ends_at+interval '1 hour',room_available_at=room_available_at+interval '1 hour',version=version+1 WHERE id=@id", ("id", showtime));
        var data = await Recovery(guest, booking.GetProperty("bookingId").GetString()!); var current = data.GetProperty("booking");
        Assert.Equal(booking.GetProperty("financialSnapshot").GetRawText(), current.GetProperty("financialSnapshot").GetRawText());
        Assert.Equal(booking.GetProperty("seats").GetRawText(), current.GetProperty("seats").GetRawText());
        Assert.Equal(booking.GetProperty("purchasedStartAt").GetString(), current.GetProperty("purchasedStartAt").GetString());
        Assert.NotEqual(current.GetProperty("purchasedStartAt").GetString(), current.GetProperty("showtime").GetProperty("officialStartsAt").GetString());
    }
    [Theory]
    [InlineData("expired")]
    [InlineData("cancelled")]
    public async Task Terminal_booking_recovers_as_select_again_without_reactivation(string state)
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await Sql("""
            UPDATE cinema.booking SET status=@state,version=version+1,
                cancelled_at=CASE WHEN @state='cancelled' THEN clock_timestamp() END,
                cancellation_reason=CASE WHEN @state='cancelled' THEN 'Test cancellation' END WHERE id=@id;
            UPDATE cinema.booking_seat SET allocation_status='released',released_at=clock_timestamp() WHERE booking_id=@id;
            """, ("id", id), ("state", state));
        var data = await Recovery(guest, id.ToString());
        Assert.Equal(state, data.GetProperty("booking").GetProperty("status").GetString());
        Assert.Equal("selectAgain", data.GetProperty("nextAction").GetString());
        Assert.Equal(booking.GetProperty("selectionDeadline").GetString(), data.GetProperty("booking").GetProperty("selectionDeadline").GetString());
        Assert.Equal(0, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE booking_id=@id AND allocation_status<>'released'", ("id", id)));
    }
    private Task ElapseBooking(long id) => Sql("UPDATE cinema.booking SET created_at=clock_timestamp()-interval '10 minutes',reservation_expires_at=clock_timestamp()-interval '5 minutes' WHERE id=@id", ("id", id));
    [Fact]
    public async Task Elapsed_pending_deadline_is_not_a_valid_checkout_and_GET_does_not_release_holds()
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await ElapseBooking(id);
        var data = await Recovery(guest, id.ToString());
        Assert.Equal("selectAgain", data.GetProperty("nextAction").GetString());
        Assert.Equal("pending", data.GetProperty("booking").GetProperty("status").GetString());
        Assert.True(data.GetProperty("booking").GetProperty("selectionDeadline").GetDateTime() < data.GetProperty("serverTime").GetDateTime());
        Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE booking_id=@id AND allocation_status='held'", ("id", id)));
    }
    [Fact]
    public async Task Ineligible_showtime_does_not_offer_payment_and_still_returns_history()
    {
        var guest = await Bootstrap(); var booking = await Book(guest);
        await Sql("UPDATE cinema.showtime SET status='draft' WHERE id=@id", ("id", showtime));
        try
        {
            var data = await Recovery(guest, booking.GetProperty("bookingId").GetString()!);
            Assert.Equal("selectAgain", data.GetProperty("nextAction").GetString());
            Assert.False(data.GetProperty("booking").GetProperty("showtime").GetProperty("bookingAvailable").GetBoolean());
        }
        finally { await Sql("UPDATE cinema.showtime SET status='scheduled' WHERE id=@id", ("id", showtime)); }
    }
    [Theory]
    [InlineData("created", false)]
    [InlineData("pending", true)]
    [InlineData("unknown", true)]
    [InlineData("succeeded", true)]
    public async Task Unresolved_payment_survives_elapsed_deadlines_without_new_payment(string state, bool initiated)
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await ElapseBooking(id); await PaymentFixture(id, state, initiated: initiated);
        var data = await Recovery(guest, id.ToString()); var payment = data.GetProperty("payment");
        Assert.Equal("awaitOutcome", data.GetProperty("nextAction").GetString());
        Assert.Equal(state, payment.GetProperty("status").GetString());
        Assert.Equal(id.ToString(), payment.GetProperty("bookingId").GetString());
        Assert.Equal(payment.GetProperty("paymentId").GetString(), data.GetProperty("booking").GetProperty("activePaymentId").GetString());
        Assert.Equal(payment.GetProperty("paymentDeadline").GetRawText(), data.GetProperty("booking").GetProperty("paymentDeadline").GetRawText());
        Assert.Equal(data.GetProperty("serverTime").GetString(), payment.GetProperty("serverTime").GetString());
        foreach (var secret in new[] { "merchantRequestId", "providerTransactionId", "receivedAmount", "claimToken", "nextReconcileAt" }) Assert.False(payment.TryGetProperty(secret, out _));
        Assert.Equal(1, await fixture.CountAsync("payment"));
        Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE booking_id=@id AND allocation_status='held'", ("id", id)));
    }
    [Theory]
    [InlineData("needs_review")]
    [InlineData("refund_required")]
    public async Task Financial_review_requires_support(string reconciliation)
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await PaymentFixture(id, "succeeded", reconciliation);
        var data = await Recovery(guest, id.ToString()); Assert.Equal("contactSupport", data.GetProperty("nextAction").GetString());
        Assert.Equal(reconciliation, data.GetProperty("payment").GetProperty("reconciliationStatus").GetString());
    }
    [Fact]
    public async Task Older_unresolved_payment_takes_precedence_over_newer_failure()
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await PaymentFixture(id, "unknown"); await PaymentFixture(id, "failed");
        var data = await Recovery(guest, id.ToString()); Assert.Equal("awaitOutcome", data.GetProperty("nextAction").GetString());
        Assert.Equal("unknown", data.GetProperty("payment").GetProperty("status").GetString());
    }
    [Theory]
    [InlineData("failed")]
    [InlineData("cancelled")]
    public async Task Terminal_attempt_does_not_offer_a_new_payment_implicitly(string state)
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await PaymentFixture(id, state);
        var data = await Recovery(guest, id.ToString()); Assert.Equal("contactSupport", data.GetProperty("nextAction").GetString());
        Assert.Equal(state, data.GetProperty("payment").GetProperty("status").GetString());
    }
    [Fact]
    public async Task Confirmed_booking_requires_existing_valid_ticket_and_never_exposes_QR()
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await Sql("UPDATE cinema.booking SET status='confirmed',confirmed_at=clock_timestamp(),version=version+1 WHERE id=@id; UPDATE cinema.booking_seat SET allocation_status='sold' WHERE booking_id=@id", ("id", id));
        await PaymentFixture(id, "succeeded", applied: true);
        var data = await Recovery(guest, id.ToString()); Assert.Equal("contactSupport", data.GetProperty("nextAction").GetString());
        await Sql("INSERT INTO cinema.ticket(booking_id,ticket_code,qr_lookup_hash,encrypted_qr_credential,qr_key_version,issued_at) VALUES(@id,@code,@hash,decode('01','hex'),'test',clock_timestamp())", ("id", id), ("code", Guid.NewGuid()), ("hash", Guid.NewGuid().ToString()));
        data = await Recovery(guest, id.ToString()); Assert.Equal("viewTicket", data.GetProperty("nextAction").GetString());
        Assert.DoesNotContain("qrCredential", data.GetRawText()); Assert.Equal("confirmed", data.GetProperty("booking").GetProperty("status").GetString());
        await Sql("UPDATE cinema.ticket SET status='voided',voided_at=clock_timestamp(),void_reason='test' WHERE booking_id=@id", ("id", id));
        data = await Recovery(guest, id.ToString()); Assert.Equal("contactSupport", data.GetProperty("nextAction").GetString());
    }
    [Fact]
    public async Task Zero_total_checkout_is_not_automatically_confirmed_or_offered_for_payment()
    {
        await Sql("UPDATE cinema.cinema_ticket_price SET amount=0 WHERE cinema_id=@id AND category_code='Adult'", ("id", cinema));
        var guest = await Bootstrap(); var booking = await Book(guest);
        var data = await Recovery(guest, booking.GetProperty("bookingId").GetString()!);
        Assert.Equal("contactSupport", data.GetProperty("nextAction").GetString());
        Assert.Equal("pending", data.GetProperty("booking").GetProperty("status").GetString());
        Assert.Equal(0, await fixture.CountAsync("payment"));
    }

    [Theory]
    [InlineData("pending_review", "pending", false)]
    [InlineData("approved_awaiting_refund", "approved", true)]
    public async Task Recovery_projects_existing_cancellation_without_exposing_review_details(string state, string projected, bool blocked)
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await Sql("UPDATE cinema.booking SET status='confirmed',confirmed_at=clock_timestamp() WHERE id=@id; UPDATE cinema.booking_seat SET allocation_status='sold' WHERE booking_id=@id", ("id", id));
        await PaymentFixture(id, "succeeded", applied: true);
        await Sql("INSERT INTO cinema.ticket(booking_id,ticket_code,qr_lookup_hash,encrypted_qr_credential,qr_key_version,issued_at) VALUES(@id,@code,@hash,decode('01','hex'),'test',clock_timestamp())", ("id", id), ("code", Guid.NewGuid()), ("hash", Guid.NewGuid().ToString()));
        // A reviewer's FK fixture only; the checkout owner remains a real Guest.
        var reviewerName = "reviewer-" + Guid.NewGuid();
        var reviewer = await Scalar("INSERT INTO cinema.app_user(user_name,normalized_user_name,primary_email,normalized_email,full_name) VALUES(@name,@name,@email,@email,'Test Reviewer') RETURNING id", ("name", reviewerName), ("email", reviewerName + "@example.invalid"));
        try
        {
            await Sql("""
            INSERT INTO cinema.cancellation_request(booking_id,requested_by_identity_id,request_key,requested_at,
                purchased_start_snapshot,official_start_at_request,reason,status,reviewed_by_user_id,reviewed_at)
            SELECT id,checkout_identity_id,@key,clock_timestamp(),purchased_start_at,purchased_start_at+interval '1 hour',
                'Private test reason',@state,CASE WHEN @approved THEN @reviewer END,
                CASE WHEN @approved THEN clock_timestamp() END FROM cinema.booking WHERE id=@id
            """, ("id", id), ("key", Guid.NewGuid()), ("state", state), ("approved", blocked), ("reviewer", reviewer));
            var data = await Recovery(guest, id.ToString()); var cancellation = data.GetProperty("booking").GetProperty("cancellation");
            Assert.Equal(projected, cancellation.GetProperty("status").GetString());
            Assert.Equal(blocked, cancellation.GetProperty("blocksAdmission").GetBoolean());
            Assert.Equal(blocked ? "contactSupport" : "viewTicket", data.GetProperty("nextAction").GetString());
            Assert.DoesNotContain("Private test reason", data.GetRawText());
        }
        finally
        {
            await Sql("DELETE FROM cinema.cancellation_request WHERE booking_id=@id; DELETE FROM cinema.app_user WHERE id=@reviewer", ("id", id), ("reviewer", reviewer));
        }
    }

    [Fact]
    public async Task Recovery_retries_a_concurrent_committed_state_change_as_one_consistent_snapshot()
    {
        var guest = await Bootstrap(); var booking = await Book(guest); var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await using var connection = new NpgsqlConnection(fixture.ConnectionString); await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT id FROM cinema.showtime WHERE id=@showtime FOR UPDATE;
            SELECT id FROM cinema.booking WHERE id=@id FOR UPDATE;
            UPDATE cinema.booking SET status='expired',version=version+1 WHERE id=@id;
            UPDATE cinema.booking_seat SET allocation_status='released',released_at=clock_timestamp() WHERE booking_id=@id;
            """, connection, tx);
        command.Parameters.AddWithValue("showtime", showtime); command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
        var read = Recovery(guest, id.ToString());
        await Task.Delay(100); Assert.False(read.IsCompleted);
        await tx.CommitAsync();
        var data = await read;
        Assert.Equal("expired", data.GetProperty("booking").GetProperty("status").GetString());
        Assert.Equal("selectAgain", data.GetProperty("nextAction").GetString());
        Assert.Equal("2", data.GetProperty("booking").GetProperty("version").GetString());
        Assert.Equal(0, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE booking_id=@id AND allocation_status='held'", ("id", id)));
    }
}
