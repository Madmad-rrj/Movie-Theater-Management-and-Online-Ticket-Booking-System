using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CinemaTicketBooking.Api.Tests.Infrastructure;
using Npgsql;
using Xunit;

namespace CinemaTicketBooking.Api.Tests.Movies;

[Collection("Database")]
public sealed class MovieEndpointTests
{
    private readonly string _connectionString;
    private readonly HttpClient _client;

    public MovieEndpointTests(PostgresFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
        _client = fixture.Client;
    }

    [Fact]
    public async Task BrowseMovies_ReturnsOk_WithData()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = Guid.NewGuid().ToString("N")[..12];
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                await cleaner.InsertMovieAsync(conn, $"MB_{tag}", $"Browse {tag}", "now_showing");
            }

            var response = await _client.GetAsync("/api/movies");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("data", out var data));
            Assert.Equal(JsonValueKind.Array, data.ValueKind);
            Assert.NotEmpty(data.EnumerateArray());
            Assert.True(root.TryGetProperty("pagination", out var pagination));
            Assert.Equal(JsonValueKind.Object, pagination.ValueKind);
        }
        finally
        {
            await cleaner.CleanupAsync();
        }
    }

    [Theory]
    [InlineData("ended")]
    [InlineData("coming_soon")]
    [InlineData("now_showing")]
    public async Task GetMovieDetail_PublicVisibleStatuses_ReturnsOk(string status)
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = Guid.NewGuid().ToString("N")[..10];
            var sfx = status[..2].ToUpperInvariant();
            long movieId;
            long genreId;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                movieId = await cleaner.InsertMovieAsync(conn, $"M_{sfx}_{tag}", $"Title {tag}", status,
                    description: status == "ended" ? null : "desc",
                    durationMinutes: 123,
                    releaseDate: status == "ended" ? null : new DateOnly(2026, 11, 14),
                    posterUrl: null, trailerUrl: null, language: status == "ended" ? null : "vi", ageRating: null);
                genreId = await cleaner.InsertGenreAsync(conn, $"G_{sfx}_{tag}", $"Genre {sfx} {tag}", description: null, isActive: true);
                await cleaner.LinkMovieGenreAsync(conn, movieId, genreId);
            }

            var response = await _client.GetAsync($"/api/movies/{movieId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("data", out var data));
            Assert.Equal(JsonValueKind.Object, data.ValueKind);

            Assert.Equal(JsonValueKind.String, data.GetProperty("movieId").ValueKind);
            Assert.Equal(movieId.ToString(), data.GetProperty("movieId").GetString());
            Assert.Equal(JsonValueKind.String, data.GetProperty("version").ValueKind);
            Assert.Equal(status, data.GetProperty("status").GetString());

            var genres = data.GetProperty("genres");
            Assert.Equal(JsonValueKind.Array, genres.ValueKind);
            var g = Assert.Single(genres.EnumerateArray());
            Assert.Equal(JsonValueKind.String, g.GetProperty("genreId").ValueKind);
            Assert.Equal(genreId.ToString(), g.GetProperty("genreId").GetString());
            Assert.Equal(JsonValueKind.String, g.GetProperty("version").ValueKind);

            if (status == "ended")
            {
                Assert.Equal(JsonValueKind.Null, data.GetProperty("description").ValueKind);
                Assert.Equal(JsonValueKind.Null, data.GetProperty("releaseDate").ValueKind);
                Assert.Equal(JsonValueKind.Null, data.GetProperty("posterUrl").ValueKind);
                Assert.Equal(JsonValueKind.Null, data.GetProperty("trailerUrl").ValueKind);
                Assert.Equal(JsonValueKind.Null, data.GetProperty("language").ValueKind);
                Assert.Equal(JsonValueKind.Null, data.GetProperty("ageRating").ValueKind);
                Assert.Equal(JsonValueKind.Null, g.GetProperty("description").ValueKind);
            }
        }
        finally
        {
            await cleaner.CleanupAsync();
        }
    }

    [Fact]
    public async Task GetMovieDetail_MultipleGenres_ReturnsAllGenres()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = Guid.NewGuid().ToString("N")[..10];
            long movieId;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                movieId = await cleaner.InsertMovieAsync(conn, $"MM_{tag}", $"Multi {tag}", "now_showing");
                var g1 = await cleaner.InsertGenreAsync(conn, $"GA_{tag}", $"Alpha {tag}");
                var g2 = await cleaner.InsertGenreAsync(conn, $"GB_{tag}", $"Beta {tag}");
                var g3 = await cleaner.InsertGenreAsync(conn, $"GC_{tag}", $"Gamma {tag}");
                await cleaner.LinkMovieGenreAsync(conn, movieId, g1);
                await cleaner.LinkMovieGenreAsync(conn, movieId, g2);
                await cleaner.LinkMovieGenreAsync(conn, movieId, g3);
            }

            var response = await _client.GetAsync($"/api/movies/{movieId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var genres = doc.RootElement.GetProperty("data").GetProperty("genres");
            Assert.Equal(3, genres.GetArrayLength());
            foreach (var g in genres.EnumerateArray())
            {
                Assert.Equal(JsonValueKind.String, g.GetProperty("genreId").ValueKind);
                Assert.Equal(JsonValueKind.String, g.GetProperty("version").ValueKind);
            }
        }
        finally
        {
            await cleaner.CleanupAsync();
        }
    }

    [Fact]
    public async Task GetMovieDetail_Archived_ReturnsNotFound()
    {
        var cleaner = new DbCleaner(_connectionString);
        try
        {
            var tag = Guid.NewGuid().ToString("N")[..10];
            long movieId;
            await using (var conn = await cleaner.OpenConnectionAsync())
            {
                movieId = await cleaner.InsertMovieAsync(conn, $"MA_{tag}", $"Arc {tag}", "archived");
            }

            var response = await _client.GetAsync($"/api/movies/{movieId}");
            await AssertErrorAsync(response, HttpStatusCode.NotFound, "NOT_FOUND");
        }
        finally
        {
            await cleaner.CleanupAsync();
        }
    }

    [Fact]
    public async Task GetMovieDetail_NotExisting_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/movies/999999999");
        await AssertErrorAsync(response, HttpStatusCode.NotFound, "NOT_FOUND");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData(" 1")]
    [InlineData("9223372036854775808")] // overflow long
    public async Task GetMovieDetail_InvalidId_ReturnsValidationError(string movieId)
    {
        var response = await _client.GetAsync($"/api/movies/{Uri.EscapeDataString(movieId)}");
        await AssertValidationErrorAsync(response, "movieId", "invalid_format");
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

    private static async Task<JsonDocument> AssertErrorAsync(HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("error", out var error));
        Assert.Equal(JsonValueKind.Object, error.ValueKind);
        Assert.Equal(expectedCode, error.GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.String, error.GetProperty("message").ValueKind);
        Assert.NotNull(error.GetProperty("message").GetString());
        Assert.True(error.TryGetProperty("details", out var details));
        Assert.Equal(JsonValueKind.Object, details.ValueKind);
        Assert.True(error.TryGetProperty("traceId", out var traceId));
        Assert.Equal(JsonValueKind.String, traceId.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
        return doc;
    }

    private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string field, string reason)
    {
        using var doc = await AssertErrorAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        var details = doc.RootElement.GetProperty("error").GetProperty("details");
        Assert.True(details.TryGetProperty("violations", out var violations));
        Assert.Equal(JsonValueKind.Array, violations.ValueKind);
        var v = Assert.Single(violations.EnumerateArray());
        Assert.Equal(field, v.GetProperty("field").GetString());
        Assert.Equal(reason, v.GetProperty("reason").GetString());
        Assert.Equal(JsonValueKind.String, v.GetProperty("message").ValueKind);
    }
}
