Ngài, dưới đây là phân công **BE 2 cho giai đoạn demo**: khách chọn ghế → nhập thông tin → tạo booking và giữ ghế → sang bước chọn MoMo.

**Điểm kết thúc là booking `pending`, chưa thanh toán.** Chưa làm MoMo API, webhook, phát hành vé, email hoặc hoàn tiền.

Các file `docs/ui-ux/` đang trống nên **không dùng chúng làm nguồn đặc tả** trong đợt này.

**1. Phạm vi và tài liệu tham khảo**

| Nguồn | Nội dung BE 2 cần đọc |
|---|---|
| `AGENTS.md`, `mandatory-instruction.md`, Git workflow | Quy trình branch, commit, kiểm thử và ghi log |
| Use-Case Specifications DOCX | UC-01, UC-09, phần hủy pending của UC-10 và hết hạn của UC-03 |
| `docs/requirements/use-cases.md` | Luồng lựa chọn, đặt vé, thời hạn và xử lý lỗi; đối chiếu DOCX khi có khác biệt |
| `docs/api/api-contract.md` | Guest credential, CSRF, idempotency, version, lỗi và response envelope |
| `docs/api/api-endpoints.md` | Endpoint, method, quyền và mã kết quả |
| `docs/api/dto-specification.md` | GuestContext, Selection, Quote, ReserveRequest, Booking, CheckoutRecovery |
| `database/database-schema.sql` | Cấu trúc và ràng buộc thực tế |
| `database/database-design.md` | Transaction, khóa, allocation và vòng đời booking |
| `docs/analysis and design/architecture.md` | C03, C07, C08, C13, C14 và các ranh giới trách nhiệm |
| Source backend hiện có | DI, EF mapping, xử lý lỗi và quy ước tổ chức code |
| Hướng dẫn local development | Chạy Docker và cấu hình kết nối |

**Giới hạn chung**

- Dùng `Models/`, `Services/`, `Controllers/`, `DTOs/` hiện có.
- Không triển khai đăng ký/đăng nhập đầy đủ; hỗ trợ Guest cho demo. Không tạo user giả để thay Guest.
- Không bỏ kiểm tra quyền sở hữu vì “chỉ là demo”.
- Không tin giá, thời gian hoặc trạng thái do FE tự gửi.
- Không sửa schema để vượt lỗi mà chưa thống nhất với Ngài.
- Test ghi dữ liệu trên Docker riêng; không reset hoặc chạy test phá dữ liệu trên Supabase chung.
- Mọi endpoint phải giữ đúng contract; không tự mở endpoint “xác nhận thanh toán thành công”.

**2. Thống nhất với BE 1 trước khi code**

| Phần dùng chung | Người phụ trách chính | BE 2 sử dụng |
|---|---|---|
| Movie, Cinema, Room, Showtime | BE 1 | Kiểm tra suất và tạo snapshot booking |
| Seat, SeatType | BE 1 | Kiểm tra ghế, loại ghế và phụ thu |
| TicketCategory, CinemaTicketPrice | BE 1 | Tính quotation và tổng tiền |
| BookingSeat | BE 2 | BE 1 dùng để đọc khả dụng trên seat map |
| Guest, Selection, Booking, OperationReceipt | BE 2 | Toàn bộ luồng ghi dữ liệu |

Chỉ có **một entity/mapping cho mỗi bảng**. BE 2 có thể bắt đầu Guest trước khi BE 1 hoàn thành; phần quotation/reservation cần tích hợp mappings dùng chung.

**3. Các branch cần làm**

Tạo từ commit nền tảng chung đã được Ngài xác nhận, không mặc định `master` đã cập nhật.

| Thứ tự | Branch | Sản phẩm |
|---|---|---|
| 1 | `feat/be-guest-context` | Cấp và xác minh quyền Guest |
| 2 | `feat/be-selection-quote` | Lưu lựa chọn, thời hạn và tính giá |
| 3 | `feat/be-booking-reservation` | Tạo booking, giữ ghế nguyên tử, chống tạo trùng |
| 4 | `feat/be-checkout-recovery` | Đọc booking và khôi phục checkout |
| 5 | `feat/be-booking-cancel-expiry` | Hủy pending và giải phóng booking hết hạn |

Merge từng branch vào branch tích hợp sau review rồi tạo branch tiếp theo. Nếu làm branch phụ thuộc trước khi merge, ghi rõ quan hệ phụ thuộc trong PR.

