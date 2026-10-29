# Customer UI State

## 1. Phạm vi — CIN-SUB-064

Đặc tả điều kiện xuất hiện, nội dung và thao tác của trạng thái giao diện Customer. Dùng cùng [đặc tả màn hình](customer-screen-specification.md), [component](customer-component-specification.md) và [mapping API](customer-screen-api-mapping.md). Đây là đặc tả để triển khai, chưa phải kết quả kiểm thử ứng dụng.

Trạng thái tải dữ liệu, trạng thái tương tác và trạng thái nghiệp vụ là ba lớp độc lập. Ví dụ, tải lại payment bị lỗi mạng không làm payment chuyển `failed`; một ghế selected trong phiên vẫn chưa được backend giữ. Mã lỗi và DTO trong tài liệu API còn **Proposed**; nội dung thông báo tiếng Việt dưới đây là đề xuất UI, cần gắn với kết quả backend thực tế.

## 2. Trạng thái dữ liệu dùng chung

| Trạng thái | Điều kiện | Nội dung hiển thị | Thao tác / kết thúc |
|---|---|---|---|
| Loading lần đầu | Chưa có dữ liệu hợp lệ, đang GET | Skeleton theo khối; nhãn đang tải cho vùng nội dung | Khóa hành động phụ thuộc dữ liệu; cho điều hướng an toàn; nhận dữ liệu → ready/empty/error. |
| Refreshing | Có dữ liệu cũ, đang đọc lại cùng tài nguyên | Giữ dữ liệu với nhãn đang cập nhật; không giả đã tải mới | Khóa mutation cần trạng thái mới. Nếu đổi ID/ngày/trang thì phân biệt ngữ cảnh, không hiển thị dữ liệu cũ như kết quả mới. |
| Ready / success khi đọc | GET thành công và dữ liệu đủ để hiển thị | Nội dung thật, thời điểm/trạng thái theo server | Cho thao tác có đủ điều kiện. Success của GET không đồng nghĩa thanh toán thành công. |
| Empty | GET thành công, danh sách đúng phạm vi rỗng | Lời giải thích và điểm đi tiếp phù hợp | Không dùng cho timeout, 403, chưa có API hoặc dữ liệu bắt buộc bị thiếu. |
| Error lần đầu | Không đọc được dữ liệu cần thiết | Lỗi trong vùng nội dung, “Thử lại”, đường quay lại | Retry GET chuyển loading. Không dựng dữ liệu giả hoặc hiển thị chưa có vé từ lỗi API. |
| Error khi cập nhật | GET mới lỗi nhưng còn dữ liệu cũ | Giữ nội dung được phép xem và cảnh báo chưa cập nhật | Cho đọc lại; không dựa vào dữ liệu cũ để cam kết ghế/giá/trạng thái thanh toán. |
| Not found | Backend xác nhận không tìm thấy | “Không tìm thấy thông tin”, liên kết về danh sách | Không suy 404 thành suất đã hủy, booking hết hạn hoặc vé vô hiệu. |
| Forbidden | Backend từ chối quyền/ownership | “Bạn không có quyền xem thông tin này” | Không để lộ nội dung từ cache; cho về vùng được phép. |
| Chưa xác thực / phiên hết hạn | Không có phiên hoặc API trả 401 tương ứng | Yêu cầu đăng nhập, lưu đích nội bộ hợp lệ | Xóa nội dung private đang hiển thị; trở lại phải GET mới. Không tự lặp POST sau đăng nhập. |
| Dữ liệu chưa đủ / contract còn thiếu | Response thiếu trường bắt buộc, chức năng chưa có API | Nêu chưa tải đủ dữ liệu hoặc chức năng chưa sẵn sàng | Giữ phần có căn cứ, khóa thao tác phụ thuộc; không gán một trạng thái nghiệp vụ mới. |

Một response chỉ được áp dụng cho đúng ID, ngày, trang và phiên đã yêu cầu. Nếu Customer đổi ngày liên tục, response cũ đến muộn không được ghi đè ngày mới. Khi logout/đổi tài khoản, kết quả request của phiên trước không được đưa lại vào UI.

## 3. Trạng thái tương tác và form

