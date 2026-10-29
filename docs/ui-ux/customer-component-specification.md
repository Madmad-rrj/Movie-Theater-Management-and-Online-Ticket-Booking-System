# Đặc tả component dùng chung của Customer — CIN-SUB-063

## 1. Mục đích và quy ước

Định nghĩa các khối UI tái sử dụng, mục đích, dữ liệu đầu vào, trạng thái và màn hình sử dụng. Đây là **đặc tả thiết kế**, không khẳng định component/code đã tồn tại. Tên thuộc tính trong bảng là đề xuất giao diện component, không phải bổ sung field vào API.

Component hiển thị dữ liệu và phát sự kiện thao tác; trang/service điều phối API và kiểm tra kết quả. Giá, trạng thái nghiệp vụ và quyền lấy từ backend; không đặt logic giữ ghế/áp dụng payment trong component. Mã KH-01..12 theo [đặc tả màn hình](customer-screen-specification.md).

## 2. Danh mục và màn hình sử dụng

| Mã | Component | Mục đích / thuộc tính chính đề xuất | Trạng thái | Màn hình dùng |
|---|---|---|---|---|
| CP-01 | Button | Hành động; label, variant, disabled, loading, onAction | Mặc định, hover, focus, disabled, loading | Tất cả |
| CP-02 | Input | Nhập text/email/password; label, value, required, readOnly, error, onChange | Trống, đang nhập, focus, lỗi, chỉ đọc, disabled | KH-07, KH-12, liên hệ nếu chốt |
| CP-03 | Select | Chọn giá trị có nguồn hợp lệ; label, options, selectedId, error, onSelect | Loading, rỗng, chọn, lỗi, disabled | KH-03, KH-09, form phù hợp |
| CP-04 | Form | Nhóm field và gửi một thao tác; fields, submitState, fieldErrors, formError, onSubmit | Nhập, validation lỗi, submitting, thành công, lỗi/chưa rõ | KH-07, KH-12; liên hệ nếu chốt |
| CP-05 | Movie Card | Poster, title, ageRating, status, duration nếu có, onOpen | Loading, có dữ liệu, ảnh lỗi, focus | KH-01; tóm tắt phim khi cần |
| CP-06 | Seat | Một đơn vị ghế; id, label, type, capacity, availability, physicalState khi có, selected | Khả dụng, selected, held, sold, bảo trì/inactive, disabled | KH-04 |
| CP-07 | Seat Map | Nhóm rows/seats, chú thích, lựa chọn, onToggleSeat, loadState | Loading, rỗng, có dữ liệu, lỗi, cập nhật sau xung đột | KH-04 |
| CP-08 | Ticket Type Row | Loại vé cho một đơn vị ghế; seat, options, ticketTypeId, eligibilityNote, onSelect | Chưa chọn, chọn, lỗi điều kiện, disabled | KH-09, vùng KH-04 |
| CP-09 | Booking Summary | Mã, phim/suất/phòng/ghế/loại vé, giá chốt, currency, mode | Loading, đủ dữ liệu, thiếu/lỗi, chỉ đọc | KH-05, KH-06, KH-08, KH-10 |
| CP-10 | Countdown | Hạn giữ; expiresAt và thông tin thời gian đáng tin cậy nếu có | Chưa có booking, còn thời gian, 0:00 | KH-05, chi tiết KH-08 pending; KH-06 trước tạo chưa chạy |
| CP-11 | QR Ticket Card | ticketCode, seatLabel, QR được cung cấp, status, ngữ cảnh vé | Loading, valid, used, voided, QR lỗi | KH-11, chi tiết KH-08 |
| CP-12 | Booking Table / List | Danh sách đơn; rows, pagination, onOpen, actions theo điều kiện | Loading, rỗng, lỗi, có dữ liệu | KH-08 |
| CP-13 | Pagination | page/pageSize/totalItems/totalPages, onChange | Trang đầu/giữa/cuối, loading, disabled | KH-01, KH-08; KH-11 nếu có contract |
| CP-14 | Modal | Xác nhận hoặc thông báo tập trung; title, content, actions, open, onClose | Đóng, mở, đang xử lý, lỗi | Hủy/đổi ghế, lỗi chặn, bản nháp |
| CP-15 | Dropdown | Menu tài khoản hoặc danh sách lựa chọn; trigger, items, open | Mở/đóng, focus, disabled item | Header; KH-03 nếu chọn kiểu này |
| CP-16 | Drawer | Điều hướng mobile; title, items, accountArea, open, onClose | Mở/đóng, focus trong lớp, cuộn nội dung | Header dùng chung |
| CP-17 | Tabs | Nhóm phim/ngày hoặc vùng tài khoản; items, activeId, onChange | Được chọn, chưa chọn, disabled, loading vùng dữ liệu | KH-01, KH-03, tài khoản nếu dùng |
| CP-18 | Loading Indicator | Spinner/skeleton; scope, accessibleLabel | Đang tải, kết thúc | Tất cả khối đọc dữ liệu và nút gửi |
| CP-19 | Empty State | Dữ liệu rỗng đã xác nhận; title, description, action nếu có | Rỗng, hành động phù hợp | KH-01/03/08/11, danh mục chưa có |
| CP-20 | Toast | Phản hồi ngắn; kind, message, dismiss | Hiện/đóng, success/error/info/warning | Thao tác không cần chặn toàn trang |
| CP-21 | Inline Feedback | Thông báo quan trọng bền trong trang; kind, message, actions | Đang xác nhận, lỗi, cảnh báo, thành công | KH-04/05/06/10/12, form |

