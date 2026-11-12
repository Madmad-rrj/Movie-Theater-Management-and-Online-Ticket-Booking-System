using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CinemaTicketBooking.Api.Data;
using CinemaTicketBooking.Api.DTOs.Checkout;
using CinemaTicketBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using static CinemaTicketBooking.Api.Services.SelectionService;

namespace CinemaTicketBooking.Api.Services;

public sealed class BookingReservationService(DbContextOptions<ApplicationDbContext> options)
{
    private static CheckoutException Conflict(string reason) => new(409, "STATE_CONFLICT", "Không thể đặt lựa chọn ở trạng thái hiện tại.", new { reason });
    private static CheckoutException Expired(SelectionSession selection) => new(422, "SELECTION_EXPIRED", "Lựa chọn đã hết hạn.", new { selectionDeadline = selection.ExpiresAt });
    private static CheckoutException SeatConflict(IEnumerable<long> ids) => new(409, "SEAT_CONFLICT", "Một hoặc nhiều ghế đã có người giữ hoặc mua.",
        new { seatIds = ids.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray() });

    public async Task<ReservationOutcome> ReserveAsync(Guid owner, Guid requestKey, ReserveRequest request, string traceId, CancellationToken ct)
    {
        var showtimeId = Number(request.ShowtimeId, "showtimeId");
        var seats = Validate(request.Quantities, request.SeatIds, true);
        ValidateContact(request);
        if (request.SelectionId is null || request.SelectionId == Guid.Empty || request.PriceAcceptance.QuoteId is null || request.PriceAcceptance.QuoteId == Guid.Empty)
            throw CheckoutException.Validation("selectionId/priceAcceptance.quoteId", "invalid_format", "ID phải là UUID hợp lệ.");
        var contact = new BookingContact(request.Contact.FullName.Trim(), request.Contact.Phone.Trim(), request.Contact.Email.Trim());
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            selectionId = request.SelectionId,
            request.SelectionVersion,
            showtimeId,
            seats,
            request.Quantities,
            contact,
            request.Declarations,
            request.PriceAcceptance
        }))));
        var scope = owner.ToString("D");
        var operationId = Guid.NewGuid();
        var bookingCode = Guid.NewGuid();
        await using var strategy = new ApplicationDbContext(options);
        return await strategy.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = new ApplicationDbContext(options);
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await Lock(db, owner, ct);
            await Authority(db, owner, ct);
            var prior = await db.OperationReceipts.SingleOrDefaultAsync(x => x.ActorScope == scope &&
                x.OperationName == "reserveSeats" && x.TargetScope == "bookings" && x.RequestKey == requestKey, ct);
            if (prior is not null)
            {
                if (prior.RequestFingerprint != fingerprint) throw CheckoutException.KeyConflict();
                var replayContext = await Context(db, showtimeId, ct, reserve: true);
                if (prior.ProcessingState == "rejected")
                {
                    await Owned(db, owner, request.SelectionId.Value, ct);
                    var error = JsonSerializer.Deserialize<RecordedFailure>(prior.ResultReference!, ReceiptJson)!;
                    throw new CheckoutException(error.Status, error.Code, error.Message, error.Details) { Replayed = true };
                }
                if (prior.ProcessingState != "committed") throw CheckoutException.InProgress();
                var id = long.Parse(prior.ResultReference!, CultureInfo.InvariantCulture);
                await using (var locked = Command(db, "SELECT id FROM cinema.booking WHERE id=@id AND checkout_identity_id=@owner FOR UPDATE"))
                {
                    locked.Parameters.AddWithValue("id", id); locked.Parameters.AddWithValue("owner", owner);
                    if (await locked.ExecuteScalarAsync(ct) is null) throw new CheckoutException(404, "NOT_FOUND", "Không tìm thấy booking.");
                }
                var replay = await ReadResult(db, owner, id, replayContext, ct);
                await Authority(db, owner, ct);
                await tx.CommitAsync(ct);
                return new ReservationOutcome(replay, Replayed: true);
            }
            // Context locks ancestors, then showtime; selection and booking follow.
            var context = await Context(db, showtimeId, ct, reserve: true);
            var selection = await Owned(db, owner, request.SelectionId.Value, ct);
            bool writing = false;
            try
            {
                if (selection.State is "consumed" or "abandoned") throw Conflict("selection_consumed_or_abandoned");
                var now = await Now(db, ct);
                if (selection.State == "expired" || selection.ExpiresAt <= now)
                {
                    if (selection.State == "active") { selection.State = "expired"; selection.Version++; }
                    throw Expired(selection);
                }
                if (selection.State != "active") throw Conflict("selection_not_active");
                if (request.SelectionVersion != selection.Version.ToString(CultureInfo.InvariantCulture))
                    throw new CheckoutException(409, "VERSION_CONFLICT", "Phiên bản lựa chọn đã thay đổi.", new { currentVersion = selection.Version.ToString(CultureInfo.InvariantCulture) });
                var selected = await db.Set<SelectionSeat>().Where(x => x.SelectionId == selection.Id).OrderBy(x => x.SeatId).Select(x => x.SeatId).ToArrayAsync(ct);
                if (selection.ShowtimeId != showtimeId || selection.CinemaRoomId != context.RoomId || !selected.SequenceEqual(seats) ||
                    selection.AdultQuantity != request.Quantities.Adult || selection.DiscountedQuantity != request.Quantities.Discounted)
                    throw Conflict("request_does_not_match_selection");
                Validate(request.Quantities, request.SeatIds, true, selection.MaximumTicketsSnapshot);
                if (seats.Length != (long)selection.AdultQuantity + selection.DiscountedQuantity)
                    throw new CheckoutException(422, "BUSINESS_RULE_VIOLATION", "Phải chọn đủ ghế cho số vé.", new
                    { violations = new[] { new { field = "seatIds", reason = "incomplete", message = "Số ghế phải bằng tổng số vé." } } });
                CheckBookable(context, now);
                var projection = await Project(db, selection, ct);
                if (projection.State != "active") throw Expired(selection);
                var quote = projection.Quote!;
                var acceptance = request.PriceAcceptance;
                if (acceptance.QuoteId != quote.QuoteId || acceptance.Version != quote.Version || (acceptance.AcceptedTotal.Amount != quote.Total.Amount || acceptance.AcceptedTotal.Currency != quote.Total.Currency))
                    throw new CheckoutException(422, "PRICE_CHANGED", "Giá hoặc thông tin suất đã thay đổi; vui lòng xác nhận lại.",
                        new { quote, selectionVersion = projection.Version });
                var conflicts = await ConflictingSeats(db, showtimeId, seats, ct);
                if (conflicts.Length > 0) throw SeatConflict(conflicts);
                var names = await CategoryNames(db, ct);
                // Preserve any refreshed quotation before the savepoint. No booking
                // or allocation may survive rollback to this point.
                await db.SaveChangesAsync(ct);
                await tx.CreateSavepointAsync("before_reservation", ct);
                writing = true;
                now = await Now(db, ct);
                if (selection.ExpiresAt <= now) throw Expired(selection);
                var booking = new Booking
                {
                    BookingCode = bookingCode,
                    CheckoutIdentityId = owner,
                    SelectionSessionId = selection.Id,
                    ShowtimeId = showtimeId,
                    CinemaRoomId = selection.CinemaRoomId,
                    ContactFullName = contact.FullName,
                    ContactPhone = contact.Phone,
                    ContactEmail = contact.Email,
                    TermsAcceptedAt = now,
                    AgeAcknowledgedAt = now,
                    PurchasedStartAt = context.Start,
                    MovieTitleSnapshot = context.MovieTitle,
                    RoomNameSnapshot = context.RoomName,
                    ReservationExpiresAt = selection.ExpiresAt,
                    MaximumTicketsSnapshot = selection.MaximumTicketsSnapshot,
                    TotalAmount = quote.Total.Amount,
                    CreatedAt = now,
                    Categories = [
                        new BookingCategory { CategoryCode = "Adult", Quantity = selection.AdultQuantity, BasePriceSnapshot = quote.AdultBasePrice.Amount, CategoryNameSnapshot = names["Adult"] },
                        new BookingCategory { CategoryCode = "Discounted", Quantity = selection.DiscountedQuantity, BasePriceSnapshot = quote.DiscountedBasePrice.Amount, CategoryNameSnapshot = names["Discounted"] }
                    ],
                    Seats = quote.Seats.Select(x => new BookingSeat
                    {
                        ShowtimeId = showtimeId,
                        CinemaRoomId = selection.CinemaRoomId,
                        SeatId = Number(x.SeatId, "seatIds"),
                        SeatLabelSnapshot = x.Label,
                        SeatTypeNameSnapshot = x.SeatTypeName,
                        SurchargeSnapshot = x.Surcharge.Amount
                    }).ToList()
                };
                selection.State = "consumed"; selection.Version++;
                db.Add(booking);
                try { await db.SaveChangesAsync(ct); }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505", ConstraintName: "ux_active_seat" })
                { throw SeatConflict(seats); }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505", ConstraintName: "booking_selection_session_id_key" })
                { throw Conflict("selection_already_reserved"); }
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO cinema.audit_event(operation_identity,event_type,actor_kind,actor_scope,resource_type,resource_id,occurred_at,correlation_id)
                    VALUES ({operationId},'booking_reserved','guest',{owner.ToString("D")},'booking',{booking.Id.ToString(CultureInfo.InvariantCulture)},{now},{traceId})
                    """, ct);
                var result = await ReadResult(db, owner, booking.Id, context, ct);
                await Authority(db, owner, ct);
                now = await Now(db, ct);
                if (selection.ExpiresAt <= now) throw Expired(selection);
                CheckBookable(context, now);
                db.OperationReceipts.Add(new OperationReceipt
                {
                    Id = operationId,
                    ActorScope = scope,
                    OperationName = "reserveSeats",
                    TargetScope = "bookings",
                    RequestKey = requestKey,
                    RequestFingerprint = fingerprint,
                    ProcessingState = "committed",
                    ResultReference = booking.Id.ToString(CultureInfo.InvariantCulture),
                    CreatedAt = now,
                    CommittedAt = now
                });
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return new ReservationOutcome(result);
            }
            catch (CheckoutException ex) when (ex.StatusCode is 409 or 422)
            {
                if (writing)
                {
                    await tx.RollbackToSavepointAsync("before_reservation", ct);
                    db.ChangeTracker.Clear();
                    if (ex.Code == "SELECTION_EXPIRED")
                    {
                        selection = await Owned(db, owner, request.SelectionId.Value, ct);
                        if (selection.State == "active") { selection.State = "expired"; selection.Version++; }
                    }
                }
                var rejectedAt = await Now(db, ct);
                db.OperationReceipts.Add(new OperationReceipt
                {
                    Id = operationId,
                    ActorScope = scope,
                    OperationName = "reserveSeats",
                    TargetScope = "bookings",
                    RequestKey = requestKey,
                    RequestFingerprint = fingerprint,
                    ProcessingState = "rejected",
                    ResultReference = JsonSerializer.Serialize(new RecordedFailure(ex.StatusCode, ex.Code, ex.Message,
                        JsonSerializer.SerializeToElement(ex.Details, ReceiptJson)), ReceiptJson),
                    CreatedAt = rejectedAt
                });
                await Authority(db, owner, ct);
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                throw;
            }
        });
    }

    private static readonly JsonSerializerOptions ReceiptJson = new(JsonSerializerDefaults.Web);
    private sealed record RecordedFailure(int Status, string Code, string Message, JsonElement Details);

    private static void ValidateContact(ReserveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Contact.FullName) || request.Contact.FullName.Trim().Length > 150)
            throw CheckoutException.Validation("contact.fullName", "invalid_format", "Tên không hợp lệ.");
        var phone = request.Contact.Phone.Trim();
        var digits = phone.Count(char.IsAsciiDigit);
        if (phone.Length > 30 || digits is < 7 or > 15 || !Regex.IsMatch(phone, @"^\+?[0-9][0-9 ()-]*[0-9]$"))
            throw CheckoutException.Validation("contact.phone", "invalid_format", "Điện thoại cần 7–15 chữ số, có thể có dấu + đầu, khoảng trắng, dấu ngoặc hoặc dấu gạch ngang.");
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Contact.Email.Trim()) || request.Contact.Email.Trim().Length > 256)
            throw CheckoutException.Validation("contact.email", "invalid_format", "Email không hợp lệ.");
        if (request.Declarations.TermsAccepted != true || request.Declarations.AgeRequirementsAcknowledged != true)
            throw CheckoutException.Validation("declarations", "must_be_true", "Phải xác nhận điều khoản và yêu cầu độ tuổi.");
        var total = request.PriceAcceptance.AcceptedTotal;
        if (total.Amount is null || total.Amount < 0 || decimal.Truncate(total.Amount.Value) != total.Amount || total.Currency != "VND")
            throw CheckoutException.Validation("priceAcceptance.acceptedTotal", "invalid_money", "Số tiền phải là VND nguyên không âm.");
    }
    private static async Task<Dictionary<string, string>> CategoryNames(ApplicationDbContext db, CancellationToken ct)
    {
        await using var command = Command(db, "SELECT code,display_name FROM cinema.ticket_category ORDER BY code FOR SHARE");
        await using var reader = await command.ExecuteReaderAsync(ct);
        var names = new Dictionary<string, string>();
        while (await reader.ReadAsync(ct)) names.Add(reader.GetString(0), reader.GetString(1));
        if (!names.ContainsKey("Adult") || !names.ContainsKey("Discounted")) throw Conflict("missing_categories");
        return names;
    }
    private static async Task<long[]> ConflictingSeats(ApplicationDbContext db, long showtime, long[] seats, CancellationToken ct)
    {
        await using var command = Command(db, "SELECT seat_id FROM cinema.booking_seat WHERE showtime_id=@showtime AND seat_id=ANY(@seats) AND allocation_status IN ('held','sold') ORDER BY seat_id");
        command.Parameters.AddWithValue("showtime", showtime); command.Parameters.AddWithValue("seats", seats);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<long>(); while (await reader.ReadAsync(ct)) result.Add(reader.GetInt64(0));
        return result.ToArray();
    }
    internal static async Task<BookingDto> ReadResult(ApplicationDbContext db, Guid owner, long id, ShowtimeContext context, CancellationToken ct)
    {
        var booking = await db.Set<Booking>().Include(x => x.Categories).Include(x => x.Seats).AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == id && x.CheckoutIdentityId == owner, ct)
            ?? throw new CheckoutException(404, "NOT_FOUND", "Không tìm thấy booking.");
        var selection = await db.Set<SelectionSession>().SingleAsync(x => x.Id == booking.SelectionSessionId && x.CheckoutIdentityId == owner, ct);
        var adult = booking.Categories.Single(x => x.CategoryCode == "Adult");
        var discounted = booking.Categories.Single(x => x.CategoryCode == "Discounted");
        var seats = booking.Seats.OrderBy(x => x.SeatId).Select(x => new SelectionSeatSnapshot(x.SeatId.ToString(CultureInfo.InvariantCulture),
            x.SeatLabelSnapshot, x.SeatTypeNameSnapshot, new(x.SurchargeSnapshot))).ToArray();
        var quote = new SelectionQuote(new Guid(Convert.FromHexString(selection.AcceptedQuoteFingerprint!)[..16]), selection.AcceptedQuoteFingerprint!,
            new(adult.Quantity, discounted.Quantity), new(adult.BasePriceSnapshot), new(discounted.BasePriceSnapshot), seats, new(booking.TotalAmount), booking.CreatedAt);
        DateTime? paymentDeadline = null; string? paymentId = null;
        await using (var command = Command(db, "SELECT id,payment_deadline FROM cinema.payment WHERE booking_id=@id AND (status IN ('created','pending','unknown') OR applied_at IS NOT NULL) ORDER BY id"))
        {
            command.Parameters.AddWithValue("id", id);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) { paymentId = reader.GetInt64(0).ToString(CultureInfo.InvariantCulture); paymentDeadline = reader.IsDBNull(1) ? null : reader.GetDateTime(1); }
        }
        return new(booking.Id.ToString(CultureInfo.InvariantCulture), booking.BookingCode.ToString("D"), booking.Status,
            context.ToDto(await Now(db, ct)), booking.PurchasedStartAt, seats, new(adult.Quantity, discounted.Quantity),
            new(adult.AdmittedQuantity, discounted.AdmittedQuantity),
            new(adult.Quantity - adult.AdmittedQuantity, discounted.Quantity - discounted.AdmittedQuantity), quote,
            new(booking.ContactFullName, booking.ContactPhone, booking.ContactEmail), booking.ReservationExpiresAt,
            paymentDeadline, paymentId, null, booking.CreatedAt, booking.ConfirmedAt, booking.Version.ToString(CultureInfo.InvariantCulture));
    }
}