---

**Việc 1 — Guest context**

```http
POST /api/guest-context
```

**Công việc**

1. Mapping `checkout_identity` và phần `operation_receipt` cần dùng.
2. Tạo credential ngẫu nhiên đủ mạnh; database chỉ lưu hash.
3. Cấp credential qua cookie theo contract, không trả secret trong JSON hoặc URL.
4. Bootstrap lại với context còn hợp lệ phải khôi phục context phù hợp, không tự tạo danh tính mới làm mất selection.
5. Xây cơ chế xác định Guest hiện tại để các endpoint sau dùng chung.
6. Xử lý credential thiếu, sai, hết hạn hoặc bị thu hồi.
7. Triển khai CSRF cho mutation sử dụng cookie theo contract.
8. Đưa thời hạn Guest vào cấu hình và ghi rõ giá trị thống nhất cho demo.
9. Kiểm tra cookie với mô hình local Vite proxy hiện tại; không dùng cấu hình bỏ bảo vệ local cho môi trường online.

**Commit theo mốc**

| Khi hoàn thành | Commit |
|---|---|
| Bootstrap, cookie và xác minh Guest chạy được | `feat(api): add guest checkout context and credential validation` |
| Kiểm thử credential, reuse và CSRF đạt | `test(api): cover guest context lifecycle and csrf validation` |
| Cập nhật contract/configuration/hướng dẫn và log | `docs(api): document guest checkout configuration` |

**Checklist nghiệm thu**

- [ ] Tạo Guest context bằng API thật.
- [ ] JSON và log không lộ credential.
- [ ] Request tiếp theo nhận diện đúng Guest.
- [ ] Context hợp lệ được tái sử dụng.
- [ ] Credential sai/hết hạn bị từ chối.
- [ ] Mutation không có CSRF hợp lệ bị xử lý đúng contract.
- [ ] Chưa tạo booking hoặc giữ ghế.

**Bàn giao FE 2:** cách bootstrap, gửi cookie/CSRF và xử lý phiên Guest không còn hợp lệ.

---

**Việc 2 — Selection và quotation**

```http
POST /api/selections
GET  /api/selections/{selectionId}
PUT  /api/selections/{selectionId}
```

**Công việc**

1. Mapping `selection_session`, `selection_seat`.
2. Tạo selection tại lần chọn physical seat đầu tiên theo UC.
3. Quản lý thời hạn **5 phút từ server**; update/reload/retry không reset thời hạn.
4. Lưu suất, ghế và số lượng Adult/Discounted theo DTO.
5. Phân biệt:
   - Lựa chọn đang chỉnh sửa có thể chưa đủ ghế theo số vé.
   - Yêu cầu reserve phải đáp ứng đầy đủ điều kiện.
6. Kiểm tra ghế thuộc đúng phòng của suất, ghế trùng, ghế ngừng sử dụng và giới hạn số vé.
7. Đọc giá từ database, tính quotation theo công thức đã chốt.
8. Quản lý version và dấu nhận diện quotation để phát hiện dữ liệu/giá thay đổi.
9. Dùng idempotency cho create và version check cho update.
10. Xử lý selection hết hạn, bị tiêu thụ hoặc thuộc Guest khác.
11. Khi tìm thấy selection active nhưng đã quá hạn, xử lý trạng thái đúng trước khi tạo lượt mới; không để unique index gây lỗi không được xử lý.

**Quan trọng:** chọn ghế trong selection **chưa giữ ghế độc quyền**. Hai Guest có thể cùng chọn một ghế; bước tạo booking mới phân xử.

**Commit theo mốc**

| Khi hoàn thành | Commit |
|---|---|
| Tạo/đọc/cập nhật selection và deadline | `feat(api): add guest selection lifecycle` |
| Quotation và version check | `feat(pricing): calculate server-side selection quotes` |
| Kiểm thử thời hạn, quyền, giá và xung đột version | `test(api): cover selection deadlines ownership and pricing` |
| Cập nhật tài liệu và log | `docs(api): document selection and quote behavior` |

**Checklist nghiệm thu**

- [ ] Reload giữ nguyên deadline.
- [ ] Guest khác không truy cập được selection.
- [ ] Không thêm allocation `held` khi chọn ghế.
- [ ] Giá không lấy trực tiếp từ FE.
- [ ] Update với version cũ bị từ chối đúng contract.
- [ ] Selection hết hạn không reserve được.
- [ ] Hai Guest cùng chọn ghế không bị coi là hai booking đã giữ ghế.

