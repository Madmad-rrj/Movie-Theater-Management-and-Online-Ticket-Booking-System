using CinemaTicketBooking.Api.Data;
using CinemaTicketBooking.Api.DTOs;
using CinemaTicketBooking.Api.DTOs.Seats;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Api.Services;

public sealed class SeatMapService(ApplicationDbContext db) : ISeatMapService
{
    private static readonly string[] PublicShowtimeStatuses = ["scheduled", "in_progress", "completed"];
    private static readonly string[] PublicMovieStatuses = ["coming_soon", "now_showing", "ended"];

    public async Task<SeatMapDto> GetSeatMapAsync(string showtimeId, CancellationToken cancellationToken)
    {
        if (!long.TryParse(showtimeId, out var id) || id <= 0)
        {
            throw new ApiRequestException("VALIDATION_ERROR", "showtimeId phải là chuỗi số nguyên dương.");
        }

        var showtime = await db.Showtimes
            .AsNoTracking()
            .Where(s => s.Id == id
                && PublicShowtimeStatuses.Contains(s.Status)
                && s.CinemaRoom.Cinema.Status == "active"
                && PublicMovieStatuses.Contains(s.Movie.Status))
            .Select(s => new { s.Id, s.CinemaRoomId })
            .SingleOrDefaultAsync(cancellationToken);

        if (showtime is null)
        {
            throw new NotFoundApiException("Không tìm thấy suất chiếu.");
        }

        var seats = await db.Seats
            .AsNoTracking()
            .Where(seat => seat.CinemaRoomId == showtime.CinemaRoomId)
            .OrderBy(seat => seat.RowLabel)
            .ThenBy(seat => seat.SeatNumber)
            .Select(seat => new
            {
                seat.Id,
                seat.RowLabel,
                seat.SeatNumber,
                seat.XPosition,
                seat.YPosition,
                seat.OperationalState,
                seat.RetiredAt,
                seat.Version,
                SeatTypeId = seat.SeatType.Id,
                SeatTypeCode = seat.SeatType.Code,
                SeatTypeName = seat.SeatType.Name,
                SurchargeAmount = seat.SeatType.SurchargeAmount,
                SeatTypeIsActive = seat.SeatType.IsActive,
                SeatTypeVersion = seat.SeatType.Version
            })
            .ToListAsync(cancellationToken);

        var allocationStatuses = await db.BookingSeats
            .AsNoTracking()
            .Where(bs => bs.ShowtimeId == id && (bs.AllocationStatus == "held" || bs.AllocationStatus == "sold"))
            .GroupBy(bs => bs.SeatId)
            .Select(g => new { SeatId = g.Key, Status = g.Max(x => x.AllocationStatus) })
            .ToListAsync(cancellationToken);
        var statusBySeat = allocationStatuses.ToDictionary(x => x.SeatId, x => x.Status);

        var serverTime = DateTimeOffset.UtcNow;
        var items = seats.Select(seat =>
        {
            var label = $"{seat.RowLabel}{seat.SeatNumber}";
            string availability;
            if (statusBySeat.TryGetValue(seat.Id, out var allocStatus))
            {
                availability = allocStatus == "sold" ? "sold" : "held";
            }
            else if (seat.RetiredAt != null || seat.OperationalState != "active")
            {
                availability = "unavailable";
            }
            else
            {
                availability = "available";
            }

            var seatDto = new SeatDto(
                seat.Id.ToString(),
                seat.RowLabel,
                seat.SeatNumber,
                label,
                seat.XPosition,
                seat.YPosition,
                new SeatTypeDto(
                    seat.SeatTypeId.ToString(),
                    seat.SeatTypeCode,
                    seat.SeatTypeName,
                    new MoneyDto(decimal.ToInt64(seat.SurchargeAmount), "VND"),
                    seat.SeatTypeIsActive,
                    seat.SeatTypeVersion.ToString()),
                seat.OperationalState,
                seat.Version.ToString());
            return new SeatAvailabilityDto(seatDto, availability);
        }).ToList();

        return new SeatMapDto(showtime.Id.ToString(), showtime.CinemaRoomId.ToString(), items, serverTime);
    }

}
