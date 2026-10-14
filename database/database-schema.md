# Database Schema

## 1. Phạm vi và quy ước

Tài liệu này đặc tả schema vật lý PostgreSQL theo 18 relation trong <code>docs/analysis and design/relational-model.md</code> và quy tắc ở <code>database/database-design.md</code>. Đây là đặc tả schema, chưa phải DDL; script tạo cấu trúc thuộc <code>database/database-schema.sql</code>.

MVP vận hành một chi nhánh nhưng mô hình hỗ trợ nhiều rạp. Bảng <code>refund</code> được giữ để mở rộng và truy vết, không có nghĩa luồng hoàn tiền thuộc phạm vi demo.

- PK đơn dùng <code>bigint GENERATED ALWAYS AS IDENTITY</code>; FK dùng <code>bigint</code>. Hai PK ghép là <code>movie_genre(movie_id, genre_id)</code> và <code>user_role(user_id, role_id)</code>.
- UUID cho mã công khai/idempotency do backend sinh; không phụ thuộc extension để tự sinh UUID.
- Thời gian dùng <code>timestamptz</code>, backend ghi UTC và giao diện hiển thị theo <code>Asia/Ho_Chi_Minh</code>.
- Giá VND dùng <code>numeric(14,0)</code>. Số tiền cổng báo nhận và tiền hoàn dùng <code>numeric(18,2)</code> để lưu chính xác sai lệch cần tra soát.
- Trạng thái dùng <code>varchar</code> cùng CHECK constraint, giá trị chữ thường.
- Chuỗi bắt buộc không được rỗng sau <code>btrim</code>. Unique key theo tên/email chuẩn hóa dùng <code>lower(btrim(value))</code>.
- FK mặc định dùng <code>ON UPDATE RESTRICT</code> và <code>ON DELETE RESTRICT</code>, ngoại trừ các bảng nối thuần túy được ghi rõ.
- PK/UNIQUE tự tạo index trong PostgreSQL; không tạo thêm index trùng. Index bổ sung được liệt kê theo bảng.
- Không dùng kiểu SQL Server như <code>NVARCHAR</code>, <code>DATETIME2</code> hay <code>rowversion</code>.

### Điểm cần đồng bộ trước khi viết DDL

Relational model chọn CHECK <code>booking_detail.unit_price &gt;= 0</code>, nhưng giữ <code>booking.total_amount &gt; 0</code>. Database design trước đó ghi giá cuối cùng phải lớn hơn 0 và MVP không xử lý booking miễn phí. Đặc tả này theo lựa chọn mới hơn trong relational model; cần xác nhận và đồng bộ hai tài liệu trước khi triển khai SQL.

## 2. Bảng và quan hệ

| Bảng | Mục đích | Primary key |
|---|---|---|
| <code>movie</code> | Phim | <code>id</code> |
| <code>genre</code> | Danh mục thể loại | <code>id</code> |
| <code>movie_genre</code> | Nối phim với thể loại | <code>(movie_id, genre_id)</code> |
| <code>cinema</code> | Rạp/chi nhánh | <code>id</code> |
| <code>cinema_room</code> | Phòng thuộc rạp | <code>id</code> |
| <code>seat_type</code> | Danh mục loại ghế | <code>id</code> |
| <code>seat</code> | Vị trí ghế trong phòng | <code>id</code> |
| <code>ticket_type</code> | Loại vé và mức giảm | <code>id</code> |
| <code>showtime</code> | Lịch chiếu và khoảng chiếm dụng phòng | <code>id</code> |
| <code>app_user</code> | Tài khoản | <code>id</code> |
| <code>role</code> | Danh mục vai trò | <code>id</code> |
| <code>user_role</code> | Nối tài khoản với vai trò | <code>(user_id, role_id)</code> |
| <code>booking</code> | Đơn đặt vé | <code>id</code> |
| <code>booking_detail</code> | Ghế, loại vé và giá snapshot của đơn | <code>id</code> |
| <code>ticket</code> | Vé phát hành từ một booking detail | <code>id</code> |
| <code>payment</code> | Một lần thử thanh toán | <code>id</code> |
| <code>payment_event</code> | Callback/kết quả đối soát | <code>id</code> |
| <code>refund</code> | Cấu trúc mở rộng cho một yêu cầu hoàn/payment | <code>id</code> |

Movie–Genre và AppUser–Role là M:N qua bảng nối. Cinema–CinemaRoom, CinemaRoom–Seat, Movie–Showtime, Booking–Payment và Payment–PaymentEvent là 1:N. Booking–BookingDetail là 1:N; mỗi booking phải có ít nhất một detail do transaction bảo đảm. BookingDetail–Ticket và Payment–Refund là 1:0..1 nhờ UNIQUE.

