using CinemaTicketBooking.Api.Data;
using CinemaTicketBooking.Api.DTOs;
using CinemaTicketBooking.Api.DTOs.Pricing;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Api.Services;

public sealed class PricingService(ApplicationDbContext db) : IPricingService
{
    public async Task<ApiResponse<IReadOnlyList<TicketCategoryDto>>> ListTicketCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await db.TicketCategories
            .AsNoTracking()
            .OrderBy(c => c.Code == "Adult" ? 0 : 1)
            .ThenBy(c => c.Code)
            .Select(c => new TicketCategoryDto(
                c.Code,
                c.DisplayName,
                c.EligibilityDescription,
                c.Version.ToString()))
            .ToListAsync(cancellationToken);

        return new ApiResponse<IReadOnlyList<TicketCategoryDto>>(categories);
    }

    public async Task<ApiResponse<CinemaPricingDto>> GetCinemaPricingAsync(string cinemaId, CancellationToken cancellationToken)
    {
        if (!long.TryParse(cinemaId, out var id) || id <= 0)
        {
            throw ApiValidation.Error("cinemaId", "invalid_format", "cinemaId phải là chuỗi số nguyên dương.");
        }

        var cinemaExists = await db.Cinemas.AsNoTracking().AnyAsync(c => c.Id == id && c.Status == "active", cancellationToken);
        if (!cinemaExists)
        {
            throw new NotFoundApiException("Không tìm thấy rạp.");
        }

        var prices = await db.CinemaTicketPrices
            .AsNoTracking()
            .Where(p => p.CinemaId == id)
            .ToListAsync(cancellationToken);

        var adult = prices.FirstOrDefault(p => p.CategoryCode == "Adult");
        var discounted = prices.FirstOrDefault(p => p.CategoryCode == "Discounted");
        if (adult is null || discounted is null)
        {
            throw new ApiRequestException(
                "STATE_CONFLICT",
                "Rạp chưa được cấu hình đầy đủ giá vé.",
                StatusCodes.Status409Conflict);
        }

        var version = Math.Max(adult.Version, discounted.Version).ToString();
        var updatedAt = adult.UpdatedAt >= discounted.UpdatedAt ? adult.UpdatedAt : discounted.UpdatedAt;
        var dto = new CinemaPricingDto(
            id.ToString(),
            new MoneyDto(decimal.ToInt64(adult.Amount), adult.Currency),
            new MoneyDto(decimal.ToInt64(discounted.Amount), discounted.Currency),
            version,
            updatedAt.ToUniversalTime());

        return new ApiResponse<CinemaPricingDto>(dto);
    }
}
