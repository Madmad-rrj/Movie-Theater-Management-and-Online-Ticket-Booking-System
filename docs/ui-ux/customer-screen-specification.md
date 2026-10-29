# Customer Screen Specification

## 1. Mục đích

Đặc tả chức năng và nội dung của từng màn hình Customer. Tài liệu trả lời câu hỏi: mỗi màn hình có gì?

## 2. Danh sách màn hình

1. Home
2. Movie List (Danh sách phim)
3. Movie Detail
4. Showtime (Lịch chiếu)
5. Seat Selection (Sơ đồ ghế)
6. Payment
7. Booking Confirmation
8. Đăng nhập / Đăng ký
9. Tài khoản (Booking của tôi)

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

### 3.2. Movie List

#### 3.2.1. Mục đích và phạm vi

- Cung cấp danh sách phim đầy đủ theo từng trạng thái được phép hiển thị cho Customer.
- Cho phép Customer duyệt danh sách, chuyển trang và mở Movie Detail của một phim.
- Chỉ hiển thị phim `now_showing` hoặc `coming_soon`; không đưa `ended`/`archived` vào danh sách Customer theo UC-06 và FR-MOVIE-001.
- Không tạo booking, giữ ghế hoặc xác nhận phim còn suất chiếu từ dữ liệu danh sách.

**Route và quyền truy cập:** `/movies`, Public.

**Yêu cầu liên quan:** FR-MOVIE-001, FR-MOVIE-002; UC-06; `customer-user-flow.md` mục 3.1–3.2; `customer-screen-api-mapping.md` mục 3.1.

#### 3.2.2. Nguồn dữ liệu và query

Movie List gọi `GET /api/movies` với các query đã có trong DTO:

| Query | Giá trị/phạm vi | Hành vi |
|---|---|---|
| `status` | `now_showing` hoặc `coming_soon` | Xác định nhóm phim đang xem |
| `page` | Trang hiện tại | Được đồng bộ với URL để Back/Forward khôi phục đúng trang |
| `pageSize` | Theo cấu hình giao diện/API | Không cho Customer nhập tùy ý |

Response gồm mảng `MovieSummary` và metadata `pagination`. Mỗi movie card sử dụng:

| Trường | Hiển thị | Quy tắc |
|---|---|---|
| `id` | Không hiển thị trực tiếp | Dùng tạo route Movie Detail |
| `posterUrl` | Poster | NULL/ảnh lỗi dùng placeholder, card vẫn hoạt động |
| `title` | Tên phim | Tiêu đề chính của card |
| `ageRating` | Phân loại tuổi | NULL thì ẩn nhãn, không tự suy diễn |
| `status` | "Đang chiếu"/"Sắp chiếu" | Phải phù hợp bộ lọc hiện tại |
| `durationMinutes` | Thời lượng | Định dạng nhất quán, không tự ước lượng khi thiếu |

Không hiển thị thể loại trên card khi `MovieSummary` hiện chưa trả `genres`.

#### 3.2.3. Tìm kiếm, lọc và sắp xếp

- **Lọc trạng thái:** cung cấp hai lựa chọn "Phim đang chiếu" và "Phim sắp chiếu"; đổi trạng thái đưa `page` về trang đầu và tải lại từ backend.
- **Phân trang:** dùng metadata từ API; chuyển trang gọi lại API và đưa vùng đầu danh sách vào tầm nhìn/focus phù hợp.
- **Tìm kiếm:** UC-06 yêu cầu khả năng tìm kiếm nhưng DTO hiện chưa định nghĩa query tìm kiếm. Không hiển thị ô tìm kiếm hoạt động giả hoặc lọc cục bộ trên một trang dữ liệu; chỉ bổ sung khi hợp đồng API chốt tên query, phạm vi và quy tắc đối sánh.
- **Sắp xếp:** API chưa định nghĩa trường/thứ tự sắp xếp. Giao diện giữ nguyên thứ tự backend trả về và không tự gắn nhãn "mới nhất", "phổ biến" hoặc "đánh giá cao".
- **Bộ lọc khác:** thể loại, độ tuổi và ngôn ngữ chưa thuộc query hiện có; không tự thêm cho đến khi yêu cầu/API được chốt.

Các giới hạn trên giữ nguyên câu hỏi mở tại UC-06, không được xem là quyết định loại bỏ tìm kiếm/lọc/sắp xếp khỏi các phiên bản sau.

