using CinemaTicketBooking.Api.DTOs;
using CinemaTicketBooking.Api.DTOs.Pricing;

namespace CinemaTicketBooking.Api.Services;

public interface IPricingService
{
    Task<ApiResponse<IReadOnlyList<TicketCategoryDto>>> ListTicketCategoriesAsync(CancellationToken cancellationToken);
    Task<ApiResponse<CinemaPricingDto>> GetCinemaPricingAsync(string cinemaId, CancellationToken cancellationToken);
}
