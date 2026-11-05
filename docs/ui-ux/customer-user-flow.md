# Customer User Flow

## 1. Mục đích và phạm vi

Tài liệu mô tả luồng Guest/Customer từ duyệt phim đến kết quả thanh toán và truy cập vé. Guest có thể dùng các chức năng công khai và Guest Checkout; Customer đã đăng nhập có thêm lịch sử booking và hồ sơ tài khoản.

## 2. Luồng chính

```text
Home
  → Movie List / Movie Detail
  → Showtime
  → Seat Selection
  → Ticket Type
  → Checkout Information
  → Payment
  → Payment Result
  → My Tickets
```

1. Guest hoặc Customer xem phim qua UC-06A Browse Movies và UC-06B View Movie Details.
2. Người dùng chọn suất còn bán qua UC-07 Browse Showtimes.
3. Người dùng chọn các ghế đơn và số lượng vé Adult/Discounted. Ghế Couple/multi-person không thuộc phạm vi PA3.
4. Khi chọn ghế đầu tiên, hệ thống mở selection session do server kiểm soát với thời hạn gốc 5 phút. Việc chọn ghế chưa tạo booking hoặc giữ ghế.
5. Người dùng nhập thông tin liên hệ ở Checkout Information. Guest không cần đăng ký hoặc đăng nhập.
6. Ở Checkout Step 2, backend kiểm tra lại selection session, ghế, số lượng, giá và quyền sở hữu session rồi tạo đúng một booking `pending` cùng các allocation `held` atomically.
7. Booking kế thừa thời hạn còn lại của selection session; tạo booking không khởi động lại đồng hồ 5 phút.
8. UC-09 tạo payment attempt hợp lệ và mở cửa sổ thanh toán riêng 10 phút. Cửa sổ này không được gia hạn bởi redirect, đóng trang hoặc webhook retry.
9. UC-02 xác minh payment bất đồng bộ. Chỉ khi payment được áp dụng, booking `confirmed`, allocation `sold` và entitlement vé đã commit thì Payment Result mới hiển thị thành công.
10. Payment Result dẫn tới My Tickets hoặc luồng đọc booking/vé Guest đã được xác minh. Một booking confirmed có một booking-level QR; không tạo QR usable cho booking chưa confirmed.

## 3. Điều kiện chuyển bước

| Bước chuyển | Điều kiện | Xử lý khi thất bại |
|---|---|---|
| Home/Movie List → Movie Detail | Phim còn tồn tại và được phép công khai | Hiển thị Not Found/empty và cho quay lại danh sách |
| Movie Detail → Showtime | Có thể tải lịch chiếu authoritative | Empty nếu chưa có suất; lỗi đọc có Retry |
| Showtime → Seat Selection | Suất `scheduled`, còn bán và ghế thuộc đúng phòng | Vô hiệu suất đã đóng/hủy; tải lại lịch |
| Seat Selection → Ticket Type | Có ít nhất một ghế đơn `available` | Hiển thị lỗi và giữ draft hợp lệ |
| Ticket Type → Checkout Information | Số lượng Adult/Discounted hợp lệ và khớp số ghế | Nêu lỗi tại lựa chọn; không tạo booking |
| Checkout Information → Payment | Selection session còn hạn; backend tạo booking `pending` atomically | Xung đột ghế/validation không giữ một phần; yêu cầu chọn lại |
| Payment → Payment Result | Có payment attempt và đọc được trạng thái authoritative | Giữ trạng thái đang xác minh; không suy từ redirect |
| Payment Result → My Tickets | Payment đã `applied`, booking `confirmed`, allocation `sold`, ticket entitlement đã phát hành | Không hiển thị QR thành công; chuyển đối soát nếu cần |

## 4. Nhánh và ngoại lệ

### 4.1. Guest và Customer

- Guest được duyệt phim, xem suất, chọn ghế, checkout và thanh toán mà không cần account.
- Customer dùng principal đã xác thực; booking history chỉ hiển thị booking của chính Customer.
- Guest đọc booking/payment/ticket bằng guest-safe session hoặc cơ chế xác minh đã được phê duyệt; biết `bookingCode` không tự cấp quyền.
- Logout không tự hủy booking/payment. Guest hoặc Customer rời trang cũng không tự hủy booking.

### 4.2. Hết hạn và hủy

- Hết selection session trước khi tạo booking: không có ghế backend để giải phóng; người dùng phải chọn lại.
- Booking `pending` sử dụng thời hạn còn lại của selection session. Countdown bằng 0 không tự chứng minh booking đã `expired`; worker và payment-safety rules quyết định.
- Payment window 10 phút hết hạn không tự chứng minh payment failed và không tự nhả ghế khi còn attempt mở hoặc kết quả chưa rõ.
- Booking `pending` chỉ được hủy khi backend xác minh không có payment attempt mở/chưa rõ. Booking confirmed không có hủy/refund Customer trong phạm vi demo.

### 4.3. Kết quả thanh toán

- `failed`/`cancelled`: chỉ cho thử lại khi backend xác nhận booking còn đủ điều kiện và attempt cũ đã chốt.
- `pending`/`unknown`: hiển thị đang xác minh; không tạo attempt song song.
- `succeeded` nhưng chưa `applied`, hoặc booking đã expired/cancelled: giữ bằng chứng tài chính, không xác nhận booking, không chiếm lại ghế và chuyển UC-04 khi cần.
- Webhook đến trễ/lặp/sai thứ tự không tạo vé hoặc QR trùng; backend xử lý idempotent.

## 5. Tài liệu liên quan

- `customer-screen-specification.md`: nội dung từng màn hình.
- `customer-navigation.md`: route, guard và chuyển màn hình.
- `customer-ui-state.md`: trạng thái giao diện và thông báo.
- `customer-screen-api-mapping.md`: dữ liệu/API và ranh giới Guest/Customer.
- `../requirements/use-cases.md`: UC-01, UC-02, UC-03, UC-06A, UC-06B, UC-07, UC-08A..UC-08D, UC-09, UC-10.
