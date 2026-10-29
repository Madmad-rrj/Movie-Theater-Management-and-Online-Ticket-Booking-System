# Customer Screen — API Mapping

## 1. Phạm vi — CIN-SUB-065

Mapping dữ liệu cho đủ 12 màn hình logic trong [đặc tả màn hình](customer-screen-specification.md), phục vụ phối hợp UI và API. Nguồn endpoint là [API Endpoints](../api/api-endpoints.md), nguồn field là [DTO Specification](../api/dto-specification.md), quy ước là [API Contract](../api/api-contract.md). Endpoint/DTO hiện ở mức **Proposed**, chưa phải bằng chứng backend đã triển khai.

“Bắt buộc” dưới đây nghĩa là UI cần dữ liệu đó để hoàn thành chức năng; không tự biến thành constraint request hoặc cam kết field đã có trong response. “Tùy chọn” là có thể thiếu và có cách hiển thị thay thế. Nếu dữ liệu cần nhưng chưa có contract, ghi **TBD** để người phụ trách API quyết định, không lấy tên cột database làm field JSON.

## 2. Quy ước dữ liệu và quyền

- Field API dùng camelCase, ví dụ `startsAt`, `expiresAt`, `totalAmount`, `ticketCode`. `bookingId`/`paymentId` định danh tài nguyên; `bookingCode`/`ticketCode` là mã hiển thị, không thay thế kiểm tra ownership.
- Response chung theo contract có `data`, danh sách phân trang có `pagination`. Một số ví dụ DTO viết trực tiếp object; cần thống nhất envelope khi chốt API, không mặc định có hai format cùng được duyệt.
- Phân trang dùng `page` từ 1, `pageSize` mặc định 20, tối đa 100; metadata gồm `page`, `pageSize`, `totalItems`, `totalPages`. Chỉ áp dụng tại nguồn đã xác định có phân trang.
- Thời gian nhận UTC ISO 8601, hiển thị `Asia/Ho_Chi_Minh`. VND là số nguyên đồng kèm `currency`. Không biến giá chưa đọc được thành 0 đồng hoặc lấy đồng hồ client làm nguồn trạng thái.
- Public: phim/lịch/sơ đồ/danh mục theo endpoint dự kiến. Request đơn, payment, vé và tài khoản dùng phiên hợp lệ; backend kiểm tra role/ownership. Không đưa token, mật khẩu hoặc dữ liệu nội bộ vào nội dung hiển thị/log.
- Dữ liệu trước tạo booking là draft; giá cuối, snapshot, hạn giữ và trạng thái do server trả sau tạo. Không gửi giá/giảm giá/status từ frontend. Mỗi screen phải xử lý loading/empty/error theo [UI State](customer-ui-state.md).

## 3. Mapping theo màn hình

### 3.1. KH-01 — Home / danh sách phim

| Hạng mục | Dữ liệu / nguồn / cách dùng |
|---|---|
| Đầu vào | Nhóm `status` now_showing/coming_soon, `page`, `pageSize`. Không tự thêm query tìm tên, thể loại hoặc sort chưa được mô tả. |
| API và trigger | `GET /api/movies` khi vào trang, đổi nhóm hoặc chuyển trang; public. Gắn response với đúng nhóm/trang đang xem. |
| Bắt buộc | `id`, `title`, `status` để mở đúng phim và gắn nhãn; `durationMinutes`, `ageRating` theo DTO để cung cấp thông tin; `pagination` cho điều hướng trang. Field cần mà thiếu phải có phản hồi phù hợp, không tự suy từ phim khác. |
| Tùy chọn / thay thế | `posterUrl`: ảnh thiếu/lỗi dùng placeholder; không chặn xem chi tiết. Banner quảng cáo và nguồn dữ liệu banner chưa có contract. |
| Lỗi / rỗng | `data: []` là chưa có phim trong nhóm; lỗi mạng/server có retry GET. Không render danh sách nhóm trước như dữ liệu nhóm mới. |

### 3.2. KH-02 — Movie Detail

| Hạng mục | Dữ liệu / nguồn / cách dùng |
|---|---|
| API và trigger | `GET /api/movies/{movieId}` khi mở/đổi movieId, public; không cần POST để xem. |
| Bắt buộc | Các field Movie Summary, thêm `description`, `releaseDate`, `language`, `genres` theo DTO; dùng title/id để giữ ngữ cảnh khi mở Showtime. Không tự điền thời lượng/tuổi/ngôn ngữ nếu thiếu. |
| Tùy chọn / thay thế | `posterUrl`, `trailerUrl`: thiếu thì placeholder hoặc ẩn thao tác trailer; không tạo URL trailer giả. Tính bắt buộc/nullability từng field cần API chốt. |
| Lỗi | 404 `MOVIE_NOT_FOUND` khác lỗi kết nối; retry lỗi đọc, cho về Home. Không tự suy phim bị xóa từ mọi response lỗi. |

