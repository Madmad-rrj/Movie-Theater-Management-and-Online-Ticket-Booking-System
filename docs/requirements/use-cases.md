# Hệ thống quản lý và đặt vé rạp chiếu phim — Use Cases & Nghiệp vụ cốt lõi

## 1. Mục đích và phạm vi tài liệu

Tài liệu này đặc tả hành vi của các Use Case (UC) trong hệ thống quản lý rạp chiếu phim và đặt vé trực tuyến (*Movie Theater Management and Online Ticket Booking System*). Tài liệu tập trung đặc tả chi tiết các quy trình có yêu cầu cao về tính toàn vẹn dữ liệu, xử lý đồng thời (*concurrency*), tính nhất quán trạng thái và tính lặp an toàn (*idempotency*), đặc biệt là các nghiệp vụ giữ ghế, thanh toán, giải phóng ghế hết hạn, đối soát giao dịch và xác thực vé.

Các nghiệp vụ duyệt phim, xem lịch chiếu, xác thực người dùng và quản trị dữ liệu được liệt kê trong danh mục Use Case tại Mục 3. Mức độ đặc tả chi tiết của từng nghiệp vụ được xác định theo phạm vi tài liệu và các tài liệu yêu cầu, phân tích, thiết kế có liên quan.

Tài liệu được sử dụng làm cơ sở để xây dựng API Specification, triển khai Backend và thiết kế Test Cases. Tài liệu tập trung vào hành vi nghiệp vụ có thể quan sát được, điều kiện trước và sau, luồng chính, luồng thay thế, xử lý lỗi và các quy tắc chuyển trạng thái; không thay thế tài liệu thiết kế cơ sở dữ liệu hoặc đặc tả API chi tiết.

### 1.1. Quan hệ giữa các tài liệu

| Tài liệu | Vai trò và phạm vi |
| :--- | :--- |
| `docs/requirements/business-system-analysis.md` | Tập hợp yêu cầu chức năng (FR), quy tắc nghiệp vụ (BR), yêu cầu phi chức năng (NFR), tiêu chí nghiệm thu (AC) và câu hỏi mở (OQ). Đây là nguồn tham chiếu chính cho các yêu cầu và quy tắc nghiệp vụ đã được xác định. |
| `docs/analysis and design/business-system-design.md` | Mô tả các tác nhân (actors), Use Case tổng quan và sơ đồ luồng nghiệp vụ ở mức thiết kế hệ thống. |
| `docs/analysis and design/relational-model.md`, `database/database-schema.md`, `database/database-schema.sql` | Mô tả mô hình quan hệ, cấu trúc bảng, trạng thái, cột và các ràng buộc dữ liệu được sử dụng làm căn cứ khi đặc tả các Use Case. |
| `database/database-design.md` | Mô tả thiết kế entity, cơ chế khóa và giao dịch (*transaction*), cùng các giao thức xử lý dữ liệu liên quan tại Mục 31–34. Đây là nguồn tham chiếu cho cơ chế kỹ thuật, không tự quyết định hành vi nghiệp vụ. |
| `use-cases.md` (tài liệu này) | Đặc tả hành vi nghiệp vụ của các Use Case thuộc phạm vi tài liệu, bao gồm điều kiện thực hiện, luồng xử lý, kết quả, luồng thay thế và cách xử lý các tình huống bất thường. |
| API Specification và Test Cases (sẽ xây dựng) | Được dẫn xuất từ yêu cầu và Use Case đã thống nhất. API Specification mô tả hợp đồng API; Test Cases mô tả các tình huống kiểm thử và kết quả mong đợi. |

### 1.2. Nguyên tắc tham chiếu và xử lý mâu thuẫn

1. **Quy tắc nghiệp vụ:** Các quy tắc đã được chốt phải được thực hiện nhất quán với tài liệu yêu cầu và quyết định nghiệp vụ tương ứng. Tài liệu này không tự ý thay đổi hoặc diễn giải lại các quyết định đã chốt, bao gồm OQ-06 Option B, OQ-07 Grace Period, chính sách giá và vòng đời booking/payment/ticket.
2. **Cấu trúc và ràng buộc dữ liệu:** Tên bảng, cột, trạng thái và constraint phải phù hợp với mô hình quan hệ và schema được tham chiếu. Không suy diễn rằng một trạng thái hoặc constraint tồn tại nếu tài liệu/schema nguồn chưa xác định.
3. **Cơ chế kỹ thuật:** Các quy tắc khóa, transaction và đồng bộ dữ liệu được tham chiếu từ tài liệu thiết kế cơ sở dữ liệu. Cơ chế kỹ thuật phải bảo đảm hành vi nghiệp vụ đã quy định, nhưng không được dùng để tự đặt ra quy tắc nghiệp vụ mới.
4. **Phạm vi theo tác nhân:** Các Use Case phải xác định rõ tác nhân thực hiện. Luồng Customer, Staff, Manager và Admin được phân biệt theo trách nhiệm và quyền hạn; không mặc định mọi tác nhân đều có thể thực hiện mọi nghiệp vụ.
5. **Câu hỏi chưa chốt:** Những điểm còn thiếu quyết định hoặc có mâu thuẫn giữa các tài liệu phải được ghi nhận tại Mục 16 dưới dạng câu hỏi mở (OQ), kèm nội dung cần quyết định. Không tự coi phương án đề xuất là quy tắc chính thức trước khi được thống nhất.
6. **Tính nhất quán khi triển khai:** API Specification, Backend và Test Cases phải tuân thủ các yêu cầu và Use Case đã thống nhất. Nếu phát hiện mâu thuẫn trong quá trình triển khai, cần cập nhật hoặc làm rõ tài liệu nguồn trước khi áp dụng một cách diễn giải mới.

## 2. Quy ước trạng thái và khái niệm nghiệp vụ

### 2.1. Các trạng thái được định nghĩa trong schema

Các giá trị trạng thái dưới đây được tham chiếu từ `database/database-schema.md` và `database/database-schema.sql`. Khi đặc tả Use Case, phải sử dụng đúng tên và giá trị đã được định nghĩa trong schema, không tự ý bổ sung hoặc đổi tên trạng thái.

| Đối tượng | Trường trạng thái | Các giá trị hợp lệ |
| :--- | :--- | :--- |
| Booking | `booking.status` | `pending`, `confirmed`, `expired`, `cancelled` |
| Chi tiết booking | `booking_detail.allocation_status` | `held`, `sold`, `released` |
| Payment | `payment.status` | `created`, `pending`, `unknown`, `succeeded`, `failed`, `cancelled` |
| Payment | `payment.reconciliation_status` | `none`, `needs_review`, `refund_required`, `resolved` |
| Payment event | `payment_event.status` | `received`, `processed`, `failed` |
| Ticket | `ticket.status` | `valid`, `used`, `voided` |
| Showtime | `showtime.status` | `draft`, `scheduled`, `in_progress`, `completed`, `cancelled` |
| Refund | `refund.status` | `pending`, `unknown`, `succeeded`, `failed` |

### 2.2. Các khái niệm nghiệp vụ

Các khái niệm dưới đây được sử dụng để mô tả hành vi của hệ thống. Chúng không phải là giá trị trạng thái độc lập được lưu trong cơ sở dữ liệu, trừ khi schema có định nghĩa riêng.

- **Booking quá hạn giữ ghế:** Booking đã vượt thời điểm `booking.expires_at`. Việc xác định booking có được giải phóng hay không còn phụ thuộc vào trạng thái booking, các payment liên quan và quy tắc nghiệp vụ áp dụng; không được mặc định chỉ cần hết hạn là có thể giải phóng ghế ngay lập tức.
- **Phân bổ ghế hiệu lực (*active seat allocation*):** Phân bổ ghế có `booking_detail.allocation_status` là `held` hoặc `sold`. Phân bổ `released` không còn giữ ghế.
- **Ghế khả dụng (*available seat*):** Ghế chưa có phân bổ hiệu lực cho suất chiếu tương ứng. Đây là khái niệm được suy ra từ dữ liệu phân bổ ghế, không phải trạng thái được lưu trực tiếp trong schema.
- **Thời điểm hết hạn giữ ghế:** Giá trị `booking.expires_at`, dùng để xác định thời hạn giữ ghế. Bộ đếm thời gian trên giao diện phải dựa trên thời điểm hết hạn do Backend trả về, không tự tạo một thời hạn độc lập ở Frontend.
- **Payment thành công:** Payment có `payment.status = succeeded`. Trạng thái này thể hiện kết quả thanh toán được ghi nhận, nhưng không tự động đồng nghĩa booking đã được xác nhận hoặc vé đã được phát hành. Việc áp dụng kết quả thanh toán phải tuân theo các điều kiện của Use Case xử lý payment.
- **Payment cần xử lý đối soát:** Payment có `payment.reconciliation_status` là `needs_review` hoặc `refund_required`, cho biết cần xử lý tiếp theo theo quy trình đối soát hoặc hoàn tiền tương ứng.

### 2.3. Nguyên tắc sử dụng trạng thái trong Use Case

1. Mọi điều kiện trước, bước xử lý, luồng thay thế và điều kiện sau phải sử dụng đúng trạng thái đã được định nghĩa trong schema.
2. Không coi khái niệm nghiệp vụ suy ra từ dữ liệu là một trạng thái DB mới nếu chưa có quyết định thiết kế tương ứng.
3. Việc chuyển trạng thái phải tuân thủ các điều kiện nghiệp vụ, tính toàn vẹn dữ liệu và quy tắc xử lý đồng thời được quy định trong các Use Case liên quan.
4. Không suy luận rằng một trạng thái đơn lẻ đủ để quyết định toàn bộ kết quả nghiệp vụ. Ví dụ, booking đã quá hạn không nhất thiết được giải phóng ngay nếu còn payment đang chờ xử lý hoặc chưa xác định được kết quả.
5. Nếu tài liệu Use Case và schema không thống nhất về tên hoặc giá trị trạng thái, cần làm rõ và cập nhật tài liệu nguồn trước khi triển khai; không tự ý tạo thêm trạng thái để giải quyết mâu thuẫn.
---
## 3. Danh mục Use Cases tổng thể

Bảng dưới đây tổng hợp các Use Case trong hệ thống theo nhóm nghiệp vụ, tác nhân và phạm vi đặc tả. Danh mục này phân biệt các Use Case được đặc tả chi tiết trong tài liệu với các nghiệp vụ được tham chiếu từ tài liệu yêu cầu và thiết kế hệ thống.

| Nhóm nghiệp vụ | Use Case | Tác nhân chính | Phạm vi đặc tả |
| :--- | :--- | :--- | :--- |
| **Xác thực và tài khoản** | **UC-08: Đăng nhập và điều hướng theo vai trò (Login and Role-Based Navigation)** | Customer, Staff, Manager, Admin | **Đặc tả chi tiết tại tài liệu này** (đăng nhập/đăng xuất/điều hướng), áp dụng `FR-AUTH-002/004`. Đăng ký, hồ sơ cá nhân và quản trị user/role vẫn tham chiếu `FR-AUTH-001/003` và `FR-ADMIN-USER-001`; ranh giới quyền còn chờ `OQ-04`. |
| **Duyệt phim và suất chiếu** | **UC-06: Xem danh sách và thông tin phim (Browse Movies)** | Customer | **Đặc tả chi tiết tại tài liệu này.** Tham chiếu `FR-MOVIE-001/002` và các quy tắc liên quan trong `business-system-analysis.md`. |
| **Duyệt phim và suất chiếu** | **UC-07: Xem lịch chiếu và chọn suất chiếu (Browse Showtimes)** | Customer | **Đặc tả chi tiết tại tài liệu này.** Tham chiếu `FR-SHOW-001` và các quy tắc liên quan trong `business-system-analysis.md`. Xem sơ đồ ghế (`FR-SHOW-002`) là hiển thị thông tin phục vụ chọn ghế; việc giữ ghế nguyên tử thuộc UC-01. |
| **Duyệt phim và suất chiếu** | Xem sơ đồ ghế | Customer | Tham chiếu `FR-SHOW-002` trong `business-system-analysis.md`. Hiển thị sơ đồ ghế và trạng thái phân bổ hiện hành là thông tin tham khảo; tính khả dụng chốt cuối cùng do Backend kiểm tra lại trong transaction tạo booking (UC-01, `FR-BOOK-001`, `BR-BOOK-002`). Việc xem sơ đồ ghế không đồng nghĩa ghế đã được giữ. |
| **Đặt vé** | **UC-01: Giữ ghế và tạo đơn hàng (Reserve Seats)** | Customer | **Đặc tả chi tiết tại tài liệu này.** |
| **Thanh toán** | **UC-09: Checkout và thanh toán (Checkout and Payment)** | Customer | **Đặc tả chi tiết tại tài liệu này** (checkout, khởi tạo payment attempt). Tạo payment attempt theo `FR-PAY-001`, `BR-PAY-001/002` đã được xác minh trong `business-system-analysis.md`; cổng thanh toán cụ thể còn chờ `OQ-01`. |
| **Đặt vé** | **UC-10: Quản lý vòng lifecycle đặt vé của Customer (Customer Booking Lifecycle)** | Customer | **Đặc tả chi tiết tại tài liệu này** (tra cứu lịch sử booking, xem vé, yêu cầu hủy). Quy tắc hủy theo `FR-BOOK-003`, `BR-BOOK-008`, `AC-BOOK-004..006` đã được xác minh trong `business-system-analysis.md`. |
| **Thanh toán** | **UC-02: Xử lý webhook thanh toán (Process Webhook)** | Cổng thanh toán, hệ thống | **Đặc tả chi tiết tại tài liệu này.** |
| **Vòng đời booking** | **UC-03: Giải phóng đơn hàng hết hạn (Release Expired Bookings)** | Hệ thống | **Đặc tả chi tiết tại tài liệu này.** |
| **Đối soát và hoàn tiền** | **UC-04: Đối soát payment (Payment Reconciliation)** | Hệ thống, Manager theo quyền được xác định | **Đặc tả chi tiết tại tài liệu này**, trong giới hạn phạm vi đã thống nhất. |
| **Đối soát và hoàn tiền** | **UC-04A: Hoàn tiền (Refund)** | Hệ thống, Manager theo quyền được xác định | **Đặc tả hợp đồng hành vi tại tài liệu này; thực thi hoàn tiền ngoài phạm vi demo.** |
| **Quản lý vé** | Xem vé và mã QR; Gửi vé qua Email | Customer, hệ thống | Tham chiếu yêu cầu nghiệp vụ liên quan. Việc phát hành và gửi vé phải tuân thủ điều kiện sau commit được quy định tại `database/database-design.md`, Mục 31.4. |
| **Soát vé** | **UC-05: Soát vé tại rạp (Validate Ticket)** | Staff | **Đặc tả chi tiết tại tài liệu này.** |
| **Quản trị hệ thống** | Quản lý phim, rạp, phòng chiếu, ghế, suất chiếu, người dùng và vai trò | Admin; Manager theo quyền được xác định | Tham chiếu các yêu cầu `FR-MGT-*`, `BR-ROOM-*`, `BR-ST-*` và các quyết định phân quyền tại `OQ-04`. |

### 3.1. Luồng Customer đặt vé

Luồng giao diện đặt vé của Customer được mô tả ở mức tổng quan như sau:

1. Customer xem chi tiết phim, chọn suất chiếu và loại vé phù hợp.
2. Customer chọn ghế trên sơ đồ ghế. Frontend gửi yêu cầu giữ ghế đến Backend.
3. Chỉ sau khi Backend xác nhận giữ ghế thành công, giao diện mới hiển thị thanh thông tin đặt vé cùng bộ đếm thời gian dựa trên `booking.expires_at` do Backend trả về.
4. Khi Customer nhấn **Đặt vé**, hệ thống chuyển đến trang Checkout và sử dụng booking đã được tạo ở bước giữ ghế. Không tạo booking thứ hai cho cùng lượt đặt vé.
5. Các bước duyệt phim, xem chi tiết và xem lịch chiếu không yêu cầu đăng nhập theo `FR-MOVIE-001/002` và `FR-SHOW-001`; việc tạo booking ở UC-01 chỉ được thực hiện bởi Customer đã đăng nhập và có tài khoản `active`. Nếu Customer chưa đáp ứng điều kiện này, hệ thống xử lý theo quy tắc xác thực và phân quyền đã thống nhất.
6. Không được coi việc chọn ghế trên giao diện là bằng chứng rằng ghế đã được giữ thành công; Backend phải trả kết quả authoritative của UC-01 trước khi Frontend chuyển sang Checkout.

Luồng trên mô tả tương tác giao diện và liên kết các Use Case. Chi tiết về tính hợp lệ của suất chiếu, khóa ghế, thời hạn giữ ghế, xử lý đồng thời và tính idempotent được quy định tại UC-01. Luồng khởi tạo thanh toán được quy định tại yêu cầu thanh toán tương ứng và không đồng nhất với thao tác giữ ghế.

### 3.2. Quy ước phạm vi đặc tả

- **Đặc tả chi tiết:** UC-01, UC-02, UC-03, UC-04, UC-04A, UC-05, UC-06, UC-07, UC-08, UC-09 và UC-10 có luồng xử lý chi tiết trong tài liệu này.
- **Tham chiếu:** Các nghiệp vụ xem sơ đồ ghế, đăng ký tài khoản, quản trị user/role, quản lý vé và CRUD quản trị được dẫn chiếu đến tài liệu yêu cầu hoặc thiết kế tương ứng. Không xem việc liệt kê trong bảng là bằng chứng rằng nghiệp vụ đã có đặc tả chi tiết đầy đủ.
- **Phân quyền:** Customer, Staff, Manager và Admin là các nhóm tác nhân khác nhau. Quyền thực hiện từng nghiệp vụ phải tuân theo quyết định phân quyền đã thống nhất; không mặc định Manager có quyền thực hiện mọi nghiệp vụ đối soát hoặc hoàn tiền khi `OQ-04` chưa được chốt.
- **Phạm vi demo:** UC-04A và luồng đối soát tài chính tự động đối với payment thành công đến muộn nằm ngoài phạm vi demo theo `business-system-analysis.md`, Mục 3.1. UC-04/UC-04A vẫn được mô tả để xác định hợp đồng hành vi và cách sử dụng mô hình dữ liệu hiện có (`refund`, `payment.reconciliation_status`). Việc đặc tả không đồng nghĩa với cam kết triển khai đầy đủ cơ chế đối soát hoặc thực thi hoàn tiền trong demo.
---
## 4. Quy ước đặc tả Use Case

Mỗi Use Case được đặc tả chi tiết theo cùng một cấu trúc nhằm bảo đảm tính nhất quán giữa các nghiệp vụ, hỗ trợ truy vết yêu cầu và làm cơ sở xây dựng API Specification, Backend và Test Cases.

