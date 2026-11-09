# Hệ thống quản lý và đặt vé rạp chiếu phim — Đặc tả Use Case

Bản rà soát: 09/11/2026. Nguồn nghiệp vụ: **RUP Use-Case Specifications v2.4**, bản đính kèm `rup_ucspec-v2.4_final(5).docx`. Nguồn kiến trúc: **SAD v3.15**, bản đính kèm `rup_sad(1).docx`. Đường dẫn lưu DOCX dự kiến trong repository là `pa/PA3-Group15/`; cần đối chiếu file thực tế khi tích hợp.

DOCX quyết định hành vi nghiệp vụ; SAD giải thích cách phối hợp component. Markdown/schema/API cũ không được dùng để ghi đè baseline. Tài liệu mô tả yêu cầu/thiết kế, **không xác nhận mã nguồn đã triển khai hoặc kiểm thử đạt**. Các mã R-xx, F-UC-xx và AC-R-xx dưới đây là mã truy vết nội bộ của bản Markdown này, không phải mã BR/FR/AC được trích nguyên văn từ DOCX.

## Mục đích và quy ước đọc

Đây là bản diễn giải tiếng Việt phục vụ triển khai, giữ đầy đủ **28 mục UC** và các nhánh nghiệp vụ trọng yếu; không phải bản dịch nguyên văn toàn bộ DOCX. Các chi tiết/ngoại lệ đầy đủ trong UC tương ứng của DOCX vẫn có hiệu lực. Mỗi mục ghi trigger, preconditions, basic/alternative flows và postconditions. Mã A1… dưới đây đánh số trong bản Markdown, không giả làm số nhánh của DOCX.

Component C01–C16 và database là thành phần thực hiện, không phải external actor. Worker là cơ chế thực thi nội bộ. Quan hệ điều hướng/phụ thuộc không tự trở thành UML include/extend; quan hệ include đã xác định là **UC-09 → UC-01**. Refresh token thuộc thiết kế C03 hỗ trợ UC-08C/D, không tạo thêm numbered UC.

## Catalogue và functional domains

| UC | Tên | Domain | Actor / phạm vi | Component liên quan |
|---|---|---|---|---|
| UC-01 | Reserve Seats | D1 | Guest / Customer | C08 |
| UC-02 | Payment Webhook | D9 | Payment Gateway | C09 |
| UC-03 | Release Expired Bookings | D9 | System Worker | C08, C14 |
| UC-04 | Payment Reconciliation | D9 | System Worker | C09, C14 |
| UC-04A | Refund | D9 | Manager / Admin; system workflow | C10 |
| UC-05 | Validate Ticket | D3 | Assigned Staff | C11 |
| UC-06A | Browse Movies | D1 | Guest / Customer | C04 |
| UC-06B | View Movie Details | D1 | Guest / Customer | C04 |
| UC-07 | Browse Showtimes | D1 | Guest / Customer | C06 |
| UC-08A | Register Customer Account | D8 | Guest | C03 |
| UC-08B | Verify Email Address | D8 | Account holder / verification link holder | C03 |
| UC-08C | Login | D8 | Account holder | C03 |
| UC-08D | Logout | D8 | Authenticated user | C03 |
| UC-09 | Checkout and Payment | D1 | Guest / Customer | C08, C09 |
| UC-10 | Customer Booking Lifecycle | D2 | Guest / Customer with resource authority | C08, C10, C11 |
| UC-11 | Chat with Promethe-Nous | D1 | Guest / Customer | C12 |
| UC-12 | Manage Movie Catalog | D4 | Admin | C04 |
| UC-13 | Manage Genres | D4 | Admin | C04 |
| UC-14A | Manage Cinemas | D5 | Admin; scoped Manager; Staff read | C05 |
| UC-14B | Manage Screening Rooms | D5 | Admin; scoped Manager/Staff read | C05 |
| UC-15A | Configure Seat Layout | D5 | Admin / scoped Manager; Staff read | C05 |
| UC-15B | Change Seat Operational Status | D5 | Admin / scoped Manager; Staff read | C05 |
| UC-15C | Manage Seat Types | D7 | Admin write; permitted operational read | C05 |
| UC-16A | Manage Ticket Types | D7 | Admin write; permitted operational read | C07 |
| UC-16B | Configure Cinema Ticket Pricing | D7 | Admin / scoped Manager; Staff read | C07 |
| UC-17 | Manage Showtimes and Schedules | D6 | Admin / scoped Manager; Staff read | C06 |
| UC-18 | Manage User Accounts and Roles | D8 | Admin; scoped Manager Staff assignments | C03 |
| UC-19 | Manage Bookings and Tickets | D2 | Scoped Staff / Manager / Admin | C08, C10, C11 |

## Phân quyền operation

Mọi quyền sau yêu cầu current active account/session; internal access yêu cầu verified email. “Assigned” kiểm tra đúng loại role–cinema assignment tại thời điểm protected commit.

| Operation | Staff | Manager | Admin |
|---|---|---|---|
| Movie/genre write | Không | Không | Có |
| Cinema identity/state | Không | Không | Có |
| Cinema minimum turnaround | Read assigned | Write assigned | Có |
| Room identity/state | Read assigned | Read assigned | Write |
| Physical seat layout/state | Read assigned | Write assigned | Write |
| Shared seat types/surcharges | Permitted read | Permitted read | Write |
| Shared ticket categories | Permitted read | Permitted read | Permitted descriptive write |
| Cinema base pricing | Read assigned | Write assigned | Write |
| Showtime schedule | Read assigned | Write assigned | Write |
| Booking support/ticket resend | Assigned | Assigned | Authorized system-wide |
| Cancellation approve/reject | Assigned | Assigned | Authorized system-wide |
| Record verified manual refund completion | Không | Assigned | Có |
| Admission | STAFF + Staff assignment | Không qua MANAGER role | Không qua ADMIN role |
| Grant/revoke internal roles; lock/unlock account | Không | Không | Có; bảo vệ last active Admin |
| Staff–Cinema assignments | Không | Chỉ cinema được giao | Có |
| Manager–Cinema assignments | Không | Không | Có |

## Quy tắc chung bắt buộc

Các R-xx được định nghĩa đồng bộ tại [Business System Analysis](business-system-analysis.md). Phần này lặp lại để người triển khai không bỏ sót điều kiện khi đọc riêng UC.

