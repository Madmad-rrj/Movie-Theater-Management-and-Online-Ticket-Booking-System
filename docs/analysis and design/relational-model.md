# Relational Model

## 1. Tổng quan

Tài liệu này chuyển thiết kế nghiệp vụ của hệ thống quản lý rạp chiếu phim và đặt vé trực tuyến thành mô hình dữ liệu quan hệ. Mô hình bao phủ các nhóm dữ liệu:

- phim và thể loại;
- chi nhánh, phòng chiếu, loại ghế và ghế;
- suất chiếu;
- tài khoản và vai trò;
- booking, chi tiết booking và vé;
- payment, sự kiện thanh toán và cấu trúc refund mở rộng.

Mô hình quan hệ là cầu nối giữa `database-design.md` và schema PostgreSQL/EF Core về sau. Tài liệu này tập trung vào relation, khóa và quan hệ logic; kiểu dữ liệu SQL, tên constraint và câu lệnh DDL thuộc `database-schema.md`, migration và `database-schema.sql`.

### 1.1. Quy ước ký hiệu

- `PK`: Primary Key.
- `FK`: Foreign Key.
- `AK`: Alternate/Candidate Key ngoài PK.
- `UQ`: ràng buộc duy nhất.
- `NULL`: thuộc tính không bắt buộc.
- Thuộc tính không ghi `NULL` được xem là bắt buộc trong mô hình logic.
- `UQ-N`: duy nhất sau chuẩn hóa `lower(btrim(value))`.
- `UQ-P`: duy nhất có điều kiện, chỉ áp dụng cho các hàng thỏa điều kiện.

Tên relation và thuộc tính dùng `snake_case` để thống nhất với PostgreSQL. Bảng người dùng dùng tên `app_user` nhằm tránh xung đột với từ khóa/ngữ nghĩa hệ thống `user`.

## 2. Danh sách relation

| STT | Relation | Mục đích |
|---:|---|---|
| 1 | `movie` | Lưu thông tin và trạng thái phim. |
| 2 | `genre` | Danh mục thể loại phim. |
| 3 | `movie_genre` | Relation nối quan hệ M:N giữa phim và thể loại. |
| 4 | `cinema` | Lưu chi nhánh/rạp vật lý. |
| 5 | `cinema_room` | Lưu phòng chiếu thuộc một chi nhánh. |
| 6 | `seat_type` | Danh mục loại ghế, sức chứa và phụ thu. |
| 7 | `seat` | Lưu vị trí ghế vật lý trong phòng. |
| 8 | `ticket_type` | Danh mục loại vé và mức giảm giá. |
| 9 | `showtime` | Lưu lịch chiếu, khoảng chiếm dụng phòng và giá cơ sở. |
| 10 | `app_user` | Lưu tài khoản và hồ sơ tối thiểu. |
| 11 | `role` | Danh mục vai trò. |
| 12 | `user_role` | Relation nối M:N giữa tài khoản và vai trò. |
| 13 | `booking` | Lưu đơn đặt ghế, hạn giữ và tổng tiền. |
| 14 | `booking_detail` | Lưu từng ghế, loại vé, giá snapshot và trạng thái phân bổ. |
| 15 | `ticket` | Lưu vé được phát hành sau khi booking được xác nhận. |
| 16 | `payment` | Lưu từng lần thử thanh toán của booking. |
| 17 | `payment_event` | Lưu callback/kết quả đối soát theo cơ chế idempotent. |
| 18 | `refund` | Cấu trúc mở rộng cho một yêu cầu hoàn toàn bộ/payment; ngoài phạm vi demo. |

## 3. Lược đồ quan hệ

### 3.1. `movie`

```text
movie(
    id [PK],
    title,
    description [NULL],
    duration_minutes,
    release_date [NULL],
    poster_url [NULL],
    trailer_url [NULL],
    language [NULL],
    age_rating [NULL],
    status
)
```

Không dùng `title` làm candidate key vì nhiều phim có thể trùng tên. `duration_minutes > 0`; `status` thuộc tập `coming_soon`, `now_showing`, `ended`, `archived`.

### 3.2. `genre`

```text
genre(
    id [PK],
    code [AK],
    name [AK: UQ-N],
    description [NULL],
    is_active
)
```

`code` là mã nghiệp vụ ổn định. Tên thể loại không được trùng sau khi bỏ khoảng trắng hai đầu và chuyển về chữ thường.

