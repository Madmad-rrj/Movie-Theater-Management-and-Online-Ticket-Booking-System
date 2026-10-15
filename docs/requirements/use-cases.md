# Hệ thống quản lý và đặt vé rạp chiếu phim — Use Cases & Nghiệp vụ cốt lõi

## 1. Mục đích và phạm vi tài liệu

Tài liệu này đặc tả chi tiết các Use Case (UC) trọng yếu — những quy trình có rủi ro cao về tính toàn vẹn dữ liệu, xử lý đồng thời (concurrency) và idempotency. Các luồng CRUD/browsing cơ bản chỉ giữ ở mức danh mục (mục 3) và được đặc tả đầy đủ trong `docs/requirements/business-system-analysis.md` (FR/BR/NFR/AC) và `docs/analysis and design/business-system-design.md` (use case UC-01..UC-05 ở mức tổng quan).

Quan hệ giữa các tài liệu:

| Tài liệu | Vai trò |
| :--- | :--- |
| `docs/requirements/business-system-analysis.md` | Yêu cầu chức năng (FR), quy tắc nghiệp vụ (BR), tiêu chí nghiệm thu (AC), câu hỏi mở (OQ) — **nguồn quy tắc nghiệp vụ** |
| `docs/analysis and design/business-system-design.md` | Use case tổng quan, actors, sơ đồ luồng |
| `docs/analysis and design/relational-model.md`, `database/database-schema.md`, `database/database-schema.sql` | Mô hình quan hệ, ràng buộc vật lý — **nguồn trạng thái/cột/constraint được tham chiếu trong UC này** |
| `database/database-design.md` | Thiết kế entity, protocol khóa/transaction (mục 31–34) — nguồn cho cơ chế, không phải hành vi nghiệp vụ |
| **Tài liệu này (`use-cases.md`)** | Hành vi nghiệp vụ quan sát được của các UC trọng yếu — đầu vào cho API Spec, Backend, test case |
| API Specification, Test Cases (sẽ tạo sau) | Được dẫn xuất từ tài liệu này; không viết endpoint/test suite chi tiết tại đây |

Quy tắc nguồn sự thật: quy tắc nghiệp vụ đã chốt (OQ-06 Option B, OQ-07 Grace Period, chính sách giá, vòng đời booking/payment/ticket) được trích dẫn theo tài liệu nguồn và **không được diễn giải lại** trong file này. Mọi điểm mâu thuẫn hoặc thiếu quyết định được ghi là câu hỏi mở (mục 11).

## 2. Quy ước trạng thái (theo schema thực tế)

Tất cả trạng thái dưới đây trích đúng từ `database/database-schema.md` / `database-schema.sql`:

- `booking.status`: `pending`, `confirmed`, `expired`, `cancelled`
- `booking_detail.allocation_status`: `held`, `sold`, `released`
- `payment.status`: `created`, `pending`, `unknown`, `succeeded`, `failed`, `cancelled`
- `payment.reconciliation_status`: `none`, `needs_review`, `refund_required`, `resolved`
- `payment_event.status`: `received`, `processed`, `failed`
- `ticket.status`: `valid`, `used`, `voided`
- `showtime.status`: `draft`, `scheduled`, `in_progress`, `completed`, `cancelled`
- `refund.status`: `pending`, `unknown`, `succeeded`, `failed`

Khái niệm nghiệp vụ (không phải trạng thái DB): "quá hạn giữ ghế" (vượt `booking.expires_at`), "phân bổ hiệu lực" (`held`/`sold`), "ghế khả dụng" (chưa có phân bổ hiệu lực cho suất — suy ra, không lưu).

## 3. Danh mục Use Cases tổng thể

| Nhóm | Use Case | Phạm vi đặc tả |
| :--- | :--- | :--- |
| **Khách hàng** | Đăng ký, Đăng nhập, Xem danh sách phim, Xem suất chiếu, Xem sơ đồ ghế | CRUD/browsing — đặc tả ở `business-system-analysis.md` (FR-AUTH/MOVIE/SHOW), bỏ qua chi tiết tại đây |
| **Đặt vé** | **UC-01: Giữ ghế và tạo đơn hàng (Reserve Seats)** | **Đặc tả chi tiết tại đây** |
| | Xem lịch sử đơn hàng; Hủy đơn hàng / Hủy thanh toán để đổi ghế | Xem `business-system-analysis.md` FR-BOOK-003, BR-BOOK-008, AC-BOOK-004..006 (đã chốt, gồm luồng gọi Cancel/Query API cổng) — không lặp lại tại đây |
| | **UC-03: Giải phóng đơn hàng hết hạn (Release Expired)** | **Đặc tả chi tiết tại đây** |
| **Thanh toán** | Khởi tạo thanh toán (tạo payment attempt) | Xem FR-PAY-001, BR-PAY-001/002 — tích hợp theo cổng (OQ-01) |
| | **UC-02: Xử lý Webhook thanh toán (Process Webhook)** | **Đặc tả chi tiết tại đây** |
| | **UC-04: Đối soát payment (Reconciliation)** và **UC-04A: Hoàn tiền (Refund)** | **Đặc tả chi tiết tại đây** (tách làm hai mục vì hai vòng đời độc lập; refund ngoài phạm vi demo — chỉ mô hình dữ liệu) |
| **Vé** | **UC-05: Soát vé tại rạp (Validate Ticket)** | **Đặc tả chi tiết tại đây** |
| | Xem vé/QR, Gửi vé qua Email | Bỏ qua chi tiết (gửi sau commit, không thuộc luồng giao dịch — `database-design.md` §31.4) |
| **Quản trị** | Quản lý Phim, Rạp/Phòng, Ghế, Suất chiếu, Users & Roles | CRUD — xem FR-MGT-*, BR-ROOM-*, BR-ST-*; ranh giới quyền theo OQ-04 |

> **Lưu ý phạm vi:** UC-04A (hoàn tiền) và luồng đối soát tài chính tự động cho payment thành công đến muộn là **OUT OF SCOPE — DEMO** theo `business-system-analysis.md` §3.1. UC-04/04A được đặc tả vì mô hình dữ liệu (`refund`, `payment.reconciliation_status`) đã sẵn sàng và logic đối soát một phần (ghi nhận kết quả) thuộc phạm vi; phần **thực thi hoàn tiền** chỉ định nghĩa hợp đồng hành vi, không cam kết triển khai demo.

## 4. Quy ước đặc tả

Mỗi UC chi tiết dùng cùng một mẫu: Mục tiêu / Tác nhân / Trigger / Tiền điều kiện / Luồng chính (đánh số) / Luồng thay thế & ngoại lệ (ID ổn định A1, A2, ...) / Hậu điều kiện (thành công + thất bại) / Quy tắc nghiệp vụ liên quan / Tiêu chí nghiệm thu / UC liên quan / Câu hỏi mở. Bước gắn tiền tố **[Khách hàng]**/[Staff]/[Worker] là hành động tác nhân; không có tiền tố là hành vi hệ thống.

---

## 5. UC-01: Giữ ghế và tạo đơn hàng (Reserve Seats)