### 4.1. Cấu trúc bắt buộc

Mỗi UC chi tiết bao gồm các thành phần sau:

1. **Mục tiêu (Goal):** Kết quả nghiệp vụ mà UC cần đạt được.
2. **Tác nhân (Actors):** Tác nhân chính và các tác nhân phụ có liên quan.
3. **Trigger:** Sự kiện khởi phát UC.
4. **Tiền điều kiện (Preconditions):** Các điều kiện phải thỏa mãn trước khi UC bắt đầu.
5. **Luồng chính (Main Flow):** Các bước xử lý của kịch bản thành công, được đánh số theo thứ tự.
6. **Luồng thay thế và ngoại lệ (Alternative/Exception Flows):** Các tình huống khác luồng chính, được gắn mã định danh ổn định như `A1`, `A2`, `A3`,...
7. **Hậu điều kiện (Postconditions):** Trạng thái hệ thống sau khi UC thành công hoặc thất bại, bao gồm các thay đổi dữ liệu và những điều kiện bất biến cần được bảo toàn.
8. **Quy tắc nghiệp vụ liên quan (Business Rules):** Các BR, FR hoặc quyết định đã chốt chi phối hành vi của UC.
9. **Tiêu chí nghiệm thu (Acceptance Criteria):** Các kết quả có thể kiểm chứng để xác nhận UC hoạt động đúng yêu cầu.
10. **UC liên quan (Related Use Cases):** Các UC có quan hệ gọi, chuyển tiếp hoặc phụ thuộc về nghiệp vụ.
11. **Câu hỏi mở (Open Questions):** Những vấn đề chưa được quyết định hoặc cần xác nhận trước khi triển khai.

### 4.2. Quy ước trình bày luồng xử lý

- Mỗi bước trong luồng chính và luồng thay thế phải thể hiện rõ hành động, điều kiện xử lý hoặc kết quả tương ứng.
- Các bước do tác nhân thực hiện được gắn tiền tố theo vai trò, chẳng hạn `[Customer]`, `[Staff]`, `[Manager]`, `[Admin]`, `[Worker]` hoặc tên tác nhân bên ngoài như `[Payment Gateway]`.
- Các bước không có tiền tố mô tả hành vi của hệ thống, bao gồm xử lý Backend, kiểm tra điều kiện, cập nhật dữ liệu và phản hồi kết quả.
- Không dùng tiền tố tác nhân cho hành vi do hệ thống tự động thực hiện. Đặc biệt, `[Worker]` chỉ được dùng khi một tiến trình nền thực sự thực hiện hành động, chẳng hạn giải phóng booking hết hạn hoặc xử lý payment event.
- Mỗi bước cần phân biệt rõ giữa yêu cầu được gửi đi, kết quả được xác nhận và thay đổi trạng thái đã được ghi nhận. Không mô tả thao tác giao diện như bằng chứng rằng giao dịch Backend đã thành công.

### 4.3. Quy ước mã luồng thay thế và ngoại lệ

- Mỗi luồng thay thế hoặc ngoại lệ có một mã định danh ổn định, theo dạng `A1`, `A2`, `A3`,...
- Mỗi mã phải nêu rõ điều kiện kích hoạt, bước trong luồng chính mà luồng đó phân nhánh và cách xử lý của hệ thống.
- Khi có thể, luồng thay thế phải chỉ rõ kết quả cuối cùng: tiếp tục luồng chính, kết thúc UC thành công, từ chối yêu cầu, giữ nguyên trạng thái hoặc chuyển sang UC khác.
- Không tái sử dụng một mã `A*` cho một tình huống khác trong cùng UC. Khi chỉnh sửa tài liệu, nên giữ nguyên mã của tình huống hiện có nếu ý nghĩa của tình huống không thay đổi, nhằm hạn chế ảnh hưởng đến tham chiếu trong API Specification và Test Cases.
- Các tình huống có liên quan đến thanh toán, khóa ghế hoặc xử lý đồng thời phải nêu rõ trạng thái dữ liệu sau khi xử lý; không chỉ mô tả thông báo hiển thị cho người dùng.

### 4.4. Nguyên tắc nhất quán

Đặc tả của mỗi UC phải tuân thủ các nguyên tắc sau:

1. Sử dụng đúng tên trạng thái, trường dữ liệu và ràng buộc đã được xác định trong schema.
2. Phân biệt trạng thái lưu trong cơ sở dữ liệu với khái niệm nghiệp vụ được suy ra từ dữ liệu.
3. Nêu rõ hậu điều kiện của cả trường hợp thành công và thất bại; không giả định rằng lỗi xảy ra đồng nghĩa với mọi thay đổi dữ liệu đã được hoàn tác nếu điều đó chưa được bảo đảm.
4. Không tự đặt thêm quy tắc nghiệp vụ hoặc coi câu hỏi mở là quyết định đã chốt.
5. Bảo đảm các luồng thay thế không mâu thuẫn với luồng chính, các UC liên quan và quy tắc nghiệp vụ nguồn.
---
## 5. UC-01: Giữ ghế và tạo đơn hàng (Reserve Seats)

### 5.1. Mục tiêu

Cho phép Customer giữ toàn bộ ghế còn khả dụng của một suất chiếu và tạo booking ở trạng thái `pending` để tiếp tục thanh toán. Việc giữ ghế phải được thực hiện nguyên tử: hoặc toàn bộ ghế được giữ thành công, hoặc không ghế nào được giữ bởi yêu cầu đó.

### 5.2. Tác nhân

- **Tác nhân chính:** Customer.
- **Tác nhân phụ:** Backend Booking Service, PostgreSQL.

### 5.3. Trigger

Customer gửi yêu cầu giữ các ghế đã chọn, kèm loại vé tương ứng cho từng ghế và `request_key` do client tạo.

### 5.4. Tiền điều kiện

- Customer đã đăng nhập, được xác thực và có tài khoản ở trạng thái `active`.
- Yêu cầu chứa ít nhất một ghế; tất cả ghế thuộc cùng một suất chiếu và không bị trùng lặp. Loại vé phải hợp lệ cho từng ghế. Trong MVP, ghế Couple sử dụng loại vé Adult theo `database/database-design.md`, Mục 10.
- Suất chiếu phải ở trạng thái `scheduled`, chưa vượt `booking_closes_at` nếu mốc này được cấu hình (`NULL` nghĩa là không có cutoff bổ sung theo `BR-ST-003`), đồng thời phải còn đủ thời gian cho thời hạn giữ ghế 5 phút mà không vượt mốc đóng bán hoặc giờ bắt đầu. Đây là các điều kiện riêng biệt; Backend phải kiểm tra theo quy tắc đã thống nhất, không tự đặt thêm cutoff nghiệp vụ. Tham chiếu `database/database-design.md`, Mục 12 và `OQ-02`.
- Chỉ cho phép tạo booking khi suất chiếu đang bán theo quy tắc đã chốt `BR-ST-003`: trạng thái `scheduled` và chưa vượt `booking_closes_at`. Không cho phép đặt khi suất chiếu `in_progress`, `completed` hoặc `cancelled` (`FR-BOOK-001` yêu cầu kiểm tra lại authoritative theo `BR-ST-003` khi tạo booking).
- `request_fingerprint` được tính bằng SHA-256 trên dữ liệu đặt vé đã chuẩn hóa theo schema, bao gồm suất chiếu, danh sách ghế và loại vé tương ứng, cùng thông tin liên hệ nếu thuộc payload. Việc sắp xếp danh sách ghế trước khi băm là đề xuất đang chờ quyết định tại `OQ-UC-05`; không được coi đây là quy tắc đã chốt cho đến khi OQ được giải quyết.

### 5.5. Luồng chính

1. **[Customer]** Chọn suất chiếu, loại vé và các ghế mong muốn trên giao diện.
2. **[Customer]** Gửi yêu cầu giữ ghế kèm `request_key` (UUID do client tạo), danh sách ghế và loại vé tương ứng.
3. Hệ thống xác thực Customer, kiểm tra định dạng payload, xác nhận ghế thuộc phòng của suất chiếu và loại vé tồn tại, đang hoạt động (`is_active`).
4. Hệ thống kiểm tra idempotency theo `(user_id, request_key)`:
   - Nếu đã có booking và `request_fingerprint` trùng khớp, trả về booking hiện có theo A1.
   - Nếu đã có booking nhưng fingerprint khác, từ chối theo A2.
   - Nếu chưa có booking, tiếp tục xử lý yêu cầu mới.
5. Hệ thống bắt đầu transaction và khóa dòng `showtime` theo protocol `Showtime → Booking → Payment attempts` tại `database/database-design.md`, Mục 31.1–31.2.
6. Sau khi lấy khóa, hệ thống đọc lại trạng thái và thời gian hiện tại từ PostgreSQL bằng `clock_timestamp()`. Các quyết định về trạng thái suất chiếu, cutoff, thời hạn giữ ghế và dọn dẹp booking phải dựa trên dữ liệu authoritative sau khi khóa, không dựa trên dữ liệu đã đọc trước đó.
7. Hệ thống kiểm tra lại điều kiện đặt vé: suất chiếu còn hợp lệ, còn đủ thời gian giữ ghế, tất cả ghế thuộc đúng phòng, ghế có `status = 'active'` và không có phân bổ hiệu lực (`held` hoặc `sold`) cho suất chiếu đó.
8. Hệ thống thực hiện dọn dẹp inline đối với booking `pending` đã quá `booking.expires_at` của cùng suất chiếu, nhưng chỉ khi booking không còn payment attempt ở trạng thái `created`, `pending` hoặc `unknown`. Việc chuyển booking sang `expired` và các detail sang `released` phải diễn ra trong cùng transaction, theo quy tắc UC-03. Không gọi API cổng thanh toán trong transaction.
9. Nếu ghế yêu cầu vẫn bị chiếm bởi booking quá hạn có payment attempt đang mở, hệ thống không giải phóng ghế theo đường inline. Yêu cầu hiện tại bị từ chối an toàn theo A7; Customer có thể thử lại sau khi UC-03 xử lý booking cũ.
10. Hệ thống khóa các dòng danh mục giá cần thiết theo thứ tự ID, tính giá ở Backend theo công thức đã thống nhất tại `BR-BOOK-005`:
    - `base_amount = base_price × capacity_units_snapshot`
    - Phụ thu được tính một lần cho mỗi `booking_detail`.
    - `discount_amount = round(base_amount × discount_percent / 100, 0)`
    - `unit_price = base_amount + surcharge - discount_amount`
    - `total_amount = Σ unit_price` và phải lớn hơn 0 theo `OQ-06 Option B`.

    Giá và mức giảm do client gửi, nếu có, không được dùng làm nguồn tính giá cuối cùng.
11. Hệ thống tạo một booking với `status = pending`, ghi nhận `created_at` và đặt `expires_at = created_at + 5 phút`. Đồng thời, hệ thống tạo đầy đủ các `booking_detail` với `allocation_status = held` và snapshot ghế, loại vé, sức chứa và giá tương ứng.
12. Nếu có xung đột phân bổ ghế, bao gồm vi phạm partial unique index `ux_booking_detail_active_seat`, transaction bị rollback toàn bộ theo A3. Không phụ thuộc vào điều kiện thời gian động như `now()` trong partial unique index.
13. Hệ thống commit transaction và trả về `booking_code`, danh sách ghế, tổng tiền và `expires_at`.
14. **[Customer]** Sau khi nhận được phản hồi giữ ghế thành công, giao diện hiển thị thanh thông tin đặt vé và bộ đếm ngược dựa trên `expires_at` do Backend trả về. Khi Customer nhấn **Đặt vé**, Frontend chuyển đến Checkout bằng booking đã tạo; không gửi yêu cầu tạo booking mới chỉ vì chuyển trang.

### 5.6. Luồng thay thế và ngoại lệ

- **A1 — Replay cùng key, cùng nội dung:** Trả về booking đã tồn tại, không tạo booking mới. Phản hồi thể hiện trạng thái hiện tại của booking; nếu booking đã `expired`, `confirmed` hoặc ở trạng thái khác, không được giả định ghế vẫn đang được giữ.
- **A2 — Cùng key, khác fingerprint:** Từ chối yêu cầu vì xung đột nội dung. Không sửa booking cũ hoặc âm thầm sử dụng payload mới.
- **A3 — Ghế bị chiếm do xử lý đồng thời:** Nếu một hoặc nhiều ghế đã có phân bổ hiệu lực bởi booking khác, rollback toàn bộ yêu cầu và trả về thông tin ghế không khả dụng nếu có thể xác định. Không giữ một phần ghế. Với hai yêu cầu cạnh tranh cùng một ghế, tối đa một yêu cầu được commit thành công; cơ chế bảo vệ cuối cùng là partial unique index, không phải chỉ dựa vào truy vấn kiểm tra trước khi INSERT.
- **A4 — Lỗi ghi dữ liệu giữa chừng:** Rollback toàn bộ transaction. Không để lại booking hoặc phân bổ ghế một phần. Bất biến yêu cầu booking thành công phải có ít nhất một detail cần được bảo đảm ở tầng transaction hoặc cơ chế kiểm tra tương ứng.
- **A5 — Suất chiếu không còn nhận đặt:** Từ chối yêu cầu nếu suất chiếu đã `cancelled`, `in_progress`, `completed`, vượt cutoff hoặc không còn đủ thời gian giữ ghế theo quy tắc đã thống nhất. Trả về lý do phù hợp với tình huống.
- **A6 — Hai request cùng key được gửi đồng thời:** Ràng buộc UNIQUE `(user_id, request_key)` bảo đảm không tạo hai booking cùng key. Request thua cuộc phải đọc lại booking đã commit và so sánh fingerprint: trùng thì xử lý như A1; khác thì xử lý như A2. Cơ chế này phải xử lý cả trường hợp lỗi UNIQUE và trường hợp transaction bị rollback do xung đột.
- **A7 — Ghế thuộc booking quá hạn nhưng còn payment attempt mở:** Không giải phóng booking inline và không tạo booking mới chiếm cùng ghế. Trả về kết quả ghế không khả dụng. UC-03 chịu trách nhiệm xử lý tiếp, bao gồm việc truy vấn cổng thanh toán ngoài transaction khi cần.
- **A8 — Client chưa nhận được phản hồi sau khi transaction đã commit:** Client có thể gửi lại cùng `request_key` và payload. Hệ thống phải xử lý theo A1 nếu booking đã được tạo, tránh tạo booking thứ hai chỉ vì timeout hoặc mất phản hồi.
- **A9 — Không thể xác định chắc chắn kết quả ghi dữ liệu:** Hệ thống không được mặc định transaction thất bại chỉ dựa trên việc client mất kết nối. Client phải dùng lại `request_key` để xác định kết quả; việc kiểm tra lại phải tuân theo cơ chế idempotency và trạng thái dữ liệu authoritative.

### 5.7. Hậu điều kiện

**Thành công**

- Có đúng một booking được tạo cho `request_key`, ở trạng thái `pending`.
- Có đầy đủ N `booking_detail` với `allocation_status = held`, trong đó N ≥ 1.
- Giá từng detail và tổng tiền được tính ở Backend và lưu theo snapshot đã chốt.
- `expires_at` được xác định từ `created_at` theo thời hạn giữ ghế 5 phút.
- Các ghế được giữ không khả dụng cho booking khác của cùng suất chiếu trong thời gian phân bổ còn hiệu lực.

**Thất bại**

- Nếu transaction chưa commit, không có booking hoặc detail mới được tạo một phần bởi yêu cầu thất bại.
- Không có ghế nào bị giữ một phần bởi yêu cầu thất bại.
- Các booking và phân bổ đã tồn tại trước yêu cầu không bị thay đổi ngoài những thay đổi dọn dẹp hợp lệ theo quy tắc UC-03.
- Với lỗi không xác định được kết quả commit, client phải truy vấn lại bằng cùng `request_key`; không được mặc định rằng dữ liệu chắc chắn đã rollback.

### 5.8. Quy tắc nghiệp vụ và ràng buộc liên quan

- Quy tắc nghiệp vụ: `BR-BOOK-001..007`, `BR-BOOK-010`, `BR-ST-003`, `OQ-06 Option B`.
- Câu hỏi mở liên quan: `OQ-02`, `OQ-UC-05`. (Mã `OQ-UC-01` không còn cần thiết — chỉ bán khi `scheduled` theo `BR-ST-003` là quy tắc đã chốt; xem ghi chú lịch sử tại Mục 5.11.)
- Ràng buộc dữ liệu: `uq_booking_user_request_key`, `ux_booking_detail_active_seat`, `ck_booking_total_amount_positive`.
- Giao thức kỹ thuật: `database/database-design.md`, Mục 31.1–31.2; công thức giá tham chiếu `BR-BOOK-005` và các mục schema tương ứng.

### 5.9. Tiêu chí nghiệm thu

- **AC-UC01-01 — Thành công:** Yêu cầu hợp lệ tạo một booking `pending` và đầy đủ N detail `held`; tổng tiền đúng theo công thức Backend.
- **AC-UC01-02 — Idempotent replay:** Gửi lại cùng key và cùng fingerprint trả về cùng booking, không tăng số booking.
- **AC-UC01-03 — Conflict:** Cùng key nhưng fingerprint khác bị từ chối; booking cũ không thay đổi.
- **AC-UC01-04 — Race:** Hai yêu cầu đồng thời tranh cùng ghế không thể cùng commit thành công.
- **AC-UC01-05 — All-or-nothing:** Yêu cầu gồm ghế đã bị chiếm và ghế còn trống không được giữ một phần.
- **AC-UC01-06 — Invalid state:** Đặt vé khi suất chiếu bị hủy, đang diễn ra, đã hoàn tất hoặc vượt điều kiện cutoff/thời hạn hợp lệ bị từ chối.
- **AC-UC01-07 — Không tin giá client:** Giá hoặc mức giảm giả mạo từ client không làm thay đổi giá được Backend tính.
- **AC-UC01-08 — Inline cleanup không gọi mạng:** Khi booking quá hạn có payment `pending` hoặc `unknown`, luồng tạo booking không gọi API cổng thanh toán bên trong transaction; ghế vẫn không khả dụng cho đến khi quy trình xử lý booking cũ hoàn tất.
- **AC-UC01-09 — Không giải phóng payment chưa chốt trái quy tắc:** Booking quá hạn có payment `unknown` không bị giải phóng chỉ bởi inline cleanup.
- **AC-UC01-10 — Rollback toàn bộ:** Lỗi trong transaction không để lại booking hoặc detail mới ở trạng thái dở dang.
- **AC-UC01-11 — Countdown sau xác nhận:** Frontend chỉ hiển thị bộ đếm giữ ghế sau khi Backend xác nhận booking thành công và phải dùng `expires_at` do Backend trả về.
- **AC-UC01-12 — Checkout dùng booking hiện có:** Khi Customer nhấn Đặt vé, Checkout sử dụng booking đã tạo ở UC-01, không tạo booking trùng lặp.
- **AC-UC01-13 — Retry sau timeout:** Gửi lại cùng key sau khi client không nhận được phản hồi không tạo thêm booking nếu yêu cầu ban đầu đã commit.