**Bàn giao FE 2:** selection ID, version, deadline, quotation và lỗi để hiển thị.

---

**Việc 3 — Tạo booking và giữ ghế**

```http
POST /api/bookings
```

**Công việc**

1. Mapping `booking`, `booking_category`, `booking_seat`.
2. Nhận selection, thông tin khách, acknowledgements và quotation được chấp nhận theo `ReserveRequest`.
3. Kiểm tra tên, điện thoại, email và xác nhận bắt buộc.
4. Kiểm tra quyền sở hữu selection.
5. Trong transaction, khóa/đọc lại dữ liệu theo protocol đã thống nhất:
   - Selection còn hợp lệ.
   - Suất còn được phép đặt.
   - Ghế thuộc đúng phòng và còn khả dụng.
   - Số ghế bằng tổng số vé.
   - Giá còn khớp quotation đã chấp nhận.
6. Tạo booking `pending`, lưu snapshot cần thiết, category quantities và toàn bộ allocation `held`.
7. Đánh dấu selection đã được sử dụng trong cùng transaction.
8. Tính thời hạn giữ ghế theo UC, tách biệt với thời hạn selection.
9. Sử dụng ràng buộc unique allocation để ngăn hai booking giữ cùng ghế.
10. Rollback toàn bộ nếu một ghế xung đột; không giữ một phần.
11. Thực hiện `Idempotency-Key`: cùng key/payload trả cùng outcome; khác payload trả conflict.
12. Nếu giá đổi, trả `PRICE_CHANGED` cùng dữ liệu theo contract; không tự tạo booking với tổng mới.

**Commit theo mốc**

| Khi hoàn thành | Commit |
|---|---|
| Reservation transaction và snapshots | `feat(booking): create pending bookings with atomic seat holds` |
| Xử lý replay và xung đột đồng thời | `feat(booking): enforce reservation idempotency and seat conflicts` |
| Kiểm thử transaction, retry và cạnh tranh ghế | `test(booking): cover atomic reservation and concurrent seat requests` |
| Cập nhật tài liệu và log | `docs(api): document booking reservation outcomes` |

**Checklist nghiệm thu**

- [ ] Request hợp lệ tạo một booking và đầy đủ ghế `held`.
- [ ] Thông tin khách và snapshots được lưu đúng.
- [ ] Double-click/retry không tạo booking thứ hai.
- [ ] Cùng key khác nội dung bị từ chối.
- [ ] Hai request từ hai kết nối cùng tranh ghế: tối đa một request thành công.
- [ ] Yêu cầu nhiều ghế có một ghế xung đột không giữ những ghế còn lại.
- [ ] Giá thay đổi yêu cầu khách chấp nhận lại.
- [ ] Không tạo payment, ticket hoặc gửi email.

**Bàn giao FE 2:** booking ID, trạng thái, tổng tiền, ghế và deadline giữ ghế để mở bước chọn phương thức thanh toán.

---

**Việc 4 — Đọc booking và khôi phục checkout**

```http
GET /api/bookings/{bookingId}
GET /api/bookings/{bookingId}/checkout
```

**Công việc**

1. Kiểm tra Guest ownership trước khi trả dữ liệu.
2. Trả booking summary từ dữ liệu đã lưu.
3. Trả trạng thái và thời hạn hiện hành theo `Booking`/`CheckoutRecovery`.
4. Giữ rõ giá/snapshot đã đặt và dữ liệu hiện tại của suất khi DTO cần phân biệt.
5. Reload trả lại booking hiện có, không tạo booking hoặc gia hạn giữ ghế.
6. Xử lý booking hết hạn/hủy theo contract để FE điều hướng phù hợp.
7. Thống nhất với FE 2 cách hiển thị MoMo là phương thức được hỗ trợ cho demo. Không tự thêm endpoint phương thức thanh toán nếu chưa cần thay contract.
8. Không trả dữ liệu riêng tư vượt quá nhu cầu checkout.

**Commit theo mốc**

| Khi hoàn thành | Commit |
|---|---|
| Hai endpoint đọc và recovery chạy được | `feat(api): add booking read and checkout recovery` |
| Kiểm thử ownership và reload | `test(api): cover checkout recovery and guest isolation` |
| Cập nhật tài liệu và log | `docs(api): document checkout recovery states` |