#### 3.2.4. Cấu trúc và thao tác

1. Header toàn cục theo `customer-navigation.md`.
2. Tiêu đề "Danh sách phim" và lựa chọn trạng thái hiện tại.
3. Vùng thông báo loading/error/empty gắn với truy vấn hiện tại.
4. Grid movie card theo `customer-web-layout.md` và `customer-responsive-specification.md`.
5. Điều khiển phân trang khi metadata cho biết có nhiều trang.
6. Footer toàn cục.

| Thao tác | Điều kiện | Kết quả |
|---|---|---|
| Chọn "Phim đang chiếu" | Luôn khả dụng | Cập nhật `status=now_showing`, về trang đầu và tải lại |
| Chọn "Phim sắp chiếu" | Luôn khả dụng | Cập nhật `status=coming_soon`, về trang đầu và tải lại |
| Chọn trang | Trang tồn tại theo metadata | Cập nhật `page` trên URL và tải trang tương ứng |
| Chọn movie card | Phim có `id` hợp lệ trong response | Mở `/movies/:movieId` |
| Back từ Movie Detail | Có lịch sử Movie List | Khôi phục query, trang và vị trí cuộn trong UI session khi có thể |

Chọn phim chỉ chuyển ngữ cảnh `movieId`; khả năng xem/đặt suất phải được truy vấn lại ở bước sau.

#### 3.2.5. Trạng thái giao diện

| Trạng thái | Hiển thị | Hành động |
|---|---|---|
| Loading ban đầu/đổi query | Skeleton movie card, giữ tiêu đề và bộ lọc hiện tại | Chặn thao tác phân trang lặp trong request |
| Success | Grid card và phân trang theo response | Cho phép mở Movie Detail |
| Empty theo trạng thái | "Hiện chưa có phim đang chiếu/sắp chiếu" | Cho phép chuyển sang trạng thái còn lại |
| Không có kết quả tìm kiếm | Chỉ áp dụng sau khi API tìm kiếm được chốt | Cho sửa/xóa từ khóa, không coi là lỗi hệ thống |
| Lỗi API/timeout | Error state thân thiện | "Thử lại" với đúng query hiện tại |
| Poster/age rating thiếu | Placeholder hoặc ẩn nhãn | Không loại bỏ card |
| Movie Detail trả 404 sau khi chọn | Thông báo phim không còn khả dụng | Cho quay lại và tải lại Movie List |

Không hiển thị stack trace, endpoint hoặc thông tin nội bộ cho Customer.

#### 3.2.6. Khả dụng và accessibility

- Bộ lọc trạng thái thể hiện trạng thái đang chọn bằng cả ngữ nghĩa và hình ảnh, không chỉ bằng màu.
- Mỗi movie card có accessible name chứa tên phim và dùng được bằng bàn phím.
- Poster có `alt` phù hợp hoặc alt rỗng nếu tên phim cạnh ảnh đã cung cấp cùng thông tin.
- Loading được thông báo phù hợp cho công nghệ hỗ trợ mà không liên tục giành focus.
- Sau khi đổi trang, focus/vị trí cuộn được quản lý nhất quán để Customer nhận biết nội dung mới.

#### 3.2.7. Tiêu chí nghiệm thu

- AC-MOVIE-LIST-01: Chỉ các phim `now_showing`/`coming_soon` phù hợp query được hiển thị.
- AC-MOVIE-LIST-02: Card chỉ dùng dữ liệu có trong `MovieSummary` và xử lý trường tùy chọn bị thiếu.
- AC-MOVIE-LIST-03: Đổi trạng thái hoặc trang gọi lại API với query đúng và đồng bộ URL.
- AC-MOVIE-LIST-04: Chọn card mở đúng Movie Detail theo `movieId`.
- AC-MOVIE-LIST-05: Loading, empty, error và retry được phân biệt rõ; danh sách rỗng không bị coi là lỗi.
- AC-MOVIE-LIST-06: Giao diện không cung cấp tìm kiếm/sắp xếp giả khi API chưa hỗ trợ và không tự suy diễn thứ tự nghiệp vụ.
- AC-MOVIE-LIST-07: Movie List truy cập được khi chưa đăng nhập và không tạo booking/giữ ghế/payment.

### 3.3. Movie Detail

#### 3.3.1. Mục đích và phạm vi

