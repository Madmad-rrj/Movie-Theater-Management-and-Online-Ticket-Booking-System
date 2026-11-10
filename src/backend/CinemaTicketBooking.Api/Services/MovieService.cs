using System.Globalization;
using CinemaTicketBooking.Api.Data;
using CinemaTicketBooking.Api.DTOs;
using CinemaTicketBooking.Api.DTOs.Movies;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Api.Services;

public sealed class MovieService(ApplicationDbContext db) : IMovieService
{
    private static readonly string[] PublicStatuses = ["coming_soon", "now_showing"];
    private static readonly string[] PublicDetailStatuses = ["coming_soon", "now_showing", "ended"];

    public async Task<ApiResponse<IReadOnlyList<MovieDto>>> BrowseAsync(
        MovieQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Page < 1)
        {
            throw ApiValidation.Error("page", "min_value", "page phải lớn hơn hoặc bằng 1.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw ApiValidation.Error("pageSize", "out_of_range", "pageSize phải nằm trong khoảng 1 đến 100.");
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            query.Status is not ("coming_soon" or "now_showing"))
        {
            throw ApiValidation.Error("status", "invalid_value", "status chỉ nhận coming_soon hoặc now_showing.");
        }

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
            if (!long.TryParse(query.GenreId, NumberStyles.None, CultureInfo.InvariantCulture, out var genreId) || genreId <= 0)
            {
                throw ApiValidation.Error("genreId", "invalid_format", "genreId phải là chuỗi số nguyên dương.");
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

    public async Task<MovieDto> GetPublicDetailAsync(string movieId, CancellationToken cancellationToken)
    {
        if (!long.TryParse(movieId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
        {
            throw ApiValidation.Error("movieId", "invalid_format", "movieId phải là chuỗi số nguyên dương.");
        }

        var movie = await db.Movies
            .AsNoTracking()
            .Where(m => m.Id == id && PublicDetailStatuses.Contains(m.Status))
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
            .SingleOrDefaultAsync(cancellationToken);

        if (movie is null)
        {
            throw new NotFoundApiException("Không tìm thấy phim.");
        }

        return movie;
    }
}