## 3. Đặc tả từng bảng

### 3.1. movie

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | ID nội bộ |
| <code>title</code> | <code>varchar(255)</code> | Không | — | — | Không rỗng sau trim |
| <code>description</code> | <code>text</code> | Có | NULL | — | — |
| <code>duration_minutes</code> | <code>smallint</code> | Không | — | — | Lớn hơn 0 |
| <code>release_date</code> | <code>date</code> | Có | NULL | — | — |
| <code>poster_url</code> | <code>text</code> | Có | NULL | — | — |
| <code>trailer_url</code> | <code>text</code> | Có | NULL | — | — |
| <code>language</code> | <code>varchar(50)</code> | Có | NULL | — | — |
| <code>age_rating</code> | <code>varchar(20)</code> | Có | NULL | — | Nhãn không tự quyết định quyền mua |
| <code>status</code> | <code>varchar(20)</code> | Không | <code>coming_soon</code> | — | <code>coming_soon</code>, <code>now_showing</code>, <code>ended</code>, <code>archived</code> |

Ràng buộc: <code>ck_movie_duration_positive</code>, <code>ck_movie_status</code>, <code>ck_movie_title_nonblank</code>. Không unique title; phim đã có lịch được lưu trữ bằng status, không xóa lịch sử. Index bổ sung: <code>ix_movie_status_release_date_id(status, release_date DESC, id)</code>.

### 3.2. genre

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>code</code> | <code>varchar(30)</code> | Không | — | UQ | Mã ổn định |
| <code>name</code> | <code>varchar(100)</code> | Không | — | UQ chuẩn hóa | Unique theo <code>lower(btrim(name))</code> |
| <code>description</code> | <code>text</code> | Có | NULL | — | — |
| <code>is_active</code> | <code>boolean</code> | Không | <code>true</code> | — | — |

Ràng buộc: <code>uq_genre_code</code>; unique expression index <code>ux_genre_name_normalized(lower(btrim(name)))</code>; CHECK chuỗi bắt buộc không rỗng. Thể loại đang được tham chiếu không xóa; có thể đặt inactive.

### 3.3. movie_genre

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>movie_id</code> | <code>bigint</code> | Không | — | PK ghép, FK | → <code>movie(id)</code> |
| <code>genre_id</code> | <code>bigint</code> | Không | — | PK ghép, FK | → <code>genre(id)</code> |

PK <code>(movie_id, genre_id)</code> ngăn gán lặp. FK movie dùng <code>ON DELETE CASCADE</code> chỉ khi phim đủ điều kiện hard delete; FK genre dùng RESTRICT. Index bổ sung: <code>ix_movie_genre_genre_movie(genre_id, movie_id)</code>.

### 3.4. cinema

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>code</code> | <code>varchar(30)</code> | Không | — | UQ | Mã duy nhất toàn hệ thống |
| <code>name</code> | <code>varchar(150)</code> | Không | — | — | Không rỗng |
| <code>address</code> | <code>text</code> | Không | — | — | Không rỗng |
| <code>phone</code> | <code>varchar(30)</code> | Có | NULL | — | Không mặc định duy nhất |
| <code>status</code> | <code>varchar(20)</code> | Không | <code>active</code> | — | <code>active</code>, <code>inactive</code> |

Ràng buộc: <code>uq_cinema_code</code>; CHECK status và chuỗi bắt buộc không rỗng. Không hard delete rạp đã có phòng/lịch/đơn.

### 3.5. cinema_room

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>cinema_id</code> | <code>bigint</code> | Không | — | FK | → <code>cinema(id)</code> |
| <code>code</code> | <code>varchar(20)</code> | Không | — | UQ ghép | Duy nhất trong rạp |
| <code>name</code> | <code>varchar(100)</code> | Không | — | UQ ghép chuẩn hóa | Duy nhất trong rạp theo trim/lower |
| <code>status</code> | <code>varchar(20)</code> | Không | <code>active</code> | — | <code>active</code>, <code>maintenance</code>, <code>inactive</code> |

FK cinema dùng RESTRICT. Unique <code>(cinema_id, code)</code>; unique expression index <code>ux_cinema_room_name_normalized(cinema_id, lower(btrim(name)))</code>; CHECK status/chuỗi. Không lưu capacity; sức chứa suy ra từ ghế và loại ghế.

