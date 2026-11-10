using CinemaTicketBooking.Api.DTOs;

namespace CinemaTicketBooking.Api.DTOs.Cinemas;

public sealed record CinemaDto(
    string CinemaId,
    string Name,
    string Address,
    string Timezone,
    bool IsActive,
    int MinimumTurnaroundMinutes,
    string Version);
