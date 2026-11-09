# Database Design — Promethe-Phos

Ngày rà soát: 09/11/2026. Baseline nghiệp vụ: Use-Case Specifications v2.4 (28 UC); baseline kiến trúc: SAD v3.15, đặc biệt C03, C08–C11 và C13–C16. Đối chiếu với các bản Markdown requirements/architecture/relational model đã soạn trong cùng đợt.

## 1. Trạng thái và phạm vi

Đây là **thiết kế vật lý đề xuất và bootstrap DDL đã kiểm tra biệt lập**, không phải migration cho database đang vận hành. Chưa kiểm tra repository EF Core, dữ liệu Supabase hoặc cấu hình deployment thực tế. SQL tạo namespace `cinema` mới; không sửa/xóa `public` và không tự chuyển dữ liệu 18 bảng cũ.

Mô hình có **37 relation nghiệp vụ/hạ tầng theo relational-model**, cộng **4 bảng hỗ trợ ASP.NET Core Identity** (`user_claim`, `role_claim`, `user_login`, `user_token`), thành **41 bảng vật lý**. Các bảng hỗ trợ Identity không bổ sung use case mới. Không có bảng lưu hội thoại AI: SAD C12 dùng bounded backend memory.

| File | Vai trò |
|---|---|
| `database-design.md` | Quyết định thiết kế, invariant, transaction, migration và giới hạn kiểm chứng |
| `database-schema.md` | Data dictionary khớp từng cột/constraint/index của DDL |
| `database-schema.sql` | Tạo mới schema trống; không phải alter migration |
| `seed-data.sql` | Fixture demo có khóa ổn định, transaction và drift detection |
| `movie-research.md` | Nguồn dữ liệu phim, phiên bản, thị trường và trường chưa xác minh |

## 2. Những điểm đã sửa từ bản cũ

| Bản cũ | Thiết kế mới |
|---|---|
| 18 bảng, booking bắt buộc user | Checkout identity tách Customer/Guest, Guest secret chỉ lưu hash |
| Booking giữ 5 phút từ created_at | Selection 5 phút từ ghế đầu tiên; reservation kế thừa deadline |
| Couple/capacity units, discount percentage từng ghế | Physical seat một entitlement; Adult/Discounted quantities cấp booking |
| Giá cơ bản nằm ở showtime | Hai base prices theo cinema; surcharge physical seat; snapshot khi reserve |
| Ticket theo booking_detail, valid→used một lần | Một ticket/QR cho booking, admission batches và category quotas |
| Role token cũ có quyền tới hết hạn | Server sessions, refresh rotation, explicit replacement challenge, current authority |
| Không có cinema scope trong MVP | Staff/Manager assignments riêng và kiểm đúng operation |
| Refund chỉ ngoài phạm vi | Nghĩa vụ, review và verified manual completion trong demo; chuyển tiền ngoài app |
| Hủy confirmed không hỗ trợ | Rescheduling request với pending_review/withdrawal/approval/completion riêng |
| Gửi mail trực tiếp sau thanh toán | Persisted notification request cùng commit, gửi sau commit với retries |
| Seed tìm phim theo title LIMIT 1 | Stable seed_key, title không unique, không chọn nhầm phim cùng tên |
| Seed fixed cutoff −10 phút, skip overlap | Không tự đặt cutoff; exclusion conflict làm rollback, không bỏ qua âm thầm |

## 3. Kiểu dữ liệu và lựa chọn vật lý