- Cung cấp thông tin đầy đủ của một phim để Customer quyết định có xem lịch chiếu hay không.
- Là điểm chuyển từ Home/Movie List sang Showtime, giữ đúng `movieId` đang xem.
- Chỉ hiển thị dữ liệu phim do backend trả về; không suy diễn lịch chiếu, khả năng còn ghế hoặc khả năng đặt vé.
- Không tạo booking, giữ ghế hoặc yêu cầu đăng nhập.

**Route và quyền truy cập:** `/movies/:movieId`, Public.

**Yêu cầu liên quan:** FR-MOVIE-002; UC-06; `customer-user-flow.md` mục 3.2; `customer-navigation.md` mục 4 và 5.1; `customer-screen-api-mapping.md` mục 3.2.

#### 3.3.2. Dữ liệu đầu vào và tải màn hình

- Đọc `movieId` từ route và gọi `GET /api/movies/{movieId}` khi vào màn hình hoặc mở deep link.
- Response `MovieDetail` là nguồn dữ liệu authoritative của màn hình; dữ liệu card đã giữ từ Home/Movie List chỉ có thể dùng làm placeholder trong lúc tải, không thay thế response chi tiết.
- Khi Customer chọn xem lịch chiếu, chỉ chuyển `movieId`; Showtime phải tải lịch hiện tại từ backend.
- Backend quyết định phim có tồn tại và được phép công khai. Frontend không tự cho phép phim dựa trên dữ liệu cache cũ.

#### 3.3.3. Thông tin hiển thị

| Trường `MovieDetail` | Cách hiển thị | Khi thiếu/không hợp lệ |
|---|---|---|
| `id` | Không hiển thị trực tiếp; dùng cho route | Không render màn hình chi tiết nếu response không xác định được phim |
| `title` | Tiêu đề chính của trang | Không tự đặt tên thay thế mang ý nghĩa nghiệp vụ |
| `posterUrl` | Poster phim | Dùng placeholder cùng tỷ lệ khi NULL hoặc ảnh lỗi |
| `description` | Nội dung giới thiệu phim | Hiển thị trạng thái chưa có mô tả hoặc ẩn khối theo quy ước UI |
| `durationMinutes` | Thời lượng, định dạng giờ/phút nhất quán | Không tự ước lượng |
| `genres` | Danh sách tên thể loại; `code` dùng làm định danh khi cần | Mảng rỗng thì ẩn danh sách, không tự gán thể loại |
| `ageRating` | Nhãn phân loại tuổi | NULL thì hiển thị chưa có thông tin/ẩn nhãn; không suy ra quyền mua |
| `releaseDate` | Ngày phát hành theo định dạng giao diện | NULL thì ẩn trường; không tạo ngày thay thế |
| `language` | Ngôn ngữ phim | NULL thì ẩn trường; không suy đoán từ nội dung |
| `trailerUrl` | Link/nút xem trailer khi có URL sử dụng được | NULL/không dùng được thì ẩn hành động trailer |
| `status` | Nhãn "Đang chiếu" hoặc "Sắp chiếu" theo giá trị backend | Không đổi trạng thái dựa trên ngày phía client |

Phạm vi Customer hiện xác nhận danh sách công khai cho `now_showing` và `coming_soon`. Việc phim `ended` có được mở bằng deep link hay không vẫn là câu hỏi mở của UC-06; màn hình không hard-code quyết định này và tuân theo kết quả API.

#### 3.3.4. Cấu trúc màn hình

1. **Header toàn cục** theo `customer-navigation.md`.
2. **Ngữ cảnh quay lại** — Back của trình duyệt hoặc liên kết điều hướng phù hợp; khi quay lại danh sách, khôi phục query/vị trí trong UI session nếu còn.
3. **Khối thông tin chính**
   - Desktop: poster ở cột trái; tên phim, trạng thái, metadata và hành động ở cột phải.
   - Mobile: poster và nội dung xếp dọc theo đặc tả responsive.
4. **Metadata phim** — thời lượng, thể loại, phân loại tuổi, ngày phát hành và ngôn ngữ khi có.
5. **Mô tả phim** — chỉ render nội dung backend cung cấp.
6. **Trailer** — hành động tùy chọn khi có `trailerUrl` hợp lệ.
7. **Hành động chính "Xem lịch chiếu"**.
8. **Footer toàn cục**.

#### 3.3.5. Thao tác và điều hướng

