# API Endpoints

## Phạm vi và cách đọc

Catalogue đối chiếu Use-Case Specifications v2.4 và SAD v3.15. `browseMovies` **đã triển khai và đã kiểm chứng với PostgreSQL local** (theo log hiện có). Các endpoint public read thuộc BE1 được liệt kê trong tài liệu này: **đã triển khai, chưa kiểm chứng trên Docker PostgreSQL riêng** (trừ khi có bằng chứng test riêng được ghi bổ sung). Các operation khác chưa ghi rõ vẫn là **Proposed**. Mỗi dòng là một method/path duy nhất, operationId duy nhất. Policy, HTTP/error envelope, idempotency và enum mapping theo [API Contract](api-contract.md); payload theo [DTO Specification](dto-specification.md).

Path/query ID của các entity BE1 đã triển khai/căn chỉnh (`movie`, `genre`, `cinema`, `showtime`, `room`, `seat`, `seatType`) dùng **chuỗi số thập phân (`bigint`)** trong JSON/path/query, khớp schema và code hiện tại. Các entity/operation chưa triển khai khác vẫn theo contract Proposed hiện hành cho đến khi được triển khai và đối chiếu. Request có `(query)` là query parameters; `—` là không body. Response là phần bên trong `data`; `Type[]` là collection, pagination chỉ khi request có page/pageSize. Không sử dụng request DTO cho GET body.

Error envelope: `{ "error": { "code", "message", "details", "traceId" } }`; `details` luôn là object (`{}` nếu không có). Common errors: mọi operation có thể 400 `VALIDATION_ERROR`, 429 `RATE_LIMITED`, 500 `INTERNAL_ERROR`; operation protected có 401 `UNAUTHENTICATED`, 403 `FORBIDDEN`/`CSRF_REJECTED`, 404 `NOT_FOUND`. Mutations có 409 `IDEMPOTENCY_KEY_CONFLICT`/`OPERATION_IN_PROGRESS`/`STATE_CONFLICT` và 422 `BUSINESS_RULE_VIOLATION`; versioned edit thêm `VERSION_CONFLICT`. Validation dùng `VALIDATION_ERROR` với `details.violations = [{ "field", "reason", "message" }]` (ví dụ `date` thiếu `reason="required"`, sai format `reason="invalid_format"`). Provider webhook là ngoại lệ protocol.

## Identity

| Operation ID | Method | Path | UC | Policy | Request | Response | Success |
|---|---|---|---|---|---|---|---|
| `registerCustomer` | POST | `/api/auth/register` | UC-08A | PUBLIC | `RegisterRequest` | `RegisterResponse` | 201 |
| `login` | POST | `/api/auth/login` | UC-08C | PUBLIC | `LoginRequest` | `LoginResponse` | 200 |
| `verifyEmail` | POST | `/api/auth/verify-email` | UC-08B | VERIFICATION | `VerificationRequest` | `VerificationResponse` | 200 |
| `resendVerification` | POST | `/api/auth/verify-email/resend` | UC-08B | PUBLIC | `ResendVerificationRequest` | `EmailRequest` | 202 |
| `currentSession` | GET | `/api/auth/session` | UC-08C | AUTH | `—` | `Session` | 200 |
| `logout` | POST | `/api/auth/logout` | UC-08D | AUTH | `—` | `—` | 204 |
| `requestEmailChange` | POST | `/api/users/me/email-change` | UC-08B | AUTH | `EmailChangeRequest` | `EmailRequest` | 202 |
| `cancelEmailChange` | DELETE | `/api/users/me/email-change` | UC-08B | AUTH | `—` | `—` | 204 |
| `createGuestContext` | POST | `/api/guest-context` | UC-01, UC-09 | PUBLIC | `—` | `GuestContext` | 201/200 — **Implemented on `feat/be-guest-context`** |

401 INVALID_CREDENTIALS; 409 EMAIL_UNAVAILABLE khi được phép; verify token sai/hết hạn dùng 422 BUSINESS_RULE_VIOLATION; public resend không enumerate account.