- Numeric entity IDs: `bigint GENERATED ALWAYS AS IDENTITY`; security/session/selection/public/request identities: UUID do application sinh. Không dùng `MAX(id)+1`.
- Money cấu hình VND dùng domain `vnd_amount`: numeric không scale coercion, check nguyên đồng, không âm, không vượt 99,999,999,999,999. Nhờ đó 0.5 bị từ chối thay vì bị làm tròn khi cast numeric(14,0). Đây là giới hạn kỹ thuật đề xuất.
- Financial evidence `received_amount`, refund/evidence amount dùng numeric và currency riêng để giữ khoản thu lệch; không buộc received currency=VND rồi mất bằng chứng mismatch. Hoàn toàn bộ nghĩa vụ phải khớp khoản thu đã xác minh tại service.
- `booking.total_amount >= 0`; `payment.amount > 0` là giới hạn initiation adapter trong draft. **Zero-total checkout vẫn cần quyết định**; không tự tạo free-confirm workflow hoặc khôi phục Option B cũ như quyết định hiện hành. Seed không tạo tổng 0.
- `timestamptz` cho thời điểm; `date` cho release date có market rõ. Backend truyền UTC, cinema hiển thị timezone được validate với PostgreSQL timezone catalogue.
- Generic `version` là optimistic version do service tăng/check; không phải SQL Server rowversion hoặc cột tự tăng khi UPDATE. Identity còn có concurrency_stamp riêng, phải map đúng.
- Tên trạng thái vật lý là proposal trừ các lifecycle đã xác định rõ trong nguồn, đặc biệt cancellation request. Không tuyên bố mọi tên cột là nguyên văn DOCX.
- `seed_key` trên movie/showtime chỉ là định danh fixture/import ổn định, nullable cho dữ liệu thật. `release_market`, `age_rating_system`, `retired_at`, physical coordinates là các bổ sung vật lý có chủ đích.

## 4. Nhóm dữ liệu và ownership

| Owner | Bảng chính |
|---|---|
| C03 Identity and Access | app_user, role, user_role, 4 Identity support tables, cinema_assignment, authenticated_session, refresh_token, session_replacement_challenge, email_verification_request, email_address_claim |
| C04 Movie Catalogue | movie, genre, movie_genre |
| C05 Cinema and Seating | cinema, cinema_room, seat_type, seat |
| C06 Scheduling | showtime, showtime_change |
| C07 Pricing | ticket_category, cinema_ticket_price |
| C08 Booking and Checkout | checkout_identity, selection_session, selection_seat, booking, booking_category, booking_seat |
| C09 Payment | payment, payment_event |
| C10 Cancellation and Refund | cancellation_request, refund, refund_completion_evidence |
| C11 Ticketing and Admission | ticket, admission_operation |
| C13 Persistence | operation_receipt như cơ chế chung; caller sở hữu ý nghĩa operation |
| C15 Notification | notification_request, notification_attempt |
| C16 Audit | audit_event |

C14 điều phối workers sử dụng claims/leases trên durable work records; không được tự ý đổi nghiệp vụ ngoài interfaces của owner. Các owner cùng một PostgreSQL transaction khi cần atomicity, không suy ra microservices.

## 5. ASP.NET Core Identity và session

Mapping được chọn cho draft: `IdentityUser<long>`, `IdentityRole<long>`, `IdentityDbContext<..., long>` với tên bảng/cột snake_case và default schema `cinema`. **Phải cấu hình Fluent API và migrations tương ứng**, không dùng mặc định AspNetUsers rồi coi DDL này tự tương thích.

`app_user` có UserName/NormalizedUserName, Email/NormalizedEmail (map sang primary_email/normalized_email), EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd/Enabled, AccessFailedCount cùng extensions. Bốn bảng claim/login/token giữ shape hỗ trợ Identity store. `user_token` không thay bảng refresh-token rotation riêng. Role code là business extension; normalized_name/name trong fixture bằng code. Không seed mật khẩu hoặc tự viết password hasher.

Current account state, session expiry/revocation, role và Staff/Manager cinema assignment phải được kiểm trước protected commit. Assignment FK không chứng minh user còn role; Admin không mặc nhiên Staff admission. Internal access cần verified email; Customer login có thể chưa verify.

