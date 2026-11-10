using CinemaTicketBooking.Api.Data;
using CinemaTicketBooking.Api.DTOs;
using CinemaTicketBooking.Api.DTOs.Cinemas;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Api.Services;

public sealed class CinemaService(ApplicationDbContext db) : ICinemaService
{
    public async Task<ApiResponse<IReadOnlyList<CinemaDto>>> ListPublicAsync(CancellationToken cancellationToken)
    {
        var cinemas = await db.Cinemas
            .AsNoTracking()
            .Where(c => c.Status == "active")
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Select(c => new CinemaDto(
                c.Id.ToString(),
                c.Name,
                c.Address,
                c.Timezone,
                c.Status == "active",
                c.MinimumTurnaroundMinutes,
                c.Version.ToString()))
            .ToListAsync(cancellationToken);

        return new ApiResponse<IReadOnlyList<CinemaDto>>(cinemas);
    }
}