### 5.10. UC liên quan

- **UC-02:** Xử lý webhook thanh toán.
- **UC-03:** Giải phóng booking hết hạn.
- **UC-04:** Đối soát payment.

### 5.11. Câu hỏi mở

> Lưu ý lịch sử (đã giải quyết): bản nháp trước đây của UC-01 cho phép đặt vé khi suất `in_progress` và gán vấn đề này cho mã `OQ-UC-01`. Thiết kế đã chốt tại `BR-ST-003` chỉ bán khi `scheduled` và chưa đóng bán; `FR-BOOK-001` yêu cầu kiểm tra lại authoritative theo `BR-ST-003` khi tạo booking. Mã `OQ-UC-01` không tồn tại trong bảng OQ của `business-system-analysis.md` và không còn cần thiết — chỉ bán khi `scheduled` là quy tắc đã chốt, được áp dụng tại Mục 5.4, 5.6 (A5), 11.3, 11.6.

- **OQ-02:** Xác nhận quy tắc áp dụng `booking_closes_at` và điều kiện phải còn đủ thời gian giữ ghế 5 phút trước mốc đóng bán hoặc giờ bắt đầu.
- **OQ-UC-05:** Quyết định có chuẩn hóa thứ tự danh sách ghế trước khi tính `request_fingerprint` hay không.
---

## 6. UC-02: Xử lý Webhook thanh toán (Process Payment Webhook)

### 6.1. Mục tiêu

Ghi nhận kết quả thanh toán từ cổng thanh toán một cách idempotent, bảo đảm sự nhất quán giữa trạng thái tài chính và trạng thái booking.

Khi payment thành công, được xác minh và booking vẫn đáp ứng đầy đủ điều kiện áp dụng, hệ thống phải cập nhật payment, xác nhận booking, chuyển phân bổ ghế sang `sold` và phát hành vé trong **một transaction cơ sở dữ liệu duy nhất**.

Nếu payment đã thành công nhưng booking không còn đủ điều kiện áp dụng, hệ thống phải bảo toàn bằng chứng tài chính, không tự chiếm lại ghế hoặc phát hành vé, đồng thời ghi nhận trạng thái đối soát phù hợp.

### 6.2. Tác nhân

- **Tác nhân chính:** Payment Gateway.
- **Tác nhân phụ:** Backend Payment Service, Backend Booking/Payment Worker và PostgreSQL.

### 6.3. Trigger

Một trong các sự kiện sau xảy ra:

- Payment Gateway gửi webhook thông báo kết quả giao dịch.
- Worker nhận và xử lý một `payment_event` đang ở trạng thái `received`.

### 6.4. Tiền điều kiện

- Tồn tại payment attempt có `merchant_request_id` tương ứng. Payment có thể ở trạng thái `created`, `pending`, `unknown` hoặc `succeeded`; trường hợp đã `succeeded` được xử lý theo các luồng idempotency và sự kiện đến sai thứ tự.
- Webhook phải được xác minh nguồn và chữ ký/checksum theo hợp đồng của cổng thanh toán. Cổng cụ thể chưa được chốt tại `OQ-01`.
- Worker chỉ xử lý event đã được ingress xác minh và lưu bền vững. Worker không được coi dữ liệu webhook chưa xác minh là bằng chứng thanh toán hợp lệ.

### 6.5. Phân biệt các khái niệm nghiệp vụ

- **Payment succeeded:** Cổng xác nhận đã thu tiền và hệ thống đã xác minh các thông tin định danh giao dịch. Hệ thống ghi `payment.status = succeeded` cùng các thông tin tài chính cần thiết như `paid_at`, `verified_at`, `received_amount`, `received_currency` và `provider_transaction_id` theo schema.
- **Payment applied:** Payment thành công được áp dụng cho một booking đủ điều kiện; hệ thống ghi nhận `applied_at`. Mỗi booking chỉ được có tối đa một payment applied theo `ux_payment_applied_booking`.
- **Booking confirmed:** Booking chuyển từ `pending` sang `confirmed` và được ghi nhận `confirmed_at`. Việc này phải nằm trong cùng transaction với thao tác áp dụng payment.
- **Tickets issued:** Hệ thống phát hành đủ số vé theo `capacity_units_snapshot` của từng detail. Ví dụ, một ghế thường tạo một vé; ghế Couple tạo số vé theo snapshot sức chứa đã chốt. Việc phát hành phải idempotent và thuộc cùng transaction với thao tác áp dụng payment.
- **Payment succeeded không đồng nghĩa payment applied.** Nếu booking đã hết hạn, bị hủy hoặc không còn đủ điều kiện, payment vẫn phải phản ánh kết quả tài chính thực tế. Không tự động xác nhận booking hoặc phát hành vé trong trường hợp này.
- Webhook và cơ sở dữ liệu không thể được bảo đảm nguyên tử trong một transaction xuyên qua cổng thanh toán. Tính nguyên tử chỉ áp dụng cho các cập nhật dữ liệu thuộc cùng transaction PostgreSQL.

### 6.6. Luồng chính

**Transaction 1 — Tiếp nhận webhook**

1. Payment Gateway gửi webhook đến Backend Payment Service.
2. Hệ thống xác minh nguồn gửi và chữ ký/checksum tại ingress bằng adapter tương ứng với cổng thanh toán.
3. Nếu xác minh thành công, hệ thống lưu event vào `payment_event` với `source = webhook`, `status = received`, `event_key`, `event_type` và `payload_summary` đã lọc. Không lưu secret hoặc dữ liệu thẻ nhạy cảm trong payload.
4. Hệ thống commit transaction tiếp nhận trước khi gửi ACK cho cổng thanh toán.
5. Nếu `(payment_id, event_key)` đã tồn tại, hệ thống nhận diện webhook trùng và không tạo event thứ hai; phản hồi theo hợp đồng của cổng thanh toán.

**Transaction 2 — Xử lý nghiệp vụ**

6. Worker claim event bằng cơ chế khóa dòng phù hợp, chẳng hạn `SELECT ... FOR UPDATE SKIP LOCKED`, theo thiết kế xử lý hàng đợi tại schema. Nếu event đã `processed`, worker kết thúc mà không lặp lại tác động nghiệp vụ.
7. Hệ thống xác định payment và booking tương ứng bằng `merchant_request_id` cùng các định danh giao dịch đã lưu. Hệ thống đối chiếu số tiền và tiền tệ nhận được với số tiền và tiền tệ của payment.
8. Nếu cần truy vấn trạng thái trực tiếp từ cổng thanh toán, hệ thống thực hiện truy vấn bên ngoài transaction nghiệp vụ. Sau khi có kết quả, hệ thống mở transaction, khóa các bản ghi theo protocol đã thống nhất và đọc lại trạng thái hiện tại trước khi quyết định cập nhật.
9. Nếu kết quả tài chính được xác minh là thành công, hệ thống ghi nhận `payment.status = succeeded` và các trường tài chính thực tế. Cập nhật phải có điều kiện để không cho phản hồi khởi tạo thanh toán đến muộn ghi đè kết quả đã được webhook xác nhận.
10. Hệ thống kiểm tra điều kiện áp dụng theo `BR-PAY-004`:
    - Booking vẫn ở trạng thái `pending`.
    - Booking chưa vượt `booking.expires_at`.
    - Booking chưa vượt `booking_closes_at` nếu mốc này được áp dụng theo quy tắc đã chốt.
    - Showtime vẫn ở trạng thái `scheduled`.
    - Tất cả detail liên quan vẫn ở trạng thái `held`.
    - Payment chưa được áp dụng và booking chưa có payment khác được áp dụng.
    - Số tiền, tiền tệ và định danh giao dịch phù hợp với payment cần xử lý.
11. **Nếu đủ điều kiện áp dụng**, trong cùng transaction, hệ thống:
    - Ghi nhận `applied_at` cho payment.
    - Chuyển booking từ `pending` sang `confirmed` và ghi `confirmed_at`.
    - Chuyển toàn bộ detail từ `held` sang `sold`.
    - Phát hành đủ vé theo `capacity_units_snapshot`; nếu cơ chế phát hành hỗ trợ retry, chỉ tạo các vé còn thiếu và bảo đảm không tạo trùng.
    - Đánh dấu event `processed`.
12. **Nếu không đủ điều kiện áp dụng**, hệ thống giữ nguyên bằng chứng payment thành công, không đặt `applied_at`, không xác nhận booking, không chuyển detail sang `sold` và không phát hành vé. Hệ thống ghi nhận kết quả đối soát phù hợp:
    - Booking đã hết hạn hoặc bị hủy: `refund_required` nếu xác định cần hoàn tiền.
    - Sai số tiền, sai tiền tệ hoặc chưa thể xác định kết quả: `needs_review`.
    - Ghế đã được giải phóng hoặc phân bổ cho booking khác: không chiếm lại ghế; chuyển xử lý theo chính sách đối soát đã thống nhất.
13. Hệ thống đánh dấu event `processed` trong cùng transaction với các cập nhật nghiệp vụ tương ứng, rồi commit.
14. Worker hoàn tất xử lý. ACK webhook thuộc Transaction 1 đã được gửi sau khi event được lưu bền vững; việc ACK không được nhầm với kết quả xác nhận booking.

### 6.7. Luồng thay thế và ngoại lệ

- **A1 — Chữ ký hoặc nguồn không hợp lệ:** Không đưa event vào hàng xử lý nghiệp vụ và không thay đổi payment, booking, phân bổ ghế hoặc ticket. Có thể ghi nhận sự kiện bảo mật bằng cơ chế log riêng, không coi bản ghi đó là payment event hợp lệ. ACK/NACK tuân theo hợp đồng cổng thanh toán; mã HTTP cụ thể chờ `OQ-01`.

- **A2 — Webhook trùng hoặc nhiều event đại diện cùng giao dịch:** Nếu `(payment_id, event_key)` đã tồn tại, không tạo event trùng. Nếu hai event có `event_key` khác nhau nhưng đại diện cùng giao dịch, hệ thống có thể lưu cả hai; tác động tài chính và nghiệp vụ chỉ được áp dụng một lần. Nếu hai event mâu thuẫn về kết quả của cùng giao dịch, không tự chọn một kết quả; chuyển sang `needs_review`.

- **A3 — Payment thành công nhưng booking đã hết hạn hoặc ghế đã được phân bổ lại:** Ghi nhận payment `succeeded`, không đặt `applied_at`, không xác nhận booking và không phát hành vé. Đặt `reconciliation_status = refund_required` nếu đã xác định cần hoàn tiền; dùng `needs_review` nếu còn thiếu thông tin để quyết định. Không chiếm lại ghế đã giải phóng hoặc bán cho booking khác.

- **A4 — Giao dịch thành công nhưng sai số tiền hoặc tiền tệ:** Nếu cổng xác nhận đã thu tiền thật và kết quả được xác minh, hệ thống vẫn ghi nhận `payment.status = succeeded` cùng số tiền và tiền tệ thực nhận. Nếu `received_amount` khác `amount` hoặc `received_currency` khác `currency`, không áp dụng payment; đặt `reconciliation_status = needs_review`. Không sửa `booking.total_amount` để khớp với số tiền thực nhận. Trường hợp cổng xác nhận giao dịch thất bại rõ ràng được xử lý theo kết quả thất bại, không được nhầm với giao dịch đã thu tiền nhưng sai số tiền.

- **A5 — Kết quả giao dịch chưa xác định:** Khi timeout hoặc mất kết nối khiến hệ thống chưa thể xác định kết quả, ghi nhận trạng thái `unknown` theo quy tắc thanh toán và lên lịch đối soát qua UC-04. Không mặc định giao dịch thất bại và không tạo attempt mới trái với `BR-PAY-002`.

- **A6 — Transaction nghiệp vụ thất bại giữa chừng:** Rollback toàn bộ cập nhật nghiệp vụ. Event không được đánh dấu `processed` nếu các thay đổi tương ứng chưa commit. Event có thể giữ `received` để retry hoặc chuyển sang `failed` theo chính sách retry đã triển khai; việc retry phải idempotent và có cơ chế theo dõi lỗi.

- **A7 — Sự kiện đến sai thứ tự:** Nếu event thất bại đến sau khi payment đã `succeeded` và được áp dụng, không hạ trạng thái payment hoặc hoàn tác booking chỉ dựa trên event đến muộn. Lưu dấu vết sự kiện và chuyển mâu thuẫn sang đối soát khi cần.

- **A8 — Booking đã có payment khác được áp dụng:** Payment mới được xác minh thành công nhưng không được áp dụng lần thứ hai cho booking. Ghi nhận kết quả tài chính và đặt `needs_review` hoặc `refund_required` tùy tình huống; không phát hành thêm vé.

- **A9 — Booking quá hạn nhưng payment vẫn đang mở trong grace period:** Xử lý theo UC-03 và quyết định đã chốt tại `OQ-07`. Không tự động giải phóng ghế khi điều kiện grace period chưa thỏa. Nếu payment thành công và booking vẫn đáp ứng toàn bộ điều kiện áp dụng, xử lý theo luồng chính; nếu booking đã được giải phóng hoặc không còn đủ điều kiện, xử lý theo A3.

### 6.8. Hậu điều kiện

**Thành công — payment được áp dụng**

- Payment có `status = succeeded` và `applied_at` được ghi nhận.
- Booking có `status = confirmed`.
- Tất cả detail liên quan có `allocation_status = sold`.
- Đủ số vé được phát hành theo snapshot sức chứa.
- Event có `status = processed`.
- Các cập nhật trên được commit trong cùng transaction.

**Thành công — payment không được áp dụng**

- Kết quả tài chính thực tế được lưu lại.
- Booking và detail không bị xác nhận hoặc thay đổi trái quy tắc.
- Không phát hành vé và không chiếm lại ghế.
- `reconciliation_status` phản ánh tình huống cần xử lý.
- Event được đánh dấu `processed` khi các cập nhật nghiệp vụ tương ứng đã commit thành công.

**Thất bại hoặc cần retry**

- Không để lại trạng thái cập nhật một phần trong transaction đã rollback.
- Event không bị đánh dấu `processed` trước khi các thay đổi tương ứng được commit.
- Retry không được tạo payment applied hoặc ticket trùng.

### 6.9. Quy tắc nghiệp vụ và ràng buộc liên quan

- Quy tắc nghiệp vụ: `BR-PAY-001..008`, `FR-PAY-002..005`, `AC-PAY-001..005`.
- Ràng buộc dữ liệu: `uq_payment_event_payment_key`, `ux_payment_applied_booking`, `ux_payment_open_booking`, `ux_payment_provider_transaction`, `ck_payment_applied`.
- UC liên quan: UC-01, UC-03, UC-04 và UC-04A.

### 6.10. Tiêu chí nghiệm thu

- **AC-UC02-01 — Happy path:** Payment hợp lệ được áp dụng; booking được xác nhận, detail chuyển sang `sold`, đủ vé được phát hành và event được đánh dấu `processed` trong cùng transaction.
- **AC-UC02-02 — Webhook trùng:** Gửi lại cùng event không tạo thêm tác động tài chính, booking hoặc vé.
- **AC-UC02-03 — Chữ ký sai:** Webhook không hợp lệ không thay đổi trạng thái nghiệp vụ và không đi vào hàng xử lý hợp lệ.
- **AC-UC02-04 — Booking hết hạn:** Payment đã thành công nhưng booking không còn đủ điều kiện thì không phát hành vé; kết quả đối soát được ghi nhận phù hợp.
- **AC-UC02-05 — Sai tiền:** Payment đã thu tiền nhưng sai số tiền hoặc tiền tệ phải được bảo toàn kết quả tài chính và chuyển `needs_review`.
- **AC-UC02-06 — Retry sau lỗi:** Retry sau khi transaction rollback không tạo vé thừa hoặc payment applied trùng.
- **AC-UC02-07 — Event sai thứ tự:** Event đến muộn không làm hạ trạng thái payment đã được xác minh và áp dụng.
- **AC-UC02-08 — Hai worker cùng event:** Chỉ một worker thực hiện tác động nghiệp vụ; worker còn lại không tạo cập nhật trùng.
- **AC-UC02-09 — Một booking, một payment applied:** Hai payment thành công cạnh tranh cùng một booking không thể cùng được áp dụng.
- **AC-UC02-10 — Event chỉ processed sau commit:** Nếu transaction nghiệp vụ rollback, event không được ghi nhận là đã xử lý thành công.
- **AC-UC02-11 — Không gọi cổng trong transaction:** Truy vấn trạng thái giao dịch qua API cổng phải thực hiện ngoài transaction nghiệp vụ; sau đó hệ thống khóa và đọc lại dữ liệu trước khi áp dụng kết quả.

### 6.11. Câu hỏi mở

- **OQ-01:** Cổng thanh toán cụ thể, hợp đồng ACK/NACK, cơ chế truy vấn trạng thái và các khả năng hoàn tiền. của quy trình đối soát và cách xử lý event mâu thuẫn.
- **OQ-UC-06:** Có cần lưu thêm bằng chứng xác minh webhook hay không, trong bối cảnh schema hiện tại chưa có trường chuyên biệt để lưu chứng cứ xác minh có thể kiểm tra lại.

---

## 7. UC-03: Giải phóng đơn hàng hết hạn (Release Expired Bookings)

### 7.1. Mục tiêu

Giải phóng ghế của các booking `pending` đã quá thời hạn giữ ghế sau khi xác định không còn payment attempt cần được bảo vệ theo chính sách grace period và đối soát.

Việc giải phóng phải bảo đảm tính nhất quán giữa trạng thái booking và các detail, không giải phóng booking đã được xác nhận, không kết luận payment thất bại chỉ vì hết thời gian giữ ghế và không để ghế bị bán trùng.

### 7.2. Tác nhân

- **Tác nhân chính:** System Worker chạy định kỳ.
- **Tác nhân phụ:** PostgreSQL; Payment Gateway khi cần truy vấn trạng thái giao dịch ngoài transaction.

### 7.3. Trigger

