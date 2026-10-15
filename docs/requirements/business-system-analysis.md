﻿# Business System Analysis

## 1. Giới thiệu

### 1.1. Mục đích tài liệu

Tài liệu này phân tích nghiệp vụ của hệ thống quản lý rạp chiếu phim và đặt vé trực tuyến: xác định mục tiêu, phạm vi, actors, nghiệp vụ chính, yêu cầu chức năng (FR), quy tắc nghiệp vụ (BR), hành trình khách hàng/quản trị, yêu cầu phi chức năng (NFR), tiêu chí nghiệm thu (AC) và các vấn đề còn bỏ ngỏ. Tài liệu là cơ sở để thiết kế use case (`business-system-design.md`), thiết kế dữ liệu (`database/database-design.md`, `relational-model.md`) và xây dựng test case, không thay thế các tài liệu đó.

Phân công trách nhiệm giữa các tài liệu:

| Tài liệu | Chịu trách nhiệm |
|---|---|
| `docs/requirements/business-system-analysis.md` (tài liệu này) | Mục tiêu nghiệp vụ, phạm vi, actors, yêu cầu chức năng/phi chức năng, quy tắc nghiệp vụ, tiêu chí nghiệm thu. |
| `docs/analysis and design/business-system-design.md` | Use case, activity/sequence diagram, kiến trúc hệ thống. |
| `database/database-design.md` | Mô hình dữ liệu khái niệm/logic, entity, ràng buộc, transaction. |
| `docs/analysis and design/relational-model.md` | Relation, PK/FK, candidate key, ràng buộc quan hệ. |
| `database/database-schema.md`, `database-schema.sql` | Schema vật lý, DDL. |

### 1.2. Bối cảnh và bài toán

Rạp chiếu phim cần một ứng dụng web cho phép khách hàng tra cứu phim, xem lịch chiếu, chọn ghế, đặt và thanh toán vé trực tuyến; đồng thời cho phép nhân viên/quản trị vận hành danh mục phim, phòng chiếu, ghế, lịch chiếu, tra cứu booking/vé và soát vé. Bài toán cốt lõi là bảo đảm tính nhất quán của ghế khi nhiều khách cùng đặt, thời gian giữ ghế hợp lý, thanh toán qua cổng bên ngoài có kết quả không chắc chắn, và phân quyền đúng ranh giới giữa các vai trò.

### 1.3. Mục tiêu hệ thống

- Cho phép khách hàng tự phục vụ từ tra cứu đến nhận vé mà không cần đến quầy.
- Bảo đảm một ghế trong một suất chiếu không bao giờ được bán cho hai khách cùng lúc.
- Ghi nhận đầy đủ lịch sử booking và payment attempt, kể cả các attempt thất bại hoặc chưa chắc chắn.
- Cung cấp công cụ quản trị cho Staff/Manager/Admin theo đúng quyền hạn của từng vai trò.

### 1.4. Đối tượng đọc tài liệu

Nhóm phát triển (backend, frontend, database), nhóm kiểm thử và giảng viên/người review dự án.

### 1.5. Thuật ngữ và viết tắt

| Thuật ngữ | Ý nghĩa |
|---|---|
| Booking | Đơn đặt vé của khách hàng; trạng thái `pending`, `confirmed`, `expired`, `cancelled`. |
| Booking Detail | Phân bổ một ghế trong một booking; trạng thái `held`, `sold`, `released`. |
| Hold (giữ ghế) | Khoảng thời gian ghế được đặt chờ cho một booking pending. |
| Payment Attempt | Một lần thử thanh toán của booking; trạng thái `created`, `pending`, `unknown`, `succeeded`, `failed`, `cancelled`. |
| Ticket | Vé được phát hành sau khi booking được xác nhận; trạng thái `valid`, `used`, `voided`. |
| Showtime (Suất chiếu) | Lượt chiếu một phim trong một phòng tại một khoảng thời gian. |
| MVP/demo | Phạm vi tối thiểu được triển khai và demo hiện tại. |
| FR / BR / NFR / AC / OQ | Yêu cầu chức năng / quy tắc nghiệp vụ / yêu cầu phi chức năng / tiêu chí nghiệm thu / câu hỏi mở. |

## 2. Tổng quan hệ thống

- **Loại hệ thống:** Web application (web bán vé xem phim trực tuyến kèm trang quản trị), theo kiến trúc được mô tả trong `docs/analysis and design/architecture.md`.
- **Mục đích tổng quát:** Quản lý hoạt động của rạp chiếu phim và cho phép khách hàng đặt vé trực tuyến.
- **Người dùng chính:** Customer (khách hàng), Staff (nhân viên rạp), Manager (quản lý vận hành), Admin (quản trị hệ thống).
- **Hệ thống bên ngoài:** Payment Gateway — xử lý giao dịch thanh toán và trả kết quả về hệ thống.
- **Năng lực nghiệp vụ chính:**
  - Tra cứu phim/thể loại và lịch chiếu.
  - Chọn ghế, tạo booking và giữ ghế có thời hạn.
  - Thanh toán trực tuyến qua Payment Gateway và phát hành vé.
  - Soát vé tại rạp.
  - Quản lý phim, thể loại, rạp/phòng/ghế, suất chiếu.
  - Quản lý tài khoản và phân quyền.
  - Tra cứu booking/payment và xem báo cáo (phạm vi chi tiết xem mục 3 và OQ-04).

## 3. Phạm vi và ràng buộc

### 3.1. Quyết định phạm vi đã chốt

Bảng dưới đây giữ nguyên các quyết định đã thống nhất trong `business-system-design.md` (mục 0) và `database-design.md`:

Lưu ý về thời gian giữ ghế: "5 phút" ở các mục 3.1, 3.2, 3.5 là **thời hạn giữ ghế danh nghĩa** — tính từ thời điểm tạo booking, mốc này không thay đổi (BR-BOOK-004). **Thời điểm giải phóng ghế thực tế** có thể muộn hơn 5 phút: theo `database-design.md` mục 34.1, worker chỉ chuyển booking `expired` và detail `released` khi booking quá hạn **và** không còn payment attempt đang mở (`created`/`pending`/`unknown`); nếu còn attempt, cả hai sự kiện bị trì hoãn đến khi attempt chốt kết quả. Việc xác định chính xác cửa sổ chờ này là OQ-07 (chưa chốt).

| Trạng thái | Quyết định |
|---|---|
| **CONFIRMED — MVP/demo** | Một chi nhánh ban đầu; phim/thể loại, phòng/ghế, suất chiếu, đặt vé, giữ ghế 5 phút, thanh toán qua cổng, phát hành và soát vé, tài khoản và vai trò cơ bản. |
| **CONFIRMED — booking** | Ghế được giữ khi booking pending tạo thành công, giữ 5 phút tính từ lúc tạo booking; hủy trước thanh toán giải phóng ghế. |
| **CONFIRMED — bán vé** | Không áp dụng cutoff cố định 10/15/30 phút trước giờ chiếu; demo cho phép bán sát giờ bắt đầu. Mốc đóng bán cuối cùng (nếu có) còn phải nhóm chốt (OQ-02). |
| **CONFIRMED — chính sách nền** | Không hủy booking sau thanh toán và không hoàn tiền trong demo. |
| **PROPOSED** | `MANAGER` là actor quản trị vận hành với quyền quản lý phim, phòng/ghế, suất chiếu và xem báo cáo; phạm vi quyền chi tiết cần nhóm chốt (OQ-04). |
| **OUT OF SCOPE — DEMO** | Hủy booking sau thanh toán bởi customer, refund, workflow hủy suất do rạp (thông báo/đổi lịch/hoàn tiền tự động), **workflow đối soát tài chính/hoàn tiền tự động cho payment thành công đến muộn sau khi ghế đã được bán lại** (lưu ý: *ghi nhận và bảo toàn kết quả* payment thành công muộn — không áp dụng, giữ `succeeded` + payment event để truy vết — *vẫn thuộc phạm vi*; chỉ phần xử lý tài chính tiếp theo là ngoài phạm vi, xem BR-PAY-007, OQ-07), thành viên/loyalty, khuyến mãi phức tạp, báo cáo phân tích nâng cao, phân quyền theo chi nhánh. |
| **OPEN** | Cổng thanh toán cụ thể, mốc đóng bán cuối cùng, cửa sổ soát vé, điều kiện đủ quyền lợi của các loại vé giảm giá, ranh giới quyền Manager/Admin và Staff theo chi nhánh. |

