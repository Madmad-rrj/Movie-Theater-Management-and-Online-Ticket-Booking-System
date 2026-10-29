# Customer Screen Specification

## 1. Mục đích

Đặc tả chức năng và nội dung của từng màn hình Customer. Tài liệu trả lời câu hỏi: mỗi màn hình có gì?

## 2. Danh sách màn hình Customer — CIN-SUB-048

### 2.1. Phạm vi và cách định danh

Màn hình dưới đây là đơn vị chức năng để thiết kế và kiểm thử; không tự xác định URL hoặc bắt buộc tạo một route cho mỗi mục. Ticket Type có thể là bước/vùng trong chọn ghế; Payment Result có thể là chế độ xem của trang thanh toán. Cách gộp trang là **đề xuất UI**, không làm thay đổi quyền truy cập và nghiệp vụ.

API/DTO được nêu trong bộ tài liệu hiện còn ở mức Proposed. Đặc tả màn hình không đồng nghĩa chức năng đã triển khai. Những dữ liệu hoặc thao tác chưa có hợp đồng được ghi **TBD**, không tự thêm endpoint.

### 2.2. Nhóm màn hình và mục đích

| Nhóm | Mã | Màn hình | Mục đích | Quyền truy cập | Màn hình liên quan |
|---|---|---|---|---|---|
| Khám phá phim | KH-01 | Home / danh sách phim | Duyệt đang chiếu, sắp chiếu; chọn phim | Công khai | KH-02, KH-07, KH-08 |
| Khám phá phim | KH-02 | Movie Detail | Xem thông tin và mở lịch của đúng phim | Công khai | KH-01, KH-03 |
| Khám phá phim | KH-03 | Showtime | Chọn ngày và suất còn được bán | Công khai | KH-02, KH-04 |
| Đặt vé | KH-04 | Seat Selection | Chọn đơn vị ghế cho một suất | Xem sơ đồ công khai theo endpoint dự kiến; tạo booking cần đăng nhập | KH-03, KH-09, KH-07, KH-05 |
| Đặt vé | KH-09 | Ticket Type | Gán một loại vé hợp lệ cho mỗi đơn vị ghế | Theo ngữ cảnh KH-04; gửi đặt cần đăng nhập | KH-04, vùng kiểm tra đơn, KH-05 |
| Đặt vé | KH-06 | Booking Confirmation | Kiểm tra thông tin đơn và phân biệt đơn chờ với kết quả đã xác nhận | Chủ booking đã đăng nhập khi đã có đơn | KH-04, KH-09, KH-05, KH-10, KH-11 |
| Thanh toán | KH-05 | Payment | Bắt đầu thanh toán đúng đơn và số tiền chốt | Chủ booking đã đăng nhập | KH-06, KH-10, KH-08 |
| Thanh toán | KH-10 | Payment Result | Xem kết quả payment và việc booking đã được xác nhận hay chưa | Chủ booking đã đăng nhập | KH-05, KH-06, KH-11, KH-08 |
| Xác thực | KH-07 | Đăng nhập / đăng ký | Tạo tài khoản Customer hoặc phiên xác thực | Công khai; đích quay lại theo ngữ cảnh | Các trang được bảo vệ, KH-01 |
| Tài khoản | KH-08 | Booking History / chi tiết booking | Tra cứu mọi đơn của chính mình, kể cả pending/expired/cancelled | Customer đã đăng nhập, kiểm tra ownership | KH-05, KH-06, KH-10, KH-11, KH-12 |
| Tài khoản | KH-11 | My Tickets / chi tiết vé | Tra cứu vé đã được phát hành của chính mình | Chủ booking đã đăng nhập | KH-06, KH-08, KH-10 |
| Tài khoản | KH-12 | Profile | Xem và cập nhật hồ sơ trong phạm vi được cho phép | User đã đăng nhập, chỉ hồ sơ của mình | KH-07, KH-08 |

### 2.3. Ranh giới giữa các màn hình

- Home dùng danh sách phim và phân nhóm; không tạo thêm màn Movie List trùng chức năng nếu không có nhu cầu giao diện riêng.
- Seat Selection chọn ghế; Ticket Type gán loại vé cho từng ghế. Cả hai dùng chung lựa chọn trong phiên, không tạo hai booking hoặc hai đồng hồ giữ ghế.
- **Booking Confirmation có hai ngữ cảnh cần gọi tên rõ:** vùng kiểm tra đơn trước thanh toán và phần xác nhận sau khi backend trả booking `confirmed`. Vùng kiểm tra trước thanh toán có thể nằm trong KH-05; đây là đề xuất tổ chức UI, không phải thao tác tự đổi booking sang confirmed.
- User Flow hiện mô tả Booking Confirmation sau payment. Khi thiết kế vùng kiểm tra trước thanh toán theo phân công, giữ nguyên điều kiện xác nhận nghiệp vụ; cần thống nhất vị trí bước với người phụ trách User Flow/Navigation, không tự sửa luồng đã chốt.
- Payment thao tác tạo/theo dõi attempt; Payment Result hiển thị kết quả. Payment `succeeded` không tự chứng minh booking `confirmed` hoặc vé đã phát hành.
- Booking History liệt kê đơn ở mọi trạng thái; My Tickets chỉ trình bày vé đã phát hành, không biến đơn pending thành vé. Không tự thêm API danh sách vé chưa được chốt.
- Profile là chức năng hồ sơ, không gộp với lịch sử đơn; endpoint, trường được sửa và validation hồ sơ còn TBD. Không mặc định có đổi email/mật khẩu.
- Đăng nhập và đăng ký là hai form trong cùng nhóm KH-07. Response register chưa có token; chỉ tiếp tục trang được bảo vệ khi đã có phiên hợp lệ.

