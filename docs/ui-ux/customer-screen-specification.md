# Đặc tả màn hình khách hàng — Hệ thống quản lý rạp chiếu phim và đặt vé trực tuyến

## 1. Mục đích, căn cứ và trạng thái tài liệu

### 1.1. Mục đích

Tài liệu mô tả nội dung, dữ liệu, thao tác, validation, trạng thái và tiêu chí nghiệm thu của từng màn hình Customer. Đây là đầu vào cho thiết kế, triển khai và kiểm thử; không phải báo cáo chức năng đã hoàn thành.

Phạm vi tập trung vào hành trình xem phim → lịch chiếu → chọn ghế và loại vé → tạo booking giữ ghế → thanh toán → xem vé và lịch sử. Màn hình quản trị, soát vé của nhân viên và đối soát nội bộ không thuộc tài liệu này.

### 1.2. Hiện trạng kiểm chứng

Tại lần đối chiếu ngày **27/10/2026**, các file `App.tsx`, `main.tsx`, `index.css`, `package.json`, `Program.cs`, `ApplicationDbContext.cs` và project backend hiện có nhưng đều rỗng. Chưa có router, màn hình Customer, controller, DTO hoặc service nghiệp vụ trong mã nguồn hiện tại.

Repository có tài liệu yêu cầu, thiết kế, API, script schema và seed. Có script không chứng minh schema/seed đã chạy thành công trên database cụ thể. Các API, xác thực, worker và luồng thanh toán dưới đây là nội dung thiết kế, **chưa được triển khai trong mã nguồn hiện có**.

### 1.3. Quy ước mức độ xác nhận

| Nhãn | Ý nghĩa |
|---|---|
| **Theo yêu cầu đã chốt** | Có căn cứ trong FR/BR hoặc quyết định được nguồn ghi là đã chốt; không đồng nghĩa đã triển khai. |
| **Theo đặc tả UI hiện có** | Kế thừa tổ chức màn hình, bố cục hoặc component trong bộ UI/UX; chưa có giao diện chạy để xác minh. |
| **Đề xuất UI** | Cách trình bày/phản hồi đề xuất tại đây; cần review thiết kế, không tạo business rule hoặc API mới. |
| **Chờ chốt / thiếu contract** | Nguồn còn OQ, thiếu dữ liệu/endpoint hoặc có mâu thuẫn; chưa phải hành vi được phê duyệt. |

Mã `KH-*`, `NT-*`, `DL-*`, `CX-*` chỉ phục vụ truy vết trong tài liệu, không phải route, mã lỗi API, trạng thái database hay yêu cầu nghiệp vụ mới. Các câu thông báo tiếng Việt là **đề xuất UI** nếu không ghi khác.

Tên màn hình không xác định URL frontend. Repository chưa có router; tài liệu không tự chốt đường dẫn trang đăng nhập, thanh toán hoặc vé.

### 1.4. Nguồn đối chiếu

| Mã | Nguồn | Nội dung sử dụng |
|---|---|---|
| S01 | [Phân tích nghiệp vụ](../requirements/business-system-analysis.md), mục 3, 7–9, 11–13 | Phạm vi, FR/BR/NFR/AC, OQ-01 đến OQ-09; nguồn quy tắc nghiệp vụ. |
| S02 | [Use case chi tiết](../requirements/use-cases.md), mục 2–11 | Booking, idempotency, payment, hết hạn, đối soát, vé và OQ-UC. |
| S03 | [Thiết kế nghiệp vụ](../analysis%20and%20design/business-system-design.md), mục 0, 1.5 | Use case tổng quan, hủy booking và đổi ghế. Mã UC khác S02; dẫn UC phải ghi nguồn. |
| S04 | [Kiến trúc](../analysis%20and%20design/architecture.md), mục 4, 6–9, 12a, 14–17 | Ranh giới frontend/backend, hiện trạng, worker và đề xuất chưa duyệt. |
| S05 | [Thiết kế database](../../database/database-design.md), mục 8–16, 27, 31, 34–36 | Couple, giá snapshot, liên hệ và vòng đời. |
| S06 | [Mô hình quan hệ](../analysis%20and%20design/relational-model.md), mục 3, 9–10 | Trạng thái, quan hệ, snapshot, ownership. |
| S07 | [Schema mô tả](../../database/database-schema.md), [DDL PostgreSQL](../../database/database-schema.sql) | Cột, kiểu dữ liệu và constraint được viết trong repository. |
| S08 | [API contract](../api/api-contract.md), [endpoint](../api/api-endpoints.md), [DTO](../api/dto-specification.md) | Quy ước giao tiếp; endpoint/DTO còn Proposed. |
| S09 | [User flow](customer-user-flow.md), [navigation](customer-navigation.md), [mapping API](customer-screen-api-mapping.md) | Hành trình và dữ liệu màn hình; điểm chưa đồng bộ tại mục 6. |
| S10 | [Layout](customer-web-layout.md), [component](customer-component-specification.md), [UI state](customer-ui-state.md), [responsive](customer-responsive-specification.md) | Bố cục, component, trạng thái và thiết bị. |
| S11 | [Seed SQL](../../database/seed-data.sql), [hướng dẫn seed](../../database/seed-README.md), [ghi chú phim](../../database/movie-research.md) | Dữ liệu demo, không thay chính sách hoặc database thực tế. |
| S12 | [Cấu trúc](../../PROJECT_STRUCTURE.md), [thiết lập local](../development/local-development-setup.md), mã nguồn trong `src/` | Đối chiếu cấu trúc và hiện trạng. |
| S13 | [Vision PA1](../../pa/PA1/02.%20rup_vision_sp.docx), [kế hoạch PA1](../../pa/PA1/01.%20Software%20Development%20Plan.docx), PDF trong `pa/` | Bối cảnh/kế hoạch ban đầu; khác biệt thanh toán mô phỏng tại CX-10. |
| S14 | [Bảng kế hoạch công việc](../Cinema_292_Tasks_Replanned_5_Members_5_Sprints.xlsx) | Danh mục task; không chứng minh chức năng đã triển khai hoặc contract đã duyệt. |

Tài liệu này không sửa quyết định nghiệp vụ, schema hoặc API. Điểm khác nhau giữa nguồn được ghi tại mục 6 để review; không âm thầm hợp nhất thành quy tắc mới.

## 2. Phạm vi màn hình và quy tắc dùng chung

### 2.1. Danh mục màn hình

| Mã | Màn hình / nhóm màn hình | Truy cập | Ngữ cảnh đầu vào | Kết quả chính |
|---|---|---|---|---|
| KH-01 | Trang chủ — danh sách phim (Home) | Công khai | Trạng thái phim, phân trang | Chọn phim. |
| KH-02 | Chi tiết phim (Movie Detail) | Công khai | `movieId` | Xem thông tin, chuyển xem lịch. |
| KH-03 | Lịch chiếu (Showtime) | Công khai | Phim, ngày | Chọn suất còn bán. |
| KH-04 | Chọn ghế và loại vé (Seat Selection) | Xem ghế công khai theo S08; tạo booking cần đăng nhập | `showtimeId`, lựa chọn cục bộ | Tạo booking, nhận hạn giữ và giá chốt. |
| KH-05 | Thanh toán (Payment) | Chủ booking đã đăng nhập | Booking; payment attempt nếu đã biết | Thanh toán, theo dõi, hủy khi đủ điều kiện. |
| KH-06 | Xác nhận đặt vé và vé điện tử (Booking Confirmation) | Chủ booking đã đăng nhập | Booking | Hiển thị booking confirmed và vé. |
| KH-07 | Đăng nhập / đăng ký | Công khai | Form; đích quay lại nếu có | Tạo tài khoản Customer hoặc phiên. |
| KH-08 | Tài khoản — booking của tôi và chi tiết booking | Customer đã đăng nhập | Phiên, phân trang, booking được chọn | Xem lịch sử, trạng thái, vé cá nhân. |

Chi tiết booking nằm trong KH-08; trang riêng hay vùng chi tiết là **đề xuất UI**, chưa chốt route. FR-AUTH-003 có yêu cầu hồ sơ cá nhân nhưng chưa có API/DTO tương ứng; xem KH-08 và DL-08.

### 2.2. Phạm vi nghiệp vụ

- MVP/demo vận hành một cinema. Schema hỗ trợ nhiều cinema không tạo yêu cầu chọn tỉnh hoặc chuyển booking giữa chi nhánh.
- Khách được duyệt phim/lịch khi chưa đăng nhập. Tạo booking, xem payment và vé cá nhân cần xác thực.
- Ghế chỉ được giữ khi backend tạo booking thành công. Chọn trên trình duyệt chưa tạo quyền giữ ghế.
- Thời hạn giữ danh nghĩa là 5 phút từ lúc tạo booking do backend xác định. Countdown không tự đổi trạng thái booking/payment/ghế.
- Một booking gắn một suất và ít nhất một đơn vị ghế; mỗi đơn vị ghế có một loại vé. Không tách thanh toán từng detail.
- Couple là một đơn vị bán cho hai người: một detail, hai vé độc lập sau xác nhận; không bán riêng hai nửa.
- Demo không có Customer hủy booking đã thanh toán, refund, loyalty, khuyến mãi phức tạp hoặc workflow tự đổi lịch/hoàn tiền khi rạp hủy suất.
- Không đưa đồ ăn/combo, đánh giá phim, định dạng 2D/3D/IMAX, đăng nhập mạng xã hội, OTP hoặc gợi ý AI vào màn hình như chức năng đã chốt: chưa có yêu cầu và contract đầy đủ.

### 2.3. Khung trang và định dạng

Theo S09–S10, màn hình dùng chung header, main và footer. Header có logo về Home, nhóm phim đang chiếu/sắp chiếu, khu vực tài khoản. Chưa đăng nhập hiện đăng nhập/đăng ký; đã đăng nhập hiện tên, booking của tôi, đăng xuất. Footer dùng thông tin rạp/liên kết tĩnh khi đã có nội dung; không tự tạo địa chỉ, điều khoản hoặc chính sách.

| Dữ liệu | Quy tắc |
|---|---|
| Tên phim/phòng/ghế/loại vé | Dùng API; nội dung lịch sử dùng snapshot backend cung cấp, không thay bằng danh mục hiện tại. |
| Tiền | VND nguyên đồng, ví dụ định dạng `100.000 ₫`. Giá cuối/tổng do server tính; không thêm phí/thuế/giảm giá tự đặt. |
| Mốc thời gian | API truyền UTC ISO 8601; hiển thị `Asia/Ho_Chi_Minh`. **Đề xuất UI:** `dd/MM/yyyy`, `HH:mm`, ghi rõ ngày khi cần. |
| Ngày phát hành | Là `date`, không dịch múi giờ như timestamp. |
| Trạng thái | Nhãn tiếng Việt; xử lý đúng mã API. Booking, payment, ticket, phân bổ ghế độc lập. |
| Mã booking/vé | Hiển thị nguyên mã server. DDL dùng UUID; không suy định dạng từ ví dụ `BK-...`/`TK-...` của DTO; xem CX-06. |
| Dữ liệu chưa có | Không bịa mô tả, ghế trống, giá, QR hoặc chính sách. Trường tùy chọn có thể ẩn/báo chưa cập nhật; thiếu trường bắt buộc cần báo lỗi dữ liệu. |

API dùng `camelCase`, database dùng `snake_case`. Mô tả giao tiếp frontend dùng `startsAt`, `expiresAt`, `totalAmount`, `bookingCode`, `ticketCode`. Tên cột như `capacity_units_snapshot` không tự trở thành trường DTO.

### 2.4. Phản hồi và bảo vệ dữ liệu