Một unrevoked session/user được bảo vệ bằng partial UQ; expired nhưng chưa revoked phải đóng dưới account lock trước khi tạo mới. Login conflict tạo hashed single-use challenge bound `(expected_session_id,user_id)`; chỉ explicit confirm mới atomically consume/revoke/create. Stale challenge không thay phiên mới hơn. Refresh consume token cũ + insert mới trong transaction; composite FK bảo vệ replacement cùng session. Logout/lock revoke; unlock không làm token cũ sống lại.

## 6. Email ownership và verification

`email_address_claim` là keyspace chung cho primary email và pending replacement reservation; unique riêng ở user/request không đủ chống collision xuyên bảng. Mọi account creation/email change phải cập nhật claim cùng Identity user và request trong transaction. Service normalize bằng Identity normalizer nhất quán, không trộn tùy ý lower(trim) với normalization của framework.

Default verification validity 24h; resend ít nhất 60s; **pending email change** có absolute reservation 72h từ original request. Initial primary ownership không hết hạn cùng token. Sau absolute expiry, cùng account chờ 24h trước reserve lại cùng target. Giữ request history/lineage để replacement không reset cửa sổ. Một active request/account là lựa chọn vật lý mạnh hơn, cần workflow đóng request trước khi thay.

Primary email mới chỉ cập nhật sau verification thành công và reauthentication phù hợp; giữ user ID. Pending change không tự revoke role vẫn hợp lệ. Raw token không lưu trong token_hash; nếu outbox cần gửi link thì protected_payload phải được mã hóa với key ngoài DB, có retention/purge. Payload tồn tại không phải bằng chứng verified.

## 7. Selection, reservation và giá

First physical seat selection tạo session 300s; quantities không khởi động timer. `selection_seat` **không có unique seat/showtime giữa các selections**: nhiều khách được nhìn/chọn cùng ghế, chỉ reservation tạo allocation. Movie Detail reload reset lựa chọn theo policy; Checkout reload phục hồi state còn hợp lệ, không gia hạn timer.

Reserve ở Step 1→2: backend resolve Guest/Customer authority; kiểm contact full name/phone/email, terms/age acknowledgements; khóa/read lại showtime/resources/prices, đối chiếu accepted quote/time. Cùng idempotency key cùng fingerprint trả durable outcome; khác payload conflict. Tạo booking + hai booking_category rows (kể cả quantity 0) + đủ booking_seat rows + receipt/audit, consume selection trong cùng transaction.

Invariants application phải kiểm:

1. Booking owner/showtime/room/deadline đúng selection; `reservation_expires_at = selection.expires_at`, còn thời gian tại quyết định commit.
2. Sum category quantities từ 1 đến maximum snapshot (default 8); bằng số physical seat distinct; không mapping category từng ghế.
3. `total = Σ(category.quantity × base_price_snapshot) + Σ(booking_seat.surcharge_snapshot)`; không dùng client total làm authoritative.
4. Price/current official start khác giá trị đã accept phải báo/chấp nhận lại; không reset timer.
5. Snapshots không reprice khi movie/category/seat configuration đổi; mọi ghế thuộc cùng room/showtime.

Composite FK bảo vệ selection→booking và booking/seat→booking_seat. **UNIQUE (id,showtime_id,cinema_room_id) nằm ở booking**, không đặt unique cùng tuple trên child rồi làm một booking chỉ có một ghế.

## 8. Scheduling, layout và exclusion

`showtime` snapshot duration khi tạo; end và room_available_at được kiểm với duration/turnaround snapshots. Giá không nằm ở showtime. Cinema turnaround default 15 phút, không room override. Đổi turnaround phải serialize với scheduling và cập nhật/kiểm các unfinished intervals bị ảnh hưởng; không rewrite completed history.

