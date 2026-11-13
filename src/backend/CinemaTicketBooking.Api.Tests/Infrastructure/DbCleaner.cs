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
    private readonly List<long> _priceCinemaIds = new();
    private readonly List<long> _seatTypeIds = new();
    private readonly List<long> _seatIds = new();
    private readonly List<Guid> _checkoutIdentityIds = new();
    private readonly List<Guid> _selectionSessionIds = new();
    private readonly List<long> _bookingIds = new();
    private readonly List<long> _bookingSeatIds = new();

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

    public async Task InsertCinemaTicketPriceAsync(NpgsqlConnection conn, long cinemaId, string categoryCode, long amount, string currency = "VND")
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO cinema.cinema_ticket_price (cinema_id,category_code,amount,currency)
VALUES (@cid,@cat,@amount,@cur);";
        cmd.Parameters.AddWithValue("cid", cinemaId);
        cmd.Parameters.AddWithValue("cat", categoryCode);
        cmd.Parameters.AddWithValue("amount", amount);
        cmd.Parameters.AddWithValue("cur", currency);
        await cmd.ExecuteNonQueryAsync();
        if (!_priceCinemaIds.Contains(cinemaId)) _priceCinemaIds.Add(cinemaId);
    }

    public async Task<long> InsertSeatTypeAsync(NpgsqlConnection conn, string code, string name, long surchargeAmount = 0, bool isActive = true)
    {
        if (code.Length > 30) code = code[..30];
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO cinema.seat_type (code,name,surcharge_amount,is_active)
VALUES (@code,@name,@surcharge,@active)
RETURNING id;";
        cmd.Parameters.AddWithValue("code", code);
        cmd.Parameters.AddWithValue("name", name);
        cmd.Parameters.AddWithValue("surcharge", surchargeAmount);
        cmd.Parameters.AddWithValue("active", isActive);
        var id = (long)(await cmd.ExecuteScalarAsync())!;
        _seatTypeIds.Add(id);
        return id;
    }

    public async Task<long> InsertSeatAsync(NpgsqlConnection conn, long roomId, long seatTypeId, string rowLabel, short seatNumber,
        int x, int y, string operationalState = "active", DateTimeOffset? retiredAt = null)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO cinema.seat (cinema_room_id,seat_type_id,row_label,seat_number,x_position,y_position,operational_state,retired_at)
