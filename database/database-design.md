# Database Design

## 1. Tổng quan Database

<!--
Mô tả mục đích của Database và phạm vi dữ liệu mà hệ thống quản lý.

Cần xác định:
- Database phục vụ những nghiệp vụ nào.
- Những nhóm dữ liệu chính được quản lý.
- Database có vai trò gì trong toàn bộ hệ thống.
- Database nào được sử dụng.
-->

## 2. Nguyên tắc thiết kế Database

<!--
Xác định các nguyên tắc chung được sử dụng khi thiết kế Database.

Ví dụ:
- Ưu tiên tính toàn vẹn dữ liệu.
- Hạn chế dữ liệu dư thừa.
- Sử dụng Primary Key cho mỗi Entity.
- Sử dụng Foreign Key để đảm bảo quan hệ.
- Chuẩn hóa dữ liệu ở mức phù hợp.
- Thiết kế phù hợp với PostgreSQL và EF Core.
-->

## 3. Danh sách Entity

<!--
Liệt kê toàn bộ Entity chính của hệ thống và mô tả ngắn chức năng
của từng Entity.

Ví dụ:

- Movie: Lưu thông tin phim.
- Genre: Lưu thể loại phim.
- MovieGenre: Liên kết Movie và Genre.
- CinemaRoom: Lưu thông tin phòng chiếu.
- Seat: Lưu thông tin ghế trong phòng.
- SeatType: Xác định loại ghế.
- TicketType: Xác định loại vé.
- Showtime: Lưu thông tin suất chiếu.
- Booking: Lưu thông tin đơn đặt vé.
- BookingDetail: Chi tiết các ghế/vé trong Booking.
- Ticket: Lưu vé được phát hành.
- Payment: Lưu thông tin thanh toán.
- User: Lưu tài khoản người dùng.
- Role: Xác định quyền/vai trò.
- UserRole: Liên kết User và Role.
-->

## 4. Thiết kế Entity Movie

Entity `Movie` lưu trữ thông tin về các bộ phim được quản lý trong hệ thống đặt vé xem phim trực tuyến.

### 4.1. Mục đích

- Lưu trữ thông tin cơ bản của phim.
- Cung cấp dữ liệu cho chức năng xem danh sách phim, xem chi tiết phim và tra cứu phim.
- Hỗ trợ quản lý trạng thái phát hành của phim.
- Làm cơ sở liên kết phim với thể loại và các suất chiếu.

### 4.2. Các thuộc tính

| Thuộc tính | Ý nghĩa | Kiểu dữ liệu dự kiến | Bắt buộc | Giá trị mặc định | Quy tắc dữ liệu |
|---|---|---|---|---|---|
| `MovieID` | Mã định danh duy nhất của phim | Số nguyên | Có | Tự sinh | Không trùng lặp; không thay đổi sau khi tạo |
| `Title` | Tên phim | Chuỗi | Có | Không có | Không được để trống; giới hạn độ dài hợp lý |
| `Description` | Nội dung giới thiệu phim | Văn bản dài | Không | Không có | Có thể để trống |
| `Duration` | Thời lượng phim, tính bằng phút | Số nguyên | Có | Không có | Phải lớn hơn 0 |
| `ReleaseDate` | Ngày phát hành phim | Ngày | Không | Không có | Nếu có, phải là ngày hợp lệ |
| `PosterURL` | Đường dẫn ảnh áp phích phim | Chuỗi | Không | Không có | Nếu có, phải là URL hợp lệ theo quy ước của hệ thống |
| `TrailerURL` | Đường dẫn trailer phim | Chuỗi | Không | Không có | Nếu có, phải là URL hợp lệ theo quy ước của hệ thống |
| `Language` | Ngôn ngữ chính của phim | Chuỗi | Không | Không có | Nếu có, phải tuân theo định dạng thống nhất |
| `AgeRating` | Phân loại độ tuổi được phép xem | Chuỗi hoặc kiểu liệt kê | Không | Không có | Nếu có, phải thuộc tập phân loại mà hệ thống hỗ trợ |
| `Status` | Trạng thái quản lý của phim | Kiểu liệt kê | Có | `ComingSoon` | Chỉ nhận các trạng thái được hệ thống quy định |
| `CreatedAt` | Thời điểm tạo bản ghi | Ngày giờ | Có | Thời điểm tạo | Được hệ thống ghi nhận khi tạo phim |
| `UpdatedAt` | Thời điểm cập nhật gần nhất | Ngày giờ | Không | Không có | Được cập nhật khi thông tin phim thay đổi |