### 3.3. KH-03 — Showtime

| Hạng mục | Dữ liệu / nguồn / cách dùng |
|---|---|
| API và trigger | `GET /api/movies/{movieId}/showtimes?date=...` khi mở lịch hoặc đổi ngày. Movie Detail cung cấp ngữ cảnh phim; ngày là lựa chọn UI. |
| Bắt buộc | `id`, `cinemaRoomId`, `cinemaRoomName`, `startsAt`, `basePrice`, `currency`, `status`, `bookingAvailable`. Chọn suất dùng id; khả năng bán đọc từ backend, không tính cutoff riêng. |
| Còn thiếu | Tên/địa chỉ rạp, liên kết phòng với rạp và cách nhóm nhiều rạp cần DTO cụ thể. `GET /api/cinemas`, `GET /api/cinemas/{cinemaId}/rooms` đã được liệt kê nhưng chưa có DTO để tự ghép chắc chắn. |
| Lỗi / rỗng | Không có suất trong ngày → empty; lỗi GET → retry; suất không còn bán chỉ disabled theo dữ liệu có căn cứ. Không gọi tất cả 404 là suất hủy. |

### 3.4. KH-04 — Seat Selection

| Hạng mục | Dữ liệu / nguồn / cách dùng |
|---|---|
| API và trigger | `GET /api/showtimes/{showtimeId}/seats` khi mở/đổi suất, tải lại sau xung đột hoặc quay lại cần xác minh. `GET /api/showtimes/{showtimeId}` có trong danh mục nhưng DTO chi tiết còn TBD. |
| Bắt buộc | `showtimeId`, `rows[].rowLabel`; trong từng ghế có `id`, `seatNumber`, `seatTypeCode`, `capacityUnits`, `surchargeAmount`, `status` available/held/sold. Dùng id cho draft, nhãn cho hiển thị; một Couple là một seatId. |
| Còn thiếu | Trạng thái ghế vật lý độc lập với phân bổ; vị trí/lối đi/hướng màn hình nếu muốn bố trí đúng phòng; ngữ cảnh phim/rạp/phòng khi truy cập trực tiếp. Không tự thêm enum maintenance vào `status` của DTO hiện tại. |
| State cục bộ | `selectedSeatIds`, ánh xạ loại vé cho ghế còn được chọn; chưa có booking/giữ ghế/countdown. Chuyển KH-09 và KH-06, không POST theo từng click ghế. |
| Lỗi / tải lại | Lỗi đọc sơ đồ không phải hết ghế; response cũ khác showtimeId không được áp dụng. Quyết định refresh/polling và thông tin thời gian server còn TBD, không giả có WebSocket. |

### 3.5. KH-09 — Ticket Type

| Hạng mục | Dữ liệu / nguồn / cách dùng |
|---|---|
| API và trigger | `GET /api/ticket-types` đã được liệt kê, chưa có DTO. Tải khi cần gán loại vé; thông tin ghế lấy từ KH-04. `GET /api/seat-types` cũng chưa có DTO chi tiết. |
| Bắt buộc để triển khai | Định danh, tên, trạng thái hoạt động của loại vé; dữ liệu giá/giảm giá server cho phép hiển thị; quy tắc áp dụng theo loại ghế/Customer. Tên field JSON và cấu trúc cụ thể **TBD**. |
| Cục bộ / không gửi | Gán một ticketTypeId cho mỗi seatId; số chỗ dùng capacityUnits, không gửi số lượng hai lần cho Couple. Không gửi discountPercent hoặc giá tự tính. |
| Giá và dữ liệu thiếu | Chưa có API báo giá trước tạo booking. Nếu hiển thị dự kiến phải có nguồn server và nhãn dự kiến; không tự thêm endpoint quote hoặc coi bảng giá seed là giá cuối. Eligibility Student/Child còn OQ-08. |
| Lỗi / rỗng | Không tải được danh mục thì retry GET; danh mục hợp lệ rỗng/không có loại phù hợp thì chặn tiếp và giải thích, không tự mặc định Adult từ tên hiển thị. |

### 3.6. KH-06 — Booking Confirmation / rà soát trước tạo đơn

