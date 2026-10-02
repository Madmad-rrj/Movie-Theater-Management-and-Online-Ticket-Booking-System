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