| Thao tác | Điều kiện | Kết quả |
|---|---|---|
| Chọn "Xem lịch chiếu" | Movie Detail tải thành công và có `movieId` | Mở `/movies/:movieId/showtimes`, giữ đúng `movieId` |
| Chọn "Xem trailer" | Có `trailerUrl` sử dụng được | Mở/phát trailer theo component được phê duyệt; không thay đổi route đặt vé |
| Back | Có lịch sử Home/Movie List | Quay lại và khôi phục filter, trang, vị trí cuộn trong UI session khi có thể |
| Mở deep link | `movieId` hợp lệ | Tải trực tiếp chi tiết từ API, không phụ thuộc màn hình trước |

Không vô hiệu "Xem lịch chiếu" chỉ vì chưa biết có suất. Trường hợp chưa có suất được xác định sau khi Showtime gọi API và hiển thị empty state. Dữ liệu phim hiện tại không được dùng để xác nhận suất còn bán.

#### 3.3.6. Trạng thái giao diện và lỗi

| Trạng thái | Hiển thị | Hành động |
|---|---|---|
| Loading | Skeleton cho poster, tiêu đề và metadata | Không cho kích hoạt hành động cần `movieId`/dữ liệu hoàn chỉnh |
| Success | Render các trường có dữ liệu và hành động xem lịch chiếu | Cho phép điều hướng Showtime |
| 404 `MOVIE_NOT_FOUND`/không còn công khai | Error/Not Found thân thiện, không lộ dữ liệu nội bộ | Cho về Home hoặc Movie List |
| Lỗi API/timeout | Error state trong main content | "Thử lại" cùng `movieId`; Header/Footer vẫn hoạt động |
| Trường tùy chọn thiếu | Placeholder poster hoặc ẩn/ghi chưa có thông tin theo bảng trên | Không làm toàn trang lỗi |
| Dữ liệu thay đổi trước khi xem lịch | Không dùng cache để xác nhận lịch | Showtime truy vấn lại dữ liệu hiện hành |

Không hiển thị stack trace, endpoint, ID nội bộ không cần thiết hoặc chi tiết kỹ thuật cho Customer.

#### 3.3.7. Khả dụng và accessibility

- Tiêu đề phim là heading chính; các khối metadata, mô tả và trailer có thứ bậc heading phù hợp.
- Poster có alt mô tả phù hợp hoặc alt rỗng nếu thông tin cạnh ảnh đã truyền đạt cùng nội dung.
- Nhãn trạng thái và phân loại tuổi không chỉ dựa vào màu.
- Các hành động Back, xem trailer và xem lịch chiếu dùng được bằng bàn phím, có focus state rõ ràng.
- Link trailer phải có accessible name chứa ngữ cảnh tên phim; nội dung media không tự phát có âm thanh.
- Error state và kết quả tải lại được thông báo phù hợp cho công nghệ hỗ trợ.

#### 3.3.8. Tiêu chí nghiệm thu

- AC-MOVIE-DETAIL-01: Route sử dụng đúng `movieId` và gọi `GET /api/movies/{movieId}` khi tải trực tiếp hoặc điều hướng từ danh sách.
- AC-MOVIE-DETAIL-02: Màn hình hiển thị đúng các trường hiện có trong `MovieDetail`, gồm poster, tên, mô tả, thời lượng, thể loại, phân loại tuổi và các thông tin liên quan khi có.
- AC-MOVIE-DETAIL-03: Trường nullable/ảnh lỗi không làm toàn trang lỗi và UI không tự tạo dữ liệu nghiệp vụ thay thế.
- AC-MOVIE-DETAIL-04: Chọn "Xem lịch chiếu" mở đúng Showtime và giữ nguyên `movieId`.
- AC-MOVIE-DETAIL-05: 404, lỗi API và retry có trạng thái riêng, không tiết lộ dữ liệu nội bộ.
- AC-MOVIE-DETAIL-06: Back/Forward/deep link hoạt động theo navigation và dữ liệu chi tiết được tải lại khi cần.
- AC-MOVIE-DETAIL-07: Màn hình không khẳng định có suất/ghế, không tạo booking và truy cập được khi chưa đăng nhập.
- AC-MOVIE-DETAIL-08: Quyền xem phim `ended` không được frontend tự chốt khi UC-06 vẫn để mở.

### 3.4. Showtime