| Hạng mục | Dữ liệu / nguồn / cách dùng |
|---|---|
| Đầu vào | Draft gồm showtimeId, ghế và ánh xạ loại vé; context phim/lịch từ các bước trước phải khớp id. Dữ liệu liên hệ và báo giá trước đặt còn thiếu contract. |
| Bắt buộc hiển thị | Phim, rạp/phòng, ngày/giờ, ghế, loại vé, số chỗ và tiền có nguồn rõ. Trước tạo không có mã đơn/hạn giữ/giá chốt; không dựng những giá trị này ở client. |
| Tạo booking | Khi Customer đã đăng nhập, rà soát và nhấn xác nhận: `POST /api/bookings` với `showtimeId`, `requestKey`, `items: [{seatId, ticketTypeId}]`. Không có API confirm riêng. |
| Response | 201 mới / 200 replay cùng key: `bookingId`, `bookingCode`, `status`, `expiresAt`, `totalAmount`, `currency`, `details[]` gồm `seatLabel`, `seatTypeName`, `ticketTypeName`, `unitPrice`. Chuyển KH-05 theo trạng thái thực. |
| Lỗi | 409 `SEAT_ALREADY_TAKEN`, `SHOWTIME_CLOSED`, `REQUEST_KEY_CONFLICT`; 400/422 validation theo contract. Xung đột ghế không giữ một phần; timeout chưa chứng minh tạo thất bại, giữ key/payload. |
| Ngữ cảnh đơn đã có | `GET /api/bookings/{bookingId}` để đọc snapshot; không biến Back thành chỉnh sửa booking. Đơn confirmed xem ở kết quả/chi tiết đơn/vé, không lặp lại POST. |

### 3.7. KH-05 — Payment

| Hạng mục | Dữ liệu / nguồn / cách dùng |
|---|---|
| Đọc đơn | `GET /api/bookings/{bookingId}` khi mở/refresh/quay lại; chủ booking. Bắt buộc bookingId/Code, status, expiresAt, totalAmount, currency và details theo response đã nêu. |
| Tạo attempt | Chỉ khi Customer nhấn thanh toán và booking còn hợp lệ: `POST /api/payments`, request `{bookingId, provider}`. Provider còn OQ-01; giá trị vnpay trong ví dụ DTO không phải quyết định chọn cổng. |
| Response attempt | `paymentId`, `status`, `amount`, `currency`, `payUrl`. Giữ paymentId đúng booking để đọc lại; không coi response tạo là thanh toán thành công. Cơ chế mở/quay về payUrl cần chốt với cổng. |
| Theo dõi | `GET /api/payments/{paymentId}` khi cần kiểm tra kết quả và sau quay về; polling/backoff/khôi phục attempt còn TBD. Không POST callback từ trình duyệt. |
| Hủy đơn | Khi Customer xác nhận hủy: `POST /api/bookings/{bookingId}/cancel`; 200 booking cancelled, 409 `BOOKING_NOT_CANCELLABLE`. Trường hợp attempt mở cần quy trình Query/Cancel backend; cách phản hồi bất đồng bộ chưa có contract đầy đủ. |
| Lỗi / điều kiện | `PAYMENT_ATTEMPT_OPEN`, `BOOKING_EXPIRED`, `BOOKING_ALREADY_CONFIRMED`: đọc lại, không tạo attempt mới mù. Mọi created/pending/unknown đều là attempt mở theo nghiệp vụ, dù ví dụ lỗi DTO chỉ nhắc pending/unknown. Countdown 0 không tự expired. |

### 3.8. KH-10 — Payment Result

| Hạng mục | Dữ liệu / nguồn / cách dùng |
|---|---|
| API và trigger | GET payment và GET booking theo id được phép khi vào/refresh, sau cổng quay về hoặc kiểm tra lại. Không tin trạng thái/amount do URL quay về tự khai. |
| Bắt buộc | Payment `paymentId`, `bookingId`, `status`, `amount`, `currency`; booking status, mã, details/tickets để phân biệt succeeded với confirmed/đã phát hành vé. So khớp hai nguồn cùng booking. |
| Còn thiếu | Bằng chứng payment đã applied, trạng thái reconciliation, quan hệ tìm attempt từ booking khi reload và hết phiên. `appliedAt`/`reconciliationStatus` chưa có trong DTO GET payment; cần API chốt cách công bố, không đọc trực tiếp database. |
| Tùy chọn | Lý do/thời điểm giao dịch nếu API bổ sung; hiện không tự đặt tên field. Không hiển thị nội dung gateway chưa xác minh như kết luận tài chính. |
| Lỗi / chuyển tiếp | Lỗi GET cho retry đọc; unknown/pending/created không cho attempt mới; failed/cancelled chỉ retry nếu đơn còn pending/còn hạn và lần cũ đã chốt. Succeeded chưa áp dụng không có vé; trường hợp muộn cần đối soát. |

