dưới đây là phân công cho **BE 1**, dựa trên cấu trúc repository vừa gửi. BE 1 kế thừa API danh sách phim đã chạy và hoàn thiện phần dữ liệu cho **xem chi tiết phim → chọn rạp/ngày/suất → xem ghế và giá**.

**Phạm vi và nguyên tắc làm việc**

- Làm trong `src/backend/CinemaTicketBooking.Api/`.
- Dùng `Models/` hiện có; không tạo thêm `Entities/` để chứa những entity trùng.
- Tái sử dụng `ApplicationDbContext`, quy ước lỗi, pagination và service hiện có.
- Giữ `GET /api/movies` hoạt động.
- ID database kiểu `bigint` tiếp tục trả qua API dưới dạng **chuỗi số**.
- Không tự nâng/hạ .NET hoặc thay kiến trúc.
- Không tạo booking, giữ ghế, Guest context, selection hoặc payment — thuộc BE 2.
- Kiểm thử trên Docker riêng. Không reset, seed lại hoặc sửa schema Supabase dùng chung.
- Nếu cần thay schema hoặc thay contract ảnh hưởng FE, báo Ngài và thống nhất trước khi áp dụng.

**Tài liệu cần đọc**

| Tài liệu/source | Nội dung cần tham khảo |
|---|---|
| `AGENTS.md`, `mandatory-instruction.md`, `docs/management/git-workflow.md` | Quy trình làm việc, branch, commit và ghi log |
| `docs/api/api-contract.md` | ID, response envelope, lỗi, thời gian và validation |
| `docs/api/api-endpoints.md` | Method/path, query, quyền và response từng endpoint |
| `docs/api/dto-specification.md` | Field, kiểu dữ liệu, nullability |
| Use-Case DOCX và `docs/requirements/use-cases.md` | UC-06A, UC-06B, UC-07, phần đọc dữ liệu của UC-01; quy tắc cấu hình ghế/giá liên quan |
| `docs/requirements/business-system-analysis.md` | Điều kiện phim/suất được hiển thị và quy tắc giá |
| `docs/analysis and design/architecture.md` | Trách nhiệm C02, C04, C05, C06, C07 và C13 |
| `database/database-schema.sql`, `database-schema.md` | Bảng, cột, trạng thái, khóa và index thực tế |
| `database/database-design.md` | Allocation, snapshot giá/thời gian và tính nhất quán |
| `docs/ui-ux/customer-screen-api-mapping.md` | Dữ liệu FE cần ở từng màn hình |
| `docs/development/local-development-setup.md` | Chạy Docker, backend và kiểm chứng |
| `MoviesController`, `MovieService`, DTO và models hiện có | Cách triển khai để mở rộng nhất quán |

Nếu DOCX, Markdown, SQL và code mâu thuẫn, ghi rõ điểm lệch để thống nhất; **không âm thầm đổi nghiệp vụ cho khớp code**.

**Các branch cần làm**

Ngài cần cung cấp một **branch/commit nền tảng chung đã kiểm chứng**. Nếu bản tích hợp Supabase đã hoàn tất và được push, có thể dùng commit đó. Không mặc định `master` đã có toàn bộ code demo.

| Thứ tự | Branch | Sản phẩm |
|---|---|---|
| 1 | `feat/be-movie-detail` | API chi tiết phim |
| 2 | `feat/be-cinemas-showtimes` | API rạp, lịch chiếu và chi tiết suất |
| 3 | `feat/be-ticket-pricing` | API loại vé và bảng giá rạp |
| 4 | `feat/be-showtime-seat-map` | API sơ đồ ghế theo suất |

Ưu tiên **hoàn thành, review và merge từng branch vào branch tích hợp chung**, sau đó tạo branch tiếp theo từ bản tích hợp mới. Nếu phải làm tiếp trước khi merge, ghi rõ PR nào phụ thuộc PR nào.

---