| Trạng thái | Điều kiện / biểu hiện | Hành vi |
|---|---|---|
| Enabled | Đủ dữ liệu, quyền, validation và điều kiện nghiệp vụ | Thực hiện đúng một hành động, có nhãn rõ. Backend vẫn kiểm tra lại. |
| Disabled | Thiếu ghế/loại vé, không đủ quyền, suất đóng, mutation đang gửi hoặc attempt còn mở | Chặn cả chuột và bàn phím; nêu lý do gần hành động. Không chỉ giảm màu. |
| Selected / active | Ghế, loại vé, ngày hoặc tab đang chọn cục bộ | Nhãn/biểu tượng và kiểu hiển thị nhất quán; có thể đổi nếu chưa tạo booking. |
| Pristine / dirty | Form chưa đổi / đã sửa so với dữ liệu đọc gần nhất | Save chỉ khả dụng khi có thay đổi hợp lệ và đã có contract cập nhật. Back không tự lưu. |
| Validation error | Input không thỏa quy tắc đã chốt hoặc backend trả lỗi trường | Giữ giá trị, đặt lỗi cạnh trường, liên kết lỗi với input; không đưa mật khẩu vào toast/log. |
| Submitting | POST booking/payment/cancel hoặc lưu hồ sơ đang chờ | Spinner trên nút, khóa nhấn lặp và thao tác xung đột; giữ nội dung để Customer biết đang xử lý gì. |
| Mutation chưa rõ kết quả | Request thay đổi dữ liệu timeout/mất kết nối sau khi gửi | “Chưa xác định được kết quả”; khôi phục/đọc lại theo khả năng API; không báo chắc chắn thất bại và gửi lại với ý định mới. |
| Mutation thành công | Server xác nhận thao tác thành công | Cập nhật dữ liệu server và thông báo theo đúng thao tác; chỉ hiện “Đã lưu” khi thực sự có response lưu thành công. |

Các quy tắc focus, modal và nút dùng chung theo component. Đóng modal bằng X/Escape chỉ đóng giao diện; nút “Xác nhận hủy đơn” mới gửi yêu cầu hủy. Toast là phản hồi bổ sung; lỗi quan trọng của thanh toán/giữ ghế phải còn trong vùng nội dung sau khi toast hết thời gian.

## 4. Ghế và booking

### 4.1. Phân biệt các lớp trạng thái ghế

| Lớp | Giá trị / điều kiện | Biểu hiện và thao tác |
|---|---|---|
| Lựa chọn trong phiên | selected / chưa chọn | Có thể chọn/bỏ trước tạo booking; không có countdown và không nói đã giữ ghế. |
| Phân bổ theo suất | `available` | Chỉ cho chọn khi ghế vật lý hoạt động và suất còn bán; server xác minh lại lúc tạo booking. |
| Phân bổ theo suất | `held` | Không cho chọn như ghế trống; nếu thuộc đơn hiện tại thì xem trong snapshot của đơn, không sửa trực tiếp. |
| Phân bổ theo suất | `sold` | Không cho chọn; không suy từ màu selected. |
| Ghế vật lý | Bảo trì / ngừng hoạt động | Không cho chọn, chú giải riêng; field API biểu diễn tình trạng này còn TBD, không tự thêm giá trị vào enum allocation. |
| Xung đột lúc đặt | `SEAT_ALREADY_TAKEN` theo DTO Proposed | Từ chối toàn bộ yêu cầu; tải lại sơ đồ, bỏ các lựa chọn không còn hợp lệ, giữ phần draft còn hợp lệ để rà soát. |

Couple luôn là một đơn vị chọn với `capacityUnits = 2`; chưa có hai ghế đơn độc lập để chọn nửa. Hai ticket sau phát hành có trạng thái riêng. Không suy “hết ghế” từ lỗi tải sơ đồ.

### 4.2. Booking và thời hạn

| Dữ liệu backend / ngữ cảnh | Thông báo UI | Hành động phù hợp |
|---|---|---|
| Chưa tạo booking | “Lựa chọn chưa được giữ” | Sửa ghế/loại vé, rà soát ở KH-06, xác nhận tạo đơn. |
| Tạo booking đang gửi | “Đang tạo đơn” | Khóa submit; giữ requestKey và payload của ý định đó. |
| `pending`, còn hạn, không có attempt mở | Đang giữ ghế; tổng tiền đã chốt, countdown | Bắt đầu thanh toán hoặc yêu cầu hủy hợp lệ. |
| `pending`, có `created`/`pending`/`unknown` | Đang chờ/xác minh giao dịch | Khóa tạo attempt mới; kiểm tra kết quả; hủy chỉ qua quy trình backend xác minh cổng. |
| Countdown bằng 0 nhưng chưa có trạng thái mới | “Đang kiểm tra trạng thái đơn” | Khóa tạo attempt mới; GET trạng thái. Không tự đánh dấu expired hoặc mở ghế. |
| `expired` | “Đơn đã hết hạn” | Không thanh toán/tái mở; xem lịch sử hoặc chọn suất/ghế cho đơn mới. |
| `cancelled` | “Đơn đã hủy” | Xem thông tin lịch sử; không dùng lại booking để thu tiền. |
| `confirmed` | Đơn đã được xác nhận theo server | Xem vé đã phát hành; không sửa/hủy sau thanh toán trong phạm vi demo. |
| Đơn confirmed nhưng vé chưa tải đủ | Chưa tải đủ thông tin vé | Đọc lại và báo lỗi dữ liệu; không chuyển đơn về pending, không tạo vé ở client. |

