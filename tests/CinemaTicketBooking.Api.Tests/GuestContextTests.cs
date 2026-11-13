using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Npgsql;
using Xunit;

namespace CinemaTicketBooking.Api.Tests;

[Collection("Guest PostgreSQL")]
public sealed class GuestContextTests(GuestApiFixture fixture)
{
    private const string LocalOrigin = "http://127.0.0.1:5173";
    private const string OnlineOrigin = "https://cinema.example.test";

    [Fact]
    public async Task Creates_guest_with_24_hour_deadline_hash_only_and_no_booking()
    {
        using var client = fixture.App.CreateBrowser();
        var before = await fixture.CountAsync("checkout_identity");
        using var response = await BootstrapAsync(client);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = await SessionAsync(response);
        var cookieHeader = response.Headers.GetValues("Set-Cookie").Single();
        Assert.Contains("httponly", cookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api", cookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(43, session.Cookie.Length);
        Assert.Equal(43, session.Csrf.Length);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Single(json.RootElement.EnumerateObject());
        Assert.Equal(2, json.RootElement.GetProperty("data").EnumerateObject().Count());
        Assert.True(!body.Contains(session.Cookie, StringComparison.Ordinal), "Guest JSON must not disclose the cookie.");

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT guest_credential_hash, created_at, guest_expires_at, user_id FROM cinema.checkout_identity WHERE id=@id", connection);
        command.Parameters.AddWithValue("id", session.Id);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var expectedHash = Convert.ToHexString(SHA256.HashData(WebEncoders.Base64UrlDecode(session.Cookie)));
        Assert.Equal(expectedHash, reader.GetString(0));
        Assert.Equal(TimeSpan.FromHours(24), reader.GetDateTime(2) - reader.GetDateTime(1));
        Assert.Equal(session.ExpiresAt, reader.GetDateTime(2));
        Assert.True(reader.IsDBNull(3));
        Assert.Equal(before + 1, await fixture.CountAsync("checkout_identity"));
        foreach (var table in new[] { "selection_session", "booking", "booking_seat", "payment", "ticket" })
            Assert.Equal(0, await fixture.CountAsync(table));
        Assert.True(fixture.App.Logs.Messages.All(message =>
            !message.Contains(session.Cookie, StringComparison.Ordinal) && !message.Contains(session.Csrf, StringComparison.Ordinal)),
            "Application logs must not contain Guest credentials or CSRF tokens.");
    }

    [Fact]
    public async Task Reuse_and_idempotent_replay_preserve_identity_cookie_and_deadline()
    {
        using var client = fixture.App.CreateBrowser();
        var key = Guid.NewGuid();
        using var created = await BootstrapAsync(client, key);
        var original = await SessionAsync(created);
        using var replay = await BootstrapAsync(client, key, original.Cookie);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal("true", replay.Headers.GetValues("Idempotency-Replayed").Single());
        var repeated = await SessionAsync(replay);
        Assert.Equal(original.Id, repeated.Id);
        Assert.Equal(original.ExpiresAt, repeated.ExpiresAt);
        Assert.True(original.Cookie == repeated.Cookie);
        using var reuse = await BootstrapAsync(client, cookie: original.Cookie);
        Assert.Equal(HttpStatusCode.OK, reuse.StatusCode);
        Assert.False(reuse.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal(original.Id, (await SessionAsync(reuse)).Id);
        Assert.Equal(original.ExpiresAt, (await SessionAsync(reuse)).ExpiresAt);
    }

    [Fact]
    public async Task Subsequent_request_resolves_guest_and_missing_credential_is_unauthenticated()
    {
        using var client = fixture.App.CreateBrowser();
        using var created = await BootstrapAsync(client);
        var session = await SessionAsync(created);
        using var read = await SendAsync(client, HttpMethod.Get, "api/__tests/guest", session.Cookie);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        using var body = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
        Assert.Equal(session.Id, body.RootElement.GetProperty("data").GetProperty("contextId").GetGuid());
        using var missing = await SendAsync(client, HttpMethod.Get, "api/__tests/guest");
        await ErrorAsync(missing, HttpStatusCode.Unauthorized, "UNAUTHENTICATED");
    }

    [Theory]
    [InlineData("not-a-credential")]
    [InlineData("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public async Task Invalid_cookie_is_rejected_without_creating_replacement(string cookie)
    {
        using var client = fixture.App.CreateBrowser();
        var before = await fixture.CountAsync("checkout_identity");
        using var response = await BootstrapAsync(client, cookie: cookie);
        await ErrorAsync(response, HttpStatusCode.Unauthorized, "UNAUTHENTICATED");
        Assert.StartsWith("cinema_guest=;", response.Headers.GetValues("Set-Cookie").Single());
        Assert.Equal(before, await fixture.CountAsync("checkout_identity"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Expired_or_revoked_guest_is_rejected_on_bootstrap_read_and_replay(bool revoke)
    {
        using var client = fixture.App.CreateBrowser();
        var key = Guid.NewGuid();
        using var created = await BootstrapAsync(client, key);
        var session = await SessionAsync(created);
        var sql = revoke
            ? "UPDATE cinema.checkout_identity SET revoked_at=clock_timestamp(), version=version+1 WHERE id=@id"
            : "UPDATE cinema.checkout_identity SET created_at=clock_timestamp()-interval '2 hours', guest_expires_at=clock_timestamp()-interval '1 hour', version=version+1 WHERE id=@id";
        await fixture.ExecuteForGuestAsync(sql, session.Id);
        using var reused = await BootstrapAsync(client, cookie: session.Cookie);
        await ErrorAsync(reused, HttpStatusCode.Unauthorized, "UNAUTHENTICATED");
        using var replay = await BootstrapAsync(client, key, session.Cookie);
        await ErrorAsync(replay, HttpStatusCode.Unauthorized, "UNAUTHENTICATED");
        using var read = await SendAsync(client, HttpMethod.Get, "api/__tests/guest", session.Cookie);
        await ErrorAsync(read, HttpStatusCode.Unauthorized, "UNAUTHENTICATED");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("null")]
    [InlineData("https://untrusted.example")]
    [InlineData("http://127.0.0.1:5174")]
    public async Task Bootstrap_requires_exact_trusted_origin(string? origin)
    {
        using var client = fixture.App.CreateBrowser();
        var before = await fixture.CountAsync("checkout_identity");
        using var response = await BootstrapAsync(client, origin: origin);
        await ErrorAsync(response, HttpStatusCode.Forbidden, "CSRF_REJECTED");
        Assert.Equal(before, await fixture.CountAsync("checkout_identity"));
    }

    [Fact]
    public async Task Bootstrap_rejects_missing_malformed_duplicate_keys_and_any_body()
    {
        using var client = fixture.App.CreateBrowser();
        foreach (var key in new string?[] { null, "invalid", Guid.Empty.ToString() })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/guest-context");
            request.Headers.Add("Origin", LocalOrigin);
            if (key is not null) request.Headers.Add("Idempotency-Key", key);
            using var response = await client.SendAsync(request);
            await ErrorAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        }
        using var duplicate = new HttpRequestMessage(HttpMethod.Post, "api/guest-context");
        duplicate.Headers.Add("Origin", LocalOrigin);
        duplicate.Headers.Add("Idempotency-Key", new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() });
        using var duplicateResponse = await client.SendAsync(duplicate);
        await ErrorAsync(duplicateResponse, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        using var withBody = await BootstrapAsync(client, body: "{\"userId\":\"forged\"}");
        await ErrorAsync(withBody, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
    }

    [Fact]
    public async Task Invalid_bearer_never_falls_back_to_valid_guest_cookie()
    {
        using var client = fixture.App.CreateBrowser();
        using var created = await BootstrapAsync(client);
        var session = await SessionAsync(created);
        using var bootstrap = await BootstrapAsync(client, cookie: session.Cookie, bearer: true);
        await ErrorAsync(bootstrap, HttpStatusCode.Unauthorized, "UNAUTHENTICATED");
        Assert.False(bootstrap.Headers.Contains("Set-Cookie"));
        using var read = await SendAsync(client, HttpMethod.Get, "api/__tests/guest", session.Cookie, bearer: true);
        await ErrorAsync(read, HttpStatusCode.Unauthorized, "UNAUTHENTICATED");
    }

    [Fact]
    public async Task Mutation_requires_csrf_bound_to_cookie_context_and_origin()
    {
        using var client = fixture.App.CreateBrowser();
        using var firstResponse = await BootstrapAsync(client);
        using var secondResponse = await BootstrapAsync(client);
        var first = await SessionAsync(firstResponse);
        var second = await SessionAsync(secondResponse);
        foreach (var token in new string?[] { null, "invalid", second.Csrf })
        {
            using var denied = await SendAsync(client, HttpMethod.Post, "api/__tests/guest", first.Cookie, token, LocalOrigin);
            await ErrorAsync(denied, HttpStatusCode.Forbidden, "CSRF_REJECTED");
        }
        using var missingOrigin = await SendAsync(client, HttpMethod.Post, "api/__tests/guest", first.Cookie, first.Csrf);
        await ErrorAsync(missingOrigin, HttpStatusCode.Forbidden, "CSRF_REJECTED");
        using var valid = await SendAsync(client, HttpMethod.Post, "api/__tests/guest", first.Cookie, first.Csrf, LocalOrigin);
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        using var body = JsonDocument.Parse(await valid.Content.ReadAsStringAsync());
        Assert.Equal(first.Id, body.RootElement.GetProperty("data").GetProperty("contextId").GetGuid());
    }

    [Fact]
    public async Task Bootstrap_key_is_not_a_recovery_credential_and_cannot_switch_another_guest()
    {
        using var client = fixture.App.CreateBrowser();
        var key = Guid.NewGuid();
        using var firstResponse = await BootstrapAsync(client, key);
        using var secondResponse = await BootstrapAsync(client);
        var second = await SessionAsync(secondResponse);
        using var missingCookie = await BootstrapAsync(client, key);
        await ErrorAsync(missingCookie, HttpStatusCode.Unauthorized, "UNAUTHENTICATED");
        using var conflicting = await BootstrapAsync(client, key, second.Cookie);
        await ErrorAsync(conflicting, HttpStatusCode.Conflict, "IDEMPOTENCY_KEY_CONFLICT");
        Assert.False(conflicting.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Concurrent_same_key_creates_one_guest_and_one_receipt()
    {
        using var client = fixture.App.CreateBrowser();
        var beforeGuests = await fixture.CountAsync("checkout_identity");
        var beforeReceipts = await fixture.CountAsync("operation_receipt");
        var key = Guid.NewGuid();
        var responses = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => BootstrapAsync(client, key)));
        try
        {
            var winner = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            foreach (var loser in responses.Where(response => response != winner))
            {
                Assert.True(loser.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Unauthorized);
                Assert.False(loser.Headers.Contains("Set-Cookie"));
                if (loser.StatusCode == HttpStatusCode.Conflict)
                {
                    await ErrorAsync(loser, HttpStatusCode.Conflict, "OPERATION_IN_PROGRESS");
                    Assert.NotNull(loser.Headers.RetryAfter);
                }
            }
            Assert.Equal(beforeGuests + 1, await fixture.CountAsync("checkout_identity"));
            Assert.Equal(beforeReceipts + 1, await fixture.CountAsync("operation_receipt"));
            var session = await SessionAsync(winner);
            using var replay = await BootstrapAsync(client, key, session.Cookie);
            Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
            Assert.Equal(session.Id, (await SessionAsync(replay)).Id);
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [Fact]
    public async Task Concurrent_rebootstrap_reuses_existing_identity_without_extending_expiry()
    {
        using var client = fixture.App.CreateBrowser();
        using var created = await BootstrapAsync(client);
        var original = await SessionAsync(created);
        var before = await fixture.CountAsync("checkout_identity");
        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => BootstrapAsync(client, cookie: original.Cookie)));
        try
        {
            foreach (var response in responses)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var current = await SessionAsync(response);
                Assert.Equal(original.Id, current.Id);
                Assert.Equal(original.ExpiresAt, current.ExpiresAt);
            }
            Assert.Equal(before, await fixture.CountAsync("checkout_identity"));
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [Fact]
    public async Task Local_http_loopback_works_but_nonlocal_http_is_rejected()
    {
        using var client = fixture.App.CreateBrowser(https: false);
        using var created = await BootstrapAsync(client);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var header = created.Headers.GetValues("Set-Cookie").Single();
        Assert.DoesNotContain("; secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        var original = await SessionAsync(created);
        using var reused = await BootstrapAsync(client, cookie: original.Cookie);
        Assert.Equal(original.Id, (await SessionAsync(reused)).Id);
        using var remoteApp = new GuestApiFactory(fixture.ConnectionString, loopbackClient: false);
        using var remoteClient = remoteApp.CreateBrowser(https: false);
        using var denied = await BootstrapAsync(remoteClient);
        await ErrorAsync(denied, HttpStatusCode.Forbidden, "FORBIDDEN");
    }

    [Fact]
    public async Task Online_cookie_is_secure_and_cross_site_cors_is_exact()
    {
        using var app = new GuestApiFactory(fixture.ConnectionString, "Production", new()
        {
            ["GuestCheckout:CrossSiteCookies"] = "true"
        });
        using var client = app.CreateBrowser();
        using var created = await BootstrapAsync(client, origin: OnlineOrigin);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var header = created.Headers.GetValues("Set-Cookie").Single();
        Assert.Contains("; secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=none", header, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(OnlineOrigin, created.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", created.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        Assert.Contains("X-CSRF-Token", created.Headers.GetValues("Access-Control-Expose-Headers").Single());
        using var insecureClient = app.CreateBrowser(https: false);
        using var denied = await BootstrapAsync(insecureClient, origin: OnlineOrigin);
        await ErrorAsync(denied, HttpStatusCode.Forbidden, "FORBIDDEN");
        using var untrusted = await BootstrapAsync(client, origin: "https://untrusted.example");
        await ErrorAsync(untrusted, HttpStatusCode.Forbidden, "CSRF_REJECTED");
        Assert.False(untrusted.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("GuestCheckout:AllowInsecureLoopbackCookies", "true")]
    [InlineData("GuestCheckout:AllowedOrigins:0", "http://127.0.0.1:5173")]
    [InlineData("GuestCheckout:AllowedOrigins:0", "https://cinema.example.test/path")]
    [InlineData("GuestCheckout:LifetimeHours", "0")]
    public void Unsafe_online_configuration_fails_startup(string key, string value)
    {
        using var app = new GuestApiFactory(fixture.ConnectionString, "Production", new() { [key] = value });
        Assert.Throws<Microsoft.Extensions.Options.OptionsValidationException>(() => app.CreateBrowser());
    }

    [Fact]
    public async Task Bootstrap_rate_limit_returns_contract_error_and_retry_after()
    {
        using var app = new GuestApiFactory(fixture.ConnectionString, overrides: new()
        {
            ["GuestCheckout:BootstrapPermitLimit"] = "2"
        });
        using var client = app.CreateBrowser();
        using var first = await BootstrapAsync(client);
        using var second = await BootstrapAsync(client);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var before = await fixture.CountAsync("checkout_identity");
        using var third = await BootstrapAsync(client);
        await ErrorAsync(third, HttpStatusCode.TooManyRequests, "RATE_LIMITED");
        Assert.NotNull(third.Headers.RetryAfter);
        Assert.Equal(before, await fixture.CountAsync("checkout_identity"));
    }

    [Fact]
    public async Task Existing_movie_listing_still_reads_seeded_postgresql_data()
    {
        using var client = fixture.App.CreateBrowser();
        using var response = await client.GetAsync("api/movies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(6, body.RootElement.GetProperty("data").GetArrayLength());
        foreach (var movie in body.RootElement.GetProperty("data").EnumerateArray())
            Assert.True(long.TryParse(movie.GetProperty("movieId").GetString(), out var id) && id > 0);
    }

    private static Task<HttpResponseMessage> BootstrapAsync(
        HttpClient client, Guid? key = null, string? cookie = null, string? origin = LocalOrigin,
        string? body = null, bool bearer = false) => SendAsync(client, HttpMethod.Post, "api/guest-context",
            cookie, origin: origin, key: key ?? Guid.NewGuid(), body: body, bearer: bearer);

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string path, string? cookie = null, string? csrf = null,
        string? origin = null, Guid? key = null, string? body = null, bool bearer = false)
    {
        using var request = new HttpRequestMessage(method, path);
        if (cookie is not null) request.Headers.Add("Cookie", "cinema_guest=" + cookie);
        if (csrf is not null) request.Headers.Add("X-CSRF-Token", csrf);
        if (origin is not null) request.Headers.Add("Origin", origin);
        if (key is not null) request.Headers.Add("Idempotency-Key", key.Value.ToString());
        if (bearer) request.Headers.Add("Authorization", "Bearer invalid");
        if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return await client.SendAsync(request);
    }

    private static async Task<GuestSession> SessionAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = body.RootElement.GetProperty("data");
        var cookie = response.Headers.GetValues("Set-Cookie").Single()
            .Split(';')[0]["cinema_guest=".Length..];
        return new GuestSession(data.GetProperty("contextId").GetGuid(), data.GetProperty("expiresAt").GetDateTime(),
            cookie, response.Headers.GetValues("X-CSRF-Token").Single());
    }

    private static async Task ErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = body.RootElement.GetProperty("error");
        Assert.Equal(code, error.GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Object, error.GetProperty("details").ValueKind);
        if (code == "VALIDATION_ERROR")
        {
            Assert.True(error.GetProperty("details").GetProperty("violations").GetArrayLength() > 0);
        }
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("traceId").GetString()));
    }

    private sealed class GuestSession(Guid id, DateTime expiresAt, string cookie, string csrf)
    {
        public Guid Id { get; } = id;
        public DateTime ExpiresAt { get; } = expiresAt;
        public string Cookie { get; } = cookie;
        public string Csrf { get; } = csrf;
        public override string ToString() => $"Guest session {Id:D}";
    }
}