- Worker chạy định kỳ để tìm booking cần xử lý. Chu kỳ 15–30 giây là giá trị đề xuất cấu hình triển khai theo `database/database-design.md`, Mục 27.2.
- UC-01 thực hiện dọn dẹp inline đối với booking quá hạn theo các điều kiện được quy định tại UC-01.

### 7.4. Tiền điều kiện

Booking chỉ được giải phóng khi đồng thời thỏa mãn các điều kiện sau:

1. `booking.status = pending`.
2. `booking.expires_at` đã qua theo thời gian DB thực được đọc lại sau khi lấy khóa.
3. Không còn payment attempt ở trạng thái `created`, `pending` hoặc `unknown`; ngoại lệ là các trường hợp đang được xử lý theo grace period và quy trình đối soát quy định tại mục này.

**Booking quá hạn không đồng nghĩa với booking đủ điều kiện giải phóng.** Việc hết hạn giữ ghế không tự chứng minh rằng giao dịch thanh toán đã thất bại.

### 7.5. Luồng chính

1. **[Worker]** Quét các booking ứng viên thông qua `ix_booking_expiry`, tương ứng với các booking `pending` có `expires_at` đến hạn. Việc quét ban đầu không giữ khóa booking trong thời gian dài.
2. Worker bắt đầu quy trình xử lý booking theo protocol khóa `Showtime → Booking → Payment attempts` được mô tả tại `database/database-design.md`, Mục 31.1. Không khóa booking trước rồi quay lại khóa showtime nếu điều đó vi phạm thứ tự khóa đã thống nhất.
3. Sau khi lấy các khóa cần thiết, hệ thống đọc lại trạng thái booking, các payment attempt liên quan và thời gian hiện tại bằng `clock_timestamp()`. Nếu booking không còn `pending`, đã được xử lý hoặc chưa thực sự quá hạn, hệ thống bỏ qua theo A2.
4. Hệ thống phân loại các payment attempt hiện có:
   - Không có attempt hoặc chỉ có attempt `failed`/`cancelled`: tiếp tục bước 5.
   - Có attempt `created`, `pending` hoặc `unknown`: xử lý theo bảng quyết định tại Mục 7.6.
   - Có payment `succeeded` chưa `applied`: chuyển sang xử lý phối hợp với UC-02; không tự giải phóng ghế.
   - Có payment `succeeded` đã `applied`: booking phải được xử lý theo trạng thái đã áp dụng; không giải phóng booking đã xác nhận.
5. Khi không còn payment attempt cần bảo vệ và booking vẫn đủ điều kiện giải phóng, hệ thống cập nhật trong cùng transaction:
   - Chuyển booking từ `pending` sang `expired`.
   - Chuyển toàn bộ detail từ `held` sang `released`.
   - Ghi `released_at` theo schema.
6. Hệ thống commit transaction. Các thay đổi trạng thái booking và detail phải cùng thành công hoặc cùng rollback.

### 7.6. Bảng quyết định theo trạng thái payment

| Trạng thái payment | Điều kiện | Hành động | Gọi cổng? |
| :--- | :--- | :--- | :--- |
| Không có attempt hoặc chỉ có `failed`/`cancelled` | Booking `pending` và đã quá `expires_at` | Chuyển booking sang `expired`, detail sang `released` | Không |
| `created`/`pending`/`unknown` | Chưa hết grace period | Bỏ qua; giữ nguyên booking và detail | Không |
| `created`/`pending`/`unknown` | Đã hết grace period và cần xác định kết quả giao dịch | Query/Check Status API ngoài transaction; sau đó mở transaction mới và đọc lại trạng thái trước khi cập nhật | Có |
| `succeeded`, chưa `applied_at` | Booking vẫn `pending` | Chuyển sang UC-02 để đánh giá điều kiện áp dụng; không tự release | Không bắt buộc |
| `succeeded` với `reconciliation_status = needs_review` | Kết quả cần xác minh thêm | Đưa vào hàng chờ xử lý/giám sát; không tự release khi chưa có quyết định phù hợp | Theo nhu cầu đối soát |
| `succeeded` với `reconciliation_status = refund_required` | Booking đã `expired` hoặc ghế đã được giải phóng | Không reclaim ghế; xử lý theo UC-04A khi phạm vi được kích hoạt | Không bắt buộc |
| `succeeded` và đã có `applied_at` | Booking đã được áp dụng thành công | Không release; booking và vé được xử lý theo UC-02 | Không |

#### Quy trình khi cần truy vấn cổng

Khi booking có payment attempt mở và đã vượt grace period:

1. Kết thúc transaction đang giữ khóa trước khi gọi API cổng thanh toán.
2. Thực hiện truy vấn trạng thái giao dịch ngoài transaction.
3. Mở transaction mới, lấy khóa theo protocol đã thống nhất và đọc lại booking, payment cùng thời gian hiện tại.
4. Chỉ cập nhật dựa trên kết quả cổng đã xác minh và trạng thái DB mới nhất:
   - **Cổng xác nhận chưa thu tiền hoặc giao dịch thất bại:** Chỉ kết thúc attempt và giải phóng booking khi kết quả đủ chắc chắn, việc cập nhật phù hợp với trạng thái hiện tại và các điều kiện release vẫn thỏa mãn.
   - **Cổng xác nhận đã thu tiền:** Ghi nhận kết quả tài chính qua UC-02. Nếu booking vẫn đủ điều kiện áp dụng, UC-02 xử lý việc áp dụng; nếu booking đã hết hạn hoặc không còn đủ điều kiện, ghi nhận `refund_required` hoặc `needs_review` theo tình huống.
   - **Cổng không phản hồi hoặc kết quả không xác định:** Không giả định payment thất bại. Giữ trạng thái phù hợp, ghi nhận lịch sử xử lý và đưa vào hàng chờ đối soát. Việc tiếp tục giữ ghế phải chịu sự giám sát và chính sách xử lý đã thống nhất, không để booking mắc kẹt vô thời hạn mà không có cảnh báo.

### 7.7. Luồng thay thế và ngoại lệ

- **A1 — Race với UC-02:** UC-02 và UC-03 phải tuân thủ cùng protocol khóa. Nếu UC-02 áp dụng payment trước và commit booking `confirmed`, UC-03 đọc lại trạng thái rồi bỏ qua. Nếu UC-03 release trước và commit booking `expired`, UC-02 không được áp dụng payment vào booking đã hết hạn hoặc reclaim ghế đã giải phóng; xử lý theo UC-02 A3.

- **A1b — Phải gọi cổng thanh toán:** Không giữ khóa DB trong lúc chờ API cổng. Kết quả truy vấn phải được xác minh và sử dụng trong một transaction mới sau khi đọc lại các trạng thái liên quan. Nếu trạng thái đã thay đổi trong thời gian gọi cổng, hệ thống phải đánh giá lại điều kiện thay vì áp dụng mù quáng kết quả cũ.

- **A2 — Booking đã thay đổi trạng thái hoặc chưa đủ điều kiện:** Nếu booking không còn `pending` hoặc `expires_at` chưa qua theo thời gian DB hiện tại, bỏ qua mà không thực hiện chuyển trạng thái giải phóng.

- **A3 — Payment thành công sau khi ghế đã được giải phóng:** Không reclaim ghế và không phát hành vé cho booking đã `expired`. Ghi nhận payment `succeeded` cùng `refund_required` hoặc `needs_review` theo UC-02; không thay đổi booking khác đang giữ ghế.

- **A4 — Nhiều worker xử lý đồng thời:** Khóa dòng và đọc lại trạng thái bảo đảm chỉ một worker thực hiện chuyển trạng thái hợp lệ. Worker còn lại bỏ qua booking đã được xử lý. Việc retry phải idempotent.

- **A5 — Lỗi DB giữa chừng:** Rollback toàn bộ transaction. Không để booking `expired` nhưng detail vẫn `held` do một phần transaction đã được ghi nhận. Worker có thể thử lại ở chu kỳ tiếp theo.

- **A6 — Worker chậm hoặc ngừng hoạt động:** Booking quá hạn có thể vẫn tồn tại ở trạng thái `pending` và detail `held` cho đến khi worker hoạt động lại hoặc UC-01 thực hiện dọn dẹp inline hợp lệ. Trong thời gian này, ghế không được coi là đã giải phóng; các ràng buộc dữ liệu vẫn phải ngăn việc bán trùng.

- **A7 — Phân biệt quá hạn với đã giải phóng:** Booking có `expires_at` đã qua nhưng chưa có transaction chuyển trạng thái thành công vẫn có thể ở trạng thái `pending` với detail `held`. Frontend và các nghiệp vụ kiểm tra ghế không được suy luận rằng ghế đã khả dụng chỉ từ `expires_at`.

### 7.8. Hậu điều kiện

**Thành công — giải phóng booking**

- Booking có `status = expired`.
- Tất cả detail liên quan chuyển sang `allocation_status = released` và có `released_at` theo schema.
- Ghế có thể được đặt lại nếu không có phân bổ hiệu lực khác cho cùng suất chiếu.
- Lịch sử booking, detail và payment được giữ nguyên để phục vụ truy vết và đối soát.

**Bỏ qua — grace period hoặc cần đối soát**

- Booking và detail giữ nguyên trạng thái hiện tại.
- Payment không bị kết luận thất bại chỉ vì booking quá hạn.
- Hệ thống ghi nhận thông tin cần thiết để retry, giám sát hoặc chuyển xử lý thủ công theo chính sách đã thống nhất.

**Thất bại**

- Transaction bị rollback hoàn toàn.
- Không tồn tại trạng thái chuyển đổi một phần giữa booking và detail.
- Lần chạy tiếp theo có thể xử lý lại một cách idempotent.

### 7.9. Quy tắc nghiệp vụ và ràng buộc liên quan

- Quy tắc nghiệp vụ: `BR-BOOK-004`, `BR-PAY-002`, `BR-PAY-003`, `BR-PAY-007`, `OQ-07`.
- Cơ chế kỹ thuật: `database/database-design.md`, Mục 27.2 và 31.1.
- Ràng buộc dữ liệu: các index liên quan đến booking hết hạn, phân bổ ghế hiệu lực và hàng chờ đối soát theo schema thực tế.
- Nguyên tắc: không sử dụng partial unique index phụ thuộc vào thời gian động như `now()` để tự động xác định ghế đã hết hạn.

### 7.10. Tiêu chí nghiệm thu

- **AC-UC03-01 — Release hợp lệ:** Booking `pending` quá hạn, không còn payment attempt mở và đáp ứng các điều kiện release sẽ chuyển thành `expired`; toàn bộ detail chuyển sang `released`.
- **AC-UC03-02 — Trong grace period:** Booking quá hạn nhưng còn attempt `created`/`pending`/`unknown` và chưa hết grace period không bị release.
- **AC-UC03-03 — Cổng xác nhận chưa thu tiền:** Sau khi kết quả được xác minh và điều kiện được kiểm tra lại, hệ thống có thể kết thúc attempt và release booking theo chính sách đã chốt.
- **AC-UC03-04 — Cổng xác nhận đã thu tiền:** Hệ thống bảo toàn kết quả `succeeded`, không reclaim ghế đã release và không phát hành vé cho booking không còn đủ điều kiện.
- **AC-UC03-05 — Cổng không phản hồi:** Hệ thống không tự kết luận payment thất bại; trạng thái được giữ phù hợp, có log và cơ chế giám sát/đối soát.
- **AC-UC03-06 — Race với UC-02:** Không phát hành vé trùng, không release booking đã được xác nhận và không áp dụng payment vào booking đã `expired`.
- **AC-UC03-07 — Idempotency:** Chạy lại worker không tạo thêm tác động giải phóng hoặc làm hỏng trạng thái booking/detail.
- **AC-UC03-08 — Đặt lại ghế:** Sau khi release thành công, một booking mới có thể giữ ghế qua UC-01 nếu các điều kiện đặt vé khác vẫn hợp lệ.
- **AC-UC03-09 — Atomicity:** Lỗi giữa transaction không để lại booking `expired` với detail còn `held`, hoặc booking `pending` với detail đã `released` do thao tác release chưa hoàn tất.
- **AC-UC03-10 — Không giữ khóa khi gọi cổng:** Mọi lời gọi Query/Check Status API phải diễn ra ngoài transaction đang giữ khóa nghiệp vụ.

### 7.11. UC liên quan

- **UC-01:** Dọn dẹp inline booking quá hạn.
- **UC-02:** Xử lý webhook thanh toán và payment thành công đến muộn.
- **UC-04:** Đối soát payment.
- **UC-04A:** Hoàn tiền khi được kích hoạt theo phạm vi và chính sách đã thống nhất.

### 7.12. Câu hỏi mở

- **OQ-UC-03:** Giá trị grace period, chu kỳ retry và ngưỡng cảnh báo/leo thang khi payment vẫn chưa xác định.
- **OQ-UC-02:** Chính sách đối soát tự động và xử lý kết quả mâu thuẫn.
- **OQ-04:** Phân quyền xử lý thủ công các trường hợp `needs_review` và `refund_required`.

---

## 8. UC-04: Đối soát Payment (Reconciliation) và UC-04A: Hoàn tiền (Refund)

### 8.1. Phân biệt hai quy trình

**Reconciliation** là quy trình xác minh và chốt kết quả thực tế của payment đang ở trạng thái `pending`/`unknown` hoặc có dữ liệu bất thường. Đối soát không trực tiếp chuyển tiền.

**Refund** là quy trình chuyển tiền ngược cho khách hàng sau khi kết quả tài chính đã được xác minh và nghiệp vụ quyết định phải hoàn tiền.

Hai quy trình có vòng đời độc lập:

- Reconciliation xác định kết quả tài chính và định tuyến nghiệp vụ.
- Refund thực hiện việc hoàn tiền và xác minh kết quả hoàn.
- `reconciliation_status = refund_required` có nghĩa là cần hoàn tiền, không có nghĩa tiền đã được hoàn.
- `reconciliation_status = resolved` chỉ được đặt khi hồ sơ đã đáp ứng điều kiện kết thúc tương ứng.

### 8.2. Phạm vi demo

| Hoạt động | Phạm vi | Ghi chú |
| :--- | :--- | :--- |
| Truy vấn payment `created`/`pending`/`unknown` | In scope | Gọi cổng ngoài transaction đang giữ khóa nghiệp vụ |
| Ghi nhận kết quả tài chính đã xác minh | In scope | Bao gồm `succeeded`, `failed` và `cancelled` khi có đủ căn cứ |
| Áp dụng payment theo UC-02 | In scope | Chỉ khi booking còn đủ điều kiện |
| Ghi nhận payment thành công muộn khi ghế đã bán lại | In scope | Bảo toàn kết quả tài chính; không reclaim ghế, không phát hành vé |
| Đưa trường hợp bất thường vào hàng chờ đối soát | In scope | Theo dõi qua `reconciliation_status` và cơ chế giám sát |
| Thực thi Refund API | Out of scope | Chỉ đặc tả hợp đồng hành vi và mô hình dữ liệu |
| Giao diện xử lý `needs_review` thủ công | Deferred | Chờ chốt quyền hạn theo OQ-04 |

Phân loại phạm vi dựa trên `docs/requirements/business-system-analysis.md`, Mục 3.1 và 3.3.

---

### 8.3. UC-04: Đối soát Payment (Payment Reconciliation)

### Mục tiêu

Chốt kết quả thực tế của payment chưa xác định và phát hiện các trường hợp bất thường, từ đó định tuyến đúng:

- Áp dụng payment nếu booking còn đủ điều kiện.
- Ghi nhận `failed`/`cancelled` khi kết quả tương ứng được xác minh rõ ràng.
- Chuyển hồ sơ sang `needs_review` khi cần xác minh hoặc quyết định bổ sung.
- Chuyển sang `refund_required` khi tiền đã thu nhưng không thể áp dụng vào booking và nghiệp vụ yêu cầu hoàn.

### Tác nhân

- **Tác nhân chính:** System Worker.
- **Tác nhân phụ:** Payment Gateway, PostgreSQL.
- **Tác nhân xử lý ngoại lệ:** Manager hoặc nhân sự được phân quyền sau khi OQ-04 được chốt.

### Trigger

- Worker quét payment đến hạn qua cơ chế dự kiến `ix_payment_reconcile_due`, với các trường như `next_reconcile_at`, `id` và điều kiện trạng thái phù hợp.
- Payment được đưa vào hàng chờ `ix_payment_reconciliation_queue` do có `needs_review` hoặc `refund_required`.
- Hệ thống phát hiện kết quả thanh toán mâu thuẫn, giao dịch thành công muộn hoặc sai lệch dữ liệu tài chính.

Tên index và điều kiện lọc phải được đối chiếu với `database-schema.sql` trước khi triển khai.

### Tiền điều kiện

Payment thuộc ít nhất một trường hợp:

1. Trạng thái tài chính chưa chốt, chẳng hạn `created`, `pending` hoặc `unknown`.
2. Có dấu hiệu bất thường cần xác minh.
3. Có `reconciliation_status` cần xử lý tiếp, chẳng hạn `needs_review` hoặc `refund_required`.

Hồ sơ đã được giải quyết không nên tiếp tục được đưa vào hàng chờ nếu không có sự kiện mới cần xử lý.

### Luồng chính

1. **[Worker]** Chọn payment cần đối soát và thu thập thông tin định danh giao dịch.
2. **[Worker]** Gọi Query/Check Status API ngoài transaction đang giữ khóa DB.
3. **[Worker]** Xác minh phản hồi, mở transaction mới và khóa các bản ghi theo protocol đã thống nhất.
4. Đọc lại trạng thái payment và booking hiện tại trước khi ghi kết quả. Chỉ cập nhật khi điều kiện trạng thái vẫn phù hợp; phản hồi cũ không được ghi đè kết quả `succeeded` đã được xác minh theo `BR-PAY-008`.
5. Nếu cổng xác nhận thành công và dữ liệu tài chính khớp:
   - Cập nhật payment sang `succeeded`.
   - Ghi nhận các trường bằng chứng tài chính tương ứng với schema, chẳng hạn `paid_at`, `verified_at`, `received_*` và `provider_transaction_id` nếu có.
   - Đánh giá điều kiện áp dụng theo UC-02.
   - Nếu booking còn đủ điều kiện, chuyển sang UC-02 để áp dụng payment.
   - Nếu booking không còn đủ điều kiện, bảo toàn kết quả tài chính và định tuyến sang `refund_required` hoặc `needs_review`.
6. Nếu cổng xác nhận thất bại hoặc hủy giao dịch rõ ràng:
   - Cập nhật trạng thái payment tương ứng khi kết quả đã được xác minh.
   - Không coi timeout hoặc phản hồi không đầy đủ là bằng chứng thất bại.