GiST exclusion dùng hai native ranges: singleton room range `int8range(room_id,room_id,'[]')` và thời gian `[starts_at,room_available_at)`, cả hai operator `&&`. Hai singleton room ranges giao nhau chính xác khi room ID bằng nhau. Cách này không cần extension btree_gist. Draft/completed vẫn được giữ trong interval enforcement; chỉ cancelled được loại — lựa chọn vật lý cần giữ nhất quán ở scheduling service. Suất tiếp theo bắt đầu đúng room_available_at được phép.

Structural freeze áp dụng đúng room có unfinished showtime với pending/held hoặc confirmed/sold booking. Unfinished showtime trống không tự cấm mọi sửa đổi; completed history không tự freeze nhưng phải giữ references. Seat retirement không hard-delete/reuse ID. Display-only metadata chỉ sửa nếu không ảnh hưởng identity/layout/type/eligibility/pricing/history. Operational status khác allocation status.

Cancel showtime bị chặn khi còn obligations theo UC-17; không direct reactivate cancelled, không tự cancel/refund hàng loạt. DDL CHECK status chỉ giới hạn tập giá trị, **không thực thi toàn bộ transition/obligation rules**.

## 9. Payment initiation, receipt và application

`initiation_committed_at` được thêm để phân biệt “initiation đã commit nhưng chưa rõ provider response” với valid `initiated_at/payment_deadline`. Kể từ initiation commit không cho pending cancel, kể cả chưa có redirect URL. Crash trước/sau network call dùng merchant_request_id ổn định và query/reconcile, không tạo attempt mới mù quáng.

Valid initiation thiết lập 600s payment window, grace_seconds default 10. Những giá trị snapshot không reset bởi reload/duplicate. Grace để xác minh, không chứng minh thất bại hoặc cho thanh toán thêm. Backend phải xác định timestamp/window từ contract provider đã chốt; không dùng lúc client quay về.

Verified receipt lưu trước ACK phù hợp provider contract, deduplicate `(provider,provider_event_identity)`; không giả MoMo có field eventKey. Unmatched verified receipt có thể payment_id NULL để review, không áp vào booking tùy đoán. Invalid signature không đi vào valid receipt queue. Claim ngắn tách khỏi business locks.

Verified succeeded != applied. Success mismatch amount/currency được giữ với needs_review; applied yêu cầu match và các điều kiện eligibility. Application transaction phối hợp C09/C08/C11/C15/C16: payment applied, booking confirmed, held→sold, một ticket, persisted notification và audit bắt buộc. Event processed cùng domain effect. Provider call/email ngoài locks.

Một open attempt và một applied payment/booking được partial UQ bảo vệ. UQ không thay proof của payment hoặc eligibility thời gian. Late succeeded không reclaim ghế; payment financial truth không downgrade do response cũ.

## 10. Expiration và pending cancellation

Worker quét candidate theo deadline, nhưng đọc lại window/payment safety dưới locks. Không initiation: deadline gốc selection. Có initiated payment: payment deadline/grace và reconciliation điều phối release. Có durable initiation nhưng chưa xác định provider outcome: không giải phóng vì `initiated_at IS NULL`.

Query provider ngoài transaction; sau query khóa/read lại trạng thái trước khi chuyển expired/released. Unknown có retry/review/escalation, không giả failed vì hết grace. UI countdown chỉ là display.

Pending cancel chỉ qua explicit warning/confirmation trước initiation commit. Transaction kiểm lại và đổi booking cancelled + held released. Nếu initiation thắng race, từ chối cancellation; không dùng Cancel API để đổi ghế sau initiation. Hủy tab không là hủy booking.

## 11. Ticket, admission và cancellation review

Ticket unique booking; QR có lookup hash và encrypted credential để authorized redelivery cùng QR. Hash đơn thuần không thể khôi phục QR gửi lại. Key encryption/version quản ngoài DB, chưa có cryptographic implementation trong SQL này. Không expose raw credential qua audit/receipt.

