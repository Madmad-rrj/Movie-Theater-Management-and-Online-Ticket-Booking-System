# Customer Screen Specification

## 1. Mục đích

Đặc tả chức năng và nội dung của từng màn hình Customer. Tài liệu trả lời câu hỏi: mỗi màn hình có gì?

## 2. Danh sách màn hình

1. Home (Danh sách phim)
2. Movie Detail
3. Showtime (Lịch chiếu)
4. Seat Selection (Sơ đồ ghế)
5. Payment
6. Booking Confirmation
7. Đăng nhập / Đăng ký
8. Tài khoản (Booking của tôi)

## 3. Đặc tả từng màn hình

### 3.1. Home

#### 3.1.1. Mục đích và phạm vi

- Là điểm vào chính của website Customer.
- Giới thiệu nhanh hai nhóm phim `now_showing` và `coming_soon`.
- Cho phép mở Movie Detail hoặc chuyển sang Movie List đã lọc theo nhóm tương ứng.
- Home không thay thế Movie List: không cung cấp toàn bộ phân trang/bộ lọc và không tự xếp hạng phim khi nghiệp vụ chưa quy định.

**Route và quyền truy cập:** `/`, Public.

**Yêu cầu liên quan:** FR-MOVIE-001; `customer-user-flow.md` mục 3.1; `customer-navigation.md` mục 3 và 5.1.

#### 3.1.2. Nguồn dữ liệu

Khi vào Home, frontend tải độc lập hai nhóm:

- `GET /api/movies?status=now_showing&page=1&pageSize=...`
- `GET /api/movies?status=coming_soon&page=1&pageSize=...`

`pageSize` dùng theo cấu hình bố cục; tài liệu không chốt một số lượng card cố định. Mỗi nhóm giữ đúng thứ tự backend trả về, không tự suy diễn phim nổi bật hoặc độ phổ biến.

Movie card chỉ sử dụng dữ liệu hiện có trong `MovieSummary`:

| Trường | Hiển thị | Quy tắc |
|---|---|---|
| `id` | Không hiển thị trực tiếp | Dùng tạo route Movie Detail |
| `posterUrl` | Poster phim | Thiếu/ảnh lỗi dùng placeholder, không làm mất card |
| `title` | Tên phim | Bắt buộc, là tiêu đề card |
| `ageRating` | Nhãn phân loại tuổi | Nếu NULL thì ẩn nhãn; không tự suy ra độ tuổi |
| `durationMinutes` | Thời lượng theo phút | Hiển thị định dạng giờ/phút hoặc số phút nhất quán |
| `status` | Nhãn "Đang chiếu"/"Sắp chiếu" | Chỉ nhận `now_showing` hoặc `coming_soon` ở hai khu vực này |

Home không hiển thị mô tả, trailer, ngôn ngữ hoặc thể loại nếu API danh sách chưa trả các trường đó.

#### 3.1.3. Cấu trúc màn hình

1. **Header toàn cục** — theo `customer-navigation.md`.
2. **Giới thiệu trang** — tiêu đề/ngữ cảnh ngắn; không yêu cầu banner dữ liệu riêng vì mô hình hiện tại chưa có trường banner/featured.
3. **Phim đang chiếu**
   - Heading "Phim đang chiếu".
   - Danh sách movie card lấy từ `now_showing`.
   - Link "Xem tất cả" tới Movie List với `status=now_showing`.
4. **Phim sắp chiếu**
   - Heading "Phim sắp chiếu".
   - Danh sách movie card lấy từ `coming_soon`.
   - Link "Xem tất cả" tới Movie List với `status=coming_soon`.
5. **Footer toàn cục** — theo `customer-navigation.md`.

Hai khu vực phim là độc lập: một khu vực lỗi/rỗng không được che khu vực còn lại đang có dữ liệu.

#### 3.1.4. Movie card và thao tác

Toàn bộ card hoặc nút/link "Xem chi tiết" là một hành động điều hướng tới `/movies/:movieId`.

| Thao tác | Điều kiện | Kết quả |
|---|---|---|
| Chọn movie card | Movie còn tồn tại trong response | Mở Movie Detail với `movieId` |
| Chọn "Xem tất cả" ở phim đang chiếu | Luôn khả dụng | Mở Movie List `status=now_showing` |
| Chọn "Xem tất cả" ở phim sắp chiếu | Luôn khả dụng | Mở Movie List `status=coming_soon` |
| Back từ Movie Detail/Movie List | Có lịch sử Home | Khôi phục vị trí cuộn và trạng thái tải trong UI session khi có thể |

Home không tạo booking, không giữ ghế và không điều hướng thẳng đến Seat Selection khi chưa chọn phim/suất.

#### 3.1.5. Trạng thái giao diện

| Trạng thái | Hiển thị | Hành động |
|---|---|---|
| Loading ban đầu | Skeleton card riêng cho từng khu vực; giữ cấu trúc heading | Không chặn Header/Footer |
| Success | Render các card hợp lệ | Cho phép mở chi tiết/xem tất cả |
| Empty `now_showing` | "Hiện chưa có phim đang chiếu" | Cho phép xem phim sắp chiếu |
| Empty `coming_soon` | "Hiện chưa có phim sắp chiếu" | Cho phép xem phim đang chiếu |
| Một request lỗi | Error state trong đúng khu vực | Nút "Thử lại" chỉ gọi lại request bị lỗi |
| Cả hai request lỗi | Hai error state hoặc một lỗi cấp trang trong main | Cho phép thử lại; Header/Footer vẫn hoạt động |
| Poster lỗi/NULL | Placeholder có tỷ lệ như poster | Card vẫn điều hướng được |

