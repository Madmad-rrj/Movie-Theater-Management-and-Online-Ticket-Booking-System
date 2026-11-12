using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CinemaTicketBooking.Api.Data;
using CinemaTicketBooking.Api.DTOs.Checkout;
using CinemaTicketBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CinemaTicketBooking.Api.Services;

public sealed class SelectionService(DbContextOptions<ApplicationDbContext> options, IOptions<SelectionOptions> selectionOptions)
{
    private int MaximumTickets => selectionOptions.Value.MaximumTickets;
    private static CheckoutException NotFound() => new(404, "NOT_FOUND", "Không tìm thấy lựa chọn.");
    private static CheckoutException StateConflict() => new(409, "STATE_CONFLICT", "Lựa chọn không còn chỉnh sửa được.", new { reason = "selection_not_active" });
    internal static long Number(string value, string field)
    {
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
            throw CheckoutException.Validation(field, "invalid_format", "ID phải là chuỗi số nguyên dương.");
        return id;
    }
    internal static long[] Validate(Quantities quantities, string[] seats, bool create, int maximum = int.MaxValue)
    {
        if (quantities.Adult is null || quantities.Discounted is null || quantities.Adult < 0 || quantities.Discounted < 0 ||
            (long)quantities.Adult + quantities.Discounted < 1 || (long)quantities.Adult + quantities.Discounted > maximum)
            throw CheckoutException.Validation("quantities", "out_of_range", $"Tổng số vé phải từ 1 đến {maximum}.");
        if (seats.Length > quantities.Adult + quantities.Discounted || (create && seats.Length == 0))
            throw CheckoutException.Validation("seatIds", "invalid_count", "Số ghế không phù hợp số vé.");
        var ids = seats.Select(s => Number(s, "seatIds")).Order().ToArray();
        if (ids.Distinct().Count() != ids.Length)
            throw CheckoutException.Validation("seatIds", "duplicate", "Không được chọn ghế trùng.");
        return ids;
    }
    public Task<SelectionOutcome> CreateAsync(Guid owner, Guid key, CreateSelectionRequest request, CancellationToken ct)
    {
        var showtime = Number(request.ShowtimeId, "showtimeId");
        var seats = Validate(request.Quantities, request.SeatIds, true, MaximumTickets);
        var fingerprint = Hash(JsonSerializer.Serialize(new { showtime, seats, request.Quantities }));
        return Transaction(async db =>
        {
            var scope = owner.ToString("D");
            // One actor-level lock serializes active-selection creation across different keys.
            await Lock(db, owner, ct);
            await Authority(db, owner, ct);
            var receipt = await db.OperationReceipts.SingleOrDefaultAsync(r => r.ActorScope == scope &&
                r.OperationName == "createSelection" && r.TargetScope == "selections" && r.RequestKey == key, ct);
            if (receipt is not null)
            {
                if (receipt.RequestFingerprint != fingerprint) throw CheckoutException.KeyConflict();
                if (receipt.ProcessingState != "committed") throw CheckoutException.InProgress();
                await LockSelectionContext(db, owner, Guid.Parse(receipt.ResultReference!), ct);
                var existing = await Owned(db, owner, Guid.Parse(receipt.ResultReference!), ct);
                return new SelectionOutcome(await Project(db, existing, ct), Replayed: true);
            }
            await Context(db, showtime, ct);
            var now = await Now(db, ct);
            var active = await db.Set<SelectionSession>().SingleOrDefaultAsync(s =>
                s.CheckoutIdentityId == owner && s.ShowtimeId == showtime && s.State == "active", ct);
            if (active is not null && active.ExpiresAt <= now)
            {
                active.State = "expired"; active.Version++;
                await db.SaveChangesAsync(ct); // Close the partial-unique-index entry before insertion.
                active = null;
            }
            bool created = active is null;
            if (active is null)
            {
                var context = await Context(db, showtime, ct);
                CheckBookable(context, now);
                await SeatData(db, context.RoomId, seats, ct);
                active = new SelectionSession
                {
                    Id = Guid.NewGuid(),
                    CheckoutIdentityId = owner,
                    ShowtimeId = showtime,
                    CinemaRoomId = context.RoomId,
                    StartedAt = now,
                    ExpiresAt = now.AddMinutes(5),
                    AdultQuantity = request.Quantities.Adult!.Value,
                    DiscountedQuantity = request.Quantities.Discounted!.Value,
                    MaximumTicketsSnapshot = MaximumTickets
                };
                db.Add(active);
                foreach (var seat in seats) db.Add(new SelectionSeat { SelectionId = active.Id, SeatId = seat, CinemaRoomId = context.RoomId });
                await db.SaveChangesAsync(ct);
            }
            var dto = await Project(db, active, ct);
            db.OperationReceipts.Add(new OperationReceipt
            {
                Id = Guid.NewGuid(),
                ActorScope = scope,
                OperationName = "createSelection",
                TargetScope = "selections",
                RequestKey = key,
                RequestFingerprint = fingerprint,
                ProcessingState = "committed",
                ResultReference = active.Id.ToString("D"),
                CreatedAt = now,
                CommittedAt = await Now(db, ct)
            });
            return new SelectionOutcome(dto, created);
        }, ct);
    }
    public Task<SelectionDto> ReadAsync(Guid owner, Guid id, CancellationToken ct) => Transaction(async db =>
    {
        await Lock(db, owner, ct); await Authority(db, owner, ct);
        await LockSelectionContext(db, owner, id, ct);
        return await Project(db, await Owned(db, owner, id, ct), ct);
    }, ct);

