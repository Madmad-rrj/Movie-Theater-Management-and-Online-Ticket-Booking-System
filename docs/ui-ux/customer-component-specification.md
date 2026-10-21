# Customer Component Specification

## 1. Mục đích

Đặc tả các UI component được tái sử dụng trên nhiều màn hình. Tài liệu trả lời câu hỏi: các thành phần giao diện hoạt động ra sao?

## 2. Danh sách component

Button, Input, Select, Movie Card, Seat, Modal, Dropdown, Tabs, Loading Indicator, Toast/Notification, Empty State, Countdown Timer, QR Ticket Card.

## 3. Đặc tả từng component

### 3.1. Button

- **Biến thể:** primary (hành động chính: "Đặt vé", "Thanh toán"), secondary ("Quay lại"), danger ("Hủy booking"), disabled.
- **Trạng thái:** default, hover, focus, loading (hiện spinner, chặn click), disabled.
- **Quy tắc dùng chung:** mỗi khối nội dung chỉ một primary button; loading/disabled do trạng thái dữ liệu quyết định, không do caller tự đặt.

### 3.2. Input / Select

- **Loại:** text, email, password, select (danh sách cố định).
- **Trạng thái:** default, focus, error (viền đỏ + thông báo bên dưới), disabled.
- **Validation:** thông báo lỗi hiển thị sau blur/sự kiện xác nhận, không chặn gõ.

### 3.3. Movie Card

- **Cấu trúc:** poster (tỉ lệ cố định), tên phim, nhãn phân loại tuổi, nhãn trạng thái (`now_showing`/`coming_soon`), nút/click vùng xem chi tiết.
- **Trạng thái:** default, hover (hiệu ứng nhấn), skeleton (khi tải).
- **Dữ liệu:** lấy từ API movie; card không tự gọi API (danh sách gọi một lần và truyền dữ liệu xuống).

### 3.4. Seat

- **Cấu trúc:** ô ghế theo hình dạng loại ghế (Standard: ô đơn; VIP: ô đơn viền nhấn; Couple: ô ngang rộng gấp đôi).
- **Trạng thái:** trống (selectable), selected (đang chọn của phiên), held/sold (disabled).
- **Quy tắc:** ghế Couple chọn/bỏ chọn theo đơn vị ghế đôi; trạng thái ghế lấy từ sơ đồ API, component không tự suy luận.

### 3.5. Modal

- **Dùng cho:** xác nhận hủy booking, thông báo ghế bị chọn, chi tiết giá.
- **Trạng thái:** open, closed; chặn tương tác nền; đóng bằng nút X/nút phụ/nhấn Escape.
- **Quy tắc:** chỉ một modal mở tại một thời điểm.

### 3.6. Dropdown

- **Dùng cho:** menu tài khoản ở header, chọn ngày trong Showtime (trên mobile).
- **Trạng thái:** closed/open; đóng khi click ngoài.

### 3.7. Tabs

- **Dùng cho:** bộ chọn ngày trong Showtime, phân nhóm "Đang chiếu"/"Sắp chiếu".
- **Trạng thái:** active/inactive; đổi tab làm cập nhật danh sách dữ liệu (loading state).

### 3.8. Loading Indicator

- **Biến thể:** spinner inline (nút), skeleton (card/khối), fullscreen bên trong main (tải màn hình).
- **Quy tắc:** hiển thị khi API đang gọi; không dùng để che lỗi.

### 3.9. Toast / Notification

- **Loại:** success, error, warning, info.
- **Vị trí:** góc phải trên (dưới header), tự đóng sau thời gian cố định.
- **Quy tắc:** thông báo lỗi nghiệp vụ (ghế bị chọn, thanh toán thất bại) dùng toast; lỗi chặn luồng dùng modal hoặc trạng thái trang.

### 3.10. Empty State

- **Cấu trúc:** icon minh họa, câu thông báo, nút hành động tùy chọn (ví dụ "Chưa có suất chiếu").
- **Quy tắc:** chỉ hiển thị sau khi dữ liệu đã tải xong và rỗng, không nhầm với loading.

### 3.11. Countdown Timer

- **Dùng cho:** đồng hồ giữ ghế 5 phút trên Seat Selection/Payment.
- **Trạng thái:** running, hết hạn (hiển thị 0:00 và kích hoạt thông báo hết hạn một lần).

### 3.12. QR Ticket Card

- **Cấu trúc:** mã vé, mã QR riêng, tên ghế, trạng thái vé (`valid`/`used`/`voided`).
- **Quy tắc:** ghế Couple hiển thị 2 thẻ vé riêng trong cùng khối.

## 4. Quy tắc dùng chung

- Component không tự gọi API; dữ liệu truyền từ trang (xem `customer-screen-api-mapping.md`).
- Trạng thái component phản ánh trạng thái dữ liệu theo `customer-ui-state.md`.
- Định dạng hiển thị thống nhất: tiền VND nguyên đồng, thời gian `Asia/Ho_Chi_Minh`.
- Biến thể component được khai báo tập trung; tránh tạo component "nhánh" riêng cho từng trang.