### 3.6. seat_type

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>code</code> | <code>varchar(30)</code> | Không | — | UQ | Ví dụ STANDARD, VIP, COUPLE |
| <code>name</code> | <code>varchar(100)</code> | Không | — | UQ chuẩn hóa | Unique sau trim/lower |
| <code>capacity_units</code> | <code>smallint</code> | Không | <code>1</code> | — | Chỉ 1 hoặc 2 |
| <code>surcharge_amount</code> | <code>numeric(14,0)</code> | Không | <code>0</code> | — | VND, không âm |
| <code>is_active</code> | <code>boolean</code> | Không | <code>true</code> | — | — |

Ràng buộc: UQ code; unique expression index trên tên chuẩn hóa; CHECK <code>capacity_units IN (1, 2)</code>, <code>surcharge_amount &gt;= 0</code> và chuỗi bắt buộc không rỗng. Thay phụ thu không đổi giá snapshot của booking cũ.

### 3.7. seat

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>cinema_room_id</code> | <code>bigint</code> | Không | — | FK | → <code>cinema_room(id)</code> |
| <code>seat_type_id</code> | <code>bigint</code> | Không | — | FK | → <code>seat_type(id)</code> |
| <code>row_label</code> | <code>varchar(10)</code> | Không | — | — | Chuẩn hóa chữ hoa ở backend, không rỗng |
| <code>seat_number</code> | <code>smallint</code> | Không | — | — | Lớn hơn 0 |
| <code>status</code> | <code>varchar(20)</code> | Không | <code>active</code> | — | <code>active</code>, <code>maintenance</code>, <code>inactive</code> |

FK phòng và loại ghế dùng RESTRICT. Unique vị trí <code>(cinema_room_id, row_label, seat_number)</code>; composite UQ <code>(id, cinema_room_id)</code> làm đích FK ghép; CHECK số ghế dương, status hợp lệ và chuỗi không rỗng. Seat.status là trạng thái vật lý, không biểu diễn held/sold. Index bổ sung: <code>ix_seat_seat_type_id(seat_type_id)</code>.

### 3.8. ticket_type

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>code</code> | <code>varchar(30)</code> | Không | — | UQ | Ví dụ ADULT, CHILD, STUDENT |
| <code>name</code> | <code>varchar(100)</code> | Không | — | UQ chuẩn hóa | Unique sau trim/lower |
| <code>discount_percent</code> | <code>numeric(5,2)</code> | Không | <code>0</code> | — | 0 đến 100 |
| <code>eligibility_note</code> | <code>text</code> | Có | NULL | — | Điều kiện áp dụng |
| <code>is_active</code> | <code>boolean</code> | Không | <code>true</code> | — | — |

Ràng buộc: UQ code; unique expression index trên tên chuẩn hóa; CHECK <code>discount_percent BETWEEN 0 AND 100</code>. Backend xác minh điều kiện loại vé, không nhận mức giảm từ client.

### 3.9. showtime

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>movie_id</code> | <code>bigint</code> | Không | — | FK | → <code>movie(id)</code> |
| <code>cinema_room_id</code> | <code>bigint</code> | Không | — | FK | → <code>cinema_room(id)</code> |
| <code>movie_title_snapshot</code> | <code>varchar(255)</code> | Không | — | — | Tên tại lúc lập lịch |
| <code>room_name_snapshot</code> | <code>varchar(100)</code> | Không | — | — | Tên phòng tại lúc lập lịch |
| <code>duration_minutes_snapshot</code> | <code>smallint</code> | Không | — | — | Lớn hơn 0 |
| <code>starts_at</code> | <code>timestamptz</code> | Không | — | — | Bắt đầu |
| <code>ends_at</code> | <code>timestamptz</code> | Không | — | — | Kết thúc suất |
| <code>room_available_at</code> | <code>timestamptz</code> | Không | — | — | Bao gồm thời gian dọn phòng |
| <code>booking_closes_at</code> | <code>timestamptz</code> | Có | NULL | — | NULL nếu chưa cấu hình cutoff |
| <code>base_price</code> | <code>numeric(14,0)</code> | Không | — | — | VND, lớn hơn 0 |
| <code>status</code> | <code>varchar(20)</code> | Không | <code>draft</code> | — | <code>draft</code>, <code>scheduled</code>, <code>in_progress</code>, <code>completed</code>, <code>cancelled</code> |
| <code>cancelled_at</code> | <code>timestamptz</code> | Có | NULL | — | Bắt buộc nếu cancelled |
| <code>cancellation_reason</code> | <code>text</code> | Có | NULL | — | Bắt buộc, không rỗng nếu cancelled |

