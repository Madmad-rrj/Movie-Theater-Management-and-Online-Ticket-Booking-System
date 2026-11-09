# Movie Research and Demo Provenance — Promethe-Phos

Rà soát ngày 09/11/2026. Tài liệu phân biệt metadata phim, phiên bản/territory, nguồn còn cần kiểm tra và fixture giả lập. **Lịch seed 14–21/11/2026 và status now_showing không phải lịch chiếu thật được xác minh.** Đây là một bộ catalogue demo/tái chiếu giả định.

## 1. Bộ phim và runtime được dùng

| Seed key | Phim | Runtime seed (phút) | Căn cứ và mức xác minh |
|---|---|---:|---|
| DEMO_ENDGAME | Avengers: Endgame | 181 | D23/Disney có kết quả chỉ mục 181 phút; trang đọc trực tiếp bị giới hạn nội dung. Đã có corroboration, không coi full metadata được kiểm hết. [D23](https://d23.com/a-to-z/avengers-endgame-film/) |
| DEMO_INTERSTELLAR | Interstellar | 169 | [CGV Việt Nam](https://www.cgv.vn/default/interstellar.html) ghi 169 cho bản chiếu lại; [Paramount](https://www.paramount.com/movies/interstellar) ghi 168. Chọn 169 làm fixture theo bản CGV, ghi nhận khác biệt edition/runtime, không gộp thành một con số tuyệt đối. |
| DEMO_MI_FINAL | Mission: Impossible - The Final Reckoning | 169 | [Paramount](https://www.paramount.com/movies/mission-impossible-the-final-reckoning) ghi 169 phút, năm 2025. |
| DEMO_YOUR_NAME | Your Name | 106 | [CGV](https://www.cgv.vn/en/your-name.html) có kết quả chỉ mục 106 phút nhưng mở trực tiếp timeout; [MoMo Cinema](https://www.momo.vn/cinema/your-name-24939) corroborates 106. Không coi đây là xác minh toàn bộ hồ sơ bản Nhật 2016. |
| DEMO_KINGDOM | Kingdom of Heaven | 144 | [Catalogue thư viện NHRC](https://library.nhrc.or.th/en/media/8773) mô tả DVD 2005 dài 144 phút. [Studio Nhật](https://www.20thcenturystudios.jp/movies/kingdom-of-heaven) có nhiều edition, gồm Director’s Cut. Fixture chọn 144, không dùng runtime Director’s Cut cho cùng showtime. |
| DEMO_WWZ | World War Z | 116 | [Paramount](https://www.paramount.com/movies/world-war-z) ghi 116 phút, năm 2013; không trộn với unrated cut. |

Các mô tả ngắn trong seed là diễn đạt lại phục vụ demo. Genre associations là taxonomy của bộ dữ liệu (9 genres/18 links), không tuyên bố khớp mọi distributor. Ngôn ngữ English/Japanese kế thừa metadata bản trước làm fixture, không xác định audio/subtitle của một suất thật.

## 2. Release dates và classification

Bản trước thiếu market/version cho release date và dùng PG/PG-13/R như age_rating. Các nhãn đó **không được tự chuyển thành quy tắc admission Việt Nam**. Seed mới để `release_date`, `release_market`, `age_rating`, `age_rating_system` NULL cho tới khi duyệt đúng nguồn/territory/version. UI phải hiển thị trường thiếu phù hợp; admission test cần classification fixture được phê duyệt riêng, không suy NULL là mọi tuổi được vào.

| Phim | Ngày trong bản cũ — chưa chốt territory trong fixture mới | Nhãn cũ — không dùng làm admission Việt Nam |
|---|---|---|
| Avengers: Endgame | 2019-04-26 | PG-13 |
| Interstellar (2014) | 2014-11-07 | PG-13 |
| Mission: Impossible - The Final Reckoning | 2025-05-23 | PG-13 |
| Your Name (2016) | 2016-08-26 | PG |
| Kingdom of Heaven | 2005-05-06 | R |
| World War Z | 2013-06-21 | PG-13 |

Ví dụ khác biệt đã thấy: CGV ghi ngày chiếu lại Interstellar 28/02/2025, còn bản cũ dùng ngày 2014-11-07; studio Nhật ghi Kingdom of Heaven 14/05/2005, khác ngày 2005-05-06 trong bản cũ. Không thay một ngày bằng ngày khác mà bỏ market/edition. Những ngày này cũng không xác định status now_showing của demo.

## 3. Poster và trailer kế thừa cần kiểm tra

Các URL sau được giữ làm **candidates từ file người dùng**, chưa kiểm accessibility, đúng phim/edition, quyền sử dụng/hotlink hoặc tính ổn định. Seed không đưa các URL này vào DB như tài sản đã duyệt; `poster_url/trailer_url` NULL. Có thể dùng UI placeholder và bổ sung assets được phép sau.

| Phim | Poster candidate | Trailer candidate |
|---|---|---|
| Avengers: Endgame | `https://image.tmdb.org/t/p/original/or06FN3Dka5tukK1e9sl16pB3iy.jpg` | `https://www.youtube.com/watch?v=TcMBFSGVi1c` |
| Interstellar (2014) | `https://media.themoviedb.org/t/p/w600_and_h900_face/yQvGrMoipbRoddT0ZR8tPoR7NfX.jpg` | `https://www.youtube.com/watch?v=zSWdZVtXT7E` |
| Mission: Impossible - The Final Reckoning | `https://media.themoviedb.org/t/p/w600_and_h900_face/iKPsC9EFUafRP9SrUznI61getVP.jpg` | `https://www.youtube.com/watch?v=NOhDyUmT9z0` |
| Your Name (2016) | `https://media.themoviedb.org/t/p/w600_and_h900_face/vfJFJPepRKapMd5G2ro7klIRysq.jpg` | `https://www.youtube.com/watch?v=xU47nhruN-Q` |
| Kingdom of Heaven | `https://media.themoviedb.org/t/p/w600_and_h900_face/2NaFdWy9hkM35vZB1hkqsnTcms8.jpg` | `https://www.youtube.com/watch?v=13n48NlVOnM` |
| World War Z | `https://media.themoviedb.org/t/p/w600_and_h900_face/aCnVdvExw6UWSeQfr0tUH3jr4qG.jpg` | `https://www.youtube.com/watch?v=HcwTxRuq-uk` |

## 4. Lịch chiếu và dữ liệu giả lập

- Ngày mặc định 14–21/11/2026, timezone Asia/Ho_Chi_Minh; override rõ bằng `cinema.demo_start_date`, không tự chạy theo ngày hiện tại.
- Room 1: 10:00 Your Name; 13:00 Avengers: Endgame; 17:00 Mission: Impossible.
- Room 2: 11:00 World War Z; 14:30 Kingdom of Heaven; 18:00 Interstellar.
- Mỗi suất snapshot runtime fixture; turnaround 15 phút. Không hard-code cutoff −10 phút.
- Một cinema demo, hai phòng, mỗi phòng 50 physical seats; hàng A–C Standard, D–E VIP. Không Couple.
- Cinema base Adult 100,000 VND, Discounted 70,000 VND; surcharge Standard 0, VIP 15,000 VND. Đây là giá test tự chọn, không nghiên cứu giá rạp thật.
- Tổng 48 showtimes; exclusion phải reject overlapping schedules. Mọi status/giá/location này chỉ phục vụ kiểm thử.

## 5. Dữ liệu cần duyệt trước demo/triển khai

| Hạng mục | Việc cần làm |
|---|---|
| Edition | Chọn chính xác bản chiếu; giữ duration snapshot của showtime đã tạo |
| Release date | Gắn territory, original/re-release và URL nguồn cụ thể |
| Classification | Chốt nhãn/điều kiện admission theo bản được phép, không suy từ PG-13/R |
| Media | Kiểm đúng phim, accessibility và quyền sử dụng; có fallback |
| Status | Admin quản catalogue theo tình huống demo, không suy từ release date |
| Seed changes | Đổi fixture version/có review khi metadata đã bị sửa; không overwrite giá/lịch đã có booking |

## 6. Revision note

Separated researched movie metadata from synthetic screening, pricing and catalogue data. Recorded runtime discrepancies and edition/market limitations, retained unverified media URLs as candidates, and aligned the six-movie fixture with the new single-seat, two-category schema. No actual November 2026 screening availability or local age classification is asserted.