### 3.3. `movie_genre`

```text
movie_genre(
    movie_id [PK, FK -> movie.id],
    genre_id [PK, FK -> genre.id]
)
```

PK ghép `(movie_id, genre_id)` vừa định danh relation vừa ngăn gán cùng một thể loại cho một phim nhiều lần.

### 3.4. `cinema`

```text
cinema(
    id [PK],
    code [AK],
    name,
    address,
    phone [NULL],
    status
)
```

`code` duy nhất toàn hệ thống sau khi chuẩn hóa. MVP seed một cinema, nhưng mô hình cho phép mở rộng nhiều chi nhánh. `status` thuộc `active`, `inactive`.

### 3.5. `cinema_room`

```text
cinema_room(
    id [PK],
    cinema_id [FK -> cinema.id],
    code,
    name,
    status
)

AK: (cinema_id, code)
AK: (cinema_id, lower(btrim(name)))
```

Mã và tên phòng chỉ cần duy nhất trong từng cinema. Không lưu `capacity`; sức chứa được suy ra từ các ghế và `seat_type.capacity_units`. `status` thuộc `active`, `maintenance`, `inactive`.

### 3.6. `seat_type`

```text
seat_type(
    id [PK],
    code [AK],
    name [AK: UQ-N],
    capacity_units,
    surcharge_amount,
    is_active
)
```

`capacity_units` thuộc `{1, 2}`; `surcharge_amount >= 0`. Ví dụ mã: `STANDARD`, `VIP`, `COUPLE`.

### 3.7. `seat`

```text
seat(
    id [PK],
    cinema_room_id [FK -> cinema_room.id],
    seat_type_id [FK -> seat_type.id],
    row_label,
    seat_number,
    status
)

AK: (cinema_room_id, row_label, seat_number)
AK: (id, cinema_room_id)
```

AK `(id, cinema_room_id)` là khóa đích cho FK ghép từ `booking_detail`, giúp database từ chối ghế không thuộc phòng của suất chiếu. `status` chỉ mô tả tình trạng vật lý (`active`, `maintenance`, `inactive`), không dùng để biểu diễn ghế đang được giữ hoặc đã bán.

### 3.8. `ticket_type`

```text
ticket_type(
    id [PK],
    code [AK],
    name [AK: UQ-N],
    discount_percent,
    eligibility_note [NULL],
    is_active
)
```

`discount_percent` nằm trong `[0, 100]`. Điều kiện áp dụng loại vé do backend xác minh; client không được tự gửi mức giảm.

### 3.9. `showtime`

```text
showtime(
    id [PK],
    movie_id [FK -> movie.id],
    cinema_room_id [FK -> cinema_room.id],
    movie_title_snapshot,
    room_name_snapshot,
    duration_minutes_snapshot,
    starts_at,
    ends_at,
    room_available_at,
    booking_closes_at [NULL],
    base_price,
    status,
    cancelled_at [NULL],
    cancellation_reason [NULL]
)

AK: (id, cinema_room_id)
```

Các mốc thời gian thỏa `starts_at < ends_at <= room_available_at`; nếu có `booking_closes_at` thì mốc này không được sau `starts_at`. Khoảng chiếm dụng phòng là `[starts_at, room_available_at)`. Hai suất không bị chồng khoảng này trong cùng phòng, ngoại trừ suất đã `cancelled`.

### 3.10. `app_user`

```text
app_user(
    id [PK],
    email [AK: UQ-N],
    password_hash,
    full_name,
    phone [NULL],
    status,
    email_verified_at [NULL],
    last_login_at [NULL]
)
```

Email đăng nhập duy nhất sau `lower(btrim(email))`. `password_hash` chỉ chứa hash do thư viện xác thực tạo, không chứa mật khẩu gốc. `status` thuộc `active`, `locked`, `deactivated`.

### 3.11. `role`

```text
role(
    id [PK],
    code [AK],
    name,
    description [NULL],
    is_active
)
```

Các mã seed dự kiến: `CUSTOMER`, `STAFF`, `MANAGER`, `ADMIN`. `code` là mã ổn định được backend dùng trong authorization policy.

### 3.12. `user_role`

```text
user_role(
    user_id [PK, FK -> app_user.id],
    role_id [PK, FK -> role.id],
    assigned_at,
    assigned_by_user_id [NULL, FK -> app_user.id]
)
```