### 3.9. KH-07 — Đăng nhập / đăng ký / đăng xuất

| Thao tác | Request / response đã mô tả | UI và giới hạn |
|---|---|---|
| Đăng nhập | `POST /api/auth/login`: email, password → token và user `{id, email, fullName}` | Submit form; lỗi 401 `INVALID_CREDENTIALS` không đồng nghĩa phiên hiện tại hết hạn. Response chưa có role/status/refresh token; không tự suy role từ email. |
| Đăng ký | `POST /api/auth/register`: email, password, fullName, phone → 201 id, email, fullName, createdAt | Công khai chỉ tạo CUSTOMER. 400 validation, 409 `EMAIL_ALREADY_EXISTS`. Response không có token; thành công chưa phải đăng nhập. Nullability/validation chi tiết cần chốt. |
| Đăng xuất | `POST /api/auth/logout` trong danh mục, 204; chưa có DTO chi tiết | Khi Customer chọn đăng xuất; xóa nội dung/draft private và phiên cục bộ. Không gọi logout để hủy booking/payment. Chính sách thu hồi token khi lỗi mạng cần chốt. |

Đích quay lại là state điều hướng nội bộ đã kiểm tra, không phải dữ liệu server cho phép tự chuyển tới mọi URL. Không lưu mật khẩu để khôi phục sau chuyển bước; không tự lặp POST booking/payment khi login thành công.

### 3.10. KH-08 — Booking History / chi tiết booking

| Hạng mục | Dữ liệu / nguồn / cách dùng |
|---|---|
| API và trigger | `GET /api/users/me/bookings?page=...&pageSize=...` khi mở/đổi trang; `GET /api/bookings/{bookingId}` khi chọn chi tiết và trước thao tác cần trạng thái mới. |
| Bắt buộc | Định danh/mã booking, status, totalAmount, currency, createdAt, details và metadata phân trang; item được DTO mô tả là tóm tắt của chi tiết, tập field cuối cần API chốt. |
| Ngữ cảnh / tùy chọn | Phim/rạp/phòng/giờ chiếu cần cho người dùng nhận diện; chưa được định nghĩa đầy đủ trong DTO booking. confirmedAt/cancelledAt/cancellationReason/tickets chỉ dùng khi có và phù hợp trạng thái. |
| Phạm vi | Danh sách mọi đơn của chính mình gồm pending/confirmed/expired/cancelled; không lọc lấy confirmed rồi gọi là toàn bộ lịch sử. |
| Còn thiếu / lỗi | Query filter status/date, sort, cấu trúc summary cuối còn TBD. Không lọc một trang rồi tuyên bố đã lọc toàn bộ. Rỗng là GET hợp lệ trả []; 401/403/lỗi mạng xử lý riêng. |

### 3.11. KH-11 — My Tickets / chi tiết vé

| Hạng mục | Dữ liệu / nguồn / cách dùng |
|---|---|
| Nguồn đã mô tả | `GET /api/bookings/{bookingId}` có tickets `{ticketCode, seatLabel, qrCodeUrl, status}` cho đơn được phép; `GET /api/tickets/{ticketCode}` có ticketCode, seatLabel, status, bookingCode, showtimeStartsAt. |
| Bắt buộc | Mỗi vé có mã, ghế, trạng thái valid/used/voided và ngữ cảnh phim/rạp/phòng/giờ để nhận diện; QR để xuất trình vé khi dữ liệu đã phát hành. Couple có hai mã độc lập, không gộp ảnh QR. |
| Tùy chọn / thiếu | Thời điểm soát/hủy nếu được phép công bố chưa có trong DTO GET ticket. QR có trong booking nhưng chưa có trong GET ticket; quy cách nội dung QR, capacity/số thứ tự vé và ngữ cảnh đầy đủ cần bổ sung contract. |
| Danh sách còn thiếu | Chưa có endpoint danh sách vé cá nhân. Có thể phối hợp sử dụng nguồn bookings với phạm vi/phân trang rõ, nhưng chưa được coi một trang booking là mọi vé. Không tự thêm GET danh sách vé vào hợp đồng. |
| Trigger / lỗi | Đọc nguồn được chốt khi vào/đổi trang, đọc vé khi chọn chi tiết; lỗi không phải chưa có vé. Customer không gọi POST validate của Staff/Manager khi mở QR. |

### 3.12. KH-12 — Profile

