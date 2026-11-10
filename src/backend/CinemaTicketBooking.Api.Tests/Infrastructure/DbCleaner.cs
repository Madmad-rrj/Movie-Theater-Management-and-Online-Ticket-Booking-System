using Npgsql;

namespace CinemaTicketBooking.Api.Tests.Infrastructure;

public sealed class DbCleaner
{
    private readonly string _connectionString;
    private readonly List<long> _movieIds = new();
    private readonly List<long> _genreIds = new();
    private readonly List<long> _cinemaIds = new();
    private readonly List<long> _roomIds = new();
    private readonly List<long> _showtimeIds = new();

    public DbCleaner(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void TrackMovie(long id) => _movieIds.Add(id);
    public void TrackGenre(long id) => _genreIds.Add(id);
    public void TrackCinema(long id) => _cinemaIds.Add(id);
    public void TrackRoom(long id) => _roomIds.Add(id);
    public void TrackShowtime(long id) => _showtimeIds.Add(id);

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

    public async Task<long> InsertCinemaAsync(NpgsqlConnection conn, string code, string name, string address,
        string timezone = "Asia/Ho_Chi_Minh", string status = "active", short minimumTurnaroundMinutes = 15)
    {
        if (code.Length > 40) code = code[..40];
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO cinema.cinema (code,name,address,timezone,status,minimum_turnaround_minutes)
VALUES (@code,@name,@addr,@tz,@status,@turn)
RETURNING id;";
        cmd.Parameters.AddWithValue("code", code);
        cmd.Parameters.AddWithValue("name", name);
        cmd.Parameters.AddWithValue("addr", address);
        cmd.Parameters.AddWithValue("tz", timezone);
        cmd.Parameters.AddWithValue("status", status);
        cmd.Parameters.AddWithValue("turn", minimumTurnaroundMinutes);
        var id = (long)(await cmd.ExecuteScalarAsync())!;
        _cinemaIds.Add(id);
        return id;
    }

    public async Task<long> InsertRoomAsync(NpgsqlConnection conn, long cinemaId, string code, string name, string status = "active")
    {
        if (code.Length > 30) code = code[..30];
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO cinema.cinema_room (cinema_id,code,name,status)
VALUES (@cid,@code,@name,@status)
RETURNING id;";
        cmd.Parameters.AddWithValue("cid", cinemaId);
        cmd.Parameters.AddWithValue("code", code);
        cmd.Parameters.AddWithValue("name", name);
        cmd.Parameters.AddWithValue("status", status);
        var id = (long)(await cmd.ExecuteScalarAsync())!;
        _roomIds.Add(id);
        return id;
    }

    public async Task<long> InsertShowtimeAsync(NpgsqlConnection conn, long movieId, long roomId, DateTimeOffset startsAt,
        short durationMinutes = 120, short turnaroundMinutes = 15, string status = "scheduled", string? seedKey = null,
        DateTimeOffset? cancelledAt = null, string? cancellationReason = null)
    {
        var endsAt = startsAt.AddMinutes(durationMinutes);
        var roomAvailableAt = endsAt.AddMinutes(turnaroundMinutes);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO cinema.showtime (seed_key,movie_id,cinema_room_id,duration_minutes_snapshot,turnaround_minutes_applied,starts_at,ends_at,room_available_at,status,cancelled_at,cancellation_reason)
VALUES (@seed,@mid,@rid,@dur,@turn,@start,@end,@avail,@status,@cat,@creason)
RETURNING id;";
        cmd.Parameters.AddWithValue("seed", (object?)seedKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("mid", movieId);
        cmd.Parameters.AddWithValue("rid", roomId);
        cmd.Parameters.AddWithValue("dur", durationMinutes);
        cmd.Parameters.AddWithValue("turn", turnaroundMinutes);
        cmd.Parameters.AddWithValue("start", startsAt);
        cmd.Parameters.AddWithValue("end", endsAt);
        cmd.Parameters.AddWithValue("avail", roomAvailableAt);
        cmd.Parameters.AddWithValue("status", status);
        cmd.Parameters.AddWithValue("cat", (object?)cancelledAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("creason", (object?)cancellationReason ?? DBNull.Value);
        var id = (long)(await cmd.ExecuteScalarAsync())!;
        _showtimeIds.Add(id);
        return id;
    }

    public async Task CleanupAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        if (_showtimeIds.Count > 0)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cinema.showtime WHERE id = ANY(@ids);";
            cmd.Parameters.AddWithValue("ids", _showtimeIds.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
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
        if (_roomIds.Count > 0)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cinema.cinema_room WHERE id = ANY(@ids);";
            cmd.Parameters.AddWithValue("ids", _roomIds.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
        if (_cinemaIds.Count > 0)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cinema.cinema WHERE id = ANY(@ids);";
            cmd.Parameters.AddWithValue("ids", _cinemaIds.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public static async Task<(long movies, long genres, long movieGenres, long cinemas, long rooms, long showtimes)> CountCoreTablesAsync(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        async Task<long> Count(string table)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT count(*) FROM {table};";
            return (long)(await cmd.ExecuteScalarAsync())!;
        }
        return (
            await Count("cinema.movie"),
            await Count("cinema.genre"),
            await Count("cinema.movie_genre"),
            await Count("cinema.cinema"),
            await Count("cinema.cinema_room"),
            await Count("cinema.showtime")
        );
    }
}