FK movie/room dùng RESTRICT. Composite UQ <code>(id, cinema_room_id)</code> làm đích FK ghép. CHECK thời lượng/giá dương; <code>starts_at &lt; ends_at &lt;= room_available_at</code>; <code>booking_closes_at IS NULL OR booking_closes_at &lt;= starts_at</code>; status hợp lệ; trạng thái cancelled tương đương có cancelled_at và lý do không rỗng, status khác không có hai giá trị này.

Exclusion constraint <code>ex_showtime_room_time</code> ngăn các khoảng <code>[starts_at, room_available_at)</code> giao nhau trong cùng phòng, trừ suất cancelled. Cần extension <code>btree_gist</code>; constraint dùng <code>cinema_room_id WITH =</code> và <code>tstzrange(starts_at, room_available_at, '[)') WITH &amp;&amp;</code> với predicate <code>status &lt;&gt; 'cancelled'</code>. Service vẫn kiểm tra status phòng/phim, thời lượng và điều kiện mở bán. Index bổ sung: <code>ix_showtime_movie_starts_at(movie_id, starts_at)</code>, <code>ix_showtime_room_starts_at(cinema_room_id, starts_at)</code>.

### 3.10. app_user

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>email</code> | <code>varchar(254)</code> | Không | — | UQ chuẩn hóa | Unique theo <code>lower(btrim(email))</code> |
| <code>password_hash</code> | <code>text</code> | Không | — | — | Chỉ lưu hash |
| <code>full_name</code> | <code>varchar(150)</code> | Không | — | — | Không rỗng sau trim |
| <code>phone</code> | <code>varchar(20)</code> | Có | NULL | — | Không mặc định duy nhất |
| <code>status</code> | <code>varchar(20)</code> | Không | <code>active</code> | — | <code>active</code>, <code>locked</code>, <code>deactivated</code> |
| <code>email_verified_at</code> | <code>timestamptz</code> | Có | NULL | — | — |
| <code>last_login_at</code> | <code>timestamptz</code> | Có | NULL | — | — |

Ràng buộc: unique expression index <code>ux_app_user_email_normalized</code>; CHECK status và email/password_hash/full_name không rỗng. Không lưu mật khẩu gốc; email chuẩn hóa không gộp các biến thể dấu chấm/dấu cộng theo quy tắc riêng của nhà cung cấp.

### 3.11. role

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>code</code> | <code>varchar(30)</code> | Không | — | UQ | Mã policy ổn định |
| <code>name</code> | <code>varchar(100)</code> | Không | — | — | Không rỗng sau trim |
| <code>description</code> | <code>text</code> | Có | NULL | — | — |
| <code>is_active</code> | <code>boolean</code> | Không | <code>true</code> | — | — |

Ràng buộc: <code>uq_role_code</code>; CHECK code/name không rỗng. Mã seed dự kiến CUSTOMER, STAFF, MANAGER, ADMIN; quyền được backend authorization policy quyết định.

### 3.12. user_role

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>user_id</code> | <code>bigint</code> | Không | — | PK ghép, FK | → <code>app_user(id)</code> |
| <code>role_id</code> | <code>bigint</code> | Không | — | PK ghép, FK | → <code>role(id)</code> |
| <code>assigned_at</code> | <code>timestamptz</code> | Không | <code>CURRENT_TIMESTAMP</code> | — | Thời điểm cấp |
| <code>assigned_by_user_id</code> | <code>bigint</code> | Có | NULL | FK | → <code>app_user(id)</code> |

PK <code>(user_id, role_id)</code> ngăn cấp trùng. FK user_id có thể CASCADE chỉ khi user chưa có dữ liệu lịch sử cần giữ; role_id và assigned_by_user_id dùng RESTRICT. Index bổ sung: <code>ix_user_role_role_user(role_id, user_id)</code>, <code>ix_user_role_assigned_by(assigned_by_user_id)</code>.