#### 3.4.1. Mục đích và phạm vi

- Cho phép Customer xem lịch chiếu của một phim theo ngày và chọn một suất còn bán để tiếp tục sang Seat Selection.
- Hiển thị thông tin suất từ backend; không suy diễn khả năng còn ghế từ việc suất xuất hiện trong danh sách.
- Chỉ các suất `scheduled` còn bán theo BR-ST-003 mới được phép tiếp tục; trạng thái cuối cùng phải được kiểm tra lại khi Customer chọn suất.
- Không giữ ghế, tạo booking, khởi tạo payment hoặc yêu cầu đăng nhập ở màn hình này.

**Route và quyền truy cập:** `/movies/:movieId/showtimes?date={date}`, Public.

**Yêu cầu liên quan:** FR-SHOW-001; UC-07; `customer-user-flow.md` mục 3.3; `customer-navigation.md` mục 4, 5.1 và 9; `customer-screen-api-mapping.md` mục 3.3.

#### 3.4.2. Dữ liệu đầu vào và API

| Dữ liệu | Nguồn | Vai trò |
|---|---|---|
| `movieId` | Route | Xác định phim đang xem lịch chiếu |
| `date` | Query string/UI | Ngày lịch chiếu đang được tải |
| `showtimeId` | Showtime được chọn | Chuyển sang Seat Selection sau khi kiểm tra lại |

- Gọi `GET /api/movies/{movieId}/showtimes?date={date}` khi vào màn hình, refresh hoặc đổi ngày.
- Query ngày và thời gian hiển thị phải dùng cùng quy ước `Asia/Ho_Chi_Minh`; timestamp UTC từ API được chuyển đổi trước khi hiển thị.
- `movieId` và `date` được đồng bộ với URL để Back/Forward, refresh và deep link tái dựng đúng màn hình.
- Phạm vi ngày được phép chọn và ngày mặc định chưa được chốt trong UC-07; giao diện không hard-code một số ngày cố định như một business rule.

#### 3.4.3. Lựa chọn phim, ngày và rạp

| Tiêu chí | Hỗ trợ hiện tại | Hành vi giao diện |
|---|---|---|
| Phim | Có, qua `movieId` trên route | Showtime chỉ hiển thị lịch của phim đang chọn; đổi phim quay lại Movie Detail/Movie List |
| Ngày | Có, qua query `date` của API | Hiển thị bộ chọn ngày; đổi ngày cập nhật URL và tải lại danh sách |
| Rạp | **PROPOSED**, chưa phê duyệt | Sau khi API hỗ trợ, chọn rạp cập nhật `cinemaId`, tải lại danh sách và bỏ `showtimeId` cũ |
| Phòng | Có tên/ID phòng trong từng suất | Dùng để mô tả nơi chiếu, không phải bộ lọc hiện có |
| Định dạng chiếu | Chưa có trong DTO | Không tự thêm bộ lọc 2D/3D/IMAX hoặc giá trị tương tự |

Mô hình dữ liệu có quan hệ Cinema → CinemaRoom, nhưng DTO Showtime hiện chỉ trả `cinemaRoomId` và `cinemaRoomName`. Không được suy ra rạp từ tên phòng. Yêu cầu xem/lọc theo rạp của Issue 75 vì vậy cần được hoàn thiện ở API contract trước khi giao diện triển khai đầy đủ.

##### 3.4.3.1. Phương án lọc rạp — PROPOSED

Phương án dưới đây là thiết kế ban đầu để nhóm/API owner xem xét, **không phải quyết định nghiệp vụ đã chốt**:

- Danh sách rạp hoạt động: `GET /api/cinemas?status=active`.
- Lịch chiếu theo rạp: `GET /api/movies/{movieId}/showtimes?date={date}&cinemaId={cinemaId}`.
- Showtime DTO bổ sung `cinemaId` và `cinemaName`; vẫn giữ `cinemaRoomId` và `cinemaRoomName` để phân biệt rạp với phòng.
- Khi Customer đổi rạp, frontend cập nhật `cinemaId` trên URL, bỏ lựa chọn suất cũ và gọi lại API với `movieId`, `date`, `cinemaId`.
- Nếu MVP được xác nhận chỉ có một rạp, màn hình hiển thị thông tin rạp cố định và không render dropdown; không gửi một bộ lọc không có ý nghĩa.

Ví dụ DTO dự kiến:

```json
{
  "id": "uuid",
  "cinemaId": "uuid",
  "cinemaName": "Cinema 1",
  "cinemaRoomId": "uuid",
  "cinemaRoomName": "Room 2",
  "startsAt": "2026-10-29T13:00:00Z",
  "basePrice": 90000,
  "currency": "VND",
  "status": "scheduled",
  "bookingAvailable": true
}
```

**OPEN — cần nhóm/GV hoặc API owner xác nhận:**

- MVP hỗ trợ một rạp hay nhiều rạp.
- Có bắt buộc chọn rạp trước khi tải lịch hay có rạp mặc định.
- Có hiển thị địa chỉ rạp trong bộ chọn hay chỉ tên rạp.
- Tên endpoint, query và DTO chính thức; sau khi phê duyệt phải cập nhật `api-endpoints.md`, `dto-specification.md` và `customer-screen-api-mapping.md`.

#### 3.4.4. Thông tin suất chiếu

| Trường response | Hiển thị/sử dụng | Quy tắc |
|---|---|---|
| `id` | Không hiển thị trực tiếp | Dùng làm `showtimeId` cho thao tác chọn suất |
| `cinemaRoomId` | Không cần hiển thị ID | Giữ trong context khi cần đối chiếu phòng |
| `cinemaRoomName` | Tên phòng chiếu | Thiếu thông tin bắt buộc thì không suy đoán phòng/rạp |
| `startsAt` | Ngày và giờ bắt đầu | Chuyển UTC sang `Asia/Ho_Chi_Minh`, không tự thay đổi thời điểm |
| `basePrice` | Giá cơ sở nếu quyết định UI cho phép hiển thị | Không trình bày là tổng tiền hoặc giá cuối cùng của ghế/vé |
| `currency` | Đơn vị của `basePrice` | Định dạng cùng giá; phạm vi hiện tại dùng VND theo dữ liệu backend |
| `status` | Trạng thái suất | Chỉ `scheduled` còn bán mới cho phép tiếp tục; không mở rộng sang `in_progress` |
| `bookingAvailable` | Trạng thái khả dụng do backend tính | `false` thì vô hiệu thao tác và yêu cầu chọn suất khác |

UC-07 vẫn để mở quyết định có hiển thị giá ở bước Showtime hay chỉ sau khi chọn ghế. Nếu hiển thị, phải ghi rõ đây là **giá cơ sở** từ API; không tự tính phụ thu, giảm giá hoặc tổng booking.

#### 3.4.5. Cấu trúc màn hình

1. **Header toàn cục** theo `customer-navigation.md`.
2. **Ngữ cảnh phim** — tên/poster tóm tắt khi dữ liệu đã có; không dùng dữ liệu cache để xác nhận khả năng đặt.
3. **Bộ chọn ngày** — tabs trên màn hình rộng hoặc control phù hợp trên mobile; ngày đang chọn có trạng thái rõ ràng.
4. **Bộ chọn rạp** — chỉ xuất hiện khi API contract cung cấp danh sách rạp, `cinemaId` và khả năng truy vấn tương ứng.
5. **Danh sách suất chiếu** — mỗi mục hiển thị giờ bắt đầu, phòng, trạng thái khả dụng và giá cơ sở nếu quyết định hiển thị giá được chốt.
6. **Empty/error state** gắn với tổ hợp `movieId`, `date` và rạp khi bộ lọc rạp được hỗ trợ.
7. **Footer toàn cục**.

Thứ tự suất chiếu tuân theo thứ tự backend trả về cho đến khi API contract quy định rõ sắp xếp; frontend không tự gắn nhãn "sớm nhất", "phổ biến" hoặc ưu tiên rạp/phòng.

#### 3.4.6. Chọn suất và điều hướng

| Thao tác | Điều kiện | Kết quả |
|---|---|---|
| Đổi ngày | Ngày nằm trong phạm vi UI/API cho phép | Cập nhật `date`, tải lại danh sách và không giữ `showtimeId` cũ |
| Chọn suất | `status=scheduled` và `bookingAvailable=true` | Kiểm tra lại dữ liệu hiện hành rồi mở `/showtimes/:showtimeId/seats` |
| Chọn suất không còn bán | Backend trả trạng thái mới/`bookingAvailable=false` | Giữ Customer tại Showtime, thông báo và tải lại danh sách |
| Chọn suất không tồn tại | Kiểm tra lại trả 404 | Thông báo suất không còn khả dụng và yêu cầu chọn suất khác |
| Back về Movie Detail | Có `movieId` | Quay lại đúng phim; giữ ngày trong UI session khi phù hợp |

