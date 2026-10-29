# Customer Screen Specification

## 1. Mục đích

Đặc tả chức năng và nội dung của từng màn hình Customer. Tài liệu trả lời câu hỏi: mỗi màn hình có gì?

## 2. Danh sách màn hình Customer — CIN-SUB-048

### 2.1. Phạm vi và cách định danh

Màn hình dưới đây là đơn vị chức năng để thiết kế và kiểm thử; không tự xác định URL hoặc bắt buộc tạo một route cho mỗi mục. Ticket Type có thể là bước/vùng trong chọn ghế; Payment Result có thể là chế độ xem của trang thanh toán. Cách gộp trang là **đề xuất UI**, không làm thay đổi quyền truy cập và nghiệp vụ.

API/DTO được nêu trong bộ tài liệu hiện còn ở mức Proposed. Đặc tả màn hình không đồng nghĩa chức năng đã triển khai. Những dữ liệu hoặc thao tác chưa có hợp đồng được ghi **TBD**, không tự thêm endpoint.

### 2.2. Nhóm màn hình và mục đích

| Nhóm | Mã | Màn hình | Mục đích | Quyền truy cập | Màn hình liên quan |
|---|---|---|---|---|---|
| Khám phá phim | KH-01 | Home / danh sách phim | Duyệt đang chiếu, sắp chiếu; chọn phim | Công khai | KH-02, KH-07, KH-08 |
| Khám phá phim | KH-02 | Movie Detail | Xem thông tin và mở lịch của đúng phim | Công khai | KH-01, KH-03 |
| Khám phá phim | KH-03 | Showtime | Chọn ngày và suất còn được bán | Công khai | KH-02, KH-04 |
| Đặt vé | KH-04 | Seat Selection | Chọn đơn vị ghế cho một suất | Xem sơ đồ công khai theo endpoint dự kiến; tạo booking cần đăng nhập | KH-03, KH-09, KH-07, KH-05 |
| Đặt vé | KH-09 | Ticket Type | Gán một loại vé hợp lệ cho mỗi đơn vị ghế | Theo ngữ cảnh KH-04; gửi đặt cần đăng nhập | KH-04, vùng kiểm tra đơn, KH-05 |
| Đặt vé | KH-06 | Booking Confirmation | Kiểm tra thông tin đơn và phân biệt đơn chờ với kết quả đã xác nhận | Chủ booking đã đăng nhập khi đã có đơn | KH-04, KH-09, KH-05, KH-10, KH-11 |
| Thanh toán | KH-05 | Payment | Bắt đầu thanh toán đúng đơn và số tiền chốt | Chủ booking đã đăng nhập | KH-06, KH-10, KH-08 |
| Thanh toán | KH-10 | Payment Result | Xem kết quả payment và việc booking đã được xác nhận hay chưa | Chủ booking đã đăng nhập | KH-05, KH-06, KH-11, KH-08 |
| Xác thực | KH-07 | Đăng nhập / đăng ký | Tạo tài khoản Customer hoặc phiên xác thực | Công khai; đích quay lại theo ngữ cảnh | Các trang được bảo vệ, KH-01 |
| Tài khoản | KH-08 | Booking History / chi tiết booking | Tra cứu mọi đơn của chính mình, kể cả pending/expired/cancelled | Customer đã đăng nhập, kiểm tra ownership | KH-05, KH-06, KH-10, KH-11, KH-12 |
| Tài khoản | KH-11 | My Tickets / chi tiết vé | Tra cứu vé đã được phát hành của chính mình | Chủ booking đã đăng nhập | KH-06, KH-08, KH-10 |
| Tài khoản | KH-12 | Profile | Xem và cập nhật hồ sơ trong phạm vi được cho phép | User đã đăng nhập, chỉ hồ sơ của mình | KH-07, KH-08 |

### 2.3. Ranh giới giữa các màn hình