### 4.3. Các quy tắc nghiệp vụ

1. Mỗi phim phải có một mã định danh duy nhất.
2. Tên phim không được để trống.
3. Thời lượng phim phải là số nguyên dương.
4. Trạng thái phim chỉ được nhận các giá trị đã được hệ thống thống nhất, ví dụ: `ComingSoon`, `NowShowing`, `Ended`.
5. Một phim có thể thuộc nhiều thể loại; quan hệ này được quản lý thông qua entity trung gian `MovieGenre`.
6. Không nên xóa phim một cách tùy tiện nếu phim đã có suất chiếu, đặt vé hoặc dữ liệu lịch sử liên quan. Việc ngừng hiển thị hoặc ngừng phát hành cần tuân theo quy tắc quản lý dữ liệu của hệ thống.

### 4.4. Quan hệ với các entity khác

- `Genre`: quan hệ nhiều-nhiều thông qua `MovieGenre`.
- `Showtime`: một phim có thể có nhiều suất chiếu.
- `MovieGenre`: lưu thông tin liên kết giữa phim và thể loại.

### 4.5. Các quyết định cần thống nhất

- Những trạng thái nào được áp dụng cho phim?
- Có bắt buộc nhập ngày phát hành, ngôn ngữ và phân loại độ tuổi không?
- Phim đã có suất chiếu hoặc vé đã bán có được xóa không?
- Hệ thống có cần lưu lịch sử thay đổi thông tin phim không?

## 5. Thiết kế Entity Genre

<!--
Xác định các thuộc tính của Genre và các quy tắc dữ liệu.

Đặc biệt xác định:
- Tên Genre có được trùng hay không.
- Genre có thể bị xóa khi đang được Movie sử dụng hay không.
-->

## 6. Thiết kế Entity MovieGenre

<!--
Thiết kế Entity trung gian cho quan hệ Movie - Genre.

Xác định:
- Movie có thể thuộc nhiều Genre hay không.
- Genre có thể được sử dụng cho nhiều Movie hay không.
- Khóa của Entity trung gian.
- Quy tắc không cho phép cùng một Movie liên kết
  với cùng một Genre nhiều lần.
-->

## 7. Thiết kế Entity CinemaRoom

<!--
Xác định thông tin phòng chiếu.

Bao gồm:
- Tên phòng.
- Sức chứa.
- Trạng thái.
- Các thông tin khác nếu cần.

Xác định các quy tắc:
- Tên phòng có được trùng hay không.
- Khi nào phòng được xem là khả dụng.
- Có được xóa phòng đã từng có Showtime hay không.
-->

## 8. Thiết kế Entity Seat

<!--
Xác định thông tin ghế.

Bao gồm:
- Phòng chiếu.
- Hàng.
- Số ghế.
- Loại ghế.
- Trạng thái nếu có.

Xác định quy tắc:
- Một ghế thuộc đúng một phòng.
- Vị trí ghế không được trùng trong cùng một phòng.
-->

## 9. Thiết kế Entity SeatType

<!--
Xác định các loại ghế.

Ví dụ:
- Standard.
- VIP.
- Couple.

Xác định:
- Tên loại ghế.
- Đặc điểm.
- Các quy tắc liên quan đến giá hoặc trạng thái nếu có.
-->

## 10. Thiết kế Entity TicketType

<!--
Xác định các loại vé.

Ví dụ:
- Adult.
- Child.
- Student.

Xác định:
- Tên loại vé.
- Điều kiện áp dụng.
- Giá hoặc cách xác định giá.
- Trạng thái nếu cần.
-->

## 11. Thiết kế Entity Showtime

<!--
Xác định thông tin của một suất chiếu.

Bao gồm:
- Movie.
- CinemaRoom.
- StartTime.
- EndTime.
- Status.

Xác định các quy tắc:
- Phim phải tồn tại.
- Phòng phải tồn tại.
- StartTime < EndTime.
- Không được trùng lịch trong cùng một phòng.
- Quy tắc khi Showtime bị hủy.
-->

## 12. Thiết kế Entity Booking