Việc kiểm tra lại có thể sử dụng endpoint chi tiết `GET /api/showtimes/{showtimeId}` đã được liệt kê trong API endpoints. Chỉ điều hướng tiếp khi backend xác nhận suất vẫn đủ điều kiện. Nếu API contract chọn cơ chế kiểm tra khác, phải cập nhật mapping trước khi triển khai; không dựa riêng vào card đã tải trước đó.

Chuyển sang Seat Selection chỉ mang `showtimeId` và navigation context (`movieId`, ngày). Thao tác này chưa giữ ghế; UC-01 mới thực hiện kiểm tra khả dụng và giữ ghế nguyên tử.

#### 3.4.7. Trạng thái giao diện và lỗi

| Trạng thái | Hiển thị | Hành động |
|---|---|---|
| Loading ban đầu/đổi ngày | Skeleton cho bộ chọn và danh sách suất | Chặn click lặp trong request hiện tại |
| Success có dữ liệu | Danh sách suất theo response | Chỉ suất đủ điều kiện có hành động tiếp tục |
| Empty | "Chưa có suất chiếu phù hợp" cho ngày/bộ lọc hiện tại | Cho đổi ngày hoặc rạp khi rạp đã được hỗ trợ |
| Lỗi API/timeout | Error state trong main content | "Thử lại" đúng `movieId` và `date` hiện tại |
| Movie không tồn tại/không công khai | Error/Not Found | Cho về Movie Detail, Movie List hoặc Home phù hợp |
| Suất đóng bán/hủy sau khi tải | Mục suất bị vô hiệu hoặc loại khỏi response sau refresh | Thông báo trạng thái mới, không tiếp tục bằng dữ liệu cũ |
| Thiếu dữ liệu phòng/rạp bắt buộc | Không hiển thị thông tin suy đoán | Báo lỗi phù hợp hoặc không cho chọn suất theo kết quả backend |

Không hiển thị stack trace, endpoint hoặc chi tiết kỹ thuật cho Customer. Empty state không bị coi là lỗi hệ thống.

#### 3.4.8. Back, Forward, refresh và dữ liệu cũ

- Back/Forward khôi phục `movieId` và `date` từ URL rồi tải lại lịch chiếu.
- Refresh/deep link không phụ thuộc dữ liệu từ Movie Detail; màn hình tải lại bằng route/query hiện tại.
- Khi quay lại từ Seat Selection, Showtime phải tải lại trạng thái suất; không giả định suất vẫn còn bán.
- Không tự polling theo thời gian thực khi UC-07 chưa chốt yêu cầu cập nhật; bắt buộc kiểm tra lại khi Customer chọn suất.
- Nếu suất bị hủy sau khi chọn nhưng trước khi giữ ghế, không chuyển tiếp bằng dữ liệu cũ; cách thông báo/điều hướng chi tiết vẫn tuân theo quyết định nghiệp vụ sẽ được chốt.

#### 3.4.9. Khả dụng và accessibility

- Bộ chọn ngày/rạp (khi có) có label và trạng thái được chọn bằng ngữ nghĩa, không chỉ bằng màu.
- Mỗi suất có accessible name gồm giờ chiếu và phòng; trạng thái disabled được truyền đạt cho công nghệ hỗ trợ.
- Giờ chiếu hiển thị nhất quán và có đủ ngữ cảnh ngày khi cần.
- Loading, empty, lỗi và thông báo suất vừa thay đổi được công bố phù hợp mà không tự giành focus liên tục.
- Các control dùng được bằng bàn phím và có focus state rõ ràng.

#### 3.4.10. Tiêu chí nghiệm thu

