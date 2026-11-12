# API Contract

## Mục đích, nguồn và trạng thái

Bản soạn ngày 09/11/2026 cho C01 Web Application ↔ C02 Backend API của hệ thống quản lý rạp và đặt vé. Phần lớn catalogue vẫn là **thiết kế Proposed**; riêng `GET /api/movies` đã có implementation tối thiểu và được kiểm chứng với PostgreSQL local theo log công việc.

Nguồn đối chiếu: **Use-Case Specifications v2.4** (`rup_ucspec-v2.4_final(5).docx`, bản đính kèm) và **SAD v3.15** (`rup_sad(1).docx`). Trong repository, đối chiếu bản tương ứng ở `pa/PA3-Group15/` theo version/nội dung, không chỉ theo tên. Use-case quyết định hành vi; SAD quyết định component và ranh giới giao tiếp. Không dùng schema/Markdown cũ để khôi phục nghiệp vụ đã bị thay thế. Chưa kiểm tra repository hiện tại trong lần soạn này, nên không khẳng định source/schema vẫn là skeleton hoặc tất cả endpoint chưa tồn tại.

Tên endpoint, DTO, enum API, transport và mã lỗi dưới đây là các lựa chọn kỹ thuật đề xuất. Enum API là projection cần mapping; tài liệu không yêu cầu tạo trực tiếp các enum/cột database tương ứng. Các quy tắc nghiệp vụ đã chốt trong DOCX không bị coi là câu hỏi mở chỉ vì API chưa triển khai.

## HTTP, JSON và dữ liệu

- Base path `/api`; HTTPS ở môi trường online. JSON UTF-8, field camelCase. Không đưa credential/PII vào query string.
- Request có JSON body dùng `Content-Type: application/json`. GET/DELETE không có body trong catalogue. 204 không có body hoặc JSON content type bắt buộc.
- ID của entity có PK `bigint` trong schema (`movie`, `genre` và các quan hệ liên quan trong endpoint đọc phim) là chuỗi số thập phân trong JSON, ví dụ `"6"`, để không mất chính xác khi client JavaScript xử lý. Các ID UUID của những entity khác vẫn là UUID; `categoryCode` (`Adult`/`Discounted`) và mã public là các mã riêng, không phải UUID hay quyền truy cập.
- `Instant`: UTC ISO 8601 có `Z`. `Date`: YYYY-MM-DD theo timezone cinema, mặc định Asia/Ho_Chi_Minh. Khi không lọc cinema, backend diễn giải ngày riêng theo mỗi cinema, không lấy timezone trình duyệt.
- `Money`: `{ "amount": 120000, "currency": "VND" }`; amount nguyên không âm. Không trộn Money object và integer cùng một field giữa các API. Positive-total/payment eligibility vẫn do UC kiểm tra.
- Category cố định Adult/Discounted; role code CUSTOMER/STAFF/MANAGER/ADMIN. Mỗi physical seat là một chỗ; không Couple/capacity expansion.
- `officialStartsAt` hiện hành và `purchasedStartAt` snapshot là hai dữ liệu khác nhau.
- Nullability/optional theo DTO. Reject unknown write fields (400) để tránh mass assignment. Server-owned fields không được client gán; riêng dữ liệu bằng chứng hoàn tiền có client-supplied completedAt và phải xác minh.

## Response và collection

Application success có body:

```json
{ "data": { "bookingId": "00000000-0000-4000-8000-000000000001" } }
```

Collection phân trang:

```json
{ "data": [], "pagination": { "page": 1, "pageSize": 20, "totalItems": 0, "totalPages": 0 } }
```

Chỉ operation nhận PageQuery, MovieQuery, ShowtimeQuery hoặc OperationalQuery mới phân trang. Collection danh mục nhỏ không nhận pagination trả `{ "data": [] }`. Arrays lồng trong object không có pagination độc lập. page mặc định 1; pageSize mặc định 20, tối đa 100, riêng `GET /api/movies` mặc định 12 theo UC-06A. Ordering mặc định ổn định bằng ID trừ khi operation công bố khác; `GET /api/movies` sắp xếp release date giảm dần (NULL cuối), rồi movie ID giảm dần; filter không mở rộng quyền.

