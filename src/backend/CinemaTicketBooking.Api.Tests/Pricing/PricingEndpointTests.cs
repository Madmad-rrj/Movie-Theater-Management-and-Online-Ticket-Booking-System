using System.Net;
using System.Text.Json;
using CinemaTicketBooking.Api.Tests.Infrastructure;
using Xunit;

namespace CinemaTicketBooking.Api.Tests.Pricing;

[Collection("Database")]
public sealed class PricingEndpointTests
{
    private readonly string _connectionString;
    private readonly HttpClient _client;

    public PricingEndpointTests(PostgresFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
        _client = fixture.Client;
    }

    [Fact]
    public async Task GetCinemaPricing_FullPrices_ReturnsOk_WithCorrectShape()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                var cinemaId = await cleaner.InsertCinemaAsync(conn, $"C_{tag}", $"Cinema {tag}", "Addr");
                await cleaner.InsertCinemaTicketPriceAsync(conn, cinemaId, "Adult", 100000);
                await cleaner.InsertCinemaTicketPriceAsync(conn, cinemaId, "Discounted", 70000);
            }
            long cinemaIdVal;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                // fetch id by querying? easier to track - we have from insert but scope ended; re-query by code
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id FROM cinema.cinema WHERE code=@c";
                cmd.Parameters.AddWithValue("c", $"C_{tag}");
                cinemaIdVal = (long)(await cmd.ExecuteScalarAsync())!;
            }

            var response = await _client.GetAsync($"/api/cinemas/{cinemaIdVal}/ticket-pricing");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var data = doc.RootElement.GetProperty("data");
            Assert.Equal(cinemaIdVal.ToString(), data.GetProperty("cinemaId").GetString());
            Assert.Equal(JsonValueKind.String, data.GetProperty("cinemaId").ValueKind);

            var adult = data.GetProperty("adultBasePrice");
            Assert.Equal(100000, adult.GetProperty("amount").GetInt64());
            Assert.Equal("VND", adult.GetProperty("currency").GetString());
            var disc = data.GetProperty("discountedBasePrice");
            Assert.Equal(70000, disc.GetProperty("amount").GetInt64());
            Assert.Equal("VND", disc.GetProperty("currency").GetString());
            Assert.NotEqual(adult.GetProperty("amount").GetInt64(), disc.GetProperty("amount").GetInt64());
            // ensure not swapped: adult should be 100000, discounted 70000
            Assert.Equal(100000, adult.GetProperty("amount").GetInt64());
            Assert.Equal(70000, disc.GetProperty("amount").GetInt64());
            Assert.Equal(JsonValueKind.String, data.GetProperty("version").ValueKind);
            Assert.True(data.TryGetProperty("updatedAt", out _));
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Theory]
    [InlineData(0, 50000)]
    [InlineData(50000, 0)]
    [InlineData(0, 0)]
    public async Task GetCinemaPricing_ZeroAmounts_ReturnsOk(long adultAmount, long discountedAmount)
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            long cinemaId;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                cinemaId = await cleaner.InsertCinemaAsync(conn, $"CZ_{tag}", $"CinemaZ {tag}", "Addr");
                await cleaner.InsertCinemaTicketPriceAsync(conn, cinemaId, "Adult", adultAmount);
                await cleaner.InsertCinemaTicketPriceAsync(conn, cinemaId, "Discounted", discountedAmount);
            }

            var response = await _client.GetAsync($"/api/cinemas/{cinemaId}/ticket-pricing");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var data = doc.RootElement.GetProperty("data");
            Assert.Equal(adultAmount, data.GetProperty("adultBasePrice").GetProperty("amount").GetInt64());
            Assert.Equal(discountedAmount, data.GetProperty("discountedBasePrice").GetProperty("amount").GetInt64());
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task GetCinemaPricing_MissingAdult_ReturnsConflict()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            long cinemaId;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                cinemaId = await cleaner.InsertCinemaAsync(conn, $"CM_{tag}", $"CinemaM {tag}", "Addr");
                await cleaner.InsertCinemaTicketPriceAsync(conn, cinemaId, "Discounted", 70000);
            }
            await AssertErrorAsync(await _client.GetAsync($"/api/cinemas/{cinemaId}/ticket-pricing"), HttpStatusCode.Conflict, "STATE_CONFLICT");
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task GetCinemaPricing_MissingDiscounted_ReturnsConflict()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            long cinemaId;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                cinemaId = await cleaner.InsertCinemaAsync(conn, $"CM_{tag}", $"CinemaM {tag}", "Addr");
                await cleaner.InsertCinemaTicketPriceAsync(conn, cinemaId, "Adult", 100000);
            }
            await AssertErrorAsync(await _client.GetAsync($"/api/cinemas/{cinemaId}/ticket-pricing"), HttpStatusCode.Conflict, "STATE_CONFLICT");
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task GetCinemaPricing_MissingBoth_ReturnsConflict()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            long cinemaId;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                cinemaId = await cleaner.InsertCinemaAsync(conn, $"CM_{tag}", $"CinemaM {tag}", "Addr");
            }
            await AssertErrorAsync(await _client.GetAsync($"/api/cinemas/{cinemaId}/ticket-pricing"), HttpStatusCode.Conflict, "STATE_CONFLICT");
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task GetCinemaPricing_NotFoundOrInactive_ReturnsNotFound()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            long inactiveId;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                inactiveId = await cleaner.InsertCinemaAsync(conn, $"CI_{tag}", $"CinemaI {tag}", "Addr", status: "inactive");
            }
            await AssertErrorAsync(await _client.GetAsync("/api/cinemas/999999999/ticket-pricing"), HttpStatusCode.NotFound, "NOT_FOUND");
            await AssertErrorAsync(await _client.GetAsync($"/api/cinemas/{inactiveId}/ticket-pricing"), HttpStatusCode.NotFound, "NOT_FOUND");
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("9223372036854775808")]
    public async Task GetCinemaPricing_InvalidCinemaId_ReturnsValidationError(string cinemaId)
    {
        var response = await _client.GetAsync($"/api/cinemas/{Uri.EscapeDataString(cinemaId)}/ticket-pricing");
        await AssertValidationErrorAsync(response, "cinemaId", "invalid_format");
    }

    [Fact]
    public async Task GetCinemaPricing_CinemaIsolation_NoLeakBetweenCinemas()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = NewTag();
            long cinemaA, cinemaB;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                cinemaA = await cleaner.InsertCinemaAsync(conn, $"CA_{tag}", $"CinemaA {tag}", "Addr");
                cinemaB = await cleaner.InsertCinemaAsync(conn, $"CB_{tag}", $"CinemaB {tag}", "Addr");
                await cleaner.InsertCinemaTicketPriceAsync(conn, cinemaA, "Adult", 111111);
                await cleaner.InsertCinemaTicketPriceAsync(conn, cinemaA, "Discounted", 55555);
                await cleaner.InsertCinemaTicketPriceAsync(conn, cinemaB, "Adult", 222222);
                await cleaner.InsertCinemaTicketPriceAsync(conn, cinemaB, "Discounted", 66666);
            }

            using var docA = JsonDocument.Parse(await (await _client.GetAsync($"/api/cinemas/{cinemaA}/ticket-pricing")).Content.ReadAsStringAsync());
            using var docB = JsonDocument.Parse(await (await _client.GetAsync($"/api/cinemas/{cinemaB}/ticket-pricing")).Content.ReadAsStringAsync());
            var a = docA.RootElement.GetProperty("data");
            var b = docB.RootElement.GetProperty("data");
            Assert.Equal(111111, a.GetProperty("adultBasePrice").GetProperty("amount").GetInt64());
            Assert.Equal(55555, a.GetProperty("discountedBasePrice").GetProperty("amount").GetInt64());
            Assert.Equal(222222, b.GetProperty("adultBasePrice").GetProperty("amount").GetInt64());
            Assert.Equal(66666, b.GetProperty("discountedBasePrice").GetProperty("amount").GetInt64());
        }
        finally { await cleaner.CleanupAsync(); }
    }

    [Fact]
    public async Task GetTicketCategories_ReturnsTwo_FromDb_AndDoesNotWrite()
    {
        var dbCategories = await DbCleaner.GetTicketCategoriesAsync(_connectionString);
        var before = await DbCleaner.CountTableAsync(_connectionString, "cinema.cinema_ticket_price");

        var response = await _client.GetAsync("/api/ticket-categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(2, data.GetArrayLength());

        var items = data.EnumerateArray().Select(e => (
            Code: e.GetProperty("code").GetString()!,
            Name: e.GetProperty("name").GetString()!,
            Description: e.GetProperty("description").GetString(),
            Version: e.GetProperty("version").GetString()!
        )).OrderBy(x => x.Code).ToList();
        var expected = dbCategories.OrderBy(x => x.Code).ToList();
        Assert.Equal(expected.Select(x => x.Code), items.Select(x => x.Code));
        Assert.Equal(expected.Select(x => x.Name), items.Select(x => x.Name));
        Assert.Equal(expected.Select(x => x.Description), items.Select(x => x.Description));
        Assert.Equal(expected.Select(x => x.Version.ToString()), items.Select(x => x.Version));

        var after = await DbCleaner.CountTableAsync(_connectionString, "cinema.cinema_ticket_price");
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task PricingAndTicketCategories_HaveNoLeftoverAndIntact()
    {
        var prices = await DbCleaner.CountTableAsync(_connectionString, "cinema.cinema_ticket_price");
        var cinemas = await DbCleaner.CountTableAsync(_connectionString, "cinema.cinema");
        var rooms = await DbCleaner.CountTableAsync(_connectionString, "cinema.cinema_room");
        Assert.Equal(0, prices);
        Assert.Equal(0, cinemas);
        Assert.Equal(0, rooms);

        var cats = await DbCleaner.GetTicketCategoriesAsync(_connectionString);
        Assert.Equal(2, cats.Count);
        Assert.Contains(cats, c => c.Code == "Adult" && c.Name == "Adult" && c.Description == "Standard category");
        Assert.Contains(cats, c => c.Code == "Discounted" && c.Name == "Discounted" && c.Description == "Students, teachers, and seniors aged 60 or above; eligibility checked at admission");
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
