# Customer Navigation

## 1. Mục đích

Quy định cách người dùng di chuyển giữa các màn hình. Tài liệu trả lời câu hỏi: chuyển giữa các màn hình bằng cách nào?

## 2. Cấu trúc điều hướng toàn cục

### 2.1. Header

- Logo → Home.
- Liên kết: "Phim đang chiếu", "Sắp chiếu".
- Khu vực tài khoản:
  - Chưa đăng nhập: nút "Đăng nhập" / "Đăng ký".
  - Đã đăng nhập: menu tên người dùng → "Booking của tôi", "Đăng xuất".

### 2.2. Footer

- Thông tin rạp, liên kết tĩnh (giới thiệu, điều khoản).
- Footer không chứa điều hướng nghiệp vụ bắt buộc.

## 3. Quy tắc điều hướng giữa các trang

| Từ màn hình | Thao tác | Đến màn hình |
|---|---|---|
| Home | Click movie card | Movie Detail |
| Movie Detail | "Chọn suất chiếu" | Showtime (với phim đang chọn) |
| Showtime | Chọn suất còn bán | Seat Selection |
| Seat Selection | "Đặt vé" (đã chọn ≥ 1 ghế) | Payment |
| Payment | Thanh toán thành công | Booking Confirmation |
| Booking Confirmation | "Về trang chủ" | Home |
| Bất kỳ | Logo / "Phim đang chiếu" | Home |

## 4. Điều hướng sau thao tác

### 4.1. Sau đặt vé

- Tạo booking thành công → đến Payment, đồng hồ đếm ngược giữ ghế tiếp tục chạy khi di chuyển trong phạm vi luồng đặt vé.
- Rời khỏi luồng đặt vé (về Home) khi booking `pending`: hiển thị cảnh báo mất ghế nếu hết thời gian giữ.

### 4.2. Sau đăng nhập / đăng ký

- Nếu được chuyển hướng từ luồng đặt vé (guarded route): quay lại đúng màn hình trước đó.
- Nếu truy cập trực tiếp: về Home.

### 4.3. Sau đăng xuất

- Về Home; không giữ trạng thái đăng nhập; các trang cần đăng nhập chuyển hướng sang Đăng nhập.

## 5. Back / Forward

- Back/Forward của trình duyệt hoạt động theo lịch sử điều hướng; các màn hình dữ liệu động (Showtime, Seat Selection) tải lại dữ liệu khi vào lại.
- Back khỏi Seat Selection/Payment không tự hủy booking; booking `pending` hết hạn theo thời gian giữ ghế (5 phút).
- Không chặn Back/Forward; tránh trạng thái "form đã gửi" bị gửi lại (trang thanh toán dùng trạng thái phiên, không dùng resubmit).

## 6. Điều hướng khi chưa đăng nhập

- Các trang xem (Home, Movie Detail, Showtime): truy cập tự do.
- Các trang cần đăng nhập (Payment, Booking Confirmation, Booking của tôi): chuyển hướng đến Đăng nhập và lưu URL gốc để quay lại sau khi đăng nhập thành công (xem 4.2).
- Authorization luôn được backend kiểm tra; frontend chỉ ẩn/hiện điều hướng theo trạng thái đăng nhập, không dùng nó làm bảo mật.