| Tình huống | Hành vi UI |
|---|---|
| Tải dữ liệu / đổi điều kiện chính | Loading/skeleton trong main; giữ khung trang, chưa hiện empty. |
| Tải thành công, danh sách rỗng | Thông báo đúng ngữ cảnh; có thao tác đổi điều kiện/quay lại khi phù hợp. |
| Mạng / máy chủ lỗi | Lỗi thân thiện, thử lại request đọc; không lộ stack trace hoặc cấu hình. |
| Validation | Lỗi cạnh input theo `error.details`; giữ dữ liệu không nhạy cảm đã nhập. |
| HTTP 401 | Đăng nhập, giữ đích quay lại; tải lại trạng thái nghiệp vụ sau xác thực. |
| HTTP 403 | Không có quyền; không lộ dữ liệu người khác, không lặp chuyển đăng nhập khi phiên hợp lệ. |
| HTTP 404 | Không tìm thấy tài nguyên theo ngữ cảnh; cho về danh sách. Suất hủy không mặc nhiên là 404. |
| Đang gửi thao tác | Nút loading, chặn gửi trùng trong UI; backend vẫn cần idempotency. |
| Đăng xuất / đổi tài khoản | Xóa dữ liệu riêng tư trên UI và state tài khoản cũ; về Home. Đăng xuất không tự hủy booking server. |

Phân nhánh theo `error.code` khi contract đã chốt, không so chuỗi thông báo để quyết định thanh toán thành công hoặc ghế đã nhả. Mã lỗi ở S08 còn có phần Proposed.

Không lưu mật khẩu, dữ liệu thẻ, secret cổng vào URL, log hoặc state lưu bền vững. Tài liệu không chốt nơi lưu token, refresh token hay cơ chế thu hồi mới. Backend kiểm tra role/ownership; ẩn nút hoặc biết UUID không thay quyền truy cập.

### 2.5. Responsive và khả năng thao tác

Theo S10: desktop ≥ 1024px, tablet 768–1023px, mobile < 768px. Grid phim lần lượt 3/2/1 cột. Payment/Confirmation một cột. Ghế cuộn ngang trong khối trên mobile, tránh cuộn ngang toàn trang; vùng bấm ghế tối thiểu 44px. Kích thước kiểm tra: 320px, 768px, 1024px, 1440px.

**Đề xuất UI:** input có nhãn, focus bàn phím rõ, ghế có tên truy cập gồm nhãn/loại/trạng thái; không chỉ dùng màu; modal quản lý focus/hỗ trợ Escape; lỗi quan trọng vẫn đọc được ngoài toast. Countdown không cần được công cụ hỗ trợ đọc lại mỗi giây. Đây không phải tuyên bố đã đạt chứng nhận accessibility.

## 3. Đặc tả từng màn hình

Endpoint trong mục 3 theo S08, hiện chưa triển khai. Phân biệt dữ liệu **đã có trong DTO Proposed** với **nhu cầu thiếu contract**; không tự mở rộng request/response tại đây.

### 3.1. KH-01 — Trang chủ / danh sách phim

#### Mục tiêu và điều kiện vào

Giúp khách tìm phim đang chiếu hoặc sắp chiếu và mở chi tiết phim. Truy cập công khai, không tạo booking hoặc payment khi tải trang. Căn cứ: FR-MOVIE-001, S08 mục Movies, S09–S10.

#### Nội dung và dữ liệu

| Vùng | Nội dung | Nguồn / giới hạn |
|---|---|---|
| Tiêu đề và phân nhóm | “Phim đang chiếu”, “Phim sắp chiếu”; nhóm hiện tại có dấu hiệu được chọn | `now_showing`, `coming_soon` từ query/response. Không tự suy từ ngày phát hành. |
| Danh sách card | Poster, tên phim, nhãn phân loại tuổi, nhãn trạng thái | DTO danh sách: `id`, `title`, `posterUrl`, `ageRating`, `status`; có `durationMinutes` nếu thiết kế hiển thị thời lượng. |
| Phân trang | Chuyển trang hoặc tải thêm | Dùng `pagination.page`, `pageSize`, `totalItems`, `totalPages`; chọn một cách tương tác khi review UI. |
| Ảnh thiếu / hỏng | Khung thay thế có tên phim hoặc mô tả ảnh | **Đề xuất UI**; không lấy poster của phim khác để thay. |
| Banner | Không bắt buộc cho luồng nghiệp vụ | Bản đặc tả cũ có nhắc banner nhưng chưa có nguồn dữ liệu banner riêng; nếu dùng chỉ lấy nội dung đã cung cấp, không tạo khuyến mãi giả. |

Không dùng `title` làm định danh: schema cho phép trùng tên. Phim `archived` không xuất hiện theo FR-MOVIE-001; hai nhóm chính không tự gom `ended` vào đang chiếu. Chính sách truy cập trực tiếp phim đã ngừng hiển thị còn cần thống nhất ở CX-09.

#### Thao tác và trạng thái

1. Vào trang: tải `GET /api/movies?status=...&page=...&pageSize=...` theo nhóm đang xem.
2. Đổi nhóm hoặc trang: cập nhật danh sách, hiển thị loading; không trộn kết quả của nhóm trước vào nhóm mới. **Đề xuất UI:** khi đổi nhóm, trở về trang đầu.
3. Nhấn card / “Xem chi tiết”: mở KH-02 với đúng `id` của phim; không tạo booking.
4. Rỗng: “Hiện chưa có phim trong danh sách này”; vẫn cho đổi nhóm.
5. Lỗi: “Không tải được danh sách phim” và “Thử lại”; giữ nhóm đang xem.

**Chờ contract:** tìm kiếm theo từ khóa/lọc thể loại xuất hiện trong kế hoạch hoặc bản màn hình cũ, nhưng DTO Movies chỉ có `status`, `page`, `pageSize`. Chưa chốt tham số tìm kiếm, lọc thể loại hoặc sắp xếp. Không trình bày tìm kiếm cục bộ trên một trang dữ liệu như tìm kiếm toàn bộ kho phim; xem DL-01.

#### Tiêu chí nghiệm thu

- **NT-01:** API trả danh sách hợp lệ thì card hiển thị đúng dữ liệu và mở đúng phim, kể cả hai phim trùng tên.
- **NT-02:** Đổi nhóm/phân trang không lẫn dữ liệu; loading, rỗng, lỗi được phân biệt; không hiển thị phim archived trong danh sách Customer.
- **NT-03:** Thiếu poster hoặc age rating không làm hỏng trang và không tạo thông tin thay thế sai; grid hoạt động ở các kích thước mục 2.5.

### 3.2. KH-02 — Chi tiết phim

#### Mục tiêu và điều kiện vào

Cung cấp thông tin phim, giúp khách quyết định xem lịch chiếu. Truy cập công khai theo định danh phim; tải lại trực tiếp phải đọc từ API, không phụ thuộc bắt buộc vào card đã bấm trước đó. Căn cứ: FR-MOVIE-002 và DTO Movies mục 3.

#### Nội dung và bố cục

Desktop: poster bên trái, thông tin và hành động bên phải theo S10. Trên màn hình hẹp, **đề xuất UI** xếp poster/thông tin thành một cột, ưu tiên tên phim và nút xem lịch.

| Nội dung | Trường DTO Proposed | Hành vi khi không có |
|---|---|---|
| Tên phim | `title` | Dữ liệu bắt buộc; báo lỗi dữ liệu nếu thiếu. |
| Poster | `posterUrl` | Khung thay thế, giữ tên phim đọc được. |
| Thời lượng | `durationMinutes` | Không tự lấy thời lượng phim khác hoặc mặc định một giá trị. |
| Mô tả | `description` | “Nội dung đang được cập nhật” hoặc ẩn khối theo thiết kế. |
| Ngôn ngữ | `language` | Không suy phụ đề/lồng tiếng hoặc ngôn ngữ suất từ ngôn ngữ gốc. |
| Phân loại tuổi | `ageRating` | Hiển thị đúng nhãn nhận được; không đổi `PG-13` thành `T13`/`T16`. |
| Ngày phát hành | `releaseDate` | Không tự suy thời điểm mở bán. |
| Thể loại | `genres` | Hiển thị tên/mã được API cung cấp; mảng rỗng không phát sinh thể loại giả. |
| Trailer | `trailerUrl` | Chỉ có thao tác xem trailer khi có URL; nhúng hay mở liên kết là đề xuất UI. |
| Trạng thái | `status` | Ánh xạ nhãn theo đúng giá trị; không tự đổi trạng thái theo ngày. |

Không bổ sung đạo diễn, diễn viên, đánh giá sao, doanh thu hay quốc gia như dữ liệu đã có: DTO/schema hiện tại không mô tả các trường đó. Nhãn tuổi là thông tin hiển thị; chưa có contract xác minh tuổi khách từ nhãn này.

#### Thao tác và trạng thái

- Tải `GET /api/movies/{movieId}` khi vào trang hoặc đổi phim.
- “Chọn suất chiếu” mở KH-03 với cùng phim. KH-03 có thể trả danh sách rỗng; không giả định `now_showing` luôn có suất hoặc `coming_soon` chắc chắn chưa được bán.
- Nếu thiết kế hiển thị suất ngay trong chi tiết phim, dùng cùng quy tắc KH-03; không tạo bộ quy tắc bán vé thứ hai. Việc gộp thành một trang chưa chốt route, xem CX-08.
- 404: “Không tìm thấy phim”, có hành động về danh sách. Mất mạng/lỗi server có “Thử lại”, không thông báo nhầm là phim không tồn tại.
- Chưa có endpoint lịch chiếu thành công thì không hiển thị số suất hoặc giờ chiếu tự tạo. Backend vẫn quyết định phim/suất có được bán khi tạo booking.

#### Tiêu chí nghiệm thu

- **NT-04:** Vào trực tiếp theo định danh vẫn tải đúng phim; trường tùy chọn thiếu không tạo dữ liệu giả; trailer chỉ xuất hiện khi có dữ liệu.
- **NT-05:** Nút xem lịch giữ đúng phim; 404, lỗi tải và chưa có suất có thông báo khác nhau.

### 3.3. KH-03 — Lịch chiếu

#### Mục tiêu và điều kiện vào

Cho khách xem suất theo phim/ngày và chọn suất còn bán. Công khai; nhận phim đang xem và ngày được chọn. Căn cứ: FR-SHOW-001, BR-ST-003, S08 DTO Showtimes và S09–S10.

#### Nội dung và dữ liệu

| Vùng | Nội dung | Nguồn / điều kiện |
|---|---|---|
| Ngữ cảnh phim | Tên phim, có thể kèm poster nhỏ | Chi tiết phim đã tải hợp lệ hoặc API movie. Không suy tên từ UUID. |
| Chọn ngày | Ngày đang xem; tabs/dropdown theo thiết bị | Query `date` của API lịch. Dải ngày mở bán và ngày mặc định chưa được quy định. |
| Suất chiếu | Giờ bắt đầu, ngày, tên phòng, giá cơ sở, tình trạng có thể đặt | DTO: `id`, `cinemaRoomId`, `cinemaRoomName`, `startsAt`, `basePrice`, `currency`, `status`, `bookingAvailable`. |
| Thông tin rạp | Tên/địa chỉ nếu có dữ liệu phù hợp | MVP một cinema; DTO danh sách suất chưa có trường rạp, xem DL-02. |
| Ghi chú giá | Giá cơ sở cho một người; giá cuối tùy loại ghế/loại vé | Không gọi `basePrice` là tổng thanh toán hoặc giá cuối của Couple/VIP. |

Theo S10, ngày nằm trên danh sách suất; suất xếp theo giờ hoặc nhóm ngày. **Đề xuất UI:** ngày đang chọn luôn đọc được và mỗi nút suất thể hiện đủ giờ/phòng. Không tự chốt cửa sổ 7/14 ngày, nhãn “hôm nay” theo UTC hoặc số ghế còn lại khi API không trả.