### 2.4. Quy tắc truy cập và điều hướng chung

1. Public: Home, Movie Detail, Showtime; sơ đồ ghế có API đọc công khai. Không yêu cầu đăng nhập chỉ để xem phim/lịch.
2. Tạo booking, thanh toán và đọc đơn/vé/hồ sơ cá nhân cần phiên hợp lệ. Chưa đăng nhập thì lưu đích nội bộ và chuyển KH-07; quay lại phải tải lại dữ liệu.
3. Backend kiểm tra role và ownership trên mọi tài nguyên riêng tư. Biết UUID, mã booking hoặc ticketCode không tạo quyền xem.
4. Back/Forward và đổi màn hình không tự gửi lại POST, hủy đơn, giải phóng ghế hoặc gia hạn `expiresAt`.
5. Chỉ chạy countdown sau khi backend tạo booking thành công; chọn ghế trong phiên chưa giữ ghế. Countdown về 0 chỉ yêu cầu kiểm tra trạng thái, không tự xác nhận expired/released.
6. Chỉ một attempt `created`/`pending`/`unknown` được mở trên mỗi booking. Không tạo attempt mới vì chưa có kết quả từ cổng hoặc vì chuyển trang kết quả.
7. Header có logo, nhóm phim và tài khoản theo phiên. Menu mới cho My Tickets/Profile là đề xuất điều hướng cần phối hợp; quyền truy cập vẫn áp dụng dù mở trực tiếp.

### 2.5. Đối chiếu phạm vi đặc tả

| Nhu cầu | Màn hình chịu trách nhiệm | Căn cứ / sub-task |
|---|---|---|
| Duyệt phim, xem lịch và sơ đồ | KH-01..04 | FR-MOVIE-001/002, FR-SHOW-001/002 |
| Giữ ghế, loại vé và kiểm tra đơn | KH-04, KH-09, KH-06 | FR-BOOK-001/002; CIN-SUB-055/056/057 |
| Thanh toán và kết quả độc lập với booking | KH-05, KH-10 | FR-PAY-001..005; CIN-SUB-058/059 |
| Vé của tôi | KH-11, KH-06 | FR-TICKET-001; CIN-SUB-060 |
| Lịch sử và chi tiết đơn | KH-08 | FR-BOOK-004; CIN-SUB-061 |
| Xác thực và hồ sơ | KH-07, KH-12 | FR-AUTH-001..004; CIN-SUB-062 |

### 2.6. Tiêu chí nghiệm thu danh mục

- Đủ 12 mã chức năng không trùng; mỗi mục có mục đích, quyền và màn hình liên quan.
- Phân biệt rõ Ticket Type, Payment Result, My Tickets, Booking History và Profile; không cần endpoint/route mới chỉ để tách mục thiết kế.
- Có điểm kiểm tra đơn trước thanh toán nhưng không báo đã thanh toán/confirmed trước kết quả backend.
- Các giới hạn nghiệp vụ/API còn thiếu được giữ TBD; danh mục là cơ sở cho đặc tả chi tiết, không phải báo cáo kiểm thử runtime.

## 3. Đặc tả từng màn hình

### 3.1. Home

- **Mục đích:** giới thiệu phim đang chiếu / sắp chiếu, điểm vào luồng đặt vé.
- **Bố cục:** header, khu vực banner/danh sách phim dạng card, footer.
- **Dữ liệu hiển thị:** poster, tên phim, nhãn phân loại tuổi, trạng thái (`now_showing`/`coming_soon`).
- **Nút thao tác:** xem chi tiết phim (click card), tìm kiếm/lọc.
- **Validation:** không có.
- **Tiêu chí nghiệm thu:** danh sách phim tải từ API, hiển thị loading/empty state đúng.

### 3.2. Movie Detail

- **Mục đích:** cung cấp thông tin chi tiết phim và điểm chuyển sang chọn suất chiếu.
- **Bố cục:** poster + thông tin phim, nút "Chọn suất chiếu".
- **Dữ liệu hiển thị:** poster, tên phim, thời lượng, mô tả, ngôn ngữ, phân loại tuổi, trạng thái.
- **Nút thao tác:** "Chọn suất chiếu", quay lại.
- **Validation:** phim không tồn tại → error state.
- **Tiêu chí nghiệm thu:** đủ dữ liệu từ API movie; điều hướng sang Showtime đúng phim.

### 3.3. Showtime