Admission operation là committed-success ledger, bất biến. Under showtime/booking locks, service kiểm Staff–Cinema, current official start inclusive −5/+20 phút, age/Discounted eligibility, valid ticket, cancellation restrictions và remaining category counts. Insert ledger + increment counters + auto-reject pending request khi first admission + audit trong cùng transaction. `admitted_quantity<=quantity` chỉ là guard từng row; ledger sum/counter agreement và quyền phải được service bảo đảm.

Rescheduling cancellation request ghi purchased/current start tại lúc request, strict-before-start và zero admissions. Request có thể tiếp tục review khi current official start quay về snapshot nếu lúc nộp hợp lệ. Chỉ pending_review được withdrawal. Pending review không chặn admission; approval chặn admission nhưng vẫn confirmed/sold cho tới refund completion.

## 12. Refund và evidence theo origin

Origins: `payment_reconciliation` không có cancellation_request_id; `showtime_rescheduling` bắt buộc request cùng booking. Unique payment_id bảo vệ tối đa một full refund/payment. `pending/unknown/failed/succeeded` độc lập `pending_review/approved_awaiting_refund/rejected/withdrawn/completed`.

Staff/Manager/Admin có thể review trong scope; **completion chỉ assigned Manager hoặc Admin**. Kiểm full amount/currency theo verified collected payment, external reference/channel và evidence trước khi ghi accepted completion evidence. Evidence đã chấp nhận append-only, không sửa để khớp kết quả mong muốn.

- Rescheduling completion: refund succeeded, request completed, booking cancelled, ticket voided, sold seats released và audit trong một transaction.
- Reconciliation completion: resolve nghĩa vụ tài chính theo origin; không mặc định cancel/release booking đã có payment khác applied hợp lệ.

Chuyển tiền nằm ngoài app trong demo. Ghi evidence không phải thao tác phát lệnh chuyển tiền. Accepted request/timeout không được đánh completed; retry không đổi logical refund identity hoặc cộng tác động lần hai.

## 13. Outbox, claims và audit

Notification deduplication key ngăn queue cùng logical event lặp; explicit authorized resend có identity riêng, vẫn QR cũ. Claim worker gồm lease_until + claim_token; finalize bằng conditional UPDATE khớp token để worker hết lease không ghi đè worker mới. HTTP gửi ngoài transaction giữ business locks. Nếu provider gửi xong rồi process crash trước finalize, retry có thể gửi email trùng: không tuyên bố exactly-once transport.

Audit bắt buộc cùng transaction của transition. SQL có trigger cấm UPDATE/DELETE trên audit_event, admission_operation, showtime_change, refund_completion_evidence; chủ DB vẫn có thể thay trigger, nên runtime role không được owner/DDL/TRUNCATE. Rejected-attempt logs tách nghĩa khỏi committed business audit.

## 14. Protocol transaction và thứ tự khóa

Các protocol là yêu cầu cho implementation, không có stored procedures nghiệp vụ trong bản DDL.

| Operation | Khóa/điều phối | Atomic changes |
|---|---|---|
| Identity/email | Account theo ID; claims/email keys deterministic; recheck current authority | Session replacement/rotation, claim+user+request, audit/outbox |
| Layout/scheduling | Cinema→Room theo ID; phối hợp showtime và reservation đọc resources | Structural edits/schedule/turnaround + history/notification |
| Reservation/payment/expiration | Showtime→Booking→Payment attempts, stable ID order; ancestor resource locks phải lấy trước nếu cần | All seats/snapshots hoặc whole payment application/release |
| Admission/approval | Showtime→Booking, then ticket/category/request theo stable order | Counts/ledger/request decision/audit |
| Refund completion | Showtime→Booking→Payment, then cancellation/refund/evidence | Origin-specific completion effects |
| Worker claim | Claim trong transaction ngắn; không giữ claim row lock rồi lấy ngược domain locks | Lease/token update; finalize guarded bằng claim token |

