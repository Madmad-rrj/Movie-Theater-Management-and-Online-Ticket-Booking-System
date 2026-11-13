using CinemaTicketBooking.Api.DTOs;

namespace CinemaTicketBooking.Api.DTOs.Pricing;

public sealed record TicketCategoryDto(
    string Code,
    string Name,
    string? Description,
    string Version);

public sealed record CinemaPricingDto(
    string CinemaId,
    MoneyDto AdultBasePrice,
    MoneyDto DiscountedBasePrice,
    string Version,
    DateTimeOffset UpdatedAt);
