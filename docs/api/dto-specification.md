# DTO Specification

## Trạng thái và quy ước

**Proposed API design**, đối chiếu Use-Case Specifications v2.4 và SAD v3.15. Mỗi type chỉ định nghĩa một lần. Tên JSON camelCase. Không đồng nhất DTO với database entity/schema. Method/path/policy tại [API Endpoints](api-endpoints.md); envelope/error/idempotency tại [API Contract](api-contract.md).

| Ký hiệu | Ý nghĩa |
|---|---|
| R | Field bắt buộc, không null |
| O | Có thể bỏ; nếu có phải đúng type và không null |
| N | Field phải xuất hiện nhưng cho phép null |
| UUID | String UUID; ví dụ 00000000-0000-4000-8000-000000000001 |
| Instant | String UTC ISO 8601 với Z |
| Date | String YYYY-MM-DD |
| Role | CUSTOMER, STAFF, MANAGER hoặc ADMIN |
| enum:a,b | Chỉ nhận các string trong tập liệt kê; API projection Proposed |
| literal:X | String cố định X |
| T[] | Array của T; không null |

Response DTO là giá trị `data`, không có envelope lồng bên trong nested type. Input không có field identity/actor/time do server sinh trừ nơi định nghĩa rõ (refund evidence). Idempotency-Key nằm ở header, không có requestKey trong body. Unknown input field bị từ chối. String ngoài password/token được trim nếu không thay đổi ngữ nghĩa; maximum lengths/input limits phải chốt từ source validation trước code, không tự coi chưa có giới hạn là unlimited.

Các collection/query defaults, empty arrays và pagination theo API Contract. LoginResponse là nơi duy nhất trả access token; Current Session dùng Session, không trả lại token. Guest credential chỉ nằm trong cookie, QR chỉ trả qua protected Ticket.

## Pagination

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `page` | `integer` | R | >=1 |
| `pageSize` | `integer` | R | 1..100 |
| `totalItems` | `integer` | R | >=0 |
| `totalPages` | `integer` | R | ceil(totalItems/pageSize); 0 khi rỗng |

## PageQuery

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `page` | `integer` | O | Mặc định 1 |
| `pageSize` | `integer` | O | Mặc định 20, tối đa 100 |

## Money

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `amount` | `integer` | R | VND nguyên không âm |
| `currency` | `literal:VND` | R | Chỉ VND |

## Quantities

Tổng = adult + discounted. Reservation yêu cầu tổng 1..configuredMaximum (mặc định 8); admission confirmation yêu cầu tổng >0 và không vượt entitlement từng category.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `adult` | `integer` | R | >=0 |
| `discounted` | `integer` | R | >=0 |

## VersionRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `version` | `string` | R | Opaque version đọc từ response; stale => VERSION_CONFLICT |

## ReasonRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `version` | `string` | R | Version hiện hành |
| `reason` | `string` | R | Không rỗng; ghi audit |

## Contact

Cả Guest và Customer nhập đủ ba trường. Không thay đổi primary email của tài khoản khi nhập email liên hệ booking.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `fullName` | `string` | R | Không rỗng |
| `phone` | `string` | R | Số điện thoại hợp lệ |
| `email` | `string` | R | Email nhận vé hợp lệ |

## CheckoutDeclarations

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `termsAccepted` | `boolean` | R | Phải true |
| `ageRequirementsAcknowledged` | `boolean` | R | Phải true; không thay thế kiểm tra tuổi tại admission |

## User

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `userId` | `UUID` | R | Identity ổn định |
| `email` | `string` | R | Primary email |
| `fullName` | `string` | R | Tên hiển thị |
| `emailVerified` | `boolean` | R | Trạng thái hiện tại |
| `accountState` | `enum:active,locked` | R | API projection theo UC-18 |
| `roles` | `Role[]` | R | Các role hiện hành |

## CinemaAssignment

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `cinemaId` | `decimal string` | R | Cinema được giao (ID `bigint`, chuỗi số) |
| `role` | `enum:STAFF,MANAGER` | R | Không trộn assignment của hai role |

## Session

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `sessionId` | `UUID` | R | ID công khai, không phải credential |
| `expiresAt` | `Instant` | R | Deadline server |
| `user` | `User` | R | Current account |
| `cinemaAssignments` | `CinemaAssignment[]` | R | Scope hiện hành |

## RegisterRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `email` | `string` | R | Email hợp lệ/unique theo UC-08A |
| `password` | `string` | R | Identity policy; không log |
| `fullName` | `string` | R | Không rỗng |
| `phone` | `string` | O | Nếu cung cấp phải hợp lệ |

## RegisterResponse

Registration không tự đăng nhập. Không trả password/token xác minh.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `user` | `User` | R | CUSTOMER được cấp bởi server |
| `verificationDelivery` | `EmailRequest` | R | Yêu cầu gửi xác minh; không chứa token |

## LoginRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `email` | `string` | R | Primary email |
| `password` | `string` | R | Không log |

## LoginResponse

JWT transport là đề xuất API phù hợp SAD. Login thành công thay thế session cũ theo UC-08C; không thêm tùy chọn bỏ qua single-session.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `accessToken` | `string` | R | Bearer JWT nhạy cảm |
| `session` | `Session` | R | Server session tương ứng |

## VerificationRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `requestId` | `UUID` | R | Yêu cầu email |
| `token` | `string` | R | Opaque one-use credential; redact khỏi log |

## VerificationResponse

Không tự đăng nhập hoặc cấp session mới.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `verified` | `boolean` | R | true khi thành công |
| `verifiedAt` | `Instant` | R | Thời gian server |

## ResendVerificationRequest

Response chống enumeration; rate limit và cooldown theo UC-08B.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `email` | `string` | R | Email cần xác minh |

## EmailChangeRequest

User authenticated. Email cũ giữ nguyên đến khi verify thành công; token không trả trực tiếp trong API.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `newEmail` | `string` | R | Địa chỉ đề nghị thay thế |

## EmailRequest

Public resend trả shape/message tương đương dù email không tồn tại. Không lộ trạng thái tài khoản qua thông tin timing riêng.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `accepted` | `boolean` | R | Chấp nhận xử lý; không chứng minh đã gửi |
| `nextAllowedAt` | `Instant` | N | Cooldown nếu được phép công bố |

## GuestContext

Credential chỉ trong cookie HttpOnly; không nằm trong JSON. Lifetime chưa chốt: implementation phải bảo đảm truy cập booking hợp lệ và có recovery policy được review.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `contextId` | `UUID` | R | Identity không phải secret |
| `expiresAt` | `Instant` | R | Server-defined |

## MovieQuery

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `query` | `string` | O | Từ khóa |
| `genreId` | `decimal string` | O | ID `bigint` của genre; chuỗi số nguyên dương |
| `status` | `enum:coming_soon,now_showing` | O | Chỉ public listing |
| `page` | `integer` | O | >=1, mặc định 1 |
| `pageSize` | `integer` | O | 1..100, mặc định 12 cho browse movies |

## Genre

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `genreId` | `decimal string` | R | Identity `bigint`, serialize thành chuỗi số |
| `name` | `string` | R | Tên |
| `description` | `string` | N | Mô tả |
| `isActive` | `boolean` | R | Trạng thái |
| `version` | `decimal string` | R | Concurrency token `bigint`, serialize thành chuỗi số |

## Movie

Public detail cho phép `ended`; `archived` hoặc không tồn tại → `404 NOT_FOUND` (không lộ nội dung nội bộ). Phim tồn tại nhưng không có suất → browse showtimes trả `200 { "data": [], "pagination": ... }` (không ảnh hưởng detail). Management có thể đọc archived. Response không tự chứng minh phim có suất còn bán.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `movieId` | `decimal string` | R | Identity `bigint`, serialize thành chuỗi số |
| `title` | `string` | R | Tên phim |
| `description` | `string` | N | Nội dung |
| `durationMinutes` | `integer` | R | >0 |
| `releaseDate` | `Date` | N | Ngày phát hành |
| `posterUrl` | `string` | N | URL hợp lệ |
| `trailerUrl` | `string` | N | URL hợp lệ |
| `language` | `string` | N | Ngôn ngữ |
| `ageRating` | `string` | N | Có thể null khi database chưa có phân loại |
| `status` | `enum:coming_soon,now_showing,ended,archived` | R | Public listing chỉ coming_soon/now_showing |
| `genres` | `Genre[]` | R | Genre liên quan |
| `version` | `decimal string` | R | `bigint` version serialize thành chuỗi số |

## Cinema

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `cinemaId` | `decimal string` | R | Identity `bigint`, serialize thành chuỗi số |
| `name` | `string` | R | Tên |
| `address` | `string` | R | Địa chỉ |
| `timezone` | `string` | R | IANA timezone |
| `isActive` | `boolean` | R | API projection |
| `minimumTurnaroundMinutes` | `integer` | R | >=0; UC-14A |
| `version` | `string` | R | Concurrency token |

## Room

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `roomId` | `decimal string` | R | Identity `bigint`, serialize thành chuỗi số |
| `cinemaId` | `decimal string` | R | Parent (ID `bigint`, chuỗi số) |
| `code` | `string` | R | Unique trong cinema |
| `name` | `string` | R | Tên |
| `isActive` | `boolean` | R | API projection; không tự bỏ nghĩa vụ lịch chiếu |
| `version` | `string` | R | Concurrency token |

## ShowtimeQuery

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `date` | `Date` | R | Bắt buộc. `YYYY-MM-DD` theo timezone rạp; thiếu/sai format → `400 VALIDATION_ERROR` với `details.violations` (`[{field,reason,message}]`, `reason`: `required` hoặc `invalid_format`). |
| `cinemaId` | `decimal string` | O | Filter (ID `bigint`, chuỗi số) |
| `page` | `integer` | O | Mặc định 1 |
| `pageSize` | `integer` | O | Mặc định 20, tối đa 100 |