### 3.13. booking

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>booking_code</code> | <code>uuid</code> | Không | — | UQ | Backend sinh mã tra cứu |
| <code>user_id</code> | <code>bigint</code> | Không | — | FK | → <code>app_user(id)</code> |
| <code>showtime_id</code> | <code>bigint</code> | Không | — | FK ghép | Cùng cinema_room_id → showtime |
| <code>cinema_room_id</code> | <code>bigint</code> | Không | — | FK ghép | Phòng của showtime |
| <code>request_key</code> | <code>uuid</code> | Không | — | UQ ghép | Idempotency |
| <code>request_fingerprint</code> | <code>varchar(64)</code> | Không | — | — | SHA-256 dữ liệu đặt chuẩn hóa |
| <code>contact_email_snapshot</code> | <code>varchar(254)</code> | Không | — | — | Email tại thời điểm đặt |
| <code>contact_phone_snapshot</code> | <code>varchar(20)</code> | Có | NULL | — | — |
| <code>total_amount</code> | <code>numeric(14,0)</code> | Không | — | — | VND, lớn hơn 0 |
| <code>currency</code> | <code>varchar(3)</code> | Không | <code>VND</code> | — | CHECK bằng VND trong MVP |
| <code>expires_at</code> | <code>timestamptz</code> | Không | — | — | Hạn giữ ghế |
| <code>status</code> | <code>varchar(20)</code> | Không | <code>pending</code> | — | <code>pending</code>, <code>confirmed</code>, <code>expired</code>, <code>cancelled</code> |
| <code>confirmed_at</code> | <code>timestamptz</code> | Có | NULL | — | Bắt buộc khi confirmed |
| <code>cancelled_at</code> | <code>timestamptz</code> | Có | NULL | — | Bắt buộc khi cancelled |
| <code>cancellation_reason</code> | <code>text</code> | Có | NULL | — | Bắt buộc, không rỗng khi cancelled |
| <code>created_at</code> | <code>timestamptz</code> | Không | <code>CURRENT_TIMESTAMP</code> | — | Bắt đầu thời gian giữ |

FK user và FK ghép <code>(showtime_id, cinema_room_id) → showtime(id, cinema_room_id)</code> dùng RESTRICT. UNIQUE: booking_code; <code>(user_id, request_key)</code>; <code>(id, showtime_id, cinema_room_id)</code> là UQ kỹ thuật làm đích FK ghép từ detail.

CHECK: total_amount dương; currency VND; expires_at lớn hơn created_at; status hợp lệ; confirmed phải có confirmed_at, pending/expired không có; cancelled phải có cancelled_at và lý do không rỗng, status khác không có trường hủy. Check không thể tính tổng các detail. Transaction tạo booking phải tạo ít nhất một detail và giữ toàn bộ ghế nguyên tử. Hạn giữ là 5 phút từ created_at do service/worker thực thi. Cùng user/request_key nhưng khác fingerprint bị từ chối.

Index bổ sung: <code>ix_booking_user_history(user_id, created_at DESC, id DESC)</code>; <code>ix_booking_showtime_status(showtime_id, status, id)</code>; <code>ix_booking_expiry(expires_at, id) WHERE status = 'pending'</code>.

### 3.14. booking_detail

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>booking_id</code> | <code>bigint</code> | Không | — | FK ghép | Cùng suất/phòng → booking |
| <code>showtime_id</code> | <code>bigint</code> | Không | — | FK ghép, UQ-P | Ghế hiệu lực duy nhất theo suất |
| <code>cinema_room_id</code> | <code>bigint</code> | Không | — | FK ghép | Khớp booking và seat |
| <code>seat_id</code> | <code>bigint</code> | Không | — | FK ghép, UQ | Ghế vật lý |
| <code>ticket_type_id</code> | <code>bigint</code> | Không | — | FK | → <code>ticket_type(id)</code> |
| <code>seat_label_snapshot</code> | <code>varchar(30)</code> | Không | — | — | Nhãn tại lúc đặt |
| <code>seat_type_name_snapshot</code> | <code>varchar(100)</code> | Không | — | — | Loại ghế tại lúc đặt |
| <code>ticket_type_name_snapshot</code> | <code>varchar(100)</code> | Không | — | — | Loại vé tại lúc đặt |
| <code>capacity_units_snapshot</code> | <code>smallint</code> | Không | — | — | Chỉ 1 hoặc 2 |
| <code>base_amount</code> | <code>numeric(14,0)</code> | Không | — | — | VND, lớn hơn 0 |
| <code>surcharge_amount</code> | <code>numeric(14,0)</code> | Không | — | — | VND, không âm |
| <code>discount_percent_snapshot</code> | <code>numeric(5,2)</code> | Không | — | — | 0 đến 100 |
| <code>discount_amount</code> | <code>numeric(14,0)</code> | Không | — | — | 0 đến base_amount |
| <code>unit_price</code> | <code>numeric(14,0)</code> | Không | — | — | VND, không âm; base + surcharge − discount |
| <code>allocation_status</code> | <code>varchar(20)</code> | Không | <code>held</code> | UQ-P có điều kiện | <code>held</code>, <code>sold</code>, <code>released</code> |
| <code>released_at</code> | <code>timestamptz</code> | Có | NULL | — | Có đúng khi released |