| ID nội bộ | Chủ đề | Quy tắc | UC nguồn |
|---|---|---|---|
| R-01 | Guest và ownership | Guest được checkout. Customer chỉ truy cập tài nguyên thuộc quyền sở hữu; Guest cần credential/proof do backend xác minh. Biết email, phone, booking code hoặc ticket code riêng lẻ không đủ quyền truy cập quản lý booking. QR đưa cho Staff phục vụ admission theo UC-05, không cấp quyền quản lý booking. | UC-01, UC-09, UC-10, UC-19 |
| R-02 | Selection và reservation | Selection kéo dài 5 phút từ lần chọn ghế vật lý đầu tiên, do server xác định; chưa tạo booking hoặc hold. Thay quantities, bỏ rồi chọn lại ghế không reset timer. Chuyển Checkout Step 1 → Step 2 mới gọi UC-01, atomically tạo pending booking và held allocations, kế thừa deadline gốc. | UC-01, UC-09 |
| R-03 | Reload và navigation | Reload Movie Detail reset lựa chọn theo navigation policy của UC; reload Checkout Step 1/2 phục hồi session/booking còn hợp lệ và không kéo dài deadline. Back/đóng tab không tự hủy booking hoặc payment. Backend quyết định kết quả sau mất response. | UC-01, UC-09, UC-10 |
| R-04 | Quantities và ghế | Chỉ Adult và Discounted; tổng quantity từ 1 đến giới hạn cấu hình, mặc định 8. Không có giới hạn Discounted riêng. Các ghế vật lý distinct, cùng suất/phòng, số ghế bằng tổng quantity. Category ở cấp booking, không gán category từng ghế, không Couple/capacity-unit hoặc danh tính từng người tham dự. Discounted áp dụng sinh viên, giáo viên, người cao tuổi từ 60 theo kiểm tra eligibility của nguồn. | UC-01, UC-05, UC-09, UC-16A |
| R-05 | Giá và snapshot | Tổng = AdultQty × AdultBasePrice(cinema) + DiscountedQty × DiscountedBasePrice(cinema) + tổng surcharge của từng physical seat. Backend tính và lưu giá đã chấp nhận, quantities, ghế và purchased-start snapshot. Giá/giờ chính thức thay đổi trước commit phải được xử lý rõ ràng, không âm thầm áp dụng; chấp nhận lại không reset timer. Giá cấu hình mới không hồi tố booking đã tạo. Base price 0 được phép; đường đi tổng booking bằng 0 cần làm rõ riêng, không tự áp chính sách cũ Option B hoặc tự confirm miễn phí. | UC-01, UC-09, UC-15C, UC-16B, UC-17 |
| R-06 | Checkout contact | Step 1 yêu cầu full name, phone, email và acknowledgements về điều khoản/độ tuổi. Chọn phương thức MoMo không tự khởi tạo payment; phải có hành động xác nhận thanh toán. Không tạo booking thứ hai khi tải lại hoặc chuyển trang. | UC-09 |
| R-07 | Payment deadline | MoMo initiation hợp lệ thiết lập cửa sổ thanh toán 10 phút thay cho selection deadline gốc theo workflow. Grace mặc định 10 giây, cấu hình được, dành cho xác minh; không phải quyền thanh toán thêm hoặc bằng chứng chưa thu tiền. Unresolved payment vẫn được bảo vệ và đối soát. | UC-02, UC-03, UC-04, UC-09 |
| R-08 | Kết quả tài chính | Timeout, browser return, hết timer hoặc thiếu webhook không chứng minh thất bại. Phân biệt payment succeeded, payment applied, booking confirmed và ticket issued. Xác minh nguồn/chữ ký trước tác động nghiệp vụ; đối chiếu reference, amount, currency. Giao dịch đã xác minh thu tiền nhưng sai amount/currency phải bảo toàn bằng chứng và chuyển review, không áp dụng booking. | UC-02, UC-04, UC-09 |
| R-09 | Atomic payment application | Khi đủ điều kiện hiện hành, payment application, booking confirmation, held→sold, một ticket/QR cấp booking, persisted notification request và audit bắt buộc commit nhất quán. Receipt hợp lệ được lưu bền vững trước ACK theo provider contract; xử lý event có thể ở transaction sau. Email gửi sau commit, lỗi email không đảo booking. | UC-02, UC-04, UC-09 |
| R-10 | Concurrency và recovery | Không có hai active allocations cho cùng physical seat/showtime; reservation tất cả hoặc không có ghế. Idempotency gắn operation và authenticated/Guest identity, payload fingerprint. Retry không tạo booking/payment/ticket/refund/admission trùng. Không gọi provider khi giữ business locks; SAD phối hợp thứ tự Showtime → Booking → Payment attempts tại các workflow liên quan, rồi đọc lại state/time. Client mất response không đồng nghĩa rollback. | UC-01, UC-02, UC-03, UC-04, UC-04A, UC-05, UC-09, UC-10, UC-19 |
| R-11 | Expiration | Worker chỉ expired/release sau khi đủ điều kiện thời gian và payment safety. Không release do timer UI; không giải phóng payment unknown khi chưa có kết luận hợp lệ. Late success không reclaim ghế hoặc tự phát vé cho booking không đủ điều kiện; tạo review/refund obligation. | UC-02, UC-03, UC-04 |
| R-12 | Hủy pending | Chỉ hủy pending trước khi payment initiation được commit, qua cảnh báo và xác nhận rõ ràng; commit cancelled và held→released cùng nhau. Sau initiation không dùng Cancel/Query gateway làm lối tắt đổi ghế. Race initiation/cancel phải được phân xử atomically. | UC-09, UC-10 |
| R-13 | Yêu cầu hủy confirmed | Chỉ do qualifying rescheduling: current official start khác purchased-start snapshot, trước giờ bắt đầu hiện hành, chưa có admissions; hủy toàn booking. Trạng thái request: pending_review, approved_awaiting_refund, rejected, withdrawn, completed. Chỉ pending_review được rút. Request đã nộp hợp lệ có thể tiếp tục review nếu giờ hiện hành quay về snapshot; không mở quyền tạo request mới khi hai giờ bằng nhau. | UC-10, UC-17, UC-19 |
| R-14 | Review và admission | Pending review không chặn admission; admission đầu tiên tự reject pending request trong cùng transaction. Staff/Manager/Admin được review trong phạm vi hiện hành. Approved awaiting refund chặn admission nhưng booking vẫn confirmed, ghế vẫn sold cho tới refund completion hợp lệ. | UC-05, UC-10, UC-19 |
| R-15 | Refund theo nguồn | Refund nghĩa vụ từ payment_reconciliation hoặc showtime_rescheduling; financial state pending/unknown/failed/succeeded, độc lập cancellation request. Demo thực chuyển tiền bên ngoài app; Manager được phân công hoặc Admin ghi completion sau kiểm tra payment, toàn bộ amount/currency, reference và evidence. Staff không có quyền completion. Không partial refund hoặc hoàn trùng; accepted/pending không phải completed. | UC-04, UC-04A, UC-19 |
| R-16 | Refund completion effects | Rescheduling completion hợp lệ: refund succeeded + request completed + booking cancelled + ticket voided + sold allocations released + audit atomically. Reconciliation refund giải quyết nghĩa vụ tài chính theo hồ sơ, không mặc định có cùng các tác động hủy booking/giải phóng ghế. | UC-04A, UC-10, UC-19 |
| R-17 | Admission | Một QR cấp booking, nhiều đợt admission theo Adult/Discounted quota còn lại. Validation là đọc, confirmation mới mutation. Chỉ STAFF role với Staff–Cinema assignment; Manager/Admin không kế thừa quyền. Kiểm tuổi/Discounted/cancellation và cửa sổ inclusive current start −5 phút đến +20 phút. Retry cùng admission operation không tăng count; request đồng thời không vượt quota. Khóa account không tự vô hiệu paid QR. | UC-05, UC-19 |
| R-18 | Current authorization | Protected operation kiểm session, account state, current roles và đúng loại cinema assignment; internal access yêu cầu verified email. Không chờ access token hết hạn mới áp quyền hiện hành. Customer login không bắt email verified. Không gộp quyền role hoặc tự trao quyền viết từ actor association trên diagram. | UC-08C, UC-08D, UC-12, UC-13, UC-14A, UC-14B, UC-15A, UC-15B, UC-15C, UC-16A, UC-16B, UC-17, UC-18, UC-19 |
| R-19 | Single active session | Login conflict phải yêu cầu lựa chọn thay phiên qua challenge ngắn hạn, single-use và bound expected session. Cancel giữ phiên cũ. Confirm thay atomically; stale challenge không revoke phiên mới hơn. SAD C03 dùng JWT access + hashed refresh token, rotation nguyên tử; refresh đã consumed bị từ chối. Logout/account lock revoke server session; unlock không hồi sinh phiên. | UC-08C, UC-08D, UC-18 |
| R-20 | Verification email | Registration chỉ cấp permanent Customer membership, không tự login. Token verification unpredictable, hashed, single-use; mặc định 24 giờ. Resend tối thiểu 60 giây; pending email-change reservation tối đa 72 giờ từ original request; với email change, expiry=min(lastSent+validity,originalRequest+72h). Primary email của account đã đăng ký không bị giải phóng vì verification token hết hạn. Mỗi account tối đa một active pending email-change request; thay request phải kết thúc và giải phóng reservation cũ atomically. Sau absolute expiry, cùng account chờ 24 giờ trước khi reserve lại cùng target. Resend/replacement/cancel không bypass giới hạn. Primary email giữ nguyên tới verify thành công, user ID không đổi; email change cần reauthentication và báo email cũ sau thành công. | UC-08A, UC-08B, UC-18 |
| R-21 | Danh mục và lịch sử | Public list coming_soon/now_showing; ended có thể xem detail trực tiếp, archived không public. UC-06A tìm title không phân biệt hoa thường, filter status/genre, sort release_date DESC rồi movie.id DESC (release_date null cuối), backend pagination mặc định 12. General catalogue không tự lọc cinema; cinema-specific chỉ gồm phim public có suất đang bán tại cinema đó. Admin write movies/genres; liên kết movie–genre thuộc UC-12. Genre inactive không xóa liên kết lịch sử nhưng không được chọn cho liên kết mới. Không hard-delete dữ liệu lịch sử tài chính/admission. | UC-06A, UC-06B, UC-12, UC-13 |
| R-22 | Cinema và room | Admin quản lý cinema/room identity/state. Manager chỉ sửa cinema minimum turnaround thuộc assignment; mặc định 15 phút, cinema-level, không room override. Layout/seat operational state: Admin hoặc assigned Manager; Staff read-only. Thay đổi cấu hình phải kiểm dependencies/obligations và không tự hủy booking/refund. Structural layout bị khóa ở đúng room khi có unfinished showtime với pending/held hoặc confirmed/sold booking; unfinished showtime trống không tự khóa mọi sửa đổi, completed history không tự kích hoạt freeze nhưng phải được bảo toàn. Retirement không hard-delete seat hoặc tái dùng ID; display-only metadata chỉ được sửa nếu không ảnh hưởng identity/geometry/type/eligibility/pricing/history. | UC-14A, UC-14B, UC-15A, UC-15B |
| R-23 | Pricing management | Admin sửa shared seat types/surcharges và thông tin mô tả ticket categories; đúng hai category bắt buộc, không thêm loại thứ ba/xóa/disable. Giá base theo cinema do Admin hoặc assigned Manager; Staff read-only. Cấu hình khác snapshot giá đã mua. | UC-15C, UC-16A, UC-16B |
| R-24 | Showtime scheduling | Admin/assigned Manager lập lịch, Staff read. Kiểm room/cinema/movie eligibility, overlap và turnaround; snapshot duration khi tạo, sửa duration movie không đổi suất cũ. Reschedule có kiểm điều kiện, giữ purchased-start và queue thông báo. Cancel bị chặn khi còn hold/confirmed booking/payment/refund obligations chưa xử lý; không auto-cancel/refund hàng loạt. Cancelled showtime không trực tiếp reactivate trong MVP. | UC-17 |
| R-25 | Account administration | Admin quản lý internal roles, lock/unlock, assignments; bảo vệ permanent Customer membership và last active Admin. Manager chỉ quản lý Staff–Cinema assignment trong cinema được giao, không grant role hoặc quản lý Manager assignment. Account lock không xóa giao dịch/paid ticket. | UC-18 |
| R-26 | Informational AI | Promethe-Nous chỉ cung cấp thông tin từ nguồn được phép, phân biệt doanh thu/lượt xem và freshness. Không tạo/đổi booking/payment/ticket, không tự truy cập booking cá nhân hoặc báo cáo nội bộ. Theo SAD C12, history giới hạn trong backend memory, không PostgreSQL; clear/expiry/restart có thể mất, không truy cập history bằng session đã revoke. Nguồn ngoài, RAG/pgvector và model cụ thể không tự coi đã triển khai. | UC-11 |