### 3.2. Nằm trong phạm vi MVP

- Đăng ký, đăng nhập, tài khoản và vai trò (CUSTOMER, STAFF, MANAGER, ADMIN).
- Quản lý phim, thể loại, rạp, phòng, sơ đồ ghế, loại ghế.
- Quản lý suất chiếu và kiểm tra không chồng lịch phòng.
- Chọn ghế, tạo booking giữ ghế 5 phút, hủy booking pending trước thanh toán.
- Thanh toán qua Payment Gateway, phát hành vé khi payment thành công và được xác minh.
- Soát vé tại rạp.
- Tra cứu booking/vé/payment và báo cáo cơ bản (chi tiết quyền xem OQ-04).

### 3.3. Nằm ngoài phạm vi demo

- Hủy booking sau khi đã thanh toán và refund.
- Workflow tự động khi rạp hủy suất chiếu (thông báo khách, đổi lịch, hoàn tiền).
- Đối soát tài chính và hoàn tiền tự động cho payment thành công đến muộn sau khi ghế đã được bán lại (ghi nhận và bảo toàn kết quả thành công để truy vết vẫn nằm trong phạm vi — BR-PAY-007).
- Thành viên/loyalty, khuyến mãi phức tạp, báo cáo phân tích nâng cao.
- Phân quyền theo chi nhánh; vận hành nhiều chi nhánh (mô hình dữ liệu sẵn sàng nhưng demo chỉ seed một cinema).

**Lưu ý:** các cấu trúc dữ liệu như `refund`, `payment_event`, nhiều `cinema` được giữ trong mô hình dữ liệu để sẵn sàng mở rộng; việc "có trong mô hình dữ liệu" không đồng nghĩa "đã triển khai chức năng".

### 3.4. Giả định

- Hệ thống vận hành với một payment gateway bên ngoài (cổng cụ thể chưa chốt — OQ-01).
- Người dùng truy cập qua trình duyệt web trên máy tính và thiết bị di động.
- Mốc thời gian lưu theo UTC, hiển thị theo `Asia/Ho_Chi_Minh` (theo `relational-model.md` mục 9.2).

### 3.5. Ràng buộc

- Chỉ một payment attempt đang mở (`created`/`pending`/`unknown`) cho mỗi booking tại một thời điểm.
- Mỗi ghế trong một suất chỉ có tối đa một phân bổ hiệu lực (`held`/`sold`).
- Thời hạn giữ ghế danh nghĩa là 5 phút kể từ khi tạo booking. Đây là mốc hết hạn danh nghĩa, không phải thời điểm giải phóng ghế bắt buộc: giải phóng (`expired`/`released`) được thực hiện bởi worker theo chính sách lifecycle của `database-design.md` mục 34.1 — booking quá hạn chỉ được chuyển `expired` và giải phóng ghế khi không còn payment attempt đang mở; nếu còn attempt, việc hết hạn/giải phóng bị trì hoãn đến khi attempt chốt (chi tiết BR-BOOK-004, OQ-07).
- Giao dịch với Payment Gateway phụ thuộc độ trễ và khả năng lặp callback của cổng; hệ thống phải xử lý idempotent.
- Authorization phải được thực hiện ở backend; không dựa vào giao diện hay việc biết UUID/mã công khai.

## 4. Stakeholders và Actors

| Actor | Loại | Vai trò và trách nhiệm | Mục tiêu | Tương tác chính với hệ thống |
|---|---|---|---|---|
| Customer | Người dùng | Khách hàng đặt vé | Mua vé nhanh, chọn được ghế mong muốn, nhận vé | Đăng ký/đăng nhập, xem phim/lịch chiếu, chọn ghế, đặt vé, hủy booking pending, thanh toán, xem vé và lịch sử |
| Staff | Người dùng | Nhân viên rạp | Soát vé chính xác tại rạp | Đăng nhập, soát vé (Validate Ticket), tra cứu booking/vé |
| Manager | Người dùng | Quản lý vận hành (**PROPOSED** — ranh giới quyền cần chốt, OQ-04) | Vận hành lịch chiếu và dữ liệu rạp | Đăng nhập, quản lý phim, phòng/ghế, suất chiếu, xem báo cáo |
| Admin | Người dùng | Quản trị hệ thống | Bảo đảm hệ thống và dữ liệu đúng phép | Đăng nhập, toàn bộ chức năng quản trị, quản lý tài khoản và phân quyền |
| Payment Gateway | Hệ thống bên ngoài | Xử lý giao dịch thanh toán | Hoàn tất giao dịch an toàn | Nhận yêu cầu thanh toán, trả kết quả (webhook/truy vấn), nhận yêu cầu đối soát |

Ghi chú: Payment Gateway không phải người dùng trực tiếp. Ranh giới quyền đã xác nhận: Staff — soát vé và tra cứu booking/vé phục vụ soát vé; Admin — toàn quyền quản trị gồm user/role. Ranh giới quyền của Manager và các ranh giới chi tiết khác (quản lý payment, hủy booking, báo cáo) là **đề xuất, chưa chốt** — xem mục 8.5 (BR-AUTH-005) và OQ-04. Trừ khi có ghi chú PROPOSED/OPEN, các quyền nêu trong bảng trên phản ánh ranh giới đã thống nhất trong `business-system-design.md`.

## 5. Nghiệp vụ chính

| Nghiệp vụ | Mục tiêu | Actors | Kết quả mong muốn |
|---|---|---|---|
| Duyệt phim và xem thông tin phim | Khách hàng tìm được phim | Customer | Danh sách/chi tiết phim chính xác theo trạng thái |
| Xem lịch chiếu | Khách hàng chọn được suất phù hợp | Customer | Danh sách suất theo phim/ngày, chỉ suất còn bán |
| Chọn ghế và đặt vé | Mua được ghế mong muốn, không bị xung đột | Customer | Booking pending được tạo, toàn bộ ghế được giữ, không giữ một phần |
| Thanh toán và phát hành vé | Hoàn tất giao dịch và nhận vé | Customer, Payment Gateway | Payment đúng số tiền; khi thành công và đủ điều kiện, booking confirmed và vé được phát hành |
| Lịch sử đặt vé và tra cứu vé | Khách hàng tra cứu được vé của mình | Customer | Chỉ thấy booking/vé của chính mình |
| Quản lý phim và thể loại | Danh mục phim chính xác | Manager/Admin | Phim/thể loại được thêm, sửa, ngừng hiển thị an toàn |
| Quản lý rạp, phòng và ghế | Sơ đồ ghế phản ánh đúng thực tế | Manager/Admin | Phòng/ghế được cấu hình; không phá vỡ dữ liệu đã có booking |
| Quản lý suất chiếu | Lịch chiếu hợp lệ, không chồng phòng | Manager/Admin | Suất được tạo/sửa/hủy đúng quy tắc lịch |
| Tra cứu booking/vé và soát vé | Vận hành tại rạp | Staff (soát vé), Manager/Admin (tra cứu) | Vé valid được soát một lần; booking tra cứu theo quyền |
| Quản lý tài khoản và phân quyền | Truy cập đúng quyền | Admin | Role được cấp/thu hồi có kiểm soát |
| Xem báo cáo và thống kê | Theo dõi hoạt động kinh doanh | Manager/Admin (phạm vi chi tiết OQ-04) | Số liệu cơ bản theo quyền |

## 6. Miền nghiệp vụ và đối tượng dữ liệu khái niệm

Danh sách căn cứ mô hình miền của `database-design.md` (18 entity); đây là mức khái niệm — chi tiết khóa, ràng buộc và chuẩn hóa thuộc `relational-model.md`.

Lưu ý đối chiếu số lượng: mô hình ở `database-design.md` gồm 18 entity, trong đó hai entity `MovieGenre` và `UserRole` là các bảng liên kết (junction) cho hai quan hệ nhiều–nhiều Movie–Genre và User–Role (mục 6 và mục 18 của `database-design.md`; tương ứng `movie_genre`, `user_role` trong `relational-model.md` mục 3.3/3.12 và `database-schema.md`). Bảng dưới đây liệt kê 16 đối tượng nghiệp vụ; hai bảng liên kết chỉ xuất hiện ở mức quan hệ/physical schema, không cần hàng riêng ở bảng miền khái niệm này.