Thời hạn giữ ghế danh nghĩa là 5 phút sau tạo booking; UI dùng `expiresAt` trả về, không tự cộng lại mỗi lần reload. Đồng hồ chỉ hiển thị thời gian, không thực hiện lifecycle. Quá hạn mà còn attempt mở có thể cần grace period và đối soát; giá trị grace period, chu kỳ đọc lại và cách đồng bộ giờ server còn cần thống nhất. Không hứa ghế được giải phóng ở đúng thời điểm client đếm hết.

Replay cùng requestKey và cùng nội dung sử dụng booking hiện có ở trạng thái thực. `REQUEST_KEY_CONFLICT` yêu cầu kiểm tra lại ý định, không sửa nội dung dưới key cũ. Timeout tạo đơn giữ trạng thái chưa rõ; không đổi key để đặt lại mù. Contract khôi phục sau reload/mất key còn TBD.

## 5. Payment và kết quả

### 5.1. Ma trận hiển thị

| Payment | Booking / dữ liệu bổ sung | Nội dung | Thao tác |
|---|---|---|---|
| `created` | Đơn hiện tại | Đang khởi tạo giao dịch | Không tạo attempt song song; chỉ mở lại payUrl đã có khi API/cổng hỗ trợ. |
| `pending` | Chưa chốt kết quả | Đang chờ thanh toán/xác nhận | Kiểm tra trạng thái; không retry tạo attempt mới. |
| `unknown` | Kết quả tài chính chưa xác định | “Kết quả đang được xác nhận” | Chờ backend đối soát; không coi thất bại, không phát hành vé hoặc giải phóng ghế ở UI. |
| `failed` / `cancelled` | Booking pending, còn hạn, không còn attempt mở | Thanh toán thất bại / giao dịch đã hủy | Đọc lại điều kiện trước retry hoặc yêu cầu hủy đơn; không nói booking đã tự hủy. |
| `failed` / `cancelled` | Booking expired/cancelled | Kết quả giao dịch và trạng thái đơn hiển thị riêng | Không retry vào đơn cũ; cho về lịch sử/tạo đơn mới. |
| `succeeded` | Booking confirmed, vé đã phát hành; payment đã được áp dụng theo backend | Thanh toán thành công và đơn đã xác nhận | Xem My Tickets/chi tiết booking; không tạo attempt mới. |
| `succeeded` | Booking chưa confirmed hoặc chưa có bằng chứng áp dụng | Đã ghi nhận kết quả giao dịch; đang kiểm tra đơn | GET lại/đối soát; chưa hiển thị QR thành công. |
| `succeeded` | Booking đã expired/cancelled, chưa áp dụng | Giao dịch cần được xử lý/đối soát | Không chiếm lại ghế, không tự hứa hoàn tiền hoặc cho Customer tự đổi trạng thái. |
| Không đọc được payment | GET timeout/lỗi, chưa đủ dữ liệu | Chưa tải được kết quả | Thử đọc lại; không đổi thành failed và không gửi POST thanh toán để kiểm tra. |

DTO payment hiện chưa có `appliedAt`, `reconciliationStatus` và cơ chế tìm attempt theo booking. Bằng chứng áp dụng, dữ liệu khôi phục và thông báo chi tiết cần phối hợp API. Không đoán trạng thái từ URL quay về, tham số cổng hoặc việc đã đóng cửa sổ cổng. Webhook là giao tiếp cổng với backend, Customer không gọi callback để xác nhận thành công.

### 5.2. Chuyển tiếp và hành động xung đột

- POST payment thành công: giữ paymentId, mở payUrl hợp lệ nếu có; kết quả cuối được đọc từ backend. Nhà cung cấp và cơ chế quay về còn TBD.
- `PAYMENT_ATTEMPT_OPEN`: dừng POST mới, khôi phục/theo dõi attempt cũ theo contract; không tự chọn paymentId từ cache của đơn khác.
- `BOOKING_EXPIRED` hoặc `BOOKING_ALREADY_CONFIRMED`: đọc lại booking, đổi vùng nội dung theo trạng thái thật; không tự sửa lifecycle tại client.
- Hủy đơn có attempt mở: backend Query/Cancel cổng ngoài transaction; chỉ xác nhận hủy/nhả ghế sau bằng chứng chưa thu tiền. Cổng báo đã thu/chưa rõ thì từ chối hủy và hiển thị đang kiểm tra kết quả. UI không gọi cổng trực tiếp để vượt điều kiện này.
- Back, refresh, Forward hoặc mở nhiều tab không tạo một ý định thanh toán mới. Khóa nút tại UI hỗ trợ tránh nhấn lặp; tính đúng đắn đồng thời vẫn do backend bảo đảm.
- Khi rời trang hoặc logout, dừng việc đọc lặp của trang; không diễn giải việc dừng đọc là hủy payment. Tần suất polling/backoff và giới hạn chờ phải chốt với API, không tự đặt một thời gian rồi kết luận failed.