## Đặc tả 28 Use Case

### UC-01 — Reserve Seats

#### Mô tả, tác nhân và trigger

Tạo reservation nguyên tử từ selection hợp lệ, bảo toàn giá và deadline đã chấp nhận.

- Tác nhân: Guest / Customer.
- Trigger: Khách nhấn Tiếp tục từ Checkout Step 1 sang Step 2.
- Domain: D1; component chịu trách nhiệm/phối hợp: C08.

#### Tiền điều kiện

Guest/Customer có selection còn hạn, đủ contact/acknowledgements của Step 1; showtime và seats thuộc đúng ngữ cảnh.

#### Luồng cơ bản

1. Backend xác minh principal/Guest authority, selection và idempotency key/fingerprint.
2. Kiểm selection deadline gốc, showtime hiện hành, room/seat operational state, quantities 1..max và seat count khớp.
3. Tính lại quotation và kiểm giá/giờ khách đã chấp nhận; thay đổi phải báo khách trước commit.
4. Khóa và đọc lại dữ liệu cần thiết, kiểm tất cả ghế chưa có allocation hiệu lực; không gọi mạng trong business transaction.
5. Trong một transaction tạo pending booking, toàn bộ held allocations, contact/category/price/purchased-start snapshots và liên kết selection với outcome.
6. Trả booking và deadline kế thừa; UI vào Step 2 chỉ sau khi nhận outcome hợp lệ.

#### Luồng thay thế và ngoại lệ

- **A1:** Cùng key/fingerprint: trả outcome đã lưu với current state; khác payload: conflict, không sửa booking cũ.
- **A2:** Hết selection, state/quantity/seat không hợp lệ hoặc có ghế xung đột: không tạo booking/hold một phần.
- **A3:** Giá hoặc official start thay đổi: yêu cầu xem/chấp nhận lại theo policy, không reset deadline.
- **A4:** Concurrent request hoặc mất response: unique/lock/idempotency bảo vệ outcome, client truy vấn/retry cùng identity; không coi timeout là rollback.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-01, R-02, R-03, R-04, R-05, R-10. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Thành công có một pending booking, đủ held seats và snapshots; deadline không gia hạn. Thất bại trước commit không để allocation một phần; outcome không rõ phải được truy vấn lại.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-01 Reserve Seats**; SAD Logical View C08. Yêu cầu dẫn xuất: F-UC-01; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-02 — Payment Webhook

#### Mô tả, tác nhân và trigger

Bảo toàn kết quả payment đã xác minh và chỉ áp dụng vào booking đủ điều kiện.

- Tác nhân: Payment Gateway.
- Trigger: MoMo webhook đến hoặc worker xử lý durable receipt.
- Domain: D9; component chịu trách nhiệm/phối hợp: C09.

#### Tiền điều kiện

Có thể xác định payment/reference cần xử lý; provider request phải qua xác minh trước khi ảnh hưởng nghiệp vụ.

#### Luồng cơ bản

1. Ingress tiếp nhận raw provider request và xác minh chữ ký/nguồn theo adapter; không dùng browser return làm chứng cứ.
2. Xác định reference/event identity và lưu receipt hợp lệ bền vững trước ACK theo provider contract, deduplicate nhận lại.
3. Worker claim receipt; nếu cần query provider, thực hiện ngoài business transaction.
4. Khóa/đọc lại booking, allocations, attempt, current deadlines/grace, amount/currency và kết quả đã có.
5. Ghi financial truth được xác minh; nếu eligible thì atomically applied, confirmed, sold, một ticket/QR, persisted notification request và audit bắt buộc.
6. Đánh dấu xử lý nhất quán với nghiệp vụ, commit; C14/C15 gửi email sau commit.

#### Luồng thay thế và ngoại lệ

- **A1:** Chữ ký/nguồn sai: không đưa vào workflow tác động tài chính; security log đã lọc không đồng nghĩa receipt hợp lệ.
- **A2:** Đã xác minh thu tiền nhưng amount/currency/reference đối chiếu không đạt: giữ evidence có thể quy thuộc an toàn, review; không apply nhầm booking.
- **A3:** Duplicate/out-of-order: không apply lần hai hoặc downgrade succeeded; mâu thuẫn cần review.
- **A4:** Late success hoặc booking không eligible: không reclaim/reconfirm; review/refund obligation theo UC-04/04A.
- **A5:** Transaction lỗi: không đánh processed trước nghiệp vụ commit; retry idempotent. Email lỗi sau commit: retry delivery, không rollback booking.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-07, R-08, R-09, R-10, R-11. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Eligible application commit đồng bộ; ineligible success vẫn giữ financial truth và hướng xử lý. Không phát hành QR trùng hoặc tự chuyển failed khi chưa xác minh.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-02 Payment Webhook**; SAD Logical View C09. Yêu cầu dẫn xuất: F-UC-02; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-03 — Release Expired Bookings

#### Mô tả, tác nhân và trigger

Giải phóng booking hết hạn chỉ khi payment safety cho phép.

- Tác nhân: System Worker.
- Trigger: Worker định kỳ hoặc inline cleanup hợp lệ.
- Domain: D9; component chịu trách nhiệm/phối hợp: C08, C14.

#### Tiền điều kiện

Có candidate pending booking đến hạn; candidate scan không thay thế kiểm tra state/time dưới lock.

#### Luồng cơ bản

1. Tìm pending candidates và xác định deadline hiệu lực: selection gốc trước initiation hoặc payment window sau initiation.
2. Khóa theo protocol, đọc lại state/time/attempts; bỏ qua booking đã thay đổi.
3. Nếu có initiated/unresolved payment, đánh giá window/grace và phối hợp UC-04; không coi hết grace là chưa thu tiền.
4. Nếu cần provider query, nhả business transaction trước, sau phản hồi mở transaction mới và kiểm lại.
5. Chỉ khi đủ điều kiện release, atomically pending→expired và held→released, lưu lịch sử/audit theo yêu cầu.

#### Luồng thay thế và ngoại lệ

- **A1:** Grace hoặc unknown chưa được giải quyết: giữ allocations, retry/review có theo dõi.
- **A2:** Payment confirmation commit trước: không release confirmed; release commit trước thì late success không reclaim.
- **A3:** Inline cleanup không được query provider trong lock hoặc vượt payment safety.
- **A4:** Worker restart/multi-instance: claim/lease và idempotency theo SAD; lỗi transaction rollback, chu kỳ sau đọc lại.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-07, R-10, R-11. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Release thành công làm ghế khả dụng sau commit; trường hợp chưa xác định giữ nguyên và có recovery path. Timer UI không trực tiếp thay state.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-03 Release Expired Bookings**; SAD Logical View C08, C14. Yêu cầu dẫn xuất: F-UC-03; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-04 — Payment Reconciliation

#### Mô tả, tác nhân và trigger

Đối soát payment chưa chắc chắn hoặc mâu thuẫn và định tuyến nghĩa vụ.