PK ghép `(user_id, role_id)` ngăn cấp trùng một role cho cùng user. `assigned_by_user_id = NULL` được dùng cho đăng ký công khai hoặc seed hệ thống.

### 3.13. `booking`

```text
booking(
    id [PK],
    booking_code [AK],
    user_id [FK -> app_user.id],
    showtime_id,
    cinema_room_id,
    request_key,
    request_fingerprint,
    contact_email_snapshot,
    contact_phone_snapshot [NULL],
    total_amount,
    currency,
    expires_at,
    status,
    confirmed_at [NULL],
    cancelled_at [NULL],
    cancellation_reason [NULL],
    created_at,

    FK (showtime_id, cinema_room_id)
        -> showtime(id, cinema_room_id)
)

AK: booking_code
AK: (user_id, request_key)
AK: (id, showtime_id, cinema_room_id)
```

`request_key` hỗ trợ idempotency khi tạo booking; cùng key nhưng khác `request_fingerprint` phải bị từ chối. `expires_at > created_at`; thời gian giữ ghế là 5 phút tính từ `created_at`. `currency = 'VND'`; `total_amount > 0`. Trạng thái gồm `pending`, `confirmed`, `expired`, `cancelled`.

### 3.14. `booking_detail`

```text
booking_detail(
    id [PK],
    booking_id,
    showtime_id,
    cinema_room_id,
    seat_id,
    ticket_type_id [FK -> ticket_type.id],
    seat_label_snapshot,
    seat_type_name_snapshot,
    ticket_type_name_snapshot,
    capacity_units_snapshot,
    base_amount,
    surcharge_amount,
    discount_percent_snapshot,
    discount_amount,
    unit_price,
    allocation_status,
    released_at [NULL],

    FK (booking_id, showtime_id, cinema_room_id)
        -> booking(id, showtime_id, cinema_room_id),
    FK (seat_id, cinema_room_id)
        -> seat(id, cinema_room_id)
)

AK: (booking_id, seat_id)
UQ-P: (showtime_id, seat_id)
      WHERE allocation_status IN ('held', 'sold')
```

Hai FK ghép buộc detail thuộc đúng booking, suất, phòng và ghế. UQ-P bảo đảm một ghế chỉ có một phân bổ hiệu lực trong một suất. Trạng thái phân bổ gồm `held`, `sold`, `released`.

Giá của một detail được xác định bởi:

```text
base_amount = showtime.base_price * capacity_units_snapshot
discount_amount = round(base_amount * discount_percent_snapshot / 100, 0)
unit_price = base_amount + surcharge_amount - discount_amount
```

`booking.total_amount` bằng tổng `unit_price` của toàn bộ detail gốc. Các snapshot giữ nguyên lịch sử ngay cả khi danh mục hoặc giá hiện hành thay đổi.

### 3.15. `ticket`

```text
ticket(
    id [PK],
    booking_detail_id [AK, FK -> booking_detail.id],
    ticket_code [AK],
    status,
    issued_at,
    used_at [NULL],
    validated_by_user_id [NULL, FK -> app_user.id],
    voided_at [NULL],
    void_reason [NULL]
)
```

UQ `booking_detail_id` bảo đảm mỗi detail có tối đa một ticket. Ticket chỉ được tạo sau khi payment hợp lệ được áp dụng. Trạng thái gồm `valid`, `used`, `voided`; cập nhật `valid -> used` phải có điều kiện để một vé không được soát thành công hai lần.

### 3.16. `payment`

```text
payment(
    id [PK],
    booking_id [FK -> booking.id],
    provider,
    merchant_request_id [AK],
    provider_transaction_id [NULL],
    method [NULL],
    amount,
    currency,
    received_amount [NULL],
    received_currency [NULL],
    status,
    applied_at [NULL],
    paid_at [NULL],
    verified_at [NULL],
    reconciliation_status,
    failure_code [NULL],
    next_reconcile_at [NULL],
    created_at
)

AK: merchant_request_id
AK: (provider, provider_transaction_id), khi provider_transaction_id IS NOT NULL
UQ-P: booking_id WHERE status IN ('created', 'pending', 'unknown')
UQ-P: booking_id WHERE applied_at IS NOT NULL
```

