# Customer User Flow

## 1. Mục đích

Mô tả hành trình của Customer khi sử dụng hệ thống, từ lúc truy cập website đến khi đặt vé thành công. Tài liệu trả lời câu hỏi: người dùng đi đâu, theo trình tự nào?

## 2. Luồng chính

### 2.1. Xem phim và chọn suất chiếu

1. Customer truy cập trang chủ (Home) và xem danh sách phim đang chiếu / sắp chiếu.
2. Customer chọn một phim → chuyển sang Movie Detail.
3. Customer nhấn "Chọn suất chiếu" → chuyển sang Showtime.
4. Customer chọn rạp/phòng và suất chiếu phù hợp → chuyển sang Seat Selection.

### 2.2. Chọn ghế và đặt vé

5. Customer chọn ghế trên sơ đồ phòng (ghế Couple được chọn theo đơn vị ghế đôi).
6. Customer chọn loại vé áp dụng cho từng ghế (backend xác minh điều kiện áp dụng).
7. Customer nhấn "Đặt vé" → hệ thống tạo booking và giữ ghế trong 5 phút.
8. Customer nhập/xác nhận thông tin liên hệ → chuyển sang Payment.

### 2.3. Thanh toán

9. Customer thanh toán qua cổng thanh toán.
10. Thanh toán thành công → Booking Confirmation hiển thị mã booking và vé (QR).
11. Thanh toán thất bại / hết thời gian giữ ghế → hiển thị thông báo và cho phép quay lại.

## 3. Điều kiện chuyển bước

| Bước chuyển | Điều kiện | Xử lý khi thất bại |
|---|---|---|
| Home → Movie Detail | Phim tồn tại và đang được hiển thị | Quay lại Home, thông báo phim không khả dụng |
| Movie Detail → Showtime | Có ít nhất một suất chiếu | Hiển thị empty state "Chưa có suất chiếu" |
| Showtime → Seat Selection | Suất còn bán vé (chưa quá `booking_closes_at`/giờ chiếu) | Thông báo suất đã đóng bán |
| Seat Selection → Payment | Đã chọn ít nhất một ghế; booking tạo thành công (ghế chưa bị giữ/bán) | Thông báo ghế đã bị chọn, yêu cầu chọn lại |
| Payment → Booking Confirmation | Payment `succeeded` và được áp dụng trong thời gian giữ ghế | Hiển thị lỗi thanh toán; nếu quá `expires_at` booking chuyển `expired` và ghế được giải phóng |

## 4. Nhánh và ngoại lệ

### 4.1. Người dùng hủy hoặc quay lại

- Người dùng rời khỏi Seat Selection / Payment mà không hoàn tất: booking `pending` hết hạn sau 5 phút, ghế chuyển `released` bởi worker.
- Quay lại màn hình trước (Back/Forward) không mất trạng thái ghế đã chọn trong phiên hiện tại.
- Hủy booking có chủ đích trước khi thanh toán: booking chuyển `cancelled`, ghế được giải phóng.

### 4.2. Chưa đăng nhập

- Xem phim, suất chiếu: cho phép không cần đăng nhập.
- Đặt vé / thanh toán: yêu cầu đăng nhập; hệ thống điều hướng tới đăng nhập rồi quay lại màn hình đang dở (xem `customer-navigation.md`).

## 5. Tài liệu liên quan

- `customer-screen-specification.md`: nội dung từng màn hình trong luồng.
- `customer-navigation.md`: quy tắc điều hướng giữa các bước.
- `customer-ui-state.md`: trạng thái giao diện theo từng bước.
- `customer-screen-api-mapping.md`: dữ liệu và API cần cho từng bước.