**Việc 1 — API chi tiết phim**

Branch: `feat/be-movie-detail`

```http
GET /api/movies/{movieId}
```

**Cần làm**

1. Kiểm tra `Movie` DTO và quy tắc phim được phép xem công khai.
2. Mở rộng `IMovieService`, `MovieService` và `MoviesController`.
3. Truy vấn đúng phim và danh sách thể loại.
4. Trả các trường nullable đúng contract; không tự dựng poster, trailer, độ tuổi hoặc ngày phát hành.
5. Xử lý ID sai định dạng, ID ngoài phạm vi `long`, phim không tồn tại và phim không được công khai.
6. Dùng read-only query, `CancellationToken`, không tạo truy vấn riêng cho từng thể loại.
7. Kiểm tra lại endpoint danh sách phim sau khi sửa.

**File dự kiến**

- Sửa `Controllers/MoviesController.cs`.
- Sửa `Services/IMovieService.cs`, `Services/MovieService.cs`.
- Sửa DTO movie nếu cần theo contract.
- Bổ sung kiểm thử theo cấu trúc test đã thống nhất.

**Commit khi hoàn thành**

| Mốc hoàn thành | Commit |
|---|---|
| Endpoint chạy được với dữ liệu PostgreSQL thật | `feat(api): add public movie detail endpoint` |
| Kiểm thử hành vi thành công và lỗi đạt | `test(api): cover movie detail visibility and invalid identifiers` |
| Cập nhật trạng thái endpoint, hướng dẫn và log | `docs(api): document movie detail implementation and verification` |

**Checklist nghiệm thu**

- [ ] ID hợp lệ trả đúng phim và thể loại.
- [ ] Field thiếu được trả đúng `null`.
- [ ] Không lộ phim ngoài phạm vi công khai.
- [ ] ID sai/không tồn tại trả đúng mã lỗi trong contract.
- [ ] `GET /api/movies` vẫn chạy đúng.
- [ ] Không thay đổi dữ liệu database.

**Kết quả FE nhận được:** có thể mở trực tiếp hoặc reload trang chi tiết bằng `movieId`.

---

**Việc 2 — Rạp và suất chiếu**

Branch: `feat/be-cinemas-showtimes`

```http
GET /api/cinemas
GET /api/movies/{movieId}/showtimes
GET /api/showtimes/{showtimeId}
```

**Cần làm**

1. Bổ sung models và EF mappings cần thiết cho:
   - `cinema`
   - `cinema_room`
   - `showtime`
2. Trả danh sách rạp theo điều kiện công khai đã quy định.
3. Triển khai lọc lịch chiếu theo các query đã công bố, gồm rạp/ngày nếu contract quy định.
4. Diễn giải ngày theo timezone rạp; response timestamp theo quy ước UTC của API.
5. Áp dụng điều kiện hiển thị suất đã chốt. Không tự thêm cutoff hoặc giới hạn mới.
6. Trả đúng phim, rạp, phòng, giờ bắt đầu và các field của DTO.
7. Phân trang nếu endpoint quy định, với thứ tự ổn định.
8. Xử lý phim không tồn tại, không có suất và suất không được công khai.

**Lưu ý dữ liệu demo**

Seed hiện có lịch cố định **14–21/11/2026**. Không bỏ kiểm tra thời gian hoặc làm giả “hôm nay” trong production code để lịch xuất hiện. Test thời gian dùng clock kiểm soát được hoặc fixture riêng.

**File dự kiến**

- `Models/Cinema.cs`
- `Models/CinemaRoom.cs`
- `Models/Showtime.cs`
- Mapping trong `Data/`.
- Controllers, DTOs và services tương ứng theo conventions dự án.

**Commit khi hoàn thành**

