# Business System Analysis — Promethe-Phos

Bản rà soát: 09/11/2026. Nguồn nghiệp vụ: **RUP Use-Case Specifications v2.4**, bản đính kèm `rup_ucspec-v2.4_final(5).docx`. Nguồn kiến trúc: **SAD v3.15**, bản đính kèm `rup_sad(1).docx`. Đường dẫn lưu DOCX dự kiến trong repository là `pa/PA3-Group15/`; cần đối chiếu file thực tế khi tích hợp.

DOCX quyết định hành vi nghiệp vụ; SAD giải thích cách phối hợp component. Markdown/schema/API cũ không được dùng để ghi đè baseline. Tài liệu mô tả yêu cầu/thiết kế, **không xác nhận mã nguồn đã triển khai hoặc kiểm thử đạt**. Các mã R-xx, F-UC-xx và AC-R-xx dưới đây là mã truy vết nội bộ của bản Markdown này, không phải mã BR/FR/AC được trích nguyên văn từ DOCX.

## 1. Mục đích và vấn đề nghiệp vụ

Hệ thống hỗ trợ khách tìm phim/suất chiếu, chọn ghế, thanh toán và nhận vé; hỗ trợ nhân sự quản lý danh mục, lịch chiếu, admission và ngoại lệ thanh toán. Trọng tâm là không bán trùng ghế, không nhầm kết quả tài chính với quyền nhận vé, không nhận khách vượt entitlement và không cấp quyền vận hành vượt cinema assignment.

Tài liệu này xác định phạm vi, actors, yêu cầu chức năng, quy tắc nghiệp vụ, mục tiêu chất lượng và tiêu chí kiểm chứng. [use-cases.md](use-cases.md) mô tả tương tác và ngoại lệ; [business-system-design.md](../analysis%20and%20design/business-system-design.md) mô tả luồng xuyên UC; [architecture.md](../analysis%20and%20design/architecture.md) mô tả C01–C16; mô hình quan hệ và API là thiết kế dẫn xuất.

## 2. Phạm vi và trạng thái

| Nội dung | Phạm vi yêu cầu | Trạng thái triển khai trong lần rà soát |
|---|---|---|
| 28 UC / chín domain | Bao gồm Guest/Customer, internal users và recovery workflows | Chưa kiểm tra repo thực tế |
| Multi-cinema scope | Kiểm assignments theo role; seed một rạp không loại bỏ yêu cầu này | Chưa kiểm chứng |
| Reservation, payment, ticket và admission | Theo các quy tắc R-01–R-17 | Chưa kiểm chứng end-to-end |
| Confirmed cancellation | Chỉ qualifying rescheduling, review và withdrawal theo lifecycle | Không đồng nghĩa hủy tùy ý |
| Refund demo | Quản lý nghĩa vụ, evidence và verified manual completion | Thực chuyển tiền bên ngoài ứng dụng |
| MoMo | Provider định hướng cho demo | Người dùng cho biết chưa thử tích hợp; phải xác định mock hay sandbox khi chạy |
| Brevo | Verification, ticket và transactional notifications | Người dùng báo đã gửi email qua API thành công; chưa chứng minh toàn bộ outbox/retry |
| Local/cloud DB | PostgreSQL Docker local, Supabase PostgreSQL cloud | Không tự suy dùng Supabase Auth |
| Hosting | Vercel frontend, Render backend theo lựa chọn của nhóm | Không xác nhận đã deploy; SAD Deployment/Implementation View thuộc PA4 |
| Promethe-Nous | Informational-only | Không mở quyền ghi nghiệp vụ hoặc báo cáo nội bộ |

Không mở rộng MVP thành loyalty, khuyến mãi phức tạp, partial refund, manual seat swap, force-confirm payment, xóa lịch sử, thêm ticket category thứ ba hoặc tự động chuyển tiền refund. Không dùng giới hạn demo để bỏ việc ghi nhận nghĩa vụ tài chính thật.

## 3. Actors và phân quyền