Create mới trả 201; async durable acceptance trả 202; read/update/replay trả 200; delete/logout thành công trả 204. Catalogue quy định ngoại lệ cụ thể. 201 có thể kèm Location của resource; không đưa secret vào Location.

Lỗi:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Thông tin liên hệ chưa hợp lệ.",
    "details": { "violations": [ { "field": "contact.email", "reason": "invalid_format", "message": "Email không hợp lệ." } ] },
    "traceId": "opaque-correlation-id"
  }
}
```

`details` là object theo mã lỗi, `{}` khi không có bổ sung. Không trả stack trace/SQL/raw provider response. 400 cho parse/type/required/format; 422 cho eligibility/nghiệp vụ; 409 cho state/concurrency conflict. Missing/invalid authenticated hoặc Guest credential =>401; credential hợp lệ nhưng thiếu quyền=>403, hoặc 404 để không tiết lộ resource. Provider ACK không dùng envelope này; mã ACK/signature/status mapping chốt khi tích hợp MoMo, không tự coi HTTP 200 là payment success.

## Authentication, Guest access và authorization

**Transport đề xuất để hai phía có contract cụ thể:** authenticated requests dùng `Authorization: Bearer <accessToken>` (JWT gắn server session theo C03). Không lưu token trong URL/log. Token signature và expiry chưa đủ: kiểm tra current session, account, roles, assignments trước secured action và trước protected commit. Login thành công thay thế session cũ; logout revoke current session. Không tự thêm refresh-token endpoint. Customer login không bắt buộc verified email; internal operations yêu cầu account active và verified email theo UC.

Guest dùng opaque server-issued HttpOnly Secure cookie `cinema_guest`; JSON chỉ chứa contextId không phải secret. `POST /api/guest-context` bootstrap/reuse Guest context; không reset selection. Browser gọi Guest API với credentials. Khi Vercel/Render khác site, cấu hình SameSite=None; exact CORS allowlist, Allow-Credentials và chống CSRF là bắt buộc. Đề xuất bootstrap trả `X-CSRF-Token`, Guest mutation gửi lại header này; backend kiểm tra token gắn context và Origin. Bearer credentials lấy precedence; request có Bearer không hợp lệ phải bị từ chối, không fallback Guest. Việc chuyển booking từ Guest sang account không được tự suy từ email trùng.

Guest demo lifetime đã thống nhất là **24 giờ tuyệt đối**, cấu hình qua `GuestCheckout:LifetimeHours`; bootstrap/reuse không gia hạn. Branch `feat/be-guest-context` triển khai cookie, hash-only storage, database-time validation, Origin và CSRF theo [Guest checkout](../development/guest-checkout.md). Bootstrap không nhận body, yêu cầu `Idempotency-Key` UUID và trusted Origin, trả CSRF header mà không đòi token có sẵn. Mutation Guest khác phải gửi cookie + CSRF token gắn context. HTTP cookie chỉ được phép trong Development, với opt-in rõ ràng, host và peer đều loopback; online bắt buộc HTTPS/Secure.

Replay bootstrap vẫn cần cookie tương ứng; idempotency key không cấp quyền khôi phục credential. Cookie sai/hết hạn/bị thu hồi trả 401 và bị xóa, không tự tạo danh tính thay thế. Authorization không được hỗ trợ trong branch Guest-only trả 401 và không fallback Guest. Recovery sau mất cookie cho checkout có dữ liệu và lifecycle production vẫn cần review. Không bổ sung public lookup theo email/phone/bookingCode; khi chưa có cơ chế recovery đã duyệt, trả hướng dẫn hỗ trợ, không nới quyền. QR admission không thay thế Guest proof.

| Policy | Quyền cụ thể |
|---|---|
| PUBLIC | Dữ liệu công khai hoặc bootstrap/register/login; vẫn validate/rate-limit |
| AUTH | Current authenticated session; không tự cấp operational permission |
| CUSTOMER | Current CUSTOMER account; dữ liệu lịch sử của chính account |
| CHECKOUT | Customer session hoặc Guest context hiện hành |
| OWNER | Owner của selection/booking/payment/conversation tương ứng hoặc Guest context đã gắn với resource; không chỉ biết ID |
| ADMIN | Current Admin, active, verified email; operation nằm trong quyền UC |
| OPS | Staff tại Staff–Cinema assignment, Manager tại Manager–Cinema assignment, hoặc Admin; chỉ read/support/review được công bố |
| MANAGE | Admin hoặc Manager được giao cinema của resource; không gồm Staff |
| REFUND | Admin hoặc assigned Manager; Staff không ghi completion/evidence theo API này |
| ADMISSION | Explicit STAFF role + Staff–Cinema assignment; Manager/Admin không tự thừa hưởng |
| ASSIGN | Admin; hoặc assigned Manager chỉ add/remove STAFF assignment trong cinema được giao. Không cấp role |
| VERIFICATION | Verification credential hợp lệ, đúng account/email/request; không yêu cầu đã login |
| PROVIDER | MoMo signature/authentication theo adapter; không dùng browser bearer/cookie |

Resource cinema phải suy ra từ dữ liệu server, không tin cinemaId do client tự khai. Management responses lọc PII theo operation. Không có generic PATCH booking/ticket để actor tùy ý thay financial/admission state.

## Reservation, pricing và deadline

1. Tạo selection tại lần chọn physical seat đầu tiên; server deadline năm phút. Selection chưa hold ghế. Reload/update/quote retry không reset deadline; create lặp cùng active checkout context phải phục hồi selection đó.
2. Step 1 thu fullName, phone, email và declarations. Chỉ Continue sang Step 2 mới POST booking. Reservation dùng current selection version; quantities ở cấp booking, seatIds distinct và đúng tổng quantity; mặc định tổng 1..8 theo cấu hình.
3. Giá = adult quantity × cinema Adult price + discounted quantity × cinema Discounted price + tổng surcharge từng physical seat. Không gán category từng ghế, không Student category riêng. Discounted eligibility gồm nhóm được DOCX cho phép, kiểm tra thực tế tại admission.
4. PriceAcceptance bắt buộc tham chiếu quote và tổng user chấp nhận. Backend tính lại; nếu đổi giá trả PRICE_CHANGED + quote mới, không tạo booking/partial holds. Chấp nhận lại không gia hạn timer.
5. Reserve atomically tạo pending booking, financial/purchased-start snapshots và held allocations. Reservation kế thừa selectionDeadline, không cộng năm phút mới. Double sale không được phép khi concurrent.
6. Khởi tạo MoMo hợp lệ có payment window mười phút, thay thế selection deadline cho việc xử lý payment. Backend trả paymentDeadline. Grace configurable mặc định mười giây sau đó, chỉ để xác minh, không thêm thời gian thanh toán và không bắt buộc release khi grace hết.
7. Unknown/unresolved payment giữ protection và phối hợp reconciliation; countdown về zero không tự đổi persisted state.

## Idempotency, concurrency và retry

Chỉ dùng **Idempotency-Key header**, UUID, không đồng thời thêm requestKey trong body. Bắt buộc cho mọi POST mutation trừ login, verifyEmail, resendVerification và provider webhook; validateAdmission là read-only nên không cần. Public registration dùng key scope operation+normalized email; Guest bootstrap dùng key scope bootstrap, kết hợp anti-abuse. Các mutation còn lại scope actor/Guest identity + operation + target resource. PUT versioned dùng version check; DELETE logout/clear không tạo financial effects.

Key ledger lưu fingerprint canonical của request nghiệp vụ; normalize seat arrays theo ID khi chúng là tập không thứ tự, không normalize tùy tiện ordered conversation content. Cùng key/payload trả durable outcome, không ghi lần hai; khác payload=>409. Replay vẫn kiểm tra quyền hiện hành. Khi processing chưa chốt trả 409 OPERATION_IN_PROGRESS + Retry-After, không bắt đầu tác động thứ hai. Financial/admission dedup identity phải được giữ cùng lịch sử; không tự tái sử dụng key sau TTL tùy tiện.

Read model có thể đổi sau outcome cũ: replay admission trả original committed operation; GET resource cho state hiện hành. Payment initiation cùng key có thể tiến từ durable unresolved (202) sang kết quả đã xác minh (200), nhưng luôn cùng paymentId; không gửi gateway lần nữa nếu không bảo đảm an toàn. Open attempt created/pending/unknown được reuse; không tạo attempt mới chỉ vì client timeout. Header Idempotency-Replayed: true cho replay đã nhận diện.

Version token bắt buộc cho management/configuration edits và selection update; stale=>409 VERSION_CONFLICT. Sau price change, caller phải dùng selection/quote mới và key mới cho ý định reserve đã thay đổi. Transaction rollback không được đánh dấu payment event processed hoặc admission committed.

## Payment, worker, email và cancellation

Ingress C02/C09 xác minh source/signature, lưu durable receipt trước provider ACK phù hợp; domain processing xác minh reference/amount/currency và re-read state theo transaction protocol. Verified payment succeeded không tự đồng nghĩa applied. Eligible application atomically ghi payment application, booking confirmed, allocations sold, booking-level ticket/QR và persisted notification request. Email qua C14/C15 sau commit; gửi email lỗi không rollback tiền/booking.

External calls ngoài business transaction/lock; callback trùng hoặc out-of-order không downgrade verified success, tạo QR hoặc gửi nghĩa vụ lần hai. Reconciliation không reclaim ghế đã release. Browser return chỉ điều hướng frontend; frontend đọc protected payment/booking API.

C14 gọi services nội bộ C08/C09/C10/C15 cho UC-03/UC-04; **không mở HTTP `/internal/.../run` trong catalogue này**. BackgroundService cùng host không cần gọi API qua HTTP. Job identity/checkpoint/retry là implementation detail phải idempotent.

Pending cancellation chỉ trước payment-initiation commit, có explicit warning confirmation; đóng tab không phải hủy. Confirmed cancellation chỉ theo rescheduling eligibility: current official start khác purchasedStartAt, trước current start, chưa admission, toàn booking. Pending review chưa chặn admission; first successful admission tự reject pending request trong coordinated transaction. Approved-awaiting-refund chặn admission. Actual refund bên ngoài application trong MVP; Manager/Admin ghi verified completion có evidence. Rescheduling refund completion cancel booking, invalidate ticket và release sold allocation theo UC-04A. Reconciliation refund completion không tự áp dụng các transition rescheduling và không phục hồi expired booking. Approval khác completed refund.

## Admission và ticket

Một confirmed booking có một QR cho toàn booking. Ticket retrieval/download/resend không cấp thêm entitlement. QR là secret cần bảo vệ nhưng không là authority cho Customer resource APIs.

Validate chỉ đọc. Confirm revalidate QR, confirmed booking, allocations, current official showtime, cancellation/refund restriction, age/Discounted checks và remaining quantities trong transaction. Window inclusive từ start-5 phút đến start+20 phút. Partial/group admission hỗ trợ nhiều operation nhưng không vượt Adult/Discounted quotas; Idempotency-Key là operationId. Hết quota khác với từng QR “chỉ được quét một lần”.

## Lỗi dùng chung

Tất cả code và HTTP mapping là Proposed; dùng nhất quán ở catalogue và DTO.

| Code | HTTP | details |
|---|---:|---|
| VALIDATION_ERROR | 400 | violations: [{field,reason,message}] |
| UNAUTHENTICATED | 401 | {} |
| INVALID_CREDENTIALS | 401 | {}; không enumerate account |
| FORBIDDEN | 403 | {} |
| CSRF_REJECTED | 403 | {} |
| NOT_FOUND | 404 | {} |
| IDEMPOTENCY_KEY_CONFLICT | 409 | {} |
| OPERATION_IN_PROGRESS | 409 | {}; Retry-After header |
| VERSION_CONFLICT | 409 | currentVersion nếu caller có quyền |
| EMAIL_UNAVAILABLE | 409 | {}; chỉ operation cho phép disclosure |
| SEAT_CONFLICT | 409 | seatIds: UUID[] |
| STATE_CONFLICT | 409 | reason: string; không lộ private state |
| ADMISSION_ENTITLEMENT_EXCEEDED | 409 | remaining: Quantities |
| SELECTION_EXPIRED | 422 | selectionDeadline: Instant |
| PRICE_CHANGED | 422 | quote: Quote, selectionVersion: string |
| SHOWTIME_NOT_BOOKABLE | 422 | reason: string |
| PAYMENT_NOT_ELIGIBLE | 422 | reason: string |
| CANCELLATION_NOT_ELIGIBLE | 422 | reason: string |
| ADMISSION_WINDOW_CLOSED | 422 | opensAt, closesAt: Instant |
| ADMISSION_RESTRICTED | 422 | restrictions: Restriction[] |
| REFUND_EVIDENCE_INVALID | 422 | violations: [{field,reason,message}] |
| BUSINESS_RULE_VIOLATION | 422 | violations: [{field,reason,message}] |
| RATE_LIMITED | 429 | {}; Retry-After header |
| DEPENDENCY_UNAVAILABLE | 503 | {}; Retry-After nếu xác định được |
| INTERNAL_ERROR | 500 | {}; traceId để tra cứu |

Known durable unknown payment trả 202 initiation hoặc 200 read Payment(status=unknown), không giả lỗi làm client tạo attempt khác. 503 không chứng minh provider chưa thu tiền: client replay key/read status. Các lỗi response không thay thế quy tắc bảo toàn outcome đã commit.

## CORS, dữ liệu nhạy cảm và observability

Credentials CORS chỉ exact trusted origins, không wildcard. Cookie requests chống CSRF như trên; không disabled HTTPS checks. Protected booking/payment/ticket/auth/evidence trả Cache-Control: no-store. Redact Authorization, Cookie, CSRF, QR, verification token và contact không cần thiết khỏi logs. Audit lưu actor, operation, resource, time, outcome phù hợp C16; traceId không phải evidence thanh toán. Rate-limit login/register/verification/guest bootstrap/chat/resend; giới hạn cụ thể cấu hình, chưa giả là đo được.

## Những điểm cần xác nhận trước implementation

- Guest lifetime demo đã chốt 24 giờ; production lifecycle/recovery sau mất browser state và CSRF/CORS cho Vercel–Render vẫn cần review. Local Guest configuration tại [Guest checkout](../development/guest-checkout.md).
- Quote storage/versioning, idempotency ledger retention, limits cho input và pagination ở từng màn hình.
- MoMo raw payload, signature, ACK, provider reference/status mapping, idempotent initiation và query capabilities; không bịa từ DTO nội bộ. Đọc tài liệu chính thức tại lúc tích hợp.
- API enum mapping với schema mới; không coi schema cũ đã migrate. Trạng thái implementation và contract tests phải kiểm chứng trong repository.
- Brevo đã được user báo thử gửi mail thành công; điều đó chưa xác nhận delivery/outbox toàn hệ thống. Không ghi quota theo tháng/ngày khi chưa kiểm chứng.
- C12 history đã chốt theo SAD: bounded backend memory, mất khi clear/expiry/restart; không phải câu hỏi về PostgreSQL persistence.

## Tài liệu liên quan

- [API Endpoints](api-endpoints.md): operation catalogue, quyền, success/errors, coverage.
- [DTO Specification](dto-specification.md): field types, nullability, validation và examples.
- UC DOCX: UC-01/09 reservation; UC-02/03/04 payment safety; UC-04A/10/19 cancellation/refund; UC-05 admission; UC-08A–D/18 identity; UC-12–17 catalogue/configuration.
- SAD: C02/C03 API/identity, C08–C11 booking/payment/refund/admission, C12 temporary chat, C13 transactions, C14–C16 workers/notification/audit.

BE2 task 2 uses decimal-string bigint showtime/seat IDs, UUID selection/quote IDs, and a configurable maximum (default 8) snapshotted on selection creation. See [selection/quotation implementation](../development/selection-quote.md). Selection quotation is implemented without changing the SQL schema or adding BE1 entity mappings.

BE2 task 3: POST `/api/bookings` atomically commits pending booking, category/seat snapshots, held allocations, selection consumption, audit and receipt. Rejected 409/422 outcomes are also durable and replay-marked. Original selection deadline is retained; no payment window begins. [Reservation implementation](../development/booking-reservation.md).
