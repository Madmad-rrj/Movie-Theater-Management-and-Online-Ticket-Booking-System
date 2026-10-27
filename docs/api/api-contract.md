# API Contract — CIN-T-011

## 1. Phạm vi và nguồn đối chiếu

Contract FE–BE tại prefix `/api`, **bản thiết kế để review**, không phải bằng chứng API đã triển khai hoặc mọi quyết định đã được nhóm duyệt. Quy tắc nghiệp vụ dẫn nguồn; quy ước wire mới là đề xuất của CIN-T-011. `TBD` nghĩa là chưa đủ quyết định để triển khai phần liên quan, không phải quyền được cấp ngầm.

[API Endpoints](api-endpoints.md) quản lý method, URL, tham số, schema và quyền từng route; tài liệu này quản lý kiểu, schema, lỗi và bất biến dùng chung. [DTO Specification](dto-specification.md) là draft Proposed đã có; khác biệt cần đồng bộ được ghi ở §8, không tạo thêm tài liệu DTO. Vị trí hai file tuân theo [PROJECT_STRUCTURE.md](../../PROJECT_STRUCTURE.md).

| Nguồn | Nội dung đối chiếu |
|---|---|
| [Requirements](../requirements/business-system-analysis.md) §3, §7–8, §13 | Phạm vi demo, FR/BR, actor, OQ-01..09 |
| [Use cases](../requirements/use-cases.md) UC-01..11 | Booking, callback, worker, soát vé, đăng nhập, checkout, hủy pending, chatbot |
| [Business design](../analysis%20and%20design/business-system-design.md) §0–1; [Architecture](../analysis%20and%20design/architecture.md) §5–9, §12–14 | Ranh giới actor, JWT, service/transaction, gateway adapter |
| [Database design](../../database/database-design.md) §12–17, §31–35; [Schema](../../database/database-schema.md); [SQL](../../database/database-schema.sql); [Relational model](../analysis%20and%20design/relational-model.md) | Kiểu/enum/constraint thực tế, snapshot, concurrency, payment/refund |
| [Customer flow](../ui-ux/customer-user-flow.md) §2–8 | Chọn ghế → Booking Confirmation → Payment → Payment Result → My Tickets |
| [Screen specification](../ui-ux/customer-screen-specification.md), [Screen–API mapping](../ui-ux/customer-screen-api-mapping.md), [UI state](../ui-ux/customer-ui-state.md), [Navigation](../ui-ux/customer-navigation.md) | Dữ liệu màn hình, khôi phục phiên và chỗ chưa đồng bộ với flow mới |

Không có refund, hủy booking đã thanh toán hoặc workflow hủy suất trong demo theo phạm vi DB design; schema có bảng/trạng thái tương lai không tự mở chức năng. Mâu thuẫn FR-MGT-ST-002 về hủy suất giữ tại TBD-12. UC-11 chatbot xung đột phạm vi architecture, theo TBD-16.

## 2. Quy ước dữ liệu

### 2.1. HTTP và JSON

- HTTPS khi triển khai; URL local theo cấu hình. Giữ prefix `/api`, không tự thêm `/v1`.
- JSON UTF-8, `Content-Type: application/json`; tên trường camelCase, enum phân biệt hoa/thường. GET không có body.
- Thành công: `{ "data": ... }`; danh sách phân trang thêm `pagination`. Shape DTO cũ là phần bên trong `data`. HTTP 204 không body. Callback gateway theo hợp đồng riêng TBD-01, không mặc định envelope/ACK JSON.
- Request chỉ nhận trường được schema cho phép; trường lạ hoặc server quản lý → `400 VALIDATION_ERROR`, không gán trực tiếp payload vào entity.
- Thiếu/sai kiểu/định dạng → 400; đúng cấu trúc nhưng sai miền/quan hệ → 422; xung đột trạng thái/tài nguyên hiện tại → 409. Backend luôn kiểm tra lại sau khóa.
- PATCH: thiếu trường giữ nguyên, null chỉ xóa trường nullable; PUT thay toàn bộ trường có thể ghi trong schema. ID/timestamp/audit do server quản lý.