FK ghép: <code>(booking_id, showtime_id, cinema_room_id) → booking(id, showtime_id, cinema_room_id)</code>; <code>(seat_id, cinema_room_id) → seat(id, cinema_room_id)</code>; FK ticket_type → ticket_type(id). Dùng RESTRICT. UNIQUE toàn phần <code>(booking_id, seat_id)</code>; UQ kỹ thuật <code>(booking_id, showtime_id, cinema_room_id)</code>; partial unique index <code>ux_booking_detail_active_seat(showtime_id, seat_id) WHERE allocation_status IN ('held', 'sold')</code>.

CHECK: capacity_units_snapshot thuộc {1,2}; base_amount dương; surcharge_amount không âm; discount_percent_snapshot từ 0 đến 100; <code>0 &lt;= discount_amount &lt;= base_amount</code>; <code>unit_price &gt;= 0</code> và <code>unit_price = base_amount + surcharge_amount - discount_amount</code>; status hợp lệ; released tương đương released_at có giá trị. Giá snapshot do backend tính, không cập nhật theo danh mục. Giảm giá chỉ áp dụng base_amount; phụ thu tính một lần mỗi detail. Tổng booking được tính trong transaction, không phải CHECK liên bảng.

Index bổ sung: <code>ix_booking_detail_seat_room(seat_id, cinema_room_id)</code>; <code>ix_booking_detail_ticket_type(ticket_type_id)</code>.

### 3.15. ticket

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>booking_detail_id</code> | <code>bigint</code> | Không | — | UQ, FK | → <code>booking_detail(id)</code>; tối đa một vé/detail |
| <code>ticket_code</code> | <code>uuid</code> | Không | — | UQ | Backend sinh mã |
| <code>status</code> | <code>varchar(20)</code> | Không | <code>valid</code> | — | <code>valid</code>, <code>used</code>, <code>voided</code> |
| <code>issued_at</code> | <code>timestamptz</code> | Không | — | — | Thời điểm phát hành |
| <code>used_at</code> | <code>timestamptz</code> | Có | NULL | — | Thời điểm soát thành công |
| <code>validated_by_user_id</code> | <code>bigint</code> | Có | NULL | FK | → <code>app_user(id)</code> |
| <code>voided_at</code> | <code>timestamptz</code> | Có | NULL | — | Thời điểm vô hiệu hóa |
| <code>void_reason</code> | <code>text</code> | Có | NULL | — | Lý do vô hiệu |

FK booking_detail và validated_by_user dùng RESTRICT. CHECK status/timestamp: valid không có mốc dùng/hủy; used có used_at và validated_by_user_id, không có dữ liệu hủy; voided có voided_at và lý do không rỗng, không có used_at. Vé used không được void theo quy tắc hiện tại. Index bổ sung: <code>ix_ticket_validated_by_user(validated_by_user_id)</code>.

### 3.16. payment

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>booking_id</code> | <code>bigint</code> | Không | — | FK | → <code>booking(id)</code> |
| <code>provider</code> | <code>varchar(30)</code> | Không | — | — | Mã cổng |
| <code>merchant_request_id</code> | <code>uuid</code> | Không | — | UQ | Backend sinh idempotency key |
| <code>provider_transaction_id</code> | <code>varchar(150)</code> | Có | NULL | UQ-P | Unique theo provider nếu khác NULL |
| <code>method</code> | <code>varchar(50)</code> | Có | NULL | — | Phương thức do cổng xác nhận |
| <code>amount</code> | <code>numeric(14,0)</code> | Không | — | — | VND, lớn hơn 0 |
| <code>currency</code> | <code>varchar(3)</code> | Không | <code>VND</code> | — | CHECK bằng VND trong MVP |
| <code>received_amount</code> | <code>numeric(18,2)</code> | Có | NULL | — | Số tiền thực thu đã xác minh, không âm |
| <code>received_currency</code> | <code>varchar(3)</code> | Có | NULL | — | Tiền tệ thực thu |
| <code>status</code> | <code>varchar(20)</code> | Không | <code>created</code> | UQ-P có điều kiện | <code>created</code>, <code>pending</code>, <code>unknown</code>, <code>succeeded</code>, <code>failed</code>, <code>cancelled</code> |
| <code>applied_at</code> | <code>timestamptz</code> | Có | NULL | UQ-P có điều kiện | Lần thu được áp dụng cho booking |
| <code>paid_at</code> | <code>timestamptz</code> | Có | NULL | — | Thời điểm cổng báo thu |
| <code>verified_at</code> | <code>timestamptz</code> | Có | NULL | — | Backend xác minh |
| <code>reconciliation_status</code> | <code>varchar(20)</code> | Không | <code>none</code> | — | <code>none</code>, <code>needs_review</code>, <code>refund_required</code>, <code>resolved</code> |
| <code>failure_code</code> | <code>varchar(100)</code> | Có | NULL | — | Mã lỗi đã lọc |
| <code>next_reconcile_at</code> | <code>timestamptz</code> | Có | <code>CURRENT_TIMESTAMP</code> | — | NULL khi không còn retry |
| <code>created_at</code> | <code>timestamptz</code> | Không | <code>CURRENT_TIMESTAMP</code> | — | Thời điểm tạo attempt |