#### Điều kiện chọn và tải lại

1. Gọi `GET /api/movies/{movieId}/showtimes?date=...` khi vào trang/đổi ngày. Ngày phải thống nhất với ngày hiển thị tại Việt Nam; ngữ nghĩa lọc ngày cần chốt ở DL-02.
2. Chỉ cho chuyển chọn ghế khi suất `scheduled` và `bookingAvailable = true` theo dữ liệu backend. Kiểm tra này phục vụ UI; backend vẫn kiểm tra lại lúc tạo booking.
3. `draft`, `in_progress`, `completed`, `cancelled` không phải suất nhận booking mới. Không tự áp cutoff 10/15/30 phút trước giờ chiếu.
4. FR-SHOW-001 yêu cầu danh sách chỉ có suất còn bán. Nếu API không trả suất đóng/hủy thì không tạo ô suất giả; nếu dữ liệu đã hiển thị bị thay đổi/không còn bán, vô hiệu thao tác và làm mới. Khác biệt với tài liệu UI cũ được ghi tại CX-04.
5. Chọn suất hợp lệ: mở KH-04 với `showtimeId`, không chỉ truyền tên phòng/giờ vì các giá trị đó có thể trùng.

#### Loading, rỗng và lỗi

- Đổi ngày: loading cho khối suất, vẫn giữ ngữ cảnh phim. **Đề xuất UI:** bỏ qua response cũ nếu người dùng đã đổi ngày khác.
- Không có suất: “Chưa có suất chiếu cho ngày đã chọn”; cho đổi ngày hoặc về phim.
- 404: xử lý phim/suất không tìm thấy theo tài nguyên được API báo, không mặc định mọi 404 là suất hủy.
- Lỗi mạng: thử lại cùng phim/ngày; không giữ nút đặt có thể bấm dựa trên dữ liệu lỗi thời.
- Hết ghế chỉ được kết luận nếu API có dữ liệu xác nhận; `bookingAvailable` chưa được đặc tả là số ghế tồn kho.

#### Tiêu chí nghiệm thu

- **NT-06:** Đổi ngày gửi đúng ngữ cảnh, hiển thị đúng ngày/giờ Việt Nam và không lẫn response của ngày trước.
- **NT-07:** Chỉ suất được backend cho bán mới dẫn tới chọn ghế; khi suất đóng trong lúc khách đang xem, request tạo booking vẫn bị kiểm tra lại.
- **NT-08:** Danh sách rỗng có cách đổi ngày; giá cơ sở không bị hiển thị thành giá cuối; UI không tự áp cutoff từ dữ liệu seed.

### 3.4. KH-04 — Chọn ghế và loại vé

#### Mục tiêu, quyền truy cập và hai giai đoạn

Khách chọn các đơn vị ghế cho một suất, chọn loại vé rồi gửi yêu cầu tạo booking. Căn cứ: FR-SHOW-002, FR-BOOK-001/002, BR-BOOK-001..007/009/010; S02 UC-01; S05 mục 8–13, 27 và 35.

| Giai đoạn | Dữ liệu và quyền giữ ghế | Hành vi |
|---|---|---|
| Trước khi tạo booking | Lựa chọn cục bộ; API xem ghế công khai theo S08 | Chọn/bỏ chọn, đổi loại vé. **Chưa chạy countdown giữ ghế.** Yêu cầu đăng nhập trước khi gửi tạo booking. |
| Sau khi tạo thành công | Booking `pending`, toàn bộ detail `held`, có `expiresAt` và giá snapshot | Chuyển KH-05. Nếu quay lại KH-04 trong ngữ cảnh booking cũ, không sửa trực tiếp ghế/loại vé của booking đã tạo. |

Theo navigation hiện tại, API xem ghế là public nhưng chưa chốt riêng guard cho trang ghế. **Đề xuất UI:** cho xem sơ đồ trước đăng nhập và yêu cầu xác thực tại hành động đặt; sau đăng nhập tải lại suất/ghế trước khi tiếp tục. Không tự gửi POST đặt vé chỉ vì hoàn tất đăng nhập.

#### Bố cục và thành phần

- Thông tin đầu trang: tên phim, ngày/giờ suất, phòng và rạp nếu API cung cấp; có hành động quay lại lịch.
- Khối sơ đồ: các hàng `rowLabel`, các ghế `seatNumber`, phân biệt Standard/VIP/Couple, chú thích trạng thái. **Đề xuất UI:** có nhãn “Màn hình” để định hướng; vị trí thực và lối đi chưa có trong DTO, xem DL-03.
- Khối lựa chọn: nhãn ghế, loại ghế, số chỗ, loại vé áp dụng và thông tin giá được server cung cấp. Không bắt buộc sơ đồ có số hàng/cột cố định.
- Vùng thông tin liên hệ trước khi tạo booking là nhu cầu còn thiếu contract; xem bên dưới và DL-05.
- Hành động chính “Đặt vé”; hành động quay lại. Desktop hai cột sơ đồ/tóm tắt; tablet tóm tắt dưới sơ đồ; mobile có bottom bar tổng hợp và nút đặt theo S10, không che các hàng ghế cuối.

#### Dữ liệu cần dùng

| Dữ liệu | Nguồn hiện có | Giới hạn |
|---|---|---|
| Ngữ cảnh suất | `GET /api/showtimes/{showtimeId}` được liệt kê ở endpoint | Chưa có DTO chi tiết cho endpoint này; cần phim/phòng/giờ/tình trạng bán, không tự bịa response. |
| Sơ đồ và phân bổ | `GET /api/showtimes/{showtimeId}/seats` | DTO có `showtimeId`, `rows[].rowLabel`, `seats[].id`, `seatNumber`, `seatTypeCode`, `capacityUnits`, `surchargeAmount`, `status`. |
| Trạng thái vật lý | Schema `seat.status`: `active`, `maintenance`, `inactive` | DTO ghế chỉ nêu `available`, `held`, `sold`, chưa thể hiện độc lập tình trạng bảo trì; DL-03. |
| Loại vé | `GET /api/ticket-types` có trong danh mục endpoint | Chưa có DTO; cần ID, tên/mã, trạng thái áp dụng và điều kiện được backend xác định; DL-04. |
| Giá trước đặt | Có base price ở lịch và surcharge ở sơ đồ | Chưa có API báo giá chi tiết/tổng cho lựa chọn; không tự tính giá trên frontend; DL-04. |
| Kết quả booking | `POST /api/bookings` | DTO Proposed trả booking, giá chốt và hạn giữ; là dữ liệu có thẩm quyền sau tạo. |

Không tự thêm WebSocket, SignalR hoặc tần suất polling cố định. Theo S09, tải sơ đồ khi vào lại và khi cần cập nhật sau xung đột; cơ chế tự cập nhật định kỳ cần nhóm thống nhất. Sơ đồ đọc trước đó không bảo đảm ghế vẫn trống lúc gửi đặt.

#### Trạng thái ghế và thao tác

| Trạng thái / đặc tính | Hiển thị và điều kiện thao tác |
|---|---|
| Khả dụng | Có thể chọn khi ghế hoạt động, suất còn bán và chưa có phân bổ hiệu lực theo server. |
| Đang chọn | Đánh dấu lựa chọn trong phiên, có thể bỏ chọn trước tạo booking. Đây không phải `held` trong database. |
| `held` | Hiện đang được giữ, không cho người khác chọn. Không lộ danh tính chủ giữ. |
| `sold` | Hiện đã bán, không cho chọn. |
| `maintenance` / `inactive` | Không cho chọn dù không có detail held/sold; contract phải cung cấp cách phân biệt với ghế khả dụng. |
| Couple | Chọn/bỏ chọn nguyên đơn vị; một `seatId`, một `ticketTypeId`, sức chứa 2. Không gửi hai item trùng ghế. |

Một ghế Couple hiển thị rộng hơn theo đặc tả component; không suy rằng nó luôn nằm ở hàng cuối hoặc phải ghép hai ghế liên tiếp. Số ghế đang chọn và số chỗ/người phải phân biệt: chọn một ghế Standard và một Couple là hai đơn vị ghế, ba chỗ, dự kiến ba vé sau xác nhận.

Mỗi đơn vị ghế có một loại vé; các detail của cùng booking có thể khác loại vé. S05 mục 10 và tiền điều kiện UC-01 của S02 nêu Couple bản đầu dùng `ADULT`; không tự cho hai người trên Couple dùng hai loại vé khác nhau. Điều kiện cụ thể của vé trẻ em/sinh viên vẫn OPEN theo OQ-08. Có danh mục seed Student/Child không chứng minh backend đã xác minh quyền lợi.

**Giới hạn giá:** phụ thu tính một lần trên mỗi đơn vị ghế; giảm theo loại vé chỉ tính trên phần giá cơ sở. UI cần giải thích “Mức giảm áp dụng trên giá cơ sở, không áp dụng cho phụ thu ghế”. Công thức server được tham chiếu tại mục 4.3; frontend không tự tính một con số rồi hiển thị như báo giá chính thức. Khi chưa có contract báo giá, có thể hiện các thành phần tham khảo đã nhận và thông báo “Giá chốt được trả về khi tạo booking”; cách hiển thị tổng tạm tính cần chốt ở DL-04.

#### Thông tin liên hệ

Schema booking bắt buộc `contact_email_snapshot`, cho phép `contact_phone_snapshot` NULL; fingerprint ở S02 cũng đề cập contact info. Tuy nhiên request tạo booking ở S08 chưa có trường liên hệ, còn S09 đặt bước nhập liên hệ sau khi đã tạo booking. Chưa có API cập nhật liên hệ booking.

**Đề xuất UI chờ contract:** xác nhận email và điện thoại tùy chọn trước POST tạo booking; có thể điền từ hồ sơ nếu API cung cấp và chính sách cho phép. Cần nhóm chốt lấy tự động từ tài khoản hay cho nhập riêng. Không tự thêm field request hoặc giả định sửa liên hệ sau tạo đã được hỗ trợ. Email liên hệ không đồng nghĩa đã có gửi vé qua email; S04 mục 8 nêu Email/SMTP ngoài MVP.

#### Validation và gửi yêu cầu

1. Chưa có ghế: nút đặt disabled. Khi đang gửi: khóa lựa chọn và ngăn gửi trùng. Cần có loại vé hợp lệ cho từng ghế trước khi gửi.
2. Ghế không lặp, thuộc phòng của cùng suất, đang hoạt động; loại vé phải được backend kiểm tra. Không đặt giới hạn tối đa 6/8/10 vé, không tự cấm ghế lẻ hoặc ép ghế liền kề khi chưa có business rule.
3. Khi chưa đăng nhập: đi đăng nhập và giữ ngữ cảnh phù hợp; quay lại phải kiểm tra sơ đồ mới vì ghế có thể đã bị chiếm.
4. Gửi đúng cấu trúc Proposed: `showtimeId`, `requestKey`, `items[]` gồm `seatId`, `ticketTypeId`. Không gửi giá, mức giảm, tổng tiền, status hay snapshot.
5. Dùng cùng `requestKey` và cùng nội dung cho retry của cùng một ý định đặt. Không sinh key mới sau timeout khi chưa biết request trước đã tạo booking hay chưa.
6. Cùng key/cùng nội dung trả booking cũ theo S08 (HTTP 200 khi replay); tạo mới trả 201. Phải xử lý trạng thái booking trả về hiện tại, kể cả đã expired/confirmed; không luôn coi replay là một lần giữ ghế mới.
7. Nếu khách đổi nội dung, đó là ý định mới; không sửa payload dưới key cũ. Quy tắc chuẩn hóa thứ tự ghế/fingerprint vẫn ở OQ-UC-05, frontend không tự băm theo một hợp đồng chưa chốt.
8. Thành công: lấy `bookingId`, `bookingCode`, `status`, `expiresAt`, `totalAmount`, `currency`, `details` từ response và chuyển KH-05. Countdown tính từ hạn server, không bắt đầu lại 5 phút ở trang mới.