- **Mục tiêu:** Khách hàng giữ các ghế còn trống cho một suất chiếu và tạo booking `pending` để tiếp tục thanh toán.
- **Tác nhân chính:** Khách hàng (Customer).
- **Tác nhân phụ:** Backend Booking Service; PostgreSQL.
- **Trigger:** Khách hàng gửi yêu cầu đặt các ghế đã chọn (kèm loại vé tương ứng cho từng ghế) kèm `request_key`.
- **Tiền điều kiện:**
  - Khách hàng đã xác thực (đăng nhập), tài khoản `active`.
  - Yêu cầu chứa ≥ 1 ghế, ghế thuộc cùng một suất, không trùng nhau, và loại vé hợp lệ cho từng ghế (trong MVP ghế Couple dùng Adult — `database-design.md` §10).
  - Suất chiếu đang `scheduled` và chưa quá `booking_closes_at` (nếu mốc này được cấu hình; NULL = không cutoff bổ sung — BR-ST-003) **và** còn đủ thời gian giữ hợp lệ tính từ thời điểm chốt (`created_at` + 5 phút không vượt mốc đóng bán/giờ bắt đầu — `database-design.md` §12). Đây là hai điều kiện phân biệt: `booking_closes_at` là mốc cấu hình; điều kiện đủ-thời-giữ-giữ ghế là ràng buộc riêng do backend kiểm tra (không tự đặt cutoff mới — OQ-02). **Không** cho đặt khi suất `in_progress` (xem OQ-UC-01).
  - `request_fingerprint` (SHA-256 dữ liệu đặt chuẩn hóa — schema §3.13) phủ: suất chiếu, danh sách ghế + loại vé tương ứng, contact info. Danh sách ghế được chuẩn hóa thứ tự (sort) trước khi băm **[PROPOSED — chưa có quyết định nghiệp vụ; ghi OQ-UC-05]**; nếu nhóm muốn "đổi thứ tự chọn ghế = nội dung khác", bỏ bước sort.
- **Luồng chính:**
  1. [Khách hàng] Gửi yêu cầu đặt ghế kèm `request_key` (UUID do client sinh) và danh sách ghế + loại vé.
  2. Hệ thống kiểm tra xác thực và định dạng yêu cầu (ghế thuộc phòng của suất, loại vé tồn tại và `is_active`).
  3. Hệ thống kiểm tra idempotency: tra `booking` theo `(user_id, request_key)` (UNIQUE, schema §3.13). Nếu đã tồn tại: so `request_fingerprint` — giống → trả nguyên trạng kết quả booking cũ (A1); khác → từ chối (A2). Nếu chưa: bắt đầu transaction.
  4. Trong transaction: khóa dòng `showtime` theo protocol `Showtime → Booking → Payment attempts` (`database-design.md` §31.1/§31.2); sau khi khóa, đọc lại trạng thái bằng thời gian thực từ DB (`clock_timestamp()`) — không dùng dữ liệu đọc trước khi khóa.
  5. Hệ thống kiểm tra lại authoritative: suất `scheduled`, chưa quá `booking_closes_at`, chưa quá giờ bắt đầu đủ thời gian giữ; toàn bộ ghế thuộc phòng, `status = 'active'`, chưa có phân bổ hiệu lực (`held`/`sold`) cho suất này.
  6. **[Dọn dẹp inline — đường đi nhanh chỉ dùng database]** Hệ thống dọn các booking `pending` quá hạn của cùng suất nếu cần (điều kiện hai phần, CHỈ với dữ liệu DB): booking quá `expires_at` (theo `clock_timestamp()` đã đọc) **và** không còn payment `created`/`pending`/`unknown` → chuyển `expired` + detail `released` trong cùng transaction (như UC-03 bước 5). Booking còn attempt mở **bỏ qua** — không coi `pending`/`unknown` là thất bại chỉ vì quá hạn; **không gọi bất kỳ API cổng nào** trong transaction này (tránh giữ khóa trong lúc chờ mạng). Ghế của booking bị bỏ qua vẫn không khả dụng: yêu cầu hiện tại thất bại an toàn (A3/A7) và khách thử lại sau khi UC-03 worker xử lý bất đồng bộ (bao gồm bước truy vấn cổng ngoài transaction).
  7. Hệ thống khóa SHARE các dòng danh mục giá (`SeatType`, `TicketType`, `Showtime`) theo thứ tự ID, tính giá từng ghế theo công thức đã chốt (BR-BOOK-005): `base_amount = base_price × capacity_units_snapshot`; phụ thu tính một lần/detail; `discount_amount = round(base_amount × discount_percent / 100, 0)`; `unit_price = base + surcharge − discount` (CHECK schema §3.14: `unit_price >= 0`); `total_amount = Σ unit_price` phải > 0 (OQ-06 Option B). **Không tin giá do client gửi.**
  8. Hệ thống INSERT `booking` (`pending`, `created_at` = thời điểm thực sau khi có khóa, `expires_at = created_at + 5 phút`) và toàn bộ `booking_detail` (`held`, kèm snapshot ghế/loại vé/giá) trong cùng transaction. Nếu ghế vừa bị giữ/bán, partial unique index `ux_booking_detail_active_seat` từ chối → rollback toàn bộ (A3). Vận hành không phụ thuộc điều kiện `now()` trong index.
  9. Commit; trả về `booking_code`, danh sách ghế, tổng tiền, `expires_at` cho client đếm ngược (chỉ để hiển thị — NFR-USE-001).
- **Luồng thay thế và ngoại lệ:**
  - **A1 (Replay cùng key, cùng nội dung):** trả về booking đã tạo trước đó nguyên trạng (kể cả khi booking đã `expired`/`confirmed` — không tạo booking mới), kèm trạng thái hiện tại để client hiển thị.
  - **A2 (Cùng key, khác `request_fingerprint`):** từ chối yêu cầu, thông báo xung đột nội dung đặt; không âm thầm nhận dữ liệu mới (`database-design.md` §12).
  - **A3 (Mất ghế do đồng thời):** một hoặc nhiều ghế đã có phân bổ hiệu lực bởi booking khác: transaction rollback toàn bộ, không giữ một phần, trả về danh sách ghế đã bị chiếm để khách chọn lại. Hai request đồng thời cho cùng ghế: đúng một thành công (AC-BOOK-002) — phân xử cuối cùng bởi partial unique index, không phải SELECT-before-INSERT.
  - **A4 (Lỗi ghi dữ liệu giữa chừng):** rollback toàn bộ; không để lại booking không có detail hoặc ghế giữ một phần (ràng buộc "ít nhất một detail" là invariant transaction — `relational-model.md` §3.13).
  - **A5 (Suất không còn nhận đặt):** `cancelled`, `in_progress`, `completed`, hoặc đã quá `booking_closes_at`/giờ bắt đầu: từ chối với lý do rõ ràng.
  - **A6 (Idempotency xung đột do race — hai request cùng key gửi đồng thời):** một request thắng UNIQUE `(user_id, request_key)`; request kia sau rollback đọc lại booking đã commit và so fingerprint (giống → A1, khác → A2). Không tạo hai booking cùng key.
  - **A7 (Ghế vẫn bị giữ bởi booking quá hạn có attempt mở):** inline cleanup bỏ qua booking đó (xem bước 6), ghế chưa nhả: trả kết quả ghế không khả dụng, không tạo booking, không giữ một phần. Khách có thể thử lại sau; UC-03 worker sẽ xử lý booking cũ bất đồng bộ (truy vấn cổng ngoài transaction, rồi release nếu chính sách thỏa).
- **Hậu điều kiện:**
  - **Thành công:** 1 `booking` `pending` + N `booking_detail` `held` (N ≥ 1), snapshot giá chốt, `expires_at` = `created_at` + 5 phút; ghế trở thành không khả dụng cho booking khác của cùng suất.
  - **Thất bại:** không booking, không detail, không phân bổ nào bị giữ; ghế vẫn khả dụng; không có dữ liệu rác hoặc ghế bị lock sai lệch.
