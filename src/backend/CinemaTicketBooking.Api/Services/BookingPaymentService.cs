using System.Globalization;
using System.Net;
using System.Text.Json;
using CinemaTicketBooking.Api.Data;
using CinemaTicketBooking.Api.DTOs.Checkout;
using CinemaTicketBooking.Api.DTOs.Payments;
using CinemaTicketBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CinemaTicketBooking.Api.Services;

public sealed class BookingPaymentService(
    DbContextOptions<ApplicationDbContext> options,
    MockPayClient mockPay,
    IOptions<MockPayOptions> mockPayOptions)
{
    private readonly MockPayOptions settings = mockPayOptions.Value;

    public async Task<(PaymentInitiationDto Result, int StatusCode, bool Replayed)> InitiateAsync(
        Guid owner, long bookingId, Guid requestKey, InitiatePaymentRequest request,
        string traceId, CancellationToken ct)
    {
        if (!string.Equals(request.Provider, settings.Provider, StringComparison.Ordinal))
            throw CheckoutException.Validation("provider", "unsupported", "Chỉ hỗ trợ provider MockPay.");

        long paymentId;
        bool replayed;
        await using (var db = new ApplicationDbContext(options))
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            await SelectionService.Lock(db, owner, ct);
            await SelectionService.Authority(db, owner, ct);
            var showtime = await db.Set<Booking>().Where(x => x.Id == bookingId && x.CheckoutIdentityId == owner)
                .Select(x => (long?)x.ShowtimeId).SingleOrDefaultAsync(ct)
                ?? throw new CheckoutException(404, "NOT_FOUND", "Không tìm thấy booking.");
            await SelectionService.Context(db, showtime, ct, reserve: true);
            var booking = await BookingRelease.LoadLocked(db, owner, bookingId, ct);
            var now = await SelectionService.Now(db, ct);

            if (booking.Status != "pending")
                throw NotEligible("booking_not_pending");
            if (booking.ReservationExpiresAt <= now)
                throw NotEligible("booking_expired");
            if (!BookingRelease.AllHeld(booking) || booking.TotalAmount <= 0 ||
                decimal.Truncate(booking.TotalAmount) != booking.TotalAmount)
                throw NotEligible("booking_not_payable");

            var scope = owner.ToString("D");
            var target = "bookings/" + bookingId.ToString(CultureInfo.InvariantCulture);
            var fingerprint = request.Provider;
            var prior = await ReadReceiptAsync(db, scope, target, requestKey, ct);
            if (prior is not null)
            {
                if (prior.Value.Fingerprint != fingerprint) throw CheckoutException.KeyConflict();
                if (prior.Value.State != "committed") throw CheckoutException.InProgress();
                paymentId = long.Parse(prior.Value.ResultReference, CultureInfo.InvariantCulture);
                replayed = true;
            }
            else
            {
                var existing = await ReadOpenPaymentAsync(db, bookingId, ct);
                if (existing is not null)
                {
                    if (!string.Equals(existing.Provider, settings.Provider, StringComparison.Ordinal))
                        throw new CheckoutException(409, "PAYMENT_NOT_ELIGIBLE", "Booking đã có payment provider khác.");
                    paymentId = existing.Id;
                }
                else
                {
                    paymentId = await InsertPaymentAsync(db, booking, requestKey, now, ct);
                }

                var operationId = Guid.NewGuid();
                await using var receipt = SelectionService.Command(db, """
                    INSERT INTO cinema.operation_receipt
                        (id,actor_scope,operation_name,target_scope,request_key,request_fingerprint,
                         processing_state,result_reference,created_at,committed_at)
                    VALUES (@id,@actor,'initiatePayment',@target,@key,@fingerprint,'committed',@result,@now,@now)
                    """);
                receipt.Parameters.AddWithValue("id", operationId);
                receipt.Parameters.AddWithValue("actor", scope);
                receipt.Parameters.AddWithValue("target", target);
                receipt.Parameters.AddWithValue("key", requestKey);
                receipt.Parameters.AddWithValue("fingerprint", fingerprint);
                receipt.Parameters.AddWithValue("result", paymentId.ToString(CultureInfo.InvariantCulture));
                receipt.Parameters.AddWithValue("now", now);
                await receipt.ExecuteNonQueryAsync(ct);
                replayed = false;
            }

            await tx.CommitAsync(ct);
        }

        var resolved = await ResolveProviderAsync(owner, paymentId, traceId, ct);
        return (resolved.Result, resolved.StatusCode, replayed);
    }

    public async Task<PaymentDto> ReadAsync(Guid owner, long paymentId, CancellationToken ct)
    {
        var local = await ReadPaymentAsync(owner, paymentId, ct)
            ?? throw new CheckoutException(404, "NOT_FOUND", "Không tìm thấy payment.");

        if (local.ProviderTransactionId is null)
        {
            var resolved = await ResolveProviderAsync(owner, paymentId, null, ct);
            local = await ReadPaymentAsync(owner, paymentId, ct)
                ?? throw new CheckoutException(404, "NOT_FOUND", "Không tìm thấy payment.");
            if (resolved.StatusCode == StatusCodes.Status202Accepted && local.ProviderTransactionId is null)
                return ToDto(local, DateTime.UtcNow);
        }

        if (local.ProviderTransactionId is null)
            return ToDto(local, DateTime.UtcNow);

        MockPayStatusResponse status;
        try
        {
            status = await mockPay.GetStatusAsync(local.ProviderTransactionId, ct);
        }
        catch (MockPayTransportException ex)
        {
            throw new CheckoutException(503, "DEPENDENCY_UNAVAILABLE", ex.Message);
        }
        catch (MockPayProviderException ex)
        {
            throw ProviderUnavailable(ex);
        }

        ValidateStatus(local, status);
        await ApplyStatusAsync(owner, paymentId, status, ct);
        local = await ReadPaymentAsync(owner, paymentId, ct)
            ?? throw new CheckoutException(404, "NOT_FOUND", "Không tìm thấy payment.");
        return ToDto(local, DateTime.UtcNow);
    }

    private async Task<(PaymentInitiationDto Result, int StatusCode)> ResolveProviderAsync(
        Guid owner, long paymentId, string? traceId, CancellationToken ct)
    {
        var local = await ReadPaymentAsync(owner, paymentId, ct)
            ?? throw new CheckoutException(404, "NOT_FOUND", "Không tìm thấy payment.");
        var booking = await ReadBookingInfoAsync(owner, local.BookingId, ct)
            ?? throw new CheckoutException(404, "NOT_FOUND", "Không tìm thấy booking.");
        var deadline = (local.InitiatedAt ?? local.CreatedAt).ToUniversalTime()
            .AddSeconds(settings.PaymentWindowSeconds);
        var metadata = new
        {
            movieTitle = booking.MovieTitle,
            cinemaName = booking.CinemaName,
            roomName = booking.RoomName,
            seats = booking.Seats
        };
        MockPayCreateResponse created;
        try
        {
            created = await mockPay.CreateAsync(new MockPayCreateRequest(
                booking.BookingCode.ToString("D"),
                checked((long)booking.Amount),
                booking.Currency,
                "Cinema ticket booking",
                deadline,
                metadata), local.MerchantRequestId, ct);
        }
        catch (MockPayTransportException)
        {
            await MarkUnknownAsync(paymentId, ct);
            var unknown = await ReadPaymentAsync(owner, paymentId, ct)
                ?? throw new CheckoutException(404, "NOT_FOUND", "Không tìm thấy payment.");
            return (new PaymentInitiationDto(ToDto(unknown, DateTime.UtcNow), null, "awaitOutcome"), StatusCodes.Status202Accepted);
        }
        catch (MockPayProviderException ex)
        {
            throw ProviderUnavailable(ex);
        }

        await MarkCreatedAsync(paymentId, created, ct);
        var updated = await ReadPaymentAsync(owner, paymentId, ct)
            ?? throw new CheckoutException(404, "NOT_FOUND", "Không tìm thấy payment.");
        return (new PaymentInitiationDto(ToDto(updated, DateTime.UtcNow), created.PaymentUrl, "redirect"), StatusCodes.Status201Created);
    }

    private async Task ApplyStatusAsync(Guid owner, long paymentId, MockPayStatusResponse status, CancellationToken ct)
    {
        await using var db = new ApplicationDbContext(options);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await SelectionService.Lock(db, owner, ct);
        await SelectionService.Authority(db, owner, ct);
        var local = await ReadPaymentAsync(db, owner, paymentId, ct)
            ?? throw new CheckoutException(404, "NOT_FOUND", "Không tìm thấy payment.");
        var now = await SelectionService.Now(db, ct);
        if (status.Status == "PENDING")
        {
            await using var pending = SelectionService.Command(db, """
                UPDATE cinema.payment SET status='pending',version=version+1
                WHERE id=@id AND booking_id=@booking AND applied_at IS NULL
                """);
            pending.Parameters.AddWithValue("id", paymentId);
            pending.Parameters.AddWithValue("booking", local.BookingId);
            await pending.ExecuteNonQueryAsync(ct);
        }
        else if (status.Status == "PAID")
        {
            if (status.PaidAt is null) throw new CheckoutException(502, "PAYMENT_PROVIDER_INVALID", "MockPay thiếu paidAt.");
            var late = local.PaymentDeadline is not null && status.PaidAt.Value.ToUniversalTime() > local.PaymentDeadline.Value.ToUniversalTime();
            await using var paid = SelectionService.Command(db, """
                UPDATE cinema.payment
                SET status='succeeded',verified_at=@now,paid_at=@paid,received_amount=@amount,
                    received_currency=@currency,reconciliation_status=CASE WHEN @late THEN 'needs_review' ELSE 'none' END,
                    version=version+1
                WHERE id=@id AND booking_id=@booking
                """);
            paid.Parameters.AddWithValue("now", now);
            paid.Parameters.AddWithValue("paid", status.PaidAt.Value.ToUniversalTime());
            paid.Parameters.AddWithValue("amount", status.Amount);
            paid.Parameters.AddWithValue("currency", status.Currency);
            paid.Parameters.AddWithValue("late", late);
            paid.Parameters.AddWithValue("id", paymentId);
            paid.Parameters.AddWithValue("booking", local.BookingId);
            await paid.ExecuteNonQueryAsync(ct);
            if (!late && local.Status != "succeeded")
                await ApplyBookingAsync(db, local.BookingId, paymentId, now, ct);
        }
        else
        {
            var mapped = status.Status == "CANCELLED" ? "cancelled" : "failed";
            await using var terminal = SelectionService.Command(db, """
                UPDATE cinema.payment
                SET status=@status,verified_at=@now,reconciliation_status='resolved',version=version+1
                WHERE id=@id AND booking_id=@booking AND applied_at IS NULL
                """);
            terminal.Parameters.AddWithValue("status", mapped);
            terminal.Parameters.AddWithValue("now", now);
            terminal.Parameters.AddWithValue("id", paymentId);
            terminal.Parameters.AddWithValue("booking", local.BookingId);
            await terminal.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    private static async Task ApplyBookingAsync(ApplicationDbContext db, long bookingId, long paymentId, DateTime now, CancellationToken ct)
    {
        await using var booking = SelectionService.Command(db, """
            UPDATE cinema.booking
            SET status='confirmed',confirmed_at=@now,version=version+1
            WHERE id=@booking AND status='pending'
            """);
        booking.Parameters.AddWithValue("now", now);
        booking.Parameters.AddWithValue("booking", bookingId);
        await booking.ExecuteNonQueryAsync(ct);
        await using var seats = SelectionService.Command(db, """
            UPDATE cinema.booking_seat SET allocation_status='sold',released_at=NULL
            WHERE booking_id=@booking AND allocation_status='held'
            """);
        seats.Parameters.AddWithValue("booking", bookingId);
        await seats.ExecuteNonQueryAsync(ct);
        await using var applied = SelectionService.Command(db, """
            UPDATE cinema.payment SET applied_at=@now,version=version+1
            WHERE id=@payment AND booking_id=@booking AND status='succeeded' AND applied_at IS NULL
            """);
        applied.Parameters.AddWithValue("now", now);
        applied.Parameters.AddWithValue("payment", paymentId);
        applied.Parameters.AddWithValue("booking", bookingId);
        await applied.ExecuteNonQueryAsync(ct);
    }

    private async Task MarkCreatedAsync(long paymentId, MockPayCreateResponse created, CancellationToken ct)
    {
        await using var db = new ApplicationDbContext(options);
        await using var command = db.Database.GetDbConnection().CreateCommand();
        await db.Database.OpenConnectionAsync(ct);
        command.CommandText = """
            UPDATE cinema.payment
            SET provider_transaction_id=@transaction,status='pending',initiated_at=@initiated,
                payment_deadline=@deadline,grace_until=@grace,version=version+1
            WHERE id=@id AND applied_at IS NULL
            """;
        Add(command, "transaction", created.PaymentId);
        Add(command, "initiated", created.CreatedAt.ToUniversalTime());
        Add(command, "deadline", created.ExpiresAt.ToUniversalTime());
        Add(command, "grace", created.ExpiresAt.ToUniversalTime().AddSeconds(settings.GraceSeconds));
        Add(command, "id", paymentId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task MarkUnknownAsync(long paymentId, CancellationToken ct)
    {
        await using var db = new ApplicationDbContext(options);
        await using var command = db.Database.GetDbConnection().CreateCommand();
        await db.Database.OpenConnectionAsync(ct);
        command.CommandText = "UPDATE cinema.payment SET status='unknown',next_reconcile_at=clock_timestamp()+interval '30 seconds',version=version+1 WHERE id=@id AND applied_at IS NULL";
        Add(command, "id", paymentId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<long> InsertPaymentAsync(ApplicationDbContext db, Booking booking, Guid requestKey, DateTime now, CancellationToken ct)
    {
        await using var command = SelectionService.Command(db, """
            INSERT INTO cinema.payment
                (booking_id,provider,merchant_request_id,amount,currency,status,created_at,
                 initiation_committed_at,payment_window_seconds,grace_seconds)
            VALUES (@booking,'MockPay',@request,@amount,@currency,'created',@now,@now,@window,@grace)
            RETURNING id
            """);
        command.Parameters.AddWithValue("booking", booking.Id);
        command.Parameters.AddWithValue("request", requestKey);
        command.Parameters.AddWithValue("amount", booking.TotalAmount);
        command.Parameters.AddWithValue("currency", booking.Currency);
        command.Parameters.AddWithValue("now", now);
        command.Parameters.AddWithValue("window", settings.PaymentWindowSeconds);
        command.Parameters.AddWithValue("grace", settings.GraceSeconds);
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    private async Task<PaymentRow?> ReadPaymentAsync(Guid owner, long paymentId, CancellationToken ct)
    {
        await using var db = new ApplicationDbContext(options);
        return await ReadPaymentAsync(db, owner, paymentId, ct);
    }

    private static async Task<PaymentRow?> ReadPaymentAsync(ApplicationDbContext db, Guid owner, long paymentId, CancellationToken ct)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        await db.Database.OpenConnectionAsync(ct);
        command.CommandText = """
            SELECT p.id,p.booking_id,p.provider,p.merchant_request_id,p.amount,p.currency,p.status,
                   p.applied_at,p.reconciliation_status,p.payment_deadline,p.grace_until,p.verified_at,
                   p.created_at,p.initiated_at,p.provider_transaction_id
            FROM cinema.payment p JOIN cinema.booking b ON b.id=p.booking_id
            WHERE p.id=@payment AND b.checkout_identity_id=@owner
            """;
        Add(command, "payment", paymentId);
        Add(command, "owner", owner);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadRow(reader) : null;
    }

    private static async Task<OpenPayment?> ReadOpenPaymentAsync(ApplicationDbContext db, long bookingId, CancellationToken ct)
    {
        await using var command = SelectionService.Command(db, """
            SELECT id,provider,merchant_request_id FROM cinema.payment
            WHERE booking_id=@booking AND status IN ('created','pending','unknown')
            ORDER BY id DESC LIMIT 1 FOR UPDATE
            """);
        command.Parameters.AddWithValue("booking", bookingId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? new OpenPayment(reader.GetInt64(0), reader.GetString(1), reader.GetGuid(2))
            : null;
    }

    private static async Task<(string Fingerprint, string State, string ResultReference)?> ReadReceiptAsync(
        ApplicationDbContext db, string actor, string target, Guid key, CancellationToken ct)
    {
        await using var command = SelectionService.Command(db, """
            SELECT request_fingerprint,processing_state,result_reference
            FROM cinema.operation_receipt
            WHERE actor_scope=@actor AND operation_name='initiatePayment' AND target_scope=@target AND request_key=@key
            """);
        command.Parameters.AddWithValue("actor", actor);
        command.Parameters.AddWithValue("target", target);
        command.Parameters.AddWithValue("key", key);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? (reader.GetString(0), reader.GetString(1), reader.GetString(2))
            : null;
    }

    private async Task<BookingInfo?> ReadBookingInfoAsync(Guid owner, long bookingId, CancellationToken ct)
    {
        await using var db = new ApplicationDbContext(options);
        await using var command = db.Database.GetDbConnection().CreateCommand();
        await db.Database.OpenConnectionAsync(ct);
        command.CommandText = """
            SELECT b.id,b.booking_code,b.total_amount,b.currency,b.created_at,b.movie_title_snapshot,
                   b.room_name_snapshot,c.name
            FROM cinema.booking b
            JOIN cinema.cinema_room r ON r.id=b.cinema_room_id
            JOIN cinema.cinema c ON c.id=r.cinema_id
            WHERE b.id=@booking AND b.checkout_identity_id=@owner
            """;
        Add(command, "booking", bookingId);
        Add(command, "owner", owner);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        var info = new BookingInfo(reader.GetInt64(0), reader.GetGuid(1), reader.GetDecimal(2), reader.GetString(3),
            reader.GetDateTime(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), []);
        await reader.DisposeAsync();
        await using var seats = db.Database.GetDbConnection().CreateCommand();
        seats.CommandText = "SELECT seat_label_snapshot FROM cinema.booking_seat WHERE booking_id=@booking ORDER BY seat_id";
        Add(seats, "booking", bookingId);
        await using var seatReader = await seats.ExecuteReaderAsync(ct);
        var labels = new List<string>();
        while (await seatReader.ReadAsync(ct)) labels.Add(seatReader.GetString(0));
        return info with { Seats = labels.ToArray() };
    }

    private static void ValidateStatus(PaymentRow local, MockPayStatusResponse status)
    {
        if (status.Status is not ("PENDING" or "PAID" or "EXPIRED" or "CANCELLED"))
            throw new CheckoutException(502, "PAYMENT_PROVIDER_INVALID", "MockPay trả về trạng thái không hợp lệ.");
        if (status.PaymentId != local.ProviderTransactionId ||
            status.Amount != checked((long)local.Amount) ||
            status.Currency != local.Currency)
            throw new CheckoutException(502, "PAYMENT_PROVIDER_INVALID", "Thông tin payment từ MockPay không khớp booking.");
    }

    private static CheckoutException ProviderUnavailable(MockPayProviderException exception)
        => new((int)exception.StatusCode is >= 400 and < 500 ? 409 : 503,
            "PAYMENT_PROVIDER_ERROR", "MockPay không thể hoàn tất yêu cầu.");

    private static CheckoutException NotEligible(string reason)
        => new(422, "PAYMENT_NOT_ELIGIBLE", "Booking chưa đủ điều kiện thanh toán.", new { reason });

    private static PaymentRow ReadRow(System.Data.Common.DbDataReader reader)
        => new(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3),
            reader.GetDecimal(4), reader.GetString(5), reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetDateTime(7), reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetDateTime(9), reader.IsDBNull(10) ? null : reader.GetDateTime(10),
            reader.IsDBNull(11) ? null : reader.GetDateTime(11), reader.GetDateTime(12),
            reader.IsDBNull(13) ? null : reader.GetDateTime(13),
            reader.IsDBNull(14) ? null : reader.GetString(14));

    private static PaymentDto ToDto(PaymentRow row, DateTime now)
        => new(row.Id.ToString(CultureInfo.InvariantCulture), row.BookingId.ToString(CultureInfo.InvariantCulture),
            row.Provider, row.Status, row.AppliedAt, row.ReconciliationStatus,
            new(row.Amount, row.Currency), row.PaymentDeadline, row.GraceUntil, row.VerifiedAt, now);

    private static void Add(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private sealed record PaymentRow(
        long Id, long BookingId, string Provider, Guid MerchantRequestId, decimal Amount, string Currency,
        string Status, DateTime? AppliedAt, string ReconciliationStatus, DateTime? PaymentDeadline,
        DateTime? GraceUntil, DateTime? VerifiedAt, DateTime CreatedAt, DateTime? InitiatedAt,
        string? ProviderTransactionId);

    private sealed record OpenPayment(long Id, string Provider, Guid MerchantRequestId);

    private sealed record BookingInfo(
        long Id, Guid BookingCode, decimal Amount, string Currency, DateTime CreatedAt,
        string MovieTitle, string RoomName, string CinemaName, string[] Seats);
}
