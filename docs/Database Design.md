# Database Design

## 1. Tổng quan Database

// Mô tả mục đích của Database và phạm vi dữ liệu mà hệ thống quản lý.

## 2. Danh sách Entity

// Liệt kê toàn bộ Entity chính của hệ thống và mô tả ngắn chức năng của từng Entity.

// Ví dụ:
// - Movie
// - Genre
// - MovieGenre
// - CinemaRoom
// - Seat
// - SeatType
// - TicketType
// - Showtime
// - Booking
// - BookingDetail
// - Ticket
// - Payment
// - User
// - Role
// - UserRole

## 3. Thiết kế Entity Movie

// Xác định các thuộc tính của Movie, ý nghĩa và quy tắc dữ liệu.

## 4. Thiết kế Entity Genre

// Xác định các thuộc tính của Genre và quy tắc dữ liệu.

## 5. Thiết kế Entity MovieGenre

// Thiết kế Entity trung gian cho quan hệ Movie - Genre.

// Xác định các khóa và ràng buộc đảm bảo một Movie không bị liên kết trùng Genre.

## 6. Thiết kế Entity CinemaRoom

// Xác định thông tin phòng chiếu như tên phòng, sức chứa, trạng thái,...

## 7. Thiết kế Entity Seat

// Xác định thông tin ghế, vị trí ghế, phòng chiếu và loại ghế.

// Xác định quy tắc đảm bảo vị trí ghế không bị trùng trong cùng một phòng.

## 8. Thiết kế Entity SeatType

// Xác định các loại ghế và đặc điểm của từng loại.

// Ví dụ: Standard, VIP, Couple,...

## 9. Thiết kế Entity TicketType

// Xác định các loại vé và quy tắc giá vé.

// Ví dụ: Adult, Child, Student,...

## 10. Thiết kế Entity Showtime

// Xác định thông tin suất chiếu: phim, phòng, thời gian bắt đầu,
// thời gian kết thúc, trạng thái,...

## 11. Thiết kế Entity Booking

// Xác định thông tin đơn đặt vé, Customer, thời gian đặt,
// tổng tiền và trạng thái của Booking.

## 12. Thiết kế Entity BookingDetail

// Xác định các ghế, loại vé, đơn giá và số lượng thuộc một Booking.

## 13. Thiết kế Entity Ticket

// Xác định thông tin vé được phát hành sau khi Booking thanh toán thành công.

// Xác định mã vé, Booking, ghế, QR Code và trạng thái vé.

## 14. Thiết kế Entity Payment

// Xác định thông tin giao dịch thanh toán.

// Xác định mã giao dịch, số tiền, phương thức và trạng thái thanh toán.

## 15. Thiết kế Entity User

// Xác định thông tin tài khoản người dùng.

// Bao gồm các thông tin cần thiết cho Authentication và Profile.

## 16. Thiết kế Entity Role

// Xác định các Role trong hệ thống.

// Ví dụ: Customer, Staff, Manager, Admin.

## 17. Thiết kế Entity UserRole

// Thiết kế Entity liên kết User và Role nếu sử dụng quan hệ N-N.

// Xác định khóa và ràng buộc của bảng liên kết.

## 18. Thiết kế Relationships

// Xác định toàn bộ mối quan hệ giữa các Entity.

// Mô tả:
// - 1-1
// - 1-N
// - N-N

// Ghi rõ Entity nào là Principal và Dependent nếu cần.

## 19. Thiết kế Primary Key

// Xác định Primary Key của từng Entity.

// Mô tả nguyên tắc lựa chọn khóa và tính duy nhất của khóa.

## 20. Thiết kế Foreign Key

// Xác định Foreign Key của từng Entity.

// Ghi rõ Foreign Key tham chiếu đến Primary Key nào.

## 21. Thiết kế Data Type

// Xác định kiểu dữ liệu cho từng thuộc tính.

// Ví dụ:
// - INT
// - VARCHAR
// - TEXT
// - DECIMAL
// - TIMESTAMP
// - BOOLEAN
// - UUID nếu sử dụng

## 22. Thiết kế Constraints

// Xác định các ràng buộc dữ liệu.

// Ví dụ:
// - NOT NULL
// - UNIQUE
// - CHECK
// - FOREIGN KEY
// - DEFAULT

## 23. Thiết kế Status

// Xác định các trạng thái của những Entity có State.

// Ví dụ:
// - Movie
// - Showtime
// - Booking
// - Ticket
// - Payment

// Mô tả ý nghĩa của từng trạng thái và các điều kiện chuyển trạng thái.

## 24. Quy tắc toàn vẹn dữ liệu

// Xác định các quy tắc nhằm đảm bảo dữ liệu luôn nhất quán và hợp lệ.

// Bao gồm các quy tắc liên quan đến PK, FK, Constraints,
// quan hệ giữa các Entity và nghiệp vụ.

## 25. Quy tắc trùng lịch Showtime

// Xác định điều kiện để một CinemaRoom không có hai Showtime
// bị chồng lấn thời gian.

// Quy tắc phải xét đến thời gian bắt đầu, thời gian kết thúc,
// phòng chiếu và trạng thái của Showtime.

## 26. Quy tắc trùng ghế

// Xác định quy tắc đảm bảo một Seat không thể được bán
// cho hai Booking trong cùng một Showtime.

// Xác định cách biểu diễn và kiểm soát trạng thái ghế.

## 27. Quy tắc xóa dữ liệu

// Xác định cách xử lý khi xóa từng Entity.

// Có thể sử dụng:
// - Cascade Delete
// - Restrict
// - Soft Delete

// Giải thích lý do lựa chọn đối với từng quan hệ hoặc Entity.

## 28. Chuẩn hóa Database

// Kiểm tra và mô tả việc chuẩn hóa Database.

// Xem xét:
// - Loại bỏ dữ liệu dư thừa
// - Phụ thuộc hàm
// - Chuẩn 1NF
// - Chuẩn 2NF
// - Chuẩn 3NF nếu áp dụng

## 29. Thiết kế Index

// Xác định các trường cần Index dựa trên các thao tác
// tìm kiếm, lọc, Join và truy vấn thường xuyên.

// Giải thích mục đích của từng Index.

## 30. Lược đồ quan hệ

// Viết lược đồ quan hệ cuối cùng của Database.

// Ví dụ:
//
// MOVIE(MovieId, Title, Description, Duration, ReleaseDate, Status, ...)
// GENRE(GenreId, Name)
// MOVIE_GENRE(MovieId, GenreId)
// CINEMA_ROOM(CinemaRoomId, Name, Capacity, Status, ...)
// SEAT(SeatId, CinemaRoomId, SeatTypeId, Row, Number, ...)
// ...

## 31. ERD

// Vẽ Entity Relationship Diagram của toàn bộ Database.

// ERD phải thể hiện:
// - Entity
// - Attribute chính
// - Primary Key
// - Foreign Key
// - Relationships
// - Cardinality

// Có thể sử dụng:
// - draw.io
// - Mermaid
// - Công cụ khác

## 32. Database Schema cuối cùng

// Tổng hợp cấu trúc Database sau khi hoàn thành thiết kế.

// Bao gồm:
// - Các bảng
// - Các cột
// - Data Type
// - PK
// - FK
// - Constraints
// - Index
// - Relationships

// Đây là phiên bản Database Design chính thức,
// được sử dụng làm cơ sở cho EF Core Entity Models.