- **Quy tắc liên quan:** BR-BOOK-001..007, BR-BOOK-010, OQ-06 (Option B), BR-ST-003; constraint: `uq_booking_user_request_key`, `ux_booking_detail_active_seat`, `ck_booking_total_amount_positive`.
- **Tiêu chí nghiệm thu:**
  - AC-UC01-01 (thành công): request hợp lệ tạo booking + N detail `held`, tổng tiền đúng tính toán backend.
  - AC-UC01-02 (idempotent replay): gửi lại cùng key+cùng fingerprint trả cùng `booking_code`, không tăng số booking.
  - AC-UC01-03 (conflict): cùng key khác payload bị từ chối, booking cũ không bị thay đổi.
  - AC-UC01-04 (race): hai request đồng thời cùng ghế — đúng một thành công.
  - AC-UC01-05 (all-or-nothing): request chọn 1 ghế trùng + 1 ghế trống → không ghế nào bị giữ.
  - AC-UC01-06 (invalid state): đặt khi suất `cancelled`/`in_progress`/quá cutoff → từ chối.
  - AC-UC01-07 (không tin client): gửi giá/mức giảm giả từ client → giá chốt vẫn theo backend.
  - AC-UC01-08 (cleanup inline không gọi mạng): trong transaction tạo booking, với booking quá hạn còn attempt `pending`/`unknown` → KHÔNG được phát sinh gọi API cổng; booking bị bỏ qua, yêu cầu hiện tại trả "ghế không khả dụng".
  - AC-UC01-09 (payment chưa chốt không bị release trái quy tắc): booking quá hạn có payment `unknown` → vẫn `pending`/`held` sau inline cleanup (chỉ UC-03 theo chính sách mới được xử lý).
  - AC-UC01-10 (transaction thất bại không để lại dữ liệu một phần): lỗi ở bất kỳ bước nào → không tồn tại booking không có detail hoặc detail `held` không thuộc booking.
- **UC liên quan:** UC-02 (thanh toán), UC-03 (hết hạn), UC-04 (đối soát).
- **Câu hỏi mở:** OQ-UC-01 (trạng thái suất cho phép đặt — mục 11), OQ-02 (mốc `booking_closes_at` cụ thể).

---

## 6. UC-02: Xử lý Webhook thanh toán (Process Payment Webhook)

- **Mục tiêu:** Ghi nhận kết quả thanh toán từ cổng một cách idempotent; khi kết quả hợp lệ và booking còn đủ điều kiện, **trong một transaction duy nhất** áp dụng payment, xác nhận booking, phát hành vé; khi không đủ điều kiện, bảo toàn kết quả tài chính để đối soát.
- **Tác nhân chính:** Payment Gateway.
- **Tác nhân phụ:** Backend Payment Service (xác minh + ghi event), Backend Booking/Payment Worker (xử lý nghiệp vụ).
- **Trigger:** Gateway gửi webhook báo kết quả giao dịch (hoặc worker xử lý event `received` từ hàng đợi).
- **Tiền điều kiện:**
  - Tồn tại `payment` attempt với `merchant_request_id` khớp; trạng thái `created`/`pending`/`unknown` (hoặc đã `succeeded` — xem A2/A7).
  - Cổng gửi request có chữ ký/checksum theo hợp đồng cổng (OQ-01: cổng cụ thể chưa chốt; bước xác minh là middleware theo adapter cổng).
- **Phân biệt khái niệm (bắt buộc đọc):**
  - **Payment succeeded:** cổng đã thu tiền thật và backend đã xác minh (đúng định danh, số tiền, tiền tệ, chữ ký) — `payment.status = 'succeeded'` + `paid_at`/`verified_at`/`received_amount`/`received_currency`/`provider_transaction_id`.
  - **Payment applied:** payment `succeeded` được gắn vào booking hợp lệ — `applied_at` đặt (tối đa một payment applied/booking — `ux_payment_applied_booking`).
  - **Booking confirmed:** `pending → confirmed` + `confirmed_at` — chỉ xảy ra trong cùng transaction với applied.
  - **Tickets issued:** phát hành đúng `capacity_units_snapshot` vé/detail (1 ghế thường, 2 ghế Couple) — cùng transaction trên.
  - Succeeded **không** kéo theo applied: payment thành công trên booking đã hết hạn/hủy **không được** tự áp dụng (FR-PAY-005, BR-PAY-007). Webhook và DB cập nhật **không** tạo thành một transaction nguyên tử xuyên qua cổng — chỉ các cập nhật DB bên dưới là nguyên tử.
- **Luồng chính (hai transaction tách biệt — BR-PAY-005/006):**
  1. **[Transaction 1 — tiếp nhận]** Hệ thống xác minh nguồn/chữ ký ở ingress (middleware theo adapter cổng) trước khi ghi nhận là event hợp lệ; lưu event vào `payment_event` (`source='webhook'`, `status='received'`, `event_key`, `event_type`, `payload_summary` đã lọc — không chứa secret/dữ liệu thẻ) trong một transaction ngắn và commit **trước khi** ACK cổng. Trùng `(payment_id, event_key)` → nhận diện, bỏ qua tạo hàng thứ hai, trả ACK theo hợp đồng cổng (A2). **Kiến trúc xác minh (xác minh một lần ở ingress, không xác minh lại ở worker):** event chỉ được ghi khi chữ ký hợp lệ; event không hợp lệ không đi vào hàng xử lý (A1). `payload_summary` (jsonb, schema §3.17) KHÔNG có cột lưu trạng thái/chứng cứ xác minh riêng — schema hiện tại không hỗ trợ xác minh lại chữ ký từ dữ liệu đã lưu; nếu cần là câu hỏi thiết kế (OQ-UC-06). Worker **không** xác minh lại chữ ký từ `payload_summary`; worker chỉ thực hiện xác minh nghiệp vụ (định danh/số tiền/tiền tệ đối chiếu với payment và cột nghiệp vụ đã lưu).
  2. **[Worker — Transaction 2 — xử lý nghiệp vụ]** Worker claim event bằng khóa dòng (`SELECT ... FOR UPDATE` / `SKIP LOCKED` — schema §3.17); nếu event đã `processed` → kết thúc.
  3. Hệ thống đối chiếu `merchant_request_id` → payment → booking; xác minh chữ ký/định danh, số tiền (`received_amount = amount`), tiền tệ (`received_currency = currency`); điều tra cổng (query status) trước transaction khi cần, xác minh lại trạng thái khi áp dụng.
  4. Nếu kết quả xác minh là **succeeded**: cập nhật `payment.status='succeeded'`, ghi `paid_at` (nếu cổng báo), `verified_at`, `received_amount`, `received_currency`, `provider_transaction_id` — UPDATE có điều kiện để phản hồi khởi tạo chậm không ghi đè `succeeded` đã nhận qua webhook (`database-design.md` §31.4).
  5. Kiểm tra điều kiện áp dụng (BR-PAY-004): booking `pending`, chưa quá `expires_at`, chưa quá `booking_closes_at` (nếu có), suất `scheduled`, mọi detail vẫn `held`, chưa có payment applied. **Đủ điều kiện → trong cùng transaction:** đặt `applied_at`, booking `pending→confirmed` + `confirmed_at`, toàn bộ detail `held→sold`, phát hành đúng `capacity_units_snapshot` ticket `valid` (phát hành idempotent: đếm vé hiện có, chỉ chèn thiếu), đánh dấu event `processed`. Commit → ACK.
  6. **Không đủ điều kiện** (booking `expired`/`cancelled`, ghế đã nhả/bán, sai tiền): giữ payment `succeeded` nguyên trạng, **không** đặt `applied_at`, **không** xác nhận booking, **không** phát hành vé, **không** chiếm lại ghế; ghi nhận kết quả qua `reconciliation_status`: booking đã hết hạn/hủy → `refund_required` (kích hoạt UC-04A khi phạm vi được kích hoạt); sai tiền/sai tiền tệ → `needs_review`. Event `processed` trong cùng transaction với các cập nhật này.
  7. Trả ACK cho Gateway theo hợp đồng cổng (sau khi event đã commit bền vững).