Table và List là hai cách trình bày cùng dữ liệu booking, không có hai hệ thống trạng thái riêng. Drawer không chứa lại một bản sao form đăng nhập hoặc toàn bộ lịch sử; Dropdown và Tabs chỉ điều hướng/chọn theo vai trò của chúng.

## 3. Quy tắc hành vi theo component

### 3.1. Button, Input, Select và Form

- Mỗi khối có một hành động chính rõ; hủy được phân biệt với tiếp tục/thanh toán. Disabled phải có lý do đọc được ở ngữ cảnh phù hợp.
- Loading giữ nhãn/diện tích nút ổn định và chặn kích hoạt lặp bằng chuột/bàn phím. Đây không thay thế idempotency/backend.
- Input có nhãn, lỗi gắn đúng field, value không mất khi lỗi validation. Không tự trim mật khẩu hoặc log giá trị nhạy cảm.
- Select chỉ phát ID trong nguồn options hợp lệ; không tự gán ID Adult khi danh mục chưa tải. Loading/rỗng/lỗi nguồn options không bị gộp thành “đã chọn mặc định”.
- Form gom validation input và lỗi server; fieldErrors khác formError. Gửi một lần theo điều kiện; thất bại giữ dữ liệu không nhạy cảm để sửa.
- Timeout POST có thể chưa rõ kết quả: form không tự gửi lặp như thao tác đọc. Cách khôi phục theo API và từng màn hình.
- Profile chỉ có field đã được chốt cho sửa; không dùng form đăng ký làm contract cập nhật. Chức năng chưa có contract giữ TBD.

### 3.2. Movie Card và phản hồi dữ liệu

Movie Card nhận dữ liệu từ trang; không tự gọi API cho từng card. Không dùng title làm định danh. Poster lỗi có vùng thay thế, không lấy poster phim khác; badge không che tên/nút. Trạng thái archived không được component tự chuyển thành now_showing để hiển thị.

Loading Indicator, Empty State và Inline Feedback có phạm vi khối hoặc trang trong main; giữ header/footer. Empty chỉ dùng sau dữ liệu thành công rỗng, không dùng cho 401/403/404 hoặc mạng lỗi. Toast tự đóng không phải nơi duy nhất chứa kết quả payment đang xác nhận.

### 3.3. Seat, Seat Map và Ticket Type Row

- Seat chỉ là một đơn vị bán. Couple rộng hơn và capacity 2, nhưng phát đúng một sự kiện chọn seatId; không phát hai item trùng ghế.
- Selected là lựa chọn phiên, không phải held server. Held/sold/bảo trì/inactive không cho chọn; field tình trạng vật lý chưa có phải được phối hợp API, không suy từ thiếu phân bổ.
- Seat Map hiển thị theo hàng/nhãn dữ liệu, không tự dựng lối đi, số cột hoặc hàng Couple. Mobile cuộn trong khối, vùng bấm tối thiểu 44px.
- Sau lỗi ghế xung đột, trang tải lại sơ đồ và thông báo toàn yêu cầu bị từ chối; component không giữ phần ghế còn lại như đã thành công.
- Ticket Type Row chỉ gán một loại vé mỗi đơn vị, không có bộ đếm quantity độc lập với ghế. Couple bản đầu Adult; eligibility giảm giá còn TBD.
- Đổi/bỏ ghế trước tạo cập nhật lựa chọn; sau tạo đơn không sửa snapshot thông qua component.

