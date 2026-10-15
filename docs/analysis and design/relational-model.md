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
- `AK`: Alternate Key — candidate key không được chọn làm PK (candidate key là siêu khóa tối thiểu; alternate key là candidate key không chọn làm PK).
- `UQ`: ràng buộc duy nhất. `UQ-C`: composite unique constraint — ràng buộc duy nhất kỹ thuật phục vụ làm đích cho FK ghép, không mặc nhiên là candidate key vì đã chứa PK và không phải khóa tối thiểu.
- `UQ-P`: partial unique index — duy nhất có điều kiện, chỉ áp dụng cho các hàng thỏa điều kiện; không phải candidate key của toàn relation.

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
UQ-C: (id, cinema_room_id)
```

`(id, cinema_room_id)` không phải candidate key (đã chứa PK, không phải siêu khóa tối thiểu) mà là composite unique constraint phục vụ FK ghép từ `booking_detail`. `status` chỉ mô tả tình trạng vật lý (`active`, `maintenance`, `inactive`), không dùng để biểu diễn ghế đang được giữ hoặc đã bán; trạng thái phân bổ thuộc `booking_detail.allocation_status`.

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

UQ-C: (id, cinema_room_id)
```

`(id, cinema_room_id)` là composite unique constraint phục vụ FK ghép từ `booking`, không phải candidate key. Các mốc thời gian thỏa `starts_at < ends_at <= room_available_at`; nếu có `booking_closes_at` thì `booking_closes_at <= starts_at` (không đóng bán sau giờ chiếu bắt đầu; trong MVP `booking_closes_at` có thể NULL). Khoảng chiếm dụng phòng là `[starts_at, room_available_at)`. Hai suất không bị chồng khoảng này trong cùng phòng, ngoại trừ suất đã `cancelled`.

Ràng buộc không chồng lấn được bảo đảm bằng exclusion constraint PostgreSQL: khoảng lưu bằng `tstzrange` với giới hạn `[starts_at, room_available_at)`, dùng extension `btree_gist` để đưa `cinema_room_id` vào ràng buộc, và toán tử chồng lấn `&&` để từ chối các khoảng giao nhau. Pseudo-SQL minh họa (DDL đầy đủ thuộc `database-schema.sql`):

```sql
EXCLUDE USING gist (
    cinema_room_id WITH =,
    tstzrange(starts_at, room_available_at, '[)') WITH &&
)
WHERE (status <> 'cancelled');
```

Điều kiện `WHERE status <> 'cancelled'` cho phép các suất đã hủy không còn chiếm phòng. Khi khôi phục một suất đã `cancelled` (ví dụ do hủy nhầm), service phải chuyển trạng thái về một trạng thái chiếm phòng và transaction phải chạy lại kiểm tra xung đột lịch — trong PostgreSQL, exclusion constraint với điều kiện partial sẽ tự từ chối insert/update tạo chồng lấn, nên việc khôi phục phải xảy ra trong transaction đọc-kiểm-tra-ghi với khóa phù hợp để race condition được database từ chối an toàn. Exclusion constraint là lớp bảo vệ cuối cùng; việc kiểm tra tính khả dụng của phòng trước khi lập lịch vẫn là trách nhiệm của service để trả về thông báo nghiệp vụ thân thiện thay vì lỗi constraint.

Về hủy suất: khi rạp hủy một suất, mô hình chỉ quy định phần dữ liệu — suất được đánh dấu `cancelled`, lưu `cancelled_at` và `cancellation_reason`. Các booking, payment, ticket lịch sử liên quan không bị xóa (xem 9.3) để giữ khả năng truy vết. Workflow tự động thông báo khách, đổi lịch hoặc hoàn tiền khi rạp hủy suất **chưa được triển khai trong demo**; điều này không đồng nghĩa nghiệp vụ đã quyết định không hoàn tiền — chính sách về quyền lợi của khách đã thanh toán khi rạp hủy suất là quyết định nghiệp vụ cần được chủ dự án xác nhận (chưa có tài liệu yêu cầu quy định rõ).

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
UQ-C: (id, showtime_id, cinema_room_id)
```

`(id, showtime_id, cinema_room_id)` là composite unique constraint phục vụ FK ghép từ `booking_detail`, không phải candidate key. `request_key` hỗ trợ idempotency khi tạo booking; cùng key nhưng khác `request_fingerprint` phải bị từ chối; `(user_id, request_key)` là candidate key/alternate key thực sự. Trong MVP mọi request tạo booking đều phải có `request_key`, nên `request_key` bắt buộc (NOT NULL) và AK `(user_id, request_key)` là unique constraint toàn phần — `database-design.md` cũng ghi `request_key` NN, và quy tắc NOT NULL này phải được chuyển nguyên trạng sang `database-schema.md`/`database-schema.sql`. Nếu sau này cho phép tạo booking không có `request_key` (nullable), cần lưu ý unique constraint thông thường trong PostgreSQL cho phép nhiều hàng có giá trị `NULL` ở cột đó (NULL không bằng NULL); khi đó AK phải chuyển thành partial unique index `WHERE request_key IS NOT NULL` — thay đổi này cần chốt nghiệp vụ trước khi triển khai. `expires_at > created_at`; thời gian giữ ghế là 5 phút tính từ `created_at` (giữ ghế hiệu lực trong `[created_at, expires_at)`). `currency = 'VND'`; `total_amount > 0`. Trạng thái gồm `pending`, `confirmed`, `expired`, `cancelled`.

Lưu ý: việc `expires_at` đã qua không tự động chuyển booking sang `expired` ở mức database; partial unique index trên `booking_detail` chỉ dựa trên `allocation_status`, không chứa điều kiện thời gian và không dùng `now()`. Việc hết hạn là trách nhiệm của backend/worker (xem mục 9.3): chuyển `booking.status` sang `expired`, chuyển các detail liên quan sang `released` và ghi `released_at` trong một transaction. Nếu worker chậm hoặc ngừng hoạt động, request giữ ghế mới phải xử lý sách hết hạn các booking `pending` đã quá `expires_at` liên quan đến cùng suất trong cùng transaction trước khi tạo phân bổ mới — cách này bảo đảm hai request đồng thời không thể giữ/bán cùng một ghế (được chặn bởi partial unique index) mà không phụ thuộc vào việc worker chạy đúng nhịp.

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
UQ-C: (booking_id, showtime_id, cinema_room_id) — đã chứa PK ghép qua FK tới booking; ràng buộc duy nhất kỹ thuật phục vụ FK ghép, không phải candidate key tối thiểu.
UQ-P: (showtime_id, seat_id)
      WHERE allocation_status IN ('held', 'sold')
```

`(booking_id, showtime_id, cinema_room_id)` là composite unique constraint phục vụ FK ghép từ `booking_detail` lên `booking`, không phải candidate key (đã chứa `booking_id` và không phải siêu khóa tối thiểu). Hai FK ghép buộc detail thuộc đúng booking, suất, phòng và ghế. Hai ràng buộc duy nhất trên `booking_detail` có ý nghĩa khác nhau:

- AK `(booking_id, seat_id)` — duy nhất toàn phần, không điều kiện: ngăn cùng một booking có nhiều detail cho cùng một ghế, kể cả các detail cũ đã `released` (lịch sử của cùng booking vẫn giữ một ghế duy nhất một hàng).
- UQ-P `(showtime_id, seat_id) WHERE allocation_status IN ('held', 'sold')` — duy nhất có điều kiện, áp dụng chéo booking: ngăn nhiều booking có phân bổ hiệu lực cho cùng một ghế trong cùng suất chiếu. `released` không còn là phân bổ hiệu lực nên không bị chặn bởi UQ-P, nhưng bản ghi lịch sử vẫn được giữ trong relation.

UQ-P không tự giải phóng ghế theo thời gian — partial index không chứa điều kiện phụ thuộc `now()`; việc giải phóng là trách nhiệm worker/transaction (mục 9.3). Trạng thái phân bổ gồm `held`, `sold`, `released`.

Giá của một detail được backend tính trong transaction tạo booking, dựa trên các giá trị hiện hành tại thời điểm đó; client không được gửi giá cuối cùng hay mức giảm. Quy trình tạo booking và snapshot giá:

1. Backend kiểm tra trạng thái suất chiếu (không `cancelled`, còn bán theo `booking_closes_at` nếu có), trạng thái ghế, `ticket_type` đang `is_active` và điều kiện áp dụng loại vé.
2. Backend đọc giá có hiệu lực tại thời điểm tạo booking: `showtime.base_price`, `seat_type.surcharge_amount`, `capacity_units` của ghế và `discount_percent` của `ticket_type` — không tin dữ liệu giá do client gửi.
3. Backend tạo snapshot tương ứng vào `booking_detail` (các cột `*_snapshot`, `base_amount`, `surcharge_amount`, `discount_amount`, `unit_price`) trong cùng transaction với booking.
4. Backend tính `booking.total_amount` từ các detail vừa tạo và lưu vào booking trong cùng transaction.
5. Sau khi tạo, giá snapshot và tổng tiền là giá trị cuối cùng đã chốt, không phải giá tham chiếu để tính lại sau; chúng không tự thay đổi khi danh mục, giá hiện hành hay `discount_percent` của `ticket_type`/`seat_type` thay đổi.

Công thức tính giá của một detail:

```text
base_amount = showtime.base_price * capacity_units_snapshot
surcharge_amount = seat_type.surcharge_amount (không nhân theo capacity_units)
discount_amount = round(base_amount * discount_percent_snapshot / 100, 0)
unit_price = base_amount + surcharge_amount - discount_amount
```

Lưu ý công thức: giảm giá chỉ áp dụng trên `base_amount`, không áp dụng trên `surcharge_amount`; phụ thu ghế được tính một lần cho mỗi `booking_detail`, kể cả ghế Couple. Đối với ghế Couple (`capacity_units = 2`), `base_amount` được nhân hai vì một detail đại diện cho toàn bộ ghế đôi; một ticket duy nhất đại diện cho quyền sử dụng toàn bộ ghế đôi và soát vé xác nhận quyền này một lần. `discount_percent_snapshot` là mức giảm của từng loại vé được áp dụng cho detail; các detail trong cùng booking có thể dùng các loại vé khác nhau và backend tự xác minh điều kiện áp dụng.

Miền giá trị bắt buộc (CHECK): `base_amount > 0`; `surcharge_amount >= 0`; `discount_percent_snapshot` nằm trong `[0, 100]`; `0 <= discount_amount <= base_amount`; `0 <= unit_price` và `unit_price = base_amount + surcharge_amount - discount_amount`; `booking.total_amount > 0`. Tiền VND dùng số nguyên đồng (integer), làm tròn bằng `round(..., 0)` theo quy tắc round half up áp dụng cho `discount_amount`.

Quyết định OQ-06 (đã chốt — Option B, APPROVED): `unit_price >= 0` — một detail được phép giá 0 (khi `discount_percent_snapshot = 100` và `surcharge_amount = 0`), nhưng `booking.total_amount > 0` chặn booking gồm toàn vé giá 0; `payment.amount > 0` và MVP không tạo payment attempt số tiền 0. Hỗ trợ mức database cho detail giá 0 không đồng nghĩa triển khai luồng vé miễn phí/100% giảm hay luồng xác nhận booking tổng 0 — các luồng đó nằm ngoài phạm vi MVP. Nếu nghiệp vụ sau này cần vé miễn phí hoàn chỉnh, cập nhật theo phương án C của OQ-06 (cho phép booking tổng 0 với luồng xác nhận riêng).

Điểm cần xác nhận về vé miễn phí — **đã chốt (OQ-06, Option B — APPROVED):** miền giá trị cho phép `discount_percent_snapshot = 100`, tức `discount_amount = base_amount`. Khi `surcharge_amount = 0`, `unit_price = 0` (detail miễn phí); khi phụ thu dương, `unit_price = surcharge_amount > 0`. Booking tổng 0 (chỉ gồm vé miễn phí) bị chặn bởi `booking.total_amount > 0` và nằm ngoài phạm vi MVP; MVP không xây luồng xác nhận không-thanh-toán. Một booking có thể chứa detail giá 0 miễn là tổng của booking vẫn dương. Nếu nghiệp vụ sau này cấm hẳn vé giá 0, cần bổ sung CHECK `discount_percent_snapshot < 100` hoặc yêu cầu `unit_price > 0` cho từng detail.

`booking.total_amount` bằng tổng `unit_price` của toàn bộ detail gốc của booking — được tính một lần trong transaction tạo booking, không tính lại dựa trên các detail còn `held` hoặc `sold` sau khi chúng chuyển trạng thái. `booking.total_amount`, `payment.amount` và toàn bộ snapshot giá là bất biến sau khi tạo; chúng không thay đổi khi danh mục, giá hiện hành hay `discount_percent` của `ticket_type`/`seat_type` thay đổi. Mọi payment attempt được khởi tạo với `amount` và `currency` đúng bằng `booking.total_amount` và `booking.currency` tại thời điểm tạo payment (số tiền và tiền tệ của booking không đổi nên mọi attempt dùng cùng giá trị này). Việc bảo đảm nhất quán tổng tiền khi ghi dữ liệu là trách nhiệm của application transaction tạo booking (MVP không thêm trigger tính tổng; CHECK domain constraint chỉ bảo vệ miền giá trị).

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
UQ-P: (provider, provider_transaction_id) WHERE provider_transaction_id IS NOT NULL
UQ-P: booking_id WHERE status IN ('created', 'pending', 'unknown')
UQ-P: booking_id WHERE applied_at IS NOT NULL
```

Một booking có thể có nhiều payment attempt và thậm chí nhiều attempt có thể đạt `succeeded`, nhưng chỉ một payment được áp dụng (`applied_at` đặt) để xác nhận booking và phát hành vé. `(provider, provider_transaction_id)` là partial unique index có điều kiện khi giá trị khác NULL, không phải candidate key toàn relation. UQ-P thứ nhất bảo đảm một booking không có nhiều attempt đang mở (`created`/`pending`/`unknown`); UQ-P thứ hai bảo đảm nhiều nhất một payment được áp dụng cho booking.

Phạm vi bảo vệ của UQ-P payment đã áp dụng: chỉ bảo đảm **tối đa một payment được áp dụng cho mỗi booking** (`applied_at` không NULL), không bảo đảm payment đó đã `succeeded`, đã xác minh hay khớp số tiền/tiền tệ — các điều kiện này là transaction/service invariant (xem bên dưới), database không tự thực thi qua UQ-P. UQ-P không thay thế các kiểm tra trạng thái và nghiệp vụ khi áp dụng payment.

**Invariant áp dụng Payment** (thực thi bởi service trong cùng một transaction; UQ-P chỉ là lớp bảo vệ cuối cùng cho tính duy nhất):

1. Payment phải thuộc booking tương ứng (bảo đảm trực tiếp bởi FK `payment.booking_id`).
2. Payment phải có kết quả thành công đã được xác minh (`status = 'succeeded'` và `verified_at` đã đặt).
3. `payment.amount = booking.total_amount`.
4. `payment.currency = booking.currency`.
5. Booking vẫn đủ điều kiện được xác nhận tại thời điểm áp dụng: `booking.status = 'pending'`; không được tự động xác nhận booking đã `expired` hoặc `cancelled`.
6. Việc đặt `applied_at`, chuyển booking `pending -> confirmed`, chuyển toàn bộ detail `held -> sold` và tạo ticket phải nguyên tử trong cùng một transaction; nếu một phần thất bại, toàn bộ rollback.
7. Nếu bất kỳ điều kiện nào trong số 1–5 không thỏa mãn, không được áp dụng payment một phần (không đặt `applied_at` mà không xác nhận booking, không xác nhận booking mà không tạo đủ ticket).

Phân biệt cơ chế bảo đảm: FK và các UQ-P là **database constraints** bảo vệ tính tham chiếu và tính duy nhất (một payment thuộc đúng một booking; nhiều nhất một payment áp dụng; không có hai attempt đang mở); các điều kiện về trạng thái, số tiền, tiền tệ, tính đủ điều kiện của booking và tính nguyên tử của trình tự áp dụng là **transaction/service invariants** — không có CHECK hay index nào bảo đảm chúng.

Phân biệt ba lớp trạng thái: trạng thái do cổng thanh toán báo (được ghi nhận qua `payment_event`), trạng thái payment nội bộ (`payment.status`) và trạng thái booking (`booking.status`). Ngoài tập trạng thái đã nêu:

- `created`: attempt đã khởi tạo, chưa gửi cổng.
- `pending`: đã gửi cổng, chờ kết quả.
- `unknown`: kết quả chưa xác định chắc chắn (mất kết nối, timeout với cổng); không mặc nhiên có nghĩa thất bại.
- `succeeded`: cổng báo thành công và được xác minh.
- `failed`: cổng báo thất bại rõ ràng.
- `cancelled`: attempt bị hủy có chủ đích trước khi có kết quả.

Vòng đời payment (đường chuyển trạng thái do service điều phối, database chỉ bảo vệ miền trạng thái và các UQ-P):

```text
created -> pending -> succeeded -> (áp dụng: applied_at đặt, booking confirmed)
                    -> failed
                    -> unknown -> (đối soát cổng) -> succeeded | failed
created -> cancelled
pending/unknown -> cancelled (chỉ khi cổng xác nhận không còn giao dịch hiệu lực)
succeeded (chưa áp dụng) -> giữ lại kết quả thành công để đối soát ngoại lệ (không chuyển sang cancelled)
succeeded (đã áp dụng)  -> trạng thái chốt, không hạ về failed vì callback muộn/lặp
```

Lưu ý về `succeeded` chưa áp dụng: kết quả tài chính từ cổng đã thành công là một sự kiện tài chính không thể hủy bằng cách đổi trạng thái nội bộ. Do đó payment `succeeded` nhưng chưa áp dụng (ví dụ booking đã `expired`/`cancelled`, hoặc booking không còn đủ điều kiện xác nhận) **không được chuyển sang `cancelled`** và **không được tự động áp dụng** (không đặt `applied_at`, không xác nhận booking, không phát hành ticket, không tự động chiếm lại ghế đã được giải phóng hay phân bổ cho booking khác). Kết quả thành công phải được giữ lại nguyên trạng để truy vết và đối soát; ghi nhận kết quả ngoại lệ qua `reconciliation_status` (và `payment_event` liên quan), còn việc xử lý tài chính tiếp theo (hoàn tiền hoặc phương án khác) là workflow ngoài phạm vi demo trừ khi tài liệu nghiệp vụ hiện có quy định rõ hơn — chính sách cụ thể cần chủ dự án xác nhận.

- `unknown` không phải trạng thái kết thúc: nó phải được giải quyết bằng đối soát với cổng (chuyển về `succeeded` hoặc `failed`) trước khi booking được phép retry; trong khi còn `pending`/`unknown`, UQ-P chặn tạo attempt mở mới.
- Trạng thái payment là trạng thái nội bộ; trạng thái booking chỉ được thay đổi trong transaction áp dụng payment (mục trên); trạng thái từ cổng chỉ là dữ liệu đầu vào được ghi nhận qua `payment_event`, không tự gán trực tiếp cho `payment.status` mà phải qua bước xác minh (đúng `merchant_request_id`, số tiền, tiền tệ).

Quy tắc xử lý:

- Khi payment đang `pending` hoặc `unknown`, hệ thống phải đối soát với cổng trước khi cho phép retry theo chính sách; không tạo attempt mới đè lên attempt đang mở (UQ-P chặn ở mức database).
- Chỉ payment đã xác minh (`verified_at` đặt), đúng số tiền (`amount` = `booking.total_amount`) và tiền tệ (`currency` = `booking.currency`), trạng thái `succeeded` và đủ điều kiện nghiệp vụ mới được áp dụng.
- Nếu nhiều attempt cùng thành công, chỉ một payment được áp dụng (UQ-P bảo đảm); các attempt `succeeded` còn lại **giữ nguyên trạng thái `succeeded`** (không tự chuyển thành `failed` hay `cancelled`) và được ghi nhận trạng thái để đối soát và xử lý ngoại lệ với cổng (hoàn tiền nằm ngoài phạm vi MVP, nhưng dữ liệu phải giữ lại để truy vết).
- Callback lặp không được tạo thêm ticket hoặc áp dụng payment lần thứ hai: việc xác minh được nhận diện qua `payment_event(payment_id, event_key)` và trạng thái payment/booking đã chốt.
- Payment đã áp dụng không được chuyển thành thất bại chỉ vì callback đến muộn hoặc bị lặp; các sự kiện muộn chỉ được ghi nhận vào `payment_event` để đối soát.
- Xác nhận booking (`pending -> confirmed`), cập nhật toàn bộ detail sang `sold`, đặt `payment.applied_at` và tạo ticket phải xử lý nguyên tử trong một transaction duy nhất; nếu một phần thất bại, toàn bộ rollback.

`reconciliation_status`, `next_reconcile_at` phục vụ worker đối soát với cổng; workflow đối soát phức tạp ngoài phạm vi MVP nhưng trạng thái được mô hình hóa từ đầu.

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

Khóa `(payment_id, event_key)` là ràng buộc duy nhất chống xử lý lặp cùng một callback/sự kiện; cùng event_key đến lần thứ hai được nhận diện và bỏ qua xử lý (idempotent), không tạo thêm ticket hay áp dụng payment lần hai. `payload_summary` chỉ chứa dữ liệu đã lọc phục vụ tích hợp với cổng, không chứa secret hoặc thông tin thẻ; nó không thay thế các cột nghiệp vụ cốt lõi (tiền, trạng thái, định danh). `source` thuộc `webhook`, `reconcile`; `status` thuộc `received`, `processed`, `failed`.

Ranh giới hai transaction trong vòng đời event:

1. **Transaction tiếp nhận** — chỉ lưu event mới với `status = 'received'` (và trả lời đã nhận cho cổng), không cập nhật nghiệp vụ. Nếu cùng `(payment_id, event_key)` đã tồn tại, nhận diện trùng và bỏ qua, không tạo hàng thứ hai. Mục đích: không mất callback trước khi xử lý được.
2. **Transaction xử lý nghiệp vụ** — xác minh kết quả, cập nhật Payment/Booking/BookingDetail/Ticket theo invariant 3.16 và đánh dấu `processed` (đặt `processed_at`) trong cùng transaction đó. Nếu transaction này lỗi, event vẫn tồn tại ở `received` để worker retry; khi xử lý lỗi tạm thời thì tăng `retry_count`, đặt `next_retry_at` và ghi `last_error`, worker retry đến khi `processed` hoặc chuyển `failed` để đối soát thủ công.

Việc lưu event ban đầu và toàn bộ xử lý nghiệp vụ **không** bắt buộc nằm trong cùng một transaction — tách hai transaction là thiết kế chủ đích để có retry bền vững (event không bị mất nếu xử lý chưa chạy hoặc lỗi). Điểm bất biến cần bảo đảm: cập nhật nghiệp vụ và việc đánh dấu `processed` cùng một event phải trong cùng transaction xử lý; event không được đánh dấu `processed` trong khi các cập nhật nghiệp vụ chưa commit.

Ba tình huống xử lý callback/idempotency:

**Tình huống 1 — Callback trùng sau khi sự kiện đã xử lý:** unique constraint `(payment_id, event_key)` từ chối event record thứ hai cho cùng cặp khóa; event trùng được nhận diện và bỏ qua. Không áp dụng payment lần hai, không phát hành ticket trùng.

**Tình huống 2 — Callback trùng trong lúc sự kiện đầu tiên đang xử lý:** hai worker/request không được đồng thời xử lý cùng một event theo cách gây cập nhật nghiệp vụ trùng. Service phải có cơ chế claim hoặc tuần tự hóa xử lý sự kiện (ví dụ khóa dòng event theo điều kiện `status`, hoặc khóa bởi một worker duy nhất). Cơ chế vật lý cụ thể (locking, advisory lock, cấu trúc queue) được để lại cho `database-schema.md` và phần triển khai, nhưng invariant sau phải được bảo đảm: cùng một `(payment_id, event_key)` chỉ được xử lý nghiệp vụ một lần thành công; các lần truy cập đồng thời chỉ có một lần dẫn đến cập nhật nghiệp vụ.

**Tình huống 3 — Xử lý nghiệp vụ thất bại giữa chừng:** việc cập nhật Payment, Booking, BookingDetail, Ticket và đánh dấu event `processed` phải nguyên tử khi thuộc cùng một transaction xử lý: nếu transaction thất bại/rollback, event không được đánh dấu `processed` trong khi cập nhật nghiệp vụ chưa commit. Phân biệt: `payment_event.status = failed` là trạng thái xử lý sự kiện (thất bại khi xử lý callback/đối soát), không phải `payment.status = failed` (kết quả tài chính của payment do cổng báo). Retry xử lý event là idempotent — nhờ nhận diện qua `(payment_id, event_key)` và các điều kiện áp dụng payment ở 3.16, retry không tạo thêm ticket và không áp dụng payment lần hai.

Ghi chú: unique constraint chỉ bảo đảm tính duy nhất của event record; nó không đủ để chống mọi race condition trong xử lý đồng thời — các invariant tuần tự hóa, claim và tính nguyên tử của transaction là trách nhiệm của service/worker như mô tả ở ba tình huống trên.

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

Về định danh hoàn tiền, phân biệt hai loại mã:

- `merchant_refund_id` — khóa nghiệp vụ do backend sinh cho lần yêu cầu hoàn, duy nhất toàn phần (AK), dùng cho idempotency khi gửi yêu cầu hoàn lên cổng.
- `provider_refund_id` — mã giao dịch refund do provider cấp sau khi xử lý, nullable vì chỉ tồn tại sau khi provider phản hồi. Yêu cầu nghiệp vụ là không có hai refund của cùng provider mang cùng một `provider_refund_id` (tương tự UQ-P `(provider, provider_transaction_id)` của `payment`).

Hạn chế của mô hình hiện tại: PostgreSQL không thể tạo unique index trên `refund` tham chiếu trực tiếp cột `payment.provider` qua join, còn `refund` không có cột `provider` riêng. Do đó tính duy nhất của `provider_refund_id` theo provider là **yêu cầu nghiệp vụ nhưng chưa được bảo đảm trực tiếp bằng index trong schema hiện tại** — nó phải được kiểm tra bởi service khi ghi dữ liệu refund (refund ngoài phạm vi demo nên rủi ro thấp trong MVP).

Điểm cần xác nhận (khi thiết kế schema vật lý): nếu muốn bảo đảm bằng index, cần cân nhắc lưu thêm `provider` trên `refund` kèm cơ chế bảo đảm nhất quán `refund.provider = payment.provider` (CHECK/trigger/service), hoặc chấp nhận kiểm tra ở tầng service. Trong phạm vi MVP hiện tại, mô hình không thêm thuộc tính mới và để lại quyết định này cho bước thiết kế schema. Vì refund ngoài phạm vi demo, mô hình không mở rộng thành workflow refund hoàn chỉnh.

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

Phân biệt cơ chế bảo đảm:

- Cardinality được bảo đảm trực tiếp bằng PK/FK/UNIQUE: Movie-Genre, AppUser-Role (qua PK ghép của relation nối), BookingDetail-Ticket (UQ), Payment-Refund (UQ), Booking-Payment attempt đang mở (UQ-P).
- Cardinality phụ thuộc transaction hoặc service: Booking-BookingDetail (tối thiểu một detail — FK không tự bảo đảm booking có ít nhất một detail; quy tắc được thực thi trong transaction tạo booking all-or-nothing), Seat-BookingDetail hiệu lực trong một suất (UQ-P chặn hàng thứ hai nhưng việc release đúng thời điểm thuộc worker), Booking-Payment được áp dụng tối đa một (UQ-P chặn, nhưng trình tự nghiệp vụ do service điều phối), AppUser-Ticket và AppUser-Refund tùy chọn theo nghiệp vụ.

## 7. Candidate key và quy tắc duy nhất

| Relation | Candidate/Alternate Key | Ràng buộc duy nhất khác (UQ-C/UQ-P) | Ý nghĩa |
|---|---|---|---|
| `genre` | `code`; tên chuẩn hóa | — | Mã và tên thể loại không trùng. |
| `movie_genre` | `(movie_id, genre_id)` (PK) | — | Một cặp phim-thể loại chỉ xuất hiện một lần. |
| `cinema` | `code` | — | Mã chi nhánh toàn hệ thống. |
| `cinema_room` | `(cinema_id, code)`; tên chuẩn hóa trong cinema | — | Mã/tên phòng duy nhất trong chi nhánh. |
| `seat_type` | `code`; tên chuẩn hóa | — | Danh mục loại ghế. |
| `seat` | `(cinema_room_id, row_label, seat_number)` | UQ-C `(id, cinema_room_id)` | Vị trí ghế duy nhất trong phòng; UQ-C làm đích FK ghép. |
| `ticket_type` | `code`; tên chuẩn hóa | — | Danh mục loại vé. |
| `showtime` | — (không có alternate key nghiệp vụ) | UQ-C `(id, cinema_room_id)`; exclusion chống chồng lịch | UQ-C làm đích FK ghép. |
| `app_user` | email chuẩn hóa | — | Một email đăng nhập cho một tài khoản. |
| `role` | `code` | — | Mã role dùng trong policy. |
| `user_role` | `(user_id, role_id)` (PK) | — | Không cấp trùng role. |
| `booking` | `booking_code`; `(user_id, request_key)` | UQ-C `(id, showtime_id, cinema_room_id)` | Tra cứu công khai, idempotency; UQ-C làm đích FK ghép. |
| `booking_detail` | `(booking_id, seat_id)` | UQ-C `(booking_id, showtime_id, cinema_room_id)`; UQ-P `(showtime_id, seat_id) WHERE allocation_status IN ('held','sold')` | AK toàn phần: một ghế không lặp trong cùng booking, kể cả detail đã `released`. UQ-P có điều kiện: chống giữ/bán trùng ghế trong cùng suất (không phải candidate key, không tự giải phóng ghế theo thời gian). |
| `ticket` | `booking_detail_id`; `ticket_code` | — | Một vé/detail và mã vé duy nhất. |
| `payment` | `merchant_request_id` | UQ-P `(provider, provider_transaction_id) WHERE ... IS NOT NULL`; UQ-P `booking_id` đang mở; UQ-P `booking_id` đã áp dụng | Idempotency; định danh giao dịch cổng có điều kiện. |
| `payment_event` | `(payment_id, event_key)` | — | Chống nhận/xử lý trùng sự kiện. |
| `refund` | `payment_id`; `merchant_refund_id` | — (duy nhất theo provider của `provider_refund_id` là yêu cầu nghiệp vụ, chưa được bảo đảm trực tiếp bằng index trong schema hiện tại — xem 3.18) | Một yêu cầu hoàn toàn bộ/payment và idempotency hoàn. |

Ghi chú phân loại:

- `UQ-C` là composite unique constraint kỹ thuật làm đích cho FK ghép trong PostgreSQL; mỗi tổ hợp đã chứa PK nên không phải siêu khóa tối thiểu, không phải candidate key và không thay đổi định danh nghiệp vụ của entity.
- `UQ-P` là partial unique index có điều kiện; chúng thực thi quy tắc nghiệp vụ quan trọng (ghế hiệu lực, attempt đang mở, payment đã áp dụng) nhưng không phải candidate key của toàn relation.
- Các mã công khai (`booking_code`, `ticket_code`, UUID trong URL) chỉ dùng để tra cứu; authorization không được suy ra từ việc biết các mã này — backend luôn kiểm tra role và quyền sở hữu tài nguyên.
- Email, `code` và tên chuẩn hóa so sánh theo `lower(btrim(value))`, quy tắc so sánh xác định và bất biến.

## 8. Chuẩn hóa dữ liệu

### 8.1. First Normal Form - 1NF

- Mỗi thuộc tính chứa một giá trị nguyên tử trong miền dữ liệu của nó.
- Không lưu danh sách genre, ghế hoặc role dưới dạng chuỗi phân cách hay mảng trong entity chính.
- Quan hệ M:N được tách thành `movie_genre` và `user_role`.
- Mỗi ghế của booking là một hàng `booking_detail`, không dùng cột `seat_ids` hay `quantity` tùy ý.
- `payment_event.payload_summary` là dữ liệu bán cấu trúc (JSON) phục vụ tích hợp với các cổng thanh toán khác nhau; nó không thay thế các cột nghiệp vụ cốt lõi (tiền, trạng thái, định danh) vốn là thuộc tính riêng.

### 8.2. Second Normal Form - 2NF

- `movie_genre` chỉ chứa các thuộc tính phụ thuộc vào toàn bộ PK `(movie_id, genre_id)`; tên phim và tên genre nằm ở relation cha.
- `user_role.assigned_at` và `assigned_by_user_id` mô tả chính lần gán của cặp `(user_id, role_id)`, không phụ thuộc riêng một phần khóa.
- Các relation còn lại có PK đơn nên không phát sinh phụ thuộc bộ phận vào khóa ghép.

### 8.3. Third Normal Form - 3NF

- Thông tin Movie, Genre, Cinema, CinemaRoom, Seat, SeatType, TicketType, User và Role được tách thành relation riêng.
- `ticket` tham chiếu `booking_detail` thay vì lặp `booking_id`, `showtime_id`, `seat_id` và `ticket_type_id`.
- `payment_event` và `refund` phụ thuộc vào `payment`; không lặp dữ liệu booking/user không cần thiết.
- Sức chứa phòng được tính từ ghế thay vì lưu một `cinema_room.capacity` có thể sai lệch.
- Lưu ý: không tuyên bố 3NF nghiêm ngặt cho toàn bộ relation — snapshot trong `showtime`, `booking`, `booking_detail` và các thuộc tính dư thừa phục vụ FK ghép là ngoại lệ có chủ đích, được liệt kê đầy đủ ở 8.4.

### 8.4. Phi chuẩn hóa có kiểm soát
Mô hình không tuyên bố 3NF nghiêm ngặt ở mọi relation. Các thuộc tính dư thừa sau được giữ có chủ đích:

| Thuộc tính | Lý do | Cơ chế giữ nhất quán |
|---|---|---|
| `booking.cinema_room_id` | Cho phép FK ghép xác nhận booking thuộc đúng phòng của showtime. | FK ghép và bất biến sau khi tạo. |
| `booking_detail.showtime_id`, `cinema_room_id` | Cho phép FK ghép và UQ-P chống đặt sai/trùng ghế. | Hai FK ghép; không cho sửa sau khi tạo. |
| Snapshot phim/phòng trong `showtime` | Giữ nội dung lịch đã công bố/bán. | Chốt khi lập lịch; hạn chế sửa sau khi có booking. |
| Snapshot ghế, loại vé và giá trong `booking_detail` | Giá trị cuối cùng đã chốt tại thời điểm đặt (không phải giá tham chiếu để tính lại). | Backend kiểm tra điều kiện, đọc giá hiệu lực, ghi snapshot và tính tổng trong cùng transaction tạo booking; client không được tự ghi; bất biến sau khi tạo. |
| `booking.total_amount` | Số tiền ổn định để gửi cổng và đối soát. | Tính một lần từ các detail vừa tạo trong cùng transaction tạo booking; không tính lại từ danh mục hay từ các detail còn `held`/`sold`. |
| `payment.amount` | Ghi số tiền yêu cầu của từng attempt. | Bằng tổng booking khi khởi tạo; bất biến. |

## 9. Ràng buộc toàn vẹn logic

### 9.1. Entity và referential integrity

- Mọi PK duy nhất và không NULL.
- Mọi FK bắt buộc phải tham chiếu bản ghi cha tồn tại.
- Các FK ghép không có thành phần NULL.
- Dữ liệu lịch sử nghiệp vụ không bị mất bởi cascade delete.

### 9.2. Domain integrity

- Các cột số lượng/thời lượng dương; phụ thu và số tiền giảm không âm (`0 <= discount_amount <= base_amount`, cho phép giảm 100% trên một detail — OQ-06 Option B đã chốt).
- `discount_percent` nằm trong `[0, 100]`.
- Giá trị VND dùng số nguyên đồng; booking/payment dùng `currency = 'VND'` trong MVP.
- Mọi cột trạng thái chỉ nhận giá trị trong tập đã quy định.
- Chuỗi bắt buộc sau khi trim không được rỗng.
- Mốc thời gian lưu theo UTC; giao diện chuyển sang `Asia/Ho_Chi_Minh` khi hiển thị.

### 9.3. Business integrity và concurrency

Ba lớp ràng buộc được phân biệt rõ:

1. **Database constraints** (thực thi trực tiếp bởi PostgreSQL): PK, FK, CHECK domain, unique, partial unique index, composite unique constraint phục vụ FK ghép và exclusion constraint chống chồng lịch phòng.
2. **Transaction/service invariants** (thực thi bởi application trong một transaction): tính tổng tiền, điều kiện chuyển trạng thái, tính nguyên tử của tạo booking và phát hành vé.
3. **Nghiệp vụ vận hành** (service/worker/ngoài phạm vi MVP): worker hết hạn, đối soát cổng, retry, thông báo.

Các quy tắc cụ thể:

- Một ghế chỉ có tối đa một `booking_detail` đang `held` hoặc `sold` trong cùng showtime — bảo đảm trực tiếp bằng partial unique index `(showtime_id, seat_id) WHERE allocation_status IN ('held','sold')`.
- Hai showtime không được chồng khoảng `[starts_at, room_available_at)` trong cùng phòng, trừ showtime `cancelled` — bảo đảm trực tiếp bằng exclusion constraint (mục 3.9).
- `seat.status` (trạng thái vật lý), `booking.status` (trạng thái đơn), `booking_detail.allocation_status` (trạng thái phân bổ trong suất) và `ticket.status` (trạng thái vé) là bốn trạng thái độc lập; không dùng một trạng thái thay thế trạng thái khác.
- Tạo booking và toàn bộ detail là thao tác all-or-nothing trong một transaction; booking phải có ít nhất một detail (invariant của transaction — FK không tự bảo đảm điều này). Trong cùng transaction, backend kiểm tra điều kiện, đọc giá hiệu lực, ghi snapshot vào detail và tính `total_amount` (quy trình 5 bước ở 3.14); client không được quyết định giá cuối cùng hay mức giảm. Booking tổng 0 (chỉ gồm vé giá 0) bị chặn bởi `booking.total_amount > 0` và nằm ngoài phạm vi MVP theo OQ-06 Option B; CHECK chỉ bảo vệ miền `unit_price >= 0`.
- Booking `pending` tương ứng mọi detail `held`; `confirmed` tương ứng `sold`; `expired/cancelled` tương ứng `released` (invariant của service; database không tự thực thi tương ứng trạng thái booking-detail).
- Thời gian giữ ghế 5 phút xác định bởi `[booking.created_at, booking.expires_at)`. Partial unique index chỉ dựa trên `allocation_status`, không chứa điều kiện `now()` và không tự giải phóng ghế theo thời gian. Worker có trách nhiệm chuyển booking hết hạn sang `expired`, detail sang `released` và cập nhật `released_at` — chỉ khi booking quá hạn và không còn payment attempt `created`/`pending`/`unknown` cần giữ để xử lý (điều kiện hai phần theo `database-design.md` mục 31.5/34.1 và BR-BOOK-004); thời hạn tối đa của cửa sổ chờ do attempt đang mở gây ra chưa được chốt (OQ-07 — OPEN). Trước khi giữ ghế mới, hệ thống phải xử lý hết hạn các booking `pending` đã quá `expires_at` liên quan trong cùng transaction, bảo đảm khi worker chậm hoặc ngừng hoạt động request đặt ghế mới vẫn không phá vỡ tính toàn vẹn.
- Hai request đồng thời không thể giữ hoặc bán cùng một ghế trong cùng suất: partial unique index từ chối hàng phân bổ hiệu lực thứ hai; race giữa giải phóng (release) và giữ mới được tuần tự hóa bởi transaction.
- Chỉ payment `succeeded`, đúng số tiền/tiền tệ, đã xác minh và được áp dụng mới có thể chuyển booking sang `confirmed` và phát hành ticket.
- Xác nhận booking, chuyển toàn bộ detail sang `sold`, đặt `payment.applied_at` và tạo ticket phải nằm trong cùng transaction (nguyên tử; callback lặp không tạo thêm ticket). Invariant áp dụng payment đầy đủ tại 3.16: UQ-P `booking_id WHERE applied_at IS NOT NULL` chỉ bảo đảm tối đa một payment được áp dụng cho booking; các điều kiện trạng thái, số tiền, tiền tệ và tính đủ điều kiện của booking là invariant của transaction/service, không được thay thế bằng UQ-P.
- Payment `succeeded` đến muộn sau khi booking đã `expired`/`cancelled` hoặc không còn đủ điều kiện xác nhận: không được tự động áp dụng (`applied_at` không đặt), không xác nhận booking, không phát hành ticket và không tự động chiếm lại ghế đã giải phóng; kết quả thành công được giữ nguyên trạng để đối soát qua `reconciliation_status` (xem 3.16).
- `payment_event.status = failed` là trạng thái xử lý sự kiện (callback/đối soát), không phải `payment.status = failed` (kết quả tài chính của payment); hai trạng thái độc lập và không được suy lẫn nhau (xem 3.17).
- Chỉ một lần soát vé có thể chuyển `ticket.valid -> used` thành công (conditional update: chỉ update khi status hiện tại là `valid`; ghế Couple được soát một lần cho toàn bộ ghế đôi).
- User `locked` hoặc `deactivated` không được tiếp tục truy cập theo chính sách xác thực; role `inactive` không cấp quyền mới; bản ghi `user_role` còn tồn tại không mặc nhiên nghĩa là quyền vẫn hợp lệ — authorization phải kết hợp `app_user.status`, `role.is_active` và tập role hiệu lực tại thời điểm kiểm tra, thực hiện ở backend. Đăng ký công khai chỉ cấp role `CUSTOMER`; việc cấp/thu hồi role quản trị được backend kiểm soát. JWT/access token đã phát hành giữ hiệu lực đến khi hết hạn trừ khi có cơ chế thu hồi; thay đổi role/status có thể không phản ánh ngay vào token đang hiệu lực — đây là giới hạn được chấp nhận trong MVP (không thêm relation quản lý token). Authorization được thực hiện ở backend, không dựa vào giao diện hay việc người dùng biết UUID/mã công khai.
- Thay đổi suất chiếu trong MVP: cho phép sửa giờ/phòng khi suất chưa có booking và không vi phạm ràng buộc lịch (exclusion constraint vẫn thực thi). Suất đã có booking `confirmed` không được đổi `movie_id`, giờ hoặc phòng trong MVP; suất có booking `pending` chỉ được xử lý sau khi các booking đó hết hạn hoặc bị hủy. Hủy suất phải lưu trạng thái (`cancelled`), thời điểm (`cancelled_at`) và lý do (`cancellation_reason`); khôi phục suất đã hủy phải kiểm tra lại xung đột lịch phòng trong transaction (mục 3.9). Khi rạp hủy suất: (1) dữ liệu lịch sử — `booking`, `booking_detail`, `ticket`, `payment` liên quan không bị xóa, giữ nguyên trạng thái để truy vết; (2) workflow tự động thông báo khách, đổi lịch hoặc hoàn tiền khi rạp hủy suất chưa được triển khai trong demo (ngoài phạm vi demo, không phải quyết định nghiệp vụ); (3) chính sách về quyền lợi của khách đã thanh toán (hoàn tiền, đổi vé hoặc phương án khác) phải được chủ dự án xác nhận ở tầng nghiệp vụ — việc chưa triển khai workflow không có nghĩa là nghiệp vụ đã quyết định không hoàn tiền, và mô hình này không tự kết luận chính sách đó.
- Xóa danh mục (movie, genre, seat_type, ticket_type, cinema, cinema_room, role...): ưu tiên soft-delete/chuyển trạng thái ngừng hoạt động (`archived`, `is_active = false`, `status = 'inactive'`) cho danh mục đã được sử dụng. Ngừng hoạt động không đồng nghĩa xóa bản ghi. `booking`, `booking_detail`, `ticket`, `payment` và `payment_event` không bị xóa dây chuyền; mặc định `ON DELETE RESTRICT` cho quan hệ lịch sử và tài chính. Chỉ cho phép xóa vật lý khi không vi phạm FK và chính sách lưu trữ. Không bổ sung audit-log relation nếu chưa có yêu cầu cụ thể.

## 10. Giả định và quyết định thiết kế

- MVP vận hành một cinema nhưng giữ relation `cinema` để sẵn sàng mở rộng nhiều chi nhánh.
- Booking giữ ghế 5 phút kể từ `booking.created_at`.
- Không hard-code cutoff 10/15/30 phút trước giờ chiếu; `booking_closes_at` là nullable và chính sách cuối cùng còn cần chốt.
- Ghế Couple được chốt là một đơn vị bán: `capacity_units = 2`, một `booking_detail` và một ticket duy nhất đại diện cho quyền sử dụng toàn bộ ghế đôi (không tạo hai ticket); giá cơ sở nhân hai, cộng phụ thu ghế một lần, áp dụng giảm giá của loại vé; soát vé xác nhận quyền ghế đôi một lần.
- Một booking có thể chứa nhiều `booking_detail` với các loại vé khác nhau (mỗi detail một ghế, đúng một `ticket_type_id`); backend xác minh điều kiện áp dụng loại vé và tự tính giảm giá; mỗi ghế chỉ có một loại vé trong MVP; không hỗ trợ tách thanh toán theo từng detail.
- `payment.unknown` biểu thị kết quả chưa xác định; khi `pending`/`unknown` phải đối soát cổng trước khi retry. Nếu nhiều attempt cùng thành công, chỉ một được áp dụng; các attempt còn lại ghi nhận để đối soát.
- Customer cancellation sau thanh toán, workflow hủy suất bởi rạp (thông báo/đổi lịch/hoàn tiền tự động), reconciliation phức tạp là ngoài phạm vi demo. Chính sách quyền lợi khách đã thanh toán khi rạp hủy suất cần được chủ dự án xác nhận; việc chưa triển khai workflow không đồng nghĩa nghiệp vụ đã quyết định không hoàn tiền.
- Relation `refund` vẫn được mô hình hóa để giữ khả năng mở rộng, không có nghĩa workflow refund đã được triển khai.
- Authorization không được suy ra từ việc biết UUID; backend luôn kiểm tra role và quyền sở hữu tài nguyên.
- Điểm cần chủ dự án xác nhận (chưa có tài liệu yêu cầu quy định): (1) chính sách quyền lợi khách đã thanh toán khi rạp hủy suất (hoàn tiền, đổi vé hoặc phương án khác); (2) cách bảo đảm tính duy nhất của `provider_refund_id` theo provider khi thiết kế schema vật lý — hiện ghi nhận là yêu cầu nghiệp vụ chưa được bảo đảm bằng index, không thêm cột `refund.provider` trong MVP (xem 3.18). Đã chốt: chính sách vé miễn phí — OQ-06 Option B: `unit_price >= 0` cho detail, `booking.total_amount > 0`, `payment.amount > 0`; booking tổng 0 ngoài phạm vi MVP (xem 3.14).

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
| Chống trùng lịch phòng | Exclusion constraint theo phòng và `tstzrange` `[starts_at, room_available_at)` với `btree_gist`, `&&`, `WHERE status <> 'cancelled'` |
| Composite unique constraint phục vụ FK ghép | 3: `seat(id, cinema_room_id)`, `showtime(id, cinema_room_id)`, `booking(id, showtime_id, cinema_room_id)` — không được tính là candidate key |
| Giải phóng ghế hết hạn | Worker + xử lý hết hạn trong transaction tạo booking; partial index không phụ thuộc `now()` |
| Payment idempotency | `merchant_request_id`, UQ-P attempt đang mở, UQ-P payment đã áp dụng (tối đa một payment áp dụng/booking) và `payment_event(payment_id, event_key)` |
| Phát hành vé idempotent | UQ `ticket.booking_detail_id` |

## 12. Tài liệu tham chiếu

- `database/database-design.md`: nguồn thiết kế entity, thuộc tính, ràng buộc và transaction.
- `database/database-schema.md`: tài liệu schema vật lý cần được đồng bộ từ mô hình này.
- `database/database-schema.sql`: DDL PostgreSQL sẽ được tạo ở bước triển khai database.
- `docs/analysis and design/business-system-design.md`: actors, use cases và luồng nghiệp vụ.
- `docs/requirements/business-system-analysis.md`: phạm vi và yêu cầu nghiệp vụ ở mức phân tích.