7. Nếu không xác định được kết quả:
   - Giữ `unknown` khi phù hợp.
   - Cập nhật `next_reconcile_at` theo chính sách retry.
   - Ghi nhận thông tin lần thử để phục vụ giám sát.
8. Các hồ sơ `needs_review` được chuyển đến quy trình xử lý thủ công khi quyền hạn và quy trình đã được phê duyệt.
9. Chỉ đánh dấu sự kiện đã xử lý hoặc hồ sơ đã giải quyết sau khi các thay đổi nghiệp vụ liên quan được commit thành công.

### Ý nghĩa của `reconciliation_status`

| Trạng thái | Ý nghĩa |
| :--- | :--- |
| `none` | Chưa có bất thường cần xử lý |
| `needs_review` | Cần xác minh hoặc quyết định bổ sung, chẳng hạn sai lệch dữ liệu hoặc kết quả mâu thuẫn |
| `refund_required` | Nghiệp vụ yêu cầu hoàn tiền; chưa có nghĩa tiền đã được hoàn |
| `resolved` | Hồ sơ đã được giải quyết theo điều kiện nghiệp vụ tương ứng |

#### Điều kiện chuyển sang `resolved`

- Với `refund_required`: chỉ chuyển sang `resolved` sau khi refund liên quan được xác minh `succeeded`, hoặc sau một phương án xử lý thay thế được nghiệp vụ cho phép và ghi nhận đầy đủ.
- Với `needs_review`: chỉ chuyển sang `resolved` sau khi người có thẩm quyền đưa ra quyết định và đóng hồ sơ hợp lệ.
- Không chuyển sang `resolved` chỉ vì cổng đã tiếp nhận yêu cầu hoặc refund vẫn đang `pending`/`unknown`.

### Luồng thay thế và ngoại lệ

- **A1 — Cổng timeout hoặc lỗi:** Không tự kết luận payment thất bại. Giữ trạng thái phù hợp, ghi nhận lỗi và lên lịch retry.
- **A2 — Phản hồi mâu thuẫn hoặc đến sai thứ tự:** Nếu một phản hồi cũ báo `failed` sau khi payment đã được xác minh `succeeded`, không hạ trạng thái tài chính đã chốt. Lưu bằng chứng mâu thuẫn và đưa vào `needs_review` khi cần.
- **A3 — Payment đã applied nhưng dữ liệu đối soát lệch:** Đưa vào `needs_review`; không tự động đảo `applied_at`, hủy booking đã xác nhận hoặc thu hồi vé chỉ vì dữ liệu đối soát không khớp.
- **A4 — Payment thành công nhưng booking không còn đủ điều kiện:** Bảo toàn `succeeded`; không phát hành vé hoặc reclaim ghế. Chuyển sang `refund_required` hoặc `needs_review` tùy căn cứ nghiệp vụ.
- **A5 — Nhiều worker đối soát đồng thời:** Dùng cơ chế khóa, claim hoặc cập nhật có điều kiện để tránh thực hiện trùng tác động nghiệp vụ.
- **A6 — Trạng thái thay đổi trong lúc gọi cổng:** Đọc lại trạng thái trong transaction mới. Nếu kết quả truy vấn không còn áp dụng được, không ghi đè trạng thái mới hơn.
- **A7 — Sai số tiền hoặc tiền tệ:** Ghi nhận bằng chứng tài chính đã xác minh nhưng không áp dụng payment vào booking khi điều kiện không thỏa mãn. Chuyển hồ sơ sang nhánh xử lý phù hợp thay vì đánh đồng sai lệch với giao dịch thất bại.

### Hậu điều kiện

**Đối soát thành công và payment được áp dụng:**

- Kết quả tài chính được ghi nhận.
- Booking và các đối tượng liên quan được cập nhật theo UC-02.
- Các thay đổi DB được commit nhất quán.

**Đối soát thành công nhưng payment không thể áp dụng:**

- Kết quả tài chính vẫn được bảo toàn.
- Không phát hành vé hoặc reclaim ghế trái quy tắc.
- Hồ sơ được định tuyến sang `refund_required` hoặc `needs_review`.

**Chưa xác định được kết quả:**

- Payment giữ trạng thái chưa chốt phù hợp.
- Lịch retry và thông tin giám sát được cập nhật.
- Không thực hiện tác động tài chính dựa trên giả định.

### Quy tắc liên quan

- `BR-PAY-002`, `BR-PAY-003`, `BR-PAY-007`, `BR-PAY-008`.
- UC-02: xử lý kết quả payment và áp dụng booking.
- UC-03: giải phóng booking hết hạn.
- UC-04A: thực thi hoàn tiền khi được kích hoạt.

### Tiêu chí nghiệm thu

- **AC-UC04-01:** Payment `unknown` được xác minh `succeeded`; booking còn đủ điều kiện thì được áp dụng qua UC-02.
- **AC-UC04-02:** Payment `unknown` được xác minh thất bại rõ ràng thì được cập nhật theo kết quả đã xác minh.
- **AC-UC04-03:** Không xác định được kết quả thì giữ trạng thái chưa chốt và lên lịch retry.
- **AC-UC04-04:** Kết quả `succeeded` đã được xác minh không bị ghi đè bởi phản hồi cũ hoặc chậm.
- **AC-UC04-05:** `needs_review` chỉ chuyển sang `resolved` sau khi hồ sơ được xử lý và đóng hợp lệ.
- **AC-UC04-06:** Payment thành công muộn không làm booking hết hạn được xác nhận hoặc ghế đã bán lại bị reclaim.
- **AC-UC04-07:** Không gọi API cổng trong transaction đang giữ khóa nghiệp vụ.
- **AC-UC04-08:** Worker chạy song song hoặc retry không tạo tác động nghiệp vụ trùng lặp.

### Câu hỏi mở

- **OQ-UC-04:** Chu kỳ retry, khoảng cách giữa các lần thử, thời gian tối đa tự động đối soát và điều kiện chuyển sang xử lý thủ công.
- **OQ-04:** Quyền hạn và quy trình xử lý thủ công các hồ sơ `needs_review`/`refund_required`.

---

### 8.4. UC-04A: Hoàn tiền (Refund)

**Phạm vi:** Out of scope — demo. Mục này quy định hợp đồng hành vi và mô hình dữ liệu, không yêu cầu thực thi Refund API trong bản demo.

### Mục tiêu

Hoàn tiền cho payment đã được xác minh `succeeded` khi nghiệp vụ quyết định phải trả lại khoản tiền đã thu.

### Tác nhân

- **Tác nhân chính:** System Worker hoặc dịch vụ xử lý refund khi phạm vi được kích hoạt.
- **Tác nhân phụ:** Payment Gateway thông qua Refund API, refund webhook hoặc polling; PostgreSQL.
- **Tác nhân quyết định:** Manager hoặc nhân sự được phân quyền theo chính sách đã phê duyệt.

### Tiền điều kiện

1. Payment gốc có `status = succeeded`.
2. Có căn cứ nghiệp vụ hợp lệ để hoàn tiền.
3. Chưa tồn tại refund thành công cho payment đó; việc tạo refund phải tuân thủ ràng buộc tối đa một refund cho mỗi payment theo schema hiện tại.
4. Refund được tạo với `merchant_refund_id` do backend sinh, là UUID và có ràng buộc `UNIQUE`.
5. Retry của cùng một refund phải sử dụng lại **chính `merchant_refund_id` đã tạo ban đầu**, không sinh ID mới.
6. Số tiền và tiền tệ hoàn phải dựa trên số tiền thực tế đã thu và dữ liệu payment gốc.

### Luồng chính

1. **[Backend]** Nhận yêu cầu hoàn tiền hợp lệ và xác định payment gốc.
2. Kiểm tra payment đã `succeeded`, căn cứ hoàn tiền, refund hiện có và số tiền có thể hoàn.
3. Tạo bản ghi `refund` với:
   - `status = pending`.
   - `amount` và `currency` dựa trên payment gốc theo chính sách hoàn tiền.
   - `provider` khớp với `payment.provider`.
   - `merchant_refund_id` là UUID do backend sinh và được lưu ổn định.
4. Nếu xảy ra xung đột theo `payment_id`, không tạo refund thứ hai. Đọc lại bản ghi đã tồn tại và tiếp tục xử lý refund đó nếu yêu cầu hợp lệ.
5. Khi phạm vi thực thi được kích hoạt, gọi Refund API ngoài transaction đang giữ khóa DB, truyền các thông tin cần thiết:
   - Số tiền hoàn.
   - `provider_transaction_id` của payment gốc.
   - `merchant_refund_id` làm khóa định danh yêu cầu refund.
6. Diễn giải phản hồi từ cổng:
   - **Chỉ chấp nhận yêu cầu:** Không coi là hoàn tiền thành công.
   - **Đang xử lý:** Giữ `pending` hoặc `unknown` theo trạng thái được xác định từ phản hồi và schema.
   - **Hoàn tiền thành công đã xác minh:** Chuyển refund sang `succeeded` và ghi `completed_at`.
   - **Hoàn tiền thất bại rõ ràng:** Chuyển refund sang `failed` và đưa hồ sơ sang `needs_review`.
7. Với refund `pending`/`unknown`, tiếp tục xác minh qua refund webhook hoặc polling theo hợp đồng của cổng.
8. Chỉ cập nhật `payment.reconciliation_status = resolved` khi refund đã `succeeded` và kết quả được xác minh, hoặc khi một hồ sơ `needs_review` được đóng bằng quyết định hợp lệ.
9. Lưu lịch sử xử lý đủ để truy vết yêu cầu, phản hồi, các lần xác minh và quyết định nghiệp vụ.

### Luồng thay thế và ngoại lệ

- **A1 — Timeout hoặc phản hồi mơ hồ:** Giữ trạng thái chưa xác định phù hợp. Không tạo `merchant_refund_id` mới và không gửi lại yêu cầu một cách mù quáng. Truy vấn trạng thái refund bằng định danh ổn định trước khi quyết định thử lại.

- **A2 — Cổng từ chối yêu cầu:** Nếu kết quả thất bại đã rõ ràng, chuyển refund sang `failed`, cập nhật payment sang `needs_review` và lưu audit trail. Không retry vô hạn.

- **A3 — Refund webhook trùng:** Xử lý idempotent dựa trên refund hiện có và định danh sự kiện/giao dịch. Không tạo refund mới và không lặp lại tác động nghiệp vụ.

- **A4 — Cổng đã hoàn tiền nhưng DB chưa ghi nhận:** Đối soát kết quả với cổng trước khi gửi yêu cầu khác. Không mặc định rằng lỗi DB có nghĩa là yêu cầu hoàn tiền chưa xảy ra.

- **A5 — Retry sau timeout:** Chỉ gửi lại khi có căn cứ cho thấy yêu cầu trước chưa được ghi nhận hoặc hợp đồng idempotency của cổng bảo đảm việc gửi lại an toàn. Tiếp tục dùng cùng `merchant_refund_id`.

- **A6 — Hoàn tiền một phần hoặc nhiều lần:** Không giả định được phép hoàn nhiều lần. Chính sách hoàn toàn phần/một phần và cách tính số tiền còn được hoàn phải được chốt trước khi triển khai.

- **A7 — Booking đã hết hạn hoặc ghế đã được bán lại:** Refund không làm booking được xác nhận lại, không reclaim ghế và không phát hành vé.

### Bảo vệ chống hoàn tiền trùng

Theo các ràng buộc được nêu trong schema:

- `uq_refund_payment`: `UNIQUE (payment_id)`, tối đa một refund cho mỗi payment.
- `uq_refund_merchant_refund_id`: bảo đảm `merchant_refund_id` duy nhất.
- `ux_refund_provider_refund_id`: unique index có điều kiện trên `(provider, provider_refund_id)` khi `provider_refund_id IS NOT NULL`.
- Khóa ngoại ghép `(payment_id, provider) → payment(id, provider)` bảo đảm provider của refund khớp với payment gốc.

Các ràng buộc DB không thay thế idempotency ở phía cổng thanh toán. Nếu cổng không hỗ trợ idempotent refund, retry an toàn phụ thuộc vào việc truy vấn trạng thái trước khi gửi lại và các bảo đảm được nêu trong OQ-01.

### Hậu điều kiện

**Hoàn tiền thành công:**

- Refund có `status = succeeded`.
- `completed_at` được ghi nhận theo schema.
- Kết quả hoàn tiền có thể truy vết về payment gốc.
- Payment chỉ chuyển sang `reconciliation_status = resolved` khi điều kiện giải quyết hồ sơ được đáp ứng.

**Hoàn tiền thất bại:**

- Refund có `status = failed`.
- Payment được chuyển sang `needs_review` theo quy tắc nghiệp vụ.
- Hồ sơ được giữ để nhân sự xem xét; không tự động đánh dấu `resolved`.

**Hoàn tiền chưa xác định:**

- Refund tiếp tục ở trạng thái `pending`/`unknown` phù hợp.
- Hệ thống truy vấn lại theo cùng định danh refund.
- Không tạo refund thứ hai chỉ để thử lại.

### Tiêu chí nghiệm thu

- **AC-UC04A-01 — Happy path:** Refund chuyển từ `pending` sang `succeeded` sau khi kết quả hoàn tiền được xác minh; payment liên quan chuyển sang `resolved` khi đủ điều kiện.
- **AC-UC04A-02 — Cổng mới chấp nhận yêu cầu:** Refund vẫn `pending`/`unknown`; payment không được chuyển sang `resolved`.
- **AC-UC04A-03 — Retry idempotent:** Retry sử dụng cùng `merchant_refund_id` và không tạo refund thứ hai.
- **AC-UC04A-04 — Cổng từ chối:** Refund chuyển sang `failed`; payment chuyển sang `needs_review`.
- **AC-UC04A-05 — Không hoàn tiền trùng:** Không tồn tại hai refund cho cùng payment theo ràng buộc schema hiện tại.
- **AC-UC04A-06 — Webhook trùng:** Xử lý lặp không gây ra tác động nghiệp vụ trùng.
- **AC-UC04A-07 — DB lỗi sau khi cổng hoàn tiền:** Hệ thống phải đối soát kết quả trước khi thử lại.
- **AC-UC04A-08 — Không thay đổi booking trái quy tắc:** Refund không tự xác nhận booking, reclaim ghế hoặc phát hành vé.

### Quy tắc liên quan và câu hỏi mở

- `BR-PAY-007`, `BR-PAY-008`.
- UC-02: xác minh và áp dụng kết quả thanh toán.
- UC-03: giải phóng booking hết hạn.
- UC-04: đối soát payment.
- **OQ-01:** Hợp đồng của cổng thanh toán, hỗ trợ idempotency, truy vấn trạng thái refund và webhook/polling.
- **OQ-04:** Quyền phê duyệt và xử lý thủ công các hồ sơ bất thường.
- **OQ-UC-04:** Chu kỳ retry và thời gian tối đa trước khi chuyển sang xử lý thủ công.
- Chính sách hoàn toàn phần/một phần và điều kiện kết thúc hồ sơ cần được thống nhất trước khi triển khai refund thực tế.

---

## 9. UC-05: Soát vé tại rạp (Validate Ticket)

- **Mục tiêu:** Nhân viên quét mã vé (QR chứa `ticket_code`) để xác thực quyền vào rạp một lần; ngăn vé giả, vé dùng lại; cho phép soát từng vé độc lập (ghế Couple = 2 vé riêng — BR-BOOK-009).
- **Tác nhân chính:** Nhân viên rạp (Staff) — role `STAFF`, authorization backend (BR-AUTH-001..003; Staff đã xác nhận cho nghiệp vụ soát vé — OQ-04).
- **Tác nhân phụ:** Backend Validation API; Database.
- **Trigger:** Staff quét QR/nhập mã vé.
- **Tiền điều kiện:**
  - Staff đã đăng nhập, tài khoản `active`, có quyền soát vé (không dựa vào việc biết `ticket_code` để suy quyền).
  - **Ngữ cảnh soát:** Staff thao tác trong ngữ cảnh một suất chiếu đang soát (chọn trước trên ứng dụng staff, hoặc hệ thống xác định suất từ vé). Nếu ứng dụng staff có chọn suất trước, hệ thống đối chiếu suất của vé với suất đang soát — khác → A3. Nếu không chọn trước, hệ thống hiển thị suất/phòng của vé và vẫn kiểm tra cửa sổ; hành vi mặc định (chọn trước hay không) là **PROPOSED — OQ-UC-07**.
  - QR giải mã ra `ticket_code` dạng UUID hợp lệ.
- **Luồng chính:**
  1. [Staff] Gửi `ticket_code` lên API.
  2. Hệ thống truy vấn `ticket` theo `ticket_code` (UNIQUE); join `booking_detail` → `booking`, `showtime`, `seat` (nhãn ghế từ `seat_label_snapshot`).
  3. Kiểm tra nghiệp vụ: booking `confirmed`; detail `sold`; suất chiếu của vé khớp; `ticket.status = 'valid'`.
  4. Kiểm tra **cửa sổ soát vé** (khoảng thời gian cho phép vào rạp so với `starts_at`/`ends_at`) — **quy tắc cấu hình, chưa chốt (OQ-03)**; hệ thống lấy giá trị từ cấu hình, không hard-code (ví dụ "-30 phút" trong bản nháp chỉ là ví dụ, không phải yêu cầu).
  5. Thực hiện chuyển trạng thái **có điều kiện** trong transaction: `UPDATE ticket SET status='used', used_at=..., validated_by_user_id=... WHERE ticket_code = ... AND status = 'valid'` — đúng một attempt đồng thời thành công; attempt khác thấy 0 dòng cập nhật → trả "vé đã dùng" (A1).
  6. Trả kết quả "Hợp lệ" kèm phòng chiếu (`room_name_snapshot`), ghế (`seat_label_snapshot`), suất chiếu; vé Couple (detail `capacity_units_snapshot = 2`) được soát từng vé riêng — người đến trễ vẫn vào được bằng vé của mình.
- **Luồng thay thế và ngoại lệ:**
  - **A1 (Vé đã dùng):** từ chối với thông tin `used_at` và nhân viên đã soát (audit có sẵn trong bản ghi) — không đổi trạng thái.
  - **A2 (Vé `voided`):** từ chối; hiển thị `void_reason`.
  - **A3 (Sai suất/sai phòng/cửa sổ không hợp lệ):** từ chối với cảnh báo chi tiết để Staff báo khách; **không đổi trạng thái**.
  - **A4 (Mã không tồn tại/sai định dạng):** "Vé không tồn tại" — không tiết lộ thêm (NFR-SEC).
  - **A5 (Booking chưa `confirmed` / detail chưa `sold`):** từ chối (vé không thể hợp lệ khi chưa được phát hành trong luồng áp dụng payment).
  - **A6 (Lỗi DB khi cập nhật):** transaction rollback; vé giữ `valid`; Staff quét lại được.
  - **A7 (Commit thành công nhưng response bị mất/đứt kết nối):** vé đã `used` + audit đã ghi; lần quét lại KHÔNG tạo lần soát thứ hai — hệ thống phát hiện `status = 'used'` và trả A1 (thông tin audit của lần soát thành công đầu tiên: `used_at`, `validated_by_user_id`). Response lỗi (timeout) không được phép ghi đè audit đã commit.