## 6. Trạng thái theo màn hình

| Màn hình | Loading / empty | Error / disabled | Ready và thao tác tiếp |
|---|---|---|---|
| KH-01 Home | Skeleton card; “Hiện chưa có phim” cho nhóm đang xem | Lỗi tải có retry; không lấy dữ liệu nhóm cũ làm nhóm mới | Chọn phim; đổi nhóm/trang được hỗ trợ. |
| KH-02 Movie Detail | Skeleton chi tiết; không dùng empty list thay phim không tồn tại | 404 riêng, ảnh lỗi có placeholder | Xem thông tin, mở Showtime đúng phim. |
| KH-03 Showtime | Tải theo phim/ngày; “Chưa có suất chiếu” | Suất không còn bán disabled khi API xác nhận; lỗi GET khác rỗng | Chọn suất với `bookingAvailable` hợp lệ. |
| KH-04 Seat Selection | Tải sơ đồ; rỗng/không còn ghế chọn được được phân biệt | Ghế held/sold/vật lý không hoạt động disabled; xung đột tải lại | Chọn/bỏ ghế cục bộ rồi tới KH-09. |
| KH-09 Ticket Type | Tải danh mục; không có loại hợp lệ thì chặn tiếp | Thiếu loại cho một ghế, loại đã ngừng, eligibility chưa xác minh | Gán loại từng đơn vị rồi tới KH-06; chưa giữ ghế. |
| KH-06 Booking Confirmation | Rà soát draft; xác nhận tạo booking có submitting | Draft không đủ/chưa login thì chưa POST; lỗi ghế trả lại lựa chọn | POST booking thành công → KH-05 với snapshot server. |
| KH-05 Payment | Đọc booking/attempt; không dùng empty cho booking không tìm thấy | Khóa khi attempt mở, hết điều kiện hoặc chưa tải được trạng thái | Khởi tạo/theo dõi attempt; tới KH-10 để xem kết quả. |
| KH-10 Payment Result | Đang kiểm tra; không dùng empty cho unknown | Lỗi GET có retry đọc; không retry POST mù | Theo ma trận mục 5; tới vé/lịch sử khi phù hợp. |
| KH-07 Login/Register | Loading trên submit, không có empty state danh sách | Lỗi trường, sai thông tin hoặc email trùng; khóa nhấn lặp | Login có phiên → đích nội bộ; register thành công → đăng nhập vì response chưa có token. |
| KH-08 Booking History | Skeleton danh sách; chưa có đơn đúng phạm vi | Lỗi tải trang khác trang rỗng; điều kiện hành động theo booking mới nhất | Phân trang, xem chi tiết; không gộp mọi đơn thành vé. |
| KH-11 My Tickets | Tải vé; “Bạn chưa có vé” chỉ sau nguồn danh sách hợp lệ trả rỗng | Thiếu API danh sách không phải chưa có vé; lỗi QR có retry | Xem valid/used/voided độc lập, không gọi validate của Staff. |
| KH-12 Profile | Tải hồ sơ; không thay lỗi GET bằng form rỗng | Contract xem/lưu còn TBD; lỗi save giữ draft | Read-only cho trường chưa cho sửa; chỉ báo lưu thành công theo response thật. |

## 7. Tiêu chí nghiệm thu đặc tả trạng thái

- **ST-01:** Có loading/refreshing/ready/empty/error/disabled/selected/validation/submitting; mỗi trạng thái có trigger, phản hồi và thao tác.
- **ST-02:** Đủ KH-01..12; cùng một mã nghiệp vụ có nội dung nhất quán trên Payment, Result, History và chi tiết đơn.
- **ST-03:** Ghế selected chưa held; Couple một đơn vị; xung đột không giữ một phần và không mất toàn bộ draft còn hợp lệ.
- **ST-04:** Countdown bằng 0, timeout và lỗi GET không tự đổi trạng thái booking/payment/ghế.
- **ST-05:** Attempt created/pending/unknown chặn tạo song song; succeeded chưa áp dụng không thành vé; expired/cancelled không tái mở.
- **ST-06:** Logout/đổi tài khoản/response cũ không làm lộ dữ liệu; modal đóng không gửi hủy; form lỗi giữ input phù hợp.
- **ST-07:** Nội dung chưa có API và giá trị thời gian chưa thống nhất được ghi TBD; các kịch bản này là tiêu chí cần kiểm thử khi có ứng dụng.