Guest có public discovery và Guest Checkout; Customer có các chức năng đó cùng tài nguyên account thuộc quyền sở hữu. Staff, Manager và Admin là các role độc lập; một account nhiều role vẫn phải đáp ứng role và assignment của từng operation. Gateway/Email/AI provider là external system; System Worker là thực thi nội bộ theo thời gian, không là role đăng nhập công khai. Database/backend là thành phần hệ thống.

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

## 4. Functional domains và catalogue

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

Domain counts: D1=6, D2=2, D3=1, D4=2, D5=4, D6=1, D7=3, D8=5, D9=4; tổng 28. Domain không ánh xạ một-một sang component, bảng DB hay service deploy.

## 5. Yêu cầu chức năng và truy vết

Mỗi dòng là yêu cầu cấp mục tiêu, cần đọc cùng các R-xx liên quan và luồng/ngoại lệ trong use-cases.md. Không suy ra quyền CRUD chung từ tên “Manage”.

| ID mới | Hệ thống phải hỗ trợ | Nguồn | Quy tắc chi phối |
|---|---|---|---|
| F-UC-01 | Reserve Seats: Selection session; atomic pending booking and held seats; chỉ actor/phạm vi nêu ở catalogue | UC-01; SAD C08 | R-01, R-02, R-03, R-04, R-05, R-10 |
| F-UC-02 | Payment Webhook: Verified durable payment outcome/application; chỉ actor/phạm vi nêu ở catalogue | UC-02; SAD C09 | R-07, R-08, R-09, R-10, R-11 |
| F-UC-03 | Release Expired Bookings: Payment-safe expiration/release; chỉ actor/phạm vi nêu ở catalogue | UC-03; SAD C08, C14 | R-07, R-10, R-11 |
| F-UC-04 | Payment Reconciliation: Resolve uncertain outcomes; route review/refund; chỉ actor/phạm vi nêu ở catalogue | UC-04; SAD C09, C14 | R-07, R-08, R-09, R-10, R-11, R-15 |
| F-UC-04A | Refund: Financial obligation and verified external completion; chỉ actor/phạm vi nêu ở catalogue | UC-04A; SAD C10 | R-10, R-15, R-16 |
| F-UC-05 | Validate Ticket: Booking QR, partial/group admission, category quotas; chỉ actor/phạm vi nêu ở catalogue | UC-05; SAD C11 | R-04, R-10, R-14, R-17 |
| F-UC-06A | Browse Movies: Public catalogue/search/filter; chỉ actor/phạm vi nêu ở catalogue | UC-06A; SAD C04 | R-21 |
| F-UC-06B | View Movie Details: Movie details and navigation to schedule; chỉ actor/phạm vi nêu ở catalogue | UC-06B; SAD C04 | R-21 |
| F-UC-07 | Browse Showtimes: Movie/cinema/date schedule discovery; chỉ actor/phạm vi nêu ở catalogue | UC-07; SAD C06 |  |
| F-UC-08A | Register Customer Account: Create permanent Customer membership; chỉ actor/phạm vi nêu ở catalogue | UC-08A; SAD C03 | R-20 |
| F-UC-08B | Verify Email Address: Verify/resend/change primary email safely; chỉ actor/phạm vi nêu ở catalogue | UC-08B; SAD C03 | R-20 |
| F-UC-08C | Login: Credentials, single session, confirmed replacement; chỉ actor/phạm vi nêu ở catalogue | UC-08C; SAD C03 | R-18, R-19 |
| F-UC-08D | Logout: Revoke current session; chỉ actor/phạm vi nêu ở catalogue | UC-08D; SAD C03 | R-18, R-19 |
| F-UC-09 | Checkout and Payment: Contact/declarations, invoke reserve, initiate MoMo; chỉ actor/phạm vi nêu ở catalogue | UC-09; SAD C08, C09 | R-01, R-02, R-03, R-04, R-05, R-06, R-07, R-08, R-09, R-10, R-12 |
| F-UC-10 | Customer Booking Lifecycle: Lookup/ticket/pending cancel/rescheduling request/withdrawal; chỉ actor/phạm vi nêu ở catalogue | UC-10; SAD C08, C10, C11 | R-01, R-03, R-10, R-12, R-13, R-14, R-16 |
| F-UC-11 | Chat with Promethe-Nous: Informational assistance only; chỉ actor/phạm vi nêu ở catalogue | UC-11; SAD C12 | R-26 |
| F-UC-12 | Manage Movie Catalog: Metadata, associations, movie lifecycle; chỉ actor/phạm vi nêu ở catalogue | UC-12; SAD C04 | R-18, R-21 |
| F-UC-13 | Manage Genres: Genre definitions and activation; chỉ actor/phạm vi nêu ở catalogue | UC-13; SAD C04 | R-18, R-21 |
| F-UC-14A | Manage Cinemas: Cinema metadata/state; Manager turnaround only; chỉ actor/phạm vi nêu ở catalogue | UC-14A; SAD C05 | R-18, R-22 |
| F-UC-14B | Manage Screening Rooms: Room identity/state, parent cinema; chỉ actor/phạm vi nêu ở catalogue | UC-14B; SAD C05 | R-18, R-22 |
| F-UC-15A | Configure Seat Layout: Physical labels/positions/type assignment; chỉ actor/phạm vi nêu ở catalogue | UC-15A; SAD C05 | R-18, R-22 |
| F-UC-15B | Change Seat Operational Status: Active/out-of-service with obligation checks; chỉ actor/phạm vi nêu ở catalogue | UC-15B; SAD C05 | R-18, R-22 |
| F-UC-15C | Manage Seat Types: Shared physical types/surcharges; chỉ actor/phạm vi nêu ở catalogue | UC-15C; SAD C05 | R-05, R-18, R-23 |
| F-UC-16A | Manage Ticket Types: Exactly Adult/Discounted descriptions; chỉ actor/phạm vi nêu ở catalogue | UC-16A; SAD C07 | R-04, R-18, R-23 |
| F-UC-16B | Configure Cinema Ticket Pricing: Cinema Adult/Discounted base prices; chỉ actor/phạm vi nêu ở catalogue | UC-16B; SAD C07 | R-05, R-18, R-23 |
| F-UC-17 | Manage Showtimes and Schedules: Duration snapshots, overlap, reschedule, restricted cancel; chỉ actor/phạm vi nêu ở catalogue | UC-17; SAD C06 | R-05, R-13, R-18, R-24 |
| F-UC-18 | Manage User Accounts and Roles: Roles, account lock/unlock, role-specific cinema assignments; chỉ actor/phạm vi nêu ở catalogue | UC-18; SAD C03 | R-18, R-19, R-20, R-25 |
| F-UC-19 | Manage Bookings and Tickets: Support, redelivery, cancellation review; restricted refund completion; chỉ actor/phạm vi nêu ở catalogue | UC-19; SAD C08, C10, C11 | R-01, R-10, R-13, R-14, R-15, R-16, R-17, R-18 |

