# API Endpoints — CIN-T-011

## 1. Phạm vi và cách đọc

Danh mục phục vụ `CIN-SUB-068`..`CIN-SUB-071`: endpoint, method, dữ liệu vào/ra và quyền theo nghiệp vụ. Đây là **đề xuất contract để FE/BE review**, không phải xác nhận API đã triển khai hoặc các chính sách `TBD` đã được nhóm phê duyệt.

- [API contract](api-contract.md) định nghĩa schema có tên, trường bắt buộc/tùy chọn, validation, lỗi, phân trang và các bảo đảm backend/database. Bảng dưới chỉ tham chiếu schema, không sao chép JSON.
- [DTO specification](dto-specification.md) là bản DTO **Proposed** hiện có; các khác biệt được ghi trong API contract, không coi ví dụ UUID/provider của bản cũ là quyết định nghiệp vụ.
- URL có prefix `/api`; `{...Id}` là định danh theo contract, `{ticketCode}` là mã vé. `—` ở cột request nghĩa là không có body.
- `Page<T>` là `data: T[]` kèm `pagination`; `T[]` là mảng không phân trang. Các schema còn lại là nội dung `data`, không phải toàn bộ response. `204` không có body.
- Các danh sách phân trang nhận `page`, `pageSize`, `sort` theo allowlist của contract. Query sai kiểu/không hỗ trợ trả `400`. Bộ lọc dưới đây là đề xuất kỹ thuật, không bổ sung quyền nghiệp vụ.
- Các lỗi chung `400/422/500`, lỗi phiên `401`, lỗi quyền `403` và tài nguyên không tồn tại `404` áp dụng theo contract. Cột lỗi ghi thêm trường hợp đặc thù; không có nghĩa endpoint được bỏ kiểm tra chung.

## 2. Chính sách truy cập dùng trong bảng

| Ký hiệu | Điều kiện backend phải kiểm tra |
|---|---|
| Public | Không cần phiên; chỉ dữ liệu được phép công khai, không trả dữ liệu khách hàng, booking hoặc payment của người khác. |
| Phiên của chính mình | Một trong `CUSTOMER`, `STAFF`, `MANAGER`, `ADMIN` đang có hiệu lực; user hiện tại lấy từ phiên đã xác minh. Không nhận `userId` của client để chọn chủ thể. |
| Customer/chủ sở hữu | Có role `CUSTOMER` đang hiệu lực và `booking.user_id = currentUser.id`; payment/ticket kiểm tra quyền qua booking. Biết mã tài nguyên không đủ quyền. |
| Staff/soát vé | Có role `STAFF` đang hiệu lực; chỉ tra cứu booking/vé phục vụ soát vé và thực hiện soát vé. Không bao gồm quản lý payment, hủy booking hoặc báo cáo. |
| Admin/danh mục | Có role `ADMIN` đang hiệu lực; quản lý danh mục/phòng/ghế/suất theo các bất biến dữ liệu. `MANAGER` mới là đề xuất, chờ OQ-04, chưa tự động được cấp quyền. |
| Admin/tài khoản | Chỉ role `ADMIN` đang hiệu lực được quản lý tài khoản/cấp thu hồi role. |
| Gateway | Xác thực nguồn, chữ ký và chống phát lại theo giao thức cổng được chọn; không dùng token Customer/Staff thay chữ ký cổng. Chi tiết chờ OQ-01. |

Khi đăng nhập, tài khoản phải `active`, chỉ role đang active được đưa vào token. Token đã phát hành có thể giữ quyền đến hết hạn theo BR-AUTH-004; không cam kết kiểm tra hiện hành/thu hồi tức thời chưa triển khai. Các role không kế thừa nhau mặc định: `ADMIN`/`MANAGER` không tự có quyền Customer hoặc Staff. Người có nhiều role phải thỏa policy tương ứng. Phân quyền theo chi nhánh ngoài demo; không tự đặt điều kiện phân công chi nhánh khi schema/policy chưa có.

## 3. Xác thực và hồ sơ hiện tại

Truy vết: FR-AUTH-001..004, BR-AUTH-001..005, UC-08.