Không bắt đầu giữ booking rồi quay lên lấy showtime/cinema/room. Mọi service phải dùng cùng lock hierarchy, kể cả trigger/procedure thêm sau. Trước commit, recheck current time từ DB, current authority và row versions. Deadlock/serialization failure retry có giới hạn và cùng operation identity, không retry external money call mù.

## 15. DB bảo vệ gì, application phải làm gì

| DB đã có | Application transaction vẫn bắt buộc |
|---|---|
| PK/FK, composite room/booking relationships | Current authorization, roles, cinema scope, ownership proof |
| Active-seat UQ, showtime range exclusion | Room/seat eligibility, structural freeze, no unsafe showtime cancellation |
| Open/applied payment UQ; financial shape checks | Provider verification, deadline eligibility, no downgrade, full applied effects |
| Ticket unique booking; admission window snapshot check | Current start đúng snapshot validation, category ledger totals, age/discount checks |
| Request state enum, origin/FK refund checks | Only allowed transitions, zero admissions at submission/approval, withdrawal conditions |
| Email claims uniqueness, challenge/refresh composite FKs | Cross-table current primary claim/user equality, cooldown lineage, challenge consumption, normalization |
| Append-only history triggers | Atomic business/audit/outbox coordination, runtime permissions, PII retention |
| Price/domain and per-row quantity checks | Booking category count=2, total quantity=seat count, total price formula, immutable snapshots |

Không cấp SQL write trực tiếp cho browser, Supabase anon/authenticated hoặc các tool tùy ý. Schema `cinema` không cần expose qua Supabase Data API; backend là access boundary. Script revoke PUBLIC nhưng người triển khai vẫn phải audit explicit/default grants và RLS/role setup. Không tự coi RLS đã bật. Nếu mở nhiều writers ngoài service, cần stored procedures/constraint triggers bổ sung và tests trước.

## 16. Seed và cách chạy

Trong database local/test biệt lập:

```powershell
psql -X -v ON_ERROR_STOP=1 -f database/database-schema.sql
psql -X -v ON_ERROR_STOP=1 -f database/seed-data.sql
```

Connection lấy từ môi trường/pgpass đã cấu hình, không đưa password vào tài liệu/command history. Namespace `cinema` đã tồn tại thì bootstrap cố ý fail, không bỏ lỗi rồi chạy tiếp. Chọn EF migrations hoặc bootstrap SQL cho một DB; không chạy cả hai initial schemas mù quáng.

Seed mặc định **14–21/11/2026**, Asia/Ho_Chi_Minh, 8 ngày × 6 suất = 48. Đổi ngày bằng custom setting trong cùng session khi chạy script, ví dụ psql `-c "SET cinema.demo_start_date='2026-11-21'" -f database/seed-data.sql`. Cùng fixture/ngày replay không thêm rows; ngày khác tạo set khác và giữ lịch cũ. Ngày không tự dịch theo CURRENT_DATE.

Fresh counts: 4 roles, 2 categories, 9 genres, 6 movies, 18 movie–genre links, 1 demo cinema, 2 rooms, 100 seats, 2 cinema prices và 48 showtimes. Có cả Standard/VIP nhưng không Couple. Demo states/prices/locations không phải dữ liệu kinh doanh thật. Movie age classification/media/release date chưa duyệt không đưa vào seed như dữ liệu thật; xem movie-research.md.

Seed transaction + advisory lock tránh hai seed processes cạnh tranh. ON CONFLICT chỉ dùng các identity keys xác định; drift kiểm các giá trị cần cho lịch/giá/ghế rồi rollback, không overwrite admin edits hoặc auto-reactivate cancelled showtime. Overlap là lỗi, không dùng `ON CONFLICT ON CONSTRAINT ex_showtime_room_time DO NOTHING`.

