# Customer UI State

## 1. Mục đích

Xác định các trạng thái giao diện và cách UI phản ứng khi dữ liệu hoặc thao tác thay đổi. Tài liệu trả lời câu hỏi: giao diện phản ứng thế nào theo trạng thái?

## 2. Trạng thái dữ liệu chuẩn

### 2.1. Loading

- Kích hoạt khi gọi API lần đầu hoặc khi dữ liệu chính của trang thay đổi (đổi ngày ở Showtime).
- Biểu hiện: skeleton cho khối danh sách, spinner cho nút; chặn tương tác tạo dữ liệu trùng lặp.
- Kết thúc khi API trả về (chuyển sang success/error/empty), không hiển thị chờ quá cần thiết.

### 2.2. Success

- Kích hoạt khi API trả về dữ liệu hợp lệ và không rỗng.
- Biểu hiện: hiển thị dữ liệu theo đặc tả màn hình (`customer-screen-specification.md`).

### 2.3. Empty state

- Kích hoạt khi API trả về danh sách rỗng.
- Ví dụ: Home không có phim → "Hiện chưa có phim"; Movie Detail không có suất → "Chưa có suất chiếu".
- Biểu hiện: khối empty state với thông báo + hành động tùy chọn; không hiển thị skeleton.

### 2.4. Error

- Kích hoạt khi API lỗi (mạng, lỗi máy chủ, lỗi xác thực).
- Biểu hiện: lỗi toàn trang → trạng thái lỗi trong main kèm nút "Thử lại"; lỗi thao tác → toast/modal kèm thông báo nghiệp vụ.
- Nguyên tắc: hiển thị thông báo nghiệp vụ thân thiện, không hiển thị stack/chi tiết kỹ thuật.

## 3. Trạng thái thao tác

### 3.1. Disabled

- Nút "Đặt vé" disabled khi chưa chọn ghế nào hoặc đang xử lý.
- Ghế disabled khi `held`/`sold`, suất disabled khi đã đóng bán/hủy.
- Nút thanh toán disabled khi còn payment attempt đang mở (`pending`/`unknown`).

### 3.2. Selected

- Ghế đang chọn trong phiên: trạng thái selected, ghi vào tổng tiền tạm tính.
- Tab ngày đang xem: trạng thái active.
- Selected chỉ là trạng thái phiên người dùng, không đồng nghĩa dữ liệu đã ghi ở backend (điều đó xảy ra khi tạo booking giữ ghế thành công).

### 3.3. Validation error

- Kích hoạt khi input không đạt quy tắc (email sai định dạng, bỏ trống trường bắt buộc).
- Biểu hiện: viền lỗi + thông báo dưới input; không tự biến mất khi đang focus lại.

## 4. Trạng thái nghiệp vụ

### 4.1. Trạng thái đăng nhập

- Chưa đăng nhập: header hiển thị "Đăng nhập/Đăng ký"; vào trang guarded → chuyển hướng đăng nhập.
- Đã đăng nhập: header hiển thị menu tài khoản; trạng thái phân giải từ phiên xác thực (token).
- Đăng xuất: toàn bộ trạng thái tài khoản và dữ liệu private bị xóa khỏi UI.

### 4.2. Trạng thái giữ ghế (booking `pending`)

- Sau khi tạo booking: đồng hồ đếm ngược 5 phút hiển thị trên Seat Selection/Payment.
- Còn thời gian: cho phép thanh toán; UI khóa sơ đồ ghế theo phân bổ đã giữ.
- Hết thời gian (`expires_at` đã qua, backend chuyển `expired`): hiển thị "Đã hết thời gian giữ ghế", ghế được giải phóng, sơ đồ cập nhật lại, không cho thanh toán.
- Ghế bị người khác giữ/bán trước khi tạo booking: toast "Ghế vừa được chọn" + cập nhật sơ đồ.

### 4.3. Trạng thái thanh toán

- Đang xử lý: nút thanh toán loading; không cho hủy booking trong lúc xử lý.
- Thành công: chuyển Booking Confirmation.
- Thất bại: toast/modal lỗi, giữ booking `pending` cho phép retry khi còn thời gian giữ ghế.
- Kết quả `unknown`: hiển thị "Kết quả đang được xác nhận" và hướng dẫn đợi đối soát, không mặc nhiên báo thất bại.

## 5. Ma trận trạng thái tổng hợp

| Trạng thái | Kích hoạt | Biểu hiện | Chuyển tiếp |
|---|---|---|---|
| Loading | Bắt đầu gọi API | Skeleton/spinner | Success / Empty / Error |
| Success | API trả dữ liệu | Hiển thị dữ liệu | — |
| Empty | Dữ liệu rỗng | Empty state | — |
| Error | API lỗi | Trang lỗi / toast | Retry → Loading |
| Disabled | Điều kiện nghiệp vụ không đạt | Mờ, chặn click | Đạt điều kiện → Enabled |
| Selected | Tương tác của người dùng | Đánh dấu nổi bật | Bỏ chọn / Xác nhận |
| Validation error | Input sai quy tắc | Viền lỗi + message | Sửa hợp lệ → Hết lỗi |
| Giữ ghế hết hạn | Quá `expires_at` | Thông báo + mở khóa ghế | Chọn lại ghế |
