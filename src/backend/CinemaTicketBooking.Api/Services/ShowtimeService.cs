using CinemaTicketBooking.Api.Data;
using CinemaTicketBooking.Api.DTOs;
using CinemaTicketBooking.Api.DTOs.Showtimes;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Api.Services;

public sealed class ShowtimeService(ApplicationDbContext db) : IShowtimeService
{
    private static readonly string[] PublicMovieStatuses = ["coming_soon", "now_showing", "ended"];
    private static readonly string[] PublicShowtimeStatuses = ["scheduled", "in_progress", "completed"];

    public async Task<ApiResponse<IReadOnlyList<ShowtimeDto>>> BrowseByMovieAsync(
        string movieId,
        ShowtimeQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryParsePositiveLong(movieId, out var movieIdValue))
        {
            throw new ApiRequestException("VALIDATION_ERROR", "movieId phải là chuỗi số nguyên dương.");
        }

        var movieExists = await db.Movies
            .AsNoTracking()
            .AnyAsync(m => m.Id == movieIdValue && PublicMovieStatuses.Contains(m.Status), cancellationToken);
        if (!movieExists)
        {
            throw new NotFoundApiException("Không tìm thấy phim.");
        }

        if (query.Page < 1)
        {
            throw new ApiRequestException("VALIDATION_ERROR", "page phải lớn hơn hoặc bằng 1.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new ApiRequestException("VALIDATION_ERROR", "pageSize phải nằm trong khoảng 1 đến 100.");
        }

        long? cinemaIdValue = null;
        if (!string.IsNullOrWhiteSpace(query.CinemaId))
        {
            if (!TryParsePositiveLong(query.CinemaId, out var parsedCinemaId))
            {
                throw new ApiRequestException("VALIDATION_ERROR", "cinemaId phải là chuỗi số nguyên dương.");
            }
            cinemaIdValue = parsedCinemaId;
        }

        DateOnly? filterDate = null;
        if (!string.IsNullOrWhiteSpace(query.Date))
        {
            if (!DateOnly.TryParse(query.Date, out var parsedDate))
            {
                throw new ApiRequestException("VALIDATION_ERROR", "date phải có định dạng YYYY-MM-DD.");
            }
            filterDate = parsedDate;
        }

        IQueryable<Models.Showtime> showtimes = db.Showtimes
            .AsNoTracking()
            .Where(s => s.MovieId == movieIdValue
                && PublicShowtimeStatuses.Contains(s.Status)
                && s.CinemaRoom.Cinema.Status == "active");

        if (cinemaIdValue.HasValue)
        {
            showtimes = showtimes.Where(s => s.CinemaRoom.CinemaId == cinemaIdValue.Value);
        }

        var serverTime = DateTimeOffset.UtcNow;
        var all = await showtimes
            .Select(s => new
            {
                s.Id,
                s.MovieId,
                MovieTitle = s.Movie.Title,
                CinemaId = s.CinemaRoom.CinemaId,
                CinemaName = s.CinemaRoom.Cinema.Name,
                CinemaTimezone = s.CinemaRoom.Cinema.Timezone,
                RoomId = s.CinemaRoomId,
                RoomName = s.CinemaRoom.Name,
                s.StartsAt,
                s.EndsAt,
                s.Status,
                s.Version
            })
            .ToListAsync(cancellationToken);

        IEnumerable<dynamic> filtered = all;
        if (filterDate.HasValue)
        {
            filtered = all.Where(s => ToCinemaDate((DateTimeOffset)s.StartsAt, (string)s.CinemaTimezone) == filterDate.Value);
        }

        var ordered = filtered
            .OrderBy(s => s.StartsAt)
            .ThenBy(s => s.Id)
            .ToList();

        var totalItems = ordered.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)query.PageSize);
        var pageItems = ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new ShowtimeDto(
                ((long)s.Id).ToString(),
                ((long)s.MovieId).ToString(),
                (string)s.MovieTitle,
                ((long)s.CinemaId).ToString(),
                (string)s.CinemaName,
                ((long)s.RoomId).ToString(),
                (string)s.RoomName,
                ((DateTimeOffset)s.StartsAt).ToUniversalTime(),
                ((DateTimeOffset)s.EndsAt).ToUniversalTime(),
                (string)s.CinemaTimezone,
                (string)s.Status,
                ComputeBookingAvailable((string)s.Status, (DateTimeOffset)s.StartsAt, serverTime),
                serverTime,
                ((long)s.Version).ToString()))
            .ToList();

        return new ApiResponse<IReadOnlyList<ShowtimeDto>>(pageItems, new PaginationDto(query.Page, query.PageSize, totalItems, totalPages));
    }

    public async Task<ShowtimeDto> GetPublicDetailAsync(string showtimeId, CancellationToken cancellationToken)
    {
        if (!TryParsePositiveLong(showtimeId, out var id))
        {
            throw new ApiRequestException("VALIDATION_ERROR", "showtimeId phải là chuỗi số nguyên dương.");
        }

        var serverTime = DateTimeOffset.UtcNow;
        var showtime = await db.Showtimes
            .AsNoTracking()
            .Where(s => s.Id == id
                && PublicShowtimeStatuses.Contains(s.Status)
                && s.CinemaRoom.Cinema.Status == "active"
                && PublicMovieStatuses.Contains(s.Movie.Status))
            .Select(s => new
            {
                s.Id,
                s.MovieId,
                MovieTitle = s.Movie.Title,
                CinemaId = s.CinemaRoom.CinemaId,
                CinemaName = s.CinemaRoom.Cinema.Name,
                CinemaTimezone = s.CinemaRoom.Cinema.Timezone,
                RoomId = s.CinemaRoomId,
                RoomName = s.CinemaRoom.Name,
                s.StartsAt,
                s.EndsAt,
                s.Status,
                s.Version
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (showtime is null)
        {
            throw new NotFoundApiException("Không tìm thấy suất chiếu.");
        }

        return new ShowtimeDto(
            showtime.Id.ToString(),
            showtime.MovieId.ToString(),
            showtime.MovieTitle,
            showtime.CinemaId.ToString(),
            showtime.CinemaName,
            showtime.RoomId.ToString(),
            showtime.RoomName,
            showtime.StartsAt.ToUniversalTime(),
            showtime.EndsAt.ToUniversalTime(),
            showtime.CinemaTimezone,
            showtime.Status,
            ComputeBookingAvailable(showtime.Status, showtime.StartsAt, serverTime),
            serverTime,
            showtime.Version.ToString());
    }

    private static bool ComputeBookingAvailable(string status, DateTimeOffset startsAt, DateTimeOffset serverTime)
    {
        return status == "scheduled" && startsAt > serverTime;
    }

    private static DateOnly ToCinemaDate(DateTimeOffset startsAt, string ianaTimezone)
    {
        var tz = ResolveTimeZone(ianaTimezone);
        var local = TimeZoneInfo.ConvertTime(startsAt, tz);
        return DateOnly.FromDateTime(local.DateTime);
    }

    private static TimeZoneInfo ResolveTimeZone(string ianaTimezone)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ianaTimezone);
        }
        catch
        {
            // Windows fallback for common Vietnam timezone
            if (ianaTimezone == "Asia/Ho_Chi_Minh")
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            return TimeZoneInfo.Utc;
        }
    }

    private static bool TryParsePositiveLong(string value, out long result)
    {
        return long.TryParse(value, out result) && result > 0;
    }
}