- Tác nhân: System Worker.
- Trigger: Worker đến hạn hoặc phát hiện mismatch/late success.
- Domain: D9; component chịu trách nhiệm/phối hợp: C09, C14.

#### Tiền điều kiện

Payment cần xác minh hoặc hồ sơ review còn mở; có định danh/evidence phù hợp.

#### Luồng cơ bản

1. Chọn hồ sơ và lấy provider reference đã lưu.
2. Query trạng thái ngoài transaction giữ business locks, xác minh phản hồi.
3. Khóa/đọc lại dữ liệu mới nhất, không để phản hồi cũ ghi đè succeeded.
4. Nếu success: bảo toàn evidence và đánh giá application theo UC-02; nếu failed/cancelled rõ ràng: ghi kết quả hợp lệ.
5. Nếu không xác định: giữ unknown, lịch retry/giám sát; nếu thu tiền không thể apply: review hoặc refund obligation.
6. Commit kết quả cùng audit/hồ sơ xử lý phù hợp; chỉ resolve khi nghĩa vụ thực sự được giải quyết.

#### Luồng thay thế và ngoại lệ

- **A1:** Provider không trả lời: không tạo attempt mới hoặc giả failed.
- **A2:** Sai amount/currency, nhiều success hoặc dữ liệu mâu thuẫn: review, không force confirm.
- **A3:** Retry/concurrent worker: không duplicate application/refund obligation.
- **A4:** Refund còn pending/unknown: chưa được đóng resolved chỉ vì đã gửi yêu cầu.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-07, R-08, R-09, R-10, R-11, R-15. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Kết quả đã xác minh được lưu hoặc hồ sơ vẫn có trạng thái chưa chốt cùng retry/review; không thay ghế/vé trái quy tắc.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-04 Payment Reconciliation**; SAD Logical View C09, C14. Yêu cầu dẫn xuất: F-UC-04; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-04A — Refund

#### Mô tả, tác nhân và trigger

Theo dõi nghĩa vụ hoàn toàn bộ tiền và ghi nhận completion có bằng chứng.

- Tác nhân: Manager / Admin; system workflow.
- Trigger: Nghĩa vụ từ reconciliation hoặc approved rescheduling cancellation; actor ghi kết quả hoàn bên ngoài.
- Domain: D9; component chịu trách nhiệm/phối hợp: C10.

#### Tiền điều kiện

Có payment thành công và căn cứ refund hợp lệ; actor completion là assigned Manager hoặc Admin; không đã hoàn thành nghĩa vụ đó.

#### Luồng cơ bản

1. Xác định origin và payment, toàn bộ amount/currency, hồ sơ cancellation nếu có.
2. Tạo/đọc nghĩa vụ hiện có với identity ổn định; không tạo duplicate vì retry.
3. Việc chuyển tiền thực hiện bên ngoài ứng dụng trong demo; trạng thái accepted/đang xử lý không phải completed.
4. Actor có quyền gửi reference/evidence completion; backend xác minh quyền, số tiền/tiền tệ, payment và evidence theo quy trình.
5. Khóa/đọc lại refund và các record liên quan; ghi succeeded chỉ khi đủ chứng cứ.
6. Với rescheduling: atomically completed request, cancelled booking, voided ticket, released sold seats và audit. Với reconciliation: resolve nghĩa vụ tài chính theo origin, không áp mặc định tác động rescheduling.

#### Luồng thay thế và ngoại lệ

- **A1:** Staff hoặc Manager ngoài cinema: từ chối completion.
- **A2:** Evidence thiếu/mismatch, chưa hoàn đủ hoặc kết quả unknown/failed: không đánh succeeded, không release vì giả định.
- **A3:** Replay/duplicate reference: không hoàn hoặc áp completion effects hai lần.
- **A4:** Confirmed booking đã có admission hoặc hồ sơ không còn cho phép approval: không bypass review để refund cancellation.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-10, R-15, R-16. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Completion hợp lệ có evidence và effects đúng origin; chưa xác minh thì giữ nghĩa vụ mở. Không partial refund, không gọi Refund API như tính năng demo đã có.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-04A Refund**; SAD Logical View C10. Yêu cầu dẫn xuất: F-UC-04A; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-05 — Validate Ticket

#### Mô tả, tác nhân và trigger

Kiểm vé và ghi nhận admission theo booking-level category entitlements.

- Tác nhân: Assigned Staff.
- Trigger: Staff quét QR, xem validation rồi xác nhận nhóm khách.
- Domain: D3; component chịu trách nhiệm/phối hợp: C11.

#### Tiền điều kiện

Staff có active verified internal access, STAFF role và Staff–Cinema assignment đúng; ticket/booking đủ điều kiện hiện hành.

#### Luồng cơ bản

1. Giải mã/tra cứu booking QR trong ngữ cảnh cinema/showtime; validation chỉ đọc.
2. Hiển thị current screening, Adult/Discounted purchased/admitted/remaining và hạn chế liên quan.
3. Staff kiểm tuổi và eligibility Discounted của nhóm thực đến, nhập batch counts.
4. Backend revalidate current time trong cửa sổ inclusive −5/+20 phút, state, assignment, cancellation restriction và quota dưới transaction.
5. Commit admission với operation identity, category counts và audit; admission đầu tiên tự reject pending_review cancellation atomically.
6. Trả remaining counts; một QR có thể dùng tiếp cho nhóm còn lại trong điều kiện hợp lệ.

#### Luồng thay thế và ngoại lệ

- **A1:** Wrong cinema/time, voided ticket, không đủ tuổi/Discounted evidence, approved awaiting refund hoặc quota không đủ: từ chối, không tăng count.
- **A2:** Duplicate operation: trả outcome cũ; operation mới/concurrent phải kiểm quota mới nhất.
- **A3:** Pending review chưa được approve không tự cấm admission; approval race phải được tuần tự hóa.
- **A4:** Mất response sau commit: replay không tăng lại. Account purchaser bị lock không tự invalidate paid QR.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-04, R-10, R-14, R-17. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Counts không vượt entitlements; failed validation/confirmation không tiêu quota. Không dùng một cờ used duy nhất làm hết toàn bộ booking ngay ở lần đầu.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-05 Validate Ticket**; SAD Logical View C11. Yêu cầu dẫn xuất: F-UC-05; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-06A — Browse Movies

#### Mô tả, tác nhân và trigger

Duyệt danh mục phim công khai và tìm phim phù hợp.

- Tác nhân: Guest / Customer.
- Trigger: Guest/Customer mở danh sách hoặc đổi điều kiện tìm/lọc.
- Domain: D1; component chịu trách nhiệm/phối hợp: C04.

#### Tiền điều kiện

Không yêu cầu login; catalogue có thể đọc từ nguồn hệ thống.

#### Luồng cơ bản

1. Nhận tìm kiếm title không phân biệt hoa thường, filter genre/status hợp lệ; chỉ status coming_soon/now_showing.
2. Truy vấn public catalogue; general list không ngầm lọc cinema, cinema-specific cần suất đang bán tại cinema đó; không lộ ended/archived trong list.
3. Sort release_date DESC, movie.id DESC, release_date null cuối; backend pagination default page size 12, validate parameters.
4. Khách chọn phim để chuyển UC-06B.

#### Luồng thay thế và ngoại lệ

- **A1:** Không kết quả: hiển thị empty state, không phải lỗi transaction.
- **A2:** Query không hợp lệ hoặc dependency lỗi: báo phù hợp, cho thử lại.
- **A3:** Dữ liệu cũ không chứng minh lịch hoặc ghế còn bán.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-21. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Chỉ đọc danh mục; không tạo selection/booking/payment.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-06A Browse Movies**; SAD Logical View C04. Yêu cầu dẫn xuất: F-UC-06A; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-06B — View Movie Details

#### Mô tả, tác nhân và trigger

Xem chi tiết phim và tiếp tục khám phá lịch chiếu.

- Tác nhân: Guest / Customer.
- Trigger: Khách mở movie detail từ danh sách hoặc link trực tiếp.
- Domain: D1; component chịu trách nhiệm/phối hợp: C04.

#### Tiền điều kiện

Movie ID hợp lệ và phim được phép xem công khai.

#### Luồng cơ bản

1. Backend tra cứu movie, kiểm public visibility.
2. Hiển thị metadata, genres và các trường có dữ liệu; không bịa trường thiếu.
3. Phim ended có thể xem detail trực tiếp, nhưng không tự có suất bán.
4. Khách chọn xem/chọn lịch qua UC-07.

#### Luồng thay thế và ngoại lệ