- **createGuestContext:** Set/reuse HttpOnly Guest cookie, lifetime demo 24 giờ không gia hạn; trả `X-CSRF-Token`, không reset selection. Cần UUID `Idempotency-Key` và trusted Origin; bootstrap không yêu cầu CSRF token đã có. Replay cần matching cookie, không dùng key để khôi phục credential. Chi tiết local/online và kiểm thử tại [Guest checkout](../development/guest-checkout.md).

## Discovery

| Operation ID | Method | Path | UC | Policy | Request | Response | Success |
|---|---|---|---|---|---|---|---|
| `browseMovies` | GET | `/api/movies` | UC-06A | PUBLIC | `MovieQuery (query)` | `Movie[]` + pagination | 200 — **Đã triển khai; đã kiểm chứng với PostgreSQL local** |
| `movieDetail` | GET | `/api/movies/{movieId}` | UC-06B | PUBLIC | `—` | `Movie` | 200 — **Đã triển khai, chưa kiểm chứng Docker PG riêng** |
| `publicGenres` | GET | `/api/genres` | UC-06A | PUBLIC | `—` | `Genre[]` | 200 |
| `publicCinemas` | GET | `/api/cinemas` | UC-07 | PUBLIC | `—` | `Cinema[]` | 200 — **Đã triển khai, chưa kiểm chứng Docker PG riêng** |
| `browseShowtimes` | GET | `/api/movies/{movieId}/showtimes` | UC-07 | PUBLIC | `ShowtimeQuery (query)` | `Showtime[]` | 200 — **Đã triển khai, chưa kiểm chứng Docker PG riêng** |
| `showtimeDetail` | GET | `/api/showtimes/{showtimeId}` | UC-07 | PUBLIC | `—` | `Showtime` | 200 — **Đã triển khai, chưa kiểm chứng Docker PG riêng** |
| `seatMap` | GET | `/api/showtimes/{showtimeId}/seats` | UC-01 | PUBLIC | `—` | `SeatMap` | 200 — **Đã triển khai, chưa kiểm chứng Docker PG riêng** |
| `publicPricing` | GET | `/api/cinemas/{cinemaId}/ticket-pricing` | UC-07, UC-16B | PUBLIC | `—` | `CinemaPricing` | 200 — **Đã triển khai, chưa kiểm chứng Docker PG riêng** |
| `ticketCategories` | GET | `/api/ticket-categories` | UC-01, UC-16A | PUBLIC | `—` | `TicketCategory[]` | 200 — **Đã triển khai, chưa kiểm chứng Docker PG riêng** |

Ghi chú (BE1): `movieDetail` cho phép `ended`; `archived`/không tồn tại → `404 NOT_FOUND`. `browseShowtimes` yêu cầu `date` bắt buộc (`YYYY-MM-DD` theo timezone rạp); thiếu/sai format → `400 VALIDATION_ERROR` với `details.violations`. Public showtime chỉ gồm status `scheduled` (cách diễn giải UC-07 bước 2 và R-21 “suất đang bán”, tài liệu không liệt kê status); showtime ngoài tập public → `404 NOT_FOUND` ở `showtimeDetail`/`seatMap`. Phim tồn tại nhưng không có suất (theo filter) → `200 { "data": [], "pagination": ... }`; phim không tồn tại hoặc archived → `404 NOT_FOUND`. `publicPricing`: thiếu cấu hình giá (thiếu dòng giá hoặc thiếu category bắt buộc) → `409 STATE_CONFLICT`; `amount = 0` là cấu hình hợp lệ.

## Checkout

