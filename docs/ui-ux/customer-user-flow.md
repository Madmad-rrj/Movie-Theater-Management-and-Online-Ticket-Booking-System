# Customer User Flow

## 1. Mục đích và phạm vi

Tài liệu mô tả hành trình chính của Customer từ lúc duyệt phim đến khi xem vé đã mua, bao gồm:

- Mục đích của từng bước.
- Điều kiện để chuyển bước.
- Dữ liệu cần giữ khi chuyển màn hình.
- Các nhánh thành công, thất bại, hủy, hết hạn và quay lại.
- Truy vết tới use case và business rule hiện có.

Luồng trong tài liệu chỉ bao phủ phía Customer. Các quyết định nghiệp vụ còn `OPEN` được ghi rõ và không được tự chốt tại tầng UI/UX.

## 2. Thuật ngữ và nguyên tắc

### 2.1. Luồng chính bắt buộc

```text
Home
  → Movie Detail
  → Showtime
  → Seat Selection
  → Ticket Type
  → Booking Confirmation
  → Payment
  → Payment Result
  → My Tickets
```

Trong tài liệu này:

- **Booking Confirmation** là màn hình rà soát và xác nhận thông tin đơn **trước khi thanh toán**. Tên màn hình không đồng nghĩa với `booking.status = confirmed`.
- **Payment Result** là màn hình hiển thị kết quả thanh toán sau khi backend xác minh.
- **My Tickets** tương ứng khu vực "Booking của tôi" trong các tài liệu UI/UX hiện có.
- Dữ liệu chọn trên frontend trước khi tạo booking chỉ là **draft**. Draft không giữ ghế và không phải nguồn dữ liệu authoritative.
- Ghế chỉ được giữ khi backend tạo thành công booking `pending` cùng toàn bộ booking detail `held` trong một transaction.
- Giá, khả dụng của ghế, trạng thái booking, trạng thái payment và vé luôn lấy lại từ backend ở các điểm chốt; frontend không tự quyết định.

### 2.2. Nhóm dữ liệu được giữ

| Nhóm | Dữ liệu | Nguồn | Thời hạn/phạm vi |
|---|---|---|---|
| Navigation context | `movieId`, `showtimeId`, ngày đang xem, URL quay lại | Route/UI | Trong phiên duyệt hiện tại |
| Booking draft | Danh sách `seatId`, loại vé gán cho từng ghế, thông tin liên hệ | UI session | Từ Seat Selection đến trước khi booking được tạo; phải xác minh lại |
| Booking authoritative | `bookingId`, `bookingCode`, `status`, `totalAmount`, `currency`, `expiresAt`, chi tiết ghế/loại vé/giá snapshot | Backend | Từ khi tạo booking đến hết luồng |
| Payment context | `paymentId`/`paymentAttemptId`, trạng thái payment, thời điểm cần kiểm tra lại | Backend | Trong Payment và Payment Result |
| Ticket context | `bookingId`, danh sách ticket, mã/QR, ghế, suất chiếu, trạng thái vé | Backend | Chỉ hiển thị cho chủ booking theo quyền truy cập |

Không lưu dữ liệu thẻ, secret của cổng thanh toán hoặc kết quả thanh toán chưa được backend xác minh vào booking draft.

## 3. Luồng chính theo từng bước

### 3.1. Home

**Mục đích**

- Hiển thị phim đang chiếu/sắp chiếu và tạo điểm vào luồng đặt vé.

**Dữ liệu sử dụng và giữ lại**

- Danh sách phim, trạng thái hiển thị, bộ lọc/từ khóa hiện tại.
- Khi chọn phim, giữ `movieId`; bộ lọc có thể được giữ trong UI session để khôi phục khi quay lại Home.

**Điều kiện chuyển bước**

- Customer chọn một phim còn tồn tại và được phép hiển thị.
- Chuyển tới **Movie Detail** với `movieId`.

**Nhánh khác**

- Danh sách rỗng: hiển thị empty state, không chuyển bước.
- Phim vừa bị gỡ hoặc không còn truy cập được: hiển thị lỗi thân thiện và tải lại Home.

### 3.2. Movie Detail

**Mục đích**

- Cung cấp thông tin chi tiết phim và cho Customer bắt đầu chọn lịch chiếu.

**Dữ liệu sử dụng và giữ lại**

- `movieId` và dữ liệu phim do backend trả về.

**Điều kiện chuyển bước**

- Customer chọn hành động "Chọn suất chiếu".
- Chuyển tới **Showtime** và tiếp tục giữ `movieId`.

**Nhánh khác**

- Phim không tồn tại: hiển thị error state và cho phép quay về Home.
- Chưa có suất chiếu: hiển thị empty state; không tự tạo hoặc suy đoán lịch chiếu.
- Quay lại: trở về Home, có thể khôi phục bộ lọc của phiên duyệt.

### 3.3. Showtime

**Mục đích**

- Cho Customer chọn ngày và một suất chiếu phù hợp của phim.

**Dữ liệu sử dụng và giữ lại**

- `movieId`, ngày đang xem và `showtimeId` được chọn.
- Thông tin giờ chiếu/phòng/giá cơ sở chỉ phục vụ hiển thị; backend phải kiểm tra lại khi tạo booking.

**Điều kiện chuyển bước**

- Suất được hiển thị là `scheduled`, không bị hủy và chưa đóng bán theo cấu hình hiện hành.
- Customer chọn một suất hợp lệ.
- Chuyển tới **Seat Selection** với `movieId` và `showtimeId`.

**Nhánh khác**

- Không có suất trong ngày: hiển thị empty state và cho phép đổi ngày.
- Suất vừa đóng bán/hủy: vô hiệu hóa lựa chọn, thông báo và tải lại lịch.
- Mốc đóng bán cụ thể chưa được chốt; UI chỉ sử dụng trạng thái/`booking_closes_at` do backend cung cấp (OQ-02).

### 3.4. Seat Selection

**Mục đích**

- Hiển thị sơ đồ ghế theo trạng thái hiện hành và cho Customer chọn ghế.

**Dữ liệu sử dụng và giữ lại**

- `movieId`, `showtimeId`.
- Sơ đồ ghế và trạng thái khả dụng từ backend.
- `selectedSeatIds` là booking draft cục bộ; việc chọn trên UI **chưa giữ ghế**.

**Điều kiện chuyển bước**

- Đã chọn ít nhất một ghế đang khả dụng.
- Ghế vật lý đang `active`; ghế `held`, `sold`, `maintenance` hoặc `inactive` không được chọn.
- Ghế Couple được chọn theo một đơn vị bán không tách rời.
- Chuyển tới **Ticket Type** với `showtimeId` và `selectedSeatIds`.

**Nhánh khác**

- Ghế đổi trạng thái trong lúc chọn: cập nhật sơ đồ, bỏ ghế không còn khả dụng khỏi draft và thông báo cho Customer.
- Quay lại Showtime: có thể giữ draft trong UI session, nhưng khi vào lại phải tải và xác minh lại sơ đồ ghế.
- Rời luồng trước khi tạo booking: không có booking để hủy và không có ghế nào được giữ ở backend.

### 3.5. Ticket Type

**Mục đích**

- Gán một loại vé hợp lệ cho từng đơn vị ghế đã chọn.

**Dữ liệu sử dụng và giữ lại**

- `movieId`, `showtimeId`, `selectedSeatIds`.
- Ánh xạ `seatId → ticketTypeId` trong booking draft.
- Giá hiển thị tại bước này chỉ là ước tính; giá chốt do backend tính khi tạo booking.

**Điều kiện chuyển bước**

- Mỗi ghế/đơn vị ghế đã chọn có đúng một loại vé đang hoạt động.
- Chuyển tới **Booking Confirmation** với toàn bộ booking draft.

**Nhánh khác**

- Loại vé vừa ngừng hoạt động: yêu cầu Customer chọn lại.
- Điều kiện hưởng vé giảm giá và cách trình bày lựa chọn vé cho ghế Couple còn `OPEN` (OQ-08); UI không tự xác định tiêu chí mới. Backend là nơi xác minh điều kiện áp dụng.
- Quay lại Seat Selection: giữ các gán loại vé cho ghế vẫn còn trong lựa chọn; loại bỏ gán của ghế đã bỏ chọn.

### 3.6. Booking Confirmation

**Mục đích**

- Cho Customer rà soát phim, suất, ghế, loại vé, thông tin liên hệ và giá dự kiến trước khi xác nhận tạo booking.

**Dữ liệu sử dụng và giữ lại**

- Toàn bộ booking draft: `showtimeId`, danh sách ghế + loại vé, thông tin liên hệ.
- `requestKey` do client tạo để hỗ trợ idempotency. Cách chuẩn hóa thứ tự ghế trong `request_fingerprint` còn `PROPOSED` (OQ-UC-05), frontend không tự thay đổi quy tắc backend.

**Điều kiện chuyển bước**

1. Customer đã đăng nhập và tài khoản còn `active`.
2. Thông tin liên hệ hợp lệ và mỗi ghế đã có loại vé.
3. Customer nhấn xác nhận.
4. Backend kiểm tra lại suất, ghế, loại vé và giá trong transaction; không tin giá từ client.
5. Nếu toàn bộ điều kiện hợp lệ, backend tạo booking `pending`, tạo toàn bộ detail `held`, chốt giá và trả `bookingId`, `bookingCode`, `totalAmount`, `currency`, `expiresAt`.
6. UI chuyển tới **Payment** và bắt đầu hiển thị countdown theo `expiresAt` do backend trả về.

**Nhánh khác**

- Chưa đăng nhập: chuyển tới Login, lưu URL/draft cần thiết và quay lại Booking Confirmation sau khi đăng nhập thành công.
- Một ghế bị giữ/bán trước lúc xác nhận: toàn bộ thao tác tạo booking thất bại, không giữ một phần; quay lại Seat Selection và tải lại sơ đồ.
- Request cùng `requestKey` và cùng nội dung: sử dụng lại booking đã tồn tại; không tạo booking thứ hai.
- Backend từ chối loại vé/giá: hiển thị lỗi và yêu cầu Customer cập nhật draft; frontend không tự sửa giá.
- Quay lại trước khi xác nhận: chỉ giữ draft, chưa có countdown và chưa giữ ghế.

### 3.7. Payment

**Mục đích**

- Thanh toán booking `pending` của Customer trong thời gian còn hiệu lực.

**Dữ liệu sử dụng và giữ lại**

- Dùng dữ liệu authoritative: `bookingId`, `bookingCode`, `totalAmount`, `currency`, `expiresAt`, `booking.status`.
- Khi bắt đầu thanh toán, giữ `paymentId`/`paymentAttemptId` và trạng thái attempt do backend trả về.

**Điều kiện chuyển bước**

- Customer là chủ booking; booking còn `pending` và còn đủ điều kiện thanh toán.
- Backend tạo payment attempt với đúng số tiền/tiền tệ của booking và idempotency key.
- Không tạo attempt mở mới nếu đã có payment `created`, `pending` hoặc `unknown`.
- Customer thực hiện thanh toán qua cổng; sau redirect/callback hoặc khi cần kiểm tra trạng thái, chuyển tới **Payment Result** với định danh booking/payment, không mang dữ liệu thanh toán nhạy cảm.

**Nhánh khác**

- Booking không còn hợp lệ: chặn tạo payment attempt và chuyển Payment Result sang trạng thái tương ứng.
- Cổng thanh toán cụ thể còn `OPEN` (OQ-01); luồng chỉ mô tả hành vi trung lập cổng.
- Nhấn Back không tự tạo attempt mới và không tự hủy booking. Khi quay lại, UI lấy lại trạng thái booking/payment từ backend.
- Hủy/đổi ghế được xử lý theo mục 5.5; UI không tự suy payment là thất bại.

### 3.8. Payment Result

**Mục đích**

- Hiển thị kết quả đã được backend xác minh và hướng Customer tới hành động an toàn tiếp theo.

**Dữ liệu sử dụng và giữ lại**

- `bookingId`, `paymentId`/`paymentAttemptId`.
- Trạng thái booking và payment lấy từ backend; không tin trạng thái chỉ có trong query string hoặc dữ liệu trả về trực tiếp từ trình duyệt.

**Các trạng thái kết quả**

| Kết quả backend | Hiển thị/hành động | Chuyển tiếp |
|---|---|---|
| Payment `succeeded` và đã `applied`; booking `confirmed`; detail `sold`; tickets đã phát hành | Thông báo thanh toán thành công, hiển thị mã booking | Cho phép chuyển tới **My Tickets** với `bookingId` |
| Payment `failed` hoặc `cancelled`; booking vẫn `pending` và còn hạn | Thông báo thất bại/hủy; không nói ghế đã được giải phóng | Cho phép quay lại Payment để thử attempt mới hoặc hủy booking |
| Payment `pending` hoặc `unknown` | Hiển thị "Kết quả đang được xác nhận"; vô hiệu retry/tạo attempt mới | Tiếp tục kiểm tra trạng thái theo backend; chưa chuyển sang vé thành công |
| Booking `expired` và ghế `released` | Thông báo hết hạn giữ ghế | Quay lại Showtime/Seat Selection để bắt đầu lựa chọn mới |
| Payment `succeeded` nhưng không thể `applied` do booking đã hết hạn/hủy | Thông báo giao dịch cần được xử lý/đối soát; không hiển thị vé | Không tự chiếm lại ghế, không chuyển booking thành `confirmed`; không tự hứa hoàn tiền |

**Nhánh khác**

- Refresh/đi thẳng vào URL: tải trạng thái từ backend và render đúng nhánh hiện tại.
- Callback lặp hoặc đến sai thứ tự không làm UI phát hành thêm vé; backend xử lý idempotent.
- Khi trạng thái còn `unknown`, UI không mặc nhiên báo thất bại và không cho Customer tạo payment attempt song song.

### 3.9. My Tickets

**Mục đích**

- Cho Customer xem booking và vé thuộc chính tài khoản của mình.

**Dữ liệu sử dụng và giữ lại**

- Phiên đăng nhập và `bookingId` được chọn từ Payment Result nếu có.
- Danh sách booking, trạng thái hiện tại và ticket do backend trả về.

**Điều kiện vào màn hình**

- Customer đã đăng nhập.
- Backend chỉ trả booking/vé thuộc user hiện tại.
- Vé chỉ tồn tại sau khi payment hợp lệ được áp dụng và booking chuyển `confirmed`.

**Nhánh khác**

- Sau luồng thành công: ưu tiên mở/highlight booking vừa hoàn tất.
- Không có booking: hiển thị empty state và hành động quay về Home.
- Booking `pending`, `expired` hoặc `cancelled` có thể xuất hiện trong lịch sử với đúng trạng thái; không hiển thị QR nếu chưa có ticket.
- Ghế Couple phát hành số ticket theo `capacity_units_snapshot` (hai ticket/QR độc lập khi capacity là 2).
- Truy cập booking của user khác: backend từ chối; UI hiển thị lỗi quyền truy cập.

## 4. Ma trận chuyển bước

| Từ | Hành động/điều kiện | Đến | Dữ liệu tiếp tục giữ |
|---|---|---|---|
| Home | Chọn phim hợp lệ | Movie Detail | `movieId`, bộ lọc Home |
| Movie Detail | Chọn xem lịch | Showtime | `movieId` |
| Showtime | Chọn suất còn bán | Seat Selection | `movieId`, `showtimeId`, ngày xem |
| Seat Selection | Chọn ít nhất một ghế hợp lệ | Ticket Type | Context + `selectedSeatIds` draft |
| Ticket Type | Mỗi ghế có một loại vé hợp lệ | Booking Confirmation | Context + ánh xạ ghế/loại vé |
| Booking Confirmation | Đăng nhập; backend tạo booking all-or-nothing thành công | Payment | `bookingId`, mã booking, tổng tiền, `expiresAt` |
| Payment | Có payment attempt; Customer hoàn tất/rời cổng hoặc cần xem trạng thái | Payment Result | `bookingId`, `paymentId`/`paymentAttemptId` |
| Payment Result | Payment succeeded và applied, booking confirmed, ticket đã phát hành | My Tickets | `bookingId` |

## 5. Nhánh xuyên suốt

### 5.1. Luồng thành công

1. Customer chọn phim, suất, ghế và loại vé.
2. Customer rà soát và xác nhận đơn.
3. Backend tạo booking `pending` + toàn bộ detail `held`, chốt giá và thời hạn giữ 5 phút.
4. Customer thanh toán; backend xác minh payment.
5. Trong một transaction, backend đặt payment `applied`, booking `confirmed`, detail `sold` và phát hành ticket.
6. Payment Result hiển thị thành công; Customer chuyển tới My Tickets.

### 5.2. Ghế bị tranh chấp

- Sơ đồ ghế chỉ là ảnh chụp trạng thái tại thời điểm tải.
- Backend kiểm tra lại trong transaction tạo booking.
- Nếu bất kỳ ghế nào không còn khả dụng, toàn bộ yêu cầu thất bại; không giữ một phần.
- UI thông báo danh sách ghế xung đột, tải lại sơ đồ và đưa Customer về Seat Selection.

### 5.3. Hết thời gian giữ ghế

- Countdown 5 phút dùng `expiresAt` của backend và chỉ phục vụ hiển thị.
- Booking quá hạn chỉ chuyển `expired` và detail `released` khi thỏa lifecycle backend, đặc biệt không được tự giải phóng khi vẫn còn payment `created`/`pending`/`unknown` cần xử lý.
- Chính sách grace period đã được chấp thuận, nhưng số phút chính xác còn cấu hình/chưa chốt (OQ-UC-03); UI không hard-code giá trị đề xuất 2–3 phút như một business rule.
- Khi backend xác nhận booking đã `expired`, draft cũ không được dùng để thanh toán; Customer phải chọn lại ghế.

### 5.4. Thanh toán thất bại hoặc chưa xác định

- `failed`/`cancelled`: không tự đổi booking; nếu booking còn `pending` và còn hạn, Customer có thể thử lại sau khi attempt cũ đã chốt.
- `pending`/`unknown`: không tạo attempt mới, không tự coi là thất bại và không hiển thị vé; UI chờ backend đối soát.
- Payment thành công muộn nhưng booking không còn đủ điều kiện: giữ bằng chứng payment `succeeded`, không applied, không phát hành vé và chuyển xử lý đối soát; workflow hoàn tiền tự động nằm ngoài phạm vi demo.

### 5.5. Customer hủy hoặc đổi ghế

- Trước khi booking được tạo: bỏ draft và quay lại, không cần gọi hủy booking.
- Booking `pending` chưa có attempt mở: Customer có thể yêu cầu hủy toàn bộ; backend chuyển booking `cancelled` và giải phóng toàn bộ ghế trong một transaction.
- Có payment `created`/`pending`/`unknown`: backend phải gọi Cancel/Query API của cổng ngoài transaction. Chỉ khi cổng xác nhận chưa thu tiền mới chuyển payment `cancelled`, hủy booking và giải phóng ghế.
- Nếu cổng báo đã thu hoặc kết quả chưa chắc chắn: từ chối hủy/đổi ghế; UI đưa Customer tới Payment Result trạng thái đang xác nhận.
- Không hỗ trợ hủy booking `confirmed` sau thanh toán trong phạm vi demo.

### 5.6. Back, Forward, refresh và truy cập trực tiếp

- Trước khi tạo booking: có thể giữ draft trong UI session để hỗ trợ Back/Forward, nhưng phải tải lại dữ liệu động và xác minh ghế/suất khi quay lại.
- Sau khi tạo booking: chỉ giữ `bookingId` làm khóa điều hướng; tải lại trạng thái authoritative từ backend, không gửi lại lệnh tạo booking ngoài cơ chế idempotency.
- Back khỏi Payment không tự hủy booking hoặc payment attempt.
- Refresh Payment Result/My Tickets phải tái dựng giao diện từ trạng thái backend.
- Route yêu cầu đăng nhập lưu URL đích; sau đăng nhập thành công quay lại đúng bước nếu tài nguyên vẫn hợp lệ.

## 6. Truy vết Use Case và Business Rule

| Phần luồng | Use case / Functional requirement | Business rule chính |
|---|---|---|
| Home, Movie Detail | FR-MOVIE-001, FR-MOVIE-002 | Phạm vi duyệt phim hiện có |
| Showtime | FR-SHOW-001 | BR-ST-003, OQ-02 |
| Seat Selection | FR-SHOW-002, UC-01 | BR-BOOK-001, BR-BOOK-002, BR-BOOK-003, BR-BOOK-006 |
| Ticket Type | FR-BOOK-002, UC-01 | BR-BOOK-005, BR-BOOK-010, OQ-08 |
| Booking Confirmation | FR-AUTH-002, FR-AUTH-004, FR-BOOK-001, FR-BOOK-002, UC-01 | BR-BOOK-001, BR-BOOK-002, BR-BOOK-004, BR-BOOK-005, BR-AUTH-002 |
| Payment | FR-PAY-001, UC-02 | BR-PAY-001, BR-PAY-002, OQ-01 |
| Payment Result | FR-PAY-002..005, UC-02, UC-03, UC-04 | BR-PAY-003..008, BR-BOOK-004, BR-BOOK-007 |
| Hủy/đổi ghế | FR-BOOK-003 | BR-BOOK-008 |
| My Tickets | FR-BOOK-004, FR-TICKET-001 | BR-BOOK-009, BR-AUTH-002 |

Nguồn đối chiếu chính:

- `docs/requirements/business-system-analysis.md`
- `docs/requirements/use-cases.md`
- `docs/analysis and design/business-system-design.md`
- `database/database-design.md`
- `docs/analysis and design/relational-model.md`

## 7. Quyết định còn bỏ ngỏ

| ID | Nội dung | Cách tài liệu này xử lý |
|---|---|---|
| OQ-01 | Cổng thanh toán cụ thể | Giữ luồng trung lập cổng; không ghi provider, callback URL hoặc hành vi riêng của cổng |
| OQ-02 | Mốc đóng bán cuối cùng | Dùng trạng thái/`booking_closes_at` do backend trả về; không hard-code số phút |
| OQ-08 | Điều kiện hưởng vé giảm giá và chi tiết UI ghế Couple | Backend xác minh; không tự đặt giấy tờ/độ tuổi. Giữ quy tắc đã chốt: Couple là một đơn vị bán, phát hành ticket theo capacity |
| OQ-UC-03 | Giá trị grace period chính xác | Không hiển thị 2–3 phút như giá trị cố định; lấy trạng thái và hướng dẫn từ backend |
| OQ-UC-05 | Chuẩn hóa thứ tự ghế trong `request_fingerprint` | Client gửi request ổn định và giữ nguyên `requestKey`; quy tắc fingerprint do backend/nhóm chốt |

## 8. Lưu ý đồng bộ tài liệu

Issue 68 yêu cầu `Booking Confirmation` đứng trước Payment và có màn hình `Payment Result` riêng. Một số tài liệu UI/UX hiện tại đang dùng `Booking Confirmation` như trang thành công sau Payment. Khi các tài liệu đó được cập nhật, cần thống nhất thuật ngữ theo tài liệu này:

- `Booking Confirmation`: rà soát/xác nhận đơn trước thanh toán.
- `Payment Result`: kết quả thanh toán.
- `My Tickets`: danh sách booking/vé của Customer.

Việc đồng bộ `customer-screen-specification.md`, `customer-navigation.md`, `customer-ui-state.md` và `customer-screen-api-mapping.md` nằm ngoài phạm vi file của CIN-SUB-047, nhưng là dependency cần theo dõi để tránh mâu thuẫn khi triển khai giao diện.