- **Luồng thay thế và ngoại lệ:**
  - **A1 (Chữ ký/nguồn không hợp lệ):** event KHÔNG được dùng để thay đổi bất kỳ trạng thái payment/booking/ghế/ticket nào (NFR-SEC-003); ghi nhận cho truy vết; ACK/NACK theo hợp đồng cổng (không quy định HTTP status cụ thể — OQ-01).
  - **A2 (Webhook trùng lặp / hai event khác `event_key` cùng đại diện một giao dịch):** (i) `(payment_id, event_key)` đã tồn tại → bỏ qua xử lý nghiệp vụ, trả ACK ngay; (ii) hai event **khác** `event_key` nhưng cùng thể hiện một kết quả giao dịch (ví dụ hai loại event của cùng `provider_transaction_id`): cả hai đều được lưu (không bị UNIQUE chặn) nhưng kết quả tài chính chỉ được ghi một lần — cập nhật payment là UPDATE có điều kiện theo trạng thái hiện tại (đã `succeeded` → không ghi đè, không phát hành thêm vé, không áp dụng lần hai); nếu hai event mang kết quả MÂU THUẪN cho cùng giao dịch → ghi nhận cả hai, không tự chọn, chuyển `needs_review` (A7).
  - **A3 (Thanh toán thành công khi booking đã hết hạn / ghế đã bán lại):** payment `succeeded`, `reconciliation_status = 'refund_required'` (hoặc `needs_review` nếu tiền không khớp), không vé, không chiếm ghế (AC-PAY-004); UC-04A thực thi khi được kích hoạt.
  - **A4 (Giao dịch thành công nhưng sai tiền/tiền tệ — phân biệt với thất bại thật):** phân biệt hai trường hợp: (i) cổng xác nhận giao dịch **thất bại** rõ ràng → `failed`/`cancelled` (bằng chứng kết quả không thành công được ghi qua `failure_code`/event); (ii) giao dịch **đã thu tiền thật** (cổng báo succeeded, backend xác minh) nhưng `received_amount ≠ amount` hoặc `received_currency ≠ currency` → **vẫn cập nhật `payment.status='succeeded'`** với giá trị thực nhận (schema cho phép: CHECK chỉ yêu cầu succeeded có đủ `paid_at`/`verified_at`/`received_amount`/`received_currency`/`provider_transaction_id`, không yêu cầu khớp — khớp mới là điều kiện apply), **không** đặt `applied_at`, **không** đổi `booking.total_amount`, `reconciliation_status = 'needs_review'` để nhân sự quyết định; bằng chứng thu tiền không bị loại bỏ chỉ vì sai tiền.
  - **A5 (Kết quả chưa chắc chắn — timeout/mất kết nối với cổng):** payment `unknown` (không mặc nhiên failed); không tạo attempt mở mới (BR-PAY-002); lên lịch đối soát `next_reconcile_at` → UC-04.
  - **A6 (Xử lý thất bại giữa chừng — partial failure):** transaction nghiệp vụ rollback → event vẫn `received` (hoặc `failed` + `retry_count`/`next_retry_at`/`last_error`), worker retry idempotent; event **không bao giờ** được đánh `processed` trong khi cập nhật nghiệp vụ chưa commit.
  - **A7 (Sự kiện đến sai thứ tự — báo failed sau khi đã succeeded+applied):** không hạ trạng thái payment đã áp dụng; ghi nhận event, đưa mâu thuẫn vào đối soát (`needs_review` nếu cần).
  - **A8 (Booking có payment khác đã applied):** payment này `succeeded` nhưng không applied; `needs_review`/`refund_required` theo tình huống.
  - **A9 (Booking pending quá hạn nhưng payment đang mở — grace period chưa hết):** worker không chuyển `expired`; xử lý theo UC-03; nếu payment thành công trong grace period → áp dụng bình thường.
- **Hậu điều kiện:**
  - **Thành công (áp dụng):** payment `succeeded` + `applied_at`; booking `confirmed`; detail `sold`; đúng số vé `valid`; event `processed`.
  - **Thành công (không áp dụng):** payment `succeeded` nguyên trạng; booking giữ trạng thái cũ; `reconciliation_status` phản ánh lý do; event `processed`.
  - **Thất bại/tạm thời:** event `received`/`failed` chờ retry; trạng thái nghiệp vụ không đổi nửa vời.
- **Quy tắc liên quan:** BR-PAY-001..008, FR-PAY-002..005, AC-PAY-001..005; constraints: `uq_payment_event_payment_key`, `ux_payment_applied_booking`, `ux_payment_open_booking`, `ux_payment_provider_transaction`, `ck_payment_applied`.
- **Tiêu chí nghiệm thu:** AC-UC02-01 (happy path), 02 (duplicate webhook chỉ một tác động nghiệp vụ), 03 (sai chữ ký không đổi trạng thái), 04 (booking expired + succeeded → không vé, `refund_required`), 05 (sai tiền → `needs_review`, booking không đổi), 06 (retry sau lỗi giữa chừng không tạo vé thừa), 07 (out-of-order không hạ trạng thái), 08 (hai worker cùng event — chỉ một cập nhật nghiệp vụ).
- **UC liên quan:** UC-01, UC-03 (race — A9), UC-04/04A.
- **Câu hỏi mở:** OQ-01 (cổng cụ thể — hợp đồng ACK, truy vấn, hoàn tiền), OQ-UC-02 (mức đối soát tự động — mục 11).

---

## 7. UC-03: Giải phóng đơn hàng hết hạn (Release Expired Bookings)