- **Hậu điều kiện:**
  - **Thành công:** vé `used` + `used_at` + `validated_by_user_id` (audit truy vết); không thể soát thành công lần hai.
  - **Thất bại:** vé giữ nguyên trạng thái; attempt thất bại không ghi đè audit của lần soát thành công.
- **Quy tắc liên quan:** BR-BOOK-009, FR-TICKET-002, AC-TICKET-001, BR-AUTH-002/003; OQ-03, OQ-04.
- **Tiêu chí nghiệm thu:** AC-UC05-01 (valid → used một lần), 02 (lần hai bị từ chối với thông tin audit), 03 (hai scan đồng thời — đúng một thành công), 04 (vé used/voided/không tồn tại bị từ chối), 05 (cửa sổ không hợp lệ → từ chối không đổi trạng thái), 06 (Couple: hai vé độc lập, soát riêng, khách đến trễ vào được bằng vé của mình), 07 (commit thành công + response mất → quét lại trả A1 với audit của lần soát thành công, không tạo lần soát thứ hai).
- **UC liên quan:** UC-02 (phát hành vé), OQ-03/OQ-04.
- **Câu hỏi mở:** OQ-03 (cửa sổ soát vé — mục 13).

## 10. UC-06: Xem danh sách và thông tin phim (Browse Movies)

### 10.1. Thông tin chung

- **Mục tiêu:** Cho phép khách hàng duyệt danh sách phim, tìm kiếm phim và xem thông tin chi tiết để quyết định phim muốn xem.
- **Tác nhân chính:** Khách hàng (Customer).
- **Tác nhân phụ:** Backend API; Database.
- **Trigger:** Khách hàng mở trang phim, tìm kiếm phim hoặc chọn một phim trong danh sách.
- **Phạm vi:** Hiển thị danh sách, tìm kiếm, lọc và xem chi tiết phim. Không bao gồm xem lịch chiếu chi tiết, giữ ghế, Checkout hoặc thanh toán.

### 10.2. Tiền điều kiện

1. Hệ thống có thể truy cập danh mục phim.
2. Khách hàng có thể truy cập chức năng duyệt phim mà không cần đăng nhập. Căn cứ: `FR-MOVIE-001/002` không yêu cầu đăng nhập cho việc xem, và luồng rủi ro trong `business-system-analysis.md` mục 8.1 ghi nhận "Chưa đăng nhập khi đặt vé... duyệt phim/xem lịch vẫn khả dụng". Nếu tài liệu yêu cầu quy định khác, cần cập nhật theo chính sách xác thực đã chốt.
3. Hệ thống xác định được những phim đủ điều kiện hiển thị theo trạng thái nghiệp vụ được quy định: chỉ hiển thị phim `now_showing` và `coming_soon`; phim `archived` không xuất hiện trong danh sách khách hàng (theo kiểm tra của `FR-MOVIE-001`).
4. Dữ liệu phim được đọc từ nguồn dữ liệu chính thức của hệ thống; frontend không tự suy diễn trạng thái phim hoặc lịch chiếu.

### 10.3. Luồng chính

1. **[Customer]** Mở trang danh sách phim.
2. **[System]** Gửi yêu cầu lấy danh sách phim đến Backend API.
3. **[System]** Truy vấn các phim đủ điều kiện hiển thị theo quy tắc nghiệp vụ.
4. **[System]** Trả về danh sách phim cùng thông tin cần thiết để hiển thị, chẳng hạn:
   - Tên phim.
   - Poster, nếu có.
   - Thể loại.
   - Thời lượng.
   - Phân loại độ tuổi, nếu được quản lý.
   - Trạng thái phát hành hoặc thông tin giới thiệu phù hợp với thiết kế giao diện.
5. **[Customer]** Có thể nhập từ khóa tìm kiếm hoặc chọn bộ lọc được giao diện hỗ trợ.
6. **[System]** Áp dụng điều kiện tìm kiếm/lọc ở phía được thiết kế chịu trách nhiệm, trả về danh sách phù hợp. Nếu có phân trang, metadata phân trang được trả về cùng kết quả.
7. **[Customer]** Chọn một phim.
8. **[System]** Truy vấn thông tin chi tiết của phim và hiển thị dữ liệu hiện có, chẳng hạn:
   - Tên và nội dung giới thiệu.
   - Poster hoặc hình ảnh liên quan.
   - Thể loại, thời lượng và phân loại độ tuổi.
   - Ngày phát hành hoặc thông tin liên quan nếu được lưu trữ.
9. **[Customer]** Có thể chọn chức năng xem lịch chiếu để chuyển sang UC-07.

### 10.4. Luồng thay thế và ngoại lệ

- **A1 — Không có phim phù hợp:** Trả về danh sách rỗng và thông báo phù hợp. Không xem đây là lỗi hệ thống.
- **A2 — Không có kết quả tìm kiếm:** Hiển thị thông báo không tìm thấy phim và cho phép khách hàng sửa từ khóa hoặc bộ lọc.
- **A3 — Mã phim không tồn tại / không được phép hiển thị:** Trả về thông báo phù hợp mà không tiết lộ dữ liệu nội bộ. (Khác A1/A2: đây là lỗi truy xuất một phim cụ thể theo định danh, không phải trường hợp danh sách rỗng hay không có kết quả tìm kiếm.)
- **A4 — Phim không có đầy đủ thông tin tùy chọn:** Hiển thị những trường có dữ liệu; không tự tạo nội dung, thời lượng hoặc trạng thái còn thiếu.
- **A5 — Lỗi API hoặc Database:** Trả lỗi theo quy ước lỗi thống nhất của hệ thống. Giao diện hiển thị thông báo có thể thử lại.
- **A6 — Dữ liệu thay đổi trong lúc xem:** Khi khách hàng chuyển sang UC-07, hệ thống phải kiểm tra lại lịch chiếu và trạng thái suất ở thời điểm truy vấn; không dựa vào dữ liệu danh sách phim đã tải trước đó để xác nhận khả năng đặt vé.

### 10.5. Hậu điều kiện

- **Thành công:** Khách hàng xem được danh sách hoặc thông tin chi tiết của phim.
- **Không có kết quả:** Danh sách rỗng hoặc thông báo không tìm thấy phim được hiển thị phù hợp.
- **Thất bại:** Không phát sinh thay đổi về booking, ghế, payment hoặc ticket.

### 10.6. Quy tắc và truy vết

- UC liên quan: UC-07 (xem lịch chiếu), UC-01 (giữ ghế — chỉ khi khách hàng tiếp tục sang luồng đặt vé).
- Nguồn cần đối chiếu: `FR-MOVIE-001/002` trong `business-system-analysis.md`; trạng thái phim trong `database/database-schema.md`, mục 3.1 (`coming_soon`, `now_showing`, `ended`, `archived`).
- Các trường dữ liệu hiển thị tương ứng các cột đã được xác nhận trong schema: `title`, `description`, `duration_minutes`, `release_date`, `poster_url`, `age_rating`, `status`; thể loại qua `movie_genre`. Trường `poster_url`, `age_rating`, `release_date` được phép NULL — giao diện hiển thị khi có dữ liệu, không coi là bắt buộc.

### 10.7. Tiêu chí nghiệm thu

- **AC-UC06-01:** Khách hàng có thể xem danh sách phim đủ điều kiện hiển thị.
- **AC-UC06-02:** Tìm kiếm trả về các phim phù hợp với điều kiện tìm kiếm đã hỗ trợ.
- **AC-UC06-03:** Bộ lọc, nếu được hỗ trợ, không trả về phim không đáp ứng điều kiện.
- **AC-UC06-04:** Khách hàng có thể mở trang chi tiết của một phim hợp lệ.
- **AC-UC06-05:** Phim không tồn tại hoặc không được phép hiển thị không làm lộ dữ liệu nội bộ.
- **AC-UC06-06:** Danh sách rỗng được xử lý bình thường, không bị xem là lỗi hệ thống.
- **AC-UC06-07:** Lỗi API hoặc Database được xử lý theo quy ước lỗi chung.
- **AC-UC06-08:** UC-06 không tạo booking, không giữ ghế và không tạo payment attempt.

### 10.8. Quy tắc đã chốt và câu hỏi mở

**Đã có căn cứ trong tài liệu nguồn — không còn là câu hỏi mở:**

- Khách chưa đăng nhập vẫn duyệt phim được: căn cứ `FR-MOVIE-001/002` (không yêu cầu đăng nhập cho xem) và luồng rủi ro trong `business-system-analysis.md` mục 8.1 ("Chưa đăng nhập khi đặt vé... duyệt phim/xem lịch vẫn khả dụng").
- Chỉ hiển thị phim `now_showing` và `coming_soon`; phim `archived` không xuất hiện trong danh sách khách hàng — căn cứ kiểm tra của `FR-MOVIE-001`.
- Các trường dữ liệu hiển thị lấy từ schema hiện tại: `title`, `description`, `duration_minutes`, `release_date`, `poster_url`, `age_rating`, `status`; thể loại qua `movie_genre` (xem Mục 10.6).

**Còn là câu hỏi mở (chưa có quyết định chính thức):**

- Tìm kiếm, bộ lọc và phân trang nào nằm trong phạm vi phiên bản đầu?
- Phim đã ngừng chiếu (`ended`) có tiếp tục được xem chi tiết không?

---

## 11. UC-07: Xem lịch chiếu và chọn suất chiếu (Browse Showtimes)

### 11.1. Thông tin chung

- **Mục tiêu:** Cho phép khách hàng xem các suất chiếu phù hợp với phim, ngày và rạp; lựa chọn một suất chiếu để tiếp tục đặt vé.
- **Tác nhân chính:** Khách hàng (Customer).
- **Tác nhân phụ:** Backend API; Database.
- **Trigger:** Khách hàng chọn chức năng xem lịch chiếu từ trang phim hoặc mở lịch chiếu trực tiếp.
- **Phạm vi:** Tra cứu và hiển thị lịch chiếu; lựa chọn suất chiếu để chuyển sang luồng đặt vé. Không giữ ghế, không tạo booking và không xử lý thanh toán trong UC này.

### 11.2. Tiền điều kiện

1. Hệ thống xác định được phim mà khách hàng muốn xem.
2. Phim và suất chiếu được kiểm tra theo trạng thái nghiệp vụ hiện tại.
3. Hệ thống có dữ liệu cần thiết để xác định thời gian chiếu, rạp và phòng chiếu.
4. Việc một suất xuất hiện trong lịch chiếu **không tự động bảo đảm ghế còn trống hoặc việc đặt vé sẽ thành công**. Khả năng giữ ghế phải được kiểm tra lại trong UC-01.

### 11.3. Luồng chính

1. **[Customer]** Chọn xem lịch chiếu của một phim từ UC-06.
2. **[System]** Gửi yêu cầu lấy lịch chiếu theo phim; có thể nhận thêm điều kiện ngày chiếu hoặc rạp nếu giao diện hỗ trợ.
3. **[System]** Truy vấn các suất chiếu đáp ứng điều kiện hiển thị: chỉ các suất `scheduled` còn bán theo `BR-ST-003` (không `cancelled`, chưa quá `booking_closes_at` nếu có mốc — `FR-SHOW-001`). Đây là thông tin hiển thị phục vụ duyệt; điều kiện chốt cuối cùng khi tạo booking do backend kiểm tra lại (UC-01, `FR-BOOK-001`, `BR-BOOK-001`).
4. **[System]** Trả về danh sách suất chiếu cùng thông tin cần thiết, chẳng hạn:
   - Phim tương ứng.
   - Ngày và giờ bắt đầu.
   - Rạp và phòng chiếu.
   - Trạng thái suất chiếu.
   - Thông tin giá hoặc nhãn giá, nếu hệ thống có quy định và cung cấp dữ liệu này.
5. **[Customer]** Chọn ngày chiếu hoặc rạp để thu hẹp danh sách, nếu các bộ lọc này được hỗ trợ.
6. **[System]** Trả về lịch chiếu tương ứng với các điều kiện được chọn.
7. **[Customer]** Chọn một suất chiếu muốn đặt vé.
8. **[System]** Kiểm tra lại suất chiếu theo dữ liệu hiện tại, bao gồm trạng thái và điều kiện được phép tiếp tục đặt vé.
9. **[System]** Nếu suất chiếu đáp ứng điều kiện, chuyển khách hàng sang bước chọn ghế/đặt vé với định danh suất chiếu đã chọn.
10. **[Customer]** Tiếp tục sang UC-01 để yêu cầu giữ ghế.

### 11.4. Luồng thay thế và ngoại lệ

- **A1 — Không có suất chiếu:** Hiển thị thông báo chưa có lịch chiếu phù hợp với phim, ngày hoặc rạp đã chọn.
- **A2 — Suất chiếu không tồn tại:** Từ chối yêu cầu và thông báo suất chiếu không khả dụng.
- **A3 — Suất chiếu không còn được phép đặt:** Nếu trạng thái suất không đáp ứng quy tắc nghiệp vụ (chỉ `scheduled` còn bán theo `BR-ST-003`), không cho tiếp tục sang bước đặt vé.
- **A4 — Suất chiếu bị hủy hoặc thay đổi:** Không dùng thông tin đã lưu ở giao diện để tiếp tục đặt vé. Thông báo trạng thái mới và yêu cầu khách chọn suất khác nếu có.
- **A5 — Lịch chiếu đã cũ:** Nếu dữ liệu thay đổi sau khi tải lịch, hệ thống truy vấn lại khi khách chọn suất; kết quả cũ không được coi là xác nhận đặt vé thành công.
- **A6 — Không thể lấy dữ liệu rạp/phòng:** Không hiển thị thông tin suy đoán. Trả lỗi phù hợp hoặc ẩn suất nếu thiếu dữ liệu bắt buộc để khách xác định nơi chiếu.
- **A7 — Lỗi API hoặc Database:** Trả lỗi theo quy ước chung; không tạo booking hoặc giữ ghế một phần.
- **A8 — Suất chiếu hợp lệ nhưng hết ghế:** Việc hiển thị lịch chiếu không bảo đảm còn ghế. Khi UC-01 kiểm tra và không thể giữ ghế, hệ thống xử lý theo ngoại lệ của UC-01; khách hàng có thể quay lại chọn suất khác.

### 11.5. Hậu điều kiện

- **Thành công:** Khách hàng xem được lịch chiếu và chọn được suất chiếu đủ điều kiện để tiếp tục đặt vé.
- **Không có kết quả:** Hiển thị lịch trống hoặc thông báo không có suất phù hợp.
- **Thất bại:** Không phát sinh booking, giữ ghế, payment attempt hoặc ticket.
- **Ranh giới nghiệp vụ:** UC-07 chỉ chuyển thông tin suất chiếu đã chọn sang luồng đặt vé. UC-01 chịu trách nhiệm kiểm tra và giữ ghế một cách nguyên tử.

### 11.6. Quy tắc và truy vết

- UC liên quan: UC-06 (xem phim), UC-01 (giữ ghế).
- Quy tắc trạng thái suất chiếu: chỉ các suất `scheduled` còn bán được hiển thị và cho phép tiếp tục đặt vé (theo `BR-ST-003` và `FR-SHOW-001` — đã chốt; không mở rộng sang `in_progress`). `FR-MGT-ST-001` là yêu cầu quản trị liên quan đến vòng đời suất; `OQ-UC-01` trong mục 16 ghi nhận bản nháp UC-01 từng cho phép `in_progress` — thiết kế đã chốt chỉ bán khi `scheduled`, ưu tiên áp dụng quy tắc đã chốt.
- Ranh giới nghiệp vụ: UC-07 chỉ chuyển thông tin suất chiếu đã chọn sang luồng đặt vé (định danh suất, không kèm booking). UC-01 chịu trách nhiệm kiểm tra authoritative và giữ ghế một cách nguyên tử (`FR-BOOK-001`, `BR-BOOK-001/002`). Việc hiển thị lịch chiếu không đồng nghĩa ghế đã được giữ. Phân định này áp dụng nhất quán ở cả luồng chính (bước 3: thông tin hiển thị; bước 8: kiểm tra lại cho phép điều hướng, không thay thế kiểm tra authoritative), luồng ngoại lệ (A8: hết ghế được xử lý theo ngoại lệ của UC-01) và tiêu chí nghiệm thu (AC-UC-07-06: UC-07 không tạo booking/giữ ghế; AC-UC-07-07: lịch chiếu không là bằng chứng ghế còn trống).

### 11.7. Tiêu chí nghiệm thu

- **AC-UC07-01:** Khách hàng có thể xem lịch chiếu theo phim hợp lệ.
- **AC-UC07-02:** Các bộ lọc ngày/rạp, nếu được hỗ trợ, trả về kết quả phù hợp.
- **AC-UC07-03:** Suất chiếu không tồn tại hoặc không đủ điều kiện đặt vé bị từ chối.
- **AC-UC07-04:** Khi suất chiếu thay đổi hoặc bị hủy, hệ thống không tiếp tục dựa trên dữ liệu cũ.
- **AC-UC07-05:** Khách hàng có thể chọn một suất chiếu hợp lệ để tiếp tục sang UC-01.
- **AC-UC07-06:** UC-07 không tự tạo booking, không giữ ghế và không tạo payment attempt.
- **AC-UC07-07:** Lịch chiếu không được coi là bằng chứng xác nhận ghế còn trống.
- **AC-UC07-08:** Lỗi truy vấn lịch chiếu không làm phát sinh thay đổi nghiệp vụ một phần.

### 11.8. Quy tắc đã chốt và câu hỏi mở

**Đã có căn cứ trong tài liệu nguồn — không còn là câu hỏi mở:**

- Trạng thái suất chiếu được hiển thị công khai: chỉ các suất `scheduled` còn bán (theo `BR-ST-003`: không `cancelled`, chưa quá `booking_closes_at` nếu có mốc) — căn cứ `FR-SHOW-001`.
- Hai khái niệm này là **một** quy tắc duy nhất trong phạm vi hiện tại, không phải hai quyết định riêng: "được hiển thị" và "được tiếp tục đặt" cùng áp dụng điều kiện `scheduled` còn bán theo `BR-ST-003`. Phân định trách nhiệm vẫn giữ nguyên: hiển thị ở UC-07 chỉ là thông tin phục vụ duyệt; điều kiện chốt cuối cùng khi tạo booking do backend kiểm tra lại theo `FR-BOOK-001`/`BR-BOOK-001` (xem mục 11.3 bước 3, mục 11.6, AC-UC07-06/07).