- Home dùng danh sách phim và phân nhóm; không tạo thêm màn Movie List trùng chức năng nếu không có nhu cầu giao diện riêng.
- Seat Selection chọn ghế; Ticket Type gán loại vé cho từng ghế. Cả hai dùng chung lựa chọn trong phiên, không tạo hai booking hoặc hai đồng hồ giữ ghế.
- **Booking Confirmation có hai ngữ cảnh cần gọi tên rõ:** vùng kiểm tra đơn trước thanh toán và phần xác nhận sau khi backend trả booking `confirmed`. Vùng kiểm tra trước thanh toán có thể nằm trong KH-05; đây là đề xuất tổ chức UI, không phải thao tác tự đổi booking sang confirmed.
- User Flow hiện mô tả Booking Confirmation sau payment. Khi thiết kế vùng kiểm tra trước thanh toán theo phân công, giữ nguyên điều kiện xác nhận nghiệp vụ; cần thống nhất vị trí bước với người phụ trách User Flow/Navigation, không tự sửa luồng đã chốt.
- Payment thao tác tạo/theo dõi attempt; Payment Result hiển thị kết quả. Payment `succeeded` không tự chứng minh booking `confirmed` hoặc vé đã phát hành.
- Booking History liệt kê đơn ở mọi trạng thái; My Tickets chỉ trình bày vé đã phát hành, không biến đơn pending thành vé. Không tự thêm API danh sách vé chưa được chốt.
- Profile là chức năng hồ sơ, không gộp với lịch sử đơn; endpoint, trường được sửa và validation hồ sơ còn TBD. Không mặc định có đổi email/mật khẩu.
- Đăng nhập và đăng ký là hai form trong cùng nhóm KH-07. Response register chưa có token; chỉ tiếp tục trang được bảo vệ khi đã có phiên hợp lệ.

### 2.4. Quy tắc truy cập và điều hướng chung

1. Public: Home, Movie Detail, Showtime; sơ đồ ghế có API đọc công khai. Không yêu cầu đăng nhập chỉ để xem phim/lịch.
2. Tạo booking, thanh toán và đọc đơn/vé/hồ sơ cá nhân cần phiên hợp lệ. Chưa đăng nhập thì lưu đích nội bộ và chuyển KH-07; quay lại phải tải lại dữ liệu.
3. Backend kiểm tra role và ownership trên mọi tài nguyên riêng tư. Biết UUID, mã booking hoặc ticketCode không tạo quyền xem.
4. Back/Forward và đổi màn hình không tự gửi lại POST, hủy đơn, giải phóng ghế hoặc gia hạn `expiresAt`.
5. Chỉ chạy countdown sau khi backend tạo booking thành công; chọn ghế trong phiên chưa giữ ghế. Countdown về 0 chỉ yêu cầu kiểm tra trạng thái, không tự xác nhận expired/released.
6. Chỉ một attempt `created`/`pending`/`unknown` được mở trên mỗi booking. Không tạo attempt mới vì chưa có kết quả từ cổng hoặc vì chuyển trang kết quả.
7. Header có logo, nhóm phim và tài khoản theo phiên. Menu mới cho My Tickets/Profile là đề xuất điều hướng cần phối hợp; quyền truy cập vẫn áp dụng dù mở trực tiếp.

### 2.5. Đối chiếu phạm vi đặc tả

| Nhu cầu | Màn hình chịu trách nhiệm | Căn cứ / sub-task |
|---|---|---|
| Duyệt phim, xem lịch và sơ đồ | KH-01..04 | FR-MOVIE-001/002, FR-SHOW-001/002 |
| Giữ ghế, loại vé và kiểm tra đơn | KH-04, KH-09, KH-06 | FR-BOOK-001/002; CIN-SUB-055/056/057 |
| Thanh toán và kết quả độc lập với booking | KH-05, KH-10 | FR-PAY-001..005; CIN-SUB-058/059 |
| Vé của tôi | KH-11, KH-06 | FR-TICKET-001; CIN-SUB-060 |
| Lịch sử và chi tiết đơn | KH-08 | FR-BOOK-004; CIN-SUB-061 |
| Xác thực và hồ sơ | KH-07, KH-12 | FR-AUTH-001..004; CIN-SUB-062 |

### 2.6. Tiêu chí nghiệm thu danh mục

- Đủ 12 mã chức năng không trùng; mỗi mục có mục đích, quyền và màn hình liên quan.
- Phân biệt rõ Ticket Type, Payment Result, My Tickets, Booking History và Profile; không cần endpoint/route mới chỉ để tách mục thiết kế.
- Có điểm kiểm tra đơn trước thanh toán nhưng không báo đã thanh toán/confirmed trước kết quả backend.
- Các giới hạn nghiệp vụ/API còn thiếu được giữ TBD; danh mục là cơ sở cho đặc tả chi tiết, không phải báo cáo kiểm thử runtime.

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