| Đối tượng | Ý nghĩa nghiệp vụ | Thông tin chính | Quan hệ khái niệm |
|---|---|---|---|
| Movie | Phim được chiếu tại rạp | Tiêu đề, mô tả, thời lượng, ngày phát hành, ngôn ngữ, độ tuổi, trạng thái (`coming_soon`, `now_showing`, `ended`, `archived`) | Một phim có nhiều thể loại và nhiều suất chiếu |
| Genre | Thể loại phim | Mã, tên, mô tả, còn hoạt động | Gắn với nhiều phim qua quan hệ nhiều–nhiều |
| Cinema | Chi nhánh/rạp vật lý | Mã, tên, địa chỉ, điện thoại, trạng thái | Chứa nhiều phòng; MVP chỉ seed một cinema |
| CinemaRoom | Phòng chiếu trong rạp | Mã, tên, trạng thái (`active`, `maintenance`, `inactive`) | Thuộc một cinema; chứa nhiều ghế; tổ chức nhiều suất chiếu |
| SeatType | Loại ghế (STANDARD, VIP, COUPLE...) | Mã, tên, sức chứa (`capacity_units`), phụ thu | Đặc trưng cho nhiều ghế; ghế Couple có `capacity_units = 2` |
| Seat | Ghế vật lý trong phòng | Hàng, số ghế, loại ghế, trạng thái vật lý | Thuộc một phòng; được phân bổ trong booking detail |
| TicketType | Loại vé và mức giảm giá | Mã, tên, mức giảm %, điều kiện áp dụng, còn hoạt động | Áp dụng cho booking detail; điều kiện áp dụng do backend xác minh |
| Showtime | Suất chiếu | Phim, phòng, thời gian bắt đầu/kết thúc, thời điểm phòng rảnh, giá cơ sở, trạng thái | Một phim chiếu trong một phòng; có nhiều booking |
| User (AppUser) | Tài khoản người dùng | Email, tên, trạng thái (`active`, `locked`, `deactivated`) | Có nhiều role; là chủ booking; có thể là người soát vé |
| Role | Vai trò phân quyền | Mã (CUSTOMER, STAFF, MANAGER, ADMIN), còn hoạt động | Gán cho nhiều user qua quan hệ nhiều–nhiều |
| Booking | Đơn đặt vé | Chủ đơn, suất, tổng tiền, thời hạn giữ, trạng thái, mã booking | Thuộc một user và một suất; gồm nhiều detail; có nhiều payment attempt |
| BookingDetail | Phân bổ một ghế trong booking | Ghế, loại vé, snapshot giá, trạng thái phân bổ | Thuộc booking; gắn một ghế; có tối đa một ticket |
| Ticket | Vé đã phát hành | Mã vé, trạng thái (`valid`, `used`, `voided`), thời điểm phát hành/sử dụng | Phát hành từ một booking detail; được staff soát |
| Payment | Lần thử thanh toán | Cổng, số tiền, tiền tệ, trạng thái, thời điểm xác minh/áp dụng | Thuộc một booking; có nhiều payment event |
| PaymentEvent | Sự kiện callback/đối soát từ cổng | Nguồn, khóa sự kiện, loại, trạng thái xử lý | Ghi nhận sự kiện của một payment; phục vụ idempotency |
| Refund | Yêu cầu hoàn tiền (**data-model readiness, ngoài phạm vi demo**) | Mã hoàn, số tiền, trạng thái | Gắn với một payment; chưa có workflow demo |

## 7. Yêu cầu chức năng

Định dạng mỗi yêu cầu: **FR-XXX-nnn** — mô tả; actor; hành vi kỳ vọng ("hệ thống shall..."); quy tắc liên quan; điều kiện kiểm tra được.

### 7.1. Xác thực và quản lý tài khoản

- **FR-AUTH-001** — Đăng ký tài khoản. Actor: khách (chưa đăng nhập). Hệ thống shall cho phép đăng ký tài khoản và khi thành công cấp đúng role `CUSTOMER`, không cấp role khác.
  Kiểm tra: tài khoản mới chỉ có role CUSTOMER.
- **FR-AUTH-002** — Đăng nhập. Actor: mọi user. Khi thông tin xác thực hợp lệ và tài khoản `active`, hệ thống shall tạo phiên đăng nhập và trả về quyền truy cập theo role. Khi tài khoản `locked`/`deactivated` hoặc thông tin sai, hệ thống shall từ chối mà không tiết lộ thông tin nhạy cảm.
  Kiểm tra: đăng nhập tài khoản locked bị từ chối với thông báo chung.
- **FR-AUTH-003** — Xem và cập nhật hồ sơ cá nhân. Actor: user đã đăng nhập. Hệ thống shall chỉ cho phép user sửa hồ sơ của chính mình.
  Kiểm tra: sửa hồ sơ của user khác bị từ chối.
- **FR-AUTH-004** — Kiểm soát truy cập theo role. Hệ thống shall thực thi authorization ở backend dựa trên role hiệu lực và quyền sở hữu tài nguyên; thay đổi role có thể không phản ánh ngay vào access token đang hiệu lực (giới hạn MVP — BR-AUTH-004).

### 7.2. Duyệt phim và lịch chiếu (khách hàng)

- **FR-MOVIE-001** — Xem danh sách phim. Hệ thống shall hiển thị phim theo trạng thái (đang chiếu, sắp chiếu) với thông tin cơ bản.
  Kiểm tra: phim `archived` không xuất hiện trong danh sách khách hàng.
- **FR-MOVIE-002** — Xem chi tiết phim. Hệ thống shall hiển thị thông tin phim và các suất chiếu còn bán.
- **FR-SHOW-001** — Xem lịch chiếu theo phim/ngày. Hệ thống shall hiển thị chỉ các suất `scheduled` còn bán (theo BR-ST-003: không `cancelled`, chưa quá hạn bán nếu có mốc). Đây là thông tin hiển thị phục vụ duyệt; điều kiện chốt cuối cùng khi tạo booking do backend kiểm tra lại (FR-BOOK-001, BR-BOOK-001).
- **FR-SHOW-002** — Xem sơ đồ ghế của suất. Hệ thống shall hiển thị trạng thái ghế theo phân bổ hiện hành: khả dụng (chưa có phân bổ hiệu lực), đang giữ, đã bán; ghế bảo trì/ngừng hoạt động không chọn được. Sơ đồ hiển thị là thông tin phục vụ chọn ghế; tính khả dụng chốt cuối cùng do backend kiểm tra lại trong transaction tạo booking (FR-BOOK-001, BR-BOOK-002).
  Kiểm tra: ghế đang giữ bởi booking khác hiển thị không chọn được.

### 7.3. Đặt vé và giữ ghế

- **FR-BOOK-001** — Tạo booking giữ ghế. Actor: Customer đã đăng nhập. Khi khách chọn ít nhất một ghế khả dụng trong suất còn bán, hệ thống shall trong một transaction **kiểm tra lại authoritative toàn bộ điều kiện**: suất còn bán theo BR-ST-003 (không `cancelled`, chưa quá `booking_closes_at` nếu có), toàn bộ ghế còn khả dụng — sau đó tạo booking `pending` với thời hạn giữ 5 phút và phân bổ `held` cho toàn bộ ghế đã chọn, hoặc từ chối toàn bộ nếu bất kỳ điều kiện nào không thỏa (không giữ một phần — BR-BOOK-001, BR-BOOK-002). Kết quả hiển thị trước đó (FR-SHOW-001/002) không thay thế kiểm tra này.
  Kiểm tra: request chọn 1 ghế trùng + 1 ghế trống không giữ ghế nào.
- **FR-BOOK-002** — Tính tiền khi tạo booking. Hệ thống shall tự tính giá từng ghế và tổng tiền từ giá hiện hành tại thời điểm tạo (BR-BOOK-005); client không được quyết định giá cuối hay mức giảm.
  Kiểm tra: giá booking khớp tính toán backend từ giá cơ sở suất, phụ thu loại ghế và mức giảm loại vé.
- **FR-BOOK-003** — Hủy booking pending. Actor: chủ booking. Khi booking còn `pending`, chưa có payment `succeeded` được áp dụng và không có payment `created`/`pending`/`unknown`, hệ thống shall chuyển booking sang `cancelled` và giải phóng toàn bộ ghế đang giữ trong một transaction (BR-BOOK-008). Các trường hợp khác (booking của người khác, đã `confirmed`/`expired`/`cancelled`, có attempt chưa chắc chắn) shall bị từ chối. Lưu ý: booking có attempt đang mở không thể hủy thủ công; vòng đời hết hạn của nó theo BR-BOOK-004 (`database-design.md` mục 34.1) — hết hạn là lifecycle, không phải hủy.
  Kiểm tra: hủy booking có payment `pending` bị từ chối và ghế giữ nguyên.