- **Mục đích:** hiển thị lịch chiếu của phim để khách chọn suất.
- **Bố cục:** nhóm theo ngày → danh sách suất (giờ bắt đầu, phòng, giá cơ sở).
- **Dữ liệu hiển thị:** `starts_at`, tên phòng, giá cơ sở, trạng thái còn/đóng bán.
- **Nút thao tác:** chọn suất → Seat Selection.
- **Validation:** suất đã đóng bán hoặc đã hủy không thể chọn (disabled).
- **Tiêu chí nghiệm thu:** suất bị vô hiệu có trạng thái disabled; chọn suất chuyển đúng phòng/suất.

### 3.4. Seat Selection

**KH-04 — CIN-SUB-055.** Chọn đơn vị ghế cho một suất; chọn cục bộ chưa giữ ghế. Xem sơ đồ theo API public dự kiến; tạo booking cần Customer đã đăng nhập.

#### Dữ liệu và vị trí ghế

| Khối | Dữ liệu cần hiển thị | Nguồn / giới hạn |
|---|---|---|
| Ngữ cảnh suất | Phim, ngày/giờ, phòng, khả năng bán | Endpoint chi tiết suất được đề xuất nhưng DTO ngữ cảnh đầy đủ còn TBD. Không suy rạp từ tên phòng. |
| Sơ đồ | `rows[].rowLabel`, `seats[].id`, `seatNumber`, `seatTypeCode`, `capacityUnits` | `GET /api/showtimes/{showtimeId}/seats` theo tài liệu API Proposed. |
| Phân bổ | `available`, `held`, `sold` | Dữ liệu theo từng suất, không thay bằng trạng thái ghế vật lý. |
| Tình trạng vật lý | Hoạt động, bảo trì, ngừng hoạt động | Cần API biểu diễn độc lập; DTO hiện chưa có field đầy đủ — TBD phối hợp API. |
| Loại vé / giá | Loại vé cho từng đơn vị ghế, phụ thu, giá server nếu có | Chuyển vùng KH-09; chưa có contract báo giá tổng trước booking. |

Hiển thị nhãn hàng và số ghế theo dữ liệu; không hard-code số hàng, lối đi hoặc vị trí Couple từ seed. Nhãn hướng màn hình và bố trí lối đi cần dữ liệu/thiết kế đã thống nhất. Couple là một đơn vị hai chỗ, không có hai nút cho hai nửa ghế.

#### Thao tác và giới hạn lựa chọn

| Thao tác | Điều kiện | Kết quả / phản hồi |
|---|---|---|
| Chọn ghế | Suất còn bán; ghế hoạt động và available | Đánh dấu selected trong phiên, thêm một đơn vị ghế vào tóm tắt. Không POST giữ riêng từng ghế. |
| Bỏ chọn | Ghế đang selected, chưa tạo booking | Bỏ lựa chọn và loại vé gắn với ghế đó. Không tự hủy booking đã có. |
| Chọn Couple | Cả đơn vị available | Một seatId, hai chỗ, dự kiến hai vé khi booking được xác nhận; không gửi hai item trùng seatId. |
| Chọn loại vé | Đã chọn ghế | Mở/tập trung KH-09, giữ cùng showtimeId và lựa chọn trong phiên. |
| Đặt vé | Phiên hợp lệ, ít nhất một ghế, mỗi ghế có loại vé hợp lệ | Gửi một yêu cầu booking với requestKey; loading và chặn nhấn lặp. Backend kiểm tra lại toàn bộ. |
| Quay lại / đổi suất | Không có mutation đang gửi | Giữ ngữ cảnh cần thiết; khi đổi suất phải xác nhận/chọn lại ghế của suất mới, không mang seatId cũ sang phòng khác. |

Không cho chọn ghế held/sold/maintenance/inactive. Ghế được chọn phải khác nhau, thuộc phòng của cùng suất. Chưa có giới hạn tối đa ghế/booking đã chốt; không tự áp 6/8/10 vé, quy tắc ghế lẻ hoặc ép ngồi liền nhau. Số đơn vị ghế khác số chỗ: một Standard và một Couple là hai ghế, ba chỗ.

#### Tạo booking và chuyển bước

1. Chưa đăng nhập thì chuyển KH-07 và giữ đích quay lại; sau đăng nhập tải lại suất/ghế, không tự gửi POST đặt vé.
2. Gửi `POST /api/bookings` với `showtimeId`, `requestKey`, `items` gồm `seatId`, `ticketTypeId`. Không gửi giá, mức giảm, status hoặc snapshot.
3. Tạo mới thành công nhận booking pending, details, giá chốt và `expiresAt`; chuyển vùng kiểm tra đơn/KH-05. Countdown bắt đầu từ hạn server, không từ lúc mở sơ đồ.
4. Retry cùng ý định dùng cùng key và nội dung. Replay trả booking cũ ở trạng thái hiện tại; confirmed thì xem kết quả/vé, expired/cancelled không tái mở.
5. Timeout POST chưa chứng minh không có đơn. Giữ thông tin yêu cầu để khôi phục cùng key; cách khôi phục sau reload khi mất key còn cần contract, không tạo key mới mù.
6. Sau khi đã tạo booking, ghế/loại vé/giá đã chốt không sửa trực tiếp. Đổi ghế phải qua hủy hợp lệ rồi tạo đơn mới.

#### Loading, rỗng, lỗi và xung đột