Một booking có thể có nhiều payment attempt và thậm chí có nhiều attempt `succeeded`, nhưng chỉ một payment được áp dụng để xác nhận booking. Trạng thái payment gồm `created`, `pending`, `unknown`, `succeeded`, `failed`, `cancelled`.

### 3.17. `payment_event`

```text
payment_event(
    id [PK],
    payment_id [FK -> payment.id],
    source,
    event_key,
    event_type,
    payload_summary,
    received_at,
    processed_at [NULL],
    status,
    retry_count,
    next_retry_at [NULL],
    last_error [NULL]
)

AK: (payment_id, event_key)
```

Khóa `(payment_id, event_key)` chống xử lý lặp cùng một callback/sự kiện. `payload_summary` chỉ chứa dữ liệu đã lọc, không chứa secret hoặc thông tin thẻ. `source` thuộc `webhook`, `reconcile`; `status` thuộc `received`, `processed`, `failed`.

### 3.18. `refund`

```text
refund(
    id [PK],
    payment_id [AK, FK -> payment.id],
    merchant_refund_id [AK],
    provider_refund_id [NULL],
    amount,
    currency,
    reason,
    status,
    requested_by_user_id [NULL, FK -> app_user.id],
    completed_at [NULL],
    next_retry_at [NULL],
    last_error [NULL]
)
```

UQ `payment_id` biểu diễn chính sách tối đa một yêu cầu hoàn toàn bộ cho một payment. Relation này chỉ phục vụ khả năng mở rộng và truy vết; refund không thuộc phạm vi demo hiện tại.

## 4. Tổng hợp Primary Key

| Relation | Primary Key | Loại khóa |
|---|---|---|
| `movie` | `id` | Đơn |
| `genre` | `id` | Đơn |
| `movie_genre` | `(movie_id, genre_id)` | Ghép |
| `cinema` | `id` | Đơn |
| `cinema_room` | `id` | Đơn |
| `seat_type` | `id` | Đơn |
| `seat` | `id` | Đơn |
| `ticket_type` | `id` | Đơn |
| `showtime` | `id` | Đơn |
| `app_user` | `id` | Đơn |
| `role` | `id` | Đơn |
| `user_role` | `(user_id, role_id)` | Ghép |
| `booking` | `id` | Đơn |
| `booking_detail` | `id` | Đơn |
| `ticket` | `id` | Đơn |
| `payment` | `id` | Đơn |
| `payment_event` | `id` | Đơn |
| `refund` | `id` | Đơn |

Các PK đơn dùng định danh nội bộ `bigint identity` ở mức vật lý. Các mã công khai như `booking_code`, `ticket_code` và các idempotency key dùng UUID nhưng không thay thế authorization.

## 5. Tổng hợp Foreign Key và toàn vẹn tham chiếu

| Relation con | Foreign Key | Relation cha | Ý nghĩa |
|---|---|---|---|
| `movie_genre` | `movie_id` | `movie(id)` | Phim được phân loại. |
| `movie_genre` | `genre_id` | `genre(id)` | Thể loại được gán cho phim. |
| `cinema_room` | `cinema_id` | `cinema(id)` | Phòng thuộc chi nhánh. |
| `seat` | `cinema_room_id` | `cinema_room(id)` | Ghế thuộc phòng. |
| `seat` | `seat_type_id` | `seat_type(id)` | Ghế có loại ghế. |
| `showtime` | `movie_id` | `movie(id)` | Suất chiếu một phim. |
| `showtime` | `cinema_room_id` | `cinema_room(id)` | Suất diễn ra trong phòng. |
| `user_role` | `user_id` | `app_user(id)` | User được cấp role. |
| `user_role` | `role_id` | `role(id)` | Role được cấp cho user. |
| `user_role` | `assigned_by_user_id` | `app_user(id)` | User đã thực hiện cấp role; nullable. |
| `booking` | `user_id` | `app_user(id)` | Chủ booking. |
| `booking` | `(showtime_id, cinema_room_id)` | `showtime(id, cinema_room_id)` | Booking thuộc đúng phòng của suất. |
| `booking_detail` | `(booking_id, showtime_id, cinema_room_id)` | `booking(id, showtime_id, cinema_room_id)` | Detail thuộc đúng booking/suất/phòng. |
| `booking_detail` | `(seat_id, cinema_room_id)` | `seat(id, cinema_room_id)` | Ghế thuộc đúng phòng. |
| `booking_detail` | `ticket_type_id` | `ticket_type(id)` | Loại vé được áp dụng. |
| `ticket` | `booking_detail_id` | `booking_detail(id)` | Vé phát hành từ một detail. |
| `ticket` | `validated_by_user_id` | `app_user(id)` | Nhân viên soát vé; nullable. |
| `payment` | `booking_id` | `booking(id)` | Payment attempt của booking. |
| `payment_event` | `payment_id` | `payment(id)` | Sự kiện của payment. |
| `refund` | `payment_id` | `payment(id)` | Yêu cầu hoàn của payment. |
| `refund` | `requested_by_user_id` | `app_user(id)` | Người yêu cầu hoàn; nullable. |