## Showtime

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `showtimeId` | `decimal string` | R | Identity `bigint`, serialize thành chuỗi số |
| `movieId` | `decimal string` | R | Phim (ID `bigint`, chuỗi số) |
| `movieTitle` | `string` | R | Tên |
| `cinemaId` | `decimal string` | R | Cinema (ID `bigint`, chuỗi số) |
| `cinemaName` | `string` | R | Tên cinema |
| `roomId` | `decimal string` | R | Room (ID `bigint`, chuỗi số) |
| `roomName` | `string` | R | Tên room |
| `officialStartsAt` | `Instant` | R | Giờ bắt đầu hiện hành |
| `officialEndsAt` | `Instant` | R | Server tính từ duration snapshot |
| `timezone` | `string` | R | IANA |
| `status` | `enum:draft,scheduled,in_progress,completed,cancelled` | R | API projection. **Public showtime (BE1 read endpoints)** chỉ gồm `scheduled` — cách diễn giải UC-07 bước 2 và R-21 (“suất đang bán”) vì tài liệu không liệt kê status; `draft/in_progress/completed/cancelled` ngoài tập public → `404 NOT_FOUND` ở detail. |
| `bookingAvailable` | `boolean` | R | Derived tại serverTime; phải kiểm tra lại khi reserve |
| `serverTime` | `Instant` | R | Thời điểm đọc |
| `version` | `string` | R | Concurrency token |

## SeatType

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `seatTypeId` | `decimal string` | R | Identity `bigint`, serialize thành chuỗi số |
| `code` | `string` | R | Stable code |
| `name` | `string` | R | Tên |
| `surcharge` | `Money` | R | Phụ thu mỗi physical seat |
| `isActive` | `boolean` | R | Định nghĩa hiện hành |
| `version` | `string` | R | Concurrency token |

## TicketCategory

Hai category luôn được duy trì; không có API tạo category thứ ba hoặc xóa/deactivate category bắt buộc.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `code` | `enum:Adult,Discounted` | R | Identity bất biến |
| `name` | `string` | R | Tên hiển thị |
| `description` | `string` | N | Mô tả |
| `version` | `string` | R | Concurrency token |

## CinemaPricing

Giá áp dụng hiện tại; không tự thêm effectiveFrom/future pricing workflow. Thiếu cấu hình giá (thiếu dòng giá hoặc thiếu category bắt buộc) → `409 STATE_CONFLICT`; `amount` = 0 là cấu hình hợp lệ.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `cinemaId` | `decimal string` | R | Cinema (ID `bigint`, chuỗi số) |
| `adultBasePrice` | `Money` | R | Không âm |
| `discountedBasePrice` | `Money` | R | Không âm |
| `version` | `string` | R | Version |
| `updatedAt` | `Instant` | R | Server timestamp |

## Seat

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `seatId` | `decimal string` | R | Identity `bigint`, serialize thành chuỗi số |
| `rowLabel` | `string` | R | Hàng |
| `seatNumber` | `integer` | R | >0 |
| `label` | `string` | R | Unique trong room |
| `x` | `integer` | R | Vị trí layout đề xuất; >=0 |
| `y` | `integer` | R | Vị trí layout đề xuất; >=0 |
| `seatType` | `SeatType` | R | Shared seat type |
| `operationalState` | `enum:active,out_of_service` | R | Physical seat state |
| `version` | `string` | R | Concurrency token |

## SeatAvailability

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `seat` | `Seat` | R | Physical data |
| `availability` | `enum:available,held,sold,unavailable` | R | Derived tại thời điểm đọc; không phải physical state |

## SeatMap

Không có capacityUnits, Couple hoặc ticket category từng ghế.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `showtimeId` | `decimal string` | R | Showtime context (ID `bigint`, chuỗi số) |
| `roomId` | `decimal string` | R | Room (ID `bigint`, chuỗi số) |
| `seats` | `SeatAvailability[]` | R | Kể cả ghế unavailable |
| `serverTime` | `Instant` | R | Snapshot timestamp |

## SeatLayout

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `roomId` | `decimal string` | R | Không cần showtime (ID `bigint`, chuỗi số) |
| `seats` | `Seat[]` | R | Physical layout |
| `version` | `string` | R | Whole-layout concurrency |

## SeatSnapshot

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `seatId` | `decimal string` | R | Physical identity (ID `bigint`, chuỗi số) |
| `label` | `string` | R | Purchase snapshot |
| `seatTypeName` | `string` | R | Snapshot |
| `surcharge` | `Money` | R | Snapshot |

## Quote

Quote không giữ ghế, không gia hạn selection. Final amount phải đáp ứng điều kiện thanh toán DOCX. Không tin tổng tiền client.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `quoteId` | `UUID` | R | Server-generated |
| `version` | `string` | R | Opaque |
| `quantities` | `Quantities` | R | Quantity dùng tính giá |
| `adultBasePrice` | `Money` | R | Giá cinema hiện hành |
| `discountedBasePrice` | `Money` | R | Giá cinema hiện hành |
| `seats` | `SeatSnapshot[]` | R | Seat surcharge |
| `total` | `Money` | R | adult*adultBasePrice + discounted*discountedBasePrice + sum(surcharge) |
| `quotedAt` | `Instant` | R | Server time |

## PriceAcceptance

Shape Proposed. Backend phải đối chiếu quote với selection, seats, quantities và giá hiện hành. PRICE_CHANGED trả quote mới, không tạo booking.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `quoteId` | `UUID` | R | Quote đã hiển thị |
| `version` | `string` | R | Version quote |
| `acceptedTotal` | `Money` | R | Số tiền user chấp nhận; không là nguồn định giá |

## CreateSelectionRequest