- **FR-BOOK-004** — Xem lịch sử đặt vé. Actor: Customer. Hệ thống shall hiển thị booking và vé của chính user với trạng thái hiện tại; không hiển thị booking của user khác (BR-AUTH-002).

### 7.4. Thanh toán

- **FR-PAY-001** — Tạo payment attempt. Actor: Customer (với booking pending của mình). Khi khách bắt đầu thanh toán cho booking pending còn hạn, hệ thống shall tạo payment attempt với số tiền và tiền tệ đúng bằng tổng tiền/tiền tệ của booking và có idempotency key; nếu đã có attempt `created`/`pending`/`unknown` thì không tạo attempt mở mới (BR-PAY-001, BR-PAY-002).
  Kiểm tra: attempt thứ hai khi còn attempt mở bị từ chối.
- **FR-PAY-002** — Nhận và xác minh kết quả thanh toán. Hệ thống shall xử lý sự kiện từ cổng theo bốn bước phân biệt: (1) tiếp nhận — lưu sự kiện bền vững trước (`received`), kể cả khi chưa xác minh, để không mất callback; (2) xác minh nguồn/chữ ký và đối chiếu định danh yêu cầu, số tiền, tiền tệ; (3) xử lý nghiệp vụ — chỉ sự kiện hợp lệ mới được ảnh hưởng trạng thái payment/booking/ghế/ticket; (4) đánh dấu xử lý thành công (`processed`) cùng transaction nghiệp vụ (BR-PAY-005, BR-PAY-006). Callback không hợp lệ (sai chữ ký/sai số tiền/sai tiền tệ) shall được ghi nhận nhưng không thay đổi bất kỳ trạng thái payment, booking, phân bổ ghế hay ticket nào (NFR-SEC-003).
  Kiểm tra: callback sai chữ ký hoặc sai số tiền được lưu nhận nhưng trạng thái nghiệp vụ không thay đổi.
- **FR-PAY-003** — Xử lý callback lặp idempotent. Hệ thống shall nhận diện callback trùng qua khóa sự kiện, chỉ xử lý nghiệp vụ một lần và không tạo thêm ticket (BR-PAY-006). Callback trùng của một sự kiện đã xử lý thành công được bỏ qua; callback trùng trong khi sự kiện gốc đang xử lý được tuần tự hóa, không gây cập nhật nghiệp vụ trùng; callback lặp của sự kiện hợp lệ không đảo ngược trạng thái payment đã áp dụng (BR-PAY-008).
  Kiểm tra: gửi cùng callback hai lần chỉ tạo một kết quả xử lý nghiệp vụ.
- **FR-PAY-004** — Xử lý kết quả chưa chắc chắn. Khi giao dịch timeout hoặc mất kết nối, hệ thống: (1) shall chuyển payment sang `pending`/`unknown`; (2) shall **không tự động retry** — không tạo attempt mở mới khi còn attempt chưa chắc chắn (BR-PAY-002), kết quả chỉ chốt sau khi đối soát với cổng (BR-PAY-003); (3) shall **không gia hạn giữ ghế theo ý muốn client** — hết hạn/giải phóng tuân theo chính sách lifecycle đã thống nhất trong `database-design.md` mục 34.1: worker chỉ chuyển booking `expired` + detail `released` khi quá hạn và không còn attempt đang mở (BR-BOOK-004); nếu attempt thành công trên booking đã hết hạn, áp dụng FR-PAY-005/BR-PAY-007: không tự chiếm lại ghế, không phát hành ticket.
  Kiểm tra: payment `unknown` không cho retry tự động; booking quá hạn với attempt đang mở không được chuyển `expired`/`released` cho đến khi attempt chốt (theo nguồn 34.1); payment thành công sau đó không tạo ticket và không chiếm lại ghế.
- **FR-PAY-005** — Áp dụng payment và phát hành vé. Khi payment `succeeded` đã xác minh thỏa mọi điều kiện áp dụng (booking vẫn `pending`), hệ thống shall trong một transaction: đánh dấu payment được áp dụng, chuyển booking `pending -> confirmed`, chuyển toàn bộ detail `held -> sold` và tạo ticket; nếu booking không còn đủ điều kiện (đã `expired`/`cancelled` với ghế đã được giải phóng) thì shall không áp dụng, không xác nhận booking, không phát hành ticket, không chiếm lại ghế; giữ kết quả thành công nguyên trạng để đối soát (BR-PAY-004, BR-PAY-007). Kết quả tài chính thành công và việc xác nhận booking là hai kết quả độc lập.
  Kiểm tra: payment thành công trên booking đã expired không confirm booking, không tạo ticket và không chiếm lại ghế đã giải phóng.

### 7.5. Vé

- **FR-TICKET-001** — Xem vé. Actor: chủ booking đã confirmed. Hệ thống shall hiển thị vé (mã vé, ghế, suất) cho khách tra cứu.
- **FR-TICKET-002** — Soát vé. Actor: Staff. Khi nhân viên quét/nhập mã vé, hệ thống shall kiểm tra mã vé, booking `confirmed`, detail `sold` và trạng thái vé; nếu vé `valid`, cập nhật có điều kiện `valid -> used` và ghi thời điểm/người soát (BR-BOOK-009). Vé đã `used`/`voided` shall bị từ chối.
  Kiểm tra: soát cùng mã vé hai lần, lần hai bị từ chối.

### 7.6. Quản trị nội dung và lịch chiếu

- **FR-MGT-MOVIE-001** — Quản lý phim. Actor: Manager/Admin. Hệ thống shall cho phép thêm, sửa, cập nhật trạng thái phim; phim/thể loại đã được tham chiếu (suất chiếu, booking...) ưu tiên ngừng hoạt động thay vì xóa vật lý (nguyên tắc vòng đời danh mục — xem BR-ROOM-004 về khóa cấu trúc ghế và nguyên tắc ngừng hoạt động danh mục).
- **FR-MGT-MOVIE-002** — Quản lý thể loại. Actor: Manager/Admin. Hệ thống shall cho phép quản lý thể loại; thể loại đang được phim tham chiếu không được xóa.
- **FR-MGT-ROOM-001** — Quản lý phòng và ghế. Actor: Manager/Admin. Hệ thống shall cho phép cấu hình phòng và sơ đồ ghế theo BR-ROOM-001..004; khi phòng đã có suất được công bố, cấu trúc sơ đồ ghế không được thay đổi phá vỡ dữ liệu đã bán.
- **FR-MGT-ST-001** — Tạo/sửa suất chiếu. Actor: Manager/Admin. Hệ thống shall chỉ tạo/sửa suất khi hợp lệ: phim và phòng tồn tại và khả dụng, thời gian hợp lệ (`starts_at < ends_at <= room_available_at`), không chồng khoảng chiếm dụng phòng với suất khác (trừ suất `cancelled`) (BR-ST-001..003).
  Kiểm tra: tạo suất chồng lịch phòng bị từ chối với lý do rõ ràng.
- **FR-MGT-ST-002** — Hủy suất chiếu. Actor: Manager/Admin. Khi hủy, hệ thống shall lưu trạng thái `cancelled`, thời điểm và lý do (BR-ST-004). Workflow tự động xử lý booking đã thanh toán khi rạp hủy suất không thuộc phạm vi demo; chính sách quyền lợi khách cần xác nhận (OQ-05).
- **FR-MGT-ST-003** — Sửa suất đã có booking. Hệ thống shall không cho đổi phim/giờ/phòng của suất đã có booking `confirmed` trong MVP (BR-ST-005).

### 7.7. Quản trị booking, payment, tài khoản

- **FR-ADMIN-BOOK-001** — Tra cứu booking và vé. Actor: Staff/Manager/Admin theo quyền. Hệ thống shall cho phép tra cứu booking/vé theo mã hoặc thông tin khách, chỉ trong phạm vi quyền (OQ-04).
- **FR-ADMIN-PAY-001** — Xem trạng thái payment. Actor: Manager/Admin (phạm vi OQ-04). Hệ thống shall hiển thị các attempt, trạng thái và kết quả đối soát của một booking. Thay đổi trạng thái thủ công phải tuân thủ BR-PAY-001..008.
- **FR-ADMIN-USER-001** — Quản lý tài khoản và phân quyền. Actor: Admin. Hệ thống shall cho phép khóa/mở khóa tài khoản và cấp/thu hồi role có kiểm soát; đăng ký công khai không được cấp role quản trị (BR-AUTH-003).
  Kiểm tra: chỉ Admin thao tác được quản lý user/role.
