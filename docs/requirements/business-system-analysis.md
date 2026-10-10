# Business System Analysis

## 1. Xác định loại hệ thống

<!--
Xác định hệ thống đang xây dựng là loại hệ thống nào, nền tảng nào
và mục đích tổng quát của hệ thống.

Cần mô tả:

- Loại hệ thống: Web App bán vé xem phim.
- Đối tượng sử dụng chính.
- Mục đích của hệ thống.
- Các thành phần chính mà hệ thống cần quản lý.
-->

## 2. Xác định phạm vi hệ thống

<!--
Xác định rõ hệ thống sẽ làm những gì và không làm những gì.

Cần xác định:

- Các nghiệp vụ nằm trong phạm vi hệ thống.
- Các chức năng chính được triển khai.
- Các chức năng không thuộc phạm vi dự án.
- Ranh giới giữa hệ thống và các hệ thống bên ngoài.

Mục tiêu là tránh việc trong quá trình phát triển phát sinh thêm
nghiệp vụ không nằm trong phạm vi ban đầu.
-->

## 3. Xác định Actor

<!--
Xác định tất cả các đối tượng bên ngoài có tương tác với hệ thống.

Với mỗi Actor cần mô tả:

- Tên Actor.
- Vai trò.
- Mục đích sử dụng hệ thống.
- Các nghiệp vụ/chức năng mà Actor có thể thực hiện.

Các Actor dự kiến:

- Customer
- Staff
- Manager
- Admin

Nếu có hệ thống bên ngoài như Payment Gateway thì cũng xác định
đó là một Actor / External System.
-->

## 4. Xác định nghiệp vụ chính

<!--
Liệt kê các nghiệp vụ lớn mà hệ thống phải hỗ trợ.

Mỗi nghiệp vụ cần mô tả:

- Tên nghiệp vụ.
- Actor thực hiện.
- Mục đích.
- Kết quả mong muốn.

Ví dụ:

- Xem phim.
- Xem lịch chiếu.
- Chọn ghế.
- Đặt vé.
- Thanh toán.
- Nhận vé.
- Quản lý phim.
- Quản lý phòng chiếu.
- Quản lý suất chiếu.
- Quản lý booking.
-->

## 5. Xác định đối tượng dữ liệu

<!--
Xác định các đối tượng dữ liệu chính xuất hiện trong nghiệp vụ.

Đây là cơ sở ban đầu cho việc thiết kế Database.

Liệt kê:

- Tên đối tượng.
- Mục đích của đối tượng.
- Các thông tin chính mà đối tượng cần lưu trữ.
- Quan hệ sơ bộ với các đối tượng khác.

Các đối tượng dự kiến:

Movie, Genre, CinemaRoom, Seat, SeatType, TicketType,
Showtime, Booking, BookingDetail, Ticket, Payment,
User, Role,...

Lưu ý: đây mới là phân tích nghiệp vụ, chưa cần quyết định
chi tiết PK/FK hoặc kiểu dữ liệu Database.
-->

## 6. Phân tích luồng khách hàng

<!--
Mô tả toàn bộ quy trình mà Customer thực hiện khi sử dụng hệ thống.

Cần xác định:

- Điểm bắt đầu.
- Các bước người dùng thực hiện.
- Điều kiện/rẽ nhánh trong quá trình thực hiện.
- Kết quả cuối cùng.

Ví dụ luồng chính:

Home
→ Movie Detail
→ Showtime
→ Seat Selection
→ Ticket Type
→ Booking Confirmation
→ Payment
→ Payment Result
→ My Tickets

Ngoài luồng chính cần xác định các trường hợp như:

- Chưa đăng nhập.
- Ghế đã được người khác chọn.
- Hết thời gian giữ ghế.
- Thanh toán thất bại.
- Booking bị hủy.
-->

## 7. Phân tích luồng quản trị

<!--
Mô tả quy trình Staff / Manager / Admin sử dụng hệ thống quản trị.

Cần xác định:

- Đăng nhập.
- Truy cập khu vực quản trị.
- Các module được phép truy cập.
- Các thao tác CRUD.
- Quyền hạn khác nhau giữa các Role.

Ví dụ:

Login
→ Dashboard
→ Movie Management
→ Cinema Room Management
→ Seat Management
→ Showtime Management
→ Booking Management
→ Ticket Management
→ Payment Management
→ User Management
-->

## 8. Phân tích nghiệp vụ đặt vé

<!--
Phân tích chi tiết quy trình Booking từ lúc Customer chọn ghế
cho đến khi hoàn tất hoặc hủy.

Cần xác định:

- Khi nào Booking được tạo.
- Khi nào ghế được giữ.
- Thời gian giữ ghế.
- Điều kiện để ghế được đặt thành công.
- Điều kiện khiến Booking thất bại.
- Cách tính tổng tiền.
- Khi nào Booking chuyển trạng thái.
- Khi nào Ticket được phát hành.
- Điều kiện hủy Booking.

Cần đặc biệt làm rõ trường hợp nhiều Customer cùng chọn một ghế.
-->

## 9. Phân tích nghiệp vụ phòng chiếu

<!--
Xác định cách hệ thống quản lý Cinema Room và Seat.

Cần xác định:

- Phòng chiếu có những thông tin gì.
- Phòng có bao nhiêu ghế.
- Cách đánh số / xác định vị trí ghế.
- Các loại ghế.
- Sức chứa phòng.
- Trạng thái phòng.
- Quy tắc thêm/sửa/xóa ghế.
- Quan hệ giữa phòng, ghế và suất chiếu.
-->

## 10. Phân tích nghiệp vụ lịch chiếu

<!--
Xác định cách tạo và quản lý Showtime.

Một Showtime phải xác định được:

- Phim nào.
- Phòng nào.
- Thời gian bắt đầu.
- Thời gian kết thúc.
- Trạng thái.

Cần xác định các quy tắc:

- Phim phải tồn tại.
- Phòng phải tồn tại và khả dụng.
- Thời gian phải hợp lệ.
- Không được có hai suất chiếu chồng thời gian
  trong cùng một phòng.
- Cách xử lý khi Showtime bị hủy.
-->

## 11. Phân tích nghiệp vụ thanh toán

<!--
Xác định quy trình thanh toán từ Booking đến kết quả giao dịch.

Cần xác định:

- Khi nào Payment được tạo.
- Số tiền cần thanh toán.
- Phương thức thanh toán.
- Trạng thái Payment.
- Cách nhận kết quả từ Payment Gateway.
- Cách xác thực kết quả giao dịch.
- Timeout.
- Thanh toán thất bại.
- Thanh toán bị hủy.
- Refund nếu hệ thống hỗ trợ.

Cần xác định rõ mối quan hệ:

Booking
→ Payment
→ Payment Result
→ Ticket
-->

## 12. Xác định chức năng hệ thống

<!--
Chuyển các nghiệp vụ đã phân tích thành danh sách chức năng cụ thể.

Phân loại chức năng theo Actor hoặc module.

Ví dụ:

Customer:

- Register
- Login
- View Movies
- View Showtime
- Select Seat
- Book Ticket
- Payment
- View Ticket
- View Booking History
- Update Profile

Staff / Manager / Admin:

- Manage Movie
- Manage Genre
- Manage Cinema Room
- Manage Seat
- Manage Showtime
- Manage Booking
- Manage Ticket
- Manage Payment
- Manage User
- View Statistics

Danh sách ở đây sẽ là cơ sở để xây dựng API,
Frontend và Test Case.
-->

## 13. Xác định yêu cầu phi chức năng

<!--
Xác định các yêu cầu về chất lượng và đặc tính vận hành của hệ thống,
không chỉ riêng chức năng.

Cần xem xét:

- Security.
- Performance.
- Availability.
- Reliability.
- Usability.
- Maintainability.
- Scalability.
- Responsive.
- Data Integrity.

Mỗi yêu cầu cần có mô tả đủ cụ thể để sau này có thể kiểm tra
hoặc đánh giá được.
-->