- **A1:** Archived/không tồn tại: không tiết lộ nội dung nội bộ.
- **A2:** Thiếu media tùy chọn: UI có fallback phù hợp.
- **A3:** Refresh detail áp navigation/selection policy R-03; không âm thầm reset timer của booking đã tạo.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-21. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Khách nhận thông tin phim; chưa hold ghế và chưa có payment.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-06B View Movie Details**; SAD Logical View C04. Yêu cầu dẫn xuất: F-UC-06B; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-07 — Browse Showtimes

#### Mô tả, tác nhân và trigger

Tra cứu/chọn showtime theo phim, cinema và ngày.

- Tác nhân: Guest / Customer.
- Trigger: Khách mở lịch hoặc chọn bộ lọc.
- Domain: D1; component chịu trách nhiệm/phối hợp: C06.

#### Tiền điều kiện

Không bắt login; các filter thuộc contract hỗ trợ.

#### Luồng cơ bản

1. Validate movie/cinema/date, diễn giải ngày theo timezone cinema.
2. Đọc showtimes đang đủ điều kiện hiển thị/bán và thông tin cinema/room.
3. Trả giờ chính thức, ngữ cảnh cần thiết và empty state nếu không có.
4. Khi chọn showtime, đọc lại eligibility trước khi chuyển sang chọn quantities/seats.

#### Luồng thay thế và ngoại lệ

- **A1:** Suất bị hủy/hết điều kiện: không tiếp tục theo dữ liệu cache.
- **A2:** Hết ghế hoặc state đổi sau đọc: reservation UC-01 quyết định cuối.
- **A3:** Lỗi truy vấn: không tạo hold một phần.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng . Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Chuyển ngữ cảnh suất, chưa đảm bảo reservation; UC-01 vẫn revalidate.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-07 Browse Showtimes**; SAD Logical View C06. Yêu cầu dẫn xuất: F-UC-07; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-08A — Register Customer Account

#### Mô tả, tác nhân và trigger

Đăng ký Customer account theo identity/uniqueness rules.

- Tác nhân: Guest.
- Trigger: Guest gửi form đăng ký.
- Domain: D8; component chịu trách nhiệm/phối hợp: C03.

#### Tiền điều kiện

Guest cung cấp thông tin đăng ký hợp lệ; email không xung đột ownership/reservation.

#### Luồng cơ bản

1. Validate thông tin, mật khẩu theo identity policy và email chuẩn hóa.
2. Kiểm uniqueness cùng pending email reservations dưới transaction phù hợp.
3. Tạo account với permanent Customer membership; không cấp internal roles.
4. Lưu verification request/token hash và notification request theo workflow; commit trước delivery.
5. Trả kết quả đăng ký và hướng dẫn verification/login riêng.

#### Luồng thay thế và ngoại lệ

- **A1:** Email conflict/validation lỗi: không tạo account một phần, phản hồi không lộ dữ liệu nhạy cảm.
- **A2:** Email delivery lỗi: giữ account/request đã commit, hỗ trợ resend theo cooldown.
- **A3:** Duplicate submission không được tạo account trùng hoặc auto-login.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-20. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Account Customer được tạo hợp lệ; registration không tự tạo authenticated session. Verification độc lập UC-08B.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-08A Register Customer Account**; SAD Logical View C03. Yêu cầu dẫn xuất: F-UC-08A; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-08B — Verify Email Address

#### Mô tả, tác nhân và trigger

Xác minh email và thay primary email an toàn.

- Tác nhân: Account holder / verification link holder.
- Trigger: Mở link verification, yêu cầu resend hoặc đổi email.
- Domain: D8; component chịu trách nhiệm/phối hợp: C03.

#### Tiền điều kiện

Token/request hoặc account authority hợp lệ; đổi email yêu cầu approved reauthentication.

#### Luồng cơ bản

1. Đọc account/request/target email; phân biệt verify primary hiện tại và proposed replacement.
2. Kiểm token hash, single-use, expiry, ownership/reservation; bảo toàn user ID.
3. Consume token và cập nhật verified/primary email atomically; giải phóng reservation thích hợp.
4. Sau email replacement thành công, queue thông báo bảo mật tới email cũ.
5. Resend hợp lệ tạo token mới, vô hiệu token cũ; với pending email change, cập nhật expiry bị chặn bởi absolute reservation limit. Verification expiry không giải phóng primary email đã thuộc account đăng ký.
6. Trả authoritative email/request state; email transport thực hiện sau commit.

#### Luồng thay thế và ngoại lệ

- **A1:** Token invalid/used/expired hoặc email bị chiếm: không thay primary.
- **A2:** Resend trước 60 giây: từ chối; default validity 24h bị giới hạn bởi original request+72h.
- **A3:** Absolute expiry: không kéo dài bằng replacement/cancel/resend; cùng account chờ 24h trước reserve lại cùng target.
- **A4:** Delivery lỗi không làm token giả thành verified hoặc đổi email; retry/resend theo policy.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-20. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Thành công verified và primary state đúng, token không reuse; failure giữ primary cũ. Không đổi user identity/history.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-08B Verify Email Address**; SAD Logical View C03. Yêu cầu dẫn xuất: F-UC-08B; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-08C — Login

#### Mô tả, tác nhân và trigger

Xác thực và thiết lập đúng một active session có lựa chọn thay thế rõ ràng.

- Tác nhân: Account holder.
- Trigger: Người dùng đăng nhập; hoặc xác nhận challenge thay session.
- Domain: D8; component chịu trách nhiệm/phối hợp: C03.

#### Tiền điều kiện

Credentials hợp lệ, account cho phép login; Customer không bắt verified email, internal operations vẫn kiểm verified.

#### Luồng cơ bản

1. Backend xác minh credentials/account, không tin roles từ client.
2. Nếu chưa có active session, tạo server session và access/refresh pair theo SAD C03.
3. Nếu có session, trả conflict/challenge ngắn hạn single-use bound expected session; người dùng chọn cancel hoặc replace.
4. Cancel giữ session cũ; replace revalidate challenge/account/current expected session rồi atomically revoke cũ và tạo mới.
5. Trả quyền điều hướng theo current roles; protected operations vẫn kiểm current authority.
6. Refresh theo SAD rotate hash atomically trong cùng session; không coi refresh là numbered UC mới.

#### Luồng thay thế và ngoại lệ

- **A1:** Credentials/account không hợp lệ: không tạo session.
- **A2:** Challenge hết hạn/đã dùng/stale do login khác: không revoke phiên mới hơn.
- **A3:** Refresh token consumed/revoked hoặc session không active: từ chối.
- **A4:** Role/assignment thay đổi: token cũ không bảo đảm operation tiếp theo được phép.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-18, R-19. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Tối đa một active session; không silently replace. Login failure/cancel không thay phiên cũ hợp lệ.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-08C Login**; SAD Logical View C03. Yêu cầu dẫn xuất: F-UC-08C; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-08D — Logout

#### Mô tả, tác nhân và trigger

Kết thúc session hiện tại ở backend và client.

- Tác nhân: Authenticated user.
- Trigger: Người dùng chọn logout.
- Domain: D8; component chịu trách nhiệm/phối hợp: C03.

#### Tiền điều kiện

Có session cần kết thúc hoặc retry cùng logout context.

#### Luồng cơ bản

1. Xác định current session qua authentication context.
2. Revoke server session/refresh authority theo SAD, không chỉ xóa localStorage.
3. Client xóa thông tin phiên cục bộ và điều hướng public.
4. Protected request/refresh tiếp theo bằng session cũ bị từ chối; Guest public access vẫn còn.

#### Luồng thay thế và ngoại lệ

- **A1:** Session đã hết/revoked: xử lý an toàn, không revoke session khác ngoài phạm vi.
- **A2:** Response mất/lỗi mạng: UI không được tuyên bố server revoke nếu chưa xác nhận; có thể thử lại.
- **A3:** Logout không tự cancel pending booking/payment hoặc invalidate paid ticket.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-18, R-19. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Logout được xác nhận thì session cũ không dùng lại được; dữ liệu nghiệp vụ được bảo toàn.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-08D Logout**; SAD Logical View C03. Yêu cầu dẫn xuất: F-UC-08D; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-09 — Checkout and Payment

#### Mô tả, tác nhân và trigger

Điều phối Guest/Customer checkout và khởi tạo MoMo cho booking hiện có.

- Tác nhân: Guest / Customer.
- Trigger: Khách vào checkout, tiếp tục sang payment hoặc nhấn xác nhận thanh toán.
- Domain: D1; component chịu trách nhiệm/phối hợp: C08, C09.

#### Tiền điều kiện

Selection hoặc booking hợp lệ theo bước hiện tại và authority của khách.

#### Luồng cơ bản