## 6. Quy tắc nghiệp vụ dùng chung

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

## 7. Các luồng nghiệp vụ chính

| Luồng | Chuỗi hành vi | Kết quả/bất biến |
|---|---|---|
| Account access | Register → verification riêng → login → current authorization → logout | Không tự login sau register; explicit session replacement |
| Discovery/reservation | Browse → detail/showtime → quantities/seats → Step 1 contact → Step 2 reservation | Selection chưa giữ ghế; booking kế thừa deadline |
| Payment/ticket delivery | Explicit initiation → verified webhook/query → atomic apply → post-commit email | Một QR cấp booking, email failure không đảo payment |
| Expiration/recovery | Deadline/grace → kiểm payment → release hoặc reconcile/review | Unknown không bị coi failed; không reclaim ghế |
| Admission | Staff validate → kiểm khách/remaining quota → confirm batch | Validation không tiêu quota; confirmation idempotent |
| Cancellation/refund | Pending pre-init cancel hoặc confirmed reschedule request → review → verified refund completion | Hai workflow độc lập, khác điều kiện và tác động |
| Cinema preparation | Movies/genres → cinema/room/layout → pricing → showtimes | Đúng quyền; kiểm dependencies/overlap trước commit |
| Operational support | Scoped lookup → resend/review/completion được phép | Không tạo quyền financial override/admission ngầm |