| Method | URL | Mục đích / query | Request → data | Quyền | Thành công; lỗi bổ sung |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Đăng ký; server chỉ cấp `CUSTOMER` | `RegisterRequest` → `RegisteredUser` | Public | `201`; `409 EMAIL_ALREADY_EXISTS` |
| POST | `/api/auth/login` | Tạo phiên sau xác minh tài khoản | `LoginRequest` → `AuthSession` | Public | `200`; `401 INVALID_CREDENTIALS` cho sai thông tin hoặc tài khoản không active |
| POST | `/api/auth/logout` | Kết thúc phiên hiện tại; cơ chế thu hồi theo contract/TBD | — → — | Phiên của chính mình | `204` |
| GET | `/api/users/me` | Khôi phục profile và quyền của token hiện tại sau tải lại trang; không làm mới quyền token | — → `CurrentUser` | Phiên của chính mình | `200` |
| PATCH | `/api/users/me` | Cập nhật hồ sơ; không sửa role/status từ route này | `ProfileWrite` → `CurrentUser` | Phiên của chính mình | `200` |

Đăng ký thành công không mặc nhiên cấp token như đăng nhập. Chính sách mật khẩu, thời hạn/refresh/thu hồi phiên còn `TBD` được theo dõi trong contract.

## 4. Duyệt phim, lịch chiếu và danh mục công khai

Truy vết: FR-MOVIE-*, FR-SHOW-*, UC-06/07, Customer User Flow §§3.1–3.5.

| Method | URL | Mục đích / query | Request → data | Quyền | Thành công; lỗi bổ sung |
|---|---|---|---|---|---|
| GET | `/api/movies` | Lọc `status`, tìm `q`, lọc `genreId`; phân trang/sort theo contract | — → `Page<MovieSummary>` | Public | `200` |
| GET | `/api/movies/{movieId}` | Chi tiết phim được công khai | — → `MovieDetail` | Public | `200`; `404 MOVIE_NOT_FOUND` |
| GET | `/api/movies/{movieId}/showtimes` | `date` bắt buộc theo Asia/Ho_Chi_Minh, `cinemaId` tùy chọn; phân trang/sort | — → `Page<Showtime>` | Public | `200`; `404 MOVIE_NOT_FOUND` |
| GET | `/api/showtimes/{showtimeId}` | Chi tiết, thời điểm đóng bán và khả năng đặt | — → `Showtime` | Public | `200`; `404 SHOWTIME_NOT_FOUND` |
| GET | `/api/showtimes/{showtimeId}/seats` | Sơ đồ ghế, trạng thái vật lý và phân bổ theo suất | — → `SeatMap` | Public | `200`; `404 SHOWTIME_NOT_FOUND` |
| GET | `/api/genres` | Thể loại phục vụ duyệt phim; phân trang | — → `Page<Genre>` | Public | `200` |
| GET | `/api/cinemas` | Rạp được công khai; phân trang; demo một chi nhánh | — → `Page<Cinema>` | Public | `200` |
| GET | `/api/cinemas/{cinemaId}/rooms` | Phòng phục vụ hiển thị lịch; phân trang | — → `Page<Room>` | Public | `200` |
| GET | `/api/seat-types` | Loại ghế, capacity và phụ thu; phân trang | — → `Page<SeatType>` | Public | `200` |
| GET | `/api/ticket-types` | Loại vé đang được phép chọn; phân trang | — → `Page<TicketType>` | Public | `200` |

Sơ đồ ghế chỉ là ảnh chụp khả dụng, không tạo hold. Việc chọn ghế/loại vé trên UI không cần endpoint ghi dữ liệu riêng; backend giữ ghế khi tạo booking thành công. API không tiết lộ người đang giữ ghế. Giá xem trước và điều kiện vé giảm giá theo `TBD` của contract/OQ-08.

## 5. Booking và lịch sử của Customer

Truy vết: UC-01/10, FR-BOOK-*, BR-BOOK-001..010; Customer User Flow §§3.6, 3.9, 5.