<!--
Xác định thông tin đơn đặt vé.

Bao gồm:
- Customer/User.
- Showtime.
- Thời gian đặt.
- Tổng tiền.
- Trạng thái.

Xác định vòng đời của Booking và các điều kiện
để Booking được tạo, thanh toán, hủy hoặc hết hạn.
-->

## 13. Thiết kế Entity BookingDetail

<!--
Xác định thông tin chi tiết của một Booking.

Xác định:
- Booking.
- Seat.
- TicketType.
- Đơn giá.
- Số lượng nếu cần.

Làm rõ cách biểu diễn việc Customer mua một hoặc nhiều vé
trong cùng một Booking.
-->

## 14. Thiết kế Entity Ticket

<!--
Xác định thông tin vé được phát hành sau khi Booking
thanh toán thành công.

Bao gồm:
- Mã vé.
- Booking.
- Showtime.
- Seat.
- TicketType nếu cần.
- QR Code.
- Trạng thái.

Xác định quan hệ giữa Ticket và BookingDetail.
-->

## 15. Thiết kế Entity Payment

<!--
Xác định thông tin giao dịch thanh toán.

Bao gồm:
- Booking.
- Mã giao dịch.
- Số tiền.
- Phương thức thanh toán.
- Trạng thái.
- Thời gian giao dịch.

Xác định khả năng có nhiều Payment cho một Booking
nếu có retry hoặc giao dịch thất bại.
-->

## 16. Thiết kế Entity User

<!--
Xác định thông tin tài khoản người dùng.

Bao gồm các thông tin phục vụ:
- Authentication.
- Profile.
- Account status.

Không lưu các thông tin không cần thiết cho phạm vi hệ thống.
-->

## 17. Thiết kế Entity Role

<!--
Xác định các Role của hệ thống.

Ví dụ:
- Customer.
- Staff.
- Manager.
- Admin.

Mô tả mục đích của từng Role.
-->

## 18. Thiết kế Entity UserRole

<!--
Thiết kế Entity liên kết User và Role.

Xác định:
- Quan hệ User - Role.
- Primary Key.
- Foreign Key.
- Quy tắc không cho phép một User được gán cùng
  một Role nhiều lần.
-->

## 19. Thiết kế Relationships

<!--
Xác định toàn bộ mối quan hệ giữa các Entity.

Phân loại:
- 1-1.
- 1-N.
- N-N.

Với mỗi quan hệ cần xác định:
- Principal.
- Dependent.
- Cardinality.
- Foreign Key.
- Optional/Required relationship.

Ví dụ:

Movie 1 --- N Showtime
CinemaRoom 1 --- N Seat
Showtime 1 --- N Booking
Booking 1 --- N BookingDetail
Booking 1 --- N Payment
Movie N --- N Genre
User N --- N Role
-->

## 20. Thiết kế Primary Key

<!--
Xác định chiến lược Primary Key.

Cần quyết định:
- Kiểu dữ liệu.
- Cách sinh giá trị.
- Identity/Sequence hoặc UUID nếu sử dụng.
- Composite Key cho các Entity trung gian nếu cần.

Mục tiêu là đảm bảo mỗi Entity có định danh duy nhất.
-->

## 21. Thiết kế Foreign Key

<!--
Xác định các Foreign Key dựa trên Relationships.

Với mỗi Foreign Key cần xác định:
- Entity chứa Foreign Key.
- Cột Foreign Key.
- Entity được tham chiếu.
- Primary Key được tham chiếu.
- Required/Optional.
-->

## 22. Thiết kế Data Type

<!--
Xác định kiểu dữ liệu phù hợp cho các thuộc tính.

Ví dụ:
- INTEGER.
- BIGINT.
- VARCHAR.
- TEXT.
- DECIMAL.
- TIMESTAMP.
- BOOLEAN.
- UUID nếu sử dụng.

Việc lựa chọn Data Type cần phù hợp với PostgreSQL
và EF Core.
-->

## 23. Thiết kế Constraints

<!--
Xác định các ràng buộc Database.

Bao gồm:
- NOT NULL.
- UNIQUE.
- CHECK.
- PRIMARY KEY.
- FOREIGN KEY.
- DEFAULT.

Mỗi Constraint cần xác định mục đích và Entity áp dụng.
-->

