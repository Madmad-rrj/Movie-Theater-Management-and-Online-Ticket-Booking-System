using CinemaTicketBooking.Api.Data;
using CinemaTicketBooking.Api.DTOs.Movies;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Api.Services;

public sealed class MovieService(ApplicationDbContext db) : IMovieService
{
    private static readonly string[] PublicStatuses = ["coming_soon", "now_showing"];

    public async Task<ApiResponse<IReadOnlyList<MovieDto>>> BrowseAsync(
        MovieQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<Models.Movie> movies = db.Movies
            .AsNoTracking()
            .Where(movie => PublicStatuses.Contains(movie.Status));

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var titleQuery = query.SearchTerm.Trim();
            movies = movies.Where(movie => EF.Functions.ILike(movie.Title, $"%{titleQuery}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            movies = movies.Where(movie => movie.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.GenreId))
        {
            if (!long.TryParse(query.GenreId, out var genreId) || genreId <= 0)
            {
                throw new MovieQueryException("INVALID_GENRE_ID", "genreId phải là chuỗi số nguyên dương.");
            }

            movies = movies.Where(movie => movie.MovieGenres.Any(movieGenre => movieGenre.GenreId == genreId));
        }

        var totalItems = await movies.CountAsync(cancellationToken);
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)query.PageSize);

        var rows = await movies
            .OrderByDescending(movie => movie.ReleaseDate.HasValue)
            .ThenByDescending(movie => movie.ReleaseDate)
            .ThenByDescending(movie => movie.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(movie => new MovieDto(
                movie.Id.ToString(),
                movie.Title,
                movie.Description,
                movie.DurationMinutes,
                movie.ReleaseDate,
                movie.PosterUrl,
                movie.TrailerUrl,
                movie.Language,
                movie.AgeRating,
                movie.Status,
                movie.MovieGenres
                    .OrderBy(movieGenre => movieGenre.Genre.Name)
                    .Select(movieGenre => new GenreDto(
                        movieGenre.Genre.Id.ToString(),
                        movieGenre.Genre.Name,
                        movieGenre.Genre.Description,
                        movieGenre.Genre.IsActive,
                        movieGenre.Genre.Version.ToString()))
                    .ToList(),
                movie.Version.ToString()))
            .ToListAsync(cancellationToken);

        return new ApiResponse<IReadOnlyList<MovieDto>>(
            rows,
            new PaginationDto(query.Page, query.PageSize, totalItems, totalPages));
    }
}

public sealed class MovieQueryException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