Tạo tại lần chọn physical seat đầu tiên; chưa cần contact hoặc đủ ghế. Không tạo booking/hold. Một active selection của checkout identity cho cùng showtime được phục hồi thay vì reset timer.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `showtimeId` | `decimal string` | R | Suất eligible (ID `bigint`, chuỗi số) |
| `quantities` | `Quantities` | R | 1..configuredMaximum |
| `seatIds` | `decimal string[]` | R | Ít nhất một ghế distinct, không vượt tổng quantity (ID `bigint`, chuỗi số) |

## UpdateSelectionRequest

Full selection edit; seat count không vượt quantity. Không đổi showtime/deadline. Reserve mới bắt buộc số ghế bằng số vé.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `version` | `string` | R | Selection version |
| `quantities` | `Quantities` | R | 1..configuredMaximum |
| `seatIds` | `decimal string[]` | R | Distinct; cho phép tạm thời ít hơn tổng quantity khi đang chọn (ID `bigint`, chuỗi số) |

## Selection

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `selectionId` | `UUID` | R | Identity |
| `showtimeId` | `decimal string` | R | Suất (ID `bigint`, chuỗi số) |
| `quantities` | `Quantities` | R | Đang chọn |
| `seatIds` | `decimal string[]` | R | Đang chọn (ID `bigint`, chuỗi số) |
| `quote` | `Quote` | N | null khi chưa đủ thông tin tính giá |
| `selectionDeadline` | `Instant` | R | 5 phút từ lần bắt đầu; không reset |
| `serverTime` | `Instant` | R | Server time |
| `state` | `enum:active,expired,reserved` | R | API projection |
| `bookingId` | `UUID` | N | Nếu đã reserve |
| `version` | `string` | R | Concurrency token |

## ReserveRequest

Backend so khớp showtimeId, seatIds và quantities với selectionVersion; mismatch bị từ chối, không âm thầm chọn một bản. Atomic reservation chỉ khi đủ ghế, còn hạn, showtime eligible, giá được chấp nhận. Identity từ token/cookie, không nhận userId.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `selectionId` | `UUID` | R | Selection của caller |
| `selectionVersion` | `string` | R | Phải hiện hành |
| `showtimeId` | `decimal string` | R | Phải khớp selection (ID `bigint`, chuỗi số) |
| `seatIds` | `decimal string[]` | R | Distinct, đủ tổng quantity, khớp selection version (ID `bigint`, chuỗi số) |
| `quantities` | `Quantities` | R | Khớp selection version |
| `contact` | `Contact` | R | Checkout Step 1 |
| `declarations` | `CheckoutDeclarations` | R | Các xác nhận |
| `priceAcceptance` | `PriceAcceptance` | R | Bắt buộc chấp nhận giá |

## Booking

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `bookingId` | `UUID` | R | Identity |
| `bookingCode` | `string` | R | Public reference; không đủ authority |
| `status` | `enum:pending,confirmed,expired,cancelled` | R | API state projection |
| `showtime` | `Showtime` | R | Current official information |
| `purchasedStartAt` | `Instant` | R | Purchase-time official start snapshot |
| `seats` | `SeatSnapshot[]` | R | Committed snapshot |
| `quantities` | `Quantities` | R | Purchased |
| `admitted` | `Quantities` | R | Committed admissions |
| `remaining` | `Quantities` | R | Derived quota; không tự khẳng định được admission |
| `financialSnapshot` | `Quote` | R | Giá đã commit; không reprice |
| `contact` | `Contact` | R | Protected booking contact |
| `selectionDeadline` | `Instant` | R | Original deadline |
| `paymentDeadline` | `Instant` | N | Khi initiated, supersedes selection deadline |
| `activePaymentId` | `UUID` | N | Attempt hiện hành |
| `cancellation` | `CancellationSummary` | N | Request hiện hành nếu có |
| `createdAt` | `Instant` | R | Server |
| `confirmedAt` | `Instant` | N | Server |
| `version` | `string` | R | Concurrency token |

## CancelPendingRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `confirmed` | `boolean` | R | true sau warning; không tự dùng tab close làm trigger |

## CheckoutRecovery

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `booking` | `Booking` | R | Authoritative |
| `payment` | `Payment` | N | Attempt hiện hành |
| `nextAction` | `enum:pay,awaitOutcome,viewTicket,selectAgain,contactSupport` | R | Derived hint; không cấp quyền |
| `serverTime` | `Instant` | R | Server |

## Payment

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `paymentId` | `UUID` | R | Identity |
| `bookingId` | `UUID` | R | Protected booking |
| `provider` | `literal:MoMo` | R | Intended gateway |
| `status` | `enum:created,pending,unknown,succeeded,failed,cancelled` | R | Financial state |
| `appliedAt` | `Instant` | N | null không đồng nghĩa failed |
| `reconciliationStatus` | `enum:none,needs_review,refund_required,resolved` | R | Reconciliation projection |
| `amount` | `Money` | R | Snapshot amount |
| `paymentDeadline` | `Instant` | N | 10 phút khi initiated thành công |
| `graceUntil` | `Instant` | N | Mặc định deadline +10 giây; không là payment extension |
| `lastVerifiedAt` | `Instant` | N | Evidence time |
| `serverTime` | `Instant` | R | Server |

## InitiatePaymentRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `provider` | `literal:MoMo` | R | Không nhận amount hoặc return URL tùy ý |

## PaymentInitiation

HTTP 201 khi tạo attempt có kết quả khởi tạo, 200 reuse đã xác định, 202 khi đã lưu attempt nhưng outcome chưa xác định. 202 không có nghĩa chưa thu tiền.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `payment` | `Payment` | R | Durable attempt |
| `redirectUrl` | `string` | N | HTTPS provider URL đã allowlist; null nếu chưa xác định |
| `nextAction` | `enum:redirect,awaitOutcome,viewTicket,contactSupport` | R | Hint |

## Ticket

Không cấp Guest access bằng QR. QR được dùng tại protected Staff admission API. Download ticket render từ DTO này ở frontend; không sinh QR mới.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `bookingId` | `UUID` | R | Booking |
| `bookingCode` | `string` | R | Reference |
| `qrCredential` | `string` | R | Một credential cho toàn booking; nhạy cảm, no-store |
| `showtime` | `Showtime` | R | Current official time |
| `purchasedStartAt` | `Instant` | R | Purchase snapshot |
| `seats` | `SeatSnapshot[]` | R | Purchased |
| `purchased` | `Quantities` | R | Purchased quota |
| `admitted` | `Quantities` | R | Admissions |
| `remaining` | `Quantities` | R | Remaining quota |
| `admissionState` | `enum:eligible,blocked,exhausted,voided` | R | Derived; eligibility còn kiểm tại UC-05 |

## Notification

Resend dùng email đã lưu ở booking; không nhận tùy ý recipient mới. Retry không phát hành ticket mới.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `requestId` | `UUID` | R | Notification identity |
| `status` | `enum:queued,sent,failed` | R | Delivery outcome |
| `acceptedAt` | `Instant` | R | Server time |

## ValidateAdmissionRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `qrCredential` | `string` | R | Không log |
| `showtimeId` | `decimal string` | O | Nếu Staff đã chọn context; backend đối chiếu (ID `bigint`, chuỗi số) |

## Restriction

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `code` | `string` | R | Business code |
| `message` | `string` | R | Thông tin phù hợp Staff |

## AdmissionValidation

Read-only validation không reserve admission quota. Backend kiểm tra lại mọi điều kiện khi confirm.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `bookingId` | `UUID` | R | Booking |
| `showtime` | `Showtime` | R | Current official |
| `seats` | `SeatSnapshot[]` | R | Seats |
| `purchased` | `Quantities` | R | Quota |
| `admitted` | `Quantities` | R | Consumed |
| `remaining` | `Quantities` | R | Remaining |
| `windowOpensAt` | `Instant` | R | Official start -5 phút |
| `windowClosesAt` | `Instant` | R | Official start +20 phút; inclusive |
| `restrictions` | `Restriction[]` | R | Rỗng khi chưa phát hiện blocker |
| `serverTime` | `Instant` | R | Authoritative time |

## ConfirmAdmissionRequest

Idempotency-Key là admissionOperationId (UUID). Staff user/cinema scope lấy từ session. Mọi check và ghi nhận quota phải nguyên tử.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `qrCredential` | `string` | R | Booking QR |
| `showtimeId` | `decimal string` | R | Context (ID `bigint`, chuỗi số) |
| `quantities` | `Quantities` | R | Tổng >0, từng category <= remaining |
| `ageVerified` | `boolean` | R | true sau Staff kiểm thực tế |
| `discountedEligibilityVerified` | `boolean` | R | true nếu có Discounted vào đợt này |

## AdmissionResult

Replay trả cùng outcome; không đổi result sang một trạng thái khác. GET history cung cấp trạng thái hiện hành nếu đã có operation sau đó.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `operationId` | `UUID` | R | Idempotency-Key |
| `bookingId` | `UUID` | R | Booking |
| `admittedThisOperation` | `Quantities` | R | Lượng vào đợt này |
| `admittedTotal` | `Quantities` | R | Tổng sau commit |
| `remaining` | `Quantities` | R | Còn lại sau commit |
| `confirmedAt` | `Instant` | R | Server commit time |

## CancellationSummary

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `requestId` | `UUID` | R | Identity |
| `status` | `enum:pending,approved,rejected` | R | Request state; refund lifecycle riêng |
| `refundId` | `UUID` | N | Financial obligation |
| `blocksAdmission` | `boolean` | R | Derived |

## CancellationRequest

Chỉ confirmed+paid, start hiện tại khác purchasedStartAt, request trước current start và zero successful admissions. Không cho chọn ghế hủy một phần.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `reason` | `string` | R | Lý do trình bày; eligibility do server kiểm |

## Cancellation

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `requestId` | `UUID` | R | Identity |
| `bookingId` | `UUID` | R | Booking |
| `status` | `enum:pending,approved,rejected` | R | API mapping Proposed; không là enum migration |
| `reason` | `string` | R | Request reason |
| `requestedAt` | `Instant` | R | Server ghi |
| `reviewedAt` | `Instant` | N | Server |
| `decisionReason` | `string` | N | Thông tin được phép |
| `refundId` | `UUID` | N | Financial obligation |
| `blocksAdmission` | `boolean` | R | Approved-awaiting-refund chặn admission |
| `version` | `string` | R | Concurrency token |

## ReviewCancellationRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `version` | `string` | R | Current request |
| `decision` | `enum:approve,reject` | R | Explicit decision |
| `reason` | `string` | R | Audit reason |

## Refund

Không trả evidence nội bộ ở response Customer. Admin/Manager đọc evidence qua operation riêng.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `refundId` | `UUID` | R | Identity |
| `bookingId` | `UUID` | R | Booking |
| `paymentId` | `UUID` | R | Original payment |
| `origin` | `enum:reconciliation,rescheduling` | R | Quyết định completion effects |
| `status` | `enum:pending,unknown,succeeded,failed` | R | Financial state |
| `amount` | `Money` | R | Full eligible obligation |
| `completedAt` | `Instant` | N | Verified completion |
| `version` | `string` | R | Concurrency token |

## RefundEvidence

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `externalReference` | `string` | R | External completion reference |
| `completedAt` | `Instant` | R | Thời điểm hoàn thực tế đã xác minh |
| `amount` | `Money` | R | Khớp toàn bộ nghĩa vụ |
| `evidenceNote` | `string` | R | Bằng chứng đủ kiểm tra; không secret |
| `recordedBy` | `UUID` | R | Actor server ghi |
| `recordedAt` | `Instant` | R | Server timestamp |

## CompleteRefundRequest

Idempotency-Key bắt buộc. Staff không có quyền. Reconciliation origin không tự cancel confirmed booking hoặc release sold seats; rescheduling origin áp dụng cancellation/void/release theo UC-04A.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `version` | `string` | R | Refund version |
| `externalReference` | `string` | R | Verified external reference |
| `completedAt` | `Instant` | R | Không ở tương lai; không chỉ là transfer initiated |
| `amount` | `Money` | R | Khớp payment/obligation |
| `evidenceNote` | `string` | R | Bằng chứng xác minh completion |

## MovieWrite

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `title` | `string` | R | Không rỗng |
| `description` | `string` | N | Mô tả |
| `durationMinutes` | `integer` | R | >0 |
| `releaseDate` | `Date` | N | Ngày |
| `posterUrl` | `string` | N | URL |
| `trailerUrl` | `string` | N | URL |
| `language` | `string` | N | Ngôn ngữ |
| `ageRating` | `string` | R | Classification hợp lệ |
| `genreIds` | `UUID[]` | R | Distinct; association mới chỉ genre active |

## CreateMovieRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `movie` | `MovieWrite` | R | New metadata |
| `status` | `enum:coming_soon,now_showing,ended` | R | Trạng thái ban đầu hợp lệ |

## UpdateMovieRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `movie` | `MovieWrite` | R | Full editable metadata |
| `version` | `string` | R | Concurrency |

## MovieStateRequest

Không cascade xóa lịch sử; archived restore phải kiểm tra eligibility.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `status` | `enum:coming_soon,now_showing,ended,archived` | R | Transition phải theo UC-12 |
| `version` | `string` | R | Concurrency |
| `reason` | `string` | R | Audit |

## GenreWrite

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `name` | `string` | R | Không rỗng/unique theo UC |
| `description` | `string` | N | Mô tả |

## UpdateGenreRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `genre` | `GenreWrite` | R | Metadata |
| `version` | `string` | R | Concurrency |

## ActiveStateRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `isActive` | `boolean` | R | Transition hợp lệ |
| `version` | `string` | R | Concurrency |
| `reason` | `string` | R | Audit |

## CinemaWrite

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `name` | `string` | R | Tên |
| `address` | `string` | R | Địa chỉ |
| `timezone` | `string` | R | IANA timezone |
| `minimumTurnaroundMinutes` | `integer` | R | >=0 |

## UpdateCinemaRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `cinema` | `CinemaWrite` | R | Admin-only metadata |
| `version` | `string` | R | Concurrency |

## TurnaroundRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `minimumTurnaroundMinutes` | `integer` | R | >=0; kiểm tra scheduling impact |
| `version` | `string` | R | Concurrency |

## RoomWrite

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `code` | `string` | R | Unique within cinema |
| `name` | `string` | R | Tên |

## UpdateRoomRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `room` | `RoomWrite` | R | Metadata |
| `version` | `string` | R | Concurrency |

## LayoutSeatInput

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `seatId` | `decimal string` | N | null cho new seat (ID `bigint`, chuỗi số) |
| `rowLabel` | `string` | R | Hàng |
| `seatNumber` | `integer` | R | >0 |
| `x` | `integer` | R | >=0 |
| `y` | `integer` | R | >=0 |
| `seatTypeId` | `decimal string` | R | Eligible existing type (ID `bigint`, chuỗi số) |

## ConfigureLayoutRequest

Omitted existing seats là yêu cầu loại bỏ có kiểm tra UC-15A; không xóa seat lịch sử đang được tham chiếu. Out-of-service change qua operation riêng. Structural change bị từ chối khi scheduling/allocation obligations không cho phép.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `version` | `string` | R | Whole-layout version |
| `seats` | `LayoutSeatInput[]` | R | Full target layout; distinct label/position |

## SeatStateRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `state` | `enum:active,out_of_service` | R | Physical state |
| `version` | `string` | R | Concurrency |
| `reason` | `string` | R | Audit |

