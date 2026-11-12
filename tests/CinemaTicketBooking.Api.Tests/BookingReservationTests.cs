using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;
using Xunit;

namespace CinemaTicketBooking.Api.Tests;

[Collection("Guest PostgreSQL")]
public sealed class BookingReservationTests(GuestApiFixture fixture) : IAsyncLifetime
{
    private readonly List<Guid> owners = [];
    private long showtime, standard, vip, otherRoom, cinema;
    private decimal adultPrice;
    private long priceVersion;
    private HttpClient client = null!;
    private sealed record Guest(string Cookie, string Csrf);

    public async Task InitializeAsync()
    {
        client = fixture.App.CreateBrowser();
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT s.id,c.id,
              (SELECT se.id FROM cinema.seat se JOIN cinema.seat_type st ON st.id=se.seat_type_id WHERE se.cinema_room_id=r.id AND st.surcharge_amount=0 ORDER BY se.id LIMIT 1),
              (SELECT se.id FROM cinema.seat se JOIN cinema.seat_type st ON st.id=se.seat_type_id WHERE se.cinema_room_id=r.id AND st.surcharge_amount>0 ORDER BY se.id LIMIT 1),
              (SELECT se.id FROM cinema.seat se WHERE se.cinema_room_id<>r.id ORDER BY se.id LIMIT 1),p.amount,p.version
            FROM cinema.showtime s JOIN cinema.cinema_room r ON r.id=s.cinema_room_id
            JOIN cinema.cinema c ON c.id=r.cinema_id JOIN cinema.cinema_ticket_price p ON p.cinema_id=c.id AND p.category_code='Adult'
            ORDER BY s.id LIMIT 1
            """, connection);
        await using (var r = await command.ExecuteReaderAsync())
        {
            Assert.True(await r.ReadAsync()); showtime = r.GetInt64(0); cinema = r.GetInt64(1);
            standard = r.GetInt64(2); vip = r.GetInt64(3); otherRoom = r.GetInt64(4); adultPrice = r.GetDecimal(5); priceVersion = r.GetInt64(6);
        }
        // A controlled future fixture; production uses actual DB time unchanged.
        await Sql("UPDATE cinema.showtime SET starts_at=date_trunc('day',clock_timestamp())+interval '365 days', ends_at=date_trunc('day',clock_timestamp())+interval '365 days'+duration_minutes_snapshot*interval '1 minute', room_available_at=date_trunc('day',clock_timestamp())+interval '365 days'+(duration_minutes_snapshot+turnaround_minutes_applied)*interval '1 minute' WHERE id=@id", ("id", showtime));
    }
    public async Task DisposeAsync()
    {
        await Sql("DELETE FROM cinema.booking_seat WHERE booking_id IN (SELECT id FROM cinema.booking WHERE checkout_identity_id=ANY(@owners))", ("owners", owners.ToArray()));
        await Sql("DELETE FROM cinema.booking_category WHERE booking_id IN (SELECT id FROM cinema.booking WHERE checkout_identity_id=ANY(@owners))", ("owners", owners.ToArray()));
        await Sql("DELETE FROM cinema.booking WHERE checkout_identity_id=ANY(@owners)", ("owners", owners.ToArray()));
        await Sql("DELETE FROM cinema.selection_seat WHERE selection_id IN (SELECT id FROM cinema.selection_session WHERE checkout_identity_id=ANY(@owners))", ("owners", owners.ToArray()));
        await Sql("DELETE FROM cinema.operation_receipt WHERE actor_scope=ANY(@scopes)", ("scopes", owners.Select(o => o.ToString()).ToArray()));
        await Sql("DELETE FROM cinema.selection_session WHERE checkout_identity_id=ANY(@owners)", ("owners", owners.ToArray()));
        await Sql("UPDATE cinema.cinema_ticket_price SET amount=@amount,version=@version WHERE cinema_id=@id AND category_code='Adult'", ("amount", adultPrice), ("version", priceVersion), ("id", cinema));
        client.Dispose();
    }
    private async Task Sql(string sql, params (string, object)[] parameters)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString); await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync();
    }
    private async Task<Guest> Bootstrap()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/guest-context");
        request.Headers.Add("Origin", "http://127.0.0.1:5173"); request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        using var response = await client.SendAsync(request); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await Json(response); owners.Add(json.GetProperty("data").GetProperty("contextId").GetGuid());
        return new(response.Headers.GetValues("Set-Cookie").Single().Split(';')[0], response.Headers.GetValues("X-CSRF-Token").Single());
    }
    private object Payload(long[]? seats = null, int adult = 1, int discounted = 0) => new { showtimeId = showtime.ToString(), quantities = new { adult, discounted }, seatIds = (seats ?? [standard]).Select(s => s.ToString()).ToArray() };
    private async Task<HttpResponseMessage> Send(Guest guest, HttpMethod method, string path, object? body = null, Guid? key = null, bool csrf = true)
    {
        using var request = new HttpRequestMessage(method, path); request.Headers.Add("Cookie", guest.Cookie);
        request.Headers.Add("Origin", "http://127.0.0.1:5173"); if (csrf) request.Headers.Add("X-CSRF-Token", guest.Csrf);
        if (key is not null) request.Headers.Add("Idempotency-Key", key.ToString());
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }
    private static async Task<JsonElement> Json(HttpResponseMessage response)
    { using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); return doc.RootElement.Clone(); }
    private async Task<JsonElement> Create(Guest guest, object? body = null)
    {
        using var response = await Send(guest, HttpMethod.Post, "api/selections", body ?? Payload(), Guid.NewGuid());
        if (response.StatusCode != HttpStatusCode.Created) throw new InvalidOperationException(string.Join("\n", fixture.App.Logs.Messages.Where(x => x.Contains("Exception")).TakeLast(2)));
        return (await Json(response)).GetProperty("data");
    }

    private static JsonObject Reservation(JsonElement selection)
    {
        return JsonSerializer.SerializeToNode(new
        {
            selectionId = selection.GetProperty("selectionId").GetGuid(),
            selectionVersion = selection.GetProperty("version").GetString(),
            showtimeId = selection.GetProperty("showtimeId").GetString(),
            seatIds = selection.GetProperty("seatIds"),
            quantities = selection.GetProperty("quantities"),
            contact = new { fullName = "Demo Guest", phone = "0900000000", email = "guest@example.invalid" },
            declarations = new { termsAccepted = true, ageRequirementsAcknowledged = true },
            priceAcceptance = new
            {
                quoteId = selection.GetProperty("quote").GetProperty("quoteId"),
                version = selection.GetProperty("quote").GetProperty("version"),
                acceptedTotal = selection.GetProperty("quote").GetProperty("total")
            }
        })!.AsObject();
    }
    private async Task<HttpResponseMessage> Reserve(Guest guest, JsonObject body, Guid? key = null)
        => await Send(guest, HttpMethod.Post, "api/bookings", body, key ?? Guid.NewGuid());
    private async Task<JsonElement> Success(HttpResponseMessage response)
    {
        if (response.StatusCode is not (HttpStatusCode.Created or HttpStatusCode.OK))
            throw new InvalidOperationException($"Expected reservation success, got {(int)response.StatusCode}: " + await response.Content.ReadAsStringAsync());
        return (await Json(response)).GetProperty("data");
    }
    [Fact]
    public async Task Creates_pending_booking_with_full_snapshots_categories_and_atomic_holds()
    {
        var guest = await Bootstrap(); var selection = await Create(guest, Payload([standard, vip], 1, 1));
        using var response = await Reserve(guest, Reservation(selection));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = await Success(response);
        Assert.Equal("pending", booking.GetProperty("status").GetString());
        Assert.Equal(185000, booking.GetProperty("financialSnapshot").GetProperty("total").GetProperty("amount").GetDecimal());
        Assert.Equal(selection.GetProperty("selectionDeadline").GetString(), booking.GetProperty("selectionDeadline").GetString());
        Assert.Equal("Demo Guest", booking.GetProperty("contact").GetProperty("fullName").GetString());
        Assert.Equal(2, booking.GetProperty("seats").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, booking.GetProperty("paymentDeadline").ValueKind);
        Assert.Equal(JsonValueKind.Null, booking.GetProperty("activePaymentId").ValueKind);
        Assert.Equal(2, await fixture.CountAsync("booking_category"));
        Assert.Equal(2, await fixture.CountAsync("booking_seat"));
        Assert.Equal(2, await Scalar("SELECT count(*) FROM cinema.booking_seat WHERE allocation_status='held'"));
        Assert.Equal(1, await Scalar("SELECT count(*) FROM cinema.audit_event WHERE actor_scope=@owner AND event_type='booking_reserved'", ("owner", owners[0].ToString())));
        foreach (var table in new[] { "payment", "ticket", "notification_request" }) Assert.Equal(0, await fixture.CountAsync(table));
        using var read = await Send(guest, HttpMethod.Get, "api/selections/" + selection.GetProperty("selectionId").GetString());
        var consumed = (await Json(read)).GetProperty("data");
        Assert.Equal("reserved", consumed.GetProperty("state").GetString());
        Assert.Equal(booking.GetProperty("bookingId").GetString(), consumed.GetProperty("bookingId").GetString());
    }
    [Fact]
    public async Task Identical_replay_returns_same_booking_without_new_holds_and_changed_payload_conflicts()
    {
        var guest = await Bootstrap(); var selection = await Create(guest); var body = Reservation(selection); var key = Guid.NewGuid();
        using var first = await Reserve(guest, body, key); var booking = await Success(first);
        using var replay = await Reserve(guest, body, key); Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal("true", replay.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.Equal(booking.GetProperty("bookingId").GetString(), (await Success(replay)).GetProperty("bookingId").GetString());
        body["contact"]!["fullName"] = "Another Demo Guest";
        using var conflict = await Reserve(guest, body, key); Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("IDEMPOTENCY_KEY_CONFLICT", (await Json(conflict)).GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(1, await fixture.CountAsync("booking")); Assert.Equal(1, await fixture.CountAsync("booking_seat"));
    }
    [Fact]
    public async Task Concurrent_same_key_commits_once_and_returns_same_outcome()
    {
        var guest = await Bootstrap(); var selection = await Create(guest); var body = Reservation(selection); var key = Guid.NewGuid();
        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Reserve(guest, body, key)));
        try
        {
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
            var ids = new List<string>(); foreach (var response in responses) ids.Add((await Success(response)).GetProperty("bookingId").GetString()!);
            Assert.Single(ids.Distinct()); Assert.Equal(1, await fixture.CountAsync("booking")); Assert.Equal(1, await fixture.CountAsync("booking_seat"));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }
    [Fact]
    public async Task Different_guests_contending_for_same_seat_have_one_winner()
    {
        var a = await Bootstrap(); var b = await Bootstrap();
        var sa = await Create(a); var sb = await Create(b);
        var responses = await Task.WhenAll(Reserve(a, Reservation(sa)), Reserve(b, Reservation(sb)));
        try
        {
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
            var loser = Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal("SEAT_CONFLICT", (await Json(loser)).GetProperty("error").GetProperty("code").GetString());
            Assert.Equal(1, await fixture.CountAsync("booking")); Assert.Equal(1, await fixture.CountAsync("booking_seat"));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }
    [Fact]
    public async Task Multi_seat_conflict_does_not_hold_the_uncontested_seat_or_consume_selection()
    {
        var a = await Bootstrap(); var b = await Bootstrap(); var sa = await Create(a);
        var sb = await Create(b, Payload([standard, vip], 2));
        using var winner = await Reserve(a, Reservation(sa)); await Success(winner);
        var key = Guid.NewGuid(); var body = Reservation(sb);
        using var loser = await Reserve(b, body, key); Assert.Equal(HttpStatusCode.Conflict, loser.StatusCode);
        Assert.Equal(1, await fixture.CountAsync("booking_seat"));
        var winnerId = long.Parse((await Success(winner)).GetProperty("bookingId").GetString()!);
        await Sql("UPDATE cinema.booking_seat SET allocation_status='released',released_at=clock_timestamp() WHERE booking_id=@id; UPDATE cinema.booking SET status='expired' WHERE id=@id", ("id", winnerId));
        using var replay = await Reserve(b, body, key); Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);
        Assert.Equal("true", replay.Headers.GetValues("Idempotency-Replayed").Single());
        using var read = await Send(b, HttpMethod.Get, "api/selections/" + sb.GetProperty("selectionId").GetString());
        Assert.Equal("active", (await Json(read)).GetProperty("data").GetProperty("state").GetString());
    }
    [Fact]
    public async Task Late_unique_constraint_failure_rolls_back_booking_categories_and_partial_allocations()
    {
        var guest = await Bootstrap(); var selection = await Create(guest, Payload([standard, vip], 2));
        await Sql($"""
            CREATE FUNCTION cinema.test_reservation_conflict() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN IF NEW.seat_id={vip} THEN RAISE EXCEPTION 'injected allocation conflict'
            USING ERRCODE='23505', CONSTRAINT='ux_active_seat'; END IF; RETURN NEW; END; $$;
            CREATE TRIGGER test_reservation_conflict BEFORE INSERT ON cinema.booking_seat
            FOR EACH ROW EXECUTE FUNCTION cinema.test_reservation_conflict();
            """);
        try
        {
            using var response = await Reserve(guest, Reservation(selection));
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("SEAT_CONFLICT", (await Json(response)).GetProperty("error").GetProperty("code").GetString());
            foreach (var table in new[] { "booking", "booking_seat", "booking_category" }) Assert.Equal(0, await fixture.CountAsync(table));
            using var read = await Send(guest, HttpMethod.Get, "api/selections/" + selection.GetProperty("selectionId").GetString());
            Assert.Equal("active", (await Json(read)).GetProperty("data").GetProperty("state").GetString());
        }
        finally { await Sql("DROP TRIGGER test_reservation_conflict ON cinema.booking_seat; DROP FUNCTION cinema.test_reservation_conflict();"); }
    }
    [Fact]
    public async Task Price_change_requires_explicit_reacceptance_and_replay_preserves_rejected_outcome()
    {
        var guest = await Bootstrap(); var selection = await Create(guest); var body = Reservation(selection); var key = Guid.NewGuid();
        await Sql("UPDATE cinema.cinema_ticket_price SET amount=amount+1000,version=version+1 WHERE cinema_id=@id AND category_code='Adult'", ("id", cinema));
        using var changed = await Reserve(guest, body, key); Assert.Equal(HttpStatusCode.UnprocessableEntity, changed.StatusCode);
        var error = (await Json(changed)).GetProperty("error"); Assert.Equal("PRICE_CHANGED", error.GetProperty("code").GetString());
        Assert.Equal(adultPrice + 1000, error.GetProperty("details").GetProperty("quote").GetProperty("total").GetProperty("amount").GetDecimal());
        Assert.Equal(0, await fixture.CountAsync("booking")); Assert.Equal(0, await fixture.CountAsync("booking_seat"));
        using var read = await Send(guest, HttpMethod.Get, "api/selections/" + selection.GetProperty("selectionId").GetString());
        var current = (await Json(read)).GetProperty("data");
        Assert.Equal(selection.GetProperty("selectionDeadline").GetString(), current.GetProperty("selectionDeadline").GetString());
        using var accepted = await Reserve(guest, Reservation(current)); var booking = await Success(accepted);
        Assert.Equal(adultPrice + 1000, booking.GetProperty("financialSnapshot").GetProperty("total").GetProperty("amount").GetDecimal());
        using var replay = await Reserve(guest, body, key); Assert.Equal(HttpStatusCode.UnprocessableEntity, replay.StatusCode);
        Assert.Equal("PRICE_CHANGED", (await Json(replay)).GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("true", replay.Headers.GetValues("Idempotency-Replayed").Single());
    }
    [Fact]
    public async Task Expired_selection_cannot_reserve_and_failure_does_not_create_holds()
    {
        var guest = await Bootstrap(); var selection = await Create(guest); var id = selection.GetProperty("selectionId").GetGuid();
        await Sql("UPDATE cinema.selection_session SET started_at=date_trunc('second',clock_timestamp())-interval '10 minutes',expires_at=date_trunc('second',clock_timestamp())-interval '5 minutes' WHERE id=@id", ("id", id));
        using var response = await Reserve(guest, Reservation(selection)); Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("SELECTION_EXPIRED", (await Json(response)).GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(0, await fixture.CountAsync("booking")); Assert.Equal(0, await fixture.CountAsync("booking_seat"));
    }
    [Fact]
    public async Task Incomplete_selection_foreign_ownership_and_payload_mismatch_are_rejected()
    {
        var a = await Bootstrap(); var b = await Bootstrap(); var sa = await Create(a, Payload([standard], 2));
        using var incomplete = await Reserve(a, Reservation(sa)); Assert.Equal(HttpStatusCode.UnprocessableEntity, incomplete.StatusCode);
        using var foreign = await Reserve(b, Reservation(sa)); Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        var mismatch = Reservation(sa); mismatch["seatIds"] = new JsonArray(vip.ToString());
        using var wrong = await Reserve(a, mismatch); Assert.Equal(HttpStatusCode.Conflict, wrong.StatusCode);
        Assert.Equal(0, await fixture.CountAsync("booking"));
    }
    [Theory]
    [InlineData("name")]
    [InlineData("phone")]
    [InlineData("email")]
    [InlineData("declarations")]
    [InlineData("money")]
    [InlineData("acceptedAmount")]
    [InlineData("acceptedCurrency")]
    public async Task Invalid_contact_acknowledgements_or_money_are_rejected(string field)
    {
        var guest = await Bootstrap(); var selection = await Create(guest); var body = Reservation(selection);
        switch (field)
        {
            case "name": body["contact"]!["fullName"] = " "; break;
            case "phone": body["contact"]!["phone"] = "not a phone"; break;
            case "email": body["contact"]!["email"] = "invalid"; break;
            case "declarations": body["declarations"]!["termsAccepted"] = false; break;
            case "money": body["priceAcceptance"]!["acceptedTotal"]!["amount"] = 0.5m; break;
            case "acceptedAmount": body["priceAcceptance"]!["acceptedTotal"]!.AsObject().Remove("amount"); break;
            case "acceptedCurrency": body["priceAcceptance"]!["acceptedTotal"]!.AsObject().Remove("currency"); break;
        }
        using var response = await Reserve(guest, body); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await fixture.CountAsync("booking_seat"));
    }
    [Fact]
    public async Task Expired_booking_replay_never_reactivates_and_prices_are_snapshots()
    {
        var guest = await Bootstrap(); var selection = await Create(guest); var body = Reservation(selection); var key = Guid.NewGuid();
        using var first = await Reserve(guest, body, key); var booking = await Success(first);
        var id = long.Parse(booking.GetProperty("bookingId").GetString()!);
        await Sql("UPDATE cinema.booking_seat SET allocation_status='released',released_at=clock_timestamp() WHERE booking_id=@id; UPDATE cinema.booking SET status='expired',version=version+1 WHERE id=@id", ("id", id));
        await Sql("UPDATE cinema.cinema_ticket_price SET amount=amount+1000,version=version+1 WHERE cinema_id=@id AND category_code='Adult'", ("id", cinema));
        using var replay = await Reserve(guest, body, key); var recovered = await Success(replay);
        Assert.Equal("expired", recovered.GetProperty("status").GetString());
        Assert.Equal(booking.GetProperty("financialSnapshot").GetProperty("total").GetProperty("amount").GetDecimal(), recovered.GetProperty("financialSnapshot").GetProperty("total").GetProperty("amount").GetDecimal());
        Assert.Equal(booking.GetProperty("selectionDeadline").GetString(), recovered.GetProperty("selectionDeadline").GetString());
        Assert.Equal(1, await fixture.CountAsync("booking"));
    }
    [Fact]
    public async Task Stale_selection_and_tampered_total_are_rejected_without_allocations()
    {
        var guest = await Bootstrap(); var selection = await Create(guest); var body = Reservation(selection);
        body["selectionVersion"] = "0";
        using var stale = await Reserve(guest, body); Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("VERSION_CONFLICT", (await Json(stale)).GetProperty("error").GetProperty("code").GetString());
        body = Reservation(selection); body["priceAcceptance"]!["acceptedTotal"]!["amount"] = 1;
        using var tampered = await Reserve(guest, body); Assert.Equal(HttpStatusCode.UnprocessableEntity, tampered.StatusCode);
        Assert.Equal("PRICE_CHANGED", (await Json(tampered)).GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(0, await fixture.CountAsync("booking_seat"));
    }

    private async Task<long> Scalar(string sql, params (string, object)[] parameters)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString); await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        return (long)(await command.ExecuteScalarAsync())!;
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Expiry_during_database_write_rolls_back_all_financial_effects(bool guestExpiry)
    {
        var guest = await Bootstrap(); var selection = await Create(guest);
        if (guestExpiry)
            await Sql("WITH t AS (SELECT clock_timestamp() AS n) UPDATE cinema.checkout_identity SET created_at=t.n-interval '5 minutes',guest_expires_at=t.n+interval '2 seconds' FROM t WHERE id=@id", ("id", owners[0]));
        else
            await Sql("WITH t AS (SELECT clock_timestamp() AS n) UPDATE cinema.selection_session SET started_at=t.n-interval '298 seconds',expires_at=t.n+interval '2 seconds' FROM t WHERE id=@id", ("id", selection.GetProperty("selectionId").GetGuid()));
        await Sql("""
            CREATE FUNCTION cinema.test_expiry_during_write() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN PERFORM pg_sleep(2.2); RETURN NEW; END; $$;
            CREATE TRIGGER test_expiry_during_write BEFORE INSERT ON cinema.booking_seat
            FOR EACH ROW EXECUTE FUNCTION cinema.test_expiry_during_write();
            """);
        try
        {
            using var response = await Reserve(guest, Reservation(selection));
            Assert.Equal(guestExpiry ? HttpStatusCode.Unauthorized : HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal(guestExpiry ? "UNAUTHENTICATED" : "SELECTION_EXPIRED", (await Json(response)).GetProperty("error").GetProperty("code").GetString());
            foreach (var table in new[] { "booking", "booking_seat", "booking_category" }) Assert.Equal(0, await fixture.CountAsync(table));
            Assert.Equal(0, await Scalar("SELECT count(*) FROM cinema.audit_event WHERE actor_scope=@owner AND event_type='booking_reserved'", ("owner", owners[0].ToString())));
        }
        finally { await Sql("DROP TRIGGER test_expiry_during_write ON cinema.booking_seat; DROP FUNCTION cinema.test_expiry_during_write();"); }
    }
    [Fact]
    public async Task Editing_selection_and_reserving_concurrently_produces_one_valid_version_outcome()
    {
        var guest = await Bootstrap(); var selection = await Create(guest); var body = Reservation(selection);
        var responses = await Task.WhenAll(Reserve(guest, body), Send(guest, HttpMethod.Put,
            "api/selections/" + selection.GetProperty("selectionId").GetString(),
            new { version = selection.GetProperty("version").GetString(), quantities = new { adult = 1, discounted = 0 }, seatIds = new[] { standard.ToString() } }));
        try
        {
            if (responses[0].StatusCode == HttpStatusCode.Created)
            {
                Assert.Equal(HttpStatusCode.Conflict, responses[1].StatusCode); Assert.Equal(1, await fixture.CountAsync("booking"));
            }
            else
            {
                Assert.Equal(HttpStatusCode.Conflict, responses[0].StatusCode); Assert.Equal(HttpStatusCode.OK, responses[1].StatusCode);
                Assert.Equal("VERSION_CONFLICT", (await Json(responses[0])).GetProperty("error").GetProperty("code").GetString());
                Assert.Equal(0, await fixture.CountAsync("booking"));
            }
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }
    [Fact]
    public async Task Canonical_seat_order_and_trimmed_contact_replay_same_operation()
    {
        var guest = await Bootstrap(); var selection = await Create(guest, Payload([standard, vip], 2));
        var body = Reservation(selection); var key = Guid.NewGuid();
        using var first = await Reserve(guest, body, key); var saved = await Success(first);
        body["seatIds"] = new JsonArray(vip.ToString(), standard.ToString());
        body["contact"]!["fullName"] = "  Demo Guest  "; body["showtimeId"] = "00" + showtime.ToString();
        using var replay = await Reserve(guest, body, key); Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(saved.GetProperty("bookingId").GetString(), (await Success(replay)).GetProperty("bookingId").GetString());
        using var duplicate = await Reserve(guest, body); Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("STATE_CONFLICT", (await Json(duplicate)).GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(1, await fixture.CountAsync("booking"));
    }

    [Fact]
    public async Task Official_showtime_changes_require_reacceptance_before_holding_seats()
    {
        var guest = await Bootstrap(); var selection = await Create(guest);
        await Sql("UPDATE cinema.showtime SET starts_at=starts_at+interval '1 hour',ends_at=ends_at+interval '1 hour',room_available_at=room_available_at+interval '1 hour',version=version+1 WHERE id=@id", ("id", showtime));
        using var response = await Reserve(guest, Reservation(selection));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("PRICE_CHANGED", (await Json(response)).GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(0, await fixture.CountAsync("booking_seat"));
    }

}