- **FR-ADMIN-REPORT-001** — Xem báo cáo và thống kê. Actor: Manager/Admin (ranh giới PROPOSED — OQ-04). Hệ thống shall hiển thị báo cáo cơ bản (phạm vi chi tiết OQ-04; báo cáo phân tích nâng cao ngoài phạm vi demo).

## 8. Quy tắc nghiệp vụ chi tiết

### 8.1. Đặt vé và phân bổ ghế

| ID | Quy tắc |
|---|---|
| BR-BOOK-001 | Booking chỉ được tạo khi: khách đã đăng nhập; suất chiếu còn bán theo vòng đời BR-ST-003 (đang `scheduled`, không `cancelled`, chưa quá `booking_closes_at` nếu có — OQ-02); ít nhất một ghế khả dụng được chọn. Đây là kiểm tra authoritative do backend thực hiện trong transaction tạo booking; hiển thị sơ đồ ghế/lịch chiếu cho khách (FR-SHOW-001/002) chỉ là thông tin phục vụ chọn, không thay thế kiểm tra này. |
| BR-BOOK-002 | Tạo booking và toàn bộ phân bổ ghế là thao tác all-or-nothing trong một transaction: nếu một ghế trong lựa chọn bị xung đột, không ghế nào được giữ. |
| BR-BOOK-003 | Ghế trong một suất có ba trạng thái phân bổ: khả dụng (chưa có phân bổ hiệu lực — trạng thái suy ra, không lưu), `held` (đang giữ cho booking pending), `sold` (đã bán cho booking confirmed). Trạng thái vật lý của ghế (`active`/`maintenance`/`inactive`) là thông tin độc lập. |
| BR-BOOK-004 | Thời gian giữ ghế là 5 phút, tính từ thời điểm booking được tạo (CONFIRMED). Mốc 5 phút không được gia hạn kể cả khi booking còn payment `pending`/`unknown`. **Vòng đời booking và giải phóng ghế theo `database-design.md` (mục 34.1):** worker chỉ chuyển booking `expired` và các detail sang `released` khi booking quá hạn **và** không còn payment `created`/`pending`/`unknown` cần giữ để xử lý; nếu còn attempt đang mở, cả hai sự kiện được trì hoãn đến khi attempt chốt kết quả — đây là chính sách của nguồn, không phải gia hạn giữ ghế theo ý muốn client. Lưu ý: khoảng "quá hạn nhưng chưa hết hạn" này có thể vượt quá 5 phút nếu attempt kéo dài — đây chính là điểm cần nhóm chốt về cửa sổ chờ (OQ-07). |
| BR-BOOK-005 | Giá được backend tính khi tạo booking: giá cơ sở suất × sức chứa loại ghế (ghế Couple nhân 2), cộng phụ thu loại ghế một lần, trừ giảm giá theo loại vé (chỉ tính trên phần giá cơ sở, làm tròn VND nguyên đồng). Client không gửi giá/mức giảm. Giá và tổng tiền chốt tại thời điểm tạo, không tính lại khi danh mục/giá thay đổi. **Quyết định OQ-06 (đã chốt — Option B):** một booking detail được phép có giá 0 (`unit_price >= 0`, ví dụ giảm 100% trên detail đó), nhưng `booking.total_amount > 0` và `payment.amount > 0` — booking gồm toàn vé giá 0 (tổng 0) bị chặn và booking miễn phí toàn bộ/luồng xác nhận không-thanh-toán/hệ thống khuyến mãi nằm ngoài phạm vi MVP. Hỗ trợ mức database cho detail giá 0 không đồng nghĩa triển khai luồng vé miễn phí. |
| BR-BOOK-006 | Hai request đồng thời không thể giữ/bán cùng một ghế trong cùng suất: hệ thống tuần tự hóa qua transaction và từ chối phân bổ hiệu lực thứ hai. |
| BR-BOOK-007 | Booking `pending` tương ứng mọi detail `held`; `confirmed` tương ứng `sold`; `expired`/`cancelled` tương ứng `released`. |
| BR-BOOK-008 | Customer chỉ được hủy booking của chính mình khi booking `pending`, chưa có payment `succeeded` được áp dụng và không có payment `created`/`pending`/`unknown`. Nếu có attempt chưa có kết quả chắc chắn, hủy bị từ chối. Không hủy một phần. Không có hủy booking `confirmed` sau thanh toán trong demo. |
| BR-BOOK-009 | Mỗi booking detail có tối đa một ticket; ticket chỉ được phát hành khi payment hợp lệ được áp dụng; mỗi ticket `valid` chỉ được soát thành công một lần (chuyển `used` có điều kiện); ghế Couple được soát một lần cho cả ghế đôi. Cửa sổ soát vé chưa chốt (OQ-03). |
| BR-BOOK-010 | Một booking có thể gồm nhiều booking detail với các loại vé khác nhau (mỗi ghế một loại vé trong MVP); không hỗ trợ tách thanh toán theo từng detail. |

### 8.2. Phòng chiếu và ghế

| ID | Quy tắc |
|---|---|
| BR-ROOM-001 | Cinema chứa nhiều CinemaRoom; phòng chứa nhiều ghế. Sức chứa phòng được suy ra từ ghế và `capacity_units` của loại ghế (ghế Couple tính 2 chỗ), không lưu trường sức chứa riêng dễ sai lệch. |
| BR-ROOM-002 | Ghế được định vị bằng (hàng, số ghế) trong phòng; vị trí duy nhất trong phòng. Loại ghế xác định sức chứa và phụ thu. |
| BR-ROOM-003 | Phòng có trạng thái `active`/`maintenance`/`inactive`; phòng bảo trì không tạo suất mới; thay đổi tình trạng bảo trì phải xử lý các suất và đơn bị ảnh hưởng trước. |
| BR-ROOM-004 | Khi phòng đã có suất được công bố, cấu trúc sơ đồ ghế bị khóa (không thêm/xóa/chuyển ghế/đổi loại). Danh mục (phim, thể loại, loại ghế, loại vé) đã được sử dụng ưu tiên ngừng hoạt động thay vì xóa; dữ liệu booking/payment/ticket không bị xóa dây chuyền. |

### 8.3. Suất chiếu

| ID | Quy tắc |
|---|---|
| BR-ST-001 | Suất chiếu hợp lệ phải xác định được: phim, phòng, thời gian bắt đầu, thời gian kết thúc, thời điểm phòng rảnh — với `starts_at < ends_at <= room_available_at`; và giá cơ sở. |
| BR-ST-002 | Hai suất trong cùng phòng không được chồng khoảng chiếm dụng `[starts_at, room_available_at)`, trừ suất đã `cancelled`. Ràng buộc được bảo vệ bằng exclusion constraint ở mức database; kiểm tra trước để báo lỗi thân thiện. |
| BR-ST-003 | Vòng đời suất: `draft -> scheduled -> in_progress -> completed`; `draft`/`scheduled` có thể `cancelled`. Bán vé khi `scheduled` và chưa đóng bán. |
| BR-ST-004 | Hủy suất phải lưu trạng thái `cancelled`, thời điểm và lý do. Suất đã hủy không chiếm lịch phòng. Khôi phục suất đã hủy phải kiểm tra lại xung đột lịch phòng. |
| BR-ST-005 | Cho phép sửa giờ/phòng khi suất chưa có booking và không vi phạm lịch. Suất đã có booking `confirmed` không được đổi phim/giờ/phòng trong MVP. Workflow tự động thông báo/đổi lịch/hoàn tiền khi rạp hủy suất ngoài phạm vi demo; chính sách quyền lợi khách đã thanh toán cần chủ dự án xác nhận (OQ-05) — việc chưa triển khai workflow không có nghĩa là nghiệp vụ đã quyết định không hoàn tiền. |

### 8.4. Thanh toán