- Loading sơ đồ: không dựng ghế chọn được từ dữ liệu chưa tải; giữ ngữ cảnh suất.
- Sơ đồ rỗng hoặc không có ghế khả dụng: thông báo phù hợp, cho về lịch; không đồng nhất lỗi tải với hết ghế.
- `SEAT_ALREADY_TAKEN`: yêu cầu bị từ chối toàn bộ, làm mới sơ đồ và nêu ghế xung đột nếu API cung cấp; không báo các ghế còn lại đã held.
- `SHOWTIME_CLOSED`: khóa đặt và cho chọn suất khác; `REQUEST_KEY_CONFLICT`: báo nội dung yêu cầu không khớp, không đổi payload dưới key cũ.
- Lỗi mạng/401/403/404: phân biệt lỗi tải, cần xác thực, không có quyền và không tìm thấy; không tự báo suất hủy chỉ từ 404.
- Countdown bằng 0 không tự nhả ghế; UI đọc trạng thái server. Hết hạn thực tế do backend xử lý theo điều kiện booking/payment.

#### Nghiệm thu Seat Selection

- **SS-01:** Đúng hàng/ghế/loại; ghế không khả dụng không chọn được; trường vật lý chưa có được ghi TBD.
- **SS-02:** Selected chưa held; Couple nguyên đơn vị; số ghế và số chỗ tách rõ.
- **SS-03:** Hai khách cùng ghế chỉ một request giữ được; lựa chọn có ghế xung đột không giữ một phần.
- **SS-04:** Retry cùng key không tạo đơn/hạn mới; timeout không tự phát sinh booking khác.
- **SS-05:** Thành công dùng giá/hạn server, không gửi giá hoặc tự sửa booking cũ.
- **SS-06:** Back/Forward và countdown không tự hủy, giải phóng hoặc gia hạn; responsive theo layout.

### 3.5. Payment

**KH-05 — CIN-SUB-058.** Chủ booking đã đăng nhập thực hiện thanh toán cho đơn pending còn hợp lệ. Trang không tạo booking mới và không thu tiền chỉ vì được mở/reload.

#### Nội dung và dữ liệu

| Khối | Nội dung | Giới hạn |
|---|---|---|
| Kiểm tra đơn | Mã, phim/rạp/suất/phòng, ghế, loại vé, số chỗ | Vùng kiểm tra KH-06; các trường ngữ cảnh thiếu trong DTO phải được chốt, không lấy từ đơn khác. |
| Giá chốt | Giá từng detail, tổng tiền, currency VND | `totalAmount`, `details[].unitPrice` từ booking; không sửa, không thêm phí hoặc tính lại theo giá mới. |
| Hạn giữ | `expiresAt`, countdown thời gian còn lại | Không reset khi retry/chuyển trang; 0:00 không tự đổi booking thành expired. |
| Phương thức | Cổng/phương thức đang thực sự hỗ trợ | **Nhà cung cấp TBD**; ví dụ provider trong DTO không phải quyết định chọn VNPay hoặc cổng khác. |
| Attempt | Định danh, trạng thái và số tiền được phép xem | API payment Proposed; khôi phục attempt mở sau reload nếu thiếu paymentId/field còn TBD. |
| Thông báo / hành động | Thanh toán, kiểm tra lại, quay lại hoặc yêu cầu hủy hợp lệ | Theo trạng thái được xác minh, không luôn bật nút thử lại. |

#### Bắt đầu và quay lại từ cổng

1. Đọc `GET /api/bookings/{bookingId}` theo ownership. Confirmed thì xem kết quả/vé; expired/cancelled thì không bắt đầu payment.
2. Chỉ mở thanh toán khi pending còn hạn và không còn attempt created/pending/unknown. Nút loading khi gửi; client chặn nhấn lặp, backend vẫn bảo vệ điều kiện/idempotency.
3. Gửi `POST /api/payments` với bookingId và provider đã được cấu hình/chốt. Client không gửi giá/status/snapshot để quyết định số tiền thu. Idempotency attempt do backend thực hiện; field request bổ sung còn cần contract.
4. Response dự kiến có paymentId, status, amount, currency, payUrl. Chỉ điều hướng bằng dữ liệu backend hợp lệ; popup, return URL và chi tiết cổng còn TBD.
5. Đóng trang cổng, bấm Back hoặc nhận query “success” không phải bằng chứng đã hủy/thu tiền. Khi trở về, đọc payment và booking từ server rồi mở KH-10.
6. “Kiểm tra lại” chỉ gọi request đọc trạng thái, không POST tạo attempt mới. Chu kỳ polling, điều kiện dừng và nút tiếp tục payUrl cũ phải được chốt theo contract, không hard-code thời gian chờ.
7. Không gọi webhook `/api/payments/callback` từ frontend để tự áp dụng kết quả. Cổng và backend chịu trách nhiệm xác minh.

#### Trạng thái và chống gửi lặp