| Mốc hoàn thành | Commit |
|---|---|
| Mapping và API danh sách rạp chạy được | `feat(api): add public cinema listing` |
| API lịch chiếu và chi tiết suất chạy được | `feat(api): add showtime discovery and detail endpoints` |
| Kiểm thử filter, thời gian và visibility đạt | `test(api): cover showtime filtering and cinema timezone boundaries` |
| Cập nhật tài liệu và log | `docs(api): document cinema and showtime endpoints` |

**Checklist nghiệm thu**

- [ ] Chọn phim nào trả suất của phim đó.
- [ ] Filter rạp không trả suất của rạp khác.
- [ ] Filter ngày xử lý đúng ranh giới ngày theo timezone.
- [ ] Không trả suất không đủ điều kiện công khai.
- [ ] Không có suất trả collection rỗng đúng envelope.
- [ ] Chi tiết suất trả đúng phòng và rạp.
- [ ] Không giữ ghế hoặc tạo booking.

**Kết quả FE nhận được:** có thể dựng bộ chọn rạp/ngày/giờ và lấy `showtimeId` cho bước chọn ghế.

---

**Việc 3 — Loại vé và giá rạp**

Branch: `feat/be-ticket-pricing`

```http
GET /api/ticket-categories
GET /api/cinemas/{cinemaId}/ticket-pricing
```

**Cần làm**

1. Mapping `ticket_category` và `cinema_ticket_price`.
2. Trả đúng hai category `Adult`, `Discounted`.
3. Trả tên hiển thị và mô tả điều kiện áp dụng từ dữ liệu.
4. Trả giá của đúng rạp theo Money DTO đã quy định.
5. Không hard-code giá seed trong service.
6. Xử lý rạp không tồn tại hoặc thiếu cấu hình giá. Không tự coi giá thiếu là `0`.
7. Thống nhất với BE 2 cách tái sử dụng dữ liệu/logic đọc giá.

**Ranh giới trách nhiệm**

API này cung cấp **bảng giá tham khảo**. BE 2 vẫn phải tính quotation và kiểm tra lại giá khi tạo booking; FE không được dùng tổng tiền tự tính làm giá cuối cùng.

**Commit khi hoàn thành**

| Mốc hoàn thành | Commit |
|---|---|
| Hai API trả dữ liệu thật | `feat(api): expose ticket categories and cinema pricing` |
| Kiểm thử giá, rạp và cấu hình thiếu đạt | `test(api): cover cinema pricing and missing configuration` |
| Cập nhật tài liệu và log | `docs(api): document ticket category and pricing endpoints` |

**Checklist nghiệm thu**

- [ ] Category đúng `Adult`/`Discounted`.
- [ ] Giá đúng với rạp được yêu cầu.
- [ ] Tiền trả đúng cấu trúc Money và VND.
- [ ] Không tự thêm loại vé hoặc quy tắc giảm giá.
- [ ] Không dùng giá mặc định thay cho cấu hình bị thiếu.
- [ ] Không cập nhật giá trong database.

**Kết quả FE nhận được:** hiển thị hai loại vé, điều kiện và đơn giá.

---

**Việc 4 — Sơ đồ ghế theo suất**

Branch: `feat/be-showtime-seat-map`

```http
GET /api/showtimes/{showtimeId}/seats
```

**Cần làm**

1. Mapping các bảng còn thiếu:
   - `seat`
   - `seat_type`
   - `booking_seat` phục vụ đọc allocation.
2. Xác định phòng từ `showtimeId`, không tin một `roomId` riêng do client tự gửi.
3. Trả nhãn ghế, tọa độ, loại ghế, phụ thu và trạng thái hiển thị theo `SeatMap` DTO.
4. Xử lý ghế retired/ngừng sử dụng đúng đặc tả.
5. Xác định ghế không khả dụng khi có allocation hiệu lực `held` hoặc `sold`.
6. Allocation `released` không tiếp tục chiếm ghế.
7. Không suy ghế đã trống chỉ vì thời hạn booking đã qua; phải dựa trên trạng thái allocation đã được cập nhật hợp lệ.
8. Không trả thông tin khách, booking ID hoặc payment của người đang giữ ghế.
9. Truy vấn toàn bộ sơ đồ hiệu quả, tránh một query cho mỗi ghế.