VALUES (@rid,@stid,@row,@num,@x,@y,@state,@ret)
RETURNING id;";
        cmd.Parameters.AddWithValue("rid", roomId);
        cmd.Parameters.AddWithValue("stid", seatTypeId);
        cmd.Parameters.AddWithValue("row", rowLabel);
        cmd.Parameters.AddWithValue("num", seatNumber);
        cmd.Parameters.AddWithValue("x", x);
        cmd.Parameters.AddWithValue("y", y);
        cmd.Parameters.AddWithValue("state", operationalState);
        cmd.Parameters.AddWithValue("ret", (object?)retiredAt ?? DBNull.Value);
        var id = (long)(await cmd.ExecuteScalarAsync())!;
        _seatIds.Add(id);
        return id;
    }

    public async Task<Guid> InsertCheckoutIdentityAsync(NpgsqlConnection conn, DateTimeOffset createdAt, DateTimeOffset guestExpiresAt)
    {
        var id = Guid.NewGuid();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO cinema.checkout_identity (id,user_id,guest_credential_hash,guest_expires_at,revoked_at,created_at)
VALUES (@id,NULL,@hash,@exp,NULL,@created);";
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("hash", $"hash_{id:N}");
        cmd.Parameters.AddWithValue("exp", guestExpiresAt);
        cmd.Parameters.AddWithValue("created", createdAt);
        await cmd.ExecuteNonQueryAsync();
        _checkoutIdentityIds.Add(id);
        return id;
    }

    public async Task<Guid> InsertSelectionSessionAsync(NpgsqlConnection conn, Guid checkoutIdentityId, long showtimeId, long roomId,
        DateTimeOffset startedAt, string state = "consumed", int adultQty = 1, int discountedQty = 0)
    {
        var id = Guid.NewGuid();
        var expiresAt = startedAt.AddSeconds(300);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO cinema.selection_session (id,checkout_identity_id,showtime_id,cinema_room_id,started_at,expires_at,adult_quantity,discounted_quantity,state)
VALUES (@id,@cid,@sid,@rid,@start,@exp,@aq,@dq,@state);";
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("cid", checkoutIdentityId);
        cmd.Parameters.AddWithValue("sid", showtimeId);
        cmd.Parameters.AddWithValue("rid", roomId);
        cmd.Parameters.AddWithValue("start", startedAt);
        cmd.Parameters.AddWithValue("exp", expiresAt);
        cmd.Parameters.AddWithValue("aq", adultQty);
        cmd.Parameters.AddWithValue("dq", discountedQty);
        cmd.Parameters.AddWithValue("state", state);
        await cmd.ExecuteNonQueryAsync();
        _selectionSessionIds.Add(id);
        return id;
    }

    public async Task<long> InsertBookingAsync(NpgsqlConnection conn, Guid checkoutIdentityId, Guid selectionSessionId,
        long showtimeId, long roomId, DateTimeOffset createdAt, DateTimeOffset reservationExpiresAt, string status = "pending",
        string movieTitle = "Movie", string roomName = "Room", long totalAmount = 100000)
    {
        var now = createdAt;
        DateTimeOffset? confirmedAt = status == "confirmed" ? now : null;
        DateTimeOffset? cancelledAt = status == "cancelled" ? now : null;
        string? cancellationReason = status == "cancelled" ? "test cancel" : null;
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO cinema.booking (booking_code,checkout_identity_id,selection_session_id,showtime_id,cinema_room_id,contact_full_name,contact_phone,contact_email,terms_accepted_at,age_acknowledged_at,purchased_start_at,movie_title_snapshot,room_name_snapshot,reservation_expires_at,maximum_tickets_snapshot,total_amount,currency,status,created_at,confirmed_at,cancelled_at,cancellation_reason)
VALUES (@code,@cid,@ssid,@sid,@rid,@name,@phone,@mail,@terms,@age,@pstart,@mtitle,@rname,@rexp,8,@total,'VND',@status,@created,@conf,@canc,@creason)
RETURNING id;";
        cmd.Parameters.AddWithValue("code", Guid.NewGuid());
        cmd.Parameters.AddWithValue("cid", checkoutIdentityId);
        cmd.Parameters.AddWithValue("ssid", selectionSessionId);
        cmd.Parameters.AddWithValue("sid", showtimeId);
        cmd.Parameters.AddWithValue("rid", roomId);
        cmd.Parameters.AddWithValue("name", "Test User");
        cmd.Parameters.AddWithValue("phone", "0900000000");
        cmd.Parameters.AddWithValue("mail", "test@example.com");
        cmd.Parameters.AddWithValue("terms", now);
        cmd.Parameters.AddWithValue("age", now);
        cmd.Parameters.AddWithValue("pstart", now);
        cmd.Parameters.AddWithValue("mtitle", movieTitle);
        cmd.Parameters.AddWithValue("rname", roomName);
        cmd.Parameters.AddWithValue("rexp", reservationExpiresAt);
        cmd.Parameters.AddWithValue("total", totalAmount);
        cmd.Parameters.AddWithValue("status", status);
        cmd.Parameters.AddWithValue("created", createdAt);
        cmd.Parameters.AddWithValue("conf", (object?)confirmedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("canc", (object?)cancelledAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("creason", (object?)cancellationReason ?? DBNull.Value);
        var id = (long)(await cmd.ExecuteScalarAsync())!;
        _bookingIds.Add(id);
        return id;
    }

    public async Task<long> InsertBookingSeatAsync(NpgsqlConnection conn, long bookingId, long showtimeId, long roomId, long seatId,
        string seatLabel, string seatTypeName, long surchargeSnapshot, string allocationStatus)
    {
        DateTimeOffset? releasedAt = allocationStatus == "released" ? DateTimeOffset.UtcNow : null;
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO cinema.booking_seat (booking_id,showtime_id,cinema_room_id,seat_id,seat_label_snapshot,seat_type_name_snapshot,surcharge_snapshot,allocation_status,released_at)
VALUES (@bid,@sid,@rid,@seatid,@label,@stype,@surcharge,@status,@rel)
RETURNING id;";
        cmd.Parameters.AddWithValue("bid", bookingId);
        cmd.Parameters.AddWithValue("sid", showtimeId);
        cmd.Parameters.AddWithValue("rid", roomId);
        cmd.Parameters.AddWithValue("seatid", seatId);
        cmd.Parameters.AddWithValue("label", seatLabel);
        cmd.Parameters.AddWithValue("stype", seatTypeName);
        cmd.Parameters.AddWithValue("surcharge", surchargeSnapshot);
        cmd.Parameters.AddWithValue("status", allocationStatus);
        cmd.Parameters.AddWithValue("rel", (object?)releasedAt ?? DBNull.Value);
        var id = (long)(await cmd.ExecuteScalarAsync())!;
        _bookingSeatIds.Add(id);
        return id;
    }

    public async Task CleanupAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        if (_bookingSeatIds.Count > 0)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cinema.booking_seat WHERE id = ANY(@ids);";
            cmd.Parameters.AddWithValue("ids", _bookingSeatIds.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
        if (_bookingIds.Count > 0)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cinema.booking WHERE id = ANY(@ids);";
            cmd.Parameters.AddWithValue("ids", _bookingIds.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
        if (_selectionSessionIds.Count > 0)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cinema.selection_session WHERE id = ANY(@ids);";
            cmd.Parameters.AddWithValue("ids", _selectionSessionIds.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
        if (_checkoutIdentityIds.Count > 0)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cinema.checkout_identity WHERE id = ANY(@ids);";
            cmd.Parameters.AddWithValue("ids", _checkoutIdentityIds.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
        if (_showtimeIds.Count > 0)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cinema.showtime WHERE id = ANY(@ids);";
            cmd.Parameters.AddWithValue("ids", _showtimeIds.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
        if (_seatIds.Count > 0)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cinema.seat WHERE id = ANY(@ids);";
            cmd.Parameters.AddWithValue("ids", _seatIds.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
        if (_seatTypeIds.Count > 0)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cinema.seat_type WHERE id = ANY(@ids);";
            cmd.Parameters.AddWithValue("ids", _seatTypeIds.ToArray());
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
        var priceCinemaIds = _priceCinemaIds.Union(_cinemaIds).Distinct().ToArray();
        if (priceCinemaIds.Length > 0)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cinema.cinema_ticket_price WHERE cinema_id = ANY(@ids);";
            cmd.Parameters.AddWithValue("ids", priceCinemaIds);
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

    public static async Task<long> CountTableAsync(string connectionString, string table)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT count(*) FROM {table};";
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public static async Task<(long seats, long seatTypes, long bookingSeats, long bookings, long showtimes, long rooms, long cinemas, long movies)> CountSeatRelatedTablesAsync(string connectionString)
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
            await Count("cinema.seat"),
            await Count("cinema.seat_type"),
            await Count("cinema.booking_seat"),
            await Count("cinema.booking"),
            await Count("cinema.showtime"),
            await Count("cinema.cinema_room"),
            await Count("cinema.cinema"),
            await Count("cinema.movie")
        );
    }

    public static async Task<List<(string Code, string Name, string Description, long Version)>> GetTicketCategoriesAsync(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT code, display_name, eligibility_description, version FROM cinema.ticket_category ORDER BY code;";
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<(string, string, string, long)>();
        while (await reader.ReadAsync())
        {
            list.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3)));
        }
        return list;
    }
}