| Booking / payment | Phản hồi UI | Hành động tiếp theo |
|---|---|---|
| Pending còn hạn, không attempt mở | Sẵn sàng thanh toán | Cho bắt đầu khi backend chấp nhận. |
| Attempt created | Đang khởi tạo | Chặn attempt song song, không bấm lại chỉ vì chưa có payUrl. |
| Attempt pending | Đang xử lý | Theo dõi attempt hiện tại, không tạo mới. |
| Attempt unknown / timeout chưa rõ | Đang xác nhận kết quả | Không báo failed hoặc tự retry thu tiền; khôi phục/đọc lại kết quả. |
| Failed/cancelled đã xác minh | Hiển thị kết quả của lần thử | Chỉ cho retry khi booking vẫn pending còn hạn và không có attempt mở. |
| Succeeded, booking chưa confirmed | Tiền/kết quả đặt vé cần tiếp tục kiểm tra theo dữ liệu server | Không phát vé, không mời thanh toán lần nữa chỉ vì chưa thấy vé. |
| Booking confirmed | Đã xác nhận đơn | KH-10/KH-06/My Tickets, không thanh toán lại. |
| Quá hạn danh nghĩa nhưng còn pending | Đã qua hạn giữ, đang kiểm tra trạng thái | Chặn bắt đầu attempt mới, không tự expired/released. |
| Booking expired/cancelled | Đơn không còn thanh toán được | Lịch sử hoặc đặt đơn mới. |

Tải lại trang khi state client trống không chứng minh không có attempt. Nếu API chưa cho khôi phục attempt hiện tại, ghi hạn chế TBD và giữ trạng thái chưa xác định; không giải quyết bằng POST mới. Callback lặp hoặc đến sai thứ tự không được làm UI tự phát thêm vé/hạ kết quả đã xác nhận.

#### Quay lại, hủy và lỗi

- Back không tự cancel server hoặc gia hạn. Trở lại KH-06 đọc snapshot hiện tại; quay về chọn ghế không cho sửa trực tiếp đơn cũ.
- Hủy khi chủ booking pending chưa có payment áp dụng và không có attempt mở theo backend. Nếu attempt còn mở, backend phải kiểm tra/hủy với cổng; chỉ xác nhận chưa thu mới hoàn tất hủy. Đã thu/chưa rõ thì từ chối, giữ phân bổ.
- `PAYMENT_ATTEMPT_OPEN`: đọc/khôi phục attempt cũ; `BOOKING_ALREADY_CONFIRMED`: đọc vé; `BOOKING_EXPIRED`: đọc lại đơn và chặn thanh toán mới.
- Lỗi GET trạng thái là lỗi đọc dữ liệu, không phải payment failed. Timeout POST không đủ chứng minh request chưa tạo attempt.
- 401 chuyển đăng nhập rồi đọc lại trạng thái; 403 không lộ đơn người khác; 404 không tự khởi tạo đơn/payment thay thế.
- Sai tổng tiền/currency trong response cần kiểm tra dữ liệu; không sửa giá booking hoặc tự chốt failed. Payment thành công muộn trên đơn expired/cancelled không tự reclaim ghế, phát vé hoặc hứa hoàn tiền.

#### Nghiệm thu Payment

- **PA-01:** Dùng đúng booking và số tiền/hạn server, reload không tạo đơn/attempt hoặc reset hạn.
- **PA-02:** Created/pending/unknown chặn attempt mới; retry chỉ khi lần cũ đã có kết quả chắc chắn và booking còn hợp lệ.
- **PA-03:** Kết quả redirect/đóng cổng không xác nhận thanh toán hoặc hủy; đọc server trước điều hướng.
- **PA-04:** Countdown về 0 và lỗi mạng không tự đổi booking/payment/ghế.
- **PA-05:** Hủy/đổi ghế chỉ hoàn tất sau backend; không hủy confirmed hoặc một phần.
- **PA-06:** Nhà cung cấp, polling/return và khôi phục attempt được ghi TBD khi chưa có contract; không tự chọn cổng.

### 3.6. Booking Confirmation

**KH-06 — CIN-SUB-057.** Phân biệt **kiểm tra đơn trước thanh toán** và **booking đã được backend xác nhận**. Hai ngữ cảnh không tạo hai loại booking hoặc quyền sửa giá. Cách tách khối kiểm tra đơn thành một bước riêng là đề xuất UI cần thống nhất với User Flow/Navigation.

#### Dữ liệu xác nhận

| Dữ liệu | Yêu cầu |
|---|---|
| Phim, rạp, suất | Đúng phim, rạp/phòng, ngày/giờ Việt Nam của đơn; trường chưa có trong DTO booking đầy đủ phải bổ sung contract, không ghép từ một phim đang xem khác. |
| Ghế và loại vé | Nhãn ghế, loại ghế, loại vé, số chỗ mỗi đơn vị; sau tạo dùng dữ liệu đã chốt của booking. |
| Giá | Giá mỗi detail, tổng tiền, VND từ server; không tính lại theo danh mục hiện tại. |
| Mã và hạn | `bookingId`, `bookingCode`, `status`, `expiresAt` khi đã tạo; mã hiển thị nguyên giá trị, không tự chế tiền tố. |
| Liên hệ | Thông tin liên hệ được phép xem; nguồn lấy/field và thời điểm xác nhận còn TBD trong contract tạo booking. |
| Vé sau xác nhận | `tickets` theo booking, mỗi vé có mã/ghế/QR/trạng thái được server trả. Không dựng vé từ lựa chọn cục bộ. |

