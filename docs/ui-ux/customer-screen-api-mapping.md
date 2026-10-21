# Customer Screen — API Mapping

## 1. Mục đích

Liên kết từng màn hình với dữ liệu và API mà frontend cần sử dụng. Tài liệu trả lời câu hỏi: dữ liệu trên màn hình lấy từ đâu?

## 2. Quy ước chung

- Thời điểm gọi API được ghi tường minh để tránh gọi lặp hoặc gọi quá sớm.
- Trạng thái lỗi quy ước theo `customer-ui-state.md`: lỗi toàn trang (error state) hoặc lỗi thao tác (toast/modal).
- Danh mục endpoint đầy đủ tham chiếu `docs/api/api-endpoints.md`; cấu trúc request/response tham chiếu `docs/api/api-contract.md` và `docs/api/dto-specification.md`.

## 3. Bảng mapping màn hình — API

### 3.1. Home

| Mục | Giá trị |
|---|---|
| Dữ liệu đầu vào | Trang, phân nhóm (`now_showing`/`coming_soon`) |
| API endpoint | `GET /api/movies?status=...&page=...` |
| Thời điểm gọi | Khi vào Home; khi chuyển phân nhóm/tải thêm |
| Dữ liệu trả về | Danh sách phim (phân trang): poster, tên, phân loại tuổi, trạng thái |
| Lỗi | 500/timeout → error state "Thử lại"; danh sách rỗng → empty state |
| Hành vi UI | Loading skeleton → render grid card |

### 3.2. Movie Detail

| Mục | Giá trị |
|---|---|
| Dữ liệu đầu vào | `movieId` (từ route) |
| API endpoint | `GET /api/movies/{movieId}` |
| Thời điểm gọi | Khi vào màn hình |
| Dữ liệu trả về | Chi tiết phim: poster, tên, thời lượng, mô tả, ngôn ngữ, phân loại tuổi, trạng thái |
| Lỗi | 404 → error state "Không tìm thấy phim" |
| Hành vi UI | Hiển thị chi tiết; nút "Chọn suất chiếu" điều hướng Showtime |

### 3.3. Showtime

| Mục | Giá trị |
|---|---|
| Dữ liệu đầu vào | `movieId`, ngày xem |
| API endpoint | `GET /api/movies/{movieId}/showtimes?date=...` |
| Thời điểm gọi | Khi vào màn hình; khi đổi ngày |
| Dữ liệu trả về | Danh sách suất: `starts_at`, phòng, giá cơ sở, trạng thái còn/đóng bán |
| Lỗi | Không có suất → empty state; lỗi → error state |
| Hành vi UI | Chọn suất hợp lệ → Seat Selection |

### 3.4. Seat Selection

| Mục | Giá trị |
|---|---|
| Dữ liệu đầu vào | `showtimeId` |
| API endpoint | `GET /api/showtimes/{showtimeId}/seats` (sơ đồ + trạng thái phân bổ) |
| Thời điểm gọi | Khi vào màn hình; tải lại khi ghế thay đổi |
| Dữ liệu trả về | Ghế theo hàng: vị trí, loại ghế, trạng thái (trống/held/sold), phụ thu |
| Lỗi | 404 suất đã hủy → error state; ghế bị chọn khi đặt → toast + cập nhật sơ đồ |
| Hành vi UI | Chọn ghế (phiên cục bộ); "Đặt vé" → tạo booking giữ ghế |
| API tạo booking | `POST /api/bookings` (body: `showtimeId`, danh sách ghế + loại vé, `requestKey`) — trả về `bookingId`, `expires_at`, tổng tiền |

### 3.5. Payment

| Mục | Giá trị |
|---|---|
| Dữ liệu đầu vào | `bookingId` |
| API endpoint | `GET /api/bookings/{bookingId}` (tóm tắt booking); `POST /api/payments` (khởi tạo attempt); callback/kiểm tra kết quả qua endpoint payment của nhóm API |
| Thời điểm gọi | Tóm tắt khi vào trang; khởi tạo payment khi bấm "Thanh toán"; poll/kiểm tra kết quả theo thiết kế API |
| Dữ liệu trả về | Chi tiết booking + tổng tiền; trạng thái payment attempt |
| Lỗi | Hết `expires_at` → trạng thái expired, chặn thanh toán; attempt đang mở → nút disabled |
| Hành vi UI | Thành công → Booking Confirmation; thất bại → toast/modal; `unknown` → "Đang xác nhận" |

### 3.6. Booking Confirmation

| Mục | Giá trị |
|---|---|
| Dữ liệu đầu vào | `bookingId` |
| API endpoint | `GET /api/bookings/{bookingId}` (kèm vé sau khi xác nhận) |
| Thời điểm gọi | Sau khi chuyển từ Payment; tải lại khi vào trực tiếp |
| Dữ liệu trả về | Mã booking, suất, ghế, danh sách vé (mã + QR, ghế Couple = 2 vé riêng) |
| Lỗi | 403 (không phải chủ booking) → trang lỗi; chưa `confirmed` → chờ/kiểm tra lại |
| Hành vi UI | Hiển thị vé; điều hướng "Về trang chủ" / "Booking của tôi" |

### 3.7. Đăng nhập / Đăng ký

| Mục | Giá trị |
|---|---|
| Dữ liệu đầu vào | Email, mật khẩu (đăng ký: thêm họ tên, điện thoại) |
| API endpoint | `POST /api/auth/login`; `POST /api/auth/register` |
| Thời điểm gọi | Khi gửi form |
| Dữ liệu trả về | Token phiên + thông tin cơ bản user |
| Lỗi | Sai thông tin → thông báo lỗi chung; validation → lỗi dưới input |
| Hành vi UI | Thành công → quay lại URL lưu trước đó (xem `customer-navigation.md` 4.2) |

### 3.8. Tài khoản (Booking của tôi)

| Mục | Giá trị |
|---|---|
| Dữ liệu đầu vào | Token phiên, phân trang |
| API endpoint | `GET /api/users/me/bookings?page=...` |
| Thời điểm gọi | Khi vào trang; khi tải thêm |
| Dữ liệu trả về | Danh sách booking của user: trạng thái, suất, tổng tiền, vé |
| Lỗi | 401 → chuyển hướng đăng nhập; rỗng → empty state |
| Hành vi UI | Chọn booking → xem chi tiết/vé |

## 4. Lưu ý

- Thời gian hiển thị: nhận UTC từ API, frontend chuyển `Asia/Ho_Chi_Minh`.
- Không hiển thị dữ liệu giá do frontend tự tính; giá và tổng tiền lấy từ API (đã snapshot khi tạo booking).
- Mọi request cần xác thực gửi token phiên; authorization được backend kiểm tra.
