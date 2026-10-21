# Customer Screen Specification

## 1. Mục đích

Đặc tả chức năng và nội dung của từng màn hình Customer. Tài liệu trả lời câu hỏi: mỗi màn hình có gì?

## 2. Danh sách màn hình

1. Home (Danh sách phim)
2. Movie Detail
3. Showtime (Lịch chiếu)
4. Seat Selection (Sơ đồ ghế)
5. Payment
6. Booking Confirmation
7. Đăng nhập / Đăng ký
8. Tài khoản (Booking của tôi)

## 3. Đặc tả từng màn hình

### 3.1. Home

- **Mục đích:** giới thiệu phim đang chiếu / sắp chiếu, điểm vào luồng đặt vé.
- **Bố cục:** header, khu vực banner/danh sách phim dạng card, footer.
- **Dữ liệu hiển thị:** poster, tên phim, nhãn phân loại tuổi, trạng thái (`now_showing`/`coming_soon`).
- **Nút thao tác:** xem chi tiết phim (click card), tìm kiếm/lọc.
- **Validation:** không có.
- **Tiêu chí nghiệm thu:** danh sách phim tải từ API, hiển thị loading/empty state đúng.

### 3.2. Movie Detail

- **Mục đích:** cung cấp thông tin chi tiết phim và điểm chuyển sang chọn suất chiếu.
- **Bố cục:** poster + thông tin phim, nút "Chọn suất chiếu".
- **Dữ liệu hiển thị:** poster, tên phim, thời lượng, mô tả, ngôn ngữ, phân loại tuổi, trạng thái.
- **Nút thao tác:** "Chọn suất chiếu", quay lại.
- **Validation:** phim không tồn tại → error state.
- **Tiêu chí nghiệm thu:** đủ dữ liệu từ API movie; điều hướng sang Showtime đúng phim.

### 3.3. Showtime

- **Mục đích:** hiển thị lịch chiếu của phim để khách chọn suất.
- **Bố cục:** nhóm theo ngày → danh sách suất (giờ bắt đầu, phòng, giá cơ sở).
- **Dữ liệu hiển thị:** `starts_at`, tên phòng, giá cơ sở, trạng thái còn/đóng bán.
- **Nút thao tác:** chọn suất → Seat Selection.
- **Validation:** suất đã đóng bán hoặc đã hủy không thể chọn (disabled).
- **Tiêu chí nghiệm thu:** suất bị vô hiệu có trạng thái disabled; chọn suất chuyển đúng phòng/suất.

### 3.4. Seat Selection

- **Mục đích:** cho khách chọn ghế và loại vé, khởi tạo booking giữ ghế 5 phút.
- **Bố cục:** sơ đồ ghế theo hàng (row_label + seat_number), chú thích loại ghế (Standard/VIP/Couple), tổng tiền tạm tính, đồng hồ đếm ngược giữ ghế.
- **Dữ liệu hiển thị:** trạng thái ghế (trống / đã bán / đang chọn), loại ghế, phụ thu, giá từng ghế.
- **Nút thao tác:** chọn/bỏ chọn ghế, chọn loại vé cho từng ghế, "Đặt vé".
- **Validation:** ghế Couple chọn theo đơn vị ghế đôi; ghế đã bị người khác giữ/bán → thông báo và cập nhật sơ đồ.
- **Tiêu chí nghiệm thu:** giữ ghế thành công tạo booking; hết 5 phút → thông báo hết hạn, ghế được giải phóng.

### 3.5. Payment

- **Mục đích:** thực hiện thanh toán booking trong thời gian giữ ghế.
- **Bố cục:** tóm tắt booking (mã, suất, ghế, tổng tiền), chọn phương thức thanh toán, nút thanh toán.
- **Dữ liệu hiển thị:** `booking.total_amount` (VND), thời gian còn lại, chi tiết từng detail (giá đã snapshot).
- **Nút thao tác:** "Thanh toán", hủy booking.
- **Validation:** tổng tiền không được sửa; hết thời gian giữ ghế → chặn thanh toán, hiển thị expired.
- **Tiêu chí nghiệm thu:** thanh toán thành công chuyển sang Booking Confirmation; thất bại giữ booking `pending` cho phép retry theo chính sách.

### 3.6. Booking Confirmation

- **Mục đích:** xác nhận đặt vé thành công, hiển thị vé.
- **Bố cục:** mã booking, thông tin suất/ghế, danh sách vé với mã QR riêng cho từng vé (ghế Couple phát hành 2 vé riêng).
- **Dữ liệu hiển thị:** `booking_code`, giờ chiếu, phòng, ghế, mã vé/QR, trạng thái vé `valid`.
- **Nút thao tác:** về trang chủ, xem "Booking của tôi".
- **Tiêu chí nghiệm thu:** số vé hiển thị đúng số ghế/capacity_units; trạng thái `confirmed` chỉ xuất hiện sau khi payment được áp dụng.

### 3.7. Đăng nhập / Đăng ký

- **Mục đích:** xác thực người dùng trước khi đặt vé.
- **Dữ liệu hiển thị:** form email, mật khẩu; đăng ký chỉ cấp role `CUSTOMER`.
- **Validation:** định dạng email, mật khẩu bắt buộc; lỗi xác thực hiển thị thông báo chung.
- **Tiêu chí nghiệm thu:** đăng nhập thành công quay lại màn hình trước khi bị chuyển hướng.

### 3.8. Tài khoản (Booking của tôi)

- **Mục đích:** xem lịch sử booking và vé của user.
- **Dữ liệu hiển thị:** danh sách booking với trạng thái (`pending`, `confirmed`, `expired`, `cancelled`), vé theo booking.
- **Tiêu chí nghiệm thu:** chỉ hiển thị booking của user hiện tại (authorization ở backend).
