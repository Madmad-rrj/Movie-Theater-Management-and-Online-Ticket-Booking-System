using System.Net;
using System.Text.Json;
using CinemaTicketBooking.Api.Tests.Infrastructure;
using Xunit;

namespace CinemaTicketBooking.Api.Tests.Cinemas;

[Collection("Database")]
public sealed class CinemaEndpointTests
{
    private readonly string _connectionString;
    private readonly HttpClient _client;

    public CinemaEndpointTests(PostgresFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
        _client = fixture.Client;
    }

    [Fact]
    public async Task ListCinemas_ActiveReturned_InactiveExcluded_WithCorrectFields()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            long activeId, inactiveId;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                activeId = await cleaner.InsertCinemaAsync(conn, $"CA_{tag}", $"Active {tag}", $"Addr A {tag}", "Asia/Ho_Chi_Minh", "active", 20);
                inactiveId = await cleaner.InsertCinemaAsync(conn, $"CI_{tag}", $"Inactive {tag}", $"Addr I {tag}", "UTC", "inactive", 10);
            }

            var response = await _client.GetAsync("/api/cinemas");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("data", out var data));
            Assert.Equal(JsonValueKind.Array, data.ValueKind);

            var items = data.EnumerateArray().ToList();
            Assert.Contains(items, e => e.GetProperty("cinemaId").GetString() == activeId.ToString());
            Assert.DoesNotContain(items, e => e.GetProperty("cinemaId").GetString() == inactiveId.ToString());

            var active = items.First(e => e.GetProperty("cinemaId").GetString() == activeId.ToString());
            Assert.Equal(JsonValueKind.String, active.GetProperty("cinemaId").ValueKind);
            Assert.Equal(JsonValueKind.String, active.GetProperty("version").ValueKind);
            Assert.Equal($"Active {tag}", active.GetProperty("name").GetString());
            Assert.Equal($"Addr A {tag}", active.GetProperty("address").GetString());
            Assert.Equal("Asia/Ho_Chi_Minh", active.GetProperty("timezone").GetString());
            Assert.True(active.GetProperty("isActive").GetBoolean());
            Assert.Equal(20, active.GetProperty("minimumTurnaroundMinutes").GetInt32());
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task ListCinemas_NoTestCinemas_ReturnsEmptyEnvelope_NotError()
    {
        var response = await _client.GetAsync("/api/cinemas");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("data", out var data));
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
        Assert.Empty(data.EnumerateArray());
        // envelope shape: no error object
        Assert.False(root.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task CinemaAndRoomTables_HaveNoLeftoverRows_AfterCleanups()
    {
        var cinemas = await DbCleaner.CountTableAsync(_connectionString, "cinema.cinema");
        var rooms = await DbCleaner.CountTableAsync(_connectionString, "cinema.cinema_room");
        Assert.Equal(0, cinemas);
        Assert.Equal(0, rooms);
    }

    private static string NewTag() => Guid.NewGuid().ToString("N")[..10];
}
