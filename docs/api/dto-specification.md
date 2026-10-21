# DTO Specification

## 1. Mục đích

Đặc tả chi tiết Request DTO / Response DTO cho từng nhóm API. Trạng thái: **Proposed** — cần nhóm chốt trước khi triển khai backend/frontend.

Tên trường theo `camelCase` (quy ước tại `api-contract.md`), không khớp tên cột database (`snake_case`).

## 2. Auth

### 2.1. POST `/api/auth/register`

Request:

```json
{ "email": "user@example.com", "password": "secret", "fullName": "Nguyen Van A", "phone": "0901234567" }
```

Response 201:

```json
{ "id": "uuid", "email": "user@example.com", "fullName": "Nguyen Van A", "createdAt": "2026-10-21T08:30:00Z" }
```

Lỗi: 400 `VALIDATION_ERROR`, 409 `EMAIL_ALREADY_EXISTS`.

### 2.2. POST `/api/auth/login`

Request: `{ "email": "...", "password": "..." }`

Response 200: `{ "token": "...", "user": { "id": "uuid", "email": "...", "fullName": "..." } }`

Lỗi: 401 `INVALID_CREDENTIALS`.

## 3. Movies

### 3.1. GET `/api/movies`

Query: `status` (`now_showing` | `coming_soon`), `page`, `pageSize`.

Response 200 `data` (mảng):

```json
{
  "id": "uuid",
  "title": "Interstellar",
  "posterUrl": "https://...",
  "ageRating": "T16",
  "status": "now_showing",
  "durationMinutes": 169
}
```

Kèm `pagination`. 3.2. GET `/api/movies/{movieId}` — như trên cộng `description`, `releaseDate`, `trailerUrl`, `language`, `genres` (mảng mã/name). Lỗi: 404 `MOVIE_NOT_FOUND`.

## 4. Showtimes

### 4.1. GET `/api/movies/{movieId}/showtimes?date=2026-10-22`

Response 200 `data`:

```json
{
  "id": "uuid",
  "cinemaRoomId": "uuid",
  "cinemaRoomName": "Room 2",
  "startsAt": "2026-10-22T13:00:00Z",
  "basePrice": 90000,
  "currency": "VND",
  "status": "scheduled",
  "bookingAvailable": true
}
```

### 4.2. GET `/api/showtimes/{showtimeId}/seats`

Response 200 `data`:

```json
{
  "showtimeId": "uuid",
  "rows": [
    { "rowLabel": "A", "seats": [
      { "id": "uuid", "seatNumber": 5, "seatTypeCode": "STANDARD", "capacityUnits": 1, "surchargeAmount": 0, "status": "available" }
    ] }
  ]
}
```

`status` ghế: `available` | `held` | `sold`. Lỗi: 404 `SHOWTIME_NOT_FOUND`.

## 5. Bookings

### 5.1. POST `/api/bookings` (tạo booking giữ ghế)

Request:

```json
{
  "showtimeId": "uuid",
  "requestKey": "uuid-của-trang-thao-tác",
  "items": [
    { "seatId": "uuid", "ticketTypeId": "uuid" }
  ]
}
```

Lưu ý: client không gửi giá; giá do server tính từ giá hiện hành.

Response 201:

```json
{
  "bookingId": "uuid",
  "bookingCode": "BK-20261021-0001",
  "status": "pending",
  "expiresAt": "2026-10-21T08:35:00Z",
  "totalAmount": 200000,
  "currency": "VND",
  "details": [
    { "seatLabel": "A5", "seatTypeName": "Standard", "ticketTypeName": "Student", "unitPrice": 100000 }
  ]
}
```

Lỗi: 409 `SEAT_ALREADY_TAKEN` (details: danh sách seatId bị chọn), 409 `SHOWTIME_CLOSED`, 400 `VALIDATION_ERROR`, 409 `REQUEST_KEY_CONFLICT`.

### 5.2. GET `/api/bookings/{bookingId}`

Response 200: như 5.1 cộng `confirmedAt`, `cancelledAt`, `cancellationReason`, `createdAt`, `tickets` (mảng `{ ticketCode, seatLabel, qrCodeUrl, status }` — ghế Couple phát hành 2 vé riêng). Lỗi: 403 `FORBIDDEN`, 404 `BOOKING_NOT_FOUND`.

### 5.3. POST `/api/bookings/{bookingId}/cancel`

Chỉ áp dụng khi `pending`. Response 200 với booking `status: "cancelled"`. Lỗi: 409 `BOOKING_NOT_CANCELLABLE`.

### 5.4. GET `/api/users/me/bookings`

Phân trang; item như 5.2 (tóm tắt). Rỗng → `data: []`.

## 6. Payments

### 6.1. POST `/api/payments`

Request: `{ "bookingId": "uuid", "provider": "vnpay" }` (provider theo cổng đã chốt).

Response 201:

```json
{ "paymentId": "uuid", "status": "pending", "amount": 200000, "currency": "VND", "payUrl": "https://..." }
```

Lỗi: 409 `PAYMENT_ATTEMPT_OPEN` (đang có attempt `pending`/`unknown`), 409 `BOOKING_EXPIRED`, 409 `BOOKING_ALREADY_CONFIRMED`.

### 6.2. GET `/api/payments/{paymentId}`

Response 200: `{ "paymentId": "uuid", "bookingId": "uuid", "status": "succeeded", "amount": 200000, "currency": "VND" }`.

`status`: `created` | `pending` | `succeeded` | `failed` | `cancelled` | `unknown`.

### 6.3. POST `/api/payments/callback` (webhook cổng)

Callback cổng thanh toán; nhận diện lặp qua `eventKey`, lặp trả 200 và không phát hành vé lần hai. Không mô tả chi tiết DTO webhook trong MVP; tham chiếu tài liệu cổng.

## 7. Tickets

### 7.1. GET `/api/tickets/{ticketCode}`

Response 200: `{ "ticketCode": "TK-...", "seatLabel": "A5", "status": "valid", "bookingCode": "BK-...", "showtimeStartsAt": "..." }`. Lỗi: 403 `FORBIDDEN`, 404 `TICKET_NOT_FOUND`.

### 7.2. POST `/api/tickets/{ticketCode}/validate` (staff)

Response 200: `{ "ticketCode": "TK-...", "status": "used", "validatedAt": "...", "validatedByUserId": "uuid" }`. Lỗi: 409 `TICKET_ALREADY_USED`, 403 `FORBIDDEN` (không phải staff).

## 8. Quản trị

- `POST /api/manage/movies`, `PUT /api/manage/movies/{id}`: request/response tương ứng các trường trong `MovieSummary`/`MovieDetail` (xem 3); chỉ Manager/Admin.
- `POST /api/manage/showtimes`: request `{ movieId, cinemaRoomId, startsAt, basePrice, bookingClosesAt? }`; lỗi 409 `SHOWTIME_ROOM_OVERLAP` khi chồng lịch phòng.
- `POST /api/manage/showtimes/{id}/cancel`: request `{ cancellationReason }`; response suất `status: "cancelled"`.

## 9. Ghi chú chung

- Tất cả DTO ở trên là **Proposed**; đối chiếu `api-contract.md` (quy ước, mã lỗi) và `docs/analysis and design/relational-model.md` (miền dữ liệu) trước khi chốt.
- Trường `*_snapshot` của database không được client gửi; chúng do backend sinh khi tạo booking.
- Mọi đổi tiền/định dạng thời gian theo quy ước tại `api-contract.md` mục 2.