| Operation ID | Method | Path | UC | Policy | Request | Response | Success |
|---|---|---|---|---|---|---|---|
| `createSelection` | POST | `/api/selections` | UC-01 | CHECKOUT | `CreateSelectionRequest` | `Selection` | 201/200  **Implemented on `feat/be-selection-quote`** |
| `readSelection` | GET | `/api/selections/{selectionId}` | UC-01, UC-09 | OWNER | `—` | `Selection` | 200  **Implemented on `feat/be-selection-quote`** |
| `updateSelection` | PUT | `/api/selections/{selectionId}` | UC-01 | OWNER | `UpdateSelectionRequest` | `Selection` | 200  **Implemented on `feat/be-selection-quote`** |
| `reserveSeats` | POST | `/api/bookings` | UC-01, UC-09 | CHECKOUT | `ReserveRequest` | `Booking` | 201 / 200 replay — **Implemented on `feat/be-booking-reservation`** |
| `readBooking` | GET | `/api/bookings/{bookingId}` | UC-10 | OWNER | `—` | `Booking` | 200 — **Implemented on `feat/be-checkout-recovery`** |
| `recoverCheckout` | GET | `/api/bookings/{bookingId}/checkout` | UC-09 | OWNER | `—` | `CheckoutRecovery` | 200 — **Implemented on `feat/be-checkout-recovery`** |
| `myBookings` | GET | `/api/users/me/bookings` | UC-10 | CUSTOMER | `PageQuery (query)` | `Booking[]` | 200 |
| `cancelPending` | POST | `/api/bookings/{bookingId}/cancel` | UC-10 | OWNER | `CancelPendingRequest` | `Booking` | 200 — **Implemented on `feat/be-booking-cancel-expiry`** |

409 SEAT_CONFLICT/VERSION_CONFLICT; 422 SELECTION_EXPIRED/PRICE_CHANGED/SHOWTIME_NOT_BOOKABLE; cancelPending có CANCELLATION_NOT_ELIGIBLE.

- **createSelection:** Idempotent; reuse active context không reset deadline.
- **reserveSeats:** Idempotency-Key bắt buộc; Step 1 → Step 2, atomic hold.
- **readBooking / recoverCheckout:** Valid owning Guest cookie, positive decimal-string booking ID, no-store. No CSRF/idempotency header for GET. Missing/foreign booking both return 404; invalid Guest proof 401; malformed ID 400. Reads preserve snapshots, versions and deadlines, create no resources and release no seats. See [recovery states](../development/checkout-recovery.md).
- **cancelPending:** Valid owning Guest cookie, trusted Origin, CSRF, UUID Idempotency-Key and `confirmed=true`. Pending only, before any payment-initiation commit; one transaction cancels/releases/audits/records outcome. Same-key success/rejection replay is marked; new intent on cancelled booking returns 422. [Cancellation/expiry behavior](../development/booking-cancel-expiry.md).

## Payment

| Operation ID | Method | Path | UC | Policy | Request | Response | Success |
|---|---|---|---|---|---|---|---|
| `initiatePayment` | POST | `/api/bookings/{bookingId}/payments` | UC-09 | OWNER | `InitiatePaymentRequest` | `PaymentInitiation` | 201/200/202 — **MockPay implemented on `feat/integrate-mockpay`** |
| `readPayment` | GET | `/api/payments/{paymentId}` | UC-09, UC-10 | OWNER | `—` | `Payment` | 200 — **MockPay implemented on `feat/integrate-mockpay`** |
| `paymentWebhook` | POST | `/api/integrations/momo/webhook` | UC-02 | PROVIDER | `Provider payload` | `Provider ACK` | Provider |

422 PAYMENT_NOT_ELIGIBLE; 503 DEPENDENCY_UNAVAILABLE. Durable unknown initiation trả 202 cùng paymentId; không suy failed. MockPay không dùng webhook: backend tạo payment bằng `orderId = bookingCode`, trả `redirectUrl`, rồi frontend polling endpoint PHOS `GET /api/payments/{paymentId}`.

- **initiatePayment:** Key bắt buộc; reuse open attempt, không tạo attempt mới.
- **MockPay:** `provider_transaction_id` là MockPay `paymentId`; backend đối chiếu order, amount, currency và chỉ chuyển booking/ghế sang confirmed/sold sau khi trạng thái `PAID` được xác minh. `EXPIRED`/`CANCELLED` không xác nhận booking.
- **paymentWebhook:** Raw verified provider request; không application DTO.