| ID | Quy tắc |
|---|---|
| BR-PAY-001 | Payment attempt được tạo với số tiền và tiền tệ đúng bằng tổng tiền/tiền tệ của booking tại thời điểm tạo; mọi attempt của cùng booking dùng cùng giá trị (booking bất biến sau tạo). |
| BR-PAY-002 | Một booking không có nhiều attempt đang mở (`created`/`pending`/`unknown`) tại cùng thời điểm (bảo đảm bởi partial unique index). Retry phải chờ attempt cũ có kết quả chắc chắn. Attempt đang mở không tự chuyển thành `failed` chỉ vì booking hết hạn — kết quả của nó phải đến từ cổng (xem BR-PAY-003, BR-PAY-007). |
| BR-PAY-003 | `unknown` biểu thị kết quả chưa xác định, không mặc nhiên là thất bại. Payment `pending`/`unknown` phải được đối soát với cổng trước khi được retry; kết quả chỉ chốt sau khi xác minh với cổng. Booking hết hạn không tự động coi payment `pending`/`unknown` là thất bại — kết quả tài chính và trạng thái booking là hai kết quả độc lập. |
| BR-PAY-004 | Chỉ payment `succeeded`, đã xác minh (đúng định danh yêu cầu, số tiền, tiền tệ, nguồn/chữ ký) và booking còn đủ điều kiện (`pending`) mới được áp dụng: trong một transaction — đánh dấu áp dụng, booking `pending -> confirmed`, toàn bộ detail `held -> sold`, tạo ticket. Không áp dụng một phần. |
| BR-PAY-005 | Phân biệt bốn khâu xử lý sự kiện: tiếp nhận (lưu bền vững, `received`) — xác minh nguồn/chữ ký — xử lý nghiệp vụ — đánh dấu `processed` cùng transaction nghiệp vụ. Callback không xác minh được không ảnh hưởng trạng thái payment/booking/ghế/ticket. Trạng thái payment là trạng thái nội bộ; trạng thái báo từ cổng chỉ là dữ liệu đầu vào qua payment event, phải qua xác minh mới ảnh hưởng trạng thái. Booking `expired` là sự kiện vòng đời của booking, không phải trạng thái payment. |
| BR-PAY-006 | Callback lặp được nhận diện qua khóa `(payment, event_key)` và bỏ qua xử lý; callback trùng không tạo thêm ticket hay áp dụng payment lần hai. Hai worker không được đồng thời xử lý cùng một event gây cập nhật nghiệp vụ trùng; cập nhật nghiệp vụ và đánh dấu event `processed` nguyên tử trong cùng transaction xử lý. Việc lưu event ban đầu và xử lý nghiệp vụ không bắt buộc cùng một transaction (tách để retry bền vững). |
| BR-PAY-007 | Payment `succeeded` đến muộn khi booking đã `expired`/`cancelled` hoặc không còn đủ điều kiện: không tự động áp dụng, không xác nhận booking, không phát hành ticket, không chiếm lại ghế đã giải phóng; kết quả thành công giữ nguyên trạng thái `succeeded` để đối soát. Xử lý tài chính tiếp theo (refund) ngoài phạm vi demo trừ khi nghiệp vụ quy định rõ (OQ-07). |
| BR-PAY-008 | Payment `failed`/`cancelled` không làm thay đổi booking; booking còn hạn có thể thử thanh toán lại. Không hạ trạng thái payment đã áp dụng thành `failed` chỉ vì callback đến muộn hoặc lặp. |

**Phân biệt các nhóm trạng thái:** trạng thái booking (`pending`/`confirmed`/`expired`/`cancelled`), trạng thái phân bổ ghế trong suất (`held`/`sold`/`released`), trạng thái payment (`created`/`pending`/`unknown`/`succeeded`/`failed`/`cancelled`), trạng thái ticket (`valid`/`used`/`voided`) là bốn nhóm độc lập; payment `succeeded` không tự động là bằng chứng booking được xác nhận — cần đủ điều kiện áp dụng (BR-PAY-004).

### 8.5. Phân quyền và quản trị

| ID | Quy tắc |
|---|---|
| BR-AUTH-001 | Mọi thao tác quản trị yêu cầu phiên đăng nhập hợp lệ và role phù hợp; authorization thực hiện ở backend. |
| BR-AUTH-002 | Customer chỉ truy cập/sửa/hủy được booking, vé và hồ sơ của chính mình; truy cập tài nguyên của người khác bị từ chối kể cả khi biết mã công khai. |
| BR-AUTH-003 | Đăng ký công khai chỉ cấp role `CUSTOMER`; cấp/thu hồi role quản trị do backend kiểm soát (Admin). Role `inactive` không cấp quyền mới; tài khoản `locked`/`deactivated` không truy cập được. |
| BR-AUTH-004 | Access token đã phát hành giữ hiệu lực đến hết hạn trừ khi có cơ chế thu hồi; thay đổi role có thể không phản ánh ngay vào token đang hiệu lực — giới hạn MVP được chấp nhận (không thu hồi token tức thời; role bị thu hồi còn hiệu lực cho đến khi token hết hạn). |
| BR-AUTH-005 | Phân biệt quyền giữa Staff/Manager/Admin chưa chốt đầy đủ: Staff được xác nhận với nghiệp vụ soát vé và tra cứu; Manager (PROPOSED) quản lý phim/phòng/ghế/suất chiếu và xem báo cáo; Admin có toàn quyền gồm quản lý user/role. Ranh giới chi tiết (quản lý payment, hủy booking, báo cáo) là câu hỏi mở (OQ-04). |

## 9. Hành trình nghiệp vụ của khách hàng

**Luồng chính:** Home → Chi tiết phim → Chọn suất → Chọn ghế → Xác nhận đặt vé → Thanh toán → Kết quả thanh toán → Vé của tôi.

1. Khách duyệt phim và chọn suất chiếu còn bán (FR-SHOW-001).
2. Khách chọn ghế trên sơ đồ (FR-SHOW-002); ghế đang giữ/đã bán/bảo trì không chọn được.
3. Khách xác nhận đặt vé; backend kiểm tra lại toàn bộ ghế, tạo booking `pending` và giữ ghế, countdown 5 phút bắt đầu (FR-BOOK-001).
4. Khách thanh toán qua Payment Gateway (FR-PAY-001).
5. Khi payment thành công và được xác minh, booking confirmed và vé được phát hành (FR-PAY-005); khách xem vé tại "Vé của tôi" (FR-TICKET-001).

**Các tình huống thay thế và ngoại lệ:**

| Tình huống | Kết quả mong đợi |
|---|---|
| Chưa đăng nhập khi đặt vé | Hệ thống yêu cầu đăng nhập trước khi tạo booking; duyệt phim/xem lịch vẫn khả dụng. |
| Ghế vừa bị người khác giữ/bán | Backend từ chối toàn bộ lựa chọn (all-or-nothing), khách chọn lại ghế khác. |
| Hết thời gian giữ ghế (5 phút) | Theo `database-design.md` mục 34.1: worker chỉ chuyển booking `expired` + detail `released` khi quá hạn và không còn attempt `created`/`pending`/`unknown`; nếu còn attempt đang mở, cả hai sự kiện trì hoãn (BR-BOOK-004, OQ-07). Không gia hạn giữ ghế theo ý muốn client. |
| Payment thất bại hoặc hủy | Booking giữ nguyên `pending` nếu còn hạn, khách có thể thử lại (BR-PAY-008). |
| Payment `unknown` (timeout) | Không tạo attempt mở mới, không bán lại ghế đang phân bổ; chờ đối soát với cổng (FR-PAY-004); không gia hạn giữ ghế. |
| Callback lặp | Chỉ xử lý một lần, không tạo thêm ticket (FR-PAY-003). |
| Payment thành công khi booking đã hết hạn/hủy | Không xác nhận booking, không phát hành ticket; giữ kết quả để đối soát (BR-PAY-007). |
| Khách cố truy cập/hủy booking của người khác | Backend từ chối theo BR-AUTH-002, kể cả khi biết mã booking. |

## 10. Hành trình nghiệp vụ quản trị

Đường dẫn chung: Đăng nhập → Dashboard → các module quản trị theo quyền. CRUD không mặc nhiên có sẵn cho mọi role — quyền theo BR-AUTH-001..005 và OQ-04.