| Hạng mục | Dữ liệu / nguồn / cách dùng |
|---|---|
| Nhu cầu | FR-AUTH-003 yêu cầu hồ sơ cá nhân; Customer chỉ xem/sửa dữ liệu của mình trong phạm vi được phép. |
| Nguồn hiện có | Login trả user id/email/fullName, chưa đủ làm nguồn hồ sơ mới nhất và không có phone. Không có endpoint GET/update profile trong danh mục hiện tại. |
| Bắt buộc để triển khai | Endpoint đọc/lưu, field cho xem/sửa, nullability/validation và response sau lưu **TBD**. Họ tên/điện thoại có thể sửa chỉ là đề xuất UI, email không mặc định được đổi. |
| Tùy chọn / ngoài phạm vi | Phone có thể vắng theo persistence, nhưng API phải chốt; không tự thêm avatar/ngày sinh/đổi mật khẩu. Không hiển thị password hash/token, không cho nhập role/status. |
| Trigger / lỗi | Khi contract được chốt: GET lúc mở/đọc lại; mutation chỉ khi nhấn lưu draft hợp lệ. Lỗi GET không submit form rỗng; lỗi save giữ draft, timeout cần xác minh trước khi báo kết quả. |

## 4. Những dữ liệu cần chốt với người phụ trách API

| Mã | Màn hình | Nội dung còn thiếu / tác động |
|---|---|---|
| DM-01 | KH-03/04/06/08/11 | Ngữ cảnh phim–suất–phòng–rạp, quan hệ định danh và snapshot nào dùng cho lịch sử; cần truy cập trực tiếp mà không phụ thuộc cache bước trước. |
| DM-02 | KH-04 | Tình trạng vật lý, hình học sơ đồ/lối đi/hướng màn hình; không đồng nhất với allocation available/held/sold. |
| DM-03 | KH-09/06 | DTO ticket types/seat types, eligibility và báo giá trước tạo; chưa đủ nguồn để hiển thị tổng dự kiến có căn cứ. |
| DM-04 | KH-06/12 | Dữ liệu liên hệ bắt buộc/tùy chọn, nguồn lấy và trường được gửi khi tạo; request booking hiện chưa có contact. Không tự thêm contact vào payload. |
| DM-05 | KH-05/10 | Khôi phục/tìm attempt hiện tại theo booking, payUrl, provider, return URL, polling và thời gian server; tránh POST mới khi không biết lần cũ. |
| DM-06 | KH-05/10/08 | Bằng chứng applied/reconciliation và kết quả hủy khi cần Query/Cancel cổng; phản hồi đủ để UI phân biệt đang xử lý với đã hủy. |
| DM-07 | KH-11 | Nguồn danh sách vé/phân trang, QR ở chi tiết, ngữ cảnh và trạng thái từng vé; không tự suy từ một trang lịch sử. |
| DM-08 | KH-12 | GET/update Profile, trường và validation được duyệt, xử lý xung đột lưu; chưa có API để thực hiện form. |
| DM-09 | KH-06 | Khôi phục ý định tạo booking khi timeout/reload/mất requestKey; không phát sinh booking thứ hai. |
| DM-10 | Các màn hình | Envelope object/mảng, nullability, tập field summary, mã lỗi và thông tin phân quyền phiên; chốt đồng nhất trước tích hợp. |

Các mục DM là yêu cầu phối hợp, không phải quyết định thêm endpoint/schema. Nếu phụ thuộc chưa được chốt, UI giữ phản hồi “chưa đủ dữ liệu/chức năng chưa sẵn sàng” và không tự cam kết kết quả.

## 5. Tiêu chí nghiệm thu mapping

- **DM-AC-01:** Đủ 12 mã KH, có nguồn, thời điểm đọc/gửi và ranh giới cục bộ/server; không dùng snake_case làm tên field API.
- **DM-AC-02:** Tách dữ liệu cần cho UI với field thực sự đã có; mỗi phần thiếu có DM để phối hợp, không tự khẳng định hợp đồng mới.
- **DM-AC-03:** Register không tự có token; Profile và danh sách vé không có endpoint giả; callback/validate không thành hành động Customer.
- **DM-AC-04:** Booking POST không gửi giá/status; idempotency, ownership và attempt mở không bị thay thế bằng khóa nút đơn thuần.
- **DM-AC-05:** QR/giá/thời gian/lịch sử dùng nguồn đúng; lỗi đọc không biến thành rỗng hoặc kết luận tài chính. Đây là kiểm tra đặc tả, chưa phải kiểm thử API chạy thật.
