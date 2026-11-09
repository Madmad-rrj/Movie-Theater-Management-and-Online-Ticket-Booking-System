namespace CinemaTicketBooking.Api.DTOs.Movies;

public sealed record MovieDto(
    string MovieId,
    string Title,
    string? Description,
    int DurationMinutes,
    DateOnly? ReleaseDate,
    string? PosterUrl,
    string? TrailerUrl,
    string? Language,
    string? AgeRating,
    string Status,
    IReadOnlyList<GenreDto> Genres,
    string Version);

public sealed record GenreDto(
    string GenreId,
    string Name,
    string? Description,
    bool IsActive,
    string Version);

public sealed record PaginationDto(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record ApiResponse<T>(
    T Data,
    PaginationDto? Pagination = null);

public sealed record ApiError(
    string Code,
    string Message,
    object? Details = null);