## Ticket

| Operation ID | Method | Path | UC | Policy | Request | Response | Success |
|---|---|---|---|---|---|---|---|
| `readTicket` | GET | `/api/bookings/{bookingId}/ticket` | UC-10 | OWNER | `—` | `Ticket` | 200 |
| `resendTicket` | POST | `/api/bookings/{bookingId}/ticket/resend-email` | UC-10 | OWNER | `—` | `Notification` | 202 |
| `admissionHistory` | GET | `/api/bookings/{bookingId}/admissions` | UC-10 | OWNER | `PageQuery (query)` | `AdmissionResult[]` | 200 |

Áp dụng common errors và policy của API Contract.

- **resendTicket:** Key + cooldown; email lưu trong booking.

## Admission

| Operation ID | Method | Path | UC | Policy | Request | Response | Success |
|---|---|---|---|---|---|---|---|
| `validateAdmission` | POST | `/api/admissions/validate` | UC-05 | ADMISSION | `ValidateAdmissionRequest` | `AdmissionValidation` | 200 |
| `confirmAdmission` | POST | `/api/admissions/confirm` | UC-05 | ADMISSION | `ConfirmAdmissionRequest` | `AdmissionResult` | 200 |

422 ADMISSION_WINDOW_CLOSED/ADMISSION_RESTRICTED; 409 ADMISSION_ENTITLEMENT_EXCEEDED. Validation trả restrictions trong 200 cho QR có thể đọc; invalid/out-of-scope QR 404/403 theo policy.

- **validateAdmission:** Không mutation; trả restrictions cho QR tồn tại trong scope.
- **confirmAdmission:** Idempotency-Key = operation UUID. Recheck toàn bộ điều kiện trong transaction.

## Cancellation

| Operation ID | Method | Path | UC | Policy | Request | Response | Success |
|---|---|---|---|---|---|---|---|
| `requestCancellation` | POST | `/api/bookings/{bookingId}/cancellation-requests` | UC-10, UC-04A | OWNER | `CancellationRequest` | `Cancellation` | 201 |
| `readCancellation` | GET | `/api/bookings/{bookingId}/cancellation-requests` | UC-10 | OWNER | `PageQuery (query)` | `Cancellation[]` | 200 |
| `readRefunds` | GET | `/api/bookings/{bookingId}/refunds` | UC-10, UC-04A | OWNER | `PageQuery (query)` | `Refund[]` | 200 |

422 CANCELLATION_NOT_ELIGIBLE; 409 STATE_CONFLICT khi race admission/review/payment.


## Operational support

| Operation ID | Method | Path | UC | Policy | Request | Response | Success |
|---|---|---|---|---|---|---|---|
| `searchBookings` | GET | `/api/management/bookings` | UC-19 | OPS | `OperationalQuery (query)` | `Booking[]` | 200 |
| `searchCancellation` | GET | `/api/management/cancellation-requests` | UC-19 | OPS | `OperationalQuery (query)` | `Cancellation[]` | 200 |
| `searchRefunds` | GET | `/api/management/refunds` | UC-19, UC-04A | OPS | `OperationalQuery (query)` | `Refund[]` | 200 |
| `operationalBooking` | GET | `/api/management/bookings/{bookingId}` | UC-19 | OPS | `—` | `Booking` | 200 |
| `operationalTicket` | GET | `/api/management/bookings/{bookingId}/ticket` | UC-19 | OPS | `—` | `Ticket` | 200 |
| `operationalPayment` | GET | `/api/management/payments/{paymentId}` | UC-19 | OPS | `—` | `Payment` | 200 |
| `operationalRefund` | GET | `/api/management/refunds/{refundId}` | UC-19 | OPS | `—` | `Refund` | 200 |
| `supportResendTicket` | POST | `/api/management/bookings/{bookingId}/ticket/resend-email` | UC-19 | OPS | `—` | `Notification` | 202 |
| `reviewCancellation` | POST | `/api/management/cancellation-requests/{requestId}/review` | UC-19, UC-04A | OPS | `ReviewCancellationRequest` | `Cancellation` | 200 |
| `completeRefund` | POST | `/api/management/refunds/{refundId}/completion` | UC-04A, UC-19 | REFUND | `CompleteRefundRequest` | `Refund` | 200 |
| `readRefundEvidence` | GET | `/api/management/refunds/{refundId}/evidence` | UC-04A, UC-19 | REFUND | `—` | `RefundEvidence` | 200 |

