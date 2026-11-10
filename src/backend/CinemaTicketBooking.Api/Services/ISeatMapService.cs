using CinemaTicketBooking.Api.DTOs.Seats;

namespace CinemaTicketBooking.Api.Services;

public interface ISeatMapService
{
    Task<SeatMapDto> GetSeatMapAsync(string showtimeId, CancellationToken cancellationToken);
}
