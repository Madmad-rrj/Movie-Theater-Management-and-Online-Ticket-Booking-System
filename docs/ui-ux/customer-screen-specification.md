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