422 REFUND_EVIDENCE_INVALID/CANCELLATION_NOT_ELIGIBLE; 409 VERSION_CONFLICT/STATE_CONFLICT. Staff được review cancellation nhưng không complete refund.


## Management read

| Operation ID | Method | Path | UC | Policy | Request | Response | Success |
|---|---|---|---|---|---|---|---|
| `manageMovies` | GET | `/api/management/movies` | UC-12 | ADMIN | `PageQuery (query)` | `Movie[]` | 200 |
| `manageGenres` | GET | `/api/management/genres` | UC-13 | ADMIN | `PageQuery (query)` | `Genre[]` | 200 |
| `manageCinemas` | GET | `/api/management/cinemas` | UC-14A | OPS | `PageQuery (query)` | `Cinema[]` | 200 |
| `manageRooms` | GET | `/api/management/cinemas/{cinemaId}/rooms` | UC-14B | OPS | `PageQuery (query)` | `Room[]` | 200 |
| `manageSeatTypes` | GET | `/api/management/seat-types` | UC-15C | OPS | `PageQuery (query)` | `SeatType[]` | 200 |
| `manageCategories` | GET | `/api/management/ticket-categories` | UC-16A | OPS | `PageQuery (query)` | `TicketCategory[]` | 200 |
| `manageShowtimes` | GET | `/api/management/showtimes` | UC-17 | OPS | `PageQuery (query)` | `Showtime[]` | 200 |
| `manageUsers` | GET | `/api/management/users` | UC-18 | ADMIN | `PageQuery (query)` | `UserAccess[]` | 200 |
| `readLayout` | GET | `/api/management/rooms/{roomId}/seat-layout` | UC-15A | OPS | `—` | `SeatLayout` | 200 |
| `readCinemaPricing` | GET | `/api/management/cinemas/{cinemaId}/ticket-pricing` | UC-16B | OPS | `—` | `CinemaPricing` | 200 |

Áp dụng common errors và policy của API Contract.


## Management write