### 3.4. Booking Summary và Countdown

Booking Summary có thể dùng chế độ lựa chọn trước tạo hoặc dữ liệu booking đã chốt, nhưng nhãn phải phân biệt rõ. Trước tạo chưa có contract báo giá thì không tự tính giá cuối; sau tạo chỉ hiện giá/snapshot server. Không cộng phí hoặc nhân lại giá Couple đã tính cho hai chỗ.

Countdown chưa xuất hiện như thời gian giữ khi chỉ có selection. Với booking pending đã có, hiển thị theo expiresAt; reload không reset. Khi 0:00, chỉ phát tín hiệu để trang đọc lại trạng thái một lần/phù hợp cơ chế đã chốt; không tự hủy booking, giải phóng ghế hoặc đổi payment failed. Đồng bộ thời gian server và polling còn cần contract, không cho client gia hạn tùy ý.

### 3.5. QR Ticket Card và danh sách booking

- Một ticketCode là một thẻ vé; Couple hai thẻ/mã/QR độc lập. Cập nhật một vé used không ghi đè vé còn lại.
- valid/used/voided có nhãn chữ; QR lỗi vẫn giữ mã chữ, không tự tạo QR từ bookingCode.
- Không có API validate trong component Customer; xem vé không đổi trạng thái.
- Booking List/Table nhận dữ liệu của phiên đã kiểm tra quyền. Row action dựa vào điều kiện hiện tại do trang xác minh, không chỉ summary cũ.
- Pagination dùng metadata server, không tự tính tổng theo số item trang đang có. Chưa có pagination My Tickets thì không giả có đủ toàn bộ vé.

### 3.6. Modal, Dropdown, Drawer và Tabs

Modal chặn tương tác nền, chỉ một modal tại một thời điểm. Đóng bằng X/Escape/nút phụ là đóng lớp UI, không tự xác nhận hủy nghiệp vụ. Khi request đang xử lý, hành vi đóng phải rõ và không báo hoàn tất mutation chỉ vì lớp đóng.

Dropdown đóng khi chọn/click ngoài theo thiết kế; Drawer mobile có nút đóng và cuộn nội dung khi dài. **Đề xuất UI:** quản lý focus, trả focus về trigger, khóa cuộn nền khi lớp phủ chặn, và khôi phục khi đóng/đổi kích thước.

Tabs thể hiện active bằng dấu hiệu ngoài màu; đổi tab cập nhật vùng dữ liệu và loading, không trộn response cũ. Đổi ngày/suất không tự hủy booking.

## 4. Định dạng và giới hạn trách nhiệm

Tiền VND nguyên đồng, timestamp hiển thị Asia/Ho_Chi_Minh; tên/mã giữ dữ liệu server. Nội dung quan trọng không bị cắt, lớp phủ không che focus/thanh đặt; responsive theo [đặc tả responsive](customer-responsive-specification.md) và [layout](customer-web-layout.md).

Không tự thêm refresh token, WebSocket, hệ thống state nặng, API báo giá hay validate payment trong component. Thuộc tính UI mới ở bảng là đề xuất, cần ánh xạ bằng contract thật khi triển khai.

## 5. Tiêu chí nghiệm thu component

- **CP-AC-01:** Đủ component được yêu cầu (Movie Card, Button, Modal, Form, Table/List, Seat, Pagination, Toast) và bảng màn hình sử dụng, không trùng hệ trạng thái.
- **CP-AC-02:** Dữ liệu/props, sự kiện và trách nhiệm hiển thị tách khỏi quyền/giá/transaction backend.
- **CP-AC-03:** Loading/rỗng/lỗi/disabled/selected có hành vi rõ, không phát mutation trùng hoặc thành công giả.
- **CP-AC-04:** Couple, countdown, QR, pagination và ownership đúng quy tắc từng màn hình.
- **CP-AC-05:** Nhãn/focus/lỗi đọc được, không dựa màu duy nhất; lớp phủ và responsive không làm mất hành động.
- **CP-AC-06:** Tên thuộc tính UI không được mô tả là field API mới; thiếu dữ liệu và đề xuất vẫn được ghi rõ.
