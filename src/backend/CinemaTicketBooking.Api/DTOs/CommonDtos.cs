namespace CinemaTicketBooking.Api.DTOs;

public sealed record MoneyDto(long Amount, string Currency);

public sealed record ApiError(string Code, string Message, object? Details = null);

public sealed record PaginationDto(int Page, int PageSize, int TotalItems, int TotalPages);

public sealed record ApiResponse<T>(T Data, PaginationDto? Pagination = null);