### 2.2. Kiểu dùng chung

| Kiểu | JSON / giới hạn |
|---|---|
| `Id` | Chuỗi thập phân dương, regex `^[1-9][0-9]*$`, giá trị ≤9223372036854775807; ánh xạ bigint PK/FK. FE không ép Number. Đây là đề xuất wire, không đổi PK sang UUID. |
| `Uuid` | Chuỗi UUID chuẩn có gạch nối; dùng bookingCode, ticketCode, requestKey. Backend sinh mã booking/vé. |
| `Instant` | Chuỗi ISO 8601 UTC có Z, ví dụ `2026-10-27T08:30:00Z`. Input phải có múi giờ, backend chuẩn hóa UTC. |
| `Date` | Ngày hợp lệ YYYY-MM-DD; releaseDate không phải instant. Ngày xem ở Asia/Ho_Chi_Minh, backend đổi `[00:00, 00:00 ngày sau)` thành UTC. |
| `Money` | JSON integer VND 0..99999999999999, numeric(14,0); trường yêu cầu >0 ghi riêng. Giá/booking/payment dự kiến có currency=VND. |
| `DecimalAmount` | Chuỗi thập phân không phân nhóm, tối đa 16 chữ số nguyên và 2 chữ số lẻ, phục vụ thực thu/hoàn numeric(18,2), tránh mất chính xác JS. |
| `Percent` | JSON number 0..100, tối đa 2 chữ số lẻ; chỉ request quản trị có quyền mới được gửi. |

bookingId/paymentId trong route là Id, bookingCode là Uuid để tra cứu; ticketCode trong route là Uuid. Biết mã không thay authorization. Không trả password hash, raw callback, fingerprint, secret hoặc dữ liệu khách khác.

### 2.3. Enum

| Tên | Giá trị |
|---|---|
| MovieStatus | coming_soon, now_showing, ended, archived |
| ShowtimeStatus | draft, scheduled, in_progress, completed, cancelled |
| BookingStatus | pending, confirmed, expired, cancelled |
| AllocationStatus | held, sold, released (detail lịch sử) |
| SeatAvailability | available, held, sold (suy ra theo suất, không phải seat.status) |
| PhysicalStatus | active, maintenance, inactive (phòng/ghế); cinema chỉ active/inactive |
| PaymentStatus | created, pending, unknown, succeeded, failed, cancelled |
| ReconciliationStatus | none, needs_review, refund_required, resolved |
| TicketStatus | valid, used, voided |
| UserStatus | active, locked, deactivated |
| RoleCode | CUSTOMER, STAFF, MANAGER, ADMIN; một user có thể nhiều role |

Enum tồn tại không có nghĩa client được yêu cầu mọi transition. refund_required không có nghĩa đã hoàn tiền.

## 3. Response, HTTP status và lỗi

### 3.1. Envelope

```json
{
  "data": [],
  "pagination": { "page": 1, "pageSize": 20, "totalItems": 0, "totalPages": 0 }
}
```