- **Mục tiêu:** Thu hồi ghế của các booking `pending` đã quá hạn giữ, sau khi bảo đảm không còn payment attempt đang mở cần bảo vệ; ghế sau đó có thể được đặt lại.
- **Tác nhân chính:** System Worker (chạy định kỳ).
- **Tác nhân phụ:** PostgreSQL; Payment Gateway (chỉ trong Grace Period — bước 4b).
- **Trigger:** Worker chạy định kỳ (đề xuất 15–30 giây — `database-design.md` §27.2; giá trị là cấu hình triển khai), hoặc dọn dẹp inline trong UC-01 bước 6.
- **Tiền điều kiện (điều kiện hai phần — BR-BOOK-004, OQ-07 RESOLVED):** booking `pending` **và** `expires_at` đã quá (theo thời gian DB thực sau khi khóa) **và** không còn payment `created`/`pending`/`unknown` — trừ khi đang trong Grace Period theo bước 4. `expires_at` quá hạn **một mình** là không đủ.
- **Luồng chính:**
  1. Worker quét danh sách booking ứng viên qua `ix_booking_expiry` (`expires_at, id WHERE status='pending'`) — chưa khóa ở bước quét.
  2. Vào transaction xử lý: khóa theo protocol `Showtime → Booking → Payment attempts` (khóa Showtime trước; không dùng SKIP LOCKED trên booking rồi quay lại khóa Showtime — tránh deadlock, `database-design.md` §31.1). **Gọi API cổng chỉ xảy ra NGOÀI transaction** (bước 4b) — không giữ khóa trong lúc chờ mạng.
  3. Sau khi khóa, đọc thời gian thực DB (`clock_timestamp()`) và **đọc lại mọi trạng thái**: booking vẫn `pending`? các attempt payment hiện tại là gì? Nếu booking không còn `pending` (đã `confirmed`/`cancelled`) → bỏ qua (A2).
  4. Nếu còn attempt mở — xử lý theo **bảng quyết định** dưới đây (gọi cổng chỉ ở dòng 3, ngoài transaction):
     - **4a. Chưa quá grace period** → bỏ qua booking này, không đổi gì (ghế vẫn giữ).
     - **4b.** Đã quá grace period và cần kết quả cổng: kết thúc transaction hiện tại (chỉ thu thập payment cần tra cứu), **gọi Query/Check Status API ngoài transaction**, rồi vào transaction mới để ghi kết quả.
  5. Booking không còn attempt mở: chuyển booking `pending → expired`; chuyển toàn bộ detail `held → released`, ghi `released_at` — trong cùng transaction; commit.

**Bảng quyết định UC-03 (payment state → hành động):**

| Payment state | Điều kiện | Hành động của worker | Gọi cổng? |
| :--- | :--- | :--- | :--- |
| Không có attempt / chỉ `failed`/`cancelled` | Booking quá `expires_at` | Release: booking `expired`, detail `released` | Không |
| `created`/`pending`/`unknown` | Chưa quá grace period | Bỏ qua — ghế giữ, chờ | Không |
| `created`/`pending`/`unknown` | Quá grace period, chưa có kết quả | Tra cứu cổng ngoài transaction; chưa thu → kết thúc attempt theo kết quả cổng rồi release; đã thu → A3 (payment thành công muộn); không xác đáng → giữ `unknown` + `needs_review`, bỏ qua (có giám sát) | **Có — ngoài transaction** |
| `succeeded` chưa `applied` | Booking vẫn `pending` quá hạn | Không release: chuyển sang xử lý áp dụng theo UC-02 (booking vẫn đủ điều kiện → applied/confirmed; không → `refund_required`/`needs_review`) | Không |
| `succeeded` + `needs_review` | Booking quá hạn | Không release; chuyển hàng chờ nhân sự (OQ-04); ghế giữ đến khi hồ sơ chốt | Không |
| `succeeded` + `refund_required` | Booking đã `expired`/ghế đã nhả | Không reclaim ghế; UC-04A xử lý khi phạm vi kích hoạt | Không |
| `succeeded` + `applied_at` | — | Booking đã `confirmed`; worker bỏ qua (không bao giờ hủy đơn đã confirm) | Không |

Ghi chú chính sách: các ngưỡng grace period, chu kỳ retry và ngưỡng leo thang `needs_review` là **cấu hình triển khai chưa chốt** (OQ-UC-03/02/04) — chính sách hai phần và "không giữ vô hạn tự động" là phần đã chốt (OQ-07). Ghế không được giữ vô hạn mà không có giám sát: nhánh `needs_review` là ngoại lệ có log + hàng `ix_payment_reconciliation_queue` để theo dõi.
- **Luồng thay thế và ngoại lệ:**
  - **A1 (Race với UC-02):** UC-02 và UC-03 cùng khóa theo cùng protocol; ai lấy khóa Showtime trước chạy xong luồng của mình. Nếu UC-02 áp dụng payment xong (booking `confirmed`), worker đọc lại thấy không `pending` → bỏ qua, không hủy đơn đã confirm. Nếu worker hết hạn + release commit trước, UC-02 thấy booking `expired`/detail `released` → nhánh A3 của UC-02 (không áp dụng).
  - **A1b (Tách transaction khi cần gọi cổng — không giữ khóa khi chờ mạng):** việc kết thúc attempt `unknown` dựa trên kết quả cổng luôn thực hiện qua hai transaction: (1) transaction đọc/qua thu thập trạng thái rồi commit/rollback mà không giữ khóa khi gọi cổng; (2) transaction mới ghi kết quả (kết thúc attempt theo kết quả cổng đã xác minh, hoặc release). Kiểm tra lại toàn bộ điều kiện hai phần trong transaction (2) — trạng thái có thể đã đổi giữa hai transaction.
  - **A2 (Booking đã đổi trạng thái):** bỏ qua an toàn, không ghi gì.
  - **A3 (Provider thành công muộn sau khi release):** ghế đã nhả **không reclaimed**; payment giữ `succeeded` + `refund_required`/`needs_review` (UC-02 A3); không vé.
  - **A4 (Nhiều worker chạy song song):** khóa dòng + đọc lại trạng thái bảo đảm mỗi booking chỉ được chuyển `expired` đúng một lần; lần chạy lặp lại thấy booking không còn `pending` → bỏ qua (idempotent).
  - **A5 (Lỗi DB giữa chừng):** rollback toàn bộ transaction; booking/detail giữ nguyên; lần chạy sau xử lý lại (không rơi vào trạng thái "expired nhưng ghế còn held").
  - **A6 (Worker chậm/ngừng hoạt động):** không ai giải phóng ghế đến khi worker hoạt động lại hoặc UC-01 dọn dẹp inline; ghế tạm thời không mua lại được nhưng **không bao giờ** bán trùng (index vẫn bảo vệ).
  - **A7 (Phân biệt "quá hạn" với "đã expired/released"):** booking quá hạn nhưng chưa qua worker là **vẫn `pending`/`held`** — dữ liệu giữa hai mốc chỉ đổi khi worker (hoặc UC-01 inline) thực sự commit chuyển trạng thái. Client/soát ghế không được suy "đã nhả ghế" chỉ từ `expires_at`.
- **Hậu điều kiện:**
  - **Thành công:** booking `expired`; mọi detail `released` + `released_at`; ghế khả dụng cho UC-01 của suất; lịch sử booking/detail/payment không bị xóa.
  - **Bỏ qua (grace period/needs_review):** booking + detail giữ nguyên; payment giữ trạng thái mở; sự kiện được log để giám sát.
- **Quy tắc liên quan:** BR-BOOK-004, BR-PAY-002/003/007, OQ-07; partial index không phụ thuộc `now()` (schema §3.13, §4).
- **Tiêu chí nghiệm thu:** AC-UC03-01 (expire chuẩn), 02 (attempt mở trong grace period → bỏ qua), 03 (quá grace + cổng báo chưa thu → release), 04 (quá grace + cổng báo đã thu → payment `succeeded` + `refund_required`, ghế không reclaimed), 05 (cổng không phản hồi → `unknown` + `needs_review`, ghế giữ), 06 (race với UC-02 — không vé double, không confirm booking đã expired), 07 (chạy lại → idempotent), 08 (sau release, booking khác giữ được ghế qua UC-01).
- **UC liên quan:** UC-01 (dọn dẹp inline), UC-02 (A1/A3), UC-04 (needs_review).
- **Câu hỏi mở:** OQ-UC-03 (giá trị grace period chính xác — cấu hình triển khai, mục 11).

---

## 8. UC-04: Đối soát payment (Reconciliation) & UC-04A: Hoàn tiền (Refund)