## 8. Đối tượng nghiệp vụ và nhất quán trạng thái

Các nhóm khái niệm gồm account/session/role assignments, verification request, cinema/room/physical seat, movie/genre, category/cinema price, showtime, selection, booking/allocations/snapshots, payment/event, cancellation request/refund evidence, booking ticket/admission và notification/audit.

Tên trạng thái dưới đây dùng diễn đạt lifecycle; tên bảng/cột/enum vật lý phải được kiểm chứng qua migrations/schema thực tế. Không tuyên bố schema hiện tại đã có các cấu trúc mới.

| Sự kiện | Booking / allocations | Payment / cancellation / ticket |
|---|---|---|
| First seat selection | Chưa booking/hold | Selection deadline gốc |
| Step 1 → Step 2 thành công | pending / held | Giá và giờ mua được snapshot |
| Payment initiation hợp lệ | Giữ pending/held | Payment window mới theo R-07; không cho pending cancel |
| Verified eligible application | confirmed / sold | succeeded + applied; một booking ticket/QR |
| Release đủ điều kiện | expired / released | Không phủ nhận kết quả tài chính đến muộn |
| Explicit pre-init cancel | cancelled / released | Không tạo confirmed cancellation/refund |
| Reschedule cancellation request | confirmed / sold | pending_review; admission chưa bị chặn |
| First admission trong pending review | confirmed / sold | Tăng counts; request rejected |
| Approve request | confirmed / sold | approved_awaiting_refund; chặn admission |
| Verified rescheduling refund | cancelled / released | completed; refund succeeded; ticket voided |
| Verified reconciliation refund | Theo tình trạng nghiệp vụ hiện hành | Resolve nghĩa vụ; không suy ra rescheduling effects |

Không dùng ticket valid→used một lần cho toàn bộ booking: một QR có remaining entitlements và admission batches. Không đưa refund_required vào cột trạng thái refund để thay financial state.

## 9. Yêu cầu phi chức năng và cách đánh giá

Nguồn: Vision và SAD §2; các con số bên dưới là **mục tiêu kiểm chứng đề xuất**, không là số đo đã đạt. Khi lập test plan phải bổ sung tải, dữ liệu, thiết bị, latency, cách đo và tiêu chí percentile. Không tự công bố SLA/uptime chưa được duyệt.

| Nhóm | Yêu cầu | Kiểm chứng dự kiến |
|---|---|---|
| Usability | Luồng chính rõ, countdown đúng nguồn; desktop/mobile | Viewport 375/768/1280; mục tiêu ≥80% hoàn thành scenario, chưa đo |
| Performance | Tra cứu/ghi dữ liệu phù hợp sử dụng tương tác | Mục tiêu p95 movie/showtime ≤2s, reservation ≤3s, validation ≤2s; chưa đo |
| Concurrency | Không duplicate active seat/quota/financial effect | Race reservation, payment–expiration, cancellation–admission, concurrent refresh |
| Security | Hash credentials/tokens; current authorization; không secret/PII thừa | Role/scope/ownership/revoked-session/invalid-signature negative tests |
| Integrity | Atomic transitions và snapshots; bảo toàn lịch sử | Fault injection trước/sau commit; đối chiếu invariants |
| Reliability | Durable payment receipt/outbox; retry idempotent | Restart worker, duplicate event, provider timeout, response lost |
| Auditability | Actor, time, action, target, decision/evidence phù hợp | Review log/evidence; không lưu raw secret/verification token |
| Maintainability | Component ownership và API contracts rõ | Review cross-component writes, dependency/transaction boundaries |
| Informational AI | Không bịa dữ liệu thiếu, không thay đổi giao dịch | Câu hỏi thiếu nguồn, outdated schedule, yêu cầu vượt quyền |

## 10. Tiêu chí nghiệm thu có thể kiểm chứng

Mã dưới đây là acceptance checklist dẫn xuất, **chưa thực thi**. Không thay thế ≥15 test cases chi tiết của PA4. Mỗi test case phải bổ sung preconditions, dữ liệu, bước, expected/actual result và evidence.

