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