> **Phân biệt bắt buộc:** *Đối soát (reconciliation)* là quy trình **tìm và chốt kết quả thật** của các payment `pending`/`unknown` hoặc có dữ liệu bất thường, không di chuyển tiền. *Hoàn tiền (refund)* là quy trình **chuyển tiền ngược** cho khách, chỉ xảy ra sau khi kết quả tài chính đã chốt và nghiệp vụ quyết định hoàn. Hai vòng đời độc lập; refund nằm ngoài phạm vi demo (chỉ mô hình dữ liệu + hợp đồng hành vi).

### UC-04: Payment Reconciliation

**Phạm vi (In scope / Out of scope / Deferred — theo `business-system-analysis.md` §3.1/§3.3):**

| Hoạt động | Phạm vi demo | Ghi chú |
| :--- | :--- | :--- |
| Truy vấn trạng thái payment `created`/`pending`/`unknown` | **In scope** | Trước transaction, không giữ khóa khi gọi cổng |
| Ghi nhận kết quả tài chính (succeeded/failed) + apply theo UC-02 | **In scope** | Bao gồm thành công muộn trước khi ghế bị bán lại |
| Xử lý payment thành công muộn khi ghế đã bán lại (chỉ ghi nhận `succeeded` + routing) | **In scope (ghi nhận)** — xử lý tài chính tiếp theo | BR-PAY-007: không reclaim, không vé; `refund_required`/`needs_review` |
| Thực thi hoàn tiền (gọi Refund API) | **Out of scope — demo** | UC-04A: chỉ hợp đồng hành vi + mô hình dữ liệu |
| Quyết định needs_review thủ công | **Deferred** (theo OQ-04: quyền Manager chưa chốt) | Schema sẵn sàng; luồng người dùng khi chốt OQ-04 |

- **Mục tiêu:** Chốt kết quả thật của các payment chưa chắc chắn (`unknown`/`pending`) và phát hiện bất thường (thành công muộn, sai tiền) để routing đúng (applied / needs_review / refund_required).
- **Tác nhân chính:** System Worker; Manager (xử lý needs_review thủ công, theo OQ-04).
- **Tác nhân phụ:** Payment Gateway (Query/Check Status API), Database.
- **Trigger:** Worker quét `ix_payment_reconcile_due` (`next_reconcile_at, id WHERE status IN ('created','pending','unknown')`) và hàng `ix_payment_reconciliation_queue` (`needs_review`/`refund_required`).
- **Tiền điều kiện:** payment ở trạng thái chưa chốt hoặc `reconciliation_status ≠ 'none'/'resolved'`.
- **Luồng chính:**
  1. Worker chọn payment đến hạn; gọi Query Status API của cổng **ngoài transaction**.
  2. Vào transaction, đọc lại trạng thái payment hiện hành (khóa theo protocol); UPDATE có điều kiện — kết quả cũ `succeeded` không bị ghi đè bởi phản hồi chậm (BR-PAY-008).
  3. Nếu cổng xác nhận thành công + tiền khớp → cập nhật `succeeded` + `paid_at`/`verified_at`/`received_*`/`provider_transaction_id`; sau đó áp dụng theo UC-02 bước 5/6 (booking còn đủ điều kiện → applied; không → refund_required/needs_review).
  4. Nếu cổng xác nhận thất bại/hủy rõ ràng → `failed`/`cancelled` (kết quả từ cổng đã xác minh).
  5. Nếu cổng không xác định được → giữ `unknown`, đặt `next_reconcile_at` theo chính sách retry (chưa chốt — OQ-UC-04).
  6. Hàng `needs_review` được đưa cho nhân sự (OQ-04) xử lý thủ công; khi xử lý xong → `resolved`.
- **Ý nghĩa `reconciliation_status`:**
  - `none` — chưa có bất thường.
  - `needs_review` — cần con người quyết định (sai tiền, mâu thuẫn dữ liệu, refund bị từ chối).
  - `refund_required` — tiền đã thu nhưng phải trả lại khách (UC-04A thực thi khi phạm vi kích hoạt).
  - `resolved` — **bất thường đã được xử lý xong**: chỉ đặt khi refund liên quan đạt `succeeded` (với refund_required) hoặc nhân sự đã đóng hồ sơ (với needs_review). **Không bao giờ** đặt `resolved` chỉ vì cổng "đồng ý xử lý" hay refund còn `pending`.
- **Ngoại lệ:** A1 — cổng timeout/lỗi: giữ trạng thái, lịch retry; A2 — phản hồi mâu thuẫn (sự kiện cũ báo failed sau khi succeeded): không hạ trạng thái, log event, needs_review; A3 — payment đã applied nhưng dữ liệu đối soát lệch: needs_review, không đảo applied.
- **Tiêu chí nghiệm thu:** AC-UC04-01 (unknown → succeeded qua query, booking còn hạn → applied), 02 (unknown → failed rõ ràng), 03 (không xác định → giữ unknown, retry), 04 (succeeded cũ không bị ghi đè), 05 (needs_review → resolved chỉ bởi quyết định nhân sự/hồ sơ đóng).
- **Câu hỏi mở:** OQ-UC-04 (chu kỳ/độ trễ retry tối đa — mục 11).

### UC-04A: Refund (OUT OF SCOPE — DEMO; hợp đồng hành vi)

- **Mục tiêu:** Hoàn tiền cho payment `succeeded` mà nghiệp vụ quyết định phải trả (refund_required/needs_review được chấp thuận).
- **Tác nhân chính:** System Worker / Manager (theo phạm vi khi kích hoạt).
- **Tác nhân phụ:** Payment Gateway (Refund API + refund webhook/polling), Database.
- **Tiền điều kiện (theo schema thực tế):** payment `status='succeeded'`; chưa có refund `succeeded` cho payment; refund được tạo với `merchant_refund_id` (UUID, backend sinh, UNIQUE) làm **khóa idempotency ổn định** — retry dùng lại cùng ID, không sinh ID mới.
- **Luồng chính:**
  1. Tạo `refund` (`pending`, `amount`/`currency` từ payment thực thu, `provider` = `payment.provider` — bắt buộc khớp qua FK ghép `refund(payment_id, provider) → payment(id, provider)`, schema §3.18). `ON CONFLICT (payment_id) DO NOTHING` rồi đọc lại bản ghi đã có (không tạo refund thứ hai).
  2. Gọi Refund API truyền `amount`, `provider_transaction_id` (của payment gốc), `merchant_refund_id`.
  3. Cổng phản hồi: **"chấp nhận xử lý" hoặc "pending" KHÔNG đồng nghĩa tiền đã trả** — refund giữ `pending`/`unknown` cho đến khi cổng xác nhận kết quả qua webhook hoàn tiền hoặc polling (OQ-01).
  4. Kết quả `succeeded` (tiền đã trả thật, xác minh) → `completed_at`; cập nhật `payment.reconciliation_status = 'resolved'` **chỉ ở thời điểm này** (hoặc needs_review được đóng bởi nhân sự).
  5. Kết quả `failed` rõ ràng → `refund.failed` + `payment.reconciliation_status = 'needs_review'` (con người quyết định phương án thủ công); không tự retry vô hạn.
- **Ngoại lệ:**
  - **A1 (Timeout/phản hồi mơ hồ):** giữ `pending`/`unknown`; **không gọi Refund API lần hai với merchant_refund_id mới** — truy vấn trạng thái theo cùng ID; chỉ gửi lại khi xác nhận request trước chưa được ghi nhận ở cổng (phụ thuộc hợp đồng idempotency của cổng — OQ-01).
  - **A2 (Cổng từ chối):** `failed` + needs_review; audit trail giữ nguyên.
  - **A3 (Webhook hoàn tiền trùng):** idempotent theo trạng thái refund hiện có; không tạo refund mới.
