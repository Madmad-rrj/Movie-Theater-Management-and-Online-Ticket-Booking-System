using System.Net;
using System.Text.Json;
using CinemaTicketBooking.Api.Tests.Infrastructure;
using Npgsql;
using Xunit;

namespace CinemaTicketBooking.Api.Tests.Seats;

[Collection("Database")]
public sealed class SeatMapEndpointTests
{
    private readonly string _connectionString;
    private readonly HttpClient _client;

    public SeatMapEndpointTests(PostgresFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
        _client = fixture.Client;
    }

    [Fact]
    public async Task GetSeatMap_Returns50Seats_WithCorrectFields_AndNoSensitiveData()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            long showtimeId, roomId, seatTypeId;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                var movieId = await cleaner.InsertMovieAsync(conn, $"M_{tag}", $"Movie {tag}", "now_showing");
                var cinemaId = await cleaner.InsertCinemaAsync(conn, $"C_{tag}", $"Cinema {tag}", "Addr");
                roomId = await cleaner.InsertRoomAsync(conn, cinemaId, $"R_{tag}", $"Room {tag}");
                seatTypeId = await cleaner.InsertSeatTypeAsync(conn, $"ST_{tag}", $"Standard {tag}", 15000, true);
                await Seed50SeatsAsync(conn, cleaner, roomId, seatTypeId);
                showtimeId = await cleaner.InsertShowtimeAsync(conn, movieId, roomId, new DateTimeOffset(2026, 11, 14, 3, 0, 0, TimeSpan.Zero), status: "scheduled", seedKey: $"S_{tag}");
            }

            var response = await _client.GetAsync($"/api/showtimes/{showtimeId}/seats");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("bookingId", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"booking\"", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("customer", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("email", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("phone", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("payment", body, StringComparison.OrdinalIgnoreCase);

            using var doc = JsonDocument.Parse(body);
            var data = doc.RootElement.GetProperty("data");
            Assert.Equal(showtimeId.ToString(), data.GetProperty("showtimeId").GetString());
            Assert.Equal(JsonValueKind.String, data.GetProperty("showtimeId").ValueKind);
            Assert.Equal(roomId.ToString(), data.GetProperty("roomId").GetString());
            Assert.Equal(JsonValueKind.String, data.GetProperty("roomId").ValueKind);
            var seats = data.GetProperty("seats");
            Assert.Equal(50, seats.GetArrayLength());

            // verify a specific seat (A1)
            var a1 = seats.EnumerateArray().First(e => e.GetProperty("seat").GetProperty("label").GetString() == "A1");
            var seat = a1.GetProperty("seat");
            Assert.Equal("A", seat.GetProperty("rowLabel").GetString());
            Assert.Equal(1, seat.GetProperty("seatNumber").GetInt32());
            Assert.Equal("A1", seat.GetProperty("label").GetString());
            Assert.Equal(0, seat.GetProperty("x").GetInt32());
            Assert.Equal(0, seat.GetProperty("y").GetInt32());
            Assert.Equal(JsonValueKind.String, seat.GetProperty("seatId").ValueKind);
            Assert.Equal(JsonValueKind.String, seat.GetProperty("version").ValueKind);
            Assert.Equal("active", seat.GetProperty("operationalState").GetString());
            var st = seat.GetProperty("seatType");
            Assert.Equal(seatTypeId.ToString(), st.GetProperty("seatTypeId").GetString());
            Assert.Equal(JsonValueKind.String, st.GetProperty("seatTypeId").ValueKind);
            Assert.Equal($"ST_{tag}"[..Math.Min(30, $"ST_{tag}".Length)], st.GetProperty("code").GetString());
            Assert.Equal(15000, st.GetProperty("surcharge").GetProperty("amount").GetInt64());
            Assert.Equal("VND", st.GetProperty("surcharge").GetProperty("currency").GetString());
            Assert.Equal(JsonValueKind.String, st.GetProperty("version").ValueKind);
            Assert.Equal("available", a1.GetProperty("availability").GetString());
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task GetSeatMap_AvailabilityStatuses_AndSpecialCases()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using var conn = await cleaner.OpenConnectionAsync();
            var movieId = await cleaner.InsertMovieAsync(conn, $"M_{tag}", $"Movie {tag}", "now_showing");
            var cinemaId = await cleaner.InsertCinemaAsync(conn, $"C_{tag}", $"Cinema {tag}", "Addr");
            var roomId = await cleaner.InsertRoomAsync(conn, cinemaId, $"R_{tag}", $"Room {tag}");
            var stype = await cleaner.InsertSeatTypeAsync(conn, $"ST_{tag}", $"Std {tag}", 0, true);
            var showtimeId = await cleaner.InsertShowtimeAsync(conn, movieId, roomId, new DateTimeOffset(2026, 11, 14, 3, 0, 0, TimeSpan.Zero), status: "scheduled", seedKey: $"S_{tag}");

            var seatAvailable = await cleaner.InsertSeatAsync(conn, roomId, stype, "A", 1, 0, 0);
            var seatHeld = await cleaner.InsertSeatAsync(conn, roomId, stype, "A", 2, 1, 0);
            var seatSold = await cleaner.InsertSeatAsync(conn, roomId, stype, "A", 3, 2, 0);
            var seatReleased = await cleaner.InsertSeatAsync(conn, roomId, stype, "A", 4, 3, 0);
            var seatReleasedHeld = await cleaner.InsertSeatAsync(conn, roomId, stype, "A", 5, 4, 0);
            var seatRetired = await cleaner.InsertSeatAsync(conn, roomId, stype, "A", 6, 5, 0, "active", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
            var seatOut = await cleaner.InsertSeatAsync(conn, roomId, stype, "A", 7, 6, 0, "out_of_service", null);
            var seatExpiredBookingHeld = await cleaner.InsertSeatAsync(conn, roomId, stype, "A", 8, 7, 0);

            var now = DateTimeOffset.UtcNow;
            async Task<(long bookingId, Guid sel, Guid chk)> MakeBooking(DateTimeOffset created, DateTimeOffset expires, string status = "pending")
            {
                var chk = await cleaner.InsertCheckoutIdentityAsync(conn, created, created.AddDays(1));
                var sel = await cleaner.InsertSelectionSessionAsync(conn, chk, showtimeId, roomId, created);
                var bid = await cleaner.InsertBookingAsync(conn, chk, sel, showtimeId, roomId, created, expires, status);
                return (bid, sel, chk);
            }

            var bHeld = await MakeBooking(now.AddMinutes(-5), now.AddMinutes(5));
            await cleaner.InsertBookingSeatAsync(conn, bHeld.bookingId, showtimeId, roomId, seatHeld, "A2", "Std", 0, "held");

            var bSold = await MakeBooking(now.AddMinutes(-30), now.AddMinutes(-1), "confirmed");
            await cleaner.InsertBookingSeatAsync(conn, bSold.bookingId, showtimeId, roomId, seatSold, "A3", "Std", 0, "sold");

            var bReleased = await MakeBooking(now.AddMinutes(-30), now.AddMinutes(-1), "expired");
            await cleaner.InsertBookingSeatAsync(conn, bReleased.bookingId, showtimeId, roomId, seatReleased, "A4", "Std", 0, "released");

            // released + held different rows same seat -> held
            var bRel2 = await MakeBooking(now.AddMinutes(-30), now.AddMinutes(-1), "expired");
            await cleaner.InsertBookingSeatAsync(conn, bRel2.bookingId, showtimeId, roomId, seatReleasedHeld, "A5", "Std", 0, "released");
            var bHeld2 = await MakeBooking(now.AddMinutes(-5), now.AddMinutes(5));
            await cleaner.InsertBookingSeatAsync(conn, bHeld2.bookingId, showtimeId, roomId, seatReleasedHeld, "A5", "Std", 0, "held");

            // booking expired but allocation still held -> still held
            var bExpiredHeld = await MakeBooking(now.AddHours(-2), now.AddHours(-1), "expired");
            await cleaner.InsertBookingSeatAsync(conn, bExpiredHeld.bookingId, showtimeId, roomId, seatExpiredBookingHeld, "A8", "Std", 0, "held");

            var map = await GetSeatMapAsync(showtimeId);
            string Av(long seatId) => map.First(x => x.SeatId == seatId.ToString()).Availability;
            Assert.Equal("available", Av(seatAvailable));
            Assert.Equal("held", Av(seatHeld));
            Assert.Equal("sold", Av(seatSold));
            Assert.Equal("available", Av(seatReleased));
            Assert.Equal("held", Av(seatReleasedHeld));
            Assert.Equal("unavailable", Av(seatRetired));
            Assert.Equal("unavailable", Av(seatOut));
            Assert.Equal("held", Av(seatExpiredBookingHeld));
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task GetSeatMap_RoomIsolation_AndOtherShowtimeAllocations()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using var conn = await cleaner.OpenConnectionAsync();
            var movieId = await cleaner.InsertMovieAsync(conn, $"M_{tag}", $"Movie {tag}", "now_showing");
            var cinemaId = await cleaner.InsertCinemaAsync(conn, $"C_{tag}", $"Cinema {tag}", "Addr");
            var roomA = await cleaner.InsertRoomAsync(conn, cinemaId, $"RA_{tag}", "Room A");
            var roomB = await cleaner.InsertRoomAsync(conn, cinemaId, $"RB_{tag}", "Room B");
            var stype = await cleaner.InsertSeatTypeAsync(conn, $"ST_{tag}", $"Std {tag}", 0, true);
            var stA1 = await cleaner.InsertShowtimeAsync(conn, movieId, roomA, new DateTimeOffset(2026, 11, 14, 3, 0, 0, TimeSpan.Zero), status: "scheduled", seedKey: $"SA1_{tag}");
            var stA2 = await cleaner.InsertShowtimeAsync(conn, movieId, roomA, new DateTimeOffset(2026, 11, 14, 6, 0, 0, TimeSpan.Zero), status: "scheduled", seedKey: $"SA2_{tag}");
            var seatA = await cleaner.InsertSeatAsync(conn, roomA, stype, "A", 1, 0, 0);
            var seatB = await cleaner.InsertSeatAsync(conn, roomB, stype, "A", 1, 0, 0);

            // allocation of different showtime in same room must not affect stA1
            var now = DateTimeOffset.UtcNow;
            var chk = await cleaner.InsertCheckoutIdentityAsync(conn, now, now.AddDays(1));
            var sel = await cleaner.InsertSelectionSessionAsync(conn, chk, stA2, roomA, now);
            var bid = await cleaner.InsertBookingAsync(conn, chk, sel, stA2, roomA, now, now.AddMinutes(10));
            await cleaner.InsertBookingSeatAsync(conn, bid, stA2, roomA, seatA, "A1", "Std", 0, "held");

            var mapA1 = await GetSeatMapAsync(stA1);
            Assert.Single(mapA1); // only seatA, no seatB from other room
            Assert.Equal(seatA.ToString(), mapA1[0].SeatId);
            Assert.Equal("available", mapA1[0].Availability); // not affected by stA2 allocation
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task GetSeatMap_DoesNotWriteDb()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            long showtimeId;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                var movieId = await cleaner.InsertMovieAsync(conn, $"M_{tag}", $"Movie {tag}", "now_showing");
                var cinemaId = await cleaner.InsertCinemaAsync(conn, $"C_{tag}", $"Cinema {tag}", "Addr");
                var roomId = await cleaner.InsertRoomAsync(conn, cinemaId, $"R_{tag}", $"Room {tag}");
                var stype = await cleaner.InsertSeatTypeAsync(conn, $"ST_{tag}", $"Std {tag}", 0, true);
                await cleaner.InsertSeatAsync(conn, roomId, stype, "A", 1, 0, 0);
                showtimeId = await cleaner.InsertShowtimeAsync(conn, movieId, roomId, new DateTimeOffset(2026, 11, 14, 3, 0, 0, TimeSpan.Zero), status: "scheduled", seedKey: $"S_{tag}");
            }
            var beforeBs = await DbCleaner.CountTableAsync(_connectionString, "cinema.booking_seat");
            var beforeB = await DbCleaner.CountTableAsync(_connectionString, "cinema.booking");
            var resp = await _client.GetAsync($"/api/showtimes/{showtimeId}/seats");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var afterBs = await DbCleaner.CountTableAsync(_connectionString, "cinema.booking_seat");
            var afterB = await DbCleaner.CountTableAsync(_connectionString, "cinema.booking");
            Assert.Equal(beforeBs, afterBs);
            Assert.Equal(beforeB, afterB);
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("in_progress")]
    [InlineData("completed")]
    [InlineData("cancelled")]
    public async Task GetSeatMap_ShowtimeStatusOutsideSet_ReturnsNotFound(string status)
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using var conn = await cleaner.OpenConnectionAsync();
            var movieId = await cleaner.InsertMovieAsync(conn, $"M_{tag}", $"Movie {tag}", "now_showing");
            var cinemaId = await cleaner.InsertCinemaAsync(conn, $"C_{tag}", $"Cinema {tag}", "Addr");
            var roomId = await cleaner.InsertRoomAsync(conn, cinemaId, $"R_{tag}", $"Room {tag}");
            var start = new DateTimeOffset(2026, 11, 14, 3, 0, 0, TimeSpan.Zero);
            var stId = status == "cancelled"
                ? await cleaner.InsertShowtimeAsync(conn, movieId, roomId, start, status: status, seedKey: $"S_{tag}", cancelledAt: start, cancellationReason: "x")
                : await cleaner.InsertShowtimeAsync(conn, movieId, roomId, start, status: status, seedKey: $"S_{tag}");
            await AssertErrorAsync(await _client.GetAsync($"/api/showtimes/{stId}/seats"), HttpStatusCode.NotFound, "NOT_FOUND");
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task GetSeatMap_InactiveCinema_NotPublicMovie_NotFound_ReturnsNotFound()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            long stInactiveCinema, stArchivedMovie;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                var movieOk = await cleaner.InsertMovieAsync(conn, $"M_{tag}", $"Movie {tag}", "now_showing");
                var movieArchived = await cleaner.InsertMovieAsync(conn, $"MA_{tag}", $"Arc {tag}", "archived");
                var cinemaInactive = await cleaner.InsertCinemaAsync(conn, $"CI_{tag}", $"CinI {tag}", "Addr", status: "inactive");
                var cinemaOk = await cleaner.InsertCinemaAsync(conn, $"CO_{tag}", $"CinO {tag}", "Addr", status: "active");
                var roomI = await cleaner.InsertRoomAsync(conn, cinemaInactive, $"RI_{tag}", "RI");
                var roomO = await cleaner.InsertRoomAsync(conn, cinemaOk, $"RO_{tag}", "RO");
                var start = new DateTimeOffset(2026, 11, 14, 3, 0, 0, TimeSpan.Zero);
                stInactiveCinema = await cleaner.InsertShowtimeAsync(conn, movieOk, roomI, start, status: "scheduled", seedKey: $"SI_{tag}");
                stArchivedMovie = await cleaner.InsertShowtimeAsync(conn, movieArchived, roomO, start, status: "scheduled", seedKey: $"SA_{tag}");
            }
            await AssertErrorAsync(await _client.GetAsync($"/api/showtimes/{stInactiveCinema}/seats"), HttpStatusCode.NotFound, "NOT_FOUND");
            await AssertErrorAsync(await _client.GetAsync($"/api/showtimes/{stArchivedMovie}/seats"), HttpStatusCode.NotFound, "NOT_FOUND");
            await AssertErrorAsync(await _client.GetAsync("/api/showtimes/999999999/seats"), HttpStatusCode.NotFound, "NOT_FOUND");
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("9223372036854775808")]
    public async Task GetSeatMap_InvalidShowtimeId_ReturnsValidationError(string showtimeId)
    {
        var response = await _client.GetAsync($"/api/showtimes/{Uri.EscapeDataString(showtimeId)}/seats");
        await AssertValidationErrorAsync(response, "showtimeId", "invalid_format");
    }

    [Fact]
    public async Task SeatRelatedTables_HaveNoLeftoverRows_AfterCleanups()
    {
        var (seats, seatTypes, bookingSeats, bookings, showtimes, rooms, cinemas, movies) = await DbCleaner.CountSeatRelatedTablesAsync(_connectionString);
        Assert.Equal(0, seats);
        Assert.Equal(0, seatTypes);
        Assert.Equal(0, bookingSeats);
        Assert.Equal(0, bookings);
        Assert.Equal(0, showtimes);
        Assert.Equal(0, rooms);
        Assert.Equal(0, cinemas);
        Assert.Equal(0, movies);
    }

    // NOTE (case omitted by schema constraint): "sold + held cùng ghế" is not inserted because
    // ux_active_seat (partial unique index on (showtime_id,seat_id) WHERE allocation_status IN ('held','sold'))
    // prevents two active allocations for the same seat/showtime. Documented as omitted/skipped, not testable via insert.

    private async Task<List<(string SeatId, string Availability)>> GetSeatMapAsync(long showtimeId)
    {
        var response = await _client.GetAsync($"/api/showtimes/{showtimeId}/seats");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetProperty("seats").EnumerateArray()
            .Select(e => (SeatId: e.GetProperty("seat").GetProperty("seatId").GetString()!, Availability: e.GetProperty("availability").GetString()!))
            .ToList();
    }

    private static async Task Seed50SeatsAsync(NpgsqlConnection conn, DbCleaner cleaner, long roomId, long seatTypeId)
    {
        for (int r = 0; r < 5; r++)
        {
            var rowLabel = ((char)('A' + r)).ToString();
            for (int n = 1; n <= 10; n++)
            {
                await cleaner.InsertSeatAsync(conn, roomId, seatTypeId, rowLabel, (short)n, n - 1, r);
            }
        }
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
        var v = Assert.Single(violations.EnumerateArray());
        Assert.Equal(field, v.GetProperty("field").GetString());
        Assert.Equal(reason, v.GetProperty("reason").GetString());
    }
}