#### Ngữ cảnh A — kiểm tra đơn trước thanh toán

Đề xuất đặt vùng kiểm tra trong KH-05 sau khi KH-04/KH-09 tạo booking pending và trả snapshot giá. Hiện rõ “Đơn đang chờ thanh toán”, không hiện “Đặt vé thành công”, QR hoặc vé hợp lệ ở giai đoạn này.

| Thao tác | Điều kiện | Kết quả |
|---|---|---|
| Xem lại thông tin | Đã có booking của mình | Đọc lại `GET /api/bookings/{bookingId}`; hiển thị giá, ghế, hạn server. |
| Xác nhận / tiếp tục thanh toán | Booking pending đủ điều kiện hiện tại | Chuyển/tập trung vùng KH-05; không đổi status thành confirmed và không tự tạo attempt chỉ vì mở bước. |
| Quay lại chỉnh ghế/loại vé trước tạo đơn | Chỉ có lựa chọn cục bộ | Giữ showtimeId, ghế và loại vé còn phù hợp; chưa có quyền giữ/giá chốt nếu chưa tạo booking. |
| Đổi ghế/loại vé sau tạo đơn | Booking pending được backend chấp nhận hủy | Hủy toàn bộ hợp lệ rồi tạo đơn mới; không PATCH ghế/giá/loại vé trong booking đã có. |
| Hủy | Chủ booking và nghiệp vụ cho phép | Yêu cầu backend; nếu payment còn mở, cần kết quả kiểm tra/hủy cổng trước khi coi là thành công. |

Không tự thêm bước POST “confirm booking”. Nếu thiết kế đặt bản kiểm tra **trước** POST tạo booking, đó là tóm tắt lựa chọn cục bộ; báo giá trước đặt còn TBD, không trình bày tổng tự tính như giá cuối.

#### Ngữ cảnh B — booking đã confirmed

Chỉ hiển thị “Đặt vé thành công” sau khi đọc booking confirmed từ backend. Cùng vùng hiển thị mã đơn, phim/rạp/suất/phòng/ghế, giá chốt và vé được phát hành. Không còn countdown giữ ghế trên đơn đã xác nhận.

- Mỗi detail có số vé đúng sức chứa snapshot: Standard/VIP một vé; Couple hai vé với hai mã/QR độc lập. Hai vé cùng nhãn ghế không được gộp thành một QR.
- Hiển thị trạng thái từng vé hiện tại: valid, used hoặc voided; không mặc định mọi lần xem đều valid. Đọc vé không gọi thao tác validate của Staff.
- “My Tickets” mở KH-11; “Booking History” mở KH-08; “Về trang chủ” mở KH-01.
- Không cho Customer hủy đơn confirmed, đổi ghế hoặc yêu cầu refund trong MVP. Không thêm in/PDF/gửi email khi chức năng chưa được chốt.

#### Dữ liệu không còn hợp lệ và quay lại

1. Vào trực tiếp/reload/Back phải đọc booking theo quyền. Không có quyền/không tìm thấy hiển thị lỗi tương ứng, không dùng cache của người khác.
2. Suất đóng, ghế vừa bị chiếm hoặc loại vé không còn hợp lệ trước tạo đơn: từ chối toàn bộ yêu cầu và trở về lựa chọn với phản hồi cụ thể; không tạo vé giả.
3. Sau tạo, quá expiresAt chỉ là quá hạn danh nghĩa. Đọc trạng thái thực; pending có attempt chưa rõ vẫn hiển thị đang kiểm tra, không tự nhả ghế.
4. Booking expired/cancelled không tái mở; vẫn xem được nội dung lịch sử được phép, muốn đặt lại phải tạo booking mới.
5. Back không tự hủy hoặc reset hạn. Lựa chọn trước tạo giữ theo phiên rồi đối chiếu sơ đồ mới; sau tạo dùng booking snapshot, không coi state cục bộ là sửa đơn đã lưu.
6. Payment succeeded nhưng booking chưa confirmed: chuyển KH-10/kiểm tra kết quả, không hiện thành công hoặc tự hứa hoàn tiền.
7. Confirmed nhưng thiếu vé/dữ liệu bắt buộc: báo chưa tải đủ dữ liệu và đọc lại; đây là lỗi dữ liệu/tích hợp cần kiểm tra, không chốt một trạng thái nghiệp vụ mới.

#### Nghiệm thu Booking Confirmation

- **BC-01:** Đủ phim/rạp/suất/ghế/loại vé/tổng tiền từ đúng ngữ cảnh; dữ liệu thiếu được ghi TBD hoặc báo lỗi.
- **BC-02:** Xác nhận trước thanh toán không tự chuyển confirmed, giữ ghế hoặc thu tiền lần hai.
- **BC-03:** Back giữ dữ liệu phù hợp trước/sau tạo đơn, không đổi snapshot/hạn; sửa đơn đã tạo qua hủy hợp lệ.
- **BC-04:** Expired/cancelled/unknown và không có quyền không bị hiển thị thành đặt thành công.
- **BC-05:** Sau confirmed đủ vé đúng capacity; Couple hai mã riêng, trạng thái vé không bị ghi đè trên UI.
- **BC-06:** Điều hướng tới Payment, Payment Result, My Tickets và Booking History giữ đúng booking/ownership.

