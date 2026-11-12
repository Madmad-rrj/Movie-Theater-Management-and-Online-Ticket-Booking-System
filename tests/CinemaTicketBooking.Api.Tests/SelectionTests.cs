using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using Xunit;

namespace CinemaTicketBooking.Api.Tests;

[Collection("Guest PostgreSQL")]
public sealed class SelectionTests(GuestApiFixture fixture) : IAsyncLifetime
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
    [Fact]
    public async Task Server_quote_deadline_and_no_holds()
    {
        var guest = await Bootstrap(); var data = await Create(guest, Payload([standard, vip], 1, 1));
        Assert.Equal(185000, data.GetProperty("quote").GetProperty("total").GetProperty("amount").GetDecimal());
        var duration = data.GetProperty("selectionDeadline").GetDateTime() - data.GetProperty("serverTime").GetDateTime();
        Assert.InRange(duration.TotalSeconds, 290, 300);
        using var read = await Send(guest, HttpMethod.Get, "api/selections/" + data.GetProperty("selectionId").GetString());
        var again = (await Json(read)).GetProperty("data");
        Assert.Equal(data.GetProperty("selectionDeadline").GetString(), again.GetProperty("selectionDeadline").GetString());
        Assert.Equal(data.GetProperty("quote").GetProperty("version").GetString(), again.GetProperty("quote").GetProperty("version").GetString());
        Assert.Equal(0, await fixture.CountAsync("booking_seat"));
    }
    [Fact]
    public async Task Guests_can_select_same_seat_but_cannot_access_each_other()
    {
        var a = await Bootstrap(); var b = await Bootstrap(); var selection = await Create(a); await Create(b);
        using var read = await Send(b, HttpMethod.Get, "api/selections/" + selection.GetProperty("selectionId").GetString());
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        using var put = await Send(b, HttpMethod.Put, "api/selections/" + selection.GetProperty("selectionId").GetString(), new { version = "1", quantities = new { adult = 1, discounted = 0 }, seatIds = new[] { standard.ToString() } });
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode); Assert.Equal(0, await fixture.CountAsync("booking_seat"));
    }
    [Fact]
    public async Task Editing_incomplete_selection_and_stale_version()
    {
        var guest = await Bootstrap(); var selection = await Create(guest, Payload([standard], 2)); var path = "api/selections/" + selection.GetProperty("selectionId").GetString();
        var update = new { version = selection.GetProperty("version").GetString(), quantities = new { adult = 2, discounted = 0 }, seatIds = Array.Empty<string>() };
        using var changed = await Send(guest, HttpMethod.Put, path, update); Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var current = (await Json(changed)).GetProperty("data"); Assert.Empty(current.GetProperty("seatIds").EnumerateArray());
        Assert.Equal(selection.GetProperty("selectionDeadline").GetString(), current.GetProperty("selectionDeadline").GetString());
        using var stale = await Send(guest, HttpMethod.Put, path, update); Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("VERSION_CONFLICT", (await Json(stale)).GetProperty("error").GetProperty("code").GetString());
    }
    [Fact]
    public async Task Price_change_refreshes_quote_and_selection_version()
    {
        var guest = await Bootstrap(); var selection = await Create(guest);
        await Sql("UPDATE cinema.cinema_ticket_price SET amount=amount+1000,version=version+1 WHERE cinema_id=@id AND category_code='Adult'", ("id", cinema));
        using var response = await Send(guest, HttpMethod.Get, "api/selections/" + selection.GetProperty("selectionId").GetString());
        var data = (await Json(response)).GetProperty("data"); Assert.Equal(adultPrice + 1000, data.GetProperty("quote").GetProperty("total").GetProperty("amount").GetDecimal());
        Assert.NotEqual(selection.GetProperty("version").GetString(), data.GetProperty("version").GetString());
    }
    [Fact]
    public async Task Create_replay_canonicalizes_seats_and_conflicting_payload_is_rejected()
    {
        var guest = await Bootstrap(); var key = Guid.NewGuid();
        using var first = await Send(guest, HttpMethod.Post, "api/selections", Payload([standard, vip], 2), key);
        using var replay = await Send(guest, HttpMethod.Post, "api/selections", Payload([vip, standard], 2), key);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode); Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal("true", replay.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.Equal((await Json(first)).GetProperty("data").GetProperty("selectionId").GetGuid(), (await Json(replay)).GetProperty("data").GetProperty("selectionId").GetGuid());
        using var conflict = await Send(guest, HttpMethod.Post, "api/selections", Payload(), key); Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }
    [Fact]
    public async Task Expiry_is_persisted_and_new_create_does_not_hit_unique_index()
    {
        var guest = await Bootstrap(); var selection = await Create(guest); var id = selection.GetProperty("selectionId").GetGuid();
        await Sql("UPDATE cinema.selection_session SET started_at=date_trunc('second',clock_timestamp())-interval '10 minutes',expires_at=date_trunc('second',clock_timestamp())-interval '5 minutes' WHERE id=@id", ("id", id));
        using var expired = await Send(guest, HttpMethod.Put, "api/selections/" + id, new { version = "1", quantities = new { adult = 1, discounted = 0 }, seatIds = new[] { standard.ToString() } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, expired.StatusCode);
        using var read = await Send(guest, HttpMethod.Get, "api/selections/" + id); Assert.Equal("expired", (await Json(read)).GetProperty("data").GetProperty("state").GetString());
        var next = await Create(guest); Assert.NotEqual(id, next.GetProperty("selectionId").GetGuid());
    }
    [Fact]
    public async Task Wrong_room_duplicates_unknown_fields_and_csrf_are_rejected()
    {
        var guest = await Bootstrap();
        foreach (var body in new[] { Payload([otherRoom]), Payload([standard, standard], 2), Payload([], 1), Payload([standard], 9) })
        { using var response = await Send(guest, HttpMethod.Post, "api/selections", body, Guid.NewGuid()); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); }
        using var csrf = await Send(guest, HttpMethod.Post, "api/selections", Payload(), Guid.NewGuid(), false); Assert.Equal(HttpStatusCode.Forbidden, csrf.StatusCode);
        using var unknown = await Send(guest, HttpMethod.Post, "api/selections", new { showtimeId = showtime.ToString(), quantities = new { adult = 1, discounted = 0 }, seatIds = new[] { standard.ToString() }, total = 1 }, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }
    [Fact]
    public async Task Concurrent_creates_with_different_keys_reuse_one_active_selection()
    {
        var guest = await Bootstrap(); var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Send(guest, HttpMethod.Post, "api/selections", Payload(), Guid.NewGuid())));
        try
        {
            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
            var ids = new List<Guid>(); foreach (var r in responses) { Assert.True(r.IsSuccessStatusCode); ids.Add((await Json(r)).GetProperty("data").GetProperty("selectionId").GetGuid()); }
            Assert.Single(ids.Distinct());
        }
        finally { foreach (var r in responses) r.Dispose(); }
    }
    [Fact]
    public async Task Expired_active_is_closed_on_create_and_old_key_never_reactivates_it()
    {
        var guest = await Bootstrap(); var key = Guid.NewGuid();
        using var first = await Send(guest, HttpMethod.Post, "api/selections", Payload(), key);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var id = (await Json(first)).GetProperty("data").GetProperty("selectionId").GetGuid();
        await Sql("UPDATE cinema.selection_session SET started_at=date_trunc('second',clock_timestamp())-interval '10 minutes',expires_at=date_trunc('second',clock_timestamp())-interval '5 minutes' WHERE id=@id", ("id", id));
        var next = await Create(guest); Assert.NotEqual(id, next.GetProperty("selectionId").GetGuid());
        using var replay = await Send(guest, HttpMethod.Post, "api/selections", Payload(), key);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var old = (await Json(replay)).GetProperty("data"); Assert.Equal("expired", old.GetProperty("state").GetString());
        Assert.Equal(id, old.GetProperty("selectionId").GetGuid());
    }
    [Fact]
    public async Task Consumed_selection_cannot_be_edited_even_after_its_deadline()
    {
        var guest = await Bootstrap(); var selection = await Create(guest); var id = selection.GetProperty("selectionId").GetGuid();
        await Sql("UPDATE cinema.selection_session SET state='consumed',started_at=date_trunc('second',clock_timestamp())-interval '10 minutes',expires_at=date_trunc('second',clock_timestamp())-interval '5 minutes' WHERE id=@id", ("id", id));
        using var response = await Send(guest, HttpMethod.Put, "api/selections/" + id, new { version = "1", quantities = new { adult = 1, discounted = 0 }, seatIds = new[] { standard.ToString() } });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("STATE_CONFLICT", (await Json(response)).GetProperty("error").GetProperty("code").GetString());
    }
    [Fact]
    public async Task Out_of_service_seat_and_missing_price_are_not_silently_accepted()
    {
        var guest = await Bootstrap();
        await Sql("UPDATE cinema.seat SET operational_state='out_of_service' WHERE id=@id", ("id", standard));
        try
        {
            using var response = await Send(guest, HttpMethod.Post, "api/selections", Payload(), Guid.NewGuid());
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally { await Sql("UPDATE cinema.seat SET operational_state='active' WHERE id=@id", ("id", standard)); }
        await Sql("DELETE FROM cinema.cinema_ticket_price WHERE cinema_id=@id AND category_code='Adult'", ("id", cinema));
        try
        {
            using var response = await Send(guest, HttpMethod.Post, "api/selections", Payload(), Guid.NewGuid());
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal("BUSINESS_RULE_VIOLATION", (await Json(response)).GetProperty("error").GetProperty("code").GetString());
        }
        finally { await Sql("INSERT INTO cinema.cinema_ticket_price(cinema_id,category_code,amount,version) VALUES(@id,'Adult',@amount,@version)", ("id", cinema), ("amount", adultPrice), ("version", priceVersion)); }
    }

}