**Còn là câu hỏi mở (chưa có quyết định chính thức):**

- Lịch chiếu cho phép lọc theo ngày, rạp và định dạng chiếu nào trong phiên bản đầu?
- Có hiển thị giá vé ở bước lịch chiếu hay chỉ sau khi chọn ghế?
- Nếu suất chiếu bị hủy sau khi khách chọn nhưng trước khi giữ ghế, thông báo và điều hướng về giao diện sẽ được xử lý thế nào?
- Hệ thống có yêu cầu cập nhật lịch chiếu theo thời gian thực hay chỉ truy vấn lại khi khách thực hiện thao tác?

---

## 12. UC-08: Đăng nhập và điều hướng theo vai trò (Login and Role-Based Navigation)

### 12.1. Mục tiêu

Cho phép người dùng đăng nhập bằng tài khoản hợp lệ, đăng xuất khỏi phiên hiện tại và được điều hướng đến các chức năng phù hợp với role được Backend xác định. UC này không đặc tả đăng ký tài khoản hoặc quản trị user/role; các nghiệp vụ đó được tham chiếu từ yêu cầu tài khoản và quản trị tương ứng.

### 12.2. Tác nhân

- **Tác nhân chính:** Customer, Staff, Manager, Admin.
- **Tác nhân phụ:** Backend Authentication/Authorization Service.

### 12.3. Trigger

- Người dùng gửi yêu cầu đăng nhập.
- Người dùng gửi yêu cầu đăng xuất khỏi phiên hiện tại.
- Người dùng truy cập một chức năng cần xác thực hoặc authorization.

### 12.4. Tiền điều kiện

- Với đăng nhập, tài khoản tồn tại và thông tin xác thực được gửi đến Backend.
- Với chức năng bảo vệ, request có phiên/token mà Backend có thể xác thực.
- Tài khoản phải ở trạng thái `active` để đăng nhập thành công. Tài khoản `locked` hoặc `deactivated` không được cấp phiên đăng nhập mới.
- Role và quyền hiệu lực được lấy từ dữ liệu tài khoản/role và policy authorization của Backend. Không mặc định Staff, Manager và Admin có cùng quyền; ranh giới chi tiết còn mở tại `OQ-04`.

### 12.5. Luồng chính

1. **[User]** Nhập thông tin đăng nhập và gửi yêu cầu đến Backend.
2. Backend xác thực thông tin đăng nhập và kiểm tra trạng thái `app_user`.
3. Nếu thông tin hợp lệ và tài khoản `active`, Backend tạo phiên đăng nhập theo cơ chế xác thực của hệ thống và trả kết quả cùng quyền truy cập theo role.
4. Frontend sử dụng kết quả xác thực để điều hướng đến khu vực chức năng tương ứng; điều hướng giao diện không thay thế kiểm tra authorization.
5. Khi User gọi endpoint cần bảo vệ, Backend xác thực phiên/token và kiểm tra role cùng quyền sở hữu tài nguyên trước khi thực hiện thao tác.
6. **[User]** Gửi yêu cầu đăng xuất khi muốn kết thúc phiên hiện tại.
7. Backend xử lý đăng xuất theo hợp đồng phiên đã triển khai; Frontend xóa trạng thái phiên cục bộ và quay về khu vực công khai. Việc đăng xuất không thay đổi tài khoản, role hoặc dữ liệu booking. Nguồn hiện không quy định cơ chế thu hồi token tức thời: nếu Backend không vô hiệu hóa được phiên, logout chỉ kết thúc trạng thái phía client và token vẫn chịu giới hạn `BR-AUTH-004` cho đến khi hết hạn.

### 12.6. Luồng thay thế và ngoại lệ

- **A1 — Thông tin xác thực không hợp lệ:** Từ chối đăng nhập và không tạo phiên hợp lệ; thông báo không tiết lộ tài khoản nào tồn tại hay chi tiết xác thực.
- **A2 — Tài khoản `locked` hoặc `deactivated`:** Từ chối đăng nhập và không tạo phiên mới.
- **A3 — Phiên/token không còn hợp lệ:** Từ chối request cần xác thực; yêu cầu đăng nhập lại. Không suy luận thời hạn phiên cụ thể vì nguồn không quy định giá trị thời gian.
- **A4 — Không đủ quyền:** Backend từ chối thao tác dù Frontend có hiển thị đường dẫn hoặc User biết mã định danh tài nguyên. Customer chỉ được truy cập tài nguyên của mình theo `BR-AUTH-002`; các quyền Staff/Manager/Admin áp dụng theo policy, trong đó phần chưa chốt liên kết `OQ-04`.
- **A5 — Role bị thu hồi hoặc thay đổi:** Áp dụng giới hạn MVP của `BR-AUTH-004`: access token đã phát hành có thể tiếp tục có hiệu lực đến hết hạn nếu hệ thống không có cơ chế thu hồi/kiểm tra hiện hành; không tuyên bố cơ chế thu hồi tức thời khi nguồn chưa quy định.
- **A6 — Đăng xuất hoặc lỗi máy chủ:** Không coi việc Frontend đổi màn hình là bằng chứng token đã bị thu hồi ở Backend. Nếu cơ chế logout Backend thất bại hoặc chưa được nguồn quy định, protected request vẫn được xử lý theo trạng thái token/authorization hiện hành; Backend trả lỗi theo hợp đồng xác thực và không thay đổi dữ liệu nghiệp vụ.

### 12.7. Hậu điều kiện

- **Đăng nhập thành công:** Có phiên đăng nhập hợp lệ và quyền truy cập được Backend xác định theo role.
- **Đăng nhập thất bại:** Không tạo phiên đăng nhập hợp lệ.
- **Authorization thất bại:** Không thực hiện thay đổi dữ liệu hoặc trả dữ liệu vượt quyền.
- **Đăng xuất:** Phiên hiện tại được xử lý theo cơ chế phiên đã triển khai, nhưng tài liệu không khẳng định token bị thu hồi tức thời; không làm thay đổi booking, payment hoặc ticket.
- Đăng ký tài khoản vẫn thuộc `FR-AUTH-001`; cấp/thu hồi role và quản trị user thuộc `FR-ADMIN-USER-001`, không phải phạm vi chi tiết của UC-08.

### 12.8. Quy tắc nghiệp vụ và ràng buộc liên quan

- `FR-AUTH-002`, `FR-AUTH-004`, `BR-AUTH-001..005`, `AC-SEC-001..003`, `NFR-SEC-002`.
- Trạng thái tài khoản trong schema: `active`, `locked`, `deactivated`; role được lưu qua `user_role`.
- Backend là nguồn quyết định authoritative cho identity, role, authorization và ownership.
- `OQ-04` vẫn mở đối với ranh giới Staff/Manager/Admin; UC-08 không cấp ngầm quyền quản trị hoặc quyền payment cho các role này.

### 12.9. Tiêu chí nghiệm thu

- **AC-UC08-01:** Tài khoản `active` với thông tin hợp lệ đăng nhập thành công và nhận quyền theo role.
- **AC-UC08-02:** Thông tin sai, tài khoản `locked` hoặc `deactivated` không tạo phiên hợp lệ và không làm lộ thông tin nhạy cảm.
- **AC-UC08-03:** Request không có phiên hợp lệ hoặc không đủ role bị Backend từ chối.
- **AC-UC08-04:** Customer không truy cập hoặc thao tác booking/vé của Customer khác.
- **AC-UC08-05:** Role bị thu hồi không được xem là tự động vô hiệu hóa token cũ trước khi hết hạn, phù hợp giới hạn MVP `BR-AUTH-004`; không suy ra thời hạn token.
- **AC-UC08-06:** Đăng ký và quản trị user/role không bị mô tả nhầm là luồng con của UC-08.

### 12.10. UC liên quan

- `UC-01`, `UC-05`, `UC-09`, `UC-10`.
- `FR-AUTH-001` và `FR-ADMIN-USER-001` cho các nghiệp vụ đăng ký và quản trị được tham chiếu.

### 12.11. Câu hỏi mở

- **OQ-04:** Ranh giới quyền Staff/Manager/Admin và các quyền tra cứu payment, hủy booking, báo cáo chưa được chốt.
- Cơ chế và thời hạn phiên/token cụ thể không được quy định trong nguồn; không đặt thêm chính sách trong UC này.

---

## 13. UC-09: Checkout và thanh toán (Checkout and Payment)

### 13.1. Mục tiêu

Cho phép Customer bắt đầu thanh toán cho **booking đã được UC-01 tạo**, bằng cách kiểm tra quyền sở hữu và điều kiện hiện tại, tạo hoặc sử dụng đúng payment attempt của booking, rồi chuyển Customer sang trải nghiệm thanh toán của cổng. UC-09 không tự xác nhận booking, phát hành ticket hoặc quyết định kết quả thanh toán.

### 13.2. Tác nhân

- **Tác nhân chính:** Customer.
- **Tác nhân phụ:** Backend Payment Service, Payment Gateway.

### 13.3. Trigger

Customer chọn thanh toán từ booking `pending` đã nhận từ UC-01.

### 13.4. Tiền điều kiện

- Customer đã đăng nhập và là chủ booking.
- Booking tồn tại, thuộc Customer, ở trạng thái `pending`, chưa hết hạn theo `expires_at`, có detail `held` và có tổng tiền/tiền tệ authoritative từ Backend.
- Backend có thể đọc lại booking, detail và payment attempts theo protocol khóa; dữ liệu hiển thị trên Frontend không thay thế kiểm tra này.
- Cổng thanh toán cụ thể và hợp đồng tích hợp chưa được chốt tại `OQ-01`.

### 13.5. Luồng chính

1. **[Customer]** Chọn thanh toán cho booking đã tạo ở UC-01; Frontend gửi `booking_id` hoặc mã booking theo hợp đồng API, không gửi booking mới hoặc giá do client quyết định.
2. Backend xác thực phiên, quyền sở hữu và đọc lại booking, detail, `expires_at`, `booking_closes_at` nếu có, tổng tiền và tiền tệ.
3. Backend từ chối nếu booking không còn `pending`, đã quá hạn, không còn detail `held`, không thuộc Customer hoặc không đáp ứng điều kiện bán.
4. Backend tính số tiền thanh toán từ `booking.total_amount` và tiền tệ từ `booking.currency`; không dùng giá, tổng tiền hoặc giảm giá do client gửi.
5. Backend kiểm tra các payment attempt của booking:
   - Transaction phải khóa theo protocol `Showtime → Booking → Payment attempts`, đọc lại booking và toàn bộ payment attempts sau khi lấy khóa.
   - Nếu đã có attempt `created`, `pending` hoặc `unknown`, không tạo attempt mở thứ hai; dùng lại hoặc trả thông tin của attempt hiện có theo hợp đồng tích hợp.
   - Nếu chưa có attempt mở, tạo một payment với `status = created`, amount/currency đúng theo booking và `merchant_request_id` làm idempotency key, rồi commit transaction ngắn. Nếu hai request đồng thời cùng vượt qua lần đọc trước khi ghi, `ux_payment_open_booking`/ràng buộc giao dịch phân xử request thua; Backend phải rollback phần ghi lỗi, đọc lại attempt đã commit và trả attempt đó, không tạo attempt thứ hai.
6. Backend gọi Payment Gateway ngoài transaction cơ sở dữ liệu theo adapter trung lập cổng. Provider cụ thể, endpoint và hợp đồng retry do `OQ-01` quyết định.
7. Backend trả cho Frontend thông tin cần thiết để tiếp tục thanh toán/hiển thị trạng thái theo hợp đồng đã triển khai. Redirect hoặc trang kết quả phía client chỉ là giao diện, không phải bằng chứng payment thành công.
8. Việc tiếp nhận kết quả, xác minh, áp dụng payment, xác nhận booking và phát hành ticket được chuyển cho `UC-02`; không thực hiện các transition đó trong UC-09.

### 13.6. Luồng thay thế và ngoại lệ

- **A1 — Booking không hợp lệ hoặc không thuộc Customer:** Từ chối; không tạo payment attempt và không làm lộ chi tiết booking của người khác.
- **A2 — Booking hết hạn hoặc không còn đủ điều kiện:** Từ chối khởi tạo thanh toán; UC-03 chịu trách nhiệm xử lý `expired`/`released` theo lifecycle, không gia hạn `expires_at`.
- **A3 — Hai request checkout đồng thời:** Cả hai request phải tuần tự hóa bằng khóa/transaction `Showtime → Booking → Payment attempts`. Request commit trước tạo attempt `created`; request còn lại xử lý xung đột unique hoặc đọc lại attempt đã commit và trả cùng attempt đó. Không request nào gọi gateway trước khi transaction tạo attempt của chính nó commit.
- **A4 — Timeout hoặc mất kết nối với cổng:** Không suy ra thất bại và không tạo attempt mới. Payment được xử lý thành `pending`/`unknown` theo kết quả đã xác minh; UC-04 chịu trách nhiệm đối soát.
- **A5 — Client hiển thị thành công nhưng Backend chưa áp dụng:** Giữ nguyên booking/payment theo dữ liệu Backend. Không confirm booking, chuyển detail sang `sold` hoặc tạo ticket từ redirect/client state.
- **A6 — Payment thất bại hoặc bị hủy:** Khi kết quả được UC-02 xác minh, payment có thể `failed`/`cancelled`; booking không tự chuyển trạng thái và còn hạn thì có thể thử lại theo `BR-PAY-008`.
- **A7 — Lỗi transaction hoặc retry request:** Rollback transaction tạo attempt nếu chưa commit. Nếu lỗi là xung đột payment mở, Backend đọc lại attempt đã commit theo booking và trả kết quả hiện có; không retry bằng `merchant_request_id` mới hoặc gọi gateway khi chưa có attempt commit.

### 13.7. Hậu điều kiện

- **Thành công khởi tạo:** Booking UC-01 được giữ nguyên; có đúng một payment attempt mới `created` hoặc attempt mở hiện có được sử dụng, với amount/currency lấy từ Backend.
- **Redirect thành công ở client:** Chỉ là kết quả hiển thị/điều hướng; chưa suy ra `payment.succeeded`, `applied_at`, booking `confirmed` hoặc ticket.
- **Payment result:** Do UC-02 ghi nhận và xác minh; nếu cần đối soát thì chuyển UC-04. Booking hết hạn/release do UC-03; refund, nếu có sau này, thuộc UC-04A trong phạm vi đã mô tả.
- Không tạo booking thứ hai, không phát hành ticket và không chuyển `held→sold` trong UC-09.

### 13.8. Quy tắc nghiệp vụ và ràng buộc liên quan

- `FR-PAY-001`, `BR-PAY-001..003`, `BR-PAY-008`, `AC-PAY-001..003`, `AC-PAY-004`.
- Booking được tạo bởi UC-01; payment applied và ticket issuance thuộc `FR-PAY-005`/UC-02.
- `ux_payment_open_booking` chỉ cho một payment `created`/`pending`/`unknown` trên mỗi booking; `merchant_request_id` là khóa idempotency của attempt.
- Gọi cổng ngoài transaction theo `database-design.md`, Mục 31.3; không giả định provider, endpoint, chữ ký hoặc cancellation semantics cụ thể.

### 13.9. Tiêu chí nghiệm thu

- **AC-UC09-01:** Checkout chỉ nhận booking do UC-01 tạo và kiểm tra ownership ở Backend.
- **AC-UC09-02:** Amount/currency của payment bằng dữ liệu authoritative của booking; giá client gửi không được sử dụng.
- **AC-UC09-03:** Khi booking có payment `created`/`pending`/`unknown`, không tạo attempt mở thứ hai.
- **AC-UC09-04:** Redirect hoặc success page không confirm booking và không phát hành ticket.
- **AC-UC09-05:** Timeout/unknown không tạo attempt mới và chuyển xử lý đối soát cho UC-04.
- **AC-UC09-06:** UC-09 không tạo booking, không áp dụng payment và không phát hành ticket.
- **AC-UC09-07:** Hai request checkout đồng thời cho cùng booking không tạo quá một payment attempt mở; request thua cuộc đọc lại và dùng attempt đã commit, và chỉ request có attempt đã commit mới được gọi gateway.

### 13.10. UC liên quan

- `UC-01`: tạo booking và giữ ghế.
- `UC-02`: tiếp nhận, xác minh, áp dụng payment và phát hành ticket.
- `UC-03`: hết hạn booking và giải phóng detail.
- `UC-04`: đối soát payment chưa chắc chắn hoặc thành công đến muộn.
- `UC-04A`: refund trong phạm vi hợp đồng đã mô tả, hiện ngoài demo.
- `UC-10`: Customer tra cứu trạng thái và yêu cầu hủy theo điều kiện.

### 13.11. Câu hỏi mở

- **OQ-01:** Provider, hợp đồng ACK/signature, query status, idempotency và refund chưa được chọn.
- **OQ-04:** Quyền xem payment/đối soát của Staff/Manager/Admin chưa được chốt; UC-09 không cấp các quyền đó.
- **OQ-07:** Chính sách grace period đã chốt ở mức chính sách; giá trị cấu hình cụ thể vẫn không được UC-09 tự đặt.

---

## 14. UC-10: Quản lý vòng lifecycle đặt vé của Customer (Customer Booking Lifecycle)

### 14.1. Mục tiêu

Cho phép Customer xem các booking, payment, booking detail và ticket thuộc về mình, đồng thời yêu cầu hủy booking `pending` khi thỏa điều kiện đã được phê duyệt. UC-10 phản ánh trạng thái authoritative từ Backend; không coi thời điểm hết hạn, redirect thanh toán hoặc trạng thái giao diện là transition đã commit.

### 14.2. Tác nhân

- **Tác nhân chính:** Customer.
- **Tác nhân phụ:** Backend Booking Service, Payment Service và các worker của UC-02/UC-03/UC-04/UC-04A.

### 14.3. Trigger

- Customer mở lịch sử booking, chi tiết booking hoặc danh sách ticket.
- Customer gửi yêu cầu hủy booking đang `pending`.
- Frontend làm mới trạng thái sau checkout hoặc sau khi worker/payment event đã xử lý.

### 14.4. Tiền điều kiện