#### Xung đột, timeout và đổi lựa chọn

| Tình huống | Phản hồi |
|---|---|
| `SEAT_ALREADY_TAKEN` | Thông báo ghế vừa bị giữ/bán; làm mới sơ đồ và nêu ghế xung đột nếu API cung cấp. Không báo các ghế còn lại đã được giữ: transaction từ chối toàn bộ. |
| `SHOWTIME_CLOSED` | Chặn tạo booking, thông báo suất không còn nhận đặt; cho về KH-03. |
| `REQUEST_KEY_CONFLICT` | Báo yêu cầu đặt bị xung đột nội dung; không âm thầm coi là thành công hoặc tạo nhiều key để thử liên tục. |
| Timeout / mất response POST | Kết quả chưa biết; cho khôi phục bằng cùng key/payload, không coi như chưa có đơn. Cơ chế tìm lại key sau reload còn DL-06. |
| Loại vé không hợp lệ | Hiển thị lỗi nghiệp vụ, yêu cầu chọn lại loại vé được backend cho phép; không tự bỏ giảm giá mà vẫn giữ tổng hiển thị cũ. |
| Back trước tạo booking | Có thể giữ lựa chọn cục bộ theo S09, nhưng tải lại suất/ghế và đối chiếu trước khi gửi. |
| Back sau tạo booking | Không tự hủy; không tự gia hạn hoặc sửa booking cũ. Muốn đổi ghế phải qua luồng hủy hợp lệ rồi đặt mới. |

#### Tiêu chí nghiệm thu

- **NT-09:** Chọn ghế không làm countdown chạy và không thể hiện quyền giữ trước response thành công; ghế held/sold/bảo trì không chọn được.
- **NT-10:** Couple được chọn nguyên đơn vị; một Couple tạo một item, hiển thị hai chỗ và hai vé dự kiến; không cho tách loại vé cho hai nửa.
- **NT-11:** Hai khách gửi cùng ghế: chỉ một booking giữ được; request chứa một ghế xung đột và một ghế trống không giữ một phần (AC-BOOK-002).
- **NT-12:** Timeout/retry cùng key không tạo đơn thứ hai, không reset hạn; payload khác dưới key cũ được báo xung đột.
- **NT-13:** Giá chốt chỉ lấy từ server; không gửi giá/giảm/trạng thái; thông tin liên hệ và báo giá trước đặt phải được chốt contract trước khi nghiệm thu tích hợp đầy đủ.

### 3.5. KH-05 — Thanh toán

#### Mục tiêu và điều kiện vào

Cho chủ booking xem số tiền đã chốt, bắt đầu thanh toán khi hợp lệ và theo dõi kết quả thực tế. Truy cập bằng định danh booking, không tạo booking mới khi mở trang. Căn cứ: FR-BOOK-003, FR-PAY-001..005, BR-BOOK-004/008, BR-PAY-001..008; S02 UC-02/03/04; S05 mục 31.

Khi vào trực tiếp, reload, quay lại từ cổng hoặc quay lại bằng Back/Forward, đọc lại booking từ backend. Không dùng query string “thành công”, local state hoặc đồng hồ trên thiết bị làm bằng chứng đã thanh toán.

#### Nội dung hiển thị

| Khối | Nội dung | Nguồn / giới hạn |
|---|---|---|
| Tóm tắt | Mã booking, phim, ngày/giờ, phòng, các ghế, loại ghế/vé | `GET /api/bookings/{bookingId}`; DTO hiện chưa mô tả đầy đủ ngữ cảnh phim/suất/phòng, xem DL-05. |
| Chi tiết giá | Giá chốt mỗi đơn vị ghế và tổng tiền VND | `details[].unitPrice`, `totalAmount`, `currency`; phân rã base/phụ thu/giảm còn thiếu DTO. Không lấy bảng giá hiện tại tính lại. |
| Hạn giữ | Countdown theo `expiresAt`, nhãn thời gian còn lại | Không khởi động lại khi reload hoặc chuyển trang. Khi bằng 0, xác minh trạng thái server trước khi hiển thị đã expired. |
| Thanh toán | Cổng/phương thức đang được hỗ trợ, trạng thái đang xử lý | OQ-01 chưa chọn cổng; ví dụ `provider: "vnpay"` trong DTO không phải quyết định tích hợp. Không tự dựng danh sách ví/ngân hàng. |
| Kết quả | Trạng thái booking và attempt đang theo dõi | DTO payment: `paymentId`, `bookingId`, `status`, `amount`, `currency`; `appliedAt`/`reconciliationStatus` chưa có trong response, xem DL-07. |
| Hành động | Thanh toán, kiểm tra lại, hủy booking/đổi ghế khi hợp lệ | Điều kiện theo bảng dưới; backend chốt cuối cùng. |

Một cột, tóm tắt phía trên form/thao tác theo S10. **Đề xuất UI:** tổng tiền và cảnh báo hết hạn đặt gần nút chính; trạng thái đang xác nhận nằm ngay trong nội dung trang, không chỉ là toast.

#### Luồng bắt đầu thanh toán

1. Tải booking, kiểm tra ownership và trạng thái. Nếu đã confirmed, mở KH-06; nếu expired/cancelled, hiện trạng thái kết thúc, không tạo attempt mới.
2. Chỉ bắt đầu khi booking pending còn hạn theo server và không có attempt `created`/`pending`/`unknown`. Nút “Thanh toán” disabled trong lúc gửi hoặc có attempt mở.
3. Gọi `POST /api/payments` với `bookingId`, `provider` theo cấu hình được chốt. Client không gửi `amount`, `currency`, mức giảm hoặc trạng thái để quyết định số tiền thu.
4. Response Proposed có `paymentId`, `status`, `amount`, `currency`, `payUrl`. Lưu liên hệ giữa attempt và booking trong ngữ cảnh UI; khi cần, điều hướng đến `payUrl` do backend cung cấp. Kiểu chuyển trang và return URL còn phụ thuộc cổng, không tự chốt popup hoặc form thu dữ liệu thẻ trong website.
5. Sau khi khách trở về hoặc khi kiểm tra kết quả: đọc `GET /api/payments/{paymentId}` và booking. Không gọi `POST /api/payments/callback` từ frontend để tự xác nhận giao dịch; webhook là tương tác của cổng với backend.
6. Chỉ chuyển kết quả thành công KH-06 khi backend xác nhận booking `confirmed`. `payment.status = succeeded` riêng lẻ chưa chứng minh payment đã được áp dụng hoặc vé đã phát hành.
7. Cách polling, chu kỳ, điều kiện dừng và return route chưa có contract đầy đủ. **Đề xuất UI:** có “Kiểm tra lại” gọi API đọc; không biến nó thành thao tác tạo attempt mới.

Không tự bổ sung `requestKey` cho API payment: DTO hiện chưa có field này. Backend có idempotency key của attempt theo FR-PAY-001; contract khôi phục attempt sau timeout/reload cần được chốt riêng ở DL-06.

#### Ma trận trạng thái và hành động

| Dữ liệu backend / tình huống | Hiển thị | Thanh toán mới / retry |
|---|---|---|
| Pending còn hạn, xác định không có attempt mở | “Đang chờ thanh toán”, countdown còn lại | Cho tạo attempt khi backend chấp nhận. |
| Attempt `created` | “Đang khởi tạo thanh toán” | Chặn tạo attempt song song. Không hiện nút bấm lại chỉ vì chưa có `payUrl`. |
| Attempt `pending` | “Thanh toán đang được xử lý” | Chặn attempt mới, tiếp tục kiểm tra trạng thái. |
| Attempt `unknown` / timeout khởi tạo | “Kết quả thanh toán đang được xác nhận” | Không tự kết luận thất bại, không tự retry thu tiền; đọc/khôi phục attempt hiện tại theo contract. |
| Attempt `failed` / `cancelled`, booking vẫn pending còn hạn, không còn attempt mở | Thông báo thất bại/hủy của lần thử, giữ thông tin đơn | Cho thử lại khi backend xác nhận điều kiện; không reset `expiresAt`. |
| Payment succeeded, booking chưa confirmed | “Đang kiểm tra kết quả đặt vé” hoặc thông báo cần kiểm tra khi server cung cấp bằng chứng phù hợp | Không phát QR, không báo đặt vé thành công, không mời trả tiền lần nữa chỉ vì chưa có vé. |
| Booking confirmed | Hiển thị xác nhận, mở vé | Không thanh toán lại. |
| Countdown = 0 nhưng booking vẫn pending | “Đã qua hạn giữ ghế; đang kiểm tra trạng thái đơn” | Chặn bắt đầu thanh toán mới; không tự đổi booking thành expired. |
| Pending quá hạn, attempt còn mở / cần đối soát | “Kết quả thanh toán đang được xác nhận” | Không tự giải phóng ghế hoặc mở lại lượt thanh toán. |
| Booking expired / cancelled | Hiện kết quả kết thúc của đơn; cho xem lịch/chọn lại qua booking mới | Không tái mở booking cũ, không nhận thanh toán cho đơn cũ. |

Countdown là hiển thị hạn danh nghĩa; worker chỉ expired/released khi thỏa điều kiện lifecycle. Grace period có chính sách đã chốt ở OQ-07 nhưng con số 2–3 phút còn đề xuất/cấu hình, không được hiện như lời hứa “chắc chắn có kết quả sau 2 phút”. Nếu cổng chưa cho kết quả xác đáng, ghế có thể tiếp tục held dưới quy trình giám sát; UI không tự công bố ghế đã được nhả.

Nguồn đang mâu thuẫn về áp dụng payment thành công trong grace period và điều kiện đủ thời gian bán sát giờ. UI đọc kết quả server; không tự quyết định confirm/expired dựa trên cách hiểu một nhánh tài liệu. Các điểm này được ghi tại CX-02/CX-03.

#### Hủy booking / hủy thanh toán để đổi ghế

| Điều kiện | Thao tác và kết quả mong đợi |
|---|---|
| Chủ booking, pending, chưa có payment áp dụng và không còn attempt mở | Hiện “Hủy booking”; xác nhận hủy, gửi `POST /api/bookings/{bookingId}/cancel`; chỉ báo thành công sau response và trạng thái cập nhật. |
| Có attempt created/pending/unknown | Theo FR-BOOK-003 và BR-BOOK-008, có luồng yêu cầu backend kiểm tra/hủy với cổng: cổng xác nhận chưa thu thì mới hủy payment, booking và nhả ghế. Không coi đóng cửa sổ cổng là hủy thành công. |
| Cổng báo đã thu / chưa rõ | Backend từ chối hủy; UI giữ thông tin booking/payment và hướng dẫn đợi kiểm tra, không điều hướng như đã được giải phóng ghế. |
| Confirmed / expired / cancelled hoặc không phải chủ | Không cho thực hiện hủy theo luồng Customer này. Backend vẫn kiểm tra nếu request bị gửi trực tiếp. |

**Đề xuất UI:** nhãn “Hủy thanh toán / Đổi ghế” với giải thích phải chờ hệ thống xác nhận, modal xác nhận và loading cho yêu cầu. Trong khi một request đang gửi, chặn nhấn lặp. Việc tích hợp luồng này vào endpoint `/cancel` hiện có, response chờ xử lý và mã lỗi chưa đủ contract (DL-07); không tự thêm endpoint cancel payment ở tài liệu màn hình.

Khi hủy thành công, cho trở về chọn ghế/lịch và tạo booking mới. Ghế được nhả không đồng nghĩa sẽ tiếp tục khả dụng cho khách cũ; người khác có thể giữ sau đó. Không hủy một phần hoặc sửa ghế trong booking cũ.

#### Lỗi và kết quả không chắc chắn