1. Step 1 hiển thị selection/deadline gốc; thu full name, phone, email và terms/age acknowledgements.
2. Khách nhấn Continue; UC-09 include UC-01 để revalidate và atomically tạo booking/holds; không tạo trước chỉ vì chọn ghế.
3. Step 2 dùng booking/snapshot đã commit, hiển thị tổng tiền và thời gian còn lại.
4. Sau explicit payment confirmation, backend kiểm quyền, state/deadline và attempt hiện có; tạo/claim initiation bền vững, không có hai open attempts.
5. Gọi MoMo ngoài business transaction; áp kết quả init có điều kiện, không ghi đè webhook đã xử lý; cửa sổ hợp lệ 10 phút thay deadline gốc.
6. Theo dõi state backend; return URL chỉ điều hướng. UC-02/04 quyết định confirmation/ticket; có thể resend ticket email qua workflow được phép.

#### Luồng thay thế và ngoại lệ

- **A1:** Giá/giờ/selection không còn hợp lệ: quay lại xác nhận theo policy, không reset timer.
- **A2:** Reload Step 1/2 phục hồi eligible state; không tạo booking/attempt mới.
- **A3:** Trước initiation commit: explicit pending cancellation qua UC-10; sau initiation không cho cancel để đổi ghế.
- **A4:** Provider timeout/unknown: giữ attempt và reconcile; không retry bằng attempt mới.
- **A5:** Init response đến sau webhook: không downgrade; success page không tự confirm.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-01, R-02, R-03, R-04, R-05, R-06, R-07, R-08, R-09, R-10, R-12. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Khởi tạo có durable identity/state; booking chỉ confirmed từ verified eligible application. Không nhầm payment method selection với initiation.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-09 Checkout and Payment**; SAD Logical View C08, C09. Yêu cầu dẫn xuất: F-UC-09; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-10 — Customer Booking Lifecycle

#### Mô tả, tác nhân và trigger

Tra cứu booking/ticket và thực hiện cancellation đúng từng lifecycle.

- Tác nhân: Guest / Customer with resource authority.
- Trigger: Khách xem booking, resend vé, hủy pending, gửi/rút rescheduling cancellation.
- Domain: D2; component chịu trách nhiệm/phối hợp: C08, C10, C11.

#### Tiền điều kiện

Customer ownership hoặc Guest proof hợp lệ; state được đọc lại tại thao tác.

#### Luồng cơ bản

1. Xác minh resource authority, trả booking/payment/ticket/cancellation state được phép xem.
2. Nếu hủy pending: cảnh báo và yêu cầu confirm; chỉ commit cancelled+released khi payment initiation chưa commit.
3. Nếu confirmed: kiểm current start khác purchased snapshot, trước current start, zero admissions và toàn booking; tạo pending_review request.
4. Khách có thể withdraw chỉ khi request còn pending_review.
5. Trả trạng thái review/refund; approved awaiting refund chặn admission nhưng không tự release.
6. Hiển thị completion theo backend; ticket access/redelivery không tạo QR mới.

#### Luồng thay thế và ngoại lệ

- **A1:** Biết email/code không đủ Guest authority; từ chối truy cập vượt quyền.
- **A2:** Sau payment initiation: từ chối pending cancellation, không dùng gateway Cancel/Query để bypass.
- **A3:** Current start bằng snapshot: không cho request mới; request hợp lệ trước đó có thể tiếp tục review.
- **A4:** First admission tự reject pending request; approved/completed/rejected/withdrawn không withdraw như pending.
- **A5:** Client đóng tab/back/hết countdown không tự đổi booking state.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-01, R-03, R-10, R-12, R-13, R-14, R-16. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Tra cứu không mutate; cancellation theo nhánh hợp lệ atomically. Confirmed booking chỉ cancelled/released sau verified qualifying refund completion.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-10 Customer Booking Lifecycle**; SAD Logical View C08, C10, C11. Yêu cầu dẫn xuất: F-UC-10; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-11 — Chat with Promethe-Nous

#### Mô tả, tác nhân và trigger

Trả lời thông tin phim và hướng dẫn quy trình trong phạm vi được phép.

- Tác nhân: Guest / Customer.
- Trigger: Guest/Customer gửi câu hỏi.
- Domain: D1; component chịu trách nhiệm/phối hợp: C12.

#### Tiền điều kiện

Nguồn thông tin/context được phép truy cập; không yêu cầu quyền mutate nghiệp vụ.

#### Luồng cơ bản

1. Xác định ý định và phạm vi phim/cinema/time/metric cần hỏi.
2. Truy xuất thông tin từ nguồn được phép; lịch/giá thay đổi cần dữ liệu hiện hành.
3. Trả lời nêu phạm vi, freshness/nguồn khi có ý nghĩa; thiếu dữ liệu phải nói rõ.
4. Giữ ngữ cảnh có giới hạn trong backend memory theo SAD; clear/expiry/restart có thể mất.
5. Chỉ hướng dẫn khách sang luồng chính để booking/payment/ticket access.

#### Luồng thay thế và ngoại lệ

- **A1:** Câu hỏi mơ hồ: hỏi rõ hoặc nêu giả định; không đổi doanh thu thành lượt xem.
- **A2:** Không có nguồn/provider lỗi: không bịa số liệu.
- **A3:** Yêu cầu dữ liệu booking cá nhân/báo cáo nội bộ hoặc mutation: không cấp quyền qua hội thoại.
- **A4:** Session revoked không được tiếp tục đọc history cũ.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-26. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Có câu trả lời grounded hoặc thông báo giới hạn; không thay booking/payment/seat/ticket, không lưu history PostgreSQL như tính năng đã chốt.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-11 Chat with Promethe-Nous**; SAD Logical View C12. Yêu cầu dẫn xuất: F-UC-11; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-12 — Manage Movie Catalog

#### Mô tả, tác nhân và trigger

Quản lý movie metadata, lifecycle và movie–genre associations.

- Tác nhân: Admin.
- Trigger: Admin tạo/sửa movie hoặc association/state được phép.
- Domain: D4; component chịu trách nhiệm/phối hợp: C04.

#### Tiền điều kiện

Admin có current verified internal authority; dữ liệu tham chiếu hợp lệ.

#### Luồng cơ bản

1. Load dữ liệu hiện hành và kiểm Admin permission.
2. Validate thông tin phim, lifecycle và active genres cho association mới.
3. Kiểm dependencies của thay đổi, không sửa lịch sử đã mua hoặc snapshot duration showtime cũ.
4. Commit thay đổi hợp lệ và audit; phản hồi phiên bản hiện hành.

#### Luồng thay thế và ngoại lệ

- **A1:** Manager/Staff write: từ chối.
- **A2:** Genre inactive không dùng cho association mới, historical association không bị xóa ngầm.
- **A3:** Concurrent update/dependency conflict: không overwrite mù.
- **A4:** Không hard-delete history hoặc tự reschedule showtimes khi sửa duration movie.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-18, R-21. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Movie/associations cập nhật trong phạm vi; booking/showtime snapshots lịch sử được giữ.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-12 Manage Movie Catalog**; SAD Logical View C04. Yêu cầu dẫn xuất: F-UC-12; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-13 — Manage Genres

#### Mô tả, tác nhân và trigger

Quản lý định nghĩa genre và activation độc lập movie association.

- Tác nhân: Admin.
- Trigger: Admin tạo/sửa/activate/deactivate genre.
- Domain: D4; component chịu trách nhiệm/phối hợp: C04.

#### Tiền điều kiện

Admin có quyền; genre data hợp lệ.

#### Luồng cơ bản

1. Load danh sách/genre và kiểm Admin authority.
2. Validate tên/định nghĩa và uniqueness theo quy tắc nguồn.
3. Ghi thay đổi/state với concurrency protection và audit.
4. Trả catalogue hiện hành; association phim được quản lý riêng ở UC-12.

#### Luồng thay thế và ngoại lệ

- **A1:** Manager/Staff write hoặc permanent delete không hỗ trợ: từ chối.
- **A2:** Deactivate không xóa historical movie links.
- **A3:** Conflict/invalid input: không cập nhật một phần.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-18, R-21. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Genre definition/state đúng; không tự sửa movie associations hoặc lịch sử.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-13 Manage Genres**; SAD Logical View C04. Yêu cầu dẫn xuất: F-UC-13; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-14A — Manage Cinemas

#### Mô tả, tác nhân và trigger

Quản lý cinema identity/state và cinema minimum turnaround theo đúng quyền.

- Tác nhân: Admin; scoped Manager; Staff read.
- Trigger: Actor xem cinema; Admin sửa identity/state; Manager sửa turnaround được phép.
- Domain: D5; component chịu trách nhiệm/phối hợp: C05.

#### Tiền điều kiện

Internal current authority; Manager/Staff có cinema scope khi truy cập theo assignment.

#### Luồng cơ bản