FK booking dùng RESTRICT. UNIQUE merchant_request_id. Partial unique indexes: <code>ux_payment_provider_transaction(provider, provider_transaction_id) WHERE provider_transaction_id IS NOT NULL</code>; <code>ux_payment_open_booking(booking_id) WHERE status IN ('created', 'pending', 'unknown')</code>; <code>ux_payment_applied_booking(booking_id) WHERE applied_at IS NOT NULL</code>.

CHECK: amount dương; currency VND; received_amount nếu có thì không âm; status/reconciliation_status hợp lệ; chuỗi bắt buộc không rỗng. Succeeded yêu cầu paid_at, verified_at, received_amount, received_currency và provider_transaction_id. applied_at chỉ được đặt khi succeeded và received_amount/received_currency khớp amount/currency. Đối chiếu amount/currency với booking, điều kiện booking pending và cập nhật booking/detail/ticket là invariant của service transaction.

Index bổ sung: <code>ix_payment_booking_created(booking_id, created_at DESC)</code>; <code>ix_payment_reconcile_due(next_reconcile_at, id) WHERE status IN ('created', 'pending', 'unknown')</code>; <code>ix_payment_reconciliation_queue(reconciliation_status, created_at, id) WHERE reconciliation_status IN ('needs_review', 'refund_required')</code>; <code>ix_payment_paid_at(paid_at) WHERE status = 'succeeded'</code>.

### 3.17. payment_event

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>payment_id</code> | <code>bigint</code> | Không | — | FK | → <code>payment(id)</code> |
| <code>source</code> | <code>varchar(20)</code> | Không | — | — | <code>webhook</code>, <code>reconcile</code> |
| <code>event_key</code> | <code>varchar(200)</code> | Không | — | UQ ghép | Idempotency trong payment |
| <code>event_type</code> | <code>varchar(50)</code> | Không | — | — | Loại sự kiện |
| <code>payload_summary</code> | <code>jsonb</code> | Không | <code>'{}'::jsonb</code> | — | Dữ liệu tích hợp đã lọc |
| <code>received_at</code> | <code>timestamptz</code> | Không | <code>CURRENT_TIMESTAMP</code> | — | Thời điểm tiếp nhận |
| <code>processed_at</code> | <code>timestamptz</code> | Có | NULL | — | Thời điểm xử lý thành công |
| <code>status</code> | <code>varchar(20)</code> | Không | <code>received</code> | — | <code>received</code>, <code>processed</code>, <code>failed</code> |
| <code>retry_count</code> | <code>integer</code> | Không | <code>0</code> | — | Không âm |
| <code>next_retry_at</code> | <code>timestamptz</code> | Có | <code>CURRENT_TIMESTAMP</code> | — | NULL khi không retry |
| <code>last_error</code> | <code>varchar(500)</code> | Có | NULL | — | Lỗi đã lọc |

FK payment dùng RESTRICT. UNIQUE <code>(payment_id, event_key)</code>; CHECK source/status, retry_count không âm, chuỗi bắt buộc không rỗng và processed tương đương processed_at có giá trị. Event được lưu trước ACK; đánh dấu processed và cập nhật nghiệp vụ phải commit cùng transaction xử lý. Index bổ sung: <code>ix_payment_event_retry(next_retry_at, id) WHERE status IN ('received', 'failed')</code>. Không lưu secret hoặc dữ liệu thẻ trong payload.

### 3.18. refund

Bảng này là cấu trúc mở rộng, không phải luồng MVP.