| Module | Actor chịu trách nhiệm | Mục đích | Ràng buộc quan trọng |
|---|---|---|---|
| Xác thực quản trị | Staff/Manager/Admin | Đăng nhập vào khu vực quản trị | Chỉ role được cấp; tài khoản `locked` bị từ chối |
| Dashboard | Manager/Admin | Tổng quan hoạt động | Phạm vi nội dung theo OQ-04 |
| Quản lý phim/thể loại | Manager/Admin | Duy trì danh mục phim chính xác | Thay đổi không phá dữ liệu đã tham chiếu (FR-MGT-MOVIE-*) |
| Quản lý phòng/ghế | Manager/Admin | Cấu hình rạp và sơ đồ ghế | Khóa sơ đồ khi phòng đã có suất công bố (BR-ROOM-004) |
| Quản lý suất chiếu | Manager/Admin | Lập lịch chiếu | Không chồng lịch phòng (BR-ST-002); hạn chế sửa suất đã có booking (BR-ST-005) |
| Tra cứu booking/vé | Staff/Manager/Admin | Hỗ trợ khách | Staff: soát vé + tra cứu; tra cứu sâu hơn theo OQ-04 |
| Soát vé | Staff | Xác thực vé tại rạp | Vé chỉ soát được một lần (FR-TICKET-002) |
| Xem payment | Manager/Admin (**PROPOSED** — OQ-04) | Kiểm tra trạng thái thanh toán | Chỉ xem và đối soát; không can thiệp trạng thái tùy tiện (BR-PAY-*); quyền xem payment chưa chốt |
| Quản lý user/role | Admin | Kiểm soát truy cập | Đăng ký công khai không cấp role quản trị (BR-AUTH-003) |
| Báo cáo/thống kê | Manager/Admin (**PROPOSED** — OQ-04) | Theo dõi kinh doanh | Báo cáo cơ bản trong MVP; nâng cao ngoài phạm vi; quyền chi tiết cần chốt |

Sơ đồ activity/sequence chi tiết và kiến trúc hệ thống thuộc `business-system-design.md` (use case UC-01..UC-05) và `docs/analysis and design/architecture.md`; tài liệu này không tái lập.

## 11. Yêu cầu phi chức năng

| ID | Nhóm | Yêu cầu | Kiểm tra |
|---|---|---|---|
| NFR-SEC-001 | Bảo mật | Mật khẩu chỉ lưu dạng hash do thư viện xác thực tạo; không ghi mật khẩu gốc hay secret/thẻ vào dữ liệu sự kiện thanh toán. | Review schema/log: không có plaintext mật khẩu hay dữ liệu thẻ. |
| NFR-SEC-002 | Bảo mật | Mọi endpoint nhạy cảm kiểm tra xác thực và role ở backend. | Truy cập endpoint không đúng role bị từ chối (401/403). |
| NFR-SEC-003 | Bảo mật | Callback từ cổng phải được xác minh nguồn/chữ ký trước khi ảnh hưởng trạng thái. | Callback sai chữ ký bị từ chối. |
| NFR-INT-001 | Toàn vẹn dữ liệu | Giữ ghế, tổng tiền, áp dụng payment và phát hành vé tuân thủ BR-BOOK-001..010, BR-PAY-001..008. | Test các kịch bản xung đột trong mục 12. |
| NFR-INT-002 | Toàn vẹn dữ liệu | Lịch sử booking/payment/ticket không bị mất khi xóa/ngừng hoạt động danh mục. | Ngừng hoạt động danh mục đã dùng, dữ liệu lịch sử còn nguyên. |
| NFR-REL-001 | Độ tin cậy | Callback và kết quả chưa chắc chắn không làm mất trạng thái: sự kiện được lưu bền vững ngay khi tiếp nhận (`received`) trong transaction riêng; xử lý nghiệp vụ và đánh dấu `processed` cùng transaction; lỗi xử lý cho phép retry từ trạng thái `received`/`failed`. Callback không xác minh được được lưu nhưng không thay đổi trạng thái nghiệp vụ. | Crash giữa tiếp nhận và xử lý không mất callback và không tạo ticket trùng; callback sai chữ ký không đổi trạng thái. |
| NFR-REL-002 | Độ tin cậy | Hai request đồng thời giữ cùng ghế: đúng một request thành công. | Test đồng thời theo mục 12. |
| NFR-PERF-001 | Hiệu năng | Thời gian phản hồi tra cứu và đặt vé đạt mục tiêu đã chốt: **TBD (OQ-09)**. | Khi có mục tiêu: đo theo kịch bản chốt. |
| NFR-AVAIL-001 | Khả dụng | Mức khả dụng mục tiêu: **TBD (OQ-09)**. | Khi có mục tiêu: đo theo kỳ chốt. |
| NFR-USE-001 | Dễ dùng | Countdown giữ ghế hiển thị rõ cho khách trong lúc booking pending. Lưu ý: countdown chỉ là hiển thị cho người dùng — cơ chế bảo vệ toàn vẹn giữ ghế nằm ở backend/database (BR-BOOK-004, BR-BOOK-006), không phụ thuộc countdown trên Frontend. | Giao diện hiển thị đếm ngược 5 phút; thử gửi request giữ ghế sau khi countdown kết thúc không được chấp nhận ở backend. |
| NFR-USE-002 | Phản hồi | Giao diện sử dụng được trên desktop và trình duyệt di động. | Kiểm tra bố cục trên hai loại thiết bị. |
| NFR-MAINT-001 | Bảo trì/kiểm thử | Trạng thái và quy tắc nghiệp vụ tập trung ở backend, kiểm thử được độc lập với giao diện. | Các FR/BR mục 7–8 test được qua API không cần UI. |
| NFR-SCALE-001 | Khả năng mở rộng | Mô hình dữ liệu sẵn sàng nhiều chi nhánh; hành vi đa chi nhánh không thuộc MVP. | Mô hình chấp nhận nhiều cinema; demo seed một cinema. |

## 12. Tiêu chí nghiệm thu và kiểm chứng

Các tiêu chí rút ra từ FR/BR đã chốt; không giới thiệu chính sách mới.

- **AC-BOOK-001 (Đặt ghế khả dụng)** — Given: khách đã đăng nhập và chọn các ghế khả dụng trong suất còn bán, When: gửi yêu cầu tạo booking, Then: booking `pending` được tạo với countdown 5 phút, toàn bộ ghế `held`, tổng tiền đúng giá backend tính, chưa phát hành vé nào.
- **AC-BOOK-002 (Chặn xung đột ghế)** — Given: hai khách đồng thời chọn cùng một ghế trong cùng suất, When: cả hai gửi yêu cầu tạo booking, Then: đúng một request thành công giữ ghế; request kia bị từ chối toàn bộ và không giữ ghế nào.
- **AC-BOOK-003 (Hết hạn giữ ghế)** — Given: booking `pending` đã quá hạn giữ ghế (quá 5 phút danh nghĩa) và không còn payment `created`/`pending`/`unknown`, When: worker/quy trình hết hạn chạy (hoặc khách khác đặt cùng ghế), Then: booking chuyển `expired`, các detail chuyển `released`, ghế có thể được booking khác giữ. Given: booking đã quá hạn giữ ghế nhưng vẫn còn attempt đang mở (booking chưa `expired`, ghế chưa `released`), When: worker chạy, Then: theo nguồn `database-design.md` mục 34.1, booking và ghế giữ nguyên trạng thái cho đến khi attempt chốt kết quả (cửa sổ chờ — OQ-07); nếu attempt sau đó thành công, áp dụng AC-PAY-004 (không xác nhận booking, không chiếm lại ghế). Lưu ý thuật ngữ: "quá hạn giữ ghế" (vượt mốc 5 phút danh nghĩa), "booking expired" (trạng thái booking sau khi worker xử lý) và "detail released" (trạng thái phân bổ ghế sau khi worker giải phóng) là ba khái niệm khác nhau — "quá hạn" không đồng nghĩa là đã `expired`/`released`; hai sự kiện sau chỉ xảy ra khi điều kiện hai phần của nguồn thỏa.
- **AC-BOOK-004 (Hủy booking hợp lệ)** — Given: booking `pending` không có payment nào đang mở, When: chủ booking hủy, Then: booking `cancelled`, toàn bộ ghế `released` trong cùng transaction.
- **AC-BOOK-005 (Từ chối hủy không hợp lệ)** — Given: booking có payment `pending`/`unknown` (hoặc người yêu cầu không phải chủ booking), When: yêu cầu hủy, Then: từ chối, trạng thái booking/payment/ghế không thay đổi.
- **AC-PAY-001 (Thanh toán thành công)** — Given: booking `pending` còn hạn và payment `succeeded` đã xác minh đúng số tiền/tiền tệ, When: áp dụng, Then: payment được đánh dấu áp dụng, booking `confirmed`, toàn bộ detail `sold`, ticket `valid` — trong cùng transaction.
- **AC-PAY-002 (Thanh toán thất bại)** — Given: payment `failed`/`cancelled`, When: kết quả được xác minh, Then: booking giữ `pending` nếu còn hạn, không phát hành ticket, khách có thể thử lại.
- **AC-PAY-003 (Callback lặp)** — Given: payment đã được xử lý từ callback đầu tiên, When: cùng callback gửi lại, Then: callback trùng **không gây tác động nghiệp vụ lần hai** — không tạo thêm ticket, không áp dụng payment lần hai, trạng thái không bị đảo ngược; việc hệ thống có ghi nhận bản ghi sự kiện trùng để truy vết hay không là thiết kế riêng (không phải điều kiện nghiệp vụ của tiêu chí này).
- **AC-PAY-005 (Callback không hợp lệ)** — Given: callback sai chữ ký/nguồn hoặc sai số tiền/tiền tệ, When: hệ thống tiếp nhận, Then: sự kiện được lưu để truy vết nhưng payment, booking, phân bổ ghế và ticket giữ nguyên trạng thái.
- **AC-PAY-004 (Payment thành công muộn)** — Given: booking đã `expired` (ghế đã giải phóng hoặc bán cho booking khác) và payment `succeeded` đến sau đó, When: kết quả được xác minh, Then: booking không được xác nhận, không tạo ticket, không chiếm lại ghế; payment giữ `succeeded` để đối soát.
- **AC-SEC-001 (Phân quyền role)** — Given: người dùng với role X, When: truy cập chức năng chỉ dành cho role Y, Then: hệ thống từ chối ở backend.
- **AC-SEC-002 (Bảo vệ booking người khác)** — Given: khách A biết mã booking/vé của khách B, When: A truy cập hoặc thao tác, Then: từ chối theo quyền sở hữu.
- **AC-SEC-003 (Tài khoản bị khóa / role bị thu hồi)** — Given: user có tài khoản `locked`/`deactivated` hoặc role đã bị thu hồi, When: user đăng nhập hoặc gọi API với phiên cũ, Then: đăng nhập bị từ chối; các thao tác đòi role bị từ chối ở backend theo BR-AUTH-003. Riêng với phiên/token cũ: theo BR-AUTH-004, trong MVP token đã phát hành còn hiệu lực đến hết hạn và role bị thu hồi có thể chưa được phản ánh ngay vào token đang hiệu lực — các thao tác bằng token cũ có thể tiếp tục được chấp nhận cho đến khi token hết hạn, trừ khi hệ thống có cơ chế kiểm tra quyền hiện hành hoặc thu hồi token tức thời (giới hạn này được ghi nhận là giới hạn MVP, không phải tiêu chí nghiệm thu bắt buộc).
- **AC-ST-001 (Suất chồng lịch)** — Given: phòng đã có suất chiếm khoảng `[starts_at, room_available_at)`, When: tạo/sửa suất khác trong cùng phòng chồng khoảng này, Then: từ chối với lý do rõ ràng; suất `cancelled` không tính là chiếm lịch.
- **AC-TICKET-001 (Soát vé một lần)** — Given: ticket `valid` của booking `confirmed`, When: staff soát lần một và lần hai, Then: lần một chuyển `used` thành công; lần hai bị từ chối.

