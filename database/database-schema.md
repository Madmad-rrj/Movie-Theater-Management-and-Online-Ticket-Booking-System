# Database Schema

## 1. Tổng quan

<!--
Mô tả ngắn Database Schema này là kết quả cuối cùng
của Database Design và được sử dụng làm cơ sở
cho việc triển khai Database bằng EF Core / PostgreSQL.
-->

## 2. Relational Schema

<!--
Liệt kê lược đồ quan hệ cuối cùng của toàn bộ Database.

Ví dụ:

MOVIE(
    MovieId,
    Title,
    Description,
    Duration,
    ReleaseDate,
    Status,
    ...
)

GENRE(
    GenreId,
    Name,
    ...
)

MOVIE_GENRE(
    MovieId,
    GenreId
)

CINEMA_ROOM(
    CinemaRoomId,
    Name,
    Capacity,
    Status,
    ...
)

SEAT(
    SeatId,
    CinemaRoomId,
    SeatTypeId,
    Row,
    Number,
    ...
)

...
-->

## 3. Table Schema

### 3.1 Movie

| Column | Data Type | PK | FK | Nullable | Unique | Default | Description |
| ------ | --------- | -- | -- | -------- | ------ | ------- | ----------- |
|        |           |    |    |          |        |         |             |
Ví dụ mục 3.1 Movie trong tài liệu `database-schema.md` của hệ thống đặt vé xem phim có thể viết như sau:

### 3. Table Schema

#### 3.1 Movie

| Column        | Data Type       | PK  | FK | Nullable | Unique | Default             | Description                    |
| ------------- | --------------- | --- | -- | -------- | ------ | ------------------- | ------------------------------ |
| `MovieID`     | `INT`           | Yes | No | No       | Yes    | Auto-increment      | Unique identifier of the movie |
| `Title`       | `NVARCHAR(255)` | No  | No | No       | No     | —                   | Movie title                    |
| `Description` | `NVARCHAR(MAX)` | No  | No | Yes      | No     | `NULL`              | Movie synopsis                 |
| `Duration`    | `INT`           | No  | No | No       | No     | —                   | Movie duration in minutes      |
| `ReleaseDate` | `DATE`          | No  | No | Yes      | No     | `NULL`              | Movie release date             |
| `PosterURL`   | `VARCHAR(500)`  | No  | No | Yes      | No     | `NULL`              | URL of the movie poster        |
| `TrailerURL`  | `VARCHAR(500)`  | No  | No | Yes      | No     | `NULL`              | URL of the movie trailer       |
| `Language`    | `NVARCHAR(50)`  | No  | No | Yes      | No     | `NULL`              | Movie language                 |
| `AgeRating`   | `VARCHAR(10)`   | No  | No | Yes      | No     | `NULL`              | Movie age classification       |
| `Status`      | `VARCHAR(20)`   | No  | No | No       | No     | `'ComingSoon'`      | Movie status                   |
| `CreatedAt`   | `DATETIME2`     | No  | No | No       | No     | `CURRENT_TIMESTAMP` | Record creation timestamp      |
| `UpdatedAt`   | `DATETIME2`     | No  | No | Yes      | No     | `NULL`              | Last modification timestamp    |

Lưu ý:

- Đây là ví dụ minh họa, chưa chắc trùng với database thực tế của nhóm. Cần đối chiếu với `database-schema.sql` và entity trong backend.
- `MovieID` vừa là PK vừa có tính duy nhất, nên cột `Unique` ghi `Yes` là hợp lý nếu bảng dùng khóa chính này.
- `Duration` nên có ràng buộc `CHECK (Duration > 0)`.
- `Status` cần thống nhất các giá trị được phép, chẳng hạn `ComingSoon`, `NowShowing`, `Ended`.
- `CreatedAt` và `UpdatedAt` có thể dùng `DATETIME2` nếu nhóm sử dụng SQL Server. Nếu sử dụng PostgreSQL thì nên điều chỉnh kiểu dữ liệu cho phù hợp.
- `Genre` thường có quan hệ nhiều-nhiều với `Movie` thông qua bảng trung gian `MovieGenre`, vì vậy không cần lưu `GenreID` trực tiếp trong bảng `Movie` nếu thiết kế theo mô hình đó.

Quan trọng: Tài liệu `database-schema.md` nên mô tả schema thực tế mà nhóm thống nhất, không phải chỉ liệt kê những cột có thể có. Nếu nhóm chưa quyết định các cột như `AgeRating`, `Language` hay `Status`, hãy xác nhận trước khi đưa chúng vào thiết kế chính thức.
### 3.2 Genre

| Column | Data Type | PK | FK | Nullable | Unique | Default | Description |
| ------ | --------- | -- | -- | -------- | ------ | ------- | ----------- |
|        |           |    |    |          |        |         |             |

### 3.3 MovieGenre

| Column | Data Type | PK | FK | Nullable | Unique | Default | Description |
| ------ | --------- | -- | -- | -------- | ------ | ------- | ----------- |
|        |           |    |    |          |        |         |             |

### 3.4 CinemaRoom

| Column | Data Type | PK | FK | Nullable | Unique | Default | Description |
| ------ | --------- | -- | -- | -------- | ------ | ------- | ----------- |
|        |           |    |    |          |        |         |             |

### 3.5 Seat

| Column | Data Type | PK | FK | Nullable | Unique | Default | Description |
| ------ | --------- | -- | -- | -------- | ------ | ------- | ----------- |
|        |           |    |    |          |        |         |             |

### 3.6 SeatType

...

### 3.7 TicketType

...

### 3.8 Showtime

...

### 3.9 Booking

...

### 3.10 BookingDetail

...

### 3.11 Ticket

...

### 3.12 Payment

...

### 3.13 User

...

### 3.14 Role

...

### 3.15 UserRole

...

## 4. Primary Keys

<!--
Tổng hợp Primary Key của tất cả các bảng.
-->

## 5. Foreign Keys

<!--
Tổng hợp toàn bộ Foreign Key và bảng được tham chiếu.
-->

## 6. Constraints

<!--
Tổng hợp các Constraint quan trọng:

- NOT NULL
- UNIQUE
- CHECK
- DEFAULT
- FOREIGN KEY
-->

## 7. Indexes

<!--
Liệt kê các Index chính của Database.
-->

## 8. Relationships

<!--
Tổng hợp các quan hệ cuối cùng giữa các bảng.

Ví dụ:

Movie 1 --- N Showtime
CinemaRoom 1 --- N Seat
Showtime 1 --- N Booking
Booking 1 --- N BookingDetail
...
-->

## 9. ERD

<!--
Đặt ERD chính thức của Database tại đây.

Có thể sử dụng:

- draw.io
- Mermaid
- Công cụ khác
-->

## 10. Final Database Schema

<!--
Phiên bản Database Schema cuối cùng được dùng làm
cơ sở trực tiếp cho:

- EF Core Entity Models
- AppDbContext
- EF Core Fluent API
- Migration
- PostgreSQL Database
-->