    public Task<SelectionDto> UpdateAsync(Guid owner, Guid id, UpdateSelectionRequest request, CancellationToken ct)
    {
        var seats = Validate(request.Quantities, request.SeatIds, false);
        return Transaction(async db =>
        {
            await Lock(db, owner, ct); await Authority(db, owner, ct);
            await LockSelectionContext(db, owner, id, ct);
            var session = await Owned(db, owner, id, ct);
            if (session.State is not ("active" or "expired")) throw StateConflict();
            if (session.State == "expired" || session.ExpiresAt <= await Now(db, ct))
            {
                if (session.State == "active") { session.State = "expired"; session.Version++; }
                throw new CheckoutException(422, "SELECTION_EXPIRED", "Lựa chọn đã hết hạn.", new { selectionDeadline = session.ExpiresAt });
            }
            if (session.State != "active") throw StateConflict();
            if (request.Version != session.Version.ToString(CultureInfo.InvariantCulture))
                throw new CheckoutException(409, "VERSION_CONFLICT", "Phiên bản lựa chọn đã thay đổi.", new { currentVersion = session.Version.ToString(CultureInfo.InvariantCulture) });
            Validate(request.Quantities, request.SeatIds, false, session.MaximumTicketsSnapshot);
            await db.Set<SelectionSeat>().Where(s => s.SelectionId == id).ExecuteDeleteAsync(ct);
            foreach (var seat in seats) db.Add(new SelectionSeat { SelectionId = id, SeatId = seat, CinemaRoomId = session.CinemaRoomId });
            session.AdultQuantity = request.Quantities.Adult!.Value; session.DiscountedQuantity = request.Quantities.Discounted!.Value;
            session.Version++; session.AcceptedQuoteFingerprint = null;
            // Validate physical seats before flushing their composite foreign keys.
            await SeatData(db, session.CinemaRoomId, seats, ct);
            await db.SaveChangesAsync(ct);
            return await Project(db, session, ct);
        }, ct);
    }
    internal static async Task<SelectionSession> Owned(ApplicationDbContext db, Guid owner, Guid id, CancellationToken ct)
        => (await db.Set<SelectionSession>().FromSqlInterpolated(
            $"SELECT * FROM cinema.selection_session WHERE id={id} AND checkout_identity_id={owner} FOR UPDATE")
            .ToListAsync(ct)).SingleOrDefault() ?? throw NotFound();
    private static async Task LockSelectionContext(ApplicationDbContext db, Guid owner, Guid id, CancellationToken ct)
    {
        var showtime = await db.Set<SelectionSession>().Where(s => s.Id == id && s.CheckoutIdentityId == owner)
            .Select(s => (long?)s.ShowtimeId).SingleOrDefaultAsync(ct) ?? throw NotFound();
        await Context(db, showtime, ct);
    }
    internal static Task<DateTime> Now(ApplicationDbContext db, CancellationToken ct)
        => db.Database.SqlQuery<DateTime>($"SELECT clock_timestamp() AS \"Value\"").SingleAsync(ct);
    internal static async Task Lock(ApplicationDbContext db, Guid owner, CancellationToken ct)
    {
        var key = BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes("selection:" + owner)));
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", ct);
    }
    internal static async Task Authority(ApplicationDbContext db, Guid owner, CancellationToken ct)
    {
        var rows = await db.CheckoutIdentities.FromSqlInterpolated($"SELECT * FROM cinema.checkout_identity WHERE id={owner} FOR SHARE").AsNoTracking().ToListAsync(ct);
        var guest = rows.SingleOrDefault();
        if (guest is null || guest.UserId is not null || guest.RevokedAt is not null || guest.GuestExpiresAt <= await Now(db, ct))
            throw CheckoutException.Unauthenticated();
    }
    private async Task<T> Transaction<T>(Func<ApplicationDbContext, Task<T>> operation, CancellationToken ct)
    {
        await using var strategy = new ApplicationDbContext(options);
        return await strategy.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = new ApplicationDbContext(options);
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation(db);
                await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return result;
            }
            catch (CheckoutException ex) when (ex.Code == "SELECTION_EXPIRED")
            {
                await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); throw;
            }
        });
    }
    internal static async Task<SelectionDto> Project(ApplicationDbContext db, SelectionSession s, CancellationToken ct)
    {
        var now = await Now(db, ct);
        if (s.State == "abandoned") throw StateConflict();
        if (s.State == "active" && s.ExpiresAt <= now) { s.State = "expired"; s.Version++; }
        var ids = await db.Set<SelectionSeat>().Where(x => x.SelectionId == s.Id).OrderBy(x => x.SeatId).Select(x => x.SeatId).ToArrayAsync(ct);
        SelectionQuote? quote = null;
        if (s.State == "active")
        {
            var context = await Context(db, s.ShowtimeId, ct); CheckBookable(context, now);
            if (context.RoomId != s.CinemaRoomId) throw StateConflict();
            var seats = await SeatData(db, s.CinemaRoomId, ids, ct);
            var prices = await Prices(db, context.CinemaId, ct);
            var total = prices.Adult * s.AdultQuantity + prices.Discounted * s.DiscountedQuantity + seats.Sum(x => x.Surcharge.Amount);
            if (total > 99999999999999m)
                throw new CheckoutException(422, "BUSINESS_RULE_VIOLATION", "Tổng tiền vượt giới hạn lưu trữ.",
                    new { violations = new[] { new { field = "pricing", reason = "out_of_range", message = "Tổng tiền vượt giới hạn lưu trữ." } } });
            var fingerprint = Hash(JsonSerializer.Serialize(new
            {
                s.Id,
                s.AdultQuantity,
                s.DiscountedQuantity,
                context.Start,
                prices,
                seats
            }));
            if (s.AcceptedQuoteFingerprint is not null && s.AcceptedQuoteFingerprint != fingerprint) s.Version++;
            s.AcceptedQuoteFingerprint = fingerprint; s.QuotedAdultPrice = prices.Adult;
            s.QuotedDiscountedPrice = prices.Discounted; s.QuotedTotal = total; s.QuotedShowtimeStart = context.Start;
            quote = new SelectionQuote(new Guid(Convert.FromHexString(fingerprint)[..16]), fingerprint,
                new(s.AdultQuantity, s.DiscountedQuantity), new(prices.Adult), new(prices.Discounted), seats, new(total), now);
        }
        now = await Now(db, ct);
        if (s.State == "active" && s.ExpiresAt <= now) { s.State = "expired"; s.Version++; quote = null; }
        string? bookingId = null;
        if (s.State == "consumed")
        {
            await using var command = Command(db, "SELECT id FROM cinema.booking WHERE selection_session_id=@id");
            command.Parameters.AddWithValue("id", s.Id);
            bookingId = (await command.ExecuteScalarAsync(ct))?.ToString();
        }
        return new(s.Id, s.ShowtimeId.ToString(CultureInfo.InvariantCulture), new(s.AdultQuantity, s.DiscountedQuantity),
            ids.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray(), quote, s.ExpiresAt, now,
            s.State == "consumed" ? "reserved" : s.State, bookingId, s.Version.ToString(CultureInfo.InvariantCulture));
    }
    // Read-only projections avoid introducing duplicate BE1 entity mappings.
    internal sealed record ShowtimeContext(long RoomId, long CinemaId, DateTime Start, bool Eligible,
        long ShowtimeId, long MovieId, string MovieTitle, string CinemaName, string RoomName, string Timezone,
        DateTime End, string Status, long Version)
    {
        public BookingShowtime ToDto(DateTime now) => new(ShowtimeId.ToString(CultureInfo.InvariantCulture),
            MovieId.ToString(CultureInfo.InvariantCulture), MovieTitle, CinemaId.ToString(CultureInfo.InvariantCulture),
            CinemaName, RoomId.ToString(CultureInfo.InvariantCulture), RoomName, Start, End, Timezone, Status,
            Eligible && Start > now, now, Version.ToString(CultureInfo.InvariantCulture));
    }
    internal static NpgsqlCommand Command(ApplicationDbContext db, string sql) => new(sql,
        (NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction)db.Database.CurrentTransaction!.GetDbTransaction());
    internal static async Task<ShowtimeContext> Context(ApplicationDbContext db, long id, CancellationToken ct, bool reserve = false)
    {
        // Ancestors are locked before showtime. All selection/reservation paths use
        // the same actor lock first, then Guest -> Cinema -> Room -> Showtime.
        long cinema, room;
        await using (var peek = Command(db, "SELECT r.cinema_id,s.cinema_room_id FROM cinema.showtime s JOIN cinema.cinema_room r ON r.id=s.cinema_room_id WHERE s.id=@id"))
        {
            peek.Parameters.AddWithValue("id", id);
            await using var reader = await peek.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) throw new CheckoutException(422, "SHOWTIME_NOT_BOOKABLE", "Suất không khả dụng.", new { reason = "not_found" });
            cinema = reader.GetInt64(0); room = reader.GetInt64(1);
        }
        await using (var ancestor = Command(db, "SELECT id FROM cinema.cinema WHERE id=@id FOR SHARE"))
        {
            ancestor.Parameters.AddWithValue("id", cinema); await ancestor.ExecuteScalarAsync(ct);
        }
        await using (var ancestor = Command(db, "SELECT cinema_id FROM cinema.cinema_room WHERE id=@id FOR SHARE"))
        {
            ancestor.Parameters.AddWithValue("id", room);
            if (!Equals(await ancestor.ExecuteScalarAsync(ct), cinema)) throw StateConflict();
        }
        await using (var showtime = Command(db, "SELECT cinema_room_id FROM cinema.showtime WHERE id=@id FOR " + (reserve ? "UPDATE" : "SHARE")))
        {
            showtime.Parameters.AddWithValue("id", id);
            if (!Equals(await showtime.ExecuteScalarAsync(ct), room)) throw StateConflict();
        }
        await using var command = Command(db, """
            SELECT s.cinema_room_id,c.id,s.starts_at,
                s.status='scheduled' AND c.status='active' AND r.status='active' AND m.status IN ('coming_soon','now_showing'),
                s.id,m.id,m.title,c.name,r.name,c.timezone,s.ends_at,s.status,s.version
            FROM cinema.showtime s JOIN cinema.cinema_room r ON r.id=s.cinema_room_id
            JOIN cinema.cinema c ON c.id=r.cinema_id JOIN cinema.movie m ON m.id=s.movie_id
            WHERE s.id=@id FOR SHARE OF m
            """);
        command.Parameters.AddWithValue("id", id);
        await using var data = await command.ExecuteReaderAsync(ct);
        if (!await data.ReadAsync(ct)) throw StateConflict();
        return new(data.GetInt64(0), data.GetInt64(1), data.GetDateTime(2), data.GetBoolean(3),
            data.GetInt64(4), data.GetInt64(5), data.GetString(6), data.GetString(7), data.GetString(8),
            data.GetString(9), data.GetDateTime(10), data.GetString(11), data.GetInt64(12));
    }
    internal static void CheckBookable(ShowtimeContext context, DateTime now)
    {
        if (!context.Eligible || context.Start <= now)
            throw new CheckoutException(422, "SHOWTIME_NOT_BOOKABLE", "Suất không còn cho phép đặt.", new { reason = "not_bookable" });
    }
    internal static async Task<List<SelectionSeatSnapshot>> SeatData(ApplicationDbContext db, long room, long[] ids, CancellationToken ct)
    {
        await using var command = Command(db, """
            SELECT se.id,se.row_label,se.seat_number,st.name,st.surcharge_amount,
                se.operational_state='active' AND se.retired_at IS NULL AND st.is_active
            FROM cinema.seat se JOIN cinema.seat_type st ON st.id=se.seat_type_id
            WHERE se.id=ANY(@ids) AND se.cinema_room_id=@room ORDER BY se.id FOR SHARE OF se,st
            """);
        command.Parameters.AddWithValue("ids", ids); command.Parameters.AddWithValue("room", room);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<SelectionSeatSnapshot>();
        while (await reader.ReadAsync(ct))
        {
            if (!reader.GetBoolean(5)) throw CheckoutException.Validation("seatIds", "unavailable", "Ghế đã ngừng sử dụng.");
            result.Add(new(reader.GetInt64(0).ToString(CultureInfo.InvariantCulture), reader.GetString(1) + reader.GetInt16(2), reader.GetString(3), new(reader.GetDecimal(4))));
        }
        if (result.Count != ids.Length) throw CheckoutException.Validation("seatIds", "wrong_room", "Ghế không thuộc phòng của suất.");
        return result;
    }
    private sealed record CinemaPrices(decimal Adult, decimal Discounted, long AdultVersion, long DiscountedVersion);
    private static async Task<CinemaPrices> Prices(ApplicationDbContext db, long cinema, CancellationToken ct)
    {
        await using var command = Command(db, "SELECT category_code,amount,version FROM cinema.cinema_ticket_price WHERE cinema_id=@id ORDER BY category_code FOR SHARE");
        command.Parameters.AddWithValue("id", cinema);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var prices = new Dictionary<string, (decimal Amount, long Version)>();
        while (await reader.ReadAsync(ct)) prices.Add(reader.GetString(0), (reader.GetDecimal(1), reader.GetInt64(2)));
        if (!prices.ContainsKey("Adult") || !prices.ContainsKey("Discounted"))
            throw new CheckoutException(422, "BUSINESS_RULE_VIOLATION", "Thiếu cấu hình giá.", new { violations = new[] { new { field = "pricing", reason = "missing", message = "Thiếu cấu hình giá." } } });
        return new(prices["Adult"].Amount, prices["Discounted"].Amount, prices["Adult"].Version, prices["Discounted"].Version);
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
