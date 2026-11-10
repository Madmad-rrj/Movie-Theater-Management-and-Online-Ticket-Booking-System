using Npgsql;

namespace CinemaTicketBooking.Api.Tests.Infrastructure;

public sealed class DbCleaner
{
    private readonly string _connectionString;
    private readonly List<long> _movieIds = new();
    private readonly List<long> _genreIds = new();

    public DbCleaner(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void TrackMovie(long id) => _movieIds.Add(id);
    public void TrackGenre(long id) => _genreIds.Add(id);

    public async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }

    public async Task<long> InsertMovieAsync(NpgsqlConnection conn, string seedKey, string title, string status,
        string? description = null, int durationMinutes = 120, DateOnly? releaseDate = null,
        string? posterUrl = null, string? trailerUrl = null, string? language = null, string? ageRating = null)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO cinema.movie (seed_key,title,description,duration_minutes,release_date,poster_url,trailer_url,language,age_rating,status)
VALUES (@seed,@title,@desc,@dur,@release,@poster,@trailer,@lang,@age,@status)
RETURNING id;";
        cmd.Parameters.AddWithValue("seed", seedKey);
        cmd.Parameters.AddWithValue("title", title);
        cmd.Parameters.AddWithValue("desc", (object?)description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("dur", (short)durationMinutes);
        cmd.Parameters.AddWithValue("release", (object?)releaseDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("poster", (object?)posterUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("trailer", (object?)trailerUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("lang", (object?)language ?? DBNull.Value);
        cmd.Parameters.AddWithValue("age", (object?)ageRating ?? DBNull.Value);
        cmd.Parameters.AddWithValue("status", status);
        var id = (long)(await cmd.ExecuteScalarAsync())!;
        _movieIds.Add(id);
        return id;
    }

    public async Task<long> InsertGenreAsync(NpgsqlConnection conn, string code, string name, string? description = null, bool isActive = true)
    {
        if (code.Length > 30) code = code[..30];
        var norm = name.ToUpperInvariant();
        if (norm.Length > 100) norm = norm[..100];
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO cinema.genre (code,name,normalized_name,description,is_active)
VALUES (@code,@name,@norm,@desc,@active)
RETURNING id;";
        cmd.Parameters.AddWithValue("code", code);
        cmd.Parameters.AddWithValue("name", name);
        cmd.Parameters.AddWithValue("norm", norm);
        cmd.Parameters.AddWithValue("desc", (object?)description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("active", isActive);
        var id = (long)(await cmd.ExecuteScalarAsync())!;
        _genreIds.Add(id);
        return id;
    }

    public async Task LinkMovieGenreAsync(NpgsqlConnection conn, long movieId, long genreId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO cinema.movie_genre (movie_id, genre_id) VALUES (@m,@g);";
        cmd.Parameters.AddWithValue("m", movieId);
        cmd.Parameters.AddWithValue("g", genreId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task CleanupAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        if (_movieIds.Count > 0)
        {
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM cinema.movie_genre WHERE movie_id = ANY(@ids);";
                cmd.Parameters.AddWithValue("ids", _movieIds.ToArray());
                await cmd.ExecuteNonQueryAsync();
            }
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM cinema.movie WHERE id = ANY(@ids);";
                cmd.Parameters.AddWithValue("ids", _movieIds.ToArray());
                await cmd.ExecuteNonQueryAsync();
            }
        }
        if (_genreIds.Count > 0)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cinema.genre WHERE id = ANY(@ids);";
            cmd.Parameters.AddWithValue("ids", _genreIds.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public static async Task<(long movies, long genres, long movieGenres)> CountMovieTablesAsync(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        async Task<long> Count(string table)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT count(*) FROM {table};";
            return (long)(await cmd.ExecuteScalarAsync())!;
        }
        var movies = await Count("cinema.movie");
        var genres = await Count("cinema.genre");
        var links = await Count("cinema.movie_genre");
        return (movies, genres, links);
    }
}