| Method | URL | Mục đích / query | Request → data | Quyền | Thành công; lỗi bổ sung |
|---|---|---|---|---|---|
| POST | `/api/bookings` | Tạo booking, giữ toàn bộ ghế; `requestKey` bắt buộc | `BookingCreate` → `Booking` | Role `CUSTOMER`; chủ booking lấy từ phiên | `201` mới, `200` replay; `409 SEAT_ALREADY_TAKEN`, `SHOWTIME_CLOSED`, `REQUEST_KEY_CONFLICT` |
| GET | `/api/bookings/{bookingId}` | Booking, giá snapshot, vé và ngữ cảnh thanh toán của mình | — → `Booking` | Customer/chủ sở hữu | `200`; `404 BOOKING_NOT_FOUND` |
| POST | `/api/bookings/{bookingId}/cancel` | Hủy toàn bộ booking `pending` theo lifecycle | — → `Booking` | Customer/chủ sở hữu | `200`; `409 BOOKING_NOT_CANCELLABLE` nếu confirmed/đang thu tiền/chưa xác minh an toàn |
| GET | `/api/users/me/bookings` | Lịch sử phân trang; lọc `status`, sort theo contract | — → `Page<BookingSummary>` | Role `CUSTOMER`; chỉ booking của current user | `200`; không có booking trả mảng rỗng |

Không nhận giá, trạng thái hoặc `userId` để tạo/hủy booking. Có payment `created`/`pending`/`unknown` thì không giải phóng ghế trước khi backend xác minh an toàn với cổng. Không cung cấp hủy từng ghế, chuyển chủ booking hoặc hủy booking `confirmed` trong demo.

## 6. Payment và callback của cổng

Truy vết: UC-09/02/03/04, FR-PAY-*, BR-PAY-001..008.

| Method | URL | Mục đích / query | Request → data | Quyền | Thành công; lỗi bổ sung |
|---|---|---|---|---|---|
| POST | `/api/payments` | Khởi tạo hoặc lấy lại attempt hợp lệ của booking | `PaymentCreate` → `Payment` | Customer/chủ sở hữu | Đề xuất `201` mới, `200` attempt hiện có theo UC-09; `409 BOOKING_EXPIRED`, `BOOKING_ALREADY_CONFIRMED` |
| GET | `/api/payments/{paymentId}` | Trạng thái đã xác minh; phân biệt succeeded với applied | — → `Payment` | Customer/chủ sở hữu | `200`; `404` payment không tồn tại |
| POST | `/api/payments/callback` | Nhận sự kiện cổng; giữ route cũ, wire protocol chờ OQ-01 | `ProviderCallback` → `ProviderAck` (**TBD**) | Gateway | HTTP/ACK cuối cùng **TBD** theo cổng; baseline cũ `200` hợp lệ/lặp, `400` malformed; chữ ký không hợp lệ bị từ chối |

**TBD cần chốt:** DTO cũ trả `409 PAYMENT_ATTEMPT_OPEN` khi có attempt mở; UC-09 yêu cầu dùng lại hoặc trả trạng thái attempt hiện có. Đề xuất `200` cho attempt có thể dùng lại, không tạo attempt thứ hai; FE/BE cần phê duyệt trước triển khai. Lỗi không thể dùng lại và idempotency payment xem contract.

Redirect về trình duyệt không xác nhận đã thanh toán. Customer đọc lại payment/booking từ backend; vé chỉ có sau khi payment được áp dụng hợp lệ. Callback lặp không phát hành thêm vé. Không đưa API cancel/query của cổng hoặc worker đối soát nội bộ thành API công khai Customer.

## 7. Tra cứu phục vụ soát vé và soát vé

Truy vết: FR-ADMIN-BOOK-001, FR-TICKET-001/002, UC-05; OQ-03, OQ-04, OQ-UC-07.

| Method | URL | Mục đích / query | Request → data | Quyền | Thành công; lỗi bổ sung |
|---|---|---|---|---|---|
| GET | `/api/tickets/{ticketCode}` | Đọc vé của mình hoặc tra cứu phục vụ soát vé | — → `Ticket` (chủ sở hữu) / `AdmissionTicket` (Staff) | Customer/chủ sở hữu **hoặc** Staff/soát vé | `200`; `404 TICKET_NOT_FOUND` |
| POST | `/api/tickets/{ticketCode}/validate` | Soát vé một lần | — → `TicketValidation` | Staff/soát vé | `200`; `409 TICKET_ALREADY_USED`, `422` vé voided/sai suất/ngoài cửa sổ được cấu hình |
| GET | `/api/manage/bookings` | `bookingCode` bắt buộc, khớp chính xác; chỉ dữ liệu soát vé | — → `AdmissionBooking[]` (0 hoặc 1 phần tử) | Staff/soát vé | `200`; không mở tìm kiếm khách/payment hàng loạt |