| Operation ID | Method | Path | UC | Policy | Request | Response | Success |
|---|---|---|---|---|---|---|---|
| `createMovie` | POST | `/api/management/movies` | UC-12 | ADMIN | `CreateMovieRequest` | `Movie` | 201 |
| `updateMovie` | PUT | `/api/management/movies/{movieId}` | UC-12 | ADMIN | `UpdateMovieRequest` | `Movie` | 200 |
| `changeMovieState` | POST | `/api/management/movies/{movieId}/state` | UC-12 | ADMIN | `MovieStateRequest` | `Movie` | 200 |
| `createGenre` | POST | `/api/management/genres` | UC-13 | ADMIN | `GenreWrite` | `Genre` | 201 |
| `updateGenre` | PUT | `/api/management/genres/{genreId}` | UC-13 | ADMIN | `UpdateGenreRequest` | `Genre` | 200 |
| `changeGenreState` | POST | `/api/management/genres/{genreId}/state` | UC-13 | ADMIN | `ActiveStateRequest` | `Genre` | 200 |
| `createCinema` | POST | `/api/management/cinemas` | UC-14A | ADMIN | `CinemaWrite` | `Cinema` | 201 |
| `updateCinema` | PUT | `/api/management/cinemas/{cinemaId}` | UC-14A | ADMIN | `UpdateCinemaRequest` | `Cinema` | 200 |
| `changeCinemaState` | POST | `/api/management/cinemas/{cinemaId}/state` | UC-14A | ADMIN | `ActiveStateRequest` | `Cinema` | 200 |
| `configureTurnaround` | PUT | `/api/management/cinemas/{cinemaId}/turnaround` | UC-14A | MANAGE | `TurnaroundRequest` | `Cinema` | 200 |
| `createRoom` | POST | `/api/management/cinemas/{cinemaId}/rooms` | UC-14B | ADMIN | `RoomWrite` | `Room` | 201 |
| `updateRoom` | PUT | `/api/management/rooms/{roomId}` | UC-14B | ADMIN | `UpdateRoomRequest` | `Room` | 200 |
| `changeRoomState` | POST | `/api/management/rooms/{roomId}/state` | UC-14B | ADMIN | `ActiveStateRequest` | `Room` | 200 |
| `configureLayout` | PUT | `/api/management/rooms/{roomId}/seat-layout` | UC-15A | MANAGE | `ConfigureLayoutRequest` | `SeatLayout` | 200 |
| `changeSeatState` | POST | `/api/management/seats/{seatId}/state` | UC-15B | MANAGE | `SeatStateRequest` | `Seat` | 200 |
| `createSeatType` | POST | `/api/management/seat-types` | UC-15C | ADMIN | `SeatTypeWrite` | `SeatType` | 201 |
| `updateSeatType` | PUT | `/api/management/seat-types/{seatTypeId}` | UC-15C | ADMIN | `UpdateSeatTypeRequest` | `SeatType` | 200 |
| `changeSeatTypeState` | POST | `/api/management/seat-types/{seatTypeId}/state` | UC-15C | ADMIN | `ActiveStateRequest` | `SeatType` | 200 |
| `updateTicketCategory` | PUT | `/api/management/ticket-categories/{categoryCode}` | UC-16A | ADMIN | `UpdateCategoryRequest` | `TicketCategory` | 200 |
| `configurePricing` | PUT | `/api/management/cinemas/{cinemaId}/ticket-pricing` | UC-16B | MANAGE | `ConfigurePricingRequest` | `CinemaPricing` | 200 |
| `createShowtime` | POST | `/api/management/showtimes` | UC-17 | MANAGE | `CreateShowtimeRequest` | `Showtime` | 201 |
| `updateShowtime` | PUT | `/api/management/showtimes/{showtimeId}` | UC-17 | MANAGE | `UpdateShowtimeRequest` | `Showtime` | 200 |
| `rescheduleShowtime` | POST | `/api/management/showtimes/{showtimeId}/reschedule` | UC-17 | MANAGE | `RescheduleRequest` | `Showtime` | 200 |
| `cancelShowtime` | POST | `/api/management/showtimes/{showtimeId}/cancel` | UC-17 | MANAGE | `ReasonRequest` | `Showtime` | 200 |
| `grantRole` | POST | `/api/management/users/{userId}/roles/grant` | UC-18 | ADMIN | `RoleChangeRequest` | `UserAccess` | 200 |
| `revokeRole` | POST | `/api/management/users/{userId}/roles/revoke` | UC-18 | ADMIN | `RoleChangeRequest` | `UserAccess` | 200 |
| `changeAccountState` | POST | `/api/management/users/{userId}/state` | UC-18 | ADMIN | `UserStateRequest` | `UserAccess` | 200 |
| `addAssignment` | POST | `/api/management/users/{userId}/cinema-assignments/add` | UC-18 | ASSIGN | `AssignmentRequest` | `UserAccess` | 200 |
| `removeAssignment` | POST | `/api/management/users/{userId}/cinema-assignments/remove` | UC-18 | ASSIGN | `AssignmentRequest` | `UserAccess` | 200 |

409 VERSION_CONFLICT/STATE_CONFLICT; 422 BUSINESS_RULE_VIOLATION cho overlap, reference protection, last active Admin và invalid scope operation.


## Chat