- `PAYMENT_ATTEMPT_OPEN`: đọc/khôi phục attempt hiện tại khi API hỗ trợ; không lặp POST để vượt ràng buộc. Điều kiện attempt mở gồm cả `created`, dù một số ví dụ lỗi chỉ nhắc pending/unknown.
- `BOOKING_EXPIRED`: tải lại booking, hiển thị hết hạn theo server; không tự đặt mọi payment của đơn thành failed.
- `BOOKING_ALREADY_CONFIRMED`: đọc lại booking và vé, không tạo thanh toán khác.
- `BOOKING_NOT_CANCELLABLE`: tải lại trạng thái; giải thích theo thông tin server đã lọc; không chuyển màn hình như hủy xong.
- GET payment lỗi mạng: đây là lỗi đọc trạng thái, không phải kết quả tài chính failed. POST payment timeout cũng không đủ chứng minh chưa tạo attempt.
- Payment thành công muộn trên booking expired/cancelled: không hiện vé giả, không chiếm lại ghế, không tự hứa hoàn tiền hoặc thời hạn hoàn. `refund_required` nếu sau này được API cung cấp chỉ biểu thị hồ sơ cần xử lý, không phải đã hoàn tiền.
- Tổng/tiền tệ payment khác booking trong response: thông báo không thể tiếp tục xác nhận và yêu cầu kiểm tra lại; không tự sửa giá đơn hoặc đánh dấu payment failed. Cách trả trạng thái an toàn cần contract backend.

Nếu không có `paymentId` sau reload và booking response không cho biết attempt đang mở, UI chưa thể khôi phục đầy đủ trạng thái thanh toán. Không thể kết luận “không có attempt” chỉ vì state trình duyệt trống. Đây là phụ thuộc DL-06, không phải chức năng đã hoàn thiện.

#### Tiêu chí nghiệm thu

- **NT-14:** Mã, ghế, giá chốt và countdown lấy từ booking server; reload/chuyển trang không gia hạn và không tạo booking mới.
- **NT-15:** Chặn attempt song song cho cả created/pending/unknown; timeout đọc/ghi không biến thành kết luận failed hoặc tự thu lần hai.
- **NT-16:** Chỉ booking confirmed dẫn tới xác nhận thành công; query string của cổng hoặc payment succeeded riêng lẻ không đủ.
- **NT-17:** Countdown về 0 không tự nhả ghế hoặc hủy payment; UI phân biệt quá hạn danh nghĩa, backend expired và kết quả đang đối soát.
- **NT-18:** Hủy/đổi ghế chỉ hoàn tất theo backend; khi cổng xác nhận chưa thu thì có thể hủy, khi đã thu/chưa rõ thì không báo đã nhả ghế (AC-BOOK-004..006).
- **NT-19:** Payment thành công sau booking expired/cancelled không tạo vé hoặc hứa hoàn tiền tự động; retry hợp lệ giữ nguyên giá và hạn booking.

### 3.6. KH-06 — Xác nhận đặt vé và vé điện tử

#### Mục tiêu và điều kiện hiển thị thành công

Xác nhận booking của khách đã `confirmed`, hiển thị các vé được backend phát hành. Căn cứ: FR-PAY-005, FR-TICKET-001, BR-BOOK-009, BR-PAY-004/007 và S02 UC-02. Cần đăng nhập và là chủ booking; biết mã công khai không đủ quyền xem.

Trang được mở sau KH-05, từ lịch sử hoặc bằng truy cập trực tiếp theo định danh hợp lệ. Mọi trường hợp phải tải `GET /api/bookings/{bookingId}`; không dựng vé từ danh sách ghế đã chọn hoặc từ kết quả redirect của cổng.

#### Bố cục và dữ liệu

Một cột theo S10: thông báo xác nhận → mã booking → thông tin phim/suất/phòng/ghế → các thẻ vé → điều hướng. Không tiếp tục chạy countdown giữ ghế trên booking đã confirmed.

| Khối | Nội dung | Nguồn / giới hạn |
|---|---|---|
| Kết quả | “Đặt vé thành công” khi booking confirmed | `status`, `confirmedAt` theo DTO booking Proposed. |
| Booking | `bookingCode`, tổng tiền, tiền tệ, thời điểm xác nhận | Không tự format lại mã thành cấu trúc `BK-ngày-số`. |
| Suất và ghế | Phim, ngày/giờ Việt Nam, phòng, ghế, loại ghế/vé | Nội dung lịch sử do backend cung cấp; DTO booking còn thiếu ngữ cảnh đầy đủ, DL-05. |
| Mỗi vé | Mã riêng, QR riêng, nhãn ghế, trạng thái vé | `tickets[]` hiện có `ticketCode`, `seatLabel`, `qrCodeUrl`, `status`. QR rendering/payload còn DL-09. |
| Hướng dẫn | Xuất trình mã vé/QR khi vào rạp, mỗi người dùng vé của mình | Không tự chốt cửa sổ soát vé; OQ-03 còn OPEN. |

Mỗi Standard/VIP sức chứa 1 có một vé; Couple sức chứa 2 có hai vé với mã độc lập. Hai vé Couple có thể cùng nhãn ghế nhưng phải là hai mã khác nhau. **Đề xuất UI:** gom theo ghế Couple và ghi “Vé 1/2”, “Vé 2/2” để phân biệt; không tạo hậu tố mới trong `ticketCode` hoặc tách thành hai seatId.

| Ticket status | Nhãn hiển thị | Hành vi |
|---|---|---|
| `valid` | “Chưa sử dụng” | Hiển thị mã/QR đã phát hành; không suy vé chắc chắn được chấp nhận ở mọi thời điểm vì cửa sổ soát còn OPEN. |
| `used` | “Đã sử dụng” | Giữ lịch sử, không hiện như vé mới chưa dùng. Thời điểm sử dụng chỉ hiện khi API cung cấp. |
| `voided` | “Đã vô hiệu hóa” | Không hiện như vé có thể dùng; lý do chỉ khi API cung cấp. Có status không đồng nghĩa Customer được hủy vé sau thanh toán. |

Đọc trạng thái vé không thay đổi vé thành used; Customer không gọi API validate dành cho nhân viên. Hai vé Couple được soát độc lập; một vé used không làm UI tự đổi vé còn lại thành used.

#### Trạng thái chưa đủ điều kiện và lỗi

- Booking pending: không hiển thị thông báo đặt thành công hoặc QR giả; cho trở về KH-05/kiểm tra trạng thái theo dữ liệu thực.
- Booking expired/cancelled: hiển thị kết quả tương ứng và hành động xem lịch/booking của tôi; không biến trang thành xác nhận thành công.
- Booking confirmed nhưng `tickets` rỗng/thiếu: báo chưa tải đủ dữ liệu vé và cho đọc lại. Theo nghiệp vụ, confirm và phát hành vé là một transaction; đây là lỗi dữ liệu/tích hợp cần kiểm tra, không phải trạng thái nghiệp vụ mới “confirmed chưa phát hành” đã được chốt.
- Ảnh QR không tải: vẫn hiện mã vé dạng chữ và thử tải lại QR; không tự tạo QR từ bookingCode hoặc ghép chung hai vé Couple. Payload/cách sinh QR cần DL-09.
- 401/403/404/lỗi mạng xử lý theo mục 2.4; không để lại dữ liệu vé của người khác trên màn hình khi đổi phiên.

Hành động chính theo S09: “Về trang chủ”, “Booking của tôi”. Không cam kết nút tải PDF, in vé, gửi email hoặc chia sẻ liên kết công khai vì chưa có contract/phạm vi tương ứng.

#### Tiêu chí nghiệm thu

- **NT-20:** Chỉ booking confirmed hiển thị thành công; tải trực tiếp vẫn kiểm tra quyền và đọc dữ liệu server.
- **NT-21:** Số vé theo tổng `capacity_units_snapshot`; Couple có hai mã/QR khác nhau, callback lặp và reload không tạo thẻ vé trùng. Dữ liệu capacity trong DTO cần DL-05.
- **NT-22:** Hiển thị đúng valid/used/voided; đọc vé không validate; lỗi QR không làm mất mã vé chữ hoặc biến bookingCode thành mã vé.

### 3.7. KH-07 — Đăng nhập / đăng ký

#### Mục tiêu và phạm vi

Khách tạo tài khoản Customer hoặc xác thực để tiếp tục hành trình. Đây là hai form trong một nhóm màn hình, không bắt buộc chung route. Căn cứ: FR-AUTH-001/002/004, BR-AUTH-002..004, S08 Auth và S09 navigation.

#### Form đăng nhập

| Trường / thành phần | Nội dung | Validation / giới hạn |
|---|---|---|
| Email | Nhãn, input email | Bắt buộc, định dạng email; quy tắc chuẩn hóa tài khoản do backend thực hiện thống nhất. |
| Mật khẩu | Input password | Bắt buộc; không áp quy tắc mật khẩu mới lên mật khẩu đăng nhập, không tự trim/đổi nội dung. |
| Nút chính | “Đăng nhập” | Loading khi gửi, chặn nhấn lặp. |
| Chuyển form | “Chưa có tài khoản? Đăng ký” | Giữ ngữ cảnh quay lại phù hợp; không mang mật khẩu sang URL. |

Gửi `POST /api/auth/login` với `email`, `password`. Response DTO Proposed có `token`, `user.id`, `user.email`, `user.fullName`. Không tự giả định có refresh token, roles, ảnh đại diện hoặc profile đầy đủ trong response. Thông tin sai/tài khoản locked/deactivated hiển thị thông báo chung, ví dụ “Không thể đăng nhập bằng thông tin đã cung cấp”; không tiết lộ tài khoản tồn tại hay mật khẩu đúng/sai riêng lẻ.

#### Form đăng ký

| Trường | Căn cứ | Validation / điểm còn thiếu |
|---|---|---|
| Họ tên | DTO `fullName`; schema `full_name` bắt buộc, tối đa 150 ký tự | Không rỗng sau trim; giới hạn API phải thống nhất với persistence. |
| Email | DTO `email`; schema tối đa 254 ký tự, unique chuẩn hóa | Bắt buộc, định dạng email; xử lý lỗi trùng theo contract. Không gộp dấu chấm/dấu cộng theo nhà cung cấp email. |
| Mật khẩu | DTO `password` | Bắt buộc; độ dài tối thiểu/tối đa và độ phức tạp chưa được định nghĩa, không tự ghi thành quy tắc 8 ký tự/ký tự đặc biệt. |
| Điện thoại | DTO `phone`; schema cho NULL, tối đa 20 ký tự | Không tự bắt buộc hoặc ép định dạng chỉ số Việt Nam khi chưa chốt validation API; DL-08. |
| Nhập lại mật khẩu | Không có trong DTO | **Đề xuất UI** nếu nhóm chọn dùng: kiểm tra khớp cục bộ, không tự thêm field request. |

Gửi `POST /api/auth/register` với các field theo contract được chốt. Client không gửi role; backend chỉ cấp CUSTOMER. Response đăng ký 201 hiện chỉ có thông tin user và `createdAt`, **không có token**.

**Chờ chốt:** S09 mô tả đăng ký thành công quay lại màn hình trước, nhưng DTO register chưa tạo phiên. **Đề xuất UI:** báo tạo tài khoản thành công rồi chuyển form đăng nhập, giữ đích quay lại; chỉ tiếp tục trang được bảo vệ sau khi đăng nhập thành công. Không tự coi đăng ký là đã xác thực, không tự thêm token vào response; xem CX-05.

#### Điều hướng, lỗi và trạng thái