## SeatTypeWrite

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `code` | `string` | R | Unique stable code |
| `name` | `string` | R | Tên |
| `surcharge` | `Money` | R | >=0 |

## UpdateSeatTypeRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `name` | `string` | R | Tên |
| `surcharge` | `Money` | R | >=0; không reprice booking cũ |
| `version` | `string` | R | Concurrency |

## UpdateCategoryRequest

Không đổi code, không category thứ ba, không đổi booking quantities/snapshots đã tồn tại.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `name` | `string` | R | Display name |
| `description` | `string` | N | Chỉ descriptive/classification được nguồn cho phép |
| `version` | `string` | R | Concurrency |

## ConfigurePricingRequest

Atomic cập nhật một hoặc cả hai giá qua full pair; zero base price không tự nghĩa booking zero-total được trả tiền.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `adultBasePrice` | `Money` | R | >=0 |
| `discountedBasePrice` | `Money` | R | >=0 |
| `version` | `string` | R | Concurrency |

## CreateShowtimeRequest

Server snapshot duration phim, tính endsAt và occupied interval với turnaround; không nhận endsAt/basePrice tùy ý.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `movieId` | `decimal string` | R | Eligible movie (ID `bigint`, chuỗi số) |
| `roomId` | `decimal string` | R | Eligible room; cinema suy ra (ID `bigint`, chuỗi số) |
| `startsAt` | `Instant` | R | Official start |

## UpdateShowtimeRequest

General edit chỉ khi UC-17 cho phép. Existing bookings áp dụng restricted reschedule operation, không đổi tùy ý phim/phòng.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `movieId` | `decimal string` | R | Eligible target (ID `bigint`, chuỗi số) |
| `roomId` | `decimal string` | R | Eligible target (ID `bigint`, chuỗi số) |
| `startsAt` | `Instant` | R | Start |
| `version` | `string` | R | Concurrency |

## RescheduleRequest

Server recheck overlap/turnaround và bảo toàn purchasedStartAt của booking; không auto refund/cancel booking chỉ do reschedule.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `startsAt` | `Instant` | R | New official start |
| `version` | `string` | R | Concurrency |
| `reason` | `string` | R | Audit reason |

## RoleChangeRequest

Grant yêu cầu verified email và active account theo UC-18; bảo vệ last active Admin.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `role` | `enum:STAFF,MANAGER,ADMIN` | R | Không revoke CUSTOMER permanent |
| `version` | `string` | R | User access version |
| `reason` | `string` | R | Audit |

## AssignmentRequest

Manager chỉ được thay đổi STAFF assignments ở cinema mình được giao; không cấp role hoặc MANAGER assignment.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `cinemaId` | `decimal string` | R | Cinema (ID `bigint`, chuỗi số) |
| `role` | `enum:STAFF,MANAGER` | R | Assignment kind |
| `version` | `string` | R | User access version |
| `reason` | `string` | R | Audit |

## UserStateRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `state` | `enum:active,locked` | R | Lock/unlock |
| `version` | `string` | R | User access version |
| `reason` | `string` | R | Audit |

## UserAccess

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `user` | `User` | R | Current |
| `assignments` | `CinemaAssignment[]` | R | Current |
| `version` | `string` | R | Concurrency token |

## OperationalQuery

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `cinemaId` | `decimal string` | O | Filter không mở rộng scope (ID `bigint`, chuỗi số) |
| `query` | `string` | O | Search code/contact trong scope cho phép |
| `page` | `integer` | O | >=1 |
| `pageSize` | `integer` | O | 1..100 |

## ChatContext

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `movieId` | `decimal string` | N | Public movie context (ID `bigint`, chuỗi số) |
| `showtimeId` | `decimal string` | N | Public showtime context (ID `bigint`, chuỗi số) |

## CreateChatRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `context` | `ChatContext` | O | Public context |

## ChatMessageRequest

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `message` | `string` | R | Nonempty; bound theo server policy |
| `context` | `ChatContext` | O | Public context |

## ChatMessage

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `messageId` | `UUID` | R | Identity |
| `role` | `enum:user,assistant` | R | Role |
| `content` | `string` | R | Text |
| `sourceLabels` | `string[]` | R | Nguồn cho dữ liệu truy xuất; [] khi không có |
| `createdAt` | `Instant` | R | Server |

## Conversation

Theo SAD C12: lưu tạm backend memory, không PostgreSQL; clear/expiry/restart mất history. Session logout/replacement chặn truy cập cũ.

| Field | Type | R/O/N | Validation / ý nghĩa |
|---|---|---|---|
| `conversationId` | `UUID` | R | Owner-bound |
| `messages` | `ChatMessage[]` | R | Bounded history |
| `expiresAt` | `Instant` | R | Server policy |

## MoMo provider boundary

Webhook **không** nhận một DTO nội bộ do frontend gửi. Adapter đọc raw provider request và xác minh theo contract MoMo thực tế. Chưa đặc tả payload/signature/ACK ở đây vì chưa xác minh phiên bản integration. Không tự yêu cầu provider có field eventKey. Internal event identity có thể derive từ verified provider identifiers theo mapping được review. Raw provider payload và normalized internal event không là cùng một schema.

Backend không nhận returnUrl tùy ý từ client; return target từ allowlisted configuration. Browser return không có quyền apply payment. Financial normalization phải bảo toàn verified amount/currency/reference, duplicate detection, out-of-order handling và late-success outcome.