Admin/Manager chỉ soát vé khi có role `STAFF` phù hợp. Cửa sổ soát vé (OQ-03) và việc Staff bắt buộc chọn suất trước (OQ-UC-07) chưa chốt; baseline validate chưa nhận body. Nếu chốt ngữ cảnh suất client chọn, cần bổ sung contract truyền/kiểm tra ngữ cảnh trước triển khai, không bỏ kiểm tra cửa sổ. Tra cứu rộng theo khách, quyền xem payment và quyền Manager/Admin đọc booking chi tiết chờ OQ-04.

## 8. Quản trị danh mục, phòng/ghế và lịch chiếu

Truy vết: FR-MGT-MOVIE-001/002, FR-MGT-ROOM-001, FR-MGT-ST-001..003, BR-ROOM-001..004, BR-ST-001..005. Route bổ sung là **Proposed**; quyền Admin có căn cứ, quyền Manager còn `TBD OQ-04`. Danh sách quản trị phân trang, sort theo contract; được xem các trạng thái không công khai.

| Method | URL | Mục đích / query | Request → data | Quyền | Thành công; lỗi bổ sung |
|---|---|---|---|---|---|
| GET | `/api/manage/movies` | Phân trang; `q`, `status`, `genreId` | — → `Page<MovieSummary>` | Admin/danh mục | `200` |
| GET | `/api/manage/movies/{movieId}` | Đọc đầy đủ phim để sửa, gồm trạng thái không công khai | — → `MovieDetail` | Admin/danh mục | `200`; `404 MOVIE_NOT_FOUND` |
| POST | `/api/manage/movies` | Tạo phim | `MovieWrite` → `MovieDetail` | Admin/danh mục | `201` |
| PUT | `/api/manage/movies/{movieId}` | Sửa phim/trạng thái, giữ lịch sử | `MovieWrite` → `MovieDetail` | Admin/danh mục | `200`; `409` xung đột dữ liệu đã dùng |
| DELETE | `/api/manage/movies/{movieId}` | Chỉ xóa phim chưa được tham chiếu; đã dùng chuyển trạng thái qua PUT | — → — | Admin/danh mục | `204`; `409` đang được tham chiếu |
| GET | `/api/manage/genres` | Thể loại phân trang | — → `Page<Genre>` | Admin/danh mục | `200` |
| POST | `/api/manage/genres` | Tạo thể loại | `GenreWrite` → `Genre` | Admin/danh mục | `201`; `409` trùng định danh duy nhất |
| PUT | `/api/manage/genres/{genreId}` | Sửa thể loại | `GenreWrite` → `Genre` | Admin/danh mục | `200`; `409` trùng định danh duy nhất |
| DELETE | `/api/manage/genres/{genreId}` | Xóa thể loại chưa được phim tham chiếu | — → — | Admin/danh mục | `204`; `409` đang được tham chiếu |
| GET | `/api/manage/cinemas` | Rạp phân trang | — → `Page<Cinema>` | Admin/danh mục | `200` |
| POST | `/api/manage/cinemas` | Đề xuất cấu hình rạp; vận hành thêm chi nhánh ngoài demo, chỉ mở sau chốt TBD-20 | `CinemaWrite` → `Cinema` | Admin/danh mục; scope TBD-20 | Dự kiến `201`; `409 DUPLICATE_RESOURCE` |
| PUT | `/api/manage/cinemas/{cinemaId}` | Sửa rạp/trạng thái | `CinemaWrite` → `Cinema` | Admin/danh mục | `200`; `409` phụ thuộc chưa xử lý |
| GET | `/api/manage/cinemas/{cinemaId}/rooms` | Phòng phân trang | — → `Page<Room>` | Admin/danh mục | `200` |
| POST | `/api/manage/cinemas/{cinemaId}/rooms` | Tạo phòng | `RoomWrite` → `Room` | Admin/danh mục | `201`; `409` trùng mã/tên phòng |
| PUT | `/api/manage/rooms/{roomId}` | Sửa phòng/trạng thái; không nhận tổng capacity từ client | `RoomWrite` → `Room` | Admin/danh mục | `200`; `409` suất/booking bị ảnh hưởng chưa xử lý |
| GET | `/api/manage/rooms/{roomId}/seats` | Ghế vật lý của phòng; phân trang | — → `Page<Seat>` | Admin/danh mục | `200` |
| POST | `/api/manage/rooms/{roomId}/seats` | Thêm ghế khi cấu trúc phòng chưa khóa | `SeatWrite` → `Seat` | Admin/danh mục | `201`; `409` trùng vị trí/cấu trúc đã khóa |
| PUT | `/api/manage/seats/{seatId}` | Sửa ghế theo khóa cấu trúc | `SeatWrite` → `Seat` | Admin/danh mục | `200`; `409` cấu trúc đã khóa/thay đổi không an toàn |
| DELETE | `/api/manage/seats/{seatId}` | Xóa ghế chưa dùng khi cấu trúc chưa khóa | — → — | Admin/danh mục | `204`; `409` đã khóa/được tham chiếu |
| GET | `/api/manage/seat-types` | Loại ghế phân trang | — → `Page<SeatType>` | Admin/danh mục | `200` |
| POST | `/api/manage/seat-types` | Tạo loại ghế | `SeatTypeWrite` → `SeatType` | Admin/danh mục | `201`; `409` trùng mã |
| PUT | `/api/manage/seat-types/{seatTypeId}` | Sửa loại ghế; bảo toàn snapshot/khóa cấu trúc | `SeatTypeWrite` → `SeatType` | Admin/danh mục | `200`; `409` thay đổi không an toàn |
| GET | `/api/manage/ticket-types` | Loại vé phân trang | — → `Page<TicketType>` | Admin/danh mục | `200` |
| POST | `/api/manage/ticket-types` | Tạo loại vé | `TicketTypeWrite` → `TicketType` | Admin/danh mục | `201`; `409` trùng mã |
| PUT | `/api/manage/ticket-types/{ticketTypeId}` | Sửa loại vé/trạng thái; giữ giá snapshot booking | `TicketTypeWrite` → `TicketType` | Admin/danh mục | `200`; `409` thay đổi xung đột |
| GET | `/api/manage/showtimes` | Suất chiếu phân trang | — → `Page<Showtime>` | Admin/danh mục | `200` |
| POST | `/api/manage/showtimes` | Tạo suất và khoảng chiếm dụng phòng | `ShowtimeWrite` → `Showtime` | Admin/danh mục | `201`; `409 SHOWTIME_ROOM_OVERLAP` |
| PUT | `/api/manage/showtimes/{showtimeId}` | Sửa suất theo BR-ST-005 | `ShowtimeWrite` → `Showtime` | Admin/danh mục | `200`; `409 SHOWTIME_ROOM_OVERLAP` hoặc bị khóa bởi booking |