| Operation ID | Method | Path | UC | Policy | Request | Response | Success |
|---|---|---|---|---|---|---|---|
| `createConversation` | POST | `/api/chat/conversations` | UC-11 | CHECKOUT | `CreateChatRequest` | `Conversation` | 201 |
| `getConversation` | GET | `/api/chat/conversations/{conversationId}` | UC-11 | OWNER | `—` | `Conversation` | 200 |
| `sendMessage` | POST | `/api/chat/conversations/{conversationId}/messages` | UC-11 | OWNER | `ChatMessageRequest` | `ChatMessage` | 200 |
| `clearConversation` | DELETE | `/api/chat/conversations/{conversationId}` | UC-11 | OWNER | `—` | `—` | 204 |

503 DEPENDENCY_UNAVAILABLE; expired/cleared/restarted conversation 404 NOT_FOUND. Request không thể thực hiện mutation nghiệp vụ.

- **sendMessage:** Serialize per conversation; bounded context; no business mutation.

## Workflow không có HTTP endpoint

| UC/workflow | Execution | Ranh giới |
|---|---|---|
| UC-03 Release Expired Bookings | C14 gọi service nội bộ | Re-read dưới transaction protocol; release khi payment-safe; không chỉ dựa timer |
| UC-04 Payment Reconciliation | C14/C09 service nội bộ | Provider query ngoài transaction; revalidate và apply/route refund |
| Email delivery | C14/C15 đọc persisted request | Sau business commit; retry không tạo ticket/booking mới |
| Showtime state progression | Backend/background theo UC-17 | Server time/state, không client tùy ý chuyển sang completed |
| Refund obligation creation | C09/C10 coordinated workflow | Reconciliation hoặc approved rescheduling request; không public force-create |

Không gọi `/api/internal/...` từ worker cùng modular monolith. Đề xuất topology worker riêng sau này phải review deployment/auth riêng.

## Các giới hạn vận hành cần giữ

- Public movie listings exclude ended/archived; direct detail cho ended, không cho archived. Management reads có visibility riêng.
- Physical room layout dùng SeatLayout; showtime availability dùng SeatMap. Không dùng DTO có showtimeId bắt buộc cho cấu hình phòng chưa có suất.
- Staff không sửa room, layout, seat operational state, shared definitions, pricing hoặc showtime. Staff được review cancellation trong cinema scope. Manager không sửa movie/genre/shared definitions hoặc room identity/state.
- Hai ticket categories là dữ liệu cố định; chỉ update descriptive fields. Không POST create category hoặc DELETE required category.
- Catalogue không có generic financial state override, automatic money-transfer refund hoặc public admin bootstrap.
- Download vé được frontend render từ Ticket DTO đã được cấp quyền, không tạo ticket thứ hai. Guest credential recovery ngoài browser hiện tại còn là gap cần chốt.
- Thao tác archive/restore movie dùng changeMovieState; active/inactive definitions dùng state operation và bảo vệ lịch sử. Permanent deletion không được tự suy từ tên Manage UC; không có DELETE business records trong baseline này.
- Schema và code thực tế chưa được audit trong lần soạn; không mặc định DTO này là entity/database migration.

## Coverage 28 use cases

Coverage dưới đây là mapping trách nhiệm và operations đề xuất, không phải chứng nhận mọi alternative flow đã implement/test. Điều kiện nghiệp vụ chi tiết vẫn theo DOCX.