- AC-SHOWTIME-01: Showtime tải lịch đúng `movieId` và `date` bằng API đã đặc tả.
- AC-SHOWTIME-02: Đổi ngày cập nhật URL, gọi lại API và không sử dụng `showtimeId` của ngày cũ.
- AC-SHOWTIME-03: Mỗi suất hiển thị đúng giờ, phòng, trạng thái khả dụng và dữ liệu giá chỉ khi quyết định hiển thị đã được chốt.
- AC-SHOWTIME-04: Chỉ suất `scheduled` còn bán với `bookingAvailable=true` mới cho phép tiếp tục.
- AC-SHOWTIME-05: Khi chọn suất, hệ thống kiểm tra lại trạng thái; suất hủy/đóng bán/không tồn tại không được chuyển tiếp bằng cache cũ.
- AC-SHOWTIME-06: Suất hợp lệ điều hướng tới `/showtimes/:showtimeId/seats` với đúng `showtimeId`.
- AC-SHOWTIME-07: Loading, empty, error và retry được phân biệt; danh sách rỗng không bị coi là lỗi.
- AC-SHOWTIME-08: Màn hình không khẳng định còn ghế, không giữ ghế, không tạo booking/payment và truy cập được khi chưa đăng nhập.
- AC-SHOWTIME-09: Không hiển thị bộ lọc rạp/định dạng hoạt động giả khi API chưa hỗ trợ; khoảng trống API được ghi nhận rõ.
- AC-SHOWTIME-10: Back/Forward/refresh/deep link tái dựng đúng phim/ngày và tải lại dữ liệu động.

### 3.5. Seat Selection

- **Mục đích:** cho khách chọn ghế và loại vé, khởi tạo booking giữ ghế 5 phút.
- **Bố cục:** sơ đồ ghế theo hàng (row_label + seat_number), chú thích loại ghế (Standard/VIP/Couple), tổng tiền tạm tính, đồng hồ đếm ngược giữ ghế.
- **Dữ liệu hiển thị:** trạng thái ghế (trống / đã bán / đang chọn), loại ghế, phụ thu, giá từng ghế.
- **Nút thao tác:** chọn/bỏ chọn ghế, chọn loại vé cho từng ghế, "Đặt vé".
- **Validation:** ghế Couple chọn theo đơn vị ghế đôi; ghế đã bị người khác giữ/bán → thông báo và cập nhật sơ đồ.
- **Tiêu chí nghiệm thu:** giữ ghế thành công tạo booking; hết 5 phút → thông báo hết hạn, ghế được giải phóng.

### 3.6. Payment

- **Mục đích:** thực hiện thanh toán booking trong thời gian giữ ghế.
- **Bố cục:** tóm tắt booking (mã, suất, ghế, tổng tiền), chọn phương thức thanh toán, nút thanh toán.
- **Dữ liệu hiển thị:** `booking.total_amount` (VND), thời gian còn lại, chi tiết từng detail (giá đã snapshot).
- **Nút thao tác:** "Thanh toán", hủy booking.
- **Validation:** tổng tiền không được sửa; hết thời gian giữ ghế → chặn thanh toán, hiển thị expired.
- **Tiêu chí nghiệm thu:** thanh toán thành công chuyển sang Booking Confirmation; thất bại giữ booking `pending` cho phép retry theo chính sách.

### 3.7. Booking Confirmation

- **Mục đích:** xác nhận đặt vé thành công, hiển thị vé.
- **Bố cục:** mã booking, thông tin suất/ghế, danh sách vé với mã QR riêng cho từng vé (ghế Couple phát hành 2 vé riêng).
- **Dữ liệu hiển thị:** `booking_code`, giờ chiếu, phòng, ghế, mã vé/QR, trạng thái vé `valid`.
- **Nút thao tác:** về trang chủ, xem "Booking của tôi".
- **Tiêu chí nghiệm thu:** số vé hiển thị đúng số ghế/capacity_units; trạng thái `confirmed` chỉ xuất hiện sau khi payment được áp dụng.

### 3.8. Đăng nhập / Đăng ký

- **Mục đích:** xác thực người dùng trước khi đặt vé.
- **Dữ liệu hiển thị:** form email, mật khẩu; đăng ký chỉ cấp role `CUSTOMER`.
- **Validation:** định dạng email, mật khẩu bắt buộc; lỗi xác thực hiển thị thông báo chung.
- **Tiêu chí nghiệm thu:** đăng nhập thành công quay lại màn hình trước khi bị chuyển hướng.

### 3.9. Tài khoản (Booking của tôi)

- **Mục đích:** xem lịch sử booking và vé của user.
- **Dữ liệu hiển thị:** danh sách booking với trạng thái (`pending`, `confirmed`, `expired`, `cancelled`), vé theo booking.
- **Tiêu chí nghiệm thu:** chỉ hiển thị booking của user hiện tại (authorization ở backend).