Mặc định các quan hệ lịch sử và tài chính dùng `ON DELETE RESTRICT`. Chỉ các relation nối thuần túy có thể cân nhắc cascade theo chính sách đã chốt; dữ liệu booking, payment, ticket và lịch sử vận hành không bị xóa dây chuyền.

## 6. Cardinality

| Quan hệ | Cardinality | Cách biểu diễn |
|---|---|---|
| Movie - Genre | M:N | Qua `movie_genre`. |
| Cinema - CinemaRoom | 1:0..N | `cinema_room.cinema_id`. |
| CinemaRoom - Seat | 1:0..N | `seat.cinema_room_id`. |
| SeatType - Seat | 1:0..N | `seat.seat_type_id`. |
| Movie - Showtime | 1:0..N | `showtime.movie_id`. |
| CinemaRoom - Showtime | 1:0..N | `showtime.cinema_room_id`. |
| AppUser - Role | M:N | Qua `user_role`. |
| AppUser - Booking | 1:0..N | `booking.user_id`. |
| Showtime - Booking | 1:0..N | FK ghép của `booking`. |
| Booking - BookingDetail | 1:1..N theo nghiệp vụ | FK ghép của `booking_detail`; tối thiểu một detail được bảo đảm trong transaction tạo booking. |
| Seat - BookingDetail | 1:0..N qua các suất | FK ghép của `booking_detail`; UQ-P ngăn trùng trong cùng suất. |
| TicketType - BookingDetail | 1:0..N | `booking_detail.ticket_type_id`. |
| BookingDetail - Ticket | 1:0..1 | UQ `ticket.booking_detail_id`. |
| Booking - Payment | 1:0..N | `payment.booking_id`. |
| Payment - PaymentEvent | 1:0..N | `payment_event.payment_id`. |
| Payment - Refund | 1:0..1 | UQ `refund.payment_id`. |
| AppUser - Ticket | 1:0..N tùy chọn | `ticket.validated_by_user_id`. |
| AppUser - Refund | 1:0..N tùy chọn | `refund.requested_by_user_id`. |

## 7. Candidate key và quy tắc duy nhất

| Relation | Candidate/Alternate Key | Ý nghĩa |
|---|---|---|
| `genre` | `code`; tên chuẩn hóa | Mã và tên thể loại không trùng. |
| `movie_genre` | `(movie_id, genre_id)` | Một cặp phim-thể loại chỉ xuất hiện một lần. |
| `cinema` | `code` | Mã chi nhánh toàn hệ thống. |
| `cinema_room` | `(cinema_id, code)`; tên chuẩn hóa trong cinema | Mã/tên phòng duy nhất trong chi nhánh. |
| `seat_type` | `code`; tên chuẩn hóa | Danh mục loại ghế. |
| `seat` | `(cinema_room_id, row_label, seat_number)` | Vị trí ghế duy nhất trong phòng. |
| `ticket_type` | `code`; tên chuẩn hóa | Danh mục loại vé. |
| `app_user` | email chuẩn hóa | Một email đăng nhập cho một tài khoản. |
| `role` | `code` | Mã role dùng trong policy. |
| `user_role` | `(user_id, role_id)` | Không cấp trùng role. |
| `booking` | `booking_code`; `(user_id, request_key)` | Tra cứu công khai và idempotency tạo đơn. |
| `booking_detail` | `(booking_id, seat_id)` | Một ghế không lặp trong một booking. |
| `ticket` | `booking_detail_id`; `ticket_code` | Một vé/detail và mã vé duy nhất. |
| `payment` | `merchant_request_id`; `(provider, provider_transaction_id)` khi có mã | Idempotency và định danh giao dịch cổng. |
| `payment_event` | `(payment_id, event_key)` | Chống nhận/xử lý trùng sự kiện. |
| `refund` | `payment_id`; `merchant_refund_id` | Một yêu cầu hoàn toàn bộ/payment và idempotency hoàn. |