| Cột | Kiểu PostgreSQL | NULL? | Mặc định | Khóa/duy nhất | CHECK / ghi chú |
|---|---|---:|---|---|---|
| <code>id</code> | <code>bigint GENERATED ALWAYS AS IDENTITY</code> | Không | Identity | PK | — |
| <code>payment_id</code> | <code>bigint</code> | Không | — | UQ, FK | → <code>payment(id)</code>; tối đa một refund/payment |
| <code>merchant_refund_id</code> | <code>uuid</code> | Không | — | UQ | Backend sinh idempotency key |
| <code>provider_refund_id</code> | <code>varchar(150)</code> | Có | NULL | — | Mã do provider cấp |
| <code>amount</code> | <code>numeric(18,2)</code> | Không | — | — | Lớn hơn 0 |
| <code>currency</code> | <code>varchar(3)</code> | Không | — | — | Tiền tệ thực thu |
| <code>reason</code> | <code>text</code> | Không | — | — | Không rỗng |
| <code>status</code> | <code>varchar(20)</code> | Không | <code>pending</code> | — | <code>pending</code>, <code>unknown</code>, <code>succeeded</code>, <code>failed</code> |
| <code>requested_by_user_id</code> | <code>bigint</code> | Có | NULL | FK | → <code>app_user(id)</code>; NULL nếu do hệ thống |
| <code>completed_at</code> | <code>timestamptz</code> | Có | NULL | — | Có khi succeeded |
| <code>next_retry_at</code> | <code>timestamptz</code> | Có | <code>CURRENT_TIMESTAMP</code> | — | NULL khi không retry |
| <code>last_error</code> | <code>varchar(500)</code> | Có | NULL | — | Lỗi đã lọc |

FK payment và requested_by_user dùng RESTRICT. UNIQUE payment_id và merchant_refund_id; CHECK amount dương, status hợp lệ, chuỗi bắt buộc không rỗng và succeeded tương đương completed_at có giá trị.

Index bổ sung: <code>ix_refund_retry(next_retry_at, id) WHERE status IN ('pending', 'unknown', 'failed')</code>; <code>ix_refund_requested_by(requested_by_user_id)</code>; <code>ix_refund_completed_at(completed_at) WHERE status = 'succeeded'</code>. Tính duy nhất của provider_refund_id theo provider chưa enforce được bằng index vì provider chỉ nằm ở payment; hiện để service kiểm tra hoặc thay đổi mô hình ở bước sau.

## 4. Toàn vẹn xuyên bảng

- PostgreSQL bảo đảm PK, FK đơn/ghép, UNIQUE, unique partial/expression index, CHECK và exclusion constraint lịch phòng.
- Service transaction bảo đảm booking có ít nhất một detail; tổng booking bằng tổng snapshot; giá/loại vé do server tính; chuyển trạng thái booking-detail-payment-ticket nguyên tử; expiry/release; xác minh callback và quyền sở hữu.
- Worker xử lý booking hết hạn, đối soát payment và retry sự kiện. Thời gian trôi qua không tự loại hàng khỏi partial index; không dùng điều kiện index phụ thuộc <code>now()</code>.
- Không cascade xóa dữ liệu booking/payment/ticket. FK lịch sử dùng RESTRICT; cascade chỉ áp dụng cho relation nối thuần túy như đã nêu.
- Một ghế có tối đa một detail held/sold trên mỗi suất. Booking pending giữ ghế 5 phút từ created_at; worker/service chuyển booking và detail sang expired/released trong cùng transaction.
- Mỗi booking tối đa một payment attempt created/pending/unknown và tối đa một payment có applied_at. Các attempt succeeded khác vẫn được lưu để tra soát.
- Callback trùng bị chặn bởi UNIQUE(payment_id, event_key); mỗi booking_detail tối đa một ticket; soát vé bằng cập nhật có điều kiện valid → used.

## 5. Chuẩn hóa và snapshot

Danh mục lưu riêng; Movie–Genre và AppUser–Role dùng bảng nối; mỗi ghế trong booking là một hàng booking_detail, không lưu danh sách ghế trong chuỗi. Ticket tham chiếu booking_detail thay vì lặp khóa booking, suất, ghế và loại vé.

Schema không tuyên bố mọi bảng đạt 3NF nghiêm ngặt. Snapshot tên phim/phòng, ghế/loại vé/giá cùng booking.total_amount và payment.amount được giữ để bảo toàn nội dung, giá đã chốt và dữ liệu đối soát. Các cột lặp cinema_room_id/showtime_id phục vụ FK ghép chống ghế sai phòng. Backend ghi snapshot trong transaction tạo booking và không tính lại từ danh mục hiện hành.

## 6. Tài liệu liên quan

- <code>database/database-design.md</code> — entity, nghiệp vụ, data type, constraint, index và concurrency.
- <code>docs/analysis and design/relational-model.md</code> — relation, PK/FK ghép, candidate key, cardinality và chuẩn hóa.
- <code>database/database-schema.sql</code> — DDL PostgreSQL sẽ được triển khai ở bước tiếp theo.
