using CinemaTicketBooking.Api.DTOs;

namespace CinemaTicketBooking.Api.DTOs.Seats;

public sealed record SeatTypeDto(
    string SeatTypeId,
    string Code,
    string Name,
    MoneyDto Surcharge,
    bool IsActive,
    string Version);

public sealed record SeatDto(
    string SeatId,
    string RowLabel,
    int SeatNumber,
    string Label,
    int X,
    int Y,
    SeatTypeDto SeatType,
    string OperationalState,
    string Version);

public sealed record SeatAvailabilityDto(
    SeatDto Seat,
    string Availability);

public sealed record SeatMapDto(
    string ShowtimeId,
    string RoomId,
    IReadOnlyList<SeatAvailabilityDto> Seats,
    DateTimeOffset ServerTime);