- **Bảo vệ chống hoàn tiền trùng (theo schema thực tế — đã kiểm chứng trong `database-schema.sql`):** `uq_refund_payment` UNIQUE (`payment_id`) — tối đa một refund/payment; `uq_refund_merchant_refund_id` UNIQUE; `ux_refund_provider_refund_id` partial unique `(provider, provider_refund_id) WHERE provider_refund_id IS NOT NULL`; FK ghép `(payment_id, provider) → payment(id, provider)`. **Lưu ý:** các ràng buộc này không thay thế idempotency key phía cổng; nếu cổng không hỗ trợ idempotent refund, retry an toàn phụ thuộc truy vấn trạng thái trước khi gửi lại (OQ-01).
- **Hậu điều kiện:** tiền đã trả lại khách (refund `succeeded`) + payment `resolved`; hoặc hồ sơ needs_review chờ con người; **không bao giờ** đánh `resolved` khi refund chỉ `pending`/`unknown`.
- **Tiêu chí nghiệm thu:** AC-UC04A-01 (happy: pending → succeeded → resolved), 02 (provider chấp nhận nhưng pending → KHÔNG resolved), 03 (retry cùng merchant_refund_id, không tạo refund thứ hai), 04 (provider từ chối → failed + needs_review), 05 (không hai refund `succeeded` cho cùng payment).

---

## 9. UC-05: Soát vé tại rạp (Validate Ticket)

- **Mục tiêu:** Nhân viên quét mã vé (QR chứa `ticket_code`) để xác thực quyền vào rạp một lần; ngăn vé giả, vé dùng lại; cho phép soát từng vé độc lập (ghế Couple = 2 vé riêng — BR-BOOK-009).
- **Tác nhân chính:** Nhân viên rạp (Staff) — role `STAFF`, authorization backend (BR-AUTH-001..003; Staff đã xác nhận cho nghiệp vụ soát vé — OQ-04).
- **Tác nhân phụ:** Backend Validation API; Database.
- **Trigger:** Staff quét QR/nhập mã vé.
- **Tiền điều kiện:**
  - Staff đã đăng nhập, tài khoản `active`, có quyền soát vé (không dựa vào việc biết `ticket_code` để suy quyền).
  - **Ngữ cảnh soát:** Staff thao tác trong ngữ cảnh một suất chiếu đang soát (chọn trước trên ứng dụng staff, hoặc hệ thống xác định suất từ vé). Nếu ứng dụng staff có chọn suất trước, hệ thống đối chiếu suất của vé với suất đang soát — khác → A3. Nếu không chọn trước, hệ thống hiển thị suất/phòng của vé và vẫn kiểm tra cửa sổ; hành vi mặc định (chọn trước hay không) là **PROPOSED — OQ-UC-07**.
  - QR giải mã ra `ticket_code` dạng UUID hợp lệ.
- **Luồng chính:**
  1. [Staff] Gửi `ticket_code` lên API.
  2. Hệ thống truy vấn `ticket` theo `ticket_code` (UNIQUE); join `booking_detail` → `booking`, `showtime`, `seat` (nhãn ghế từ `seat_label_snapshot`).
  3. Kiểm tra nghiệp vụ: booking `confirmed`; detail `sold`; suất chiếu của vé khớp; `ticket.status = 'valid'`.
  4. Kiểm tra **cửa sổ soát vé** (khoảng thời gian cho phép vào rạp so với `starts_at`/`ends_at`) — **quy tắc cấu hình, chưa chốt (OQ-03)**; hệ thống lấy giá trị từ cấu hình, không hard-code (ví dụ "-30 phút" trong bản nháp chỉ là ví dụ, không phải yêu cầu).
  5. Thực hiện chuyển trạng thái **có điều kiện** trong transaction: `UPDATE ticket SET status='used', used_at=..., validated_by_user_id=... WHERE ticket_code = ... AND status = 'valid'` — đúng một attempt đồng thời thành công; attempt khác thấy 0 dòng cập nhật → trả "vé đã dùng" (A1).
  6. Trả kết quả "Hợp lệ" kèm phòng chiếu (`room_name_snapshot`), ghế (`seat_label_snapshot`), suất chiếu; vé Couple (detail `capacity_units_snapshot = 2`) được soát từng vé riêng — người đến trễ vẫn vào được bằng vé của mình.
- **Luồng thay thế và ngoại lệ:**
  - **A1 (Vé đã dùng):** từ chối với thông tin `used_at` và nhân viên đã soát (audit có sẵn trong bản ghi) — không đổi trạng thái.
  - **A2 (Vé `voided`):** từ chối; hiển thị `void_reason`.
  - **A3 (Sai suất/sai phòng/cửa sổ không hợp lệ):** từ chối với cảnh báo chi tiết để Staff báo khách; **không đổi trạng thái**.
  - **A4 (Mã không tồn tại/sai định dạng):** "Vé không tồn tại" — không tiết lộ thêm (NFR-SEC).
  - **A5 (Booking chưa `confirmed` / detail chưa `sold`):** từ chối (vé không thể hợp lệ khi chưa được phát hành trong luồng áp dụng payment).
  - **A6 (Lỗi DB khi cập nhật):** transaction rollback; vé giữ `valid`; Staff quét lại được.
  - **A7 (Commit thành công nhưng response bị mất/đứt kết nối):** vé đã `used` + audit đã ghi; lần quét lại KHÔNG tạo lần soát thứ hai — hệ thống phát hiện `status = 'used'` và trả A1 (thông tin audit của lần soát thành công đầu tiên: `used_at`, `validated_by_user_id`). Response lỗi (timeout) không được phép ghi đè audit đã commit.
- **Hậu điều kiện:**
  - **Thành công:** vé `used` + `used_at` + `validated_by_user_id` (audit truy vết); không thể soát thành công lần hai.
  - **Thất bại:** vé giữ nguyên trạng thái; attempt thất bại không ghi đè audit của lần soát thành công.
- **Quy tắc liên quan:** BR-BOOK-009, FR-TICKET-002, AC-TICKET-001, BR-AUTH-002/003; OQ-03, OQ-04.
- **Tiêu chí nghiệm thu:** AC-UC05-01 (valid → used một lần), 02 (lần hai bị từ chối với thông tin audit), 03 (hai scan đồng thời — đúng một thành công), 04 (vé used/voided/không tồn tại bị từ chối), 05 (cửa sổ không hợp lệ → từ chối không đổi trạng thái), 06 (Couple: hai vé độc lập, soát riêng, khách đến trễ vào được bằng vé của mình), 07 (commit thành công + response mất → quét lại trả A1 với audit của lần soát thành công, không tạo lần soát thứ hai).
- **UC liên quan:** UC-02 (phát hành vé), OQ-03/OQ-04.
- **Câu hỏi mở:** OQ-03 (cửa sổ soát vé — mục 11).

## 10. Tính nhất quán giữa các UC (Cross-UC lifecycle)