Không seed user/password/admin hoặc giả paid ticket. Tạo test accounts bằng Identity seeder/UserManager/RoleManager rồi assignments với audit phù hợp. Admission/payment fixtures riêng có thể dùng dữ liệu test dưới transaction, không lẫn seed catalogue.

## 17. Chuyển từ schema 18 bảng cũ

1. Inventory code, migrations, actual schema, data volumes, role permissions và backup/restore rehearsal.
2. Chọn actual Identity key type và EF table mapping. bigint ở draft là proposal; nếu project đã dùng string/Guid cần mapping/migration rõ, không cast IDs mù.
3. Phân loại data cũ: Couple multi-capacity, per-seat categories/tickets, giá percent, payment/refund và admissions. Không tự cộng/gộp thành QR mới khi có vé đã dùng hoặc nghĩa vụ thật.
4. Tạo target migration/additive columns/tables và mapping IDs/financial snapshots. Preserve raw historical evidence; đặt legacy data vào reviewed migration path khi không chuyển được losslessly.
5. Backfill deadline chỉ từ evidence đủ tin cậy; không dựng first-selection time từ booking.created_at như sự thật. Giữ current vs purchased-start khác nhau.
6. Verify count/FK/amount/history/current obligations, reconcile external payments; deploy code và schema phối hợp. Rollback plan phải bảo toàn payment events sau cutover.
7. Chỉ retire cấu trúc cũ sau xác nhận consumers/tests; bộ file này không chứa DROP/reset hay migration tự động.

## 18. Kiểm chứng trong lần rà soát

Đã chạy trên PostgreSQL 16.2 trong database biệt lập bằng single-user backend:

| Kiểm tra | Kết quả |
|---|---|
| Parse và thực thi bootstrap schema | Thành công; 41 bảng |
| Seed lần đầu và chạy lại cùng ngày cấu hình | Thành công; không tạo trùng fixture |
| Số lượng sau khi chạy lại | 6 phim, 100 ghế, 48 suất chiếu, 18 liên kết phim–thể loại |
| 31 tình huống ràng buộc | Đạt; gồm tiền nguyên VND, lịch chồng lấn, session/email, cùng ghế trong selection, allocation độc quyền, payment mở/applied, ticket, admission và refund |

Test fixtures được rollback sau kiểm tra. Đây là kiểm tra SQL và ràng buộc bằng PostgreSQL thực, **chưa phải** kiểm thử race đa connection, luồng nghiệp vụ end-to-end, EF mapping/migration, Supabase deployment hay tích hợp MoMo/Brevo. Những invariant do application quản lý vẫn phải được triển khai và kiểm thử tại backend.

## 19. Điểm còn cần triển khai/chốt

- Zero-total checkout; không có business decision mới từ DDL.
- EF version/key/mappings, configuration thực tế và migration production.
- Provider signature/event identity/timing, mock vs sandbox, evidence-normalization procedure.
- Guest credential transport/recovery, QR/token encryption, key rotation và retention.
- Retry/escalation/lease durations và race tests đa connection.
- Application invariants mục 15, restricted DB grants và source-to-code traceability.

## 20. Nguồn kỹ thuật và revision

- [PostgreSQL constraints](https://www.postgresql.org/docs/current/ddl-constraints.html): CHECK/NULL, FK, uniqueness và exclusion; cross-row business rules không phải CHECK thông thường.
- [ASP.NET Core Identity model customization](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model): customize user/role keys, tables và EF mappings, review generated migrations.
- [Use cases](../docs/requirements/use-cases.md), [Architecture](../docs/analysis%20and%20design/architecture.md), [Relational model](../docs/analysis%20and%20design/relational-model.md).

Replaced the legacy 18-table design with a proposed 41-table physical mapping of the revised 37-relation model and ASP.NET Core Identity support. Aligned selection, booking pricing, payment recovery, booking-level admission, session/email security, cancellation/refund and notification/audit persistence. Added non-destructive demo seeding, migration boundaries and explicitly separated database constraints from application invariants.
