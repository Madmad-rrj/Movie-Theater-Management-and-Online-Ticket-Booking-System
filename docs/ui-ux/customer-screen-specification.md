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

- **Mục đích:** hiển thị lịch chiếu của phim để khách chọn suất.
- **Bố cục:** nhóm theo ngày → danh sách suất (giờ bắt đầu, phòng, giá cơ sở).
- **Dữ liệu hiển thị:** `starts_at`, tên phòng, giá cơ sở, trạng thái còn/đóng bán.
- **Nút thao tác:** chọn suất → Seat Selection.
- **Validation:** suất đã đóng bán hoặc đã hủy không thể chọn (disabled).
- **Tiêu chí nghiệm thu:** suất bị vô hiệu có trạng thái disabled; chọn suất chuyển đúng phòng/suất.

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
