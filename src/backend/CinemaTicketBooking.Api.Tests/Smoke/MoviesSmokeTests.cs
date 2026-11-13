using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CinemaTicketBooking.Api.Tests.Infrastructure;
using Xunit;

namespace CinemaTicketBooking.Api.Tests.Smoke;

[Collection("Database")]
public sealed class MoviesSmokeTests
{
    private readonly HttpClient _client;

    public MoviesSmokeTests(PostgresFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task GetMovies_ReturnsOk_WithExpectedEnvelope()
    {
        var response = await _client.GetAsync("/api/movies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("data", out var data));
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
        Assert.True(root.TryGetProperty("pagination", out var pagination));
        Assert.Equal(JsonValueKind.Object, pagination.ValueKind);
        Assert.True(pagination.TryGetProperty("page", out _));
        Assert.True(pagination.TryGetProperty("pageSize", out _));
        Assert.True(pagination.TryGetProperty("totalItems", out _));
        Assert.True(pagination.TryGetProperty("totalPages", out _));
    }
}
