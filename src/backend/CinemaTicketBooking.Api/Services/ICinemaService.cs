using CinemaTicketBooking.Api.DTOs;
using CinemaTicketBooking.Api.DTOs.Cinemas;

namespace CinemaTicketBooking.Api.Services;

public interface ICinemaService
{
    Task<ApiResponse<IReadOnlyList<CinemaDto>>> ListPublicAsync(CancellationToken cancellationToken);
}