| ID | Tình huống | Kết quả mong đợi | Quy tắc |
|---|---|---|---|
| AC-R-01 | Guest biết booking code của người khác, không có proof | Không truy cập/quản lý booking | R-01 |
| AC-R-02 | Chọn ghế lúc t0, reserve lúc t0+4 phút | Deadline vẫn t0+5 phút; không được thêm 5 phút | R-02 |
| AC-R-03 | Hai khách giữ cùng ghế; một request có ghế trống và ghế xung đột | Tối đa một allocation active; request thất bại không giữ một phần | R-10 |
| AC-R-04 | Giá thay đổi trước reservation | Hiển thị giá mới và yêu cầu chấp nhận, không tự commit | R-05 |
| AC-R-05 | Cùng idempotency identity/key retry sau mất response | Trả outcome durable, không tạo booking thứ hai | R-10 |
| AC-R-06 | Hết MoMo window + grace nhưng provider vẫn unknown | Không tự failed/release; reconcile/review | R-07, R-11 |
| AC-R-07 | Webhook hợp lệ lặp / out of order | Một application, một ticket; không downgrade thành công đã xác minh | R-08–R-10 |
| AC-R-08 | Chữ ký sai hoặc tiền thu đã xác minh nhưng amount sai | Sai chữ ký không tác động; amount sai bảo toàn tài chính và review, không apply | R-08 |
| AC-R-09 | Brevo lỗi sau payment commit | Booking/ticket còn hiệu lực; delivery retry có theo dõi | R-09 |
| AC-R-10 | Pending cancel cạnh tranh payment initiation | Một transition hợp lệ; initiation thắng thì không cancel/release | R-12 |
| AC-R-11 | Rescheduled confirmed booking, chưa admission, chưa giờ mới | Cho request; chỉ pending_review được withdrawal | R-13 |
| AC-R-12 | First admission cạnh tranh approve cancellation | Không vừa admission sau approval vừa approve trên state lỗi; kiểm lại dưới transaction | R-14, R-17 |
| AC-R-13 | Staff ghi refund completion / Manager ngoài scope | Từ chối; authorized completion đủ evidence mới commit | R-15 |
| AC-R-14 | Refund completion hai lần hoặc hai nguồn nghĩa vụ | Không hoàn/tác động trùng; completion effects đúng origin | R-16 |
| AC-R-15 | Hai batch admission tranh entitlement cuối cùng | Không vượt Adult/Discounted quota; replay không cộng lại | R-17 |
| AC-R-16 | Admission tại đúng −5/+20 phút và ngoài hai biên | Hai biên hợp lệ nếu điều kiện khác thỏa; ngoài biên từ chối | R-17 |
| AC-R-17 | Login conflict cancel / stale replacement challenge | Cancel giữ session; stale challenge không revoke phiên mới | R-19 |
| AC-R-18 | Logout/lock/role hoặc assignment bị thu hồi | Request bảo vệ tiếp theo không dùng quyền cũ; unlock không phục hồi token | R-18, R-19 |
| AC-R-19 | Resend sau 30s / request quá 72h / reuse token | Từ chối theo cooldown/expiry; không đổi primary email trái phép | R-20 |
| AC-R-20 | Manager sửa movie/room identity; thêm ticket category thứ ba | Từ chối dù UI hoặc payload cho phép | R-21–R-23 |
| AC-R-21 | Cancel showtime còn obligations / reactivate cancelled | Từ chối; không tự refund hoặc reset lifecycle | R-24 |
| AC-R-22 | Xóa last active Admin hoặc grant role bằng Manager | Từ chối; giữ invariants quyền | R-25 |
| AC-R-23 | Chatbot bị yêu cầu confirm payment/xem doanh thu nội bộ | Không mutate hoặc tiết lộ dữ liệu ngoài phạm vi | R-26 |

## 11. Quyết định đã chốt và câu hỏi còn mở

**Đã chốt trong baseline:** Guest Checkout; 28 UC; hai category; default max 8; selection 5 phút; MoMo 10 phút + grace default 10 giây; admission −5/+20 phút; một QR/booking; cinema-scoped permissions; rescheduling cancellation/review/refund evidence. Không tiếp tục đánh dấu những nội dung này là OQ chưa quyết định.