### 3.7. Đăng nhập / Đăng ký

- **Mục đích:** xác thực người dùng trước khi đặt vé.
- **Dữ liệu hiển thị:** form email, mật khẩu; đăng ký chỉ cấp role `CUSTOMER`.
- **Validation:** định dạng email, mật khẩu bắt buộc; lỗi xác thực hiển thị thông báo chung.
- **Tiêu chí nghiệm thu:** đăng nhập thành công quay lại màn hình trước khi bị chuyển hướng.

### 3.8. Tài khoản (Booking của tôi)

- **Mục đích:** xem lịch sử booking và vé của user.
- **Dữ liệu hiển thị:** danh sách booking với trạng thái (`pending`, `confirmed`, `expired`, `cancelled`), vé theo booking.
- **Tiêu chí nghiệm thu:** chỉ hiển thị booking của user hiện tại (authorization ở backend).

### 3.9. KH-09 — Ticket Type — CIN-SUB-056

#### Mục đích và quyền truy cập

Gán loại vé cho từng đơn vị ghế đã chọn của cùng suất. Đây có thể là vùng trong KH-04 hoặc bước liền sau; chưa chốt route riêng. Tạo booking vẫn yêu cầu đăng nhập; không có một quyền thanh toán khác cho bước chọn loại vé.

#### Nội dung và số lượng

| Nội dung | Quy tắc hiển thị / validation |
|---|---|
| Ghế đã chọn | Nhãn ghế, loại ghế, số chỗ; không cho thêm seatId không thuộc lựa chọn hiện tại. |
| Loại vé | ID, tên/mã, điều kiện áp dụng và còn hiệu lực từ server; endpoint `/api/ticket-types` mới ở mức Proposed, DTO chi tiết TBD. |
| Adult / Student / Child | Là các mã danh mục được mô tả; điều kiện tuổi/giấy tờ/quyền lợi cụ thể chưa đủ chốt. Không coi ghi chú seed là quy trình xác minh hoàn chỉnh. |
| Couple | Một đơn vị hai chỗ, bản đầu dùng Adult; không chọn Student cho một người và Adult cho người kia trong cùng detail. |
| Số lượng | Số detail bằng số đơn vị ghế, không có quantity tùy ý. Số vé sau xác nhận theo sức chứa đã snapshot, không theo số lần tăng một bộ đếm. |
| Giá và tổng | Dùng báo giá/snapshot từ server; không gửi discountPercent, unitPrice hoặc totalAmount để quyết định giá. |

Một booking được dùng nhiều loại vé cho các ghế khác nhau, mỗi ghế chỉ một loại vé. Không tách thanh toán từng detail. Loại vé ngừng hoạt động hoặc không đủ điều kiện phải bị backend từ chối dù client còn giữ lựa chọn cũ.

#### Giá theo nghiệp vụ đã xác định

Công thức sau giải thích giá do **backend** tính khi tạo booking; không yêu cầu frontend tự tính tổng để thay báo giá:

```text
base_amount = giá cơ sở suất × sức chứa của đơn vị ghế
surcharge_amount = phụ thu loại ghế, một lần cho mỗi detail
discount_amount = round(base_amount × tỷ lệ giảm / 100, 0)
unit_price = base_amount + surcharge_amount - discount_amount
booking.total_amount = tổng unit_price của các detail
```

Giảm chỉ trên base_amount, không giảm phụ thu. Couple nhân hai phần giá cơ sở, phụ thu chỉ cộng một lần; unitPrice là giá cả đơn vị hai chỗ, không nhân thêm lần nữa trên UI. VND nguyên đồng. OQ-06 đã chọn Option B: detail có thể giá 0 nhưng tổng booking/payment phải dương; không có luồng xác nhận booking 0 đồng trong MVP.

Chưa có contract báo giá tổng trước tạo booking: đánh dấu **TBD phối hợp API**, hiện các thành phần tham khảo có dữ liệu và ghi rõ giá chốt được trả khi tạo đơn. Không tự thêm API preview, phí đặt vé, coupon hoặc mức giảm mặc định.

#### Thao tác, tiếp tục và quay lại

1. Chỉ nhận danh sách ghế của KH-04, không tạo booking khi vừa mở bước.
2. Đổi loại vé cập nhật lựa chọn cục bộ của đúng ghế; giữ ghi chú điều kiện và làm mới báo giá nếu contract sau này hỗ trợ.
3. Tiếp tục khi có ít nhất một ghế và mỗi ghế có loại vé hợp lệ. Gửi cùng luồng tạo booking của KH-04, không tạo đơn lần hai ở Ticket Type.
4. Quay lại KH-04 giữ loại vé của ghế vẫn được chọn; ghế bị bỏ chọn không còn được tính. Khi thay suất, xác nhận lại toàn bộ ghế/loại vé cho suất mới.
5. Đã có booking thì đọc giá/loại vé snapshot; không sửa loại vé trực tiếp. Muốn đổi phải hủy hợp lệ và tạo booking khác.