1. Nếu đến từ hành động đặt vé/trang được bảo vệ, lưu đích cần quay lại theo S09; nếu vào trực tiếp, đăng nhập thành công về Home.
2. **Đề xuất UI:** chỉ chấp nhận đích nội bộ hợp lệ; sau đăng nhập kiểm tra lại quyền, booking, suất và ghế. Không tự gửi lại thao tác thanh toán hoặc đặt vé chưa được người dùng xác nhận.
3. Validation hiển thị cạnh field sau blur/gửi form theo S10. Backend vẫn kiểm tra mọi đầu vào; lỗi mạng khác lỗi thông tin xác thực.
4. Login 401 `INVALID_CREDENTIALS`: lỗi chung; lỗi phiên 401 của API được bảo vệ theo `UNAUTHENTICATED` ở S08, không nhầm hai ngữ cảnh.
5. Register 409 `EMAIL_ALREADY_EXISTS` theo DTO Proposed: thông báo phù hợp contract; không gắn thành công hoặc tiếp tục như đã đăng nhập.
6. Khi lỗi, giữ dữ liệu không nhạy cảm để sửa; không log/persist mật khẩu. Không khẳng định tài khoản được tạo hay chưa chỉ từ timeout; kiểm tra qua luồng đăng nhập/response phù hợp, không tự cấp phiên.

Quên mật khẩu, đổi mật khẩu, xác minh email, OTP, refresh token và ghi nhớ đăng nhập chưa có luồng API trong đặc tả hiện tại. Không thêm nút dẫn tới chức năng giả hoặc yêu cầu OTP như điều kiện đặt vé đã chốt. `email_verified_at` tồn tại trong schema không chứng minh đã có luồng xác minh email.

#### Tiêu chí nghiệm thu

- **NT-23:** Form gửi đúng field, không gửi role/giá hoặc dữ liệu không thuộc auth; đăng ký công khai không cấp role quản trị.
- **NT-24:** Sai thông tin/locked/deactivated dùng lỗi chung; validation đúng vị trí; không lộ hoặc lưu bền vững mật khẩu.
- **NT-25:** Đăng nhập từ trang được bảo vệ quay lại đúng ngữ cảnh và tải lại dữ liệu; đăng ký không bị hiểu là đã có phiên khi response chưa trả token.

### 3.8. KH-08 — Tài khoản / booking của tôi

#### Mục tiêu và truy cập

Khách xem các booking và vé của chính mình, tiếp tục thanh toán/hủy booking khi còn hợp lệ. Căn cứ: FR-BOOK-003/004, FR-TICKET-001, BR-AUTH-002 và S08 Bookings/Tickets. Mọi endpoint phải kiểm tra ownership ở backend; không lọc toàn bộ booking người dùng khác tại frontend rồi coi là an toàn.

#### Danh sách booking

Tải `GET /api/users/me/bookings?page=...&pageSize=...` khi vào trang hoặc đổi trang. DTO hiện ghi item tóm tắt theo chi tiết booking nhưng chưa mô tả rõ toàn bộ trường summary; DL-05. Không tự đặt query lọc theo trạng thái, ngày hoặc mã booking khi chưa có contract.

| Nội dung mỗi booking | Quy tắc |
|---|---|
| Mã booking | Mã server, dùng đúng định danh đã chốt để mở chi tiết. |
| Phim, suất, phòng | Dữ liệu backend cung cấp cho booking lịch sử; không thay bằng phim/phòng đang chọn ở phiên hiện tại. |
| Ghế và loại vé | Nội dung snapshot, vẫn giữ khi booking đã expired/cancelled. |
| Tổng tiền | Giá gốc của booking; không đổi về 0 vì detail đã released. |
| Trạng thái | Pending “Chờ thanh toán”; confirmed “Đã xác nhận”; expired “Đã hết hạn”; cancelled “Đã hủy”. Pending quá hạn danh nghĩa vẫn phản ánh đúng trạng thái server kèm giải thích nếu cần. |
| Thời điểm tạo/xác nhận | Chỉ hiển thị khi có dữ liệu; chuyển múi giờ đúng quy ước. |
| Hành động | Xem chi tiết; thanh toán/hủy/xem vé tùy trạng thái đã được xác minh. |

**Đề xuất UI:** danh sách thẻ dễ đọc trên mobile; desktop có thể dùng bảng hoặc thẻ. Sắp xếp/phân trang phải theo kết quả API, không khẳng định đã có server-side sort tùy chọn. Danh sách rỗng: “Bạn chưa có booking nào” và “Xem phim”. Lỗi tải có thử lại; 401 chuyển đăng nhập.

#### Chi tiết booking và hành động theo trạng thái

Chọn booking gọi `GET /api/bookings/{bookingId}` để đọc lại trạng thái. **Đề xuất UI:** tái sử dụng khối tóm tắt của KH-05 và thẻ vé của KH-06; không tạo thêm mô hình trạng thái độc lập.

| Booking | Nội dung / hành động |
|---|---|
| Pending còn hạn | Chi tiết đơn, giá, hạn giữ; “Tiếp tục thanh toán” mở KH-05. Nút hủy chỉ theo điều kiện backend và luồng mục 3.5. |
| Pending quá hạn hoặc đang xác nhận payment | Cho kiểm tra trạng thái; không tạo payment mới chỉ vì danh sách ghi pending, không tự coi là expired. |
| Confirmed | Xem vé và thông tin đã mua; không cho Customer đổi ghế, hủy sau thanh toán hoặc hoàn tiền trong demo. |
| Expired / cancelled | Giữ lịch sử ghế/giá/lý do nếu API cung cấp; có thể xem phim để tạo booking mới, không tái mở đơn cũ. |

Nếu cần đọc một vé riêng, dùng `GET /api/tickets/{ticketCode}` theo quyền. DTO vé riêng có mã, nhãn ghế, status, bookingCode, showtimeStartsAt nhưng chưa có QR/phòng đầy đủ; không giả định nó thay thế toàn bộ booking detail. Không cung cấp chức năng Customer validate vé.

Không hiển thị một danh sách payment attempt đầy đủ hoặc công cụ đối soát thủ công chỉ vì database có nhiều payment. API Customer chưa có contract liệt kê attempt theo booking; xem DL-06/DL-07. Tên chủ booking, dữ liệu tài khoản và vé riêng phải được xóa khỏi UI khi đăng xuất/đổi tài khoản.

#### Hồ sơ cá nhân — yêu cầu có, tích hợp chưa đủ

FR-AUTH-003 yêu cầu user được xem và sửa hồ sơ của mình; bảng kế hoạch có Trang Profile. Schema hiện có `full_name`, `email`, `phone`, nhưng S08 chưa có endpoint/DTO lấy hoặc cập nhật hồ sơ, quy tắc đổi email hay xử lý email trùng sau cập nhật.

Không loại bỏ yêu cầu này khỏi phạm vi nghiệp vụ; đồng thời không mô tả như chức năng đã tích hợp. **Đề xuất UI chờ contract:** vùng hồ sơ có thông tin được phép xem/sửa sau khi nhóm chốt field, quyền, validation và response. Không tự thêm ngày sinh, giới tính, địa chỉ, ảnh đại diện, đổi mật khẩu hoặc sửa email như đã được hỗ trợ.

#### Tiêu chí nghiệm thu

- **NT-26:** Endpoint me chỉ trả booking của phiên hiện tại; truy cập mã booking/vé người khác bị backend từ chối và UI không lộ dữ liệu (AC-SEC-002).
- **NT-27:** Empty/loading/error/phân trang đúng; chi tiết được tải lại, không mở quyền thanh toán/hủy chỉ từ bản summary cũ.
- **NT-28:** Giá/snapshot lịch sử không đổi khi danh mục thay đổi hoặc ghế released; vé used/voided không bị hiển thị thành valid.
- **NT-29:** Pending được xử lý theo hạn và attempt, confirmed không có hủy/refund Customer; hồ sơ cá nhân chỉ nghiệm thu tích hợp sau khi có contract DL-08.

## 4. Quy tắc xuyên màn hình

### 4.1. Luồng và điều kiện chuyển bước

| Từ → đến | Điều kiện | Không đạt điều kiện |
|---|---|---|
| KH-01 → KH-02 | Phim được chọn bằng đúng định danh | Hiện lỗi không tìm thấy hoặc lỗi tải, không tự chọn phim khác. |
| KH-02 → KH-03 | Giữ ngữ cảnh phim | KH-03 có thể rỗng; không giả có lịch. |
| KH-03 → KH-04 | Suất được backend cho bán | Làm mới lịch, yêu cầu chọn suất khác khi không còn khả dụng. |
| KH-04 → KH-07 → KH-04 | Chưa xác thực khi đặt | Giữ ngữ cảnh, kiểm tra lại ghế/suất sau đăng nhập, không tự giữ ghế lúc chuyển form. |
| KH-04 → KH-05 | Tạo booking thành công hoặc replay trả booking pending phù hợp | Xung đột từ chối toàn bộ; timeout khôi phục cùng key. Booking replay đã confirmed/expired/cancelled xử lý theo trạng thái thực. |
| KH-05 → KH-06 | Backend xác nhận booking confirmed và trả dữ liệu vé | Pending/unknown tiếp tục kiểm tra; expired/cancelled không hiển thị thành công. |
| KH-08 → KH-05 / KH-06 | Chủ booking, trạng thái hiện tại phù hợp | Tải lại chi tiết; không dùng summary cũ để cho phép mutation. |
| KH-05 → KH-04 khi đổi ghế | Booking cũ đã hủy hợp lệ nếu muốn thay nội dung đơn | Chưa xác nhận hủy thì giữ ngữ cảnh đơn cũ; không coi các ghế đã nhả. |

Theo S09, Back/Forward không tự hủy booking, không chặn lịch sử trình duyệt và không tự gửi lại form thanh toán. Rời luồng khi pending có cảnh báo về hạn giữ; đây không phải thao tác cancel server. Đăng nhập lại, reload hoặc mở nhiều tab không kéo dài `expiresAt` và không tạo quyền sở hữu ghế mới.

### 4.2. Tách biệt các nhóm trạng thái

| Nhóm | Miền giá trị từ nguồn | Điều không được suy ra |
|---|---|---|
| Booking | `pending`, `confirmed`, `expired`, `cancelled` | Pending không luôn đồng nghĩa còn thời gian bắt đầu payment; cancelled không có nghĩa đã refund. |
| Phân bổ detail | `held`, `sold`, `released` | Thời gian trôi qua không tự đổi held thành released; released không xóa lịch sử detail. |
| Payment | `created`, `pending`, `unknown`, `succeeded`, `failed`, `cancelled` | Không có status payment `expired`; unknown không phải failed; succeeded chưa chắc applied. |
| Ticket | `valid`, `used`, `voided` | Không dùng status booking để tự ghi đè ticket; một vé Couple used không làm vé còn lại used. |
| Vật lý ghế | `active`, `maintenance`, `inactive` | Active không đồng nghĩa trống trong mọi suất. |
| Suất | `draft`, `scheduled`, `in_progress`, `completed`, `cancelled` | Draft chưa bán; phim now_showing không làm mọi suất có thể đặt. |
| Đối soát payment | `none`, `needs_review`, `refund_required`, `resolved` | Là dữ liệu nội bộ theo schema; chưa có DTO Customer đầy đủ. Refund_required không có nghĩa tiền đã trả. |

Đường đi thông thường: tạo booking → pending/held; áp dụng payment hợp lệ → confirmed/sold và phát hành ticket; hết hạn hoặc hủy hợp lệ → expired/cancelled và released. Các cập nhật do backend thực hiện nguyên tử; UI không gửi status tùy ý.

### 4.3. Giá và snapshot

Công thức dưới đây được ghi ở S01 BR-BOOK-005, S05 mục 13.1 và S06 mục 3.14, để giải thích ý nghĩa dữ liệu. Đây là **công thức backend**, không phải hướng dẫn frontend tự tính báo giá:

```text
base_amount      = showtime.base_price × capacity_units_snapshot
surcharge_amount = phụ thu loại ghế, tính một lần / detail
discount_amount  = round(base_amount × discount_percent_snapshot / 100, 0)
unit_price       = base_amount + surcharge_amount - discount_amount
total_amount     = tổng unit_price của toàn bộ detail gốc
payment.amount   = booking.total_amount
```

- Giảm chỉ trên giá cơ sở, không giảm phụ thu; VND làm tròn nguyên đồng theo nguồn. Giá cuối của một Couple là giá cả đơn vị hai chỗ, không phải giá mỗi người để UI nhân thêm lần nữa.
- Sau tạo booking, tên/loại ghế/loại vé và giá snapshot không bị thay theo danh mục hiện tại. Retry payment dùng cùng số tiền, không báo giá lại đơn cũ.
- OQ-06 **đã chốt Option B**: một detail được phép giá 0, tổng booking và payment phải dương; không có luồng booking tổng 0/xác nhận không thanh toán trong MVP.
- Không có coupon, điểm thành viên, phí đặt vé, thuế bổ sung hay bảo hiểm được tài liệu nguồn chốt cho checkout này.

**Ví dụ minh họa số học, không phải báo giá API hay kết quả test:** với giá cơ sở 80.000 VND, Adult giảm 0%, phụ thu VIP 15.000 VND và Couple 40.000 VND như các giá trị mẫu trong seed: một Standard = 80.000 VND, một VIP = 95.000 VND, một Couple = 200.000 VND. Một Standard + một Couple là hai detail, ba chỗ, tổng 280.000 VND và ba vé khi booking được xác nhận. Ví dụ không đặt ra giới hạn giá hoặc phụ thu cố định cho ứng dụng.

### 4.4. Dữ liệu demo và giới hạn sử dụng

Script seed mô tả một cinema, hai phòng, ba loại ghế Standard/VIP/Couple, ba loại vé Adult/Student/Child và sáu phim đều có status now_showing. Vòng lặp ghế mô tả 50 vị trí phòng 1, 32 vị trí phòng 2; trong đó tổng cộng 9 vị trí Couple. Suy từ vòng lặp và capacity_units: **82 vị trí, 91 chỗ/người**, không phải 82 người. Đây là phân tích script, chưa phải kết quả truy vấn database đã chạy.

Lịch demo trong script dùng ngày 28/10–04/11/2026 theo Việt Nam, giá cơ sở 80.000–110.000 VND. Không hard-code dải ngày, giá, số hàng ghế, số phim hoặc tên rạp vào component nghiệp vụ. Nhóm sắp chiếu có thể rỗng với dữ liệu này; không tự bịa phim để lấp giao diện.

Seed có Student giảm 15%, Child giảm 25% và ghi chú điều kiện, nhưng OQ-08 chưa chốt đầy đủ cách xác minh; không coi dữ liệu mẫu là chính sách tuổi/giấy tờ chính thức. Nhãn PG/PG-13/R của seed hiển thị nguyên bản, không tự chuyển thành hệ phân loại Việt Nam.

Script không tạo user/booking demo. Không giả có tài khoản đăng nhập mẫu hoặc vé mẫu đã được phát hành. Không cam kết gửi email, hoàn tiền hoặc bán sát giờ chỉ từ việc thấy cột/bản ghi mẫu.

## 5. Phụ thuộc dữ liệu và contract cần bổ sung

Các hàng dưới đây là khoảng trống đối chiếu giữa nhu cầu màn hình và tài liệu hiện có. Đây là **đề nghị làm rõ contract**, không phải chấp thuận thêm endpoint, field hoặc migration. Toàn bộ endpoint/DTO Proposed vẫn cần review trước tích hợp.

| Mã | Màn hình | Hiện có và phần thiếu | Nội dung cần nhóm xác nhận |
|---|---|---|---|
| DL-01 | KH-01 | Movies có status/page/pageSize; chưa có query từ khóa, genre hoặc sort | Có hỗ trợ tìm kiếm/lọc thể loại trong MVP không; tham số, kết quả, pagination và trạng thái rỗng. Không mặc định tìm kiếm trên một trang = toàn kho. |
| DL-02 | KH-03/04 | Có API lịch theo date và endpoint chi tiết suất; chưa có DTO chi tiết suất, metadata dải ngày hoặc thông tin cinema trong item lịch | DTO ngữ cảnh phim/phòng/rạp, nghĩa `date` theo múi giờ nào, ngày mặc định/dải ngày, ý nghĩa `bookingAvailable` và dữ liệu trạng thái bán. |
| DL-03 | KH-04 | Ghế DTO chỉ có available/held/sold, loại/capacity/phụ thu; schema có trạng thái vật lý riêng; không có tọa độ/lối đi/hướng màn hình | Cách trả ghế bảo trì/inactive, phân biệt held của booking đang xem nếu cần, ánh xạ sơ đồ vật lý. Không tự suy chủ giữ hoặc lối đi từ thứ tự số ghế. |
| DL-04 | KH-04 | Có endpoint ticket-types nhưng chưa có DTO; chưa có contract báo giá lựa chọn trước tạo booking | ID/tên/mã/điều kiện loại vé, eligibility, cách trả giá từng ghế và tổng tạm tính từ server. Không tự thêm endpoint preview ở tài liệu này. |
| DL-05 | KH-04/05/06/08 | Booking DTO thiếu liên hệ đầu vào, ngữ cảnh phim/suất/phòng, capacity/detail ID và phân rã snapshot giá; summary lịch sử chưa cụ thể | Nguồn liên hệ và thời điểm chốt; trường xem lại lịch sử, số vé theo detail, dữ liệu reason/timestamp và cách định danh booking. Không đọc DB trực tiếp từ frontend để bù thiếu. |
| DL-06 | KH-04/05/08 | Request booking có requestKey; payment đọc theo paymentId; booking DTO chưa chỉ attempt hiện tại, chưa có API tìm booking theo requestKey | Khôi phục POST timeout, reload/tab mới, payment đang mở, paymentId/payUrl và idempotency tạo attempt. Không kết luận không có đơn/attempt từ state client trống. |
| DL-07 | KH-05/08 | Có cancel booking và status payment đơn giản; thiếu contract hủy attempt qua cổng, kết quả bất đồng bộ, applied/reconciliation | `/cancel` xử lý yêu cầu đổi ghế thế nào; khi nào đang xử lý/từ chối/thành công; field/cờ cần thiết và mã lỗi đã chốt; dữ liệu Customer được phép xem. |
| DL-08 | KH-07/08 | Login/register Proposed; register chưa có token, password policy/phone validation chưa rõ; FR-AUTH-003 chưa có endpoint | Phiên sau đăng ký, validation API, dữ liệu hồ sơ được sửa, email trùng/đổi email, auth state và logout. Không tự thêm refresh token hoặc nơi lưu token. |
| DL-09 | KH-06/08 | Booking DTO có qrCodeUrl; DTO vé riêng chưa có QR; schema không lưu ảnh QR và nguồn còn nêu payload mã vé hoặc payload có chữ ký | Cách cung cấp/render QR, payload chính thức, authorization và lỗi tải ảnh; không tự chọn payload hoặc tạo endpoint ảnh công khai. |

Ngoài field dữ liệu, cần thống nhất response envelope: API contract mục 3 mô tả `{ data, pagination? }`, trong khi nhiều ví dụ DTO auth/booking/payment viết trực tiếp object. Frontend không tự hỗ trợ hai cấu trúc như thể đã chốt; xem CX-12.

## 6. Điểm chưa đồng bộ và quyết định cần review

### 6.1. Đối chiếu nguồn và ảnh hưởng

Bảng ghi cả mâu thuẫn thực sự và khác biệt mức độ chi tiết/tổ chức. Chỉ các yêu cầu đã chốt có căn cứ mới được dùng như baseline nghiệp vụ; các khác biệt chưa đủ bằng chứng không được tự chuyển thành quyết định chính thức.

| Mã | Bằng chứng trong nguồn | Ảnh hưởng và cách ghi nhận tại đặc tả này |
|---|---|---|
| CX-01 | S09 user flow mục 3/4 mô tả quá expires_at thì expired/released; UI state mục 5 cũng viết quá hạn là mở khóa. S01 BR-BOOK-004, OQ-07 và S07 mục 4 yêu cầu xét attempt đang mở. | Phân biệt countdown bằng 0 với expired/released. Dùng quy tắc hai phần đã chốt; không tự nhả ghế trên client. Các tài liệu UI phụ thuộc cần được đồng bộ trong tác vụ riêng. |
| CX-02 | S01 mục 3.1 và S03 mục 0 không áp cutoff cố định 10/15/30 phút, cho bán sát giờ; seed gán booking_closes_at = starts_at − 10 phút. S02 UC-01 còn yêu cầu created_at + 5 phút không vượt mốc đóng bán/giờ bắt đầu; S05 mục 12 có đoạn lấy mốc sớm hơn. | Có cả khác biệt dữ liệu demo với chính sách và khác biệt cách xác định hạn khi bán sát giờ. Không tự đặt cutoff mới hoặc rút/ngầm kéo dài countdown. Backend trả khả năng bán và hạn; cần chốt thống nhất trước kiểm thử sát giờ, theo OQ-02. |
| CX-03 | S02 UC-02 bước 5 yêu cầu chưa quá expires_at, nhưng ngoại lệ A9 nói payment thành công trong grace period được áp dụng bình thường; UC-03 cũng chuyển một số succeeded/pending quá hạn sang kiểm tra áp dụng. S04 mục 14 và S05 mục 31.5 có nhánh đã thu sau hạn thì không áp dụng. | Chưa thống nhất điều kiện applied trong grace period khi booking vẫn pending/held. Không viết cả hai cách thành cùng một chính sách; UI chỉ báo confirmed theo server, không tự confirm từ payment succeeded. Nhóm cần quyết định để chốt backend/test. |
| CX-04 | S01 FR-SHOW-001 chỉ hiển thị suất scheduled còn bán; UI cũ yêu cầu suất đóng/hủy có ô disabled. | Không bắt API công khai trả suất đóng/hủy ngoài yêu cầu. Disabled áp dụng khi dữ liệu hiện có cho biết không thể chọn; không tự dựng danh sách suất ẩn. |
| CX-05 | S09 mapping mục 3.7 gộp đăng nhập/đăng ký thành response token + user; DTO register mục 2.1 chỉ có user, login mục 2.2 mới có token. | Đăng ký không tự tạo phiên. Đề xuất chuyển đăng nhập và giữ đích quay lại, chờ contract xác thực sau register. |
| CX-06 | S07 dùng PK bigint, booking_code/ticket_code UUID; S08 DTO dùng chuỗi “uuid” cho nhiều ID và ví dụ bookingCode/ticketCode dạng BK-/TK-. | Kiểu ID/public identifier và dạng mã chưa đồng bộ. UI coi định danh là giá trị opaque từ API; không parse tiền tố/ngày/số hoặc tự chuyển bigint/UUID. Không tự thêm cột public ID vào schema. |
| CX-07 | S01 FR-BOOK-003/BR-BOOK-008 và S05 mục 31.5 có luồng Cancel/Query cổng để đổi ghế; một số đoạn S05 mục 12, S03 UC-03A Alternative Flow và kiểm tra cũ ở FR-BOOK-003 vẫn nói có attempt mở thì luôn từ chối hủy. S10 UI state chặn hủy lúc xử lý. | Ghi nhận luồng đổi ghế trong yêu cầu đã cập nhật: chỉ thành công khi cổng xác nhận chưa thu; không coi đây là được phép ép hủy mọi attempt. Contract và trạng thái UI bất đồng bộ còn DL-07. |
| CX-08 | S04 mục 4 nêu MovieDetail kèm showtimes, Checkout, PaymentResult, MyTickets/MyBookings; S09–S10 tách Movie Detail, Showtime, Payment, Booking Confirmation. Ví dụ ở PROJECT_STRUCTURE còn đặt Booking Confirmation trước Payment. | Đây là khác biệt tổ chức và một ví dụ thứ tự chưa đồng bộ. Giữ 8 nhóm màn hình của bộ UI hiện có, vé sau confirm. Không chốt số route/component hoặc thêm màn hình xác nhận trước thanh toán như yêu cầu mới. |
| CX-09 | FR-MOVIE-001 cấm archived trong danh sách, nhưng S08 chưa nói đầy đủ chính sách GET chi tiết phim ended/archived hoặc sắp chiếu có suất mở bán. | Không suy truy cập trực tiếp luôn 404 hoặc tự cấm đặt chỉ theo movie.status. Backend cần chốt tính hiển thị/khả dụng; UI xử lý dữ liệu và lỗi được trả. |
| CX-10 | Vision PA1 phần Product Perspective/AD-05/PF-07 và kế hoạch PA1 Construction/Sprint 4 mô tả simulated payment. S01 mục 3.1, FR-PAY và S04 mục 8 mô tả Payment Gateway bên ngoài, OQ-01 chưa chọn cổng. | Phạm vi thanh toán demo giữa tài liệu PA và tài liệu nghiệp vụ chưa đồng nhất. Phần màn hình mô tả luồng an toàn theo S01, không khẳng định đã duyệt tích hợp cổng thật hoặc tên cổng cụ thể. Nếu chọn simulator, cần nhóm duyệt và đồng bộ nguồn/API; không thêm nút “giả lập thành công” cho Customer trong đặc tả này. |
| CX-11 | S05 mục 14 còn câu BookingDetail 1–0..1 Ticket và mục 32.1 còn nói FK unique tới detail; S01 BR-BOOK-009, S06 mục 3.15 và DDL ticket không còn unique booking_detail_id, quy định capacity_units vé/detail. | Giữ quyết định mới đã ghi rõ: Couple một detail/hai vé; service phát hành idempotent. Các đoạn 1–0..1/unique cũ cần đồng bộ, không dùng để gộp hai vé Couple thành một QR. |
| CX-12 | S08 API contract mục 3 quy định envelope data/pagination, còn ví dụ DTO auth/booking/payment thường chỉ viết object. | Đây là điểm trình bày chưa rõ ở ví dụ, chưa đủ để kết luận hai cấu trúc đều hợp lệ. Contract tích hợp cần chỉ rõ wrapper cho từng endpoint; UI không hard-code hai dạng hoặc bỏ envelope tùy ý. |