**Checklist nghiệm thu**

- [ ] Reload vẫn dùng cùng booking.
- [ ] Deadline không thay đổi do GET.
- [ ] Guest khác biết ID vẫn không đọc được booking.
- [ ] Booking pending được hiển thị “Chờ thanh toán”.
- [ ] Booking expired/cancelled không được thể hiện là đang giữ ghế hợp lệ.
- [ ] Không đánh dấu thanh toán thành công từ thao tác chọn MoMo.

---

**Việc 5 — Hủy pending và hết hạn**

```http
POST /api/bookings/{bookingId}/cancel
```

Kèm worker xử lý booking hết hạn, không mở HTTP endpoint công khai để chạy worker.

**Công việc**

1. Kiểm tra ownership và xác nhận cảnh báo hủy theo DTO.
2. Chỉ hủy pending khi còn đủ điều kiện, đặc biệt trước payment-initiation commit theo UC.
3. Trong một transaction:
   - Chuyển booking sang `cancelled`.
   - Chuyển allocations `held` sang `released`.
   - Ghi thời gian/lý do cần thiết.
4. Worker tìm booking pending quá hạn.
5. Khóa và đọc lại trạng thái trước khi chuyển `expired` và release.
6. Không giải phóng booking confirmed.
7. Không release mù booking có payment cần bảo vệ. Chưa tích hợp gateway thì không tự kết luận các attempt chưa rõ đã thất bại.
8. Retry/chạy worker lặp không gây cập nhật sai hoặc tác động trùng.
9. Worker sử dụng service nội bộ và cấu hình chu kỳ chạy; có cancellation khi ứng dụng dừng.

**Commit theo mốc**

| Khi hoàn thành | Commit |
|---|---|
| Hủy pending và release nguyên tử | `feat(booking): cancel eligible pending bookings` |
| Worker hết hạn và các guard bảo vệ payment | `feat(booking): release eligible expired bookings` |
| Kiểm thử hủy, hết hạn, retry và race liên quan | `test(booking): cover cancellation and expiration safety` |
| Cập nhật tài liệu/configuration và log | `docs(booking): document cancellation and expiry processing` |

**Checklist nghiệm thu**

- [ ] Hủy hợp lệ chuyển booking và ghế cùng transaction.
- [ ] Không hủy booking của Guest khác.
- [ ] Không hủy khi đã qua ranh giới payment-initiation được quy định.
- [ ] Quá hạn được xử lý bởi BE, không phụ thuộc FE còn mở.
- [ ] Worker chạy lại không làm hỏng dữ liệu.
- [ ] Không release booking confirmed hoặc payment chưa được xử lý an toàn.
- [ ] Sau release, Guest khác có thể reserve ghế đó.

---

**4. Quy tắc kiểm thử và commit chung**

Mỗi commit phải là phần thay đổi có ý nghĩa và build được; không cần tạo commit rỗng để đủ bảng trên.

Trước commit:

```powershell
git diff --check
dotnet build .\src\backend\CinemaTicketBooking.Api\CinemaTicketBooking.Api.csproj
git status
```

Chạy thêm các test liên quan. Các kiểm thử reservation/concurrency phải dùng **PostgreSQL thật**, không chỉ EF InMemory.

- Dữ liệu test tạo trong database riêng.
- Không hard-code ID phim/suất/ghế giữa Docker và Supabase.
- Không sửa ngày giờ production code để vượt seed đã quá hạn.
- Không commit secret, cookie, thông tin khách thật hoặc connection string thật.
- Không tự chạy schema/migration khi backend khởi động.
- Thay đổi contract phải cập nhật cùng code và thông báo FE 2.
- Ghi log vào file thành viên của BE 2, không mặc định dùng `0235.md`.

**5. Bàn giao cho Ngài và FE 2**

Mỗi PR cần có:

- Danh sách endpoint đã làm.
- Request/response mẫu và mã lỗi quan trọng.
- Cách chạy, cấu hình cần thêm.
- Kết quả kiểm thử thực tế và phần chưa kiểm thử.
- Các thay đổi dùng chung cần BE 1 biết.

**Mốc hoàn thành BE 2:** hai Guest có thể thực hiện luồng chọn ghế → nhập thông tin → tạo booking; hệ thống không giữ trùng ghế, reload không tạo đơn mới, hủy/hết hạn trả ghế đúng, và checkout dừng ở **chọn MoMo — chờ thanh toán**.