Giá/trạng thái được sửa có chủ đích qua schema quản trị; lệnh đặt vé/thanh toán không nhận giá/trạng thái client tự quyết định. Không xóa dây chuyền booking/payment/ticket. Suất có booking `pending` cần chốt chính sách sửa; không suy từ quy tắc cấm đổi phim/giờ/phòng khi có booking `confirmed` rằng mọi trường khác đều được sửa.

## 9. Quản trị tài khoản và vai trò

Truy vết: FR-ADMIN-USER-001, BR-AUTH-003. Các route bổ sung là đề xuất kỹ thuật cho quyền Admin đã được xác nhận.

| Method | URL | Mục đích / query | Request → data | Quyền | Thành công; lỗi bổ sung |
|---|---|---|---|---|---|
| GET | `/api/manage/users` | Danh sách tài khoản phân trang | — → `Page<ManageUser>` | Admin/tài khoản | `200` |
| GET | `/api/manage/users/{userId}` | Tài khoản/role; không trả password hash | — → `ManageUser` | Admin/tài khoản | `200` |
| PATCH | `/api/manage/users/{userId}/status` | Khóa/mở khóa theo policy | `UserStatusWrite` → `ManageUser` | Admin/tài khoản | `200`; `409` transition bị policy từ chối |
| PUT | `/api/manage/users/{userId}/roles` | Cấp/thu hồi tập role; ghi người thực hiện từ phiên | `UserRolesWrite` → `ManageUser` | Admin/tài khoản | `200`; `422` role không tồn tại/không active, `409` thay đổi bị policy từ chối |