Các khóa `(showtime.id, cinema_room_id)`, `(seat.id, cinema_room_id)` và `(booking.id, showtime_id, cinema_room_id)` là alternate key kỹ thuật dùng làm đích cho FK ghép. Chúng không thay đổi định danh nghiệp vụ của entity.

Các UQ-P như ghế đang được phân bổ, payment đang mở và payment đã áp dụng là ràng buộc duy nhất có điều kiện, không phải candidate key của toàn bộ relation.

## 8. Chuẩn hóa dữ liệu

### 8.1. First Normal Form - 1NF

- Mỗi thuộc tính chứa một giá trị nguyên tử trong miền dữ liệu của nó.
- Không lưu danh sách genre, ghế hoặc role dưới dạng chuỗi phân cách hay mảng trong entity chính.
- Quan hệ M:N được tách thành `movie_genre` và `user_role`.
- Mỗi ghế của booking là một hàng `booking_detail`, không dùng cột `seat_ids` hay `quantity` tùy ý.
- `payment_event.payload_summary` là ngoại lệ có chủ đích cho dữ liệu tích hợp thay đổi theo cổng; các khóa, tiền và trạng thái quan trọng vẫn là thuộc tính riêng.

### 8.2. Second Normal Form - 2NF

- `movie_genre` chỉ chứa các thuộc tính phụ thuộc vào toàn bộ PK `(movie_id, genre_id)`; tên phim và tên genre nằm ở relation cha.
- `user_role.assigned_at` và `assigned_by_user_id` mô tả chính lần gán của cặp `(user_id, role_id)`, không phụ thuộc riêng một phần khóa.
- Các relation còn lại có PK đơn nên không phát sinh phụ thuộc bộ phận vào khóa ghép.

### 8.3. Third Normal Form - 3NF

- Thông tin Movie, Genre, Cinema, CinemaRoom, Seat, SeatType, TicketType, User và Role được tách thành relation riêng.
- `ticket` tham chiếu `booking_detail` thay vì lặp `booking_id`, `showtime_id`, `seat_id` và `ticket_type_id`.
- `payment_event` và `refund` phụ thuộc vào `payment`; không lặp dữ liệu booking/user không cần thiết.
- Sức chứa phòng được tính từ ghế thay vì lưu một `cinema_room.capacity` có thể sai lệch.

### 8.4. Phi chuẩn hóa có kiểm soát

Mô hình không tuyên bố 3NF nghiêm ngặt ở mọi relation. Các thuộc tính dư thừa sau được giữ có chủ đích:

| Thuộc tính | Lý do | Cơ chế giữ nhất quán |
|---|---|---|
| `booking.cinema_room_id` | Cho phép FK ghép xác nhận booking thuộc đúng phòng của showtime. | FK ghép và bất biến sau khi tạo. |
| `booking_detail.showtime_id`, `cinema_room_id` | Cho phép FK ghép và UQ-P chống đặt sai/trùng ghế. | Hai FK ghép; không cho sửa sau khi tạo. |
| Snapshot phim/phòng trong `showtime` | Giữ nội dung lịch đã công bố/bán. | Chốt khi lập lịch; hạn chế sửa sau khi có booking. |
| Snapshot ghế, loại vé và giá trong `booking_detail` | Bảo toàn nội dung và giá tại thời điểm đặt. | Backend tính trong transaction; client không được tự ghi. |
| `booking.total_amount` | Số tiền ổn định để gửi cổng và đối soát. | Bằng tổng detail khi tạo; không tính lại từ danh mục. |
| `payment.amount` | Ghi số tiền yêu cầu của từng attempt. | Bằng tổng booking khi khởi tạo; bất biến. |

## 9. Ràng buộc toàn vẹn logic

### 9.1. Entity và referential integrity

- Mọi PK duy nhất và không NULL.
- Mọi FK bắt buộc phải tham chiếu bản ghi cha tồn tại.
- Các FK ghép không có thành phần NULL.
- Dữ liệu lịch sử nghiệp vụ không bị mất bởi cascade delete.

