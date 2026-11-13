namespace CinemaTicketBooking.Api.DTOs.Showtimes;

public sealed record ShowtimeDto(
    string ShowtimeId,
    string MovieId,
    string MovieTitle,
    string CinemaId,
    string CinemaName,
    string RoomId,
    string RoomName,
    DateTimeOffset OfficialStartsAt,
    DateTimeOffset OfficialEndsAt,
    string Timezone,
    string Status,
    bool BookingAvailable,
    DateTimeOffset ServerTime,
    string Version);

public sealed record ShowtimeQuery
{
    public string? Date { get; init; }
    public string? CinemaId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