1. Xác định operation và role/cinema assignment.
2. Admin tạo/sửa cinema identity/state; Manager chỉ sửa turnaround tại cinema được giao.
3. Validate turnaround cinema-level, mặc định 15 phút, cùng dependencies/lịch liên quan.
4. Kiểm lại scope và dependencies trước commit, lưu audit.

#### Luồng thay thế và ngoại lệ

- **A1:** Manager sửa identity/state hoặc Staff write: từ chối.
- **A2:** Thay turnaround xung đột lịch/nghĩa vụ: không silently áp thay đổi làm lịch sai.
- **A3:** Deactivate không tự cancel/refund các booking.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-18, R-22. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Cinema data hợp lệ và lịch sử giữ nguyên; không tạo room-level turnaround override.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-14A Manage Cinemas**; SAD Logical View C05. Yêu cầu dẫn xuất: F-UC-14A; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-14B — Manage Screening Rooms

#### Mô tả, tác nhân và trigger

Quản lý room identity/state trong cinema.

- Tác nhân: Admin; scoped Manager/Staff read.
- Trigger: Xem hoặc Admin gửi room create/update/state operation.
- Domain: D5; component chịu trách nhiệm/phối hợp: C05.

#### Tiền điều kiện

Admin cho write; assigned Manager/Staff chỉ read.

#### Luồng cơ bản

1. Kiểm actor operation và parent cinema.
2. Validate room identity và uniqueness trong cinema theo nguồn.
3. Kiểm showtime/layout/booking dependencies trước thay state.
4. Commit dữ liệu/audit hợp lệ; scheduling dùng turnaround của cinema.

#### Luồng thay thế và ngoại lệ

- **A1:** Manager/Staff gửi write: từ chối.
- **A2:** Đang có dependencies không tương thích: reject thay đổi, không tự hủy suất/booking.
- **A3:** Không thêm room override turnaround từ form cũ.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-18, R-22. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Room cập nhật an toàn; layout/state ghế thuộc UC-15A/B.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-14B Manage Screening Rooms**; SAD Logical View C05. Yêu cầu dẫn xuất: F-UC-14B; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-15A — Configure Seat Layout

#### Mô tả, tác nhân và trigger

Cấu hình vị trí/nhãn/type assignment của physical seats.

- Tác nhân: Admin / scoped Manager; Staff read.
- Trigger: Actor mở layout và gửi thay đổi cấu trúc.
- Domain: D5; component chịu trách nhiệm/phối hợp: C05.

#### Tiền điều kiện

Admin hoặc Manager thuộc cinema; Staff chỉ read; room có thể thay đổi cấu trúc theo dependency rules.

#### Luồng cơ bản

1. Đọc room/layout hiện hành và scope.
2. Validate seat labels/positions, không trùng và type assignment hợp lệ.
3. Kiểm structural freeze: room có unfinished showtime với pending/held hoặc confirmed/sold booking thì chặn structural edit. Unfinished suất trống và completed history không tự gây freeze; vẫn bảo toàn history và phối hợp concurrent booking. Phân biệt layout edit với operational-state edit.
4. Commit toàn bộ thay đổi hợp lệ với audit/concurrency protection; retirement không hard-delete hoặc reuse seat ID. Display-only metadata chỉ được phép nếu không thay identity/layout/type/eligibility/pricing/history.

#### Luồng thay thế và ngoại lệ

- **A1:** Staff write, ngoài scope hoặc duplicate label/position: từ chối.
- **A2:** Thay cấu trúc gây ảnh hưởng obligations: chặn, không chuyển khách/ghế tự động.
- **A3:** Không dùng layout editing để bypass UC-15B.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-18, R-22. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Layout hợp lệ; không thay sold/held allocations hoặc historical seat snapshots trái quy tắc.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-15A Configure Seat Layout**; SAD Logical View C05. Yêu cầu dẫn xuất: F-UC-15A; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-15B — Change Seat Operational Status

#### Mô tả, tác nhân và trigger

Đổi khả năng vận hành của physical seat.

- Tác nhân: Admin / scoped Manager; Staff read.
- Trigger: Actor yêu cầu active/out-of-service theo operation hỗ trợ.
- Domain: D5; component chịu trách nhiệm/phối hợp: C05.

#### Tiền điều kiện

Admin/assigned Manager; ghế tồn tại; Staff read-only.

#### Luồng cơ bản

1. Load ghế và current cinema scope.
2. Kiểm allocations, showtimes và obligations liên quan.
3. Validate chuyển operational state, recheck dưới transaction.
4. Commit state/audit, phản hồi authoritative seat state.

#### Luồng thay thế và ngoại lệ

- **A1:** Ghế có obligations không cho phép đổi: từ chối; không release held/sold để ép thành công.
- **A2:** Staff write/Manager ngoài scope: từ chối.
- **A3:** Operational state không đồng nghĩa available/held/sold trong một showtime.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-18, R-22. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Physical operational state đổi hợp lệ; không tự refund/cancel booking.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-15B Change Seat Operational Status**; SAD Logical View C05. Yêu cầu dẫn xuất: F-UC-15B; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-15C — Manage Seat Types

#### Mô tả, tác nhân và trigger

Quản lý shared seat types và surcharge.

- Tác nhân: Admin write; permitted operational read.
- Trigger: Admin cập nhật definition/surcharge/state được hỗ trợ.
- Domain: D7; component chịu trách nhiệm/phối hợp: C05.

#### Tiền điều kiện

Admin write; Manager/Staff chỉ permitted read.

#### Luồng cơ bản

1. Load shared type và kiểm Admin.
2. Validate definition, surcharge là số nguyên VND không âm và dependencies theo nguồn.
3. Commit current config và audit.
4. Giá mới chỉ dùng cho quotation/reservation tiếp theo; snapshot booking đã mua không đổi.

#### Luồng thay thế và ngoại lệ

- **A1:** Manager/Staff write: từ chối.
- **A2:** Thay đổi/xóa loại được tham chiếu không được phép: reject, giữ history.
- **A3:** Không suy ra Couple hoặc nhiều entitlement từ một physical seat.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-05, R-18, R-23. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Cấu hình shared type hợp lệ, không hồi tố giá/nghĩa vụ.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-15C Manage Seat Types**; SAD Logical View C05. Yêu cầu dẫn xuất: F-UC-15C; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-16A — Manage Ticket Types

#### Mô tả, tác nhân và trigger

Duy trì mô tả hai ticket categories bắt buộc.

- Tác nhân: Admin write; permitted operational read.
- Trigger: Actor xem categories hoặc Admin sửa thông tin hỗ trợ.
- Domain: D7; component chịu trách nhiệm/phối hợp: C07.

#### Tiền điều kiện

Admin write mô tả được phép; Manager/Staff read.

#### Luồng cơ bản

1. Load Adult và Discounted như hai category nghiệp vụ cố định.
2. Admin sửa các field descriptive được nguồn cho phép.
3. Validate không đổi ngữ nghĩa/identity hai category hoặc thêm category khác.
4. Commit audit; cinema base prices vẫn do UC-16B quản lý.

#### Luồng thay thế và ngoại lệ

- **A1:** Create category thứ ba, xóa/disable/replace category bắt buộc: từ chối.
- **A2:** Không tự đổi quantities/giá đã mua theo sửa description.
- **A3:** Thiếu category bắt buộc là lỗi cấu hình cần khắc phục, không cho client tự thay thế.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-04, R-18, R-23. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Vẫn đúng hai category; không phát sinh loại vé mới hoặc price changes ngoài UC-16B.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-16A Manage Ticket Types**; SAD Logical View C07. Yêu cầu dẫn xuất: F-UC-16A; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-16B — Configure Cinema Ticket Pricing

#### Mô tả, tác nhân và trigger

Cấu hình Adult/Discounted base price theo cinema.

- Tác nhân: Admin / scoped Manager; Staff read.
- Trigger: Actor xem hoặc cập nhật cinema pricing.
- Domain: D7; component chịu trách nhiệm/phối hợp: C07.

#### Tiền điều kiện

Admin hoặc assigned Manager; Staff chỉ read.

#### Luồng cơ bản

1. Load cinema, hai category và quyền hiện hành.
2. Validate base prices VND không âm theo nguồn, đầy đủ cấu hình bắt buộc.
3. Recheck scope/concurrent version rồi commit giá và audit.
4. Các quotation mới dùng current config; accepted booking snapshots giữ nguyên.

#### Luồng thay thế và ngoại lệ

- **A1:** Negative/invalid price, missing category hoặc ngoài scope: từ chối.
- **A2:** Price đổi giữa quotation/reserve: UC-01 yêu cầu reaccept.
- **A3:** Base 0 không tự xác định đường checkout total0; xem Q-01 ở analysis.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-05, R-18, R-23. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Giá cinema hợp lệ, không có retroactive charge/refund.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-16B Configure Cinema Ticket Pricing**; SAD Logical View C07. Yêu cầu dẫn xuất: F-UC-16B; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-17 — Manage Showtimes and Schedules