## 24. Thiết kế Status

<!--
Xác định các Entity có trạng thái.

Ví dụ:
- Movie.
- CinemaRoom.
- Showtime.
- Booking.
- Ticket.
- Payment.
- User.

Với mỗi Status cần xác định:
- Các giá trị trạng thái.
- Ý nghĩa.
- Điều kiện chuyển trạng thái.

Phân biệt rõ trạng thái nghiệp vụ với trạng thái
chỉ mang tính kỹ thuật.
-->

## 25. Quy tắc toàn vẹn dữ liệu

<!--
Xác định các quy tắc đảm bảo dữ liệu luôn nhất quán và hợp lệ.

Bao gồm:
- Entity Integrity.
- Referential Integrity.
- Domain Integrity.
- Các quy tắc liên quan đến nghiệp vụ.

Phân biệt quy tắc có thể đảm bảo bằng Database Constraint
và quy tắc cần xử lý ở Backend Business Logic.
-->

## 26. Quy tắc trùng lịch Showtime

<!--
Xác định điều kiện để một CinemaRoom không có hai Showtime
bị chồng lấn thời gian.

Xác định:
- Cách xác định khoảng thời gian.
- Các trạng thái Showtime được xét.
- Các trạng thái được bỏ qua.
- Cách kiểm tra khi tạo hoặc cập nhật Showtime.

Nếu Database không trực tiếp đảm bảo được quy tắc này,
ghi rõ Backend phải kiểm tra như thế nào.
-->

## 27. Quy tắc trùng ghế

<!--
Xác định quy tắc đảm bảo một Seat không thể được bán
cho hai Booking trong cùng một Showtime.

Cần làm rõ:
- Quan hệ giữa Showtime, Seat và Booking.
- Khi nào ghế được xem là đã giữ/đã bán.
- Thời gian giữ ghế nếu có.
- Cách ngăn chặn hai Customer đặt cùng một ghế.
- Phần nào được đảm bảo bằng Database.
- Phần nào được xử lý bằng Transaction/Concurrency ở Backend.
-->

## 28. Quy tắc xóa dữ liệu

<!--
Xác định cách xử lý khi Entity được xóa.

Có thể sử dụng:
- Cascade Delete.
- Restrict.
- Set Null.
- Soft Delete.

Với từng Entity/Relationship cần giải thích:
- Có được phép xóa hay không.
- Dữ liệu liên quan được xử lý như thế nào.
- Lý do lựa chọn.
-->

## 29. Chuẩn hóa Database

<!--
Kiểm tra mức độ chuẩn hóa của Database.

Xem xét:
- Loại bỏ dữ liệu dư thừa.
- Phụ thuộc hàm.
- 1NF.
- 2NF.
- 3NF nếu áp dụng.

Nếu cố tình không chuẩn hóa một phần để phục vụ
Performance, cần ghi rõ lý do.
-->

## 30. Thiết kế Index

<!--
Xác định các Index cần thiết dựa trên các thao tác truy vấn.

Xem xét:
- Search.
- Filter.
- Sort.
- JOIN.
- Foreign Key.
- Các truy vấn thường xuyên.

Với mỗi Index cần ghi:
- Entity/Table.
- Column.
- Loại Index nếu cần.
- Mục đích.
-->

## 31. Quy tắc Transaction và Concurrency

<!--
Xác định các nghiệp vụ cần Transaction và xử lý Concurrency.

Đặc biệt xem xét:
- Đặt ghế.
- Giữ ghế.
- Thanh toán.
- Phát hành Ticket.

Xác định:
- Phạm vi Transaction.
- Các thao tác phải thành công đồng thời.
- Cách xử lý khi xảy ra lỗi.
- Cách tránh hai Customer đặt cùng một Seat.
-->

## 32. Quyết định thiết kế Database

<!--
Tổng hợp các quyết định quan trọng đã được đưa ra trong Database Design.

Mỗi quyết định nên ghi:
- Vấn đề.
- Các phương án.
- Phương án được chọn.
- Lý do lựa chọn.

Ví dụ:
- UUID hay Integer cho Primary Key.
- Soft Delete hay Hard Delete.
- Một Booking có một hay nhiều Payment.
- Ticket liên kết trực tiếp với Booking hay BookingDetail.
- Có lưu trạng thái ghế hay không.
-->