**Phối hợp với BE 2**

- Chỉ một bản định nghĩa entity và mapping cho `booking_seat`.
- BE 1 đọc allocation; BE 2 tạo/chuyển trạng thái allocation.
- Trạng thái “đang chọn” trên màn hình của khách không đồng nghĩa với `held` trong database.
- API sơ đồ ghế chỉ phản ánh thời điểm đọc; BE 2 phải kiểm tra lại khi giữ ghế.

**Commit khi hoàn thành**

| Mốc hoàn thành | Commit |
|---|---|
| API trả layout và availability thực tế | `feat(api): add showtime seat map with allocation availability` |
| Kiểm thử các trạng thái ghế đạt | `test(api): cover seat availability and room isolation` |
| Cập nhật tài liệu và log | `docs(api): document seat map behavior and integration checks` |

**Checklist nghiệm thu**

- [ ] Mỗi suất trả đúng ghế của phòng tương ứng.
- [ ] Với phòng seed hiện tại, trả đúng 50 ghế khi không có thay đổi layout.
- [ ] Tọa độ, nhãn, Standard/VIP và phụ thu đúng dữ liệu.
- [ ] Ghế `held`/`sold` không khả dụng.
- [ ] Ghế đã `released` có thể khả dụng nếu không có allocation khác.
- [ ] Ghế ngừng sử dụng không được xem là ghế trống để đặt.
- [ ] Không tiết lộ người đang giữ hoặc mua ghế.
- [ ] Gọi API không tạo selection/booking hoặc sửa allocation.

**Kết quả FE nhận được:** vẽ được sơ đồ ghế và chặn lựa chọn các ghế không khả dụng.

---

**Quy tắc commit và kiểm thử chung**

Các commit phía trên là mốc đề xuất. Mỗi commit phải là một thay đổi có ý nghĩa; không tạo commit rỗng chỉ để đủ danh sách.

Trước mỗi commit:

```powershell
git diff --check
dotnet build .\src\backend\CinemaTicketBooking.Api\CinemaTicketBooking.Api.csproj
git status
```

Chạy thêm test liên quan nếu đã bổ sung. Stage các file thuộc nhiệm vụ, tránh kéo theo thay đổi của người khác.

**Không dùng Supabase chung làm nơi tạo dữ liệu lỗi để test.** Các tình huống như phim archived, ghế sold, giá thiếu hoặc suất bị hủy phải dùng database test Docker riêng và có cleanup phù hợp.

**Checklist hoàn tất mỗi branch/PR**

- [ ] Endpoint đúng path, query và DTO đã thống nhất.
- [ ] Build đạt, các test liên quan đạt.
- [ ] Đã gọi API với PostgreSQL thật.
- [ ] Không phá API danh sách phim hiện có.
- [ ] Không có password, connection string thật hoặc dữ liệu cá nhân trong commit/log.
- [ ] Không tự chạy migration/bootstrap khi backend khởi động.
- [ ] Tài liệu ghi riêng trạng thái “đã triển khai” và “đã kiểm chứng”.
- [ ] `PROJECT_STRUCTURE.md` chỉ liệt kê file thực sự được tạo.
- [ ] Ghi log vào file thành viên của chính người thực hiện.
- [ ] PR nêu cách chạy, kết quả kiểm thử và giới hạn còn lại.
- [ ] Ngài review trước khi merge vào branch tích hợp.

**Bàn giao cuối cùng của BE 1**

BE 1 cần cung cấp **7 endpoint mới**, ví dụ request/response, dữ liệu ID dùng để thử trên môi trường tương ứng và kết quả kiểm thử. FE có thể đi từ một phim đến suất chiếu, sơ đồ ghế và bảng giá mà không cần hard-code dữ liệu.