- Customer đã đăng nhập và Backend xác định được user hiện tại.
- Với thao tác tra cứu, resource phải thuộc Customer hoặc được truy cập qua phạm vi quyền đã được phê duyệt; UC-10 không mở quyền tra cứu quản trị cho Customer.
- Với thao tác hủy, booking phải được đọc lại và khóa theo protocol; quyền sở hữu, booking status, detail status và toàn bộ payment attempts phải được kiểm tra lại trước khi ghi.

### 14.5. Luồng chính

1. **[Customer]** Mở lịch sử booking hoặc chi tiết một booking.
2. Backend lọc theo user hiện tại và trả các booking mà Customer được phép xem, cùng các trường authoritative như `booking.status`, `created_at`, `expires_at`, `confirmed_at`, `total_amount` và thông tin suất chiếu.
3. Backend trả payment attempts và trạng thái `payment.status`/`reconciliation_status` trong phạm vi Customer được phép xem; không suy diễn trạng thái từ URL redirect.
4. Với từng detail, Backend trả `allocation_status` (`held`, `sold`, `released`) cùng snapshot ghế/giá; các trạng thái ghế này không bị nhầm với `seat.status` vật lý.
5. Với booking đã xác nhận, Backend trả ticket có trạng thái `valid`, `used` hoặc `voided` và các mã vé theo dữ liệu đã phát hành. Ticket chỉ được hiển thị cho chủ booking theo `BR-AUTH-002`.
6. Frontend hiển thị trạng thái hiện tại và `expires_at` nếu có để diễn giải thời hạn. Việc `expires_at` đã qua chỉ cho biết booking quá hạn; chỉ UC-03 mới chuyển booking sang `expired` và detail sang `released` khi đủ điều kiện.
7. Nếu Customer yêu cầu hủy, Backend kiểm tra ownership, booking `pending`, chưa có payment applied và trạng thái các payment attempts.
8. Nếu tồn tại payment `succeeded` nhưng chưa `applied_at`, Backend không hủy booking và không giải phóng detail. Booking/payment phải được UC-02 hoặc UC-04 xử lý theo lifecycle; UC-10 không tự chọn chuyển tiếp an toàn nào cho kết quả thành công chưa áp dụng.
9. Nếu không có payment `created`/`pending`/`unknown` và cũng không có payment `succeeded` chưa applied, Backend chuyển booking sang `cancelled`, toàn bộ detail `held` sang `released` và ghi các trường hủy trong cùng transaction.
10. Nếu có payment mở, Backend gọi Cancel/Query API của cổng ngoài transaction theo hợp đồng chưa chốt tại `OQ-01`. Sau khi nhận kết quả, Backend mở một transaction mới, lấy khóa `Showtime → Booking → Payment attempts`, đọc lại booking/payment/detail và chỉ khi booking vẫn `pending`, chưa có payment applied, attempt vẫn là attempt cần hủy và kết quả gateway xác nhận chưa thu tiền thì cập nhật payment `cancelled`, booking `cancelled` và detail `released` trong cùng transaction.
11. Nếu dữ liệu đã thay đổi trong lúc gọi gateway, Backend không áp dụng mù kết quả cũ: đọc lại và đánh giá theo A5/A9; nếu đã `succeeded`, `applied`, `confirmed`, `expired` hoặc có kết quả không chắc chắn thì từ chối hủy và giữ allocation.
12. Backend trả trạng thái mới sau commit. Customer có thể bắt đầu booking mới qua UC-01; UC-10 không tự đổi ghế hoặc tạo booking thay thế.

### 14.6. Luồng thay thế và ngoại lệ

- **A1 — Booking không thuộc Customer:** Từ chối truy cập hoặc hủy theo `BR-AUTH-002`, kể cả khi Customer biết `booking_code`, `ticket_code` hoặc UUID.
- **A2 — Booking chỉ mới quá `expires_at`:** Không tự đổi trạng thái chỉ vì Frontend phát hiện hết giờ. Hiển thị dữ liệu Backend; UC-03 quyết định khi nào `expired`/`released`.
- **A3 — Booking đã `confirmed`:** Từ chối hủy theo chính sách demo không hủy sau thanh toán; không tự tạo refund. Ticket và detail giữ trạng thái hiện tại.
- **A4 — Booking đã `expired` hoặc `cancelled`:** Từ chối hủy lại; không thay đổi detail/payment/ticket.
- **A5 — Payment `created`/`pending`/`unknown`:** Không giải phóng ghế trực tiếp. Gọi Cancel/Query ngoài transaction; sau đó phải mở transaction mới, khóa và đọc lại booking/payment/detail. Nếu gateway xác nhận đã thu hoặc kết quả không chắc chắn, hoặc dữ liệu đã chuyển trạng thái trong lúc chờ, từ chối hủy và giữ allocation.
- **A6 — Payment `succeeded` nhưng chưa applied:** Không coi `succeeded` là booking `confirmed` hoặc ticket đã phát hành, nhưng cũng không coi đây là điều kiện cho phép hủy. Không hủy booking, không giải phóng ghế và không tạo refund; chuyển việc xác định kết quả cho UC-02/UC-04 theo lifecycle.
- **A7 — Payment `succeeded` và đã applied:** Không hủy booking, không void ticket và không tạo refund trong demo.
- **A8 — Payment thành công đến sau khi booking đã release:** Hiển thị payment `succeeded`/đối soát theo dữ liệu Backend; không phục hồi booking, không reclaim ghế và không phát hành ticket. Xử lý tài chính tiếp theo thuộc UC-04/UC-04A theo phạm vi đã chốt.
- **A9 — Concurrent cancellation/payment confirmation:** Cancellation gọi gateway ngoài transaction; sau đó transaction cập nhật phải lấy `Showtime → Booking → Payment attempts`, đọc lại mọi trạng thái và chỉ commit nếu điều kiện hủy vẫn còn đúng. Nếu payment confirmation hoặc transition khác đã commit trong lúc gọi gateway, cancellation bị từ chối hoặc được đánh giá lại; không cập nhật một phần.
- **A10 — Refund chưa hoàn tất:** Booking cancellation và refund completion là hai sự kiện khác nhau. `refund.status` chỉ có ý nghĩa khi workflow refund được kích hoạt; UC-10 không tuyên bố quyền lợi refund chưa được nguồn phê duyệt.

### 14.7. Hậu điều kiện

- **Tra cứu:** Chỉ dữ liệu booking/payment/detail/ticket của Customer được trả; không phát sinh thay đổi nghiệp vụ.
- **Hủy thành công trước thanh toán:** Booking `cancelled`, mọi detail `held` thành `released`; payment mở, nếu có, đã được cổng xác nhận chưa thu và payment `cancelled` cùng transaction với booking/detail. Payment `succeeded` chưa applied không thuộc nhánh hủy thành công.
- **Hủy thất bại hoặc bị từ chối:** Booking, detail, payment và ticket giữ nguyên; không tạo refund hoặc booking mới.
- **Không đồng nhất do timeout:** Customer thấy trạng thái authoritative sau khi truy vấn lại; không suy luận thành công từ giao diện.

### 14.8. Quy tắc nghiệp vụ và ràng buộc liên quan

- `FR-BOOK-003`, `FR-BOOK-004`, `FR-TICKET-001`, `BR-AUTH-002`, `BR-BOOK-007..009`, `BR-PAY-002`, `BR-PAY-003`, `BR-PAY-007`, `BR-PAY-008`.
- `AC-BOOK-003..006`, `AC-PAY-001..005`, `AC-SEC-002..003`.
- Booking, detail, payment, ticket và refund là các vòng đời độc lập; trạng thái hợp lệ phải khớp schema. `refund` là cấu trúc mở rộng ngoài demo, không phải tính năng Customer mặc định.
- Gọi gateway, đối soát, expiration/release và payment application lần lượt thuộc UC-02, UC-03, UC-04 và UC-04A theo phạm vi.

### 14.9. Tiêu chí nghiệm thu

- **AC-UC10-01:** Customer chỉ xem được booking/payment/detail/ticket của mình.
- **AC-UC10-02:** Backend trả đúng các trạng thái schema: booking `pending`/`confirmed`/`expired`/`cancelled`; detail `held`/`sold`/`released`; payment `created`/`pending`/`unknown`/`succeeded`/`failed`/`cancelled`; ticket `valid`/`used`/`voided`.
- **AC-UC10-03:** Booking chỉ quá `expires_at` không bị Frontend tự chuyển thành `expired` hoặc `released`.
- **AC-UC10-04:** Hủy booking `pending` không có payment mở chuyển `cancelled` và `held→released` trong một transaction.
- **AC-UC10-05:** Payment mở chỉ được hủy sau Cancel/Query xác nhận chưa thu; payment đã thu hoặc không chắc chắn giữ nguyên allocation và bị từ chối.
- **AC-UC10-06:** Payment thành công không tự suy ra booking `confirmed` hoặc ticket đã phát hành; chỉ UC-02 áp dụng payment khi đủ điều kiện.
- **AC-UC10-07:** Hủy booking sau payment applied và refund completion không được biến thành tính năng demo.
- **AC-UC10-08:** Payment `succeeded` nhưng chưa `applied_at` không cho phép hủy hoặc giải phóng ghế; UC-02/UC-04 phải xử lý kết quả theo lifecycle.
- **AC-UC10-09:** Sau Cancel/Query, payment cancellation cùng booking cancellation và detail release chỉ được commit atomically sau khi khóa và đọc lại trạng thái; kết quả gateway cũ không được áp dụng nếu dữ liệu đã thay đổi.

### 14.10. UC liên quan

- `UC-01`: tạo booking và giữ ghế mới.
- `UC-02`: payment event, payment application và ticket issuance.
- `UC-03`: expiration/release thực tế.
- `UC-04`: reconciliation.
- `UC-04A`: refund behavior trong phạm vi hợp đồng, hiện ngoài demo.
- `UC-05`: sử dụng ticket tại rạp.

### 14.11. Câu hỏi mở

- **OQ-01:** Hợp đồng Cancel/Query, trạng thái gateway và refund chưa thể đặc tả provider-specifically.
- **OQ-04:** Quyền tra cứu sâu hơn của Staff/Manager/Admin không mở rộng cho Customer.
- **OQ-05:** Chính sách quyền lợi khách khi rạp hủy suất vẫn mở; UC-10 không tự thêm cancellation/refund workflow.
- **OQ-07:** Chính sách grace period đã chốt, nhưng thời lượng cấu hình cụ thể vẫn do triển khai quyết định; UC-10 không tự giải phóng ghế.

---

## 15. Tính nhất quán giữa các UC (Cross-UC lifecycle)

Bảng chuyển trạng thái cốt lõi (giá trị khớp schema — mục 2):
| Sự kiện | booking | booking_detail | payment | payment_event | ticket | refund |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| UC-01 thành công | `pending` | `held` | — | — | — | — |
| UC-02 áp dụng thành công | `confirmed` (+`confirmed_at`) | `held→sold` | `succeeded` + `applied_at` | `processed` | N×`valid` (N = capacity_units_snapshot) | — |
| UC-02 thành công nhưng không áp dụng | giữ nguyên (`pending`/`expired`/`cancelled`) | giữ nguyên | `succeeded`, **không** `applied_at`; `reconciliation_status` = `needs_review`/`refund_required` | `processed` | — | — |
| UC-03 hết hạn (điều kiện hai phần + grace) | `pending→expired` | `held→released` (+`released_at`) | attempt mở phải chốt trước (hoặc qua grace) | — | — | — |
| UC-03 bỏ qua (grace period/needs_review) | `pending` | `held` | `created`/`pending`/`unknown` | — | — | — |
| Payment thành công muộn (sau release) | `expired` (không reclaimed) | `released` | `succeeded`, không applied | `processed` | — | `refund_required` → UC-04A khi kích hoạt |
| Khách hủy (FR-BOOK-003; qua cổng xác nhận chưa thu nếu có attempt) | `cancelled` | `held→released` | attempt mở → `cancelled` (từ cổng) | — | — | — |
| UC-04 query chốt kết quả | (theo UC-02 bước 5/6) | (như trên) | `unknown→succeeded`/`failed` | `processed` | (như UC-02) | — |
| UC-04A hoàn tiền xong | giữ nguyên | giữ nguyên | `reconciliation_status='resolved'` | — | giữ nguyên | `succeeded` + `completed_at` |
| UC-05 soát | `confirmed` | `sold` | — | — | `valid→used` | — |

Điểm nhất quán cần nhớ:

- **Hết hạn ≠ giải phóng:** "quá hạn" (vượt `expires_at`) là điều kiện; `expired`/`released` là kết quả do worker thực hiện khi điều kiện hai phần thỏa (hoặc grace period đã xử lý attempt).
- **Succeeded ≠ applied:** payment thành công và áp dụng payment là hai kết quả độc lập; chỉ applied xác nhận booking và phát hành vé.
- **Confirmed ⇒ đã phát hành đúng số vé:** booking `confirmed` phải có đúng `capacity_units_snapshot` vé/detail; callback lặp không phát hành thừa (idempotent).
- **Refund accepted ≠ refund completed:** chỉ `refund.status = 'succeeded'` mới dẫn tới `reconciliation_status = 'resolved'`.
- **Idempotency & retry:** mọi luồng lặp (webhook trùng, worker chạy lại, refund retry, quét lại) phải idempotent nhờ khóa sự kiện/UQ + điều kiện cập nhật theo trạng thái; không luồng nào sinh tác động nghiệp vụ thứ hai từ cùng sự kiện.

## 16. Câu hỏi mở và traceability

| ID | Mâu thuẫn / quyết định còn thiếu | Vì sao quan trọng | Hành vi an toàn tạm (đề xuất, chưa phải quyết định) | Ảnh hưởng |
| :--- | :--- | :--- | :--- | :--- |
| **OQ-UC-02** | Mức tự động hóa đối soát (UC-04) chưa có chính sách: khi nào `unknown` leo thang `needs_review`? | Worker cần ngưỡng leo thang; không có ngưỡng, `unknown` có thể treo vô hạn ở hàng retry. | Chờ theo chu kỳ `next_reconcile_at`; leo thang khi vượt ngưỡng cấu hình (khi chốt). | UC-04; cấu hình worker; giám sát. |
| **OQ-UC-03** | Giá trị grace period chính xác (OQ-07 đã chốt **chính sách** — đề xuất 2–3 phút — chưa chốt **số liệu**). | Quyết định tổng thời gian ghế bị khóa tối đa. | Cấu hình triển khai; đề xuất mặc định 120 giây. | UC-03 (bước 4a), UC-02 (A9), cấu hình. |
| **OQ-UC-04** | Chu kỳ retry đối soát/refund (`next_reconcile_at`/`next_retry_at`) — khoảng chờ và số lần tối đa chưa định nghĩa. | Retry vô hạn làm ngập hàng đợi; ngừng quá sớm mất cơ hội chốt kết quả. | Exponential backoff có trần (khi chốt); không giới hạn số lần cho đến khi có kết quả. | UC-04, UC-04A; cấu hình worker; test retry. |
| **OQ-UC-05** | Chuẩn hóa `request_fingerprint`: danh sách ghế có được sort trước khi băm không (đổi thứ tự chọn = cùng nội dung hay khác nội dung)? | Quyết định replay/conflict của UC-01 A1/A2. | Sort trước khi băm (đổi thứ tự = cùng nội dung) [PROPOSED]. | UC-01; API request chuẩn hóa; test idempotency. |
| **OQ-UC-06** | `payment_event` không có cột lưu chứng cứ/trạng thái xác minh chữ ký (payload_summary chỉ jsonb đã lọc) — có cần lưu để worker xác minh lại không? | Kiến trúc xác minh ingress-vs-worker cho UC-02. | Xác minh một lần ở ingress; worker chỉ xác minh nghiệp vụ [PROPOSED]. | UC-02; thiết kế payment_event nếu cần thêm cột (schema change — cần phê duyệt riêng). |
| **OQ-UC-07** | Ứng dụng staff có chọn suất trước khi quét không (ngữ cảnh soát)? | Ảnh hưởng kiểm tra "vé thuộc suất đang soát" của UC-05. | Cho phép soát không chọn trước: hiển thị suất/phòng của vé, vẫn kiểm cửa sổ [PROPOSED]. | UC-05; UX staff app; API soát vé. |
| OQ-01 | Cổng thanh toán cụ thể (`business-system-analysis.md` §13). | Hợp đồng ACK/signature/query/refund/idempotency của cổng. | Middleware trung lập cổng; chi tiết theo adapter. | UC-02, UC-04A, UC-09, UC-10; API spec. |
| OQ-03 | Cửa sổ soát vé (`business-system-analysis.md` §13). | Tiền đề UC-05 bước 4. | Lấy từ cấu hình; từ chối ngoài cửa sổ. | UC-05; cấu hình rạp. |
| OQ-04 | Ranh giới quyền Staff/Manager/Admin (`business-system-analysis.md` §13). | Authz UC-04, UC-05, UC-08, quản trị và các quyền tra cứu/thanh toán. | Staff = soát vé; Manager/Admin theo PROPOSED. | UC-04, UC-05, UC-08, UC-09, UC-10; authorization. |

---

## 17. Truy vết nguồn (traceability)

- Quy tắc nghiệp vụ: `docs/requirements/business-system-analysis.md` — §7 (FR), §8 (BR), §12 (AC), §13 (OQ).
- Use case tổng quan: `docs/analysis and design/business-system-design.md` — UC-01..UC-05; tài liệu này đặc tả chi tiết UC-06..UC-10.
- Thiết kế entity/concurrency: `database/database-design.md` — §12–15 (entity), §23 (constraints), §26–27 (schedule/seat), §31–34 (transaction/concurrency protocol), §37 (checklist).
- Mô hình quan hệ: `docs/analysis and design/relational-model.md` — §3 (relation), §9 (invariants).
- Schema vật lý: `database/database-schema.md` — §3.1 (movie), §3.13–3.18, §4; DDL: `database/database-schema.sql`.
- Endpoint duyệt phim/lịch chiếu: `docs/api/api-endpoints.md` — §3 (GET `/api/movies`, GET `/api/movies/{movieId}`, GET `/api/movies/{movieId}/showtimes`), §4 (GET `/api/showtimes/{showtimeId}`).
- Endpoint xác thực, booking, payment và ticket: `docs/api/api-endpoints.md` — §2, §5–§7; các endpoint vẫn được ghi nhận là Proposed và không thay thế yêu cầu nghiệp vụ.
- Đối chiếu ngữ nghĩa hủy booking: `FR-BOOK-003`, `BR-BOOK-008`, `AC-BOOK-004..006` quy định ownership, trạng thái `pending`, payment mở/succeeded, kết quả Cancel/Query và việc cập nhật `booking`/`booking_detail` trong transaction; `database/database-design.md` §31.1 và §31.5 quy định thứ tự khóa và gọi gateway ngoài transaction.