Không hiển thị stack trace, endpoint hoặc chi tiết kỹ thuật cho Customer.

#### 3.1.6. Khả dụng và accessibility

- Movie card có tên truy cập chứa tên phim; không chỉ dựa vào ảnh.
- Poster có `alt` phù hợp hoặc alt rỗng nếu tiêu đề cạnh ảnh đã cung cấp cùng thông tin.
- Link/nút dùng được bằng bàn phím và có focus state rõ ràng.
- Heading theo thứ bậc: tiêu đề trang → tiêu đề từng khu vực → tiêu đề card.
- Nhãn trạng thái/phân loại tuổi không chỉ phân biệt bằng màu.

#### 3.1.7. Tiêu chí nghiệm thu

- AC-HOME-01: Home gọi đúng hai truy vấn trạng thái và không trộn phim giữa hai khu vực.
- AC-HOME-02: Card hiển thị poster/fallback, tên, thời lượng, phân loại tuổi khi có và nhãn trạng thái.
- AC-HOME-03: Chọn card mở đúng Movie Detail theo `movieId`.
- AC-HOME-04: Hai link "Xem tất cả" mở Movie List với đúng `status`.
- AC-HOME-05: Loading, empty, error và retry hoạt động độc lập cho từng khu vực.
- AC-HOME-06: Thiếu poster/age rating không làm màn hình lỗi và UI không tự tạo dữ liệu thay thế mang ý nghĩa nghiệp vụ.
- AC-HOME-07: Home truy cập được khi chưa đăng nhập và navigation toàn cục hoạt động đúng.

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

- **Mục đích:** cho khách chọn ghế và loại vé, khởi tạo booking giữ ghế 5 phút.
- **Bố cục:** sơ đồ ghế theo hàng (row_label + seat_number), chú thích loại ghế (Standard/VIP/Couple), tổng tiền tạm tính, đồng hồ đếm ngược giữ ghế.
- **Dữ liệu hiển thị:** trạng thái ghế (trống / đã bán / đang chọn), loại ghế, phụ thu, giá từng ghế.
- **Nút thao tác:** chọn/bỏ chọn ghế, chọn loại vé cho từng ghế, "Đặt vé".
- **Validation:** ghế Couple chọn theo đơn vị ghế đôi; ghế đã bị người khác giữ/bán → thông báo và cập nhật sơ đồ.
- **Tiêu chí nghiệm thu:** giữ ghế thành công tạo booking; hết 5 phút → thông báo hết hạn, ghế được giải phóng.

### 3.5. Payment

- **Mục đích:** thực hiện thanh toán booking trong thời gian giữ ghế.
- **Bố cục:** tóm tắt booking (mã, suất, ghế, tổng tiền), chọn phương thức thanh toán, nút thanh toán.
- **Dữ liệu hiển thị:** `booking.total_amount` (VND), thời gian còn lại, chi tiết từng detail (giá đã snapshot).
- **Nút thao tác:** "Thanh toán", hủy booking.
- **Validation:** tổng tiền không được sửa; hết thời gian giữ ghế → chặn thanh toán, hiển thị expired.
- **Tiêu chí nghiệm thu:** thanh toán thành công chuyển sang Booking Confirmation; thất bại giữ booking `pending` cho phép retry theo chính sách.

### 3.6. Booking Confirmation

- **Mục đích:** xác nhận đặt vé thành công, hiển thị vé.
- **Bố cục:** mã booking, thông tin suất/ghế, danh sách vé với mã QR riêng cho từng vé (ghế Couple phát hành 2 vé riêng).
- **Dữ liệu hiển thị:** `booking_code`, giờ chiếu, phòng, ghế, mã vé/QR, trạng thái vé `valid`.
- **Nút thao tác:** về trang chủ, xem "Booking của tôi".
- **Tiêu chí nghiệm thu:** số vé hiển thị đúng số ghế/capacity_units; trạng thái `confirmed` chỉ xuất hiện sau khi payment được áp dụng.

### 3.7. Đăng nhập / Đăng ký

- **Mục đích:** xác thực người dùng trước khi đặt vé.
- **Dữ liệu hiển thị:** form email, mật khẩu; đăng ký chỉ cấp role `CUSTOMER`.
- **Validation:** định dạng email, mật khẩu bắt buộc; lỗi xác thực hiển thị thông báo chung.
- **Tiêu chí nghiệm thu:** đăng nhập thành công quay lại màn hình trước khi bị chuyển hướng.

### 3.8. Tài khoản (Booking của tôi)

- **Mục đích:** xem lịch sử booking và vé của user.
- **Dữ liệu hiển thị:** danh sách booking với trạng thái (`pending`, `confirmed`, `expired`, `cancelled`), vé theo booking.
- **Tiêu chí nghiệm thu:** chỉ hiển thị booking của user hiện tại (authorization ở backend).