pagination chỉ có ở danh sách P. Trang vượt cuối trả mảng rỗng, vẫn giữ tổng thực tế. Các trường pagination là integer; totalPages=ceil(totalItems/pageSize).

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Dữ liệu không hợp lệ",
    "details": [
      { "field": "items[0].seatId", "code": "INVALID_FORMAT", "message": "ID phải là chuỗi số nguyên dương" }
    ]
  }
}
```

error.code/message/details bắt buộc. details là mảng `{ field: string|null, code: string, message: string }`, rỗng khi không có chi tiết. Lỗi ghế ghi vị trí items[i].seatId với code SEAT_ALREADY_TAKEN, không đổi kiểu details thành mảng ID. FE phân nhánh theo code; không trả SQL/stack trace/PII.

### 3.2. HTTP status

| HTTP | Ý nghĩa / mã lỗi |
|---|---|
| 200 | Đọc/cập nhật thành công; replay booking hoặc trả attempt hiện có |
| 201 | Tạo mới; booking/payment có Location trỏ GET tài nguyên |
| 204 | Logout/xóa thành công, không body |
| 400 | JSON/query/path/kiểu không hợp lệ hoặc thiếu trường: VALIDATION_ERROR |
| 401 | Thiếu/hết hạn/sai bearer: UNAUTHENTICATED; login sai hoặc user không active: INVALID_CREDENTIALS, thông báo chung |
| 403 | Đúng identity nhưng thiếu role/ownership: FORBIDDEN |
| 404 | Không có tài nguyên hợp lệ tương ứng: MOVIE_NOT_FOUND, SHOWTIME_NOT_FOUND, BOOKING_NOT_FOUND, PAYMENT_NOT_FOUND, TICKET_NOT_FOUND hoặc RESOURCE_NOT_FOUND |
| 409 | Trùng key, trạng thái đổi, tài nguyên bận/race: bảng §3.3 |
| 415 | Media type không hỗ trợ: UNSUPPORTED_MEDIA_TYPE |
| 422 | Sai miền/quan hệ: VALIDATION_ERROR; vé không hợp lệ: TICKET_NOT_VALID |
| 500 | INTERNAL_ERROR, thông điệp chung |
| 503 | SERVICE_UNAVAILABLE; không biến payment chưa rõ kết quả thành failed |

Lỗi chung 400/415/422/500/503 áp dụng khi phù hợp; 401/403 cho route bearer. Catalog nêu thêm lỗi riêng. Chưa chốt quota/rate limit nên chưa cam kết 429 hay số ghế tối đa (TBD-18).

### 3.3. Mã 409 nghiệp vụ

| Mã | Điều kiện / phản ứng |
|---|---|
| EMAIL_ALREADY_EXISTS | Trùng lower(trim(email)); sửa email hoặc đăng nhập |
| SEAT_ALREADY_TAKEN | Một ghế bị giữ/bán; rollback toàn bộ, tải lại sơ đồ |
| SHOWTIME_CLOSED | Suất không còn bán; tải lại lịch |
| REQUEST_KEY_CONFLICT | Cùng user/key khác fingerprint; không tự đổi key để che lỗi |
| BOOKING_EXPIRED | Không tạo thanh toán mới sau hạn; đọc booking/payment |
| BOOKING_ALREADY_CONFIRMED | Đã xác nhận, không thanh toán thêm |
| BOOKING_NOT_PAYABLE | Đơn cancelled hoặc trạng thái không cho thanh toán |
| BOOKING_NOT_CANCELLABLE | Không pending, đã thu tiền hoặc chưa rõ kết quả; không tự nhả ghế |
| PAYMENT_ATTEMPT_OPEN | Mã draft cũ; UC-09 đề xuất 200 attempt hiện có thay vì lỗi, xem TBD-09 |
| TICKET_ALREADY_USED | Không ghi đè thời gian/người soát lần đầu |
| SHOWTIME_ROOM_OVERLAP | Chồng khoảng chiếm phòng, gồm dọn phòng |
| RESOURCE_IN_USE | Xóa/sửa phá lịch sử hoặc quan hệ đang dùng |
| SHOWTIME_LOCKED | Thay đổi cấu trúc suất bị chặn, xem TBD-13 |

Tên lỗi mới là đề xuất để FE/BE review. Ánh xạ constraint có chủ đích, không coi mọi lỗi DB là 409.

## 4. Phân trang, lọc, sắp xếp

- P: page integer ≥1 mặc định 1; pageSize integer 1..100 mặc định 20; sai →400, không tự clamp.
- sort nhận một trường allowlist, tiền tố `-` giảm dần; thêm id ASC theo giá trị bigint làm tie-breaker. Offset pagination có thể dịch trang khi dữ liệu thay đổi.
- Query ngoài allowlist, scalar lặp, sort/enum sai →400. q là text trim không rỗng ≤255, tìm tiêu đề chứa chuỗi không phân biệt hoa/thường, không regex/SQL wildcard.

| Danh sách | Filter ngoài P/sort | Sort được phép; mặc định |
|---|---|---|
| Phim công khai | status=now_showing hoặc coming_soon; q; genreId: Id. Thiếu status lấy hai nhóm, không trả archived | title, -title, releaseDate, -releaseDate; title; ngày null cuối |
| Suất theo phim | date: Date bắt buộc, cinemaId: Id tùy chọn; chỉ scheduled còn bán | startsAt, -startsAt; startsAt |
| Booking của tôi | status: BookingStatus tùy chọn | createdAt, -createdAt; -createdAt |
| Phim quản trị | q, genreId, status: MovieStatus; mọi trạng thái | Như phim công khai |
| Danh mục/quản trị khác | Chỉ filter ghi trong catalog; không tự tìm PII | id, -id; id |

SeatMap và mảng con details/tickets/genres/roles không phân trang riêng. GET không giữ ghế, tạo booking/payment hay charge gateway.

## 5. Bất biến backend/database

### 5.1. Chọn ghế và tạo booking

Nguồn: FR-BOOK-001..004, BR-BOOK-001..009; DB design §12–13, §31.1–31.2; UC-01/06/09.

1. Chọn ghế trên UI là cục bộ. POST booking commit mới giữ ghế; checkout dùng booking đó, không tạo đơn thứ hai.
2. User từ principal. Sau khóa, kiểm tra lại suất scheduled/còn bán, room/seat active, ghế đúng phòng, ticket type hợp lệ, items ít nhất một và không trùng seatId. Không tự thêm cutoff hoặc điều kiện vé giảm (TBD-03/04).
3. Booking + mọi detail held trong một transaction; lỗi một ghế rollback toàn bộ. SQL ux_booking_detail_active_seat, FK ghép và unique(booking_id,seat_id) bảo vệ race/đúng phòng.
4. Unique(user_id,request_key): cùng fingerprint trả 200 booking hiện tại, không gia hạn/reopen; khác trả 409. Backend tính SHA-256, không nhận fingerprint từ client. Retry timeout dùng key/body cũ; chuẩn hóa thứ tự/contact còn TBD-05.
5. Giữ danh nghĩa 5 phút; createdAt là thời gian thực sau khóa, expiresAt do backend trả. Rút ngắn gần cutoff còn TBD-03; không gia hạn theo client.
6. Snapshot giá/lựa chọn bất biến; đổi ghế phải hủy pending hợp lệ rồi tạo đơn khác; không reopen expired/cancelled.
7. baseAmount=basePrice×capacityUnits; discountAmount=round(baseAmount×discountPercent/100) theo quy tắc làm tròn VND của DB; unitPrice=baseAmount+surchargeAmount−discountAmount. Phụ thu một lần/đơn vị bán, discount chỉ trên base. Tổng sum detail gốc, không tính lại sau release. Detail có thể 0 nhưng tổng booking/payment >0 (OQ-06).
8. Couple một ghế/một detail capacity=2, một ticket type cho detail, hai vé/QR độc lập. Không quantity tùy ý hoặc tách bán nửa ghế. Điều kiện loại vé còn TBD-04.

### 5.2. Hủy pending và expiry

Nguồn: FR-BOOK-003, BR-BOOK-004/008; UC-03/10; DB design §31.5, §34.1.

- Customer chỉ hủy đơn mình pending, chưa có tiền đã thu cần xử lý và không còn attempt mở chưa được xác minh đóng. Với created/pending/unknown, Cancel/Query chạy ngoài transaction; lấy lại khóa/đọc lại kết quả xác minh trước cancelled/released. Đã thu hoặc chưa rõ → từ chối hủy, giữ dữ liệu đối soát.
- Booking cancelled và toàn bộ held→released atomic. Hủy lại cancelled trả 409; timeout thì GET để khôi phục. Lý do hủy do server ghi cho request không body còn TBD-06.
- Countdown 0 không chứng minh expired hoặc available. Worker xử lý; rời trang không hủy đơn/nhả ghế. Grace được duyệt về cơ chế, số phút/retry/escalation còn TBD-07. Không rõ kết quả giữ unknown/needs_review, không tự retry charge.
- Failed còn hạn có được giữ ghế/thử lại và payment trong grace có được apply còn mâu thuẫn TBD-08/10. Grace không tự là gia hạn expiresAt.

### 5.3. Payment và callback

Nguồn: BR-PAY-001..008; UC-02/09; DB design §15, §31.3–31.4; architecture §9, §13–14.

- Một attempt mở created/pending/unknown mỗi booking (uq_payment_open_booking); một payment applied mỗi booking (ux_payment_applied_booking). Nhiều succeeded vẫn được lưu để phản ánh thu trùng.
- Amount/currency từ booking; merchantRequestId do server sinh và giữ nguyên qua retry cùng attempt. Tạo attempt commit trước network, gọi gateway ngoài transaction. Timeout là unknown, không tự failed hoặc tạo reference mới.
- POST payment đề xuất 201 mới, 200 attempt mở hiện có theo UC-09; không đổi provider của attempt đang mở. Khác provider và điều kiện retry còn TBD-09. Không nhận amount/status/returnUrl tùy ý từ FE.
- Redirect trình duyệt không chứng minh thu tiền. Backend xác minh chữ ký/nguồn, merchant request, provider transaction và amount/currency.
- Callback không dùng JWT user. Provider, method/raw payload/header/chữ ký, eventKey, chuẩn hóa tiền, ACK/content type/status/retry là TBD-01. Route hiện có là điểm tích hợp dự kiến, không cam kết mọi cổng POST JSON/200.
- Event phải lưu bền vững trước ACK; dedup(payment_id,event_key). Processed không apply lại; received/failed được phục hồi, không bỏ retry chỉ vì key đã tồn tại. Trình tự verify/lưu callback sai chữ ký còn TBD-11; callback giả không được sửa dữ liệu tài chính/booking.
- Áp dụng hợp lệ: payment applied + booking confirmed + mọi detail sold + đúng số ticket + event processed trong cùng transaction. Khóa nhất quán Showtime→Booking→Payment→Event với các tài nguyên tham gia; không processed trước nghiệp vụ commit.
- succeeded chỉ là tài chính. FE cần appliedAt khác null và booking confirmed rồi đọc tickets. Late/duplicate success không chiếm lại ghế/reopen/phát thêm vé. Sai tiền/tệ không apply; cách ghi trạng thái tài chính còn TBD-11.

### 5.4. Soát vé và refund

- Staff kiểm tra booking confirmed, detail sold, ticket valid rồi cập nhật có điều kiện valid→used kèm principal/thời gian. Hai lần quét chỉ một thành công; validatedAt ánh xạ ticket.used_at. Cửa sổ/ngữ cảnh suất còn TBD-14, không tự chọn 30 phút.
- Không có API refund demo. needs_review/refund_required chỉ báo đối soát, không hứa tự hoàn tiền muộn/hủy suất. Actor/quyền lợi/trigger/gateway refund còn TBD-12/15.
- Thiết kế DB tương lai: một full refund/payment succeeded đã xác minh, amount/currency thực thu; merchantRefundId ổn định qua retry, timeout unknown/query trước retry; chỉ succeeded khi gateway xác nhận. Không đổi payment succeeded thành failed hoặc xóa appliedAt. Đây không phải cam kết triển khai refund.

### 5.5. Quản trị và ranh giới bảo đảm

- Phim/phòng khả dụng, startsAt<endsAt≤roomAvailableAt; khoảng [startsAt,roomAvailableAt) gồm dọn phòng không chồng cùng phòng (ex_showtime_room_time). basePrice>0; bookingClosesAt nếu có ≤startsAt. Nguồn dọn phòng/publish/cutoff còn TBD-03/13.
- Không đổi phim/phòng/giờ của suất có confirmed booking. Pending còn mâu thuẫn TBD-13; PUT không được phá snapshot. Giá mới chỉ cho booking mới qua thao tác kiểm soát.
- Cấu trúc/trạng thái phòng/ghế phải khóa/kiểm tra lịch và đơn chịu ảnh hưởng; không xóa danh mục được tham chiếu/phá lịch sử. Không coi mọi DELETE là soft-delete.
- SQL bảo vệ PK/FK/miền/unique/exclusion. Service phải bảo đảm ít nhất một detail, tổng SUM, snapshot bất biến, transition liên bảng, cardinality vé và authorization. Không tuyên bố SQL đã có trigger cho các việc này; task không triển khai backend hoặc sửa SQL.

## 6. Request/response theo nghiệp vụ

Shape hiện có tại [DTO Specification](dto-specification.md). CIN-SUB-070 bổ sung schema với kiểu/required/optional/nullable tại mục này. UUID cho PK, mã BK-/TK- và response không envelope của draft không phải quyết định wire cuối (§2, TBD-17).

## 7. Authentication và authorization

JWT bearer theo architecture §7: kiểm tra email/password/account active, đọc roles, ký subject/roles/hạn. Request xác minh token trước truy cập; principal là nguồn current user, không nhận userId/role/validatedByUserId từ body/query hoặc token chưa xác minh.

Đăng ký chỉ CUSTOMER. Backend kiểm tra role và ownership; biết mã không đủ quyền. Thiếu bearer hợp lệ 401, thiếu quyền 403; role không tự kế thừa. Manager, vận hành tài chính/tra cứu mở rộng chờ OQ-04.

MVP không thu hồi tức thời token cũ (BR-AUTH-004, UC-08). Logout không hủy booking/payment và không bảo đảm bearer cũ mất hiệu lực; FE xóa phiên cục bộ. TTL/refresh/storage FE là TBD-02. Ma trận từng nhóm hoàn thiện trong CIN-SUB-071.

## 8. Decision register — TBD và khác biệt draft

Cần người phụ trách nghiệp vụ/FE/BE xác nhận trong review. Không triển khai phần phụ thuộc bằng giá trị tự đoán.

| ID | Câu hỏi / căn cứ và ảnh hưởng |
|---|---|
| TBD-01 | OQ-01: provider, method/headers/raw payload/chữ ký, ACK/error/retry, return URL cấu hình, Query/Cancel/recovery payUrl? vnpay trong DTO chỉ ví dụ. |
| TBD-02 | TTL, refresh, password policy, storage FE? Locked/role đổi ảnh hưởng token sống thế nào theo BR-AUTH-004? Không tự đặt 15 phút/thêm refresh route. |
| TBD-03 | OQ-02: đóng bán và expiresAt sát cutoff? DB design §12 lấy mốc sớm hơn, BR-BOOK-004 giữ 5 phút. Không tự thêm cutoff/gia hạn. |
| TBD-04 | OQ-08: điều kiện CHILD/STUDENT, bằng chứng soát, giới hạn ticket type Couple? Couple vẫn một detail/hai vé; không tự chốt chỉ ADULT. |
| TBD-05 | OQ-UC-05: chuẩn hóa/sort items/contact cho fingerprint; đảo thứ tự ghế là replay hay conflict? |
| TBD-06 | Contact từ profile hay nhập riêng? UI có contact, BookingCreate chưa có; snapshot và lý do hủy request không body thế nào? |
| TBD-07 | OQ-UC-03/04, OQ-07: grace/retry/escalation cụ thể? 2–3 phút chỉ đề xuất; hết chờ không tự suy chưa thu. |
| TBD-08 | UC-02 §6.6 yêu cầu chưa quá expiresAt khi apply; FR-PAY-005/BR-PAY-004 chủ yếu nêu pending. Thành công trong grace có apply không? |
| TBD-09 | UC-09 §13.5/A3/A7 trả attempt hiện có, DTO cũ 409. Review 201 mới/200 cũ; khác provider khi attempt mở dùng lỗi gì, lúc nào retry sau failed? |
| TBD-10 | DB design §12/34.4 cho retry failed còn hạn, §35 nói giải phóng ngay. Booking/ghế sau failed chính xác thế nào? |
| TBD-11 | FR-PAY-002/BR-PAY-005 lưu received trước verify; UC-02 verify ingress trước event. Callback sai chữ ký lưu security log hay event? Sai tiền đã thu thật: FR cấm đổi payment, UC-02 A4/DB §15.2 ghi succeeded/needs_review. Chốt ghi nhận; mọi nguồn cấm apply sai tiền. |
| TBD-12 | FR-MGT-ST-002 có hủy suất, DB §33 ngoài demo; xác nhận scope. OQ-05: quyền lợi khách/actor/workflow xử lý booking đã trả tiền? |
| TBD-13 | OQ-04/BR-ST-005: sửa suất có pending khóa tới đâu, publish draft thế nào, roomAvailableAt/dọn phòng nhập hay suy ra, đổi basePrice có kiểm soát? |
| TBD-14 | OQ-03/OQ-UC-07: cửa sổ soát, staff gắn suất/phòng nào, request cần showtimeId? Manager/Admin có được soát theo OQ-04? |
| TBD-15 | OQ-04: Manager/Admin payment/reconcile/hủy đơn/report/PII/refund; actor duyệt hoàn tiền? Staff chỉ admission lookup/validate đã xác định. |
| TBD-16 | UC-11 thêm chatbot nhưng architecture §8 loại AI demo. Chốt scope/provider/nguồn ngoài/retention/personal lookup; không cho chat sửa booking/payment hoặc mở báo cáo nội bộ. |
| TBD-17 | Đồng bộ DTO: bigint→Id string, bookingCode/ticketCode UUID, data envelope, app_user không có created_at, thêm appliedAt/physicalStatus/current payment. UI spec/mapping/navigation/state đặt Confirmation sau payment trái flow mới §2.1/3.6–3.8/8. Giữ nguồn để review riêng. |
| TBD-18 | Architecture §12a chỉ Proposed quota/anti-abuse; số ghế/pending booking/rate limit cụ thể? Không dùng số benchmark làm rule. |
| TBD-19 | Giá trước booking cần server quote hay thành phần/ước tính có nhãn? FE không chốt giá; không tự thêm quote route chưa duyệt. |
| TBD-20 | Sửa email/password/profile, khóa/deactivate, tự thu ADMIN/last-admin protection, vòng đời danh mục? Route quản trị mới là đề xuất, kiểm tra invariant trước khi mở. |

## 9. Truy vết và kiểm tra bàn giao

| Sub-task | Tài liệu |
|---|---|
| CIN-SUB-068 | §1–5: quy ước, HTTP/error, nghiệp vụ; decision register §8 |
| CIN-SUB-069 | api-endpoints.md: catalog không lặp schema |
| CIN-SUB-070 | Schema §6 và kiểu chung §2 |
| CIN-SUB-071 | Identity/role/ownership §7 và quyền từng route |

Review: route có schema/quyền/status hoặc TBD rõ; enum/kiểu/constraint khớp SQL; đủ use case/actor; JSON/link hợp lệ; PR chỉ CIN-T-011 và log member hiện tại. Kiểm tra tài liệu không thay test API, concurrency hoặc gateway khi triển khai.