| ID mới | Cần làm rõ / kiểm chứng | Loại | Hướng xử lý |
|---|---|---|---|
| Q-01 | Tổng booking bằng 0 khi base price được phép 0 | Nghiệp vụ/tích hợp | Xác nhận đường đi chính thức; không dùng lại Option B cũ hoặc tự bypass payment |
| Q-02 | MoMo demo là mock hay sandbox, capability query/signature/ACK/idempotency | Implementation/integration | Ghi mode và evidence; không coi lựa chọn provider là đã tích hợp |
| Q-03 | Endpoint, DTO, Guest credential transport, cookie/CSRF/CORS và config keys | Thiết kế triển khai | Chốt cùng API/C03; giữ session/ownership semantics của baseline |
| Q-04 | Database schema/migrations/seed đã hỗ trợ baseline chưa | Kiểm chứng implementation | Audit schema thật với relational-model; không dùng schema cũ giới hạn UC |
| Q-05 | Retry/backoff, lease/fencing, dead-letter/escalation thresholds | Vận hành | Chốt cấu hình, bounded retry và manual review; không tự release unknown |
| Q-06 | AI provider/nguồn dữ liệu/quota và giới hạn memory history | Tích hợp | Chọn nguồn được phép; thiếu dữ liệu thì thông báo giới hạn |
| Q-07 | Kết quả hiệu năng, concurrent load và uptime | Kiểm thử | Ghi test environment rồi đo; không báo targets là achieved |

## 12. Chuyển đổi tham chiếu từ bản cũ

Bản cũ dùng FR-*/BR-*/AC-* nội bộ dễ nhầm với mã trong DOCX. Bản này giữ provenance qua UC/R mới thay vì gán lại một ID cũ cho nghĩa mới. Những tài liệu còn trỏ ID cũ cần cập nhật theo **ngữ nghĩa**, không search/replace hàng loạt. Mã BR trong DOCX vẫn tham chiếu trực tiếp DOCX nếu cần.

| Nhóm tham chiếu cũ | Thay đổi | Tham chiếu hiện hành |
|---|---|---|
| FR-AUTH-001/002/004; BR-AUTH-004/005 | Tách registration/verification/login/logout; bỏ JWT stale-rights allowance | UC-08A/B/C/D, UC-18; R-18–R-20, R-25 |
| FR-AUTH-003 | Không mở rộng generic profile API chỉ từ mục cũ; email change theo UC-08B/18 | R-20; phần profile khác cần nguồn rõ |
| FR-MOVIE/SHOW; FR-MGT-* | Giữ mục tiêu, sửa actor/scope/operation và tách đúng UC | UC-06A/B,07,12–17; R-21–R-24 |
| FR-BOOK-001/002, BR-BOOK-004/005/009/010 | Bỏ timer từ createdAt, Couple, discount-percent/per-seat category, per-person QR | R-02–R-05, R-17 |
| FR-BOOK-003/004, AC-BOOK-004..006 | Guest authority, pending pre-init cancellation và rescheduling cancellation riêng | UC-10,19; R-01, R-12–R-16 |
| FR-PAY-*, BR-PAY-* | MoMo window/grace, verified truth vs application, refund obligation | UC-02/03/04/04A/09; R-07–R-16 |
| FR-TICKET-002 và tiêu chí scan một lần | Booking QR, quota/batches, admission operation idempotency | UC-05; R-17 |
| OQ-01/03/04/07/08 cũ | Provider định hướng, window, permission, grace, categories đã có baseline mới | Q-02 chỉ hỏi implementation; R-04/R-07/R-17/R-18 |
| OQ-06 Option B | Không chuyển quyết định cũ thành constraint mới nếu chưa đối chiếu | Q-01 |
| NFR-SCALE-001 | Không loại multi-cinema authorization khỏi MVP | R-18, ma trận quyền |

## 13. Revision note

Reconciled the business scope with Use-Case Specifications v2.4 and SAD v3.15; replaced outdated booking timers, pricing, admission, session, permission, cancellation and refund assumptions; added traceable requirements, acceptance scenarios and implementation gaps. No implementation or test completion is asserted.