Tự khóa/tự thu hồi quyền Admin, bảo vệ Admin cuối cùng và hiệu lực phiên sau thay đổi quyền chưa có quyết định đầy đủ: xem `TBD` trong contract, không tự tạo ngoại lệ authorization.

## 10. Nghiệp vụ chưa có contract triển khai hoặc ngoài demo

Các dòng dưới là mục truy vết, **không phải endpoint sẵn sàng gọi**. Không cấp quyền chỉ dựa trên route đề xuất.

| Nghiệp vụ / route dự kiến | Trạng thái | Quyền, request/response và điều cần chốt |
|---|---|---|
| Xem payment quản trị / đối soát thủ công | **TBD OQ-04**; đối soát tự động UC-04 là tác vụ backend | Quyền Manager/Admin chưa chốt; schema thao tác thủ công/audit TBD. Staff không có quyền mặc định. Không có API tùy ý PATCH trạng thái payment. |
| `GET /api/manage/reports/revenue` | **Proposed/TBD OQ-04** | Quyền Manager/Admin, kỳ báo cáo, múi giờ, số tiền succeeded hay applied, cách tính refund, schema và status code cần xác nhận; chưa có `RevenueReport` để FE triển khai. |
| `POST /api/manage/showtimes/{showtimeId}/cancel` (route cũ) | **Deferred/TBD OQ-05** | Không công bố như thao tác demo hoàn chỉnh. Phải lưu lý do/thời điểm; chưa chốt xử lý booking/khách đã trả tiền. Workflow thông báo/đổi lịch/hoàn tiền do hủy suất ngoài demo. Phạm vi chỉ hủy suất chưa có booking cũng cần xác nhận. |
| Hủy booking `confirmed` bởi Customer | **Ngoài demo** | Không có endpoint; không mở rộng `/cancel` của booking pending. |
| Refund / xử lý tài chính payment thành công muộn | **Ngoài demo; UC-04A, OQ-05/07, OQ-04** | Chưa có route, role phê duyệt hay DTO refund được chốt. Vẫn phải lưu payment succeeded/event, không applied trái phép, không lấy lại ghế đã bán hoặc phát hành vé sai. Quy tắc hành vi refund xem contract. |
| `POST /api/chat/messages` | **Proposed/TBD UC-11** | UC-11 mô tả Promethe-Nous nhưng còn mâu thuẫn phạm vi với architecture. Customer chỉ nhận dữ liệu được phép công khai; không đọc báo cáo nội bộ/tự đổi booking/payment/ghế/vé. `ChatRequest`, `ChatReply`, phiên hội thoại, nguồn dữ liệu và status code TBD. |

## 11. Truy vết và bàn giao

| Sub-task | Nội dung trong cặp tài liệu |
|---|---|
| CIN-SUB-068 | Method/URL/status và schema tham chiếu tại đây; envelope/quy tắc chung trong contract. |
| CIN-SUB-069 | Nhóm nghiệp vụ §§3–10; phân biệt đề xuất, chưa chốt và ngoài demo. |
| CIN-SUB-070 | Schema có tên từng dòng; kiểu dữ liệu, bắt buộc/tùy chọn và validation trong contract. |
| CIN-SUB-071 | Policy §2, quyền từng dòng, phạm vi Customer/Staff/Admin và quyền Manager/Gateway còn TBD. |

Nguồn: [requirements](../requirements/business-system-analysis.md), [use cases](../requirements/use-cases.md), [architecture](../analysis%20and%20design/architecture.md), [database design](../../database/database-design.md), [database schema](../../database/database-schema.md), [customer user flow](../ui-ux/customer-user-flow.md), [screen–API mapping](../ui-ux/customer-screen-api-mapping.md).

Customer User Flow định nghĩa **Booking Confirmation trước Payment**, rồi **Payment Result → My Tickets**; screen/navigation/mapping cũ còn gọi trang sau thanh toán là Confirmation. API dùng trạng thái backend để quyết định thành công. Dependency đồng bộ UI/DTO cũ và câu hỏi `TBD` được ghi trong [API contract](api-contract.md).