### 9.2. Domain integrity

- Các cột số lượng/thời lượng dương; phụ thu và số tiền giảm không âm.
- `discount_percent` nằm trong `[0, 100]`.
- Giá trị VND dùng số nguyên đồng; booking/payment dùng `currency = 'VND'` trong MVP.
- Mọi cột trạng thái chỉ nhận giá trị trong tập đã quy định.
- Chuỗi bắt buộc sau khi trim không được rỗng.
- Mốc thời gian lưu theo UTC; giao diện chuyển sang `Asia/Ho_Chi_Minh` khi hiển thị.

### 9.3. Business integrity và concurrency

- Một ghế chỉ có tối đa một `booking_detail` đang `held` hoặc `sold` trong cùng showtime.
- Hai showtime không được chồng khoảng `[starts_at, room_available_at)` trong cùng phòng, trừ showtime `cancelled`.
- Tạo booking và toàn bộ detail là thao tác all-or-nothing; booking phải có ít nhất một detail.
- Booking `pending` tương ứng mọi detail `held`; `confirmed` tương ứng `sold`; `expired/cancelled` tương ứng `released`.
- Chỉ payment `succeeded`, đúng số tiền/tiền tệ và được áp dụng mới có thể chuyển booking sang `confirmed` và phát hành ticket.
- Xác nhận booking, chuyển detail sang `sold`, đặt `payment.applied_at` và tạo ticket phải nằm trong cùng transaction.
- Callback payment lặp không được tạo thêm ticket hoặc hạ payment từ `succeeded` về `failed`.
- Chỉ một lần soát vé có thể chuyển `ticket.valid -> used` thành công.

Các quy tắc liên bảng và đường chuyển trạng thái được thực thi bởi service transaction/worker. PK, FK, CHECK, unique index, partial unique index và exclusion constraint là lớp bảo vệ cuối cùng tại database.

## 10. Giả định và quyết định thiết kế

- MVP vận hành một cinema nhưng giữ relation `cinema` để sẵn sàng mở rộng nhiều chi nhánh.
- Booking giữ ghế 5 phút kể từ `booking.created_at`.
- Không hard-code cutoff 10/15/30 phút trước giờ chiếu; `booking_closes_at` là nullable và chính sách cuối cùng còn cần chốt.
- Ghế Couple được đề xuất là một đơn vị bán, `capacity_units = 2`, một booking detail và một ticket.
- Một booking có thể có nhiều payment attempt, nhưng chỉ một payment được áp dụng để phát hành vé.
- Customer cancellation sau thanh toán, hủy suất bởi rạp, refund và reconciliation phức tạp là ngoài phạm vi demo.
- Relation `refund` vẫn được mô hình hóa để giữ khả năng mở rộng, không có nghĩa workflow refund đã được triển khai.
- Authorization không được suy ra từ việc biết UUID; backend luôn kiểm tra role và quyền sở hữu tài nguyên.

## 11. Đối chiếu mô hình

| Hạng mục | Kết quả |
|---|---|
| Số relation | 18 |
| PK đơn | 16 |
| PK ghép | 2: `movie_genre`, `user_role` |
| Quan hệ M:N | 2: Movie-Genre, AppUser-Role |
| FK ghép bảo vệ đúng phòng/suất/ghế | Có |
| Snapshot phục vụ lịch sử | Có ở Showtime, Booking và BookingDetail |
| Chống giữ/bán trùng ghế | UQ-P `(showtime_id, seat_id)` cho `held`, `sold` |
| Chống trùng lịch phòng | Exclusion theo khoảng thời gian và phòng |
| Payment idempotency | `merchant_request_id` và `payment_event(payment_id, event_key)` |
| Phát hành vé idempotent | UQ `ticket.booking_detail_id` |

## 12. Tài liệu tham chiếu

- `database/database-design.md`: nguồn thiết kế entity, thuộc tính, ràng buộc và transaction.
- `database/database-schema.md`: tài liệu schema vật lý cần được đồng bộ từ mô hình này.
- `database/database-schema.sql`: DDL PostgreSQL sẽ được tạo ở bước triển khai database.
- `docs/analysis and design/business-system-design.md`: actors, use cases và luồng nghiệp vụ.
- `docs/requirements/business-system-analysis.md`: phạm vi và yêu cầu nghiệp vụ ở mức phân tích.
