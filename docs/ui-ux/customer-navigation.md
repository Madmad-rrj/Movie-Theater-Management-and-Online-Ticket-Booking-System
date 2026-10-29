# Customer Navigation

## 1. Mục đích và phạm vi

Tài liệu xác định menu, liên kết, route và hành vi điều hướng dành cho Customer. Navigation phải bám theo luồng trong `customer-user-flow.md`:

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

`Booking Confirmation` là bước rà soát đơn trước Payment. `Payment Result` là màn hình kết quả sau thanh toán. `My Tickets` hiển thị vé đã phát hành; `Booking History` hiển thị toàn bộ booking của Customer theo trạng thái.

## 2. Nguyên tắc điều hướng

- Route public cho phép duyệt phim/lịch chiếu mà không cần đăng nhập.
- Route private luôn được backend kiểm tra authentication, authorization và quyền sở hữu; ẩn menu ở frontend không phải biện pháp bảo mật.
- ID trong route chỉ dùng để định vị tài nguyên; màn hình phải tải lại dữ liệu authoritative từ backend.
- Booking draft trước khi tạo booking được giữ trong UI session, không đặt toàn bộ payload vào URL và không đồng nghĩa ghế đã được giữ.
- Sau khi booking được tạo, điều hướng dùng `bookingId`; refresh/Back/Forward không được gửi lại thao tác tạo booking ngoài cơ chế idempotency.
- `returnUrl` sau đăng nhập chỉ chấp nhận route nội bộ hợp lệ để tránh open redirect.
- Route không tồn tại hiển thị Not Found với hành động về Home; tài nguyên không thuộc user hiển thị lỗi quyền truy cập, không tiết lộ dữ liệu.

## 3. Điều hướng toàn cục

### 3.1. Header

| Thành phần | Chưa đăng nhập | Đã đăng nhập | Đích/hành vi |
|---|---|---|---|
| Logo | Hiển thị | Hiển thị | Home (`/`) |
| Home | Hiển thị | Hiển thị | Home (`/`) |
| Phim đang chiếu | Hiển thị | Hiển thị | Movie List với `status=now_showing` |
| Phim sắp chiếu | Hiển thị | Hiển thị | Movie List với `status=coming_soon` |
| Đăng nhập | Hiển thị | Ẩn | Login; giữ `returnUrl` khi được chuyển từ route private |
| Đăng ký | Hiển thị | Ẩn | Register; sau xác thực áp dụng mục 7 |
| Menu tài khoản | Ẩn | Hiển thị | Account, My Tickets, Booking History, Đăng xuất |

Quy tắc:

- Mục menu tương ứng route hiện tại có trạng thái active.
- Link Movie List giữ trạng thái phim trong query string để có thể bookmark/chia sẻ.
- Header không hiển thị dữ liệu private còn sót lại sau đăng xuất.
- Khi Customer đang có booking `pending`, rời luồng qua logo/menu không tự hủy booking; UI cảnh báo thời gian giữ ghế vẫn tiếp tục chạy.

### 3.2. Menu chính

1. **Home** — nội dung nổi bật và các nhóm phim.
2. **Phim đang chiếu** — Movie List lọc `now_showing`.
3. **Phim sắp chiếu** — Movie List lọc `coming_soon`.

Không thêm menu loyalty, promotion hoặc dịch vụ ngoài phạm vi MVP.

### 3.3. Menu tài khoản

| Mục | Route | Nội dung |
|---|---|---|
| Tài khoản | `/account` | Thông tin tài khoản Customer |
| My Tickets | `/account/tickets` | Vé đã phát hành cho các booking `confirmed` |
| Booking History | `/account/bookings` | Tất cả booking của user với trạng thái hiện tại |
| Đăng xuất | Không có route cố định | Kết thúc phiên theo mục 7.3 |

My Tickets và Booking History có thể liên kết chéo:

- Từ một ticket có thể mở booking detail tương ứng.
- Từ booking `confirmed` có thể mở danh sách ticket của booking đó.
- Booking `pending`, `expired` hoặc `cancelled` không hiển thị QR nếu chưa có ticket.

### 3.4. Footer

- Tên rạp, địa chỉ, số điện thoại lấy từ dữ liệu/cấu hình hiện có.
- Link tĩnh: Giới thiệu, Điều khoản, Chính sách riêng tư nếu các trang đó tồn tại.
- Link nhanh: Home, Phim đang chiếu, Phim sắp chiếu.
- Footer không chứa hành động bắt buộc của luồng đặt vé và không tự tạo route cho nội dung chưa được dự án định nghĩa.

## 4. Danh mục route Customer

Các path dưới đây là hợp đồng điều hướng phía frontend; API endpoint vẫn tuân theo `docs/api/*`.