## Ví dụ phối hợp giữa các DTO

UUID và giá dưới đây là dữ liệu minh họa; không phải production ID hay bằng chứng giao dịch.

### Bắt đầu selection — ghế đầu tiên, chưa có contact, chưa giữ ghế

POST /api/selections; gửi Idempotency-Key và bearer hoặc Guest cookie + CSRF.

```json
{
  "showtimeId": "00000000-0000-4000-8000-000000000001",
  "quantities": { "adult": 1, "discounted": 1 },
  "seatIds": ["00000000-0000-4000-8000-000000000002"]
}
```

### Cập nhật ghế đang chọn

PUT /api/selections/{selectionId}. Deadline giữ nguyên. Client không gửi giá hoặc category cho từng ghế.

```json
{
  "version": "selection-v2",
  "quantities": { "adult": 1, "discounted": 1 },
  "seatIds": ["00000000-0000-4000-8000-000000000002", "00000000-0000-4000-8000-000000000003"]
}
```

### Continue Step 1 → Step 2

POST /api/bookings, Idempotency-Key mới cho ý định reserve. Booking kế thừa selection deadline.

```json
{
  "selectionId": "00000000-0000-4000-8000-000000000004",
  "selectionVersion": "selection-v3",
  "showtimeId": "00000000-0000-4000-8000-000000000001",
  "seatIds": ["00000000-0000-4000-8000-000000000002", "00000000-0000-4000-8000-000000000003"],
  "quantities": { "adult": 1, "discounted": 1 },
  "contact": { "fullName": "Nguyen Van A", "phone": "0901234567", "email": "customer@example.com" },
  "declarations": { "termsAccepted": true, "ageRequirementsAcknowledged": true },
  "priceAcceptance": {
    "quoteId": "00000000-0000-4000-8000-000000000005",
    "version": "quote-v1",
    "acceptedTotal": { "amount": 180000, "currency": "VND" }
  }
}
```

### Payment đã lưu nhưng kết quả khởi tạo chưa rõ

Response 202 của initiatePayment; polling/replay dùng cùng paymentId, không mở attempt thứ hai.

```json
{
  "data": {
    "payment": {
      "paymentId": "00000000-0000-4000-8000-000000000006",
      "bookingId": "00000000-0000-4000-8000-000000000007",
      "provider": "MoMo",
      "status": "unknown",
      "appliedAt": null,
      "reconciliationStatus": "needs_review",
      "amount": { "amount": 180000, "currency": "VND" },
      "paymentDeadline": null,
      "graceUntil": null,
      "lastVerifiedAt": null,
      "serverTime": "2026-11-09T03:00:00Z"
    },
    "redirectUrl": null,
    "nextAction": "awaitOutcome"
  }
}
```

### Xác nhận một đợt admission

POST /api/admissions/confirm; Idempotency-Key là operation UUID. Backend không tin boolean như thay thế authorization và kiểm tra actual quota/time/state.

```json
{
  "qrCredential": "opaque-booking-qr-example",
  "showtimeId": "00000000-0000-4000-8000-000000000001",
  "quantities": { "adult": 1, "discounted": 0 },
  "ageVerified": true,
  "discountedEligibilityVerified": false
}
```

### Ghi nhận hoàn tiền bên ngoài đã được xác minh

POST /api/management/refunds/{refundId}/completion, Manager assigned cinema hoặc Admin; Idempotency-Key bắt buộc.

```json
{
  "version": "refund-v2",
  "externalReference": "external-reference-example",
  "completedAt": "2026-11-09T03:10:00Z",
  "amount": { "amount": 180000, "currency": "VND" },
  "evidenceNote": "Da doi chieu bang chung hoan tien thanh cong voi giao dich goc."
}
```

## Những khác biệt đã xử lý so với bản trước

- Không định nghĩa DTO trùng ở phần bổ sung; không để tên nested type không có định nghĩa.
- CreateSelection không yêu cầu đủ ghế/contact từ đầu; Reserve có contact đầy đủ và declarations.
- PriceAcceptance bắt buộc ở reserve; không cho optional khiến mất bước chấp nhận giá.
- Bỏ requestedAt server-owned khỏi cancellation input; pending cancel không dùng checkoutAbandoned từ đóng tab.
- Refund financial status tách khỏi cancellation approval; evidence chỉ cho policy được phép, không nằm trong Customer Refund.
- Room SeatLayout không dùng Showtime SeatMap. Không cho client tự gán showtime endsAt/basePrice.
- Không dùng arbitrary management action string; mỗi action có path/DTO/policy riêng.
- Không POST tạo ticket category thứ ba; không generic PATCH ticket/booking.
- SAD C12 đã chốt history in-memory; không đánh dấu chat persistence như quyết định chưa chốt.

## Cần chốt trước khi triển khai

Transport Guest/CSRF, limits/lifetime, quote/idempotency storage, provider payload mapping và enum-to-domain mapping là các quyết định kỹ thuật Proposed. Data validation chi tiết theo DOCX phải được mang vào implementation/tests; các bảng này không thay thế toàn bộ alternative flows của 28 UC. Không có kiểm thử runtime của backend trong lần soạn tài liệu này.