Một số đoạn S05 mục 34–36 và S06 mục 9.3 còn ghi cửa sổ đối soát OPEN/ngoài phạm vi, trong khi S01 OQ-07 đã ghi RESOLVED về **chính sách** grace period và S02 UC-04 phân biệt truy vấn/ghi nhận kết quả trong phạm vi với thực thi refund ngoài demo. Không coi toàn bộ đối soát là ngoài phạm vi và không coi refund đã được triển khai. Tại tài liệu này, truy vấn kết quả và bảo toàn payment là yêu cầu, thực thi hoàn tiền vẫn ngoài demo.

Việc demo không có workflow tự xử lý khi rạp hủy suất không phải quyết định rằng khách mất quyền lợi. OQ-05 còn OPEN; UI không tự cam kết hoàn tiền/đổi vé, cũng không tự tuyên bố khách không được hoàn trong mọi trường hợp rạp hủy.

### 6.2. Giữ đúng trạng thái các OQ liên quan

| Câu hỏi / quyết định | Trạng thái theo nguồn | Tác động tới màn hình |
|---|---|---|
| OQ-01 — cổng thanh toán | OPEN | Phương thức, payUrl/return, callback, Query/Cancel và thông báo lỗi cổng chưa thể chốt chi tiết. |
| OQ-02 — mốc đóng bán | OPEN; không cutoff cố định 10/15/30 phút là baseline | KH-03/04/05 không tự đặt cutoff, cần xử lý CX-02. |
| OQ-03 — cửa sổ soát vé | OPEN | Không tự ghi đến trước 30 phút hoặc hết hiệu lực sau một số phút cụ thể. |
| OQ-04 — quyền quản trị / needs_review | OPEN, Manager còn Proposed | Customer chỉ xem dữ liệu của mình, không có công cụ xử lý đối soát hay quản trị. |
| OQ-05 — quyền lợi khi rạp hủy suất | OPEN | Không khẳng định chính sách hoàn/đổi vé đã chốt. |
| OQ-06 — detail giá 0 / booking miễn phí | RESOLVED — Option B | Detail có thể 0, tổng booking/payment dương; không cần xin chốt lại hoặc tự mở luồng booking 0 đồng. |
| OQ-07 — chính sách grace period | RESOLVED về chính sách | Không expired/released chỉ từ countdown. Con số cụ thể chưa chốt, CX-03 vẫn cần làm rõ điều kiện applied. |
| OQ-08 — eligibility vé giảm và chi tiết UX Couple | OPEN về điều kiện/cách xác minh | Couple một đơn vị/hai vé là phần đã chốt; không mở lại quyết định này. Student/Child chưa có quy trình eligibility hoàn chỉnh. |
| OQ-09 — hiệu năng/khả dụng | TBD | Không tự đặt SLA 2 giây/99,9% hoặc tuyên bố đã đạt mục tiêu. |
| OQ-UC-02/03/04 — leo thang/retry/grace cụ thể | Chưa chốt giá trị | Không hard-code thông điệp chắc chắn hết chờ sau 120 giây hoặc tần suất polling/worker. |
| OQ-UC-05 — fingerprint và thứ tự ghế | Proposed | Giữ requestKey cho retry cùng ý định; chuẩn hóa nội dung theo contract cuối cùng. |
| OQ-UC-06/07 — chứng cứ xác minh/ngữ cảnh soát | Proposed | Thuộc backend/staff; không thêm thao tác xác minh chữ ký hoặc validate vào màn hình Customer. |

Giới hạn số booking pending/ghế held và rate limiting tại S04 mục 12a là **Proposed**, chưa được duyệt. Không biến N = 2 booking, M = 6 ghế hoặc 10 request/phút thành validation chính thức trên màn hình.

## 7. Truy vết nghiệm thu và phạm vi kiểm chứng

### 7.1. Ma trận kiểm thử đề nghị khi triển khai

Các kịch bản dưới đây là **đầu vào kiểm thử, chưa được chạy**. Những ca cần backend/database/cổng phải kiểm tra tích hợp trên môi trường phù hợp; mock UI không chứng minh transaction, ownership hoặc idempotency server.

| Nhóm tiêu chí | Kịch bản đại diện | Nguồn yêu cầu / kết quả cần kiểm chứng |
|---|---|---|
| NT-01..05 | Danh sách hai nhóm, phân trang, phim trùng tên, thiếu poster, movie 404/lỗi mạng | FR-MOVIE-001/002; không archived trong danh sách, đúng định danh, không tạo dữ liệu thiếu. |
| NT-06..08 | Đổi ngày nhanh, chuyển ngày UTC/VN, suất vừa đóng/hủy, không có suất | FR-SHOW-001, BR-ST-003; đúng ngày, không response cũ ghi đè, không nhận đặt suất không bán. |
| NT-09/10 | Available/held/sold, bảo trì/inactive, chọn Couple và Standard | FR-SHOW-002, BR-BOOK-003/009/010; selected chưa held, đúng đơn vị ghế/số chỗ/loại vé. |
| NT-11 | Hai khách cùng ghế; một request chứa ghế trùng và ghế trống | AC-BOOK-002, BR-BOOK-002/006; một bên giữ, bên xung đột không giữ một phần. |
| NT-12 | Mất response tạo booking rồi retry cùng key; cùng key đổi payload; replay đơn expired/confirmed | S02 UC-01 A1/A2/A6, API contract mục 7; không đơn thứ hai, không gia hạn, báo conflict đúng. |
| NT-13/14/28 | Giá danh mục đổi sau tạo, mixed ticket type, Couple, detail giá 0 và tổng booking 0 | BR-BOOK-005, OQ-06; server chốt giá, giữ snapshot, chặn tổng 0. Contract báo giá/liên hệ cần bổ sung trước. |
| NT-14/17 | Reload/tab khác, countdown về 0, worker chậm, booking quá hạn có attempt mở | BR-BOOK-004, AC-BOOK-003; UI không nhả ghế hoặc tự reset, phân biệt pending quá hạn và expired. |
| NT-15/19 | Tạo attempt hai lần, timeout khởi tạo/GET, unknown, failed/cancelled còn hạn | BR-PAY-002/003/008, AC-PAY-002; không thu trùng do retry mù, không giả failed, retry đúng điều kiện. |
| NT-16/19 | Query redirect tự ghi success, payment succeeded chưa applied, late success sau expired | FR-PAY-005, BR-PAY-004/007, AC-PAY-004; không vé giả hoặc reclaim ghế. CX-03 cần chốt cho grace period. |
| NT-18 | Hủy chưa có attempt; cổng xác nhận chưa thu; cổng đã thu/chưa rõ; race với confirm | AC-BOOK-004..006; chỉ báo hủy khi backend chốt, không giải phóng ghế sai. |
| NT-20..22 | Confirmed có Standard/VIP/Couple, callback lặp, QR lỗi, một vé Couple đã used | BR-BOOK-009, AC-PAY-003, FR-TICKET-001; đúng số/mã vé, không tạo thêm, trạng thái riêng từng vé. |
| NT-23..25 | Form sai, email trùng, locked/deactivated, register không token, login quay lại | FR-AUTH-001/002/004, BR-AUTH-003/004; lỗi đúng, không cấp role/phiên sai. |
| NT-26/27/29 | Me bookings phân trang/rỗng, chi tiết người khác, URL vé người khác, đổi tài khoản | FR-BOOK-004, BR-AUTH-002, AC-SEC-002; backend từ chối và UI không giữ dữ liệu riêng tư cũ. |
| Xuyên màn hình | Desktop/mobile, sơ đồ cuộn trong khối, bottom bar không che ghế, focus/form lỗi | NFR-USE-001/002, S10; kiểm tra 320/768/1024/1440px; hướng dẫn accessibility bổ sung là đề xuất UI. |

### 7.2. Điều kiện để nghiệm thu triển khai

1. Review các màn hình và đề xuất UI, chốt contract DL-01..09 theo phạm vi MVP thực tế. Không đánh dấu một màn hình tích hợp hoàn tất khi dữ liệu bắt buộc chưa được API định nghĩa.
2. Chốt các mâu thuẫn ảnh hưởng tiền/ghế, nhất là CX-02/CX-03/CX-07/CX-10; cập nhật tài liệu nguồn theo quy trình nhóm trước khi dùng làm cơ sở test cuối cùng.
3. Triển khai endpoint, authorization, transaction, worker và tích hợp thanh toán theo quyết định đã được duyệt; UI không thay trách nhiệm backend.
4. Chạy các ca phù hợp mục 7.1; ghi riêng đã chạy/thành công/thất bại/chưa chạy và môi trường kiểm chứng. Không dùng danh sách tiêu chí trong tài liệu như bằng chứng test đã pass.
5. Kiểm tra liên kết, tên trạng thái, field DTO, tham chiếu FR/BR/OQ và tính nhất quán với các tài liệu UI phụ thuộc khi có thay đổi được duyệt.

**Giới hạn của bản đặc tả này:** được viết từ file và tài liệu trong repository, gồm đối chiếu nội dung PA/kế hoạch; không có xác minh database đang chạy, gateway đang kết nối, giao diện trình duyệt hoặc ca kiểm thử runtime. Những nhãn trạng thái, endpoint và dữ liệu Proposed phải được chốt trước khi triển khai thực tế; tài liệu không tự đóng các câu hỏi mở của dự án.