Bảng chuyển trạng thái cốt lõi (giá trị khớp schema — mục 2):
| Sự kiện | booking | booking_detail | payment | payment_event | ticket | refund |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| UC-01 thành công | `pending` | `held` | — | — | — | — |
| UC-02 áp dụng thành công | `confirmed` (+`confirmed_at`) | `held→sold` | `succeeded` + `applied_at` | `processed` | N×`valid` (N = capacity_units_snapshot) | — |
| UC-02 thành công nhưng không áp dụng | giữ nguyên (`pending`/`expired`/`cancelled`) | giữ nguyên | `succeeded`, **không** `applied_at`; `reconciliation_status` = `needs_review`/`refund_required` | `processed` | — | — |
| UC-03 hết hạn (điều kiện hai phần + grace) | `pending→expired` | `held→released` (+`released_at`) | attempt mở phải chốt trước (hoặc qua grace) | — | — | — |
| UC-03 bỏ qua (grace period/needs_review) | `pending` | `held` | `created`/`pending`/`unknown` | — | — | — |
| Payment thành công muộn (sau release) | `expired` (không reclaimed) | `released` | `succeeded`, không applied | `processed` | — | `refund_required` → UC-04A khi kích hoạt |
| Khách hủy (FR-BOOK-003; qua cổng xác nhận chưa thu nếu có attempt) | `cancelled` | `held→released` | attempt mở → `cancelled` (từ cổng) | — | — | — |
| UC-04 query chốt kết quả | (theo UC-02 bước 5/6) | (như trên) | `unknown→succeeded`/`failed` | `processed` | (như UC-02) | — |
| UC-04A hoàn tiền xong | giữ nguyên | giữ nguyên | `reconciliation_status='resolved'` | — | giữ nguyên | `succeeded` + `completed_at` |
| UC-05 soát | `confirmed` | `sold` | — | — | `valid→used` | — |

Điểm nhất quán cần nhớ:

- **Hết hạn ≠ giải phóng:** "quá hạn" (vượt `expires_at`) là điều kiện; `expired`/`released` là kết quả do worker thực hiện khi điều kiện hai phần thỏa (hoặc grace period đã xử lý attempt).
- **Succeeded ≠ applied:** payment thành công và áp dụng payment là hai kết quả độc lập; chỉ applied xác nhận booking và phát hành vé.
- **Confirmed ⇒ đã phát hành đúng số vé:** booking `confirmed` phải có đúng `capacity_units_snapshot` vé/detail; callback lặp không phát hành thừa (idempotent).
- **Refund accepted ≠ refund completed:** chỉ `refund.status = 'succeeded'` mới dẫn tới `reconciliation_status = 'resolved'`.
- **Idempotency & retry:** mọi luồng lặp (webhook trùng, worker chạy lại, refund retry, quét lại) phải idempotent nhờ khóa sự kiện/UQ + điều kiện cập nhật theo trạng thái; không luồng nào sinh tác động nghiệp vụ thứ hai từ cùng sự kiện.

## 11. Câu hỏi mở và traceability

| ID | Mâu thuẫn / quyết định còn thiếu | Vì sao quan trọng | Hành vi an toàn tạm (đề xuất, chưa phải quyết định) | Ảnh hưởng |
| :--- | :--- | :--- | :--- | :--- |
| **OQ-UC-01** | Bản nháp UC-01 cho phép đặt khi suất `in_progress`; thiết kế đã chốt chỉ bán khi `scheduled` (BR-ST-003, FR-MGT-ST). | Ảnh hưởng trực tiếp tiền đề UC-01 và luồng bán sát giờ. | Chỉ nhận đặt khi `scheduled` (theo thiết kế đã chốt). | UC-01; API đặt vé; test case xuất phát suất. |
| **OQ-UC-02** | Mức tự động hóa đối soát (UC-04) chưa có chính sách: khi nào `unknown` leo thang `needs_review`? | Worker cần ngưỡng leo thang; không có ngưỡng, `unknown` có thể treo vô hạn ở hàng retry. | Chờ theo chu kỳ `next_reconcile_at`; leo thang khi vượt ngưỡng cấu hình (khi chốt). | UC-04; cấu hình worker; giám sát. |
| **OQ-UC-03** | Giá trị grace period chính xác (OQ-07 đã chốt **chính sách** — đề xuất 2–3 phút — chưa chốt **số liệu**). | Quyết định tổng thời gian ghế bị khóa tối đa. | Cấu hình triển khai; đề xuất mặc định 120 giây. | UC-03 (bước 4a), UC-02 (A9), cấu hình. |
| **OQ-UC-04** | Chu kỳ retry đối soát/refund (`next_reconcile_at`/`next_retry_at`) — khoảng chờ và số lần tối đa chưa định nghĩa. | Retry vô hạn làm ngập hàng đợi; ngừng quá sớm mất cơ hội chốt kết quả. | Exponential backoff có trần (khi chốt); không giới hạn số lần cho đến khi có kết quả. | UC-04, UC-04A; cấu hình worker; test retry. |
| **OQ-UC-05** | Chuẩn hóa `request_fingerprint`: danh sách ghế có được sort trước khi băm không (đổi thứ tự chọn = cùng nội dung hay khác nội dung)? | Quyết định replay/conflict của UC-01 A1/A2. | Sort trước khi băm (đổi thứ tự = cùng nội dung) [PROPOSED]. | UC-01; API request chuẩn hóa; test idempotency. |
| **OQ-UC-06** | `payment_event` không có cột lưu chứng cứ/trạng thái xác minh chữ ký (payload_summary chỉ jsonb đã lọc) — có cần lưu để worker xác minh lại không? | Kiến trúc xác minh ingress-vs-worker cho UC-02. | Xác minh một lần ở ingress; worker chỉ xác minh nghiệp vụ [PROPOSED]. | UC-02; thiết kế payment_event nếu cần thêm cột (schema change — cần phê duyệt riêng). |
| **OQ-UC-07** | Ứng dụng staff có chọn suất trước khi quét không (ngữ cảnh soát)? | Ảnh hưởng kiểm tra "vé thuộc suất đang soát" của UC-05. | Cho phép soát không chọn trước: hiển thị suất/phòng của vé, vẫn kiểm cửa sổ [PROPOSED]. | UC-05; UX staff app; API soát vé. |
| OQ-01 | Cổng thanh toán cụ thể (`business-system-analysis.md` §13). | Hợp đồng ACK/signature/query/refund/idempotency của cổng. | Middleware trung lập cổng; chi tiết theo adapter. | UC-02, UC-04A; API spec. |
| OQ-03 | Cửa sổ soát vé (`business-system-analysis.md` §13). | Tiền đề UC-05 bước 4. | Lấy từ cấu hình; từ chối ngoài cửa sổ. | UC-05; cấu hình rạp. |
| OQ-04 | Ranh giới quyền Staff/Manager/Admin (`business-system-analysis.md` §13). | Authz UC-05, UC-04 (needs_review), quản trị. | Staff = soát vé; Manager/Admin theo PROPOSED. | UC-05, UC-04; authorization. |

## 12. Truy vết nguồn (traceability)

- Quy tắc nghiệp vụ: `docs/requirements/business-system-analysis.md` — §7 (FR), §8 (BR), §12 (AC), §13 (OQ).
- Use case tổng quan: `docs/analysis and design/business-system-design.md` — UC-01..UC-05.
- Thiết kế entity/concurrency: `database/database-design.md` — §12–15 (entity), §23 (constraints), §26–27 (schedule/seat), §31–34 (transaction/concurrency protocol), §37 (checklist).
- Mô hình quan hệ: `docs/analysis and design/relational-model.md` — §3 (relation), §9 (invariants).
- Schema vật lý: `database/database-schema.md` — §3.13–3.18, §4; DDL: `database/database-schema.sql`.
