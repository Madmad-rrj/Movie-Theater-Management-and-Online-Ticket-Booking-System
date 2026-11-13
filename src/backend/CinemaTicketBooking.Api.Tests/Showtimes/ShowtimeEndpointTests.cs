using System.Net;
using System.Text.Json;
using CinemaTicketBooking.Api.Tests.Infrastructure;
using Npgsql;
using Xunit;

namespace CinemaTicketBooking.Api.Tests.Showtimes;

[Collection("Database")]
public sealed class ShowtimeEndpointTests
{
    private readonly string _connectionString;
    private readonly HttpClient _client;

    public ShowtimeEndpointTests(PostgresFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
        _client = fixture.Client;
    }

    [Fact]
    public async Task Browse_DateMissing_ReturnsRequired()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                var (movieId, _, _, _) = await SeedMovieCinemaRoomAsync(conn, cleaner, tag, "now_showing");
                var response = await _client.GetAsync($"/api/movies/{movieId}/showtimes");
                await AssertValidationErrorAsync(response, "date", "required");
            }
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Browse_DateEmptyOrWhitespace_ReturnsRequired(string date)
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using var conn = await cleaner.OpenConnectionAsync();
            var (movieId, _, _, _) = await SeedMovieCinemaRoomAsync(conn, cleaner, tag, "now_showing");
            var response = await _client.GetAsync($"/api/movies/{movieId}/showtimes?date={Uri.EscapeDataString(date)}");
            await AssertValidationErrorAsync(response, "date", "required");
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Theory]
    [InlineData("11/14/2026")]
    [InlineData("2026-1-4")]
    [InlineData("2026-13-40")]
    public async Task Browse_DateInvalidFormat_ReturnsInvalidFormat(string date)
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using var conn = await cleaner.OpenConnectionAsync();
            var (movieId, _, _, _) = await SeedMovieCinemaRoomAsync(conn, cleaner, tag, "now_showing");
            var response = await _client.GetAsync($"/api/movies/{movieId}/showtimes?date={Uri.EscapeDataString(date)}");
            await AssertValidationErrorAsync(response, "date", "invalid_format");
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task Browse_CinemaIdInvalidFormat_ReturnsValidationError(string cinemaId)
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using var conn = await cleaner.OpenConnectionAsync();
            var (movieId, _, _, _) = await SeedMovieCinemaRoomAsync(conn, cleaner, tag, "now_showing");
            var response = await _client.GetAsync($"/api/movies/{movieId}/showtimes?date=2026-11-14&cinemaId={Uri.EscapeDataString(cinemaId)}");
            await AssertValidationErrorAsync(response, "cinemaId", "invalid_format");
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task Browse_ReturnsOnlyScheduled_ExcludesOtherStatuses()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using var conn = await cleaner.OpenConnectionAsync();
            var (movieId, cinemaId, roomId, _) = await SeedMovieCinemaRoomAsync(conn, cleaner, tag, "now_showing");
            var baseStart = new DateTimeOffset(2026, 11, 14, 3, 0, 0, TimeSpan.Zero); // 10:00 HCM
            var scheduledId = await cleaner.InsertShowtimeAsync(conn, movieId, roomId, baseStart, status: "scheduled", seedKey: $"S_S_{tag}");
            await cleaner.InsertShowtimeAsync(conn, movieId, roomId, baseStart.AddHours(3), status: "draft", seedKey: $"S_D_{tag}");
            await cleaner.InsertShowtimeAsync(conn, movieId, roomId, baseStart.AddHours(6), status: "in_progress", seedKey: $"S_IP_{tag}");
            await cleaner.InsertShowtimeAsync(conn, movieId, roomId, baseStart.AddHours(9), status: "completed", seedKey: $"S_C_{tag}");
            await cleaner.InsertShowtimeAsync(conn, movieId, roomId, baseStart.AddHours(12), status: "cancelled", seedKey: $"S_X_{tag}",
                cancelledAt: baseStart, cancellationReason: "test");

            var response = await _client.GetAsync($"/api/movies/{movieId}/showtimes?date=2026-11-14&cinemaId={cinemaId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var data = doc.RootElement.GetProperty("data");
            Assert.Equal(1, data.GetArrayLength());
            Assert.Equal(scheduledId.ToString(), data[0].GetProperty("showtimeId").GetString());
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task Browse_InactiveCinema_NotReturned_And_FilterByCinemaId_Isolates()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using var conn = await cleaner.OpenConnectionAsync();
            var movieId = await cleaner.InsertMovieAsync(conn, $"M_{tag}", $"Movie {tag}", "now_showing");
            var activeCinema = await cleaner.InsertCinemaAsync(conn, $"CA_{tag}", $"CineA {tag}", "Addr A", "Asia/Ho_Chi_Minh", "active");
            var inactiveCinema = await cleaner.InsertCinemaAsync(conn, $"CI_{tag}", $"CineI {tag}", "Addr I", "Asia/Ho_Chi_Minh", "inactive");
            var otherCinema = await cleaner.InsertCinemaAsync(conn, $"CO_{tag}", $"CineO {tag}", "Addr O", "Asia/Ho_Chi_Minh", "active");
            var roomA = await cleaner.InsertRoomAsync(conn, activeCinema, $"RA_{tag}", "Room A");
            var roomI = await cleaner.InsertRoomAsync(conn, inactiveCinema, $"RI_{tag}", "Room I");
            var roomO = await cleaner.InsertRoomAsync(conn, otherCinema, $"RO_{tag}", "Room O");
            var start = new DateTimeOffset(2026, 11, 14, 3, 0, 0, TimeSpan.Zero);
            var idA = await cleaner.InsertShowtimeAsync(conn, movieId, roomA, start, status: "scheduled", seedKey: $"SA_{tag}");
            await cleaner.InsertShowtimeAsync(conn, movieId, roomI, start, status: "scheduled", seedKey: $"SI_{tag}");
            await cleaner.InsertShowtimeAsync(conn, movieId, roomO, start, status: "scheduled", seedKey: $"SO_{tag}");

            // inactive cinema excluded
            var r1 = await _client.GetAsync($"/api/movies/{movieId}/showtimes?date=2026-11-14&cinemaId={inactiveCinema}");
            using (var doc = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                Assert.Equal(0, doc.RootElement.GetProperty("data").GetArrayLength());
            }

            // filter by active cinema isolates
            var r2 = await _client.GetAsync($"/api/movies/{movieId}/showtimes?date=2026-11-14&cinemaId={activeCinema}");
            using (var doc = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
            {
                var data = doc.RootElement.GetProperty("data");
                Assert.Equal(1, data.GetArrayLength());
                Assert.Equal(idA.ToString(), data[0].GetProperty("showtimeId").GetString());
                Assert.Equal(activeCinema.ToString(), data[0].GetProperty("cinemaId").GetString());
            }
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task Browse_TimezoneBoundary_Hcm_And_UtcDifference()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using var conn = await cleaner.OpenConnectionAsync();
            var movieId = await cleaner.InsertMovieAsync(conn, $"M_{tag}", $"Movie {tag}", "now_showing");
            var hcmCinema = await cleaner.InsertCinemaAsync(conn, $"CH_{tag}", $"HCM {tag}", "Addr", "Asia/Ho_Chi_Minh", "active");
            var utcCinema = await cleaner.InsertCinemaAsync(conn, $"CU_{tag}", $"UTC {tag}", "Addr", "UTC", "active");
            var roomH = await cleaner.InsertRoomAsync(conn, hcmCinema, $"RH_{tag}", "Room H");
            var roomU = await cleaner.InsertRoomAsync(conn, utcCinema, $"RU_{tag}", "Room U");

            // 2026-11-14T16:59:59Z = 2026-11-14 23:59:59+07 (HCM day 14)
            var hcmDay14 = await cleaner.InsertShowtimeAsync(conn, movieId, roomH, new DateTimeOffset(2026, 11, 14, 16, 59, 59, TimeSpan.Zero), status: "scheduled", seedKey: $"SH14_{tag}");
            // 2026-11-14T17:00:00Z = 2026-11-15 00:00:00+07 (HCM day 15), placed in another room to avoid overlap
            var roomH2 = await cleaner.InsertRoomAsync(conn, hcmCinema, $"RH2_{tag}", "Room H2");
            var hcmDay15 = await cleaner.InsertShowtimeAsync(conn, movieId, roomH2, new DateTimeOffset(2026, 11, 14, 17, 0, 0, TimeSpan.Zero), status: "scheduled", seedKey: $"SH15_{tag}");

            // Both instants are on 2026-11-14 in UTC; placed in separate rooms to avoid overlap
            var roomU2 = await cleaner.InsertRoomAsync(conn, utcCinema, $"RU2_{tag}", "Room U2");
            var utcDay14a = await cleaner.InsertShowtimeAsync(conn, movieId, roomU, new DateTimeOffset(2026, 11, 14, 16, 59, 59, TimeSpan.Zero), status: "scheduled", seedKey: $"SUa_{tag}");
            var utcDay14b = await cleaner.InsertShowtimeAsync(conn, movieId, roomU2, new DateTimeOffset(2026, 11, 14, 17, 0, 0, TimeSpan.Zero), status: "scheduled", seedKey: $"SUb_{tag}");

            // HCM cinema: date 14 -> only 16:59:59Z; date 15 -> only 17:00:00Z
            var rH14 = await GetDataAsync($"/api/movies/{movieId}/showtimes?date=2026-11-14&cinemaId={hcmCinema}");
            Assert.Equal(new[] { hcmDay14.ToString() }, rH14.Select(x => x.GetProperty("showtimeId").GetString()).ToArray());
            var rH15 = await GetDataAsync($"/api/movies/{movieId}/showtimes?date=2026-11-15&cinemaId={hcmCinema}");
            Assert.Equal(new[] { hcmDay15.ToString() }, rH15.Select(x => x.GetProperty("showtimeId").GetString()).ToArray());

            // UTC cinema: date 14 -> both on 2026-11-14 UTC; date 15 -> empty
            var rU14 = await GetDataAsync($"/api/movies/{movieId}/showtimes?date=2026-11-14&cinemaId={utcCinema}");
            var uIds = rU14.Select(x => x.GetProperty("showtimeId").GetString()).OrderBy(x => x).ToArray();
            Assert.Equal(new[] { utcDay14a.ToString(), utcDay14b.ToString() }.OrderBy(x => x).ToArray(), uIds);
            var rU15 = await GetDataAsync($"/api/movies/{movieId}/showtimes?date=2026-11-15&cinemaId={utcCinema}");
            Assert.Empty(rU15);

            // No cinemaId: date interpreted per each cinema's timezone
            var rAll14 = await GetDataAsync($"/api/movies/{movieId}/showtimes?date=2026-11-14");
            var all14Ids = rAll14.Select(x => x.GetProperty("showtimeId").GetString()!).ToHashSet();
            Assert.Contains(hcmDay14.ToString(), all14Ids);
            Assert.Contains(utcDay14a.ToString(), all14Ids);
            Assert.Contains(utcDay14b.ToString(), all14Ids);
            Assert.DoesNotContain(hcmDay15.ToString(), all14Ids);
            var rAll15 = await GetDataAsync($"/api/movies/{movieId}/showtimes?date=2026-11-15");
            var all15Ids = rAll15.Select(x => x.GetProperty("showtimeId").GetString()!).ToHashSet();
            Assert.Contains(hcmDay15.ToString(), all15Ids);
            Assert.DoesNotContain(utcDay14a.ToString(), all15Ids);
            Assert.DoesNotContain(utcDay14b.ToString(), all15Ids);
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task Browse_MovieWithNoShowtimes_ReturnsEmptyWithPagination()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using var conn = await cleaner.OpenConnectionAsync();
            var (movieId, _, _, _) = await SeedMovieCinemaRoomAsync(conn, cleaner, tag, "now_showing");
            var response = await _client.GetAsync($"/api/movies/{movieId}/showtimes?date=2026-11-14");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Empty(doc.RootElement.GetProperty("data").EnumerateArray());
            var p = doc.RootElement.GetProperty("pagination");
            Assert.Equal(1, p.GetProperty("page").GetInt32());
            Assert.Equal(20, p.GetProperty("pageSize").GetInt32());
            Assert.Equal(0, p.GetProperty("totalItems").GetInt32());
            Assert.Equal(0, p.GetProperty("totalPages").GetInt32());
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task Browse_MovieNotFoundOrArchived_ReturnsNotFound()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            long archivedId;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                archivedId = await cleaner.InsertMovieAsync(conn, $"MA_{tag}", $"Arc {tag}", "archived");
            }
            await AssertErrorAsync(await _client.GetAsync($"/api/movies/999999999/showtimes?date=2026-11-14"), HttpStatusCode.NotFound, "NOT_FOUND");
            await AssertErrorAsync(await _client.GetAsync($"/api/movies/{archivedId}/showtimes?date=2026-11-14"), HttpStatusCode.NotFound, "NOT_FOUND");
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task GetById_Scheduled_ReturnsOk_WithCorrectIdsAndRelations()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using var conn = await cleaner.OpenConnectionAsync();
            var movieId = await cleaner.InsertMovieAsync(conn, $"M_{tag}", $"Movie {tag}", "now_showing");
            var cinemaId = await cleaner.InsertCinemaAsync(conn, $"C_{tag}", $"Cinema {tag}", "Addr", "Asia/Ho_Chi_Minh", "active");
            var roomId = await cleaner.InsertRoomAsync(conn, cinemaId, $"R_{tag}", $"Room {tag}");
            var stId = await cleaner.InsertShowtimeAsync(conn, movieId, roomId, new DateTimeOffset(2026, 11, 14, 3, 0, 0, TimeSpan.Zero), status: "scheduled", seedKey: $"S_{tag}");

            var response = await _client.GetAsync($"/api/showtimes/{stId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("data", out var data));
            Assert.Equal(stId.ToString(), data.GetProperty("showtimeId").GetString());
            Assert.Equal(JsonValueKind.String, data.GetProperty("showtimeId").ValueKind);
            Assert.Equal(movieId.ToString(), data.GetProperty("movieId").GetString());
            Assert.Equal(JsonValueKind.String, data.GetProperty("movieId").ValueKind);
            Assert.Equal(cinemaId.ToString(), data.GetProperty("cinemaId").GetString());
            Assert.Equal(JsonValueKind.String, data.GetProperty("cinemaId").ValueKind);
            Assert.Equal(roomId.ToString(), data.GetProperty("roomId").GetString());
            Assert.Equal(JsonValueKind.String, data.GetProperty("roomId").ValueKind);
            Assert.Equal(JsonValueKind.String, data.GetProperty("version").ValueKind);
            Assert.Equal($"Cinema {tag}", data.GetProperty("cinemaName").GetString());
            Assert.Equal($"Room {tag}", data.GetProperty("roomName").GetString());
            Assert.Equal($"Movie {tag}", data.GetProperty("movieTitle").GetString());
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("in_progress")]
    [InlineData("completed")]
    [InlineData("cancelled")]
    public async Task GetById_StatusOutsidePublicSet_ReturnsNotFound(string status)
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using var conn = await cleaner.OpenConnectionAsync();
            var (movieId, _, roomId, _) = await SeedMovieCinemaRoomAsync(conn, cleaner, tag, "now_showing");
            var start = new DateTimeOffset(2026, 11, 14, 3, 0, 0, TimeSpan.Zero);
            long stId = status == "cancelled"
                ? await cleaner.InsertShowtimeAsync(conn, movieId, roomId, start, status: status, seedKey: $"S_{status[..1]}_{tag}", cancelledAt: start, cancellationReason: "x")
                : await cleaner.InsertShowtimeAsync(conn, movieId, roomId, start, status: status, seedKey: $"S_{status[..1]}_{tag}");
            await AssertErrorAsync(await _client.GetAsync($"/api/showtimes/{stId}"), HttpStatusCode.NotFound, "NOT_FOUND");
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task Browse_Pagination_Stable_NoDupNoMiss()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using var conn = await cleaner.OpenConnectionAsync();
            var (movieId, cinemaId, roomId, _) = await SeedMovieCinemaRoomAsync(conn, cleaner, tag, "now_showing");
            var baseStart = new DateTimeOffset(2026, 11, 14, 1, 0, 0, TimeSpan.Zero);
            var created = new List<long>();
            for (int i = 0; i < 5; i++)
            {
                created.Add(await cleaner.InsertShowtimeAsync(conn, movieId, roomId, baseStart.AddHours(i * 3), status: "scheduled", seedKey: $"SP_{i}_{tag}"));
            }

            var allIds = new List<string>();
            int? totalItems = null, totalPages = null;
            for (int page = 1; page <= 3; page++)
            {
                var resp = await _client.GetAsync($"/api/movies/{movieId}/showtimes?date=2026-11-14&cinemaId={cinemaId}&page={page}&pageSize=2");
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                var p = doc.RootElement.GetProperty("pagination");
                totalItems ??= p.GetProperty("totalItems").GetInt32();
                totalPages ??= p.GetProperty("totalPages").GetInt32();
                foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
                    allIds.Add(item.GetProperty("showtimeId").GetString()!);
            }

            Assert.Equal(5, totalItems);
            Assert.Equal(3, totalPages);
            Assert.Equal(5, allIds.Count);
            Assert.Equal(5, allIds.Distinct().Count());
            Assert.True(created.All(id => allIds.Contains(id.ToString())));
            // order by starts_at ascending matches creation order
            Assert.Equal(created.Select(id => id.ToString()).ToArray(), allIds.ToArray());

            // stability: call again and compare first page order
            var firstPage1 = await GetDataAsync($"/api/movies/{movieId}/showtimes?date=2026-11-14&cinemaId={cinemaId}&page=1&pageSize=2");
            var firstPage2 = await GetDataAsync($"/api/movies/{movieId}/showtimes?date=2026-11-14&cinemaId={cinemaId}&page=1&pageSize=2");
            Assert.Equal(
                firstPage1.Select(x => x.GetProperty("showtimeId").GetString()).ToArray(),
                firstPage2.Select(x => x.GetProperty("showtimeId").GetString()).ToArray());
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task CoreTables_HaveNoLeftoverRows_AfterCleanups()
    {
        var (movies, genres, links, cinemas, rooms, showtimes) = await DbCleaner.CountCoreTablesAsync(_connectionString);
        Assert.Equal(0, movies);
        Assert.Equal(0, genres);
        Assert.Equal(0, links);
        Assert.Equal(0, cinemas);
        Assert.Equal(0, rooms);
        Assert.Equal(0, showtimes);
    }

    private async Task<List<JsonElement>> GetDataAsync(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").EnumerateArray().Select(e => e.Clone()).ToList();
    }

    private static async Task<(long movieId, long cinemaId, long roomId, string tag)> SeedMovieCinemaRoomAsync(
        NpgsqlConnection conn, DbCleaner cleaner, string tag, string movieStatus, string cinemaStatus = "active", string timezone = "Asia/Ho_Chi_Minh")
    {
        var movieId = await cleaner.InsertMovieAsync(conn, $"M_{tag}", $"Movie {tag}", movieStatus);
        var cinemaId = await cleaner.InsertCinemaAsync(conn, $"C_{tag}", $"Cinema {tag}", "Addr", timezone, cinemaStatus);
        var roomId = await cleaner.InsertRoomAsync(conn, cinemaId, $"R_{tag}", $"Room {tag}");
        return (movieId, cinemaId, roomId, tag);
    }

    private static string NewTag() => Guid.NewGuid().ToString("N")[..10];

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var error = doc.RootElement.GetProperty("error");
        Assert.Equal(expectedCode, error.GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.String, error.GetProperty("message").ValueKind);
        Assert.NotNull(error.GetProperty("message").GetString());
        Assert.Equal(JsonValueKind.Object, error.GetProperty("details").ValueKind);
        Assert.Equal(JsonValueKind.String, error.GetProperty("traceId").ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("traceId").GetString()));
    }

    private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string field, string reason)
    {
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var violations = doc.RootElement.GetProperty("error").GetProperty("details").GetProperty("violations");
        Assert.Equal(JsonValueKind.Array, violations.ValueKind);
        var v = Assert.Single(violations.EnumerateArray());
        Assert.Equal(field, v.GetProperty("field").GetString());
        Assert.Equal(reason, v.GetProperty("reason").GetString());
        Assert.Equal(JsonValueKind.String, v.GetProperty("message").ValueKind);
    }
}