## 13. Câu hỏi mở và quyết định cần chốt

| ID | Vấn đề chưa chốt | Vì sao quan trọng | Tham chiếu | Trạng thái | Phương án đề xuất |
|---|---|---|---|---|---|
| OQ-01 | Cổng thanh toán cụ thể nào được sử dụng | Ảnh hưởng webhook, xác minh, đối soát | BR-PAY-*, `database-design.md` | OPEN | Chọn cổng hỗ trợ webhook + truy vấn trạng thái; giữ payment event trung gian cổng-độc lập |
| OQ-02 | Mốc đóng bán cuối cùng (`booking_closes_at`) | Quyết định thời điểm ngừng bán vé | BR-BOOK-001, BR-ST-003 | OPEN | Hiện cho phép bán sát giờ; nếu chốt thì đặt `booking_closes_at` theo chính sách vận hành |
| OQ-03 | Cửa sổ soát vé (thời hạn vé hợp lệ so với giờ chiếu) | Kiểm soát soát vé trước/sau giờ chiếu | BR-BOOK-009, `database-design.md` | OPEN | Đề xuất cho phép soát từ khi suất bắt đầu đến mốc nhóm chốt |
| OQ-04 | Ranh giới quyền Manager/Admin/Staff (quản lý payment, hủy booking, tra cứu, báo cáo) | Tránh cấp thừa/thiếu quyền | BR-AUTH-005, mục 10 | OPEN (Manager là PROPOSED) | Manager: phim/phòng/ghế/suất/báo cáo; Admin: thêm user/role; chi tiết payment cần chốt |
| OQ-05 | Chính sách quyền lợi khách đã thanh toán khi rạp hủy suất | Xác định nghĩa vụ hoàn tiền/đổi vé | BR-ST-005, `relational-model.md` mục 3.9/9.3 | OPEN | Cần quyết định nghiệp vụ; workflow demo chưa triển khai — chưa triển khai workflow không đồng nghĩa quyết định không hoàn tiền |
| OQ-06 | Chính sách vé miễn phí / giảm giá 100% | Đã chốt bởi chủ dự án | BR-BOOK-005, FR-BOOK-002, FR-PAY-001, FR-PAY-005, `relational-model.md` mục 3.14, `database-design.md` mục 13/13.1 | **RESOLVED — Option B (APPROVED):** `booking_detail.unit_price >= 0` (detail giá 0 được phép); `booking.total_amount > 0` và `payment.amount > 0` (booking tổng 0 bị chặn ở mức database); booking miễn phí toàn bộ, luồng xác nhận không-thanh-toán và hệ thống quản lý khuyến mãi nằm ngoài phạm vi MVP; MVP không tạo payment transaction số tiền 0; backend tính và kiểm tra giá từ dữ liệu server, không tin giá/tổng do client gửi. Đã đồng bộ: `database-design.md` mục 13/13.1, `relational-model.md` mục 3.14/9.2/9.3/10, `database-schema.md` mục 1/3.14 | Cần nhóm chọn một phương án — **đã chọn: Option B** |
| OQ-07 | Chính sách payment timeout và đối soát khoản thành công đến muộn; trong đó có **cửa sổ chờ giải phóng ghế**: theo `database-design.md` mục 34.1, worker chỉ chuyển booking `expired` + detail `released` khi quá hạn **và** không còn payment `created`/`pending`/`unknown` — tức booking quá hạn có thể vẫn `pending` (chưa expired) khi attempt kéo dài | Cần chốt: (a) booking quá hạn có attempt đang mở có được chuyển `expired` ngay không (điều kiện hai phần của nguồn có thể giữ booking `pending` quá 5 phút — xem như "gia hạn giữ ghế"?); (b) nếu có, độ dài/điều kiện kết thúc | BR-BOOK-004, BR-PAY-002, BR-PAY-003, BR-PAY-007, FR-PAY-004, AC-BOOK-003, `database-design.md` mục 34.1, UC-03 | OPEN (cửa sổ chờ: độ dài/điều kiện kết thúc chưa định nghĩa; workflow đối soát tài chính/hoàn tiền ngoài phạm vi demo) | Phương án: (A) giữ nguyên điều kiện hai phần của nguồn — booking chỉ expired khi hết hạn và không còn attempt đang mở (khoảng chờ phụ thuộc attempt); (B) booking luôn expired đúng 5 phút, chỉ release có điều kiện — cần sửa `database-design.md`/relational-model nếu chọn. Trong demo kịch bản chuẩn attempt chốt trước khi hết hạn nên hai sự kiện đồng thời |
| OQ-08 | Điều kiện đủ quyền lợi các loại vé giảm giá (trẻ em/sinh viên...) và cách bán ghế Couple trong UI | Backend cần kiểm tra điều kiện áp dụng | BR-BOOK-005, BR-BOOK-010 | OPEN | Backend xác minh (ví dụ xuất trình giấy tờ); Couple là một đơn vị bán không tách rời |
| OQ-09 | Mục tiêu hiệu năng và khả dụng cụ thể (thời gian phản hồi, uptime, tải đồng thời) | NFR-PERF-001/NFR-AVAIL-001 cần số đo | Mục 11 | TBD | Đặt mục tiêu khi chốt môi trường triển khai |

**Đã chốt (không liệt kê là câu hỏi mở):** giữ ghế 5 phút; hủy trước thanh toán giải phóng ghế; không hủy sau thanh toán/không refund trong demo; không cutoff cố định 10/15/30 phút; ghế Couple là một đơn vị bán (một detail, một ticket); một booking nhiều loại vé (mỗi ghế một loại vé); trạng thái payment và vòng đời xử lý callback như mục 8.4.