Loading danh mục: khóa tiếp tục; danh mục rỗng: nêu chưa có loại vé được cung cấp; lỗi mạng: thử đọc lại, không tự gán ID Adult. Lỗi eligibility/loại vé hết hiệu lực hiện cạnh hàng ghế; xung đột ghế hoặc suất đóng bán quay về xử lý KH-04. Không yêu cầu tải giấy tờ cá nhân khi chưa có quyết định nghiệp vụ.

#### Nghiệm thu Ticket Type

- **TT-01:** Mỗi đơn vị ghế đúng một loại vé; số detail, số chỗ và số vé không bị gộp lẫn.
- **TT-02:** Couple không bị tách loại vé hoặc nhân đôi phụ thu/giá cuối trên UI.
- **TT-03:** Không tiếp tục khi thiếu danh mục/loại vé; backend kiểm tra lại điều kiện, không tin giá từ client.
- **TT-04:** Quay lại không tạo booking và giữ lựa chọn còn hợp lệ; booking đã có dùng snapshot.
- **TT-05:** Không tự đặt giới hạn vé/giảm giá/eligibility chưa chốt; thiếu báo giá được ghi TBD, không tạo tổng giả.

### 3.10. KH-10 — Payment Result — CIN-SUB-059

**Mục đích:** thông báo kết quả lần thanh toán và kết quả xác nhận booking như hai dữ liệu độc lập. Chủ booking đã đăng nhập mới xem được; biết paymentId/bookingCode không thay quyền sở hữu.

#### Dữ liệu và thời điểm đọc

- Nhận bookingId và paymentId khi đã biết; khi vào trực tiếp/reload phải xác thực, đọc payment và booking từ server.
- Hiển thị mã booking, ngữ cảnh phim/suất/phòng/ghế khi API cung cấp, số tiền/currency của attempt, payment status và booking status.
- Không coi query string hoặc thông báo trên trang cổng là nguồn kết quả chính thức. Nếu mất paymentId, contract tìm attempt hiện tại còn TBD; không dùng một attempt ngẫu nhiên hoặc tạo mới để thay thế.
- Field `appliedAt`, `reconciliationStatus`, nguyên nhân lỗi đã lọc và ngữ cảnh đầy đủ chưa được mô tả trong DTO Customer: ghi TBD, không tự lộ dữ liệu đối soát nội bộ.

#### Kết quả, thông báo và hành động

| Payment / booking được server xác nhận | Nội dung hiển thị | Hành động được phép |
|---|---|---|
| Succeeded + booking confirmed | Thanh toán và đặt vé đã được xác nhận; mã đơn, thông tin mua | Xem xác nhận KH-06, My Tickets KH-11 hoặc Booking History KH-08. |
| Created/pending | Thanh toán đang xử lý | Kiểm tra lại attempt; xem lịch sử. Không mở attempt khác. |
| Unknown | Kết quả đang được xác nhận | Đợi/đọc lại theo cơ chế được chốt; không báo failed hay tự retry thu tiền. |
| Failed/cancelled, booking pending còn hạn, không attempt mở | Lần thử thất bại/hủy đã xác minh; đơn vẫn chờ | Về KH-05 để kiểm tra điều kiện retry, không reset hạn. |
| Succeeded nhưng booking chưa confirmed | Kết quả tài chính và đơn chưa trùng nhau; chưa thể coi có vé | Kiểm tra lại hoặc xem lịch sử; không báo đặt thành công, không thanh toán lần nữa chỉ vì chưa có vé. |
| Booking expired/cancelled, kể cả payment thành công muộn | Đơn không được xác nhận; nêu đúng kết quả payment khi có | Xem lịch sử/đặt đơn mới; không reclaim ghế, không phát vé, không hứa hoàn tiền tự động. |
| Lỗi mạng / thiếu dữ liệu đọc | Không thể tải kết quả hiện tại | Thử GET lại, không gán trạng thái tài chính từ lỗi tải. |

Loading, lỗi đọc và kết quả đang xử lý phải phân biệt. Empty payment chưa tìm thấy không có nghĩa chưa thu tiền; báo chưa đủ dữ liệu/không tìm thấy theo response. 401 cần đăng nhập; 403 không có quyền; 404 không tự tạo giao dịch khác.

“Xem My Tickets” chỉ tới danh sách của phiên hiện tại; “Booking History” giữ đúng ngữ cảnh đơn nếu có. Khi chưa confirmed, không gắn một QR giả vào thông báo đang xử lý. Vé đã used/voided giữ trạng thái thật khi mở lại.

#### Nghiệm thu Payment Result

- **RS-01:** Cùng một payment succeeded nhưng booking pending hoặc expired không bị báo thành công đặt vé.
- **RS-02:** Unknown/created/pending không có nút tạo attempt luôn bật; retry về Payment và kiểm tra lại.
- **RS-03:** Đủ thông tin kết quả được phép xem, điều hướng đúng đơn/tài khoản; dữ liệu thiếu được ghi TBD.
- **RS-04:** Mạng lỗi/redirect/callback lặp không tự đảo trạng thái hoặc tạo thêm vé.
- **RS-05:** My Tickets và Booking History khác mục đích; late success không đưa ra cam kết refund chưa có.