| UC | Operations hoặc execution |
|---|---|
| UC-01 | `createGuestContext`, `seatMap`, `ticketCategories`, `createSelection`, `readSelection`, `updateSelection`, `reserveSeats` |
| UC-02 | `paymentWebhook` |
| UC-03 | `C14 → expiration service (internal)` |
| UC-04 | `C14/C09 → reconciliation service (internal); readPayment exposes authorized result` |
| UC-04A | `requestCancellation`, `readRefunds`, `searchRefunds`, `reviewCancellation`, `completeRefund`, `readRefundEvidence` |
| UC-05 | `validateAdmission`, `confirmAdmission` |
| UC-06A | `browseMovies`, `publicGenres` |
| UC-06B | `movieDetail` |
| UC-07 | `publicCinemas`, `browseShowtimes`, `showtimeDetail`, `publicPricing` |
| UC-08A | `registerCustomer` |
| UC-08B | `verifyEmail`, `resendVerification`, `requestEmailChange`, `cancelEmailChange` |
| UC-08C | `login`, `currentSession` |
| UC-08D | `logout` |
| UC-09 | `createGuestContext`, `readSelection`, `reserveSeats`, `recoverCheckout`, `initiatePayment`, `readPayment` |
| UC-10 | `readBooking`, `myBookings`, `cancelPending`, `readPayment`, `readTicket`, `resendTicket`, `admissionHistory`, `requestCancellation`, `readCancellation`, `readRefunds` |
| UC-11 | `createConversation`, `getConversation`, `sendMessage`, `clearConversation` |
| UC-12 | `manageMovies`, `createMovie`, `updateMovie`, `changeMovieState` |
| UC-13 | `manageGenres`, `createGenre`, `updateGenre`, `changeGenreState` |
| UC-14A | `manageCinemas`, `createCinema`, `updateCinema`, `changeCinemaState`, `configureTurnaround` |
| UC-14B | `manageRooms`, `createRoom`, `updateRoom`, `changeRoomState` |
| UC-15A | `readLayout`, `configureLayout` |
| UC-15B | `changeSeatState` |
| UC-15C | `manageSeatTypes`, `createSeatType`, `updateSeatType`, `changeSeatTypeState` |
| UC-16A | `ticketCategories`, `manageCategories`, `updateTicketCategory` |
| UC-16B | `publicPricing`, `readCinemaPricing`, `configurePricing` |
| UC-17 | `manageShowtimes`, `createShowtime`, `updateShowtime`, `rescheduleShowtime`, `cancelShowtime` |
| UC-18 | `manageUsers`, `grantRole`, `revokeRole`, `changeAccountState`, `addAssignment`, `removeAssignment` |
| UC-19 | `searchBookings`, `searchCancellation`, `searchRefunds`, `operationalBooking`, `operationalTicket`, `operationalPayment`, `operationalRefund`, `supportResendTicket`, `reviewCancellation`, `completeRefund`, `readRefundEvidence` |

## API-specific acceptance checks

- Guest không thể đọc booking của Guest khác khi chỉ biết ID/code/email; token không hợp lệ không fallback Guest.
- Repeated createSelection không reset active deadline; reserve chỉ tạo một booking/hold và không partial hold.
- PRICE_CHANGED không tạo booking; accepted quote mới vẫn giữ deadline cũ.
- Payment initiation timeout/replay giữ cùng attempt; provider success không applicable không issue ticket.
- Hai admission confirmation tranh quota không vượt remaining; replay không tăng admitted.
- Admission và cancellation approval race được tuần tự hóa; không approve sau admission hoặc admit khi approved-awaiting-refund.
- Refund completion replay không lặp release/void/financial effect; reconciliation-origin không chạy rescheduling cancellation effects.
- Staff review cancellation thành công trong scope; Staff complete refund/layout edit bị từ chối.
- Management stale version bị từ chối; role/assignment revoked có hiệu lực trước protected commit.
- Chat clear/restart không trả persisted history; không dùng chat để mutate booking.

Các mục này là tiêu chí dẫn xuất để viết contract/integration tests, chưa phải kết quả đã chạy.

BE2 task 2: `createSelection`, `readSelection`, `updateSelection` are implemented on `feat/be-selection-quote`. See [selection/quote behavior](../development/selection-quote.md); showtime and seat IDs are decimal strings matching SQL. Other checkout operations remain proposed.

BE2 task 3: [reservation outcomes, idempotency and testing](../development/booking-reservation.md). POST booking is implemented; task 4 adds [booking reads/recovery](../development/checkout-recovery.md). Task 5 implements [pending cancellation and safe background expiry](../development/booking-cancel-expiry.md). No public worker-run endpoint is exposed.