| Màn hình | Route đề xuất | Quyền truy cập | Context bắt buộc |
|---|---|---|---|
| Home | `/` | Public | — |
| Movie List | `/movies?status={status}&page={page}` | Public | `status`: `now_showing` hoặc `coming_soon` |
| Movie Detail | `/movies/:movieId` | Public | `movieId` |
| Showtime | `/movies/:movieId/showtimes?date={date}` | Public | `movieId`, ngày tùy chọn |
| Seat Selection | `/showtimes/:showtimeId/seats` | Public | `showtimeId` |
| Ticket Type | `/booking/ticket-types` | Public; draft trong UI session | `showtimeId`, danh sách ghế draft |
| Booking Confirmation | `/booking/confirmation` | Yêu cầu đăng nhập trước khi tạo booking | Booking draft + thông tin liên hệ |
| Payment | `/bookings/:bookingId/payment` | Private, chủ booking | `bookingId` |
| Payment Result | `/bookings/:bookingId/payment-result` | Private, chủ booking | `bookingId`; payment lấy/xác minh từ backend |
| Account | `/account` | Private | User hiện tại |
| My Tickets | `/account/tickets` | Private | User hiện tại; có thể nhận `bookingId` để highlight |
| Booking History | `/account/bookings` | Private | User hiện tại, phân trang |
| Booking Detail | `/account/bookings/:bookingId` | Private, chủ booking | `bookingId` |
| Login | `/login?returnUrl={internalPath}` | Public | `returnUrl` nội bộ tùy chọn |
| Register | `/register?returnUrl={internalPath}` | Public | `returnUrl` nội bộ tùy chọn |

Nếu router thực tế dùng tên path khác, quan hệ màn hình, quyền truy cập và context trong bảng vẫn phải được giữ nhất quán.

## 5. Ma trận chuyển màn hình

### 5.1. Duyệt phim

| Từ | Thao tác | Đến | Dữ liệu giữ lại |
|---|---|---|---|
| Home | "Xem tất cả" ở phim đang chiếu | Movie List | `status=now_showing` |
| Home | "Xem tất cả" ở phim sắp chiếu | Movie List | `status=coming_soon` |
| Home/Movie List | Chọn movie card | Movie Detail | `movieId`; filter/list position trong UI session |
| Movie List | Đổi trạng thái/trang | Movie List | Query string mới; reset page khi đổi status |
| Movie Detail | "Xem lịch chiếu" | Showtime | `movieId` |
| Showtime | Đổi ngày | Showtime | `movieId`, ngày mới |
| Showtime | Chọn suất còn bán | Seat Selection | `showtimeId`, `movieId` |

### 5.2. Đặt vé và thanh toán

| Từ | Thao tác/điều kiện | Đến | Dữ liệu giữ lại |
|---|---|---|---|
| Seat Selection | Chọn ít nhất một ghế hợp lệ và tiếp tục | Ticket Type | `showtimeId`, `selectedSeatIds` draft |
| Ticket Type | Gán loại vé cho mọi ghế và tiếp tục | Booking Confirmation | Booking draft |
| Booking Confirmation | Chưa đăng nhập | Login | `returnUrl=/booking/confirmation`; draft trong UI session |
| Booking Confirmation | Backend tạo booking all-or-nothing thành công | Payment | `bookingId`, `expiresAt`; bỏ draft đã được chốt |
| Payment | Có payment attempt; quay về từ cổng/kiểm tra kết quả | Payment Result | `bookingId`; backend phân giải payment hiện hành |
| Payment Result | Payment `succeeded` và applied; booking `confirmed` | My Tickets | `bookingId` để mở/highlight vé vừa mua |
| Payment Result | Payment terminal thất bại, booking còn `pending`/còn hạn | Payment | `bookingId`; attempt mới chỉ khi backend cho phép |
| Payment Result | Booking đã `expired` | Showtime hoặc Seat Selection | `movieId`/`showtimeId`; không dùng lại booking cũ |

### 5.3. Tài khoản và lịch sử

| Từ | Thao tác | Đến |
|---|---|---|
| Menu tài khoản | Chọn My Tickets | My Tickets |
| Menu tài khoản | Chọn Booking History | Booking History |
| Booking History | Chọn một booking | Booking Detail |
| Booking Detail `confirmed` | "Xem vé" | My Tickets, highlight booking tương ứng |
| My Tickets | "Xem booking" | Booking Detail |
| My Tickets/Booking History rỗng | "Khám phá phim" | Movie List `now_showing` |

## 6. Trang yêu cầu đăng nhập

| Nhóm | Route | Hành vi khi chưa đăng nhập |
|---|---|---|
| Public | Home, Movie List, Movie Detail, Showtime, Seat Selection, Ticket Type | Cho phép truy cập; dữ liệu chọn chỉ là draft và chưa giữ ghế |
| Guarded booking | Booking Confirmation khi xác nhận tạo booking | Chuyển Login, giữ `returnUrl` và draft trong UI session |
| Private booking/payment | Payment, Payment Result, Booking Detail | Chuyển Login; sau đăng nhập backend kiểm tra quyền sở hữu/trạng thái |
| Private account | Account, My Tickets, Booking History | Chuyển Login và quay lại route ban đầu sau đăng nhập thành công |

Frontend xử lý độc lập:

- `401`: phiên chưa hợp lệ → Login.
- `403`: đã đăng nhập nhưng không sở hữu/không có quyền → Access Denied.
- `404`: tài nguyên không tồn tại hoặc không được công khai → Not Found.

## 7. Điều hướng sau xác thực

### 7.1. Sau đăng nhập

1. Kiểm tra `returnUrl` là route nội bộ hợp lệ.
2. Nếu có `returnUrl`, quay lại đúng màn hình và tải lại dữ liệu authoritative.
3. Với booking draft, xác minh lại phim/suất/ghế trước khi Customer xác nhận tạo booking.
4. Với Payment/Payment Result/Booking Detail, backend kiểm tra user là chủ booking.
5. Nếu không có `returnUrl`, về Home.

Không tự động gửi lại POST tạo booking hoặc payment chỉ vì đăng nhập thành công.

### 7.2. Sau đăng ký

- Nếu đăng ký đồng thời tạo phiên hợp lệ, áp dụng quy tắc như sau đăng nhập.
- Nếu cần đăng nhập riêng, chuyển tới Login và giữ `returnUrl` hợp lệ.
- Đăng ký công khai chỉ cấp role `CUSTOMER`.

### 7.3. Sau đăng xuất

- Kết thúc phiên, xóa token và dữ liệu private khỏi UI state; điều hướng về Home.
- Back/Forward quay lại route private phải kích hoạt route guard thay vì hiển thị dữ liệu cache.
- Đăng xuất không tự hủy booking `pending` hoặc thay đổi payment ở backend; các đối tượng này tiếp tục theo lifecycle nghiệp vụ.

## 8. Back, Forward, refresh và deep link

### 8.1. Trước khi tạo booking

- Back/Forward được phép giữa Movie Detail, Showtime, Seat Selection, Ticket Type và Booking Confirmation.
- Có thể khôi phục filter, ngày, vị trí danh sách và booking draft từ UI session.
- Khi vào lại Showtime/Seat Selection, phải tải lại dữ liệu động; ghế/suất không còn hợp lệ bị loại khỏi draft và thông báo cho Customer.
- Refresh ở Ticket Type/Booking Confirmation khi không còn draft: chuyển về Seat Selection hoặc Showtime với thông báo cần chọn lại.

### 8.2. Sau khi tạo booking

- Payment, Payment Result và Booking Detail tái dựng bằng `bookingId` từ backend.
- Back khỏi Payment không tự hủy booking/payment attempt.
- Forward/refresh không tạo payment attempt thứ hai khi còn attempt `created`/`pending`/`unknown`.
- Payment Result không tin trạng thái trong query string; luôn tải trạng thái đã xác minh từ backend.
- Booking `expired`/`cancelled` không được quay lại Payment để tiếp tục booking cũ.

### 8.3. Deep link

- Deep link public tải trực tiếp theo ID/query.
- Deep link private chưa đăng nhập đi qua Login rồi quay lại.
- ID sai → 404; tài nguyên thuộc user khác → 403.
- Showtime vừa đóng bán vẫn có thể hiển thị thông tin nhưng hành động tiếp tục bị disabled theo backend.

## 9. Điều hướng khi lỗi hoặc trạng thái thay đổi

| Tình huống | Hành vi điều hướng |
|---|---|
| Movie không tồn tại/không còn công khai | Error/Not Found → Home hoặc Movie List |
| Không có showtime | Giữ Movie Detail/Showtime với empty state |
| Showtime đóng bán/hủy | Giữ Showtime, vô hiệu suất và yêu cầu chọn suất khác |
| Ghế bị tranh chấp lúc tạo booking | Quay Seat Selection, tải lại sơ đồ |
| Booking hết hạn | Payment Result/expired → Showtime hoặc Seat Selection |
| Payment `pending`/`unknown` | Giữ Payment Result; không tạo attempt song song |
| Payment thành công và applied | Payment Result/success → My Tickets |
| API/network error | Giữ route hiện tại, hiển thị Retry; không tự lặp thao tác ghi dữ liệu |

## 10. Nhất quán với màn hình và tài liệu liên quan

Danh sách màn hình Customer:

- Duyệt phim: Home, Movie List, Movie Detail, Showtime.
- Đặt vé: Seat Selection, Ticket Type, Booking Confirmation, Payment, Payment Result.
- Tài khoản: Account, My Tickets, Booking History, Booking Detail.
- Xác thực/hệ thống: Login, Register, Not Found, Access Denied.

Nguồn đối chiếu:

- `docs/ui-ux/customer-user-flow.md`
- `docs/ui-ux/customer-screen-specification.md`
- `docs/ui-ux/customer-ui-state.md`
- `docs/ui-ux/customer-screen-api-mapping.md`
- `docs/requirements/business-system-analysis.md`

Các tài liệu màn hình/API hiện còn dùng thuật ngữ cũ cho Booking Confirmation và chưa liệt kê đủ Movie List, Ticket Type, Payment Result, My Tickets, Booking History. Khi cập nhật các tài liệu đó phải dùng cùng quan hệ màn hình và route guard được xác định tại đây.