#### Mô tả, tác nhân và trigger

Tạo/lập lịch/reschedule/cancel showtime trong các giới hạn nghiệp vụ.

- Tác nhân: Admin / scoped Manager; Staff read.
- Trigger: Actor gửi operation lịch chiếu.
- Domain: D6; component chịu trách nhiệm/phối hợp: C06.

#### Tiền điều kiện

Admin hoặc assigned Manager write, Staff read; cinema/room/movie đủ điều kiện.

#### Luồng cơ bản

1. Load current schedule/resources và kiểm scope.
2. Tạo showtime với movie duration snapshot; tính end/check overlap và cinema turnaround.
3. Với sửa/reschedule, kiểm state/thời điểm/dependencies; giữ purchased-start snapshots của bookings.
4. Commit official schedule và persisted notifications/audit cần thiết, không gọi email trong locks.
5. Với cancel, chỉ cho khi không còn unresolved holds/confirmed bookings/payment/refund obligations và các điều kiện nguồn khác đều thỏa.

#### Luồng thay thế và ngoại lệ

- **A1:** Overlap/turnaround/resources không hợp lệ hoặc concurrent conflict: từ chối.
- **A2:** Reschedule không tự thay giá/ghế/booking hoặc xóa admissions; subsequent admission dùng current start.
- **A3:** Cancel có obligations: reject, không bulk cancel/refund.
- **A4:** Cancelled showtime không trực tiếp reactivate; muốn suất thay thế phải operation tạo mới được phép.
- **A5:** Sửa movie duration không hồi tố duration snapshot của showtime.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-05, R-13, R-18, R-24. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Schedule hợp lệ, history preserved; khách đủ điều kiện có thể request cancellation UC-10, không auto refund.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-17 Manage Showtimes and Schedules**; SAD Logical View C06. Yêu cầu dẫn xuất: F-UC-17; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-18 — Manage User Accounts and Roles

#### Mô tả, tác nhân và trigger

Quản lý account, roles và role-specific cinema assignments.

- Tác nhân: Admin; scoped Manager Staff assignments.
- Trigger: Actor yêu cầu account/role/assignment change.
- Domain: D8; component chịu trách nhiệm/phối hợp: C03.

#### Tiền điều kiện

Admin hoặc Manager đúng operation/scope; current internal account/email/session hợp lệ.

#### Luồng cơ bản

1. Load actor/target account/current roles và assignments.
2. Admin thực hiện supported account/internal-role/lock/unlock/assignment operations.
3. Manager chỉ quản lý Staff–Cinema assignment tại cinema được giao; không grant roles/Manager assignment.
4. Validate permanent Customer membership, last active Admin và các dependencies.
5. Commit thay đổi/audit; account lock revoke sessions, quyền/scope mới có hiệu lực ở subsequent protected operations.

#### Luồng thay thế và ngoại lệ

- **A1:** Manager vượt scope hoặc xin grant internal role: reject.
- **A2:** Xóa permanent Customer/last active Admin: reject.
- **A3:** Unlock không khôi phục old session; account lock không xóa booking hoặc invalidate paid QR.
- **A4:** Email changes áp UC-08B policy; không sửa primary chưa verify bằng đường quản trị tùy ý.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-18, R-19, R-20, R-25. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Current authorization dữ liệu đúng và có audit; không phá account/financial history.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-18 Manage User Accounts and Roles**; SAD Logical View C03. Yêu cầu dẫn xuất: F-UC-18; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

### UC-19 — Manage Bookings and Tickets

#### Mô tả, tác nhân và trigger

Hỗ trợ booking/ticket và xử lý cancellation/refund theo operation authority.

- Tác nhân: Scoped Staff / Manager / Admin.
- Trigger: Actor tra cứu, resend, review hoặc ghi verified refund completion.
- Domain: D2; component chịu trách nhiệm/phối hợp: C08, C10, C11.

#### Tiền điều kiện

Staff/Manager/Admin có current scope tương ứng; completion chỉ Manager/Admin.

#### Luồng cơ bản

1. Lọc lookup theo cinema scope và quyền operation, trả dữ liệu cần thiết.
2. Ticket support/resend dùng booking ticket hiện có và persisted delivery request; không phát QR mới tùy ý.
3. Staff/Manager/Admin có quyền review đọc request, original submitted eligibility và current admission/state.
4. Approve/reject atomically theo điều kiện; approve chuyển approved_awaiting_refund và chặn admission, chưa release sold seats.
5. Manager assigned/Admin ghi verified completion qua UC-04A, không dùng generic status patch.
6. Trả kết quả/audit trace phù hợp, không tiết lộ credential/evidence thừa.

#### Luồng thay thế và ngoại lệ

- **A1:** Actor ngoài scope/Staff completion: reject.
- **A2:** First admission đã commit: không approve cancellation như chưa có admission.
- **A3:** Giờ quay về purchased snapshot không tự vô hiệu request từng nộp hợp lệ; vẫn review các điều kiện hiện hành.
- **A4:** Không force confirmed, edit payment amount, reset admissions, swap seats, transfer showtime hoặc hard-delete history.

#### Yêu cầu đặc biệt và hậu điều kiện

Áp dụng R-01, R-10, R-13, R-14, R-15, R-16, R-17, R-18. Các kiểm tra phải dùng dữ liệu hiện hành; thông báo UI không thay thế commit.

Chỉ operation được phép thay dữ liệu; resend không tạo ticket thứ hai; refund completion effects đúng origin.

#### Truy vết và liên hệ

Nguồn: Use-Case Specifications v2.4, mục **UC-19 Manage Bookings and Tickets**; SAD Logical View C08, C10, C11. Yêu cầu dẫn xuất: F-UC-19; acceptance scenarios xem mục 10 của Business System Analysis. Liên hệ tới UC khác trong các luồng là dependency/navigation trừ quan hệ include đã nêu; không tự đặt UML extension point mới.

## Tính nhất quán xuyên Use Case

| Race / failure | Bất biến cần giữ |
|---|---|
| Reserve / reserve | Một active allocation cho cùng seat/showtime; không giữ một phần |
| Initiation / pending cancel | Revalidate trong transaction; không cancel sau initiation commit |
| Payment / expiration | Confirmed không bị expired; late success không reclaim |
| Approval / first admission | Pending review được auto reject khi first admission thắng; approved-awaiting-refund chặn admission |
| Admission / admission | Tổng batch không vượt category entitlement; replay operation không tăng lại |
| Refund completion / retry | Một financial completion, đúng origin effects, không duplicate release/void |
| Login replacement / replacement | Challenge phải bound expected session; không revoke session mới hơn |
| Refresh / refresh | Consumed refresh token không thể cấp thêm pair; atomic rotation |
| Email token / resend / replacement | Single-use và absolute reservation limit không bị bypass |
| Network loss sau commit | Truy vấn/retry cùng identity, không giả rollback |

Business transaction và external transport không phải một distributed transaction. Receipt/outbox được persist và xử lý retry; không tuyên bố exactly-once email hoặc mạng. Lock/lease/fencing chi tiết thuộc SAD/persistence design; tên constraint/cột thực tế cần audit schema.

## Đồng bộ các tài liệu liên quan

- API phải giữ movie-list pagination default 12 (không áp default 20 chung cho UC-06A), explicit session replacement challenge, refresh rotation, cancellation withdrawal và đầy đủ cancellation-request lifecycle; nếu draft chỉ có login tự replace hoặc status pending/approved chung thì cần cập nhật.
- Data model phải hỗ trợ Guest identity, selection, category quantities, booking-level ticket/admission batches, separate cancellation/refund và notification requests; không giữ Couple/per-person ticket model vì schema cũ đã có.
- UI cần tách Movie Detail reload với Checkout restoration, validation với admission confirmation và pending cancel với confirmed cancellation request.
- Test cases cần cả happy path, negative authority/state, concurrency và recovery; các mục này là kế hoạch kiểm chứng, chưa phải test report.
- Các câu hỏi implementation/zero-total/source integration được quản lý ở Q-01–Q-07 của Business System Analysis; không đánh dấu lại các quy tắc DOCX đã chốt là Open.

## Revision note

Rewrote all 28 use-case summaries against Use-Case Specifications v2.4 and SAD v3.15. Clarified Guest access, selection and payment timing, price snapshots, session replacement and refresh, verification limits, booking-level admission, scoped management permissions, cancellation withdrawal and origin-specific refund completion. Added cross-use-case invariants and downstream synchronization gaps without asserting implementation completion.
