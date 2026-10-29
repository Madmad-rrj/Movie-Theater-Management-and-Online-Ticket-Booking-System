# Customer Responsive Specification

## 1. Phạm vi — CIN-SUB-067

Đặc tả cách bố cục Customer thay đổi theo chiều rộng, chiều cao, cách nhập và nội dung. Áp dụng cho KH-01..12 trong [đặc tả màn hình](customer-screen-specification.md), dùng chung [web layout](customer-web-layout.md), [component](customer-component-specification.md) và [UI State](customer-ui-state.md).

Responsive thay đổi cách trình bày; quyền, số ghế, giá, thời hạn, trạng thái booking/payment và chức năng được phép giữ nguyên. Đây là tiêu chí thiết kế/kiểm thử khi có giao diện, chưa phải kết quả chạy ứng dụng trên thiết bị. Các lựa chọn kích thước bổ sung được ghi là đề xuất UI, không phải quyết định nghiệp vụ.

## 2. Breakpoint và khung trang

| Nhóm | Chiều rộng viewport CSS | Khung chính | Điều hướng |
|---|---|---|---|
| Desktop | Từ 1024px | Container căn giữa; grid phim 3 cột, chọn ghế hai vùng cạnh nhau | Logo, nhóm phim, tài khoản cùng hàng khi đủ chỗ. |
| Tablet | 768px đến 1023px | Grid phim 2 cột; sơ đồ ghế toàn chiều rộng, tóm tắt xuống dưới | Giữ menu chính nếu vừa; thu gọn vào menu khi tràn, không giảm chữ đến mức khó đọc. |
| Mobile | Dưới 768px | Grid phim 1 cột; nội dung chính xếp dọc, padding theo container | Logo và nút menu; drawer chứa đủ nhóm phim và vùng tài khoản. |

Dùng cùng các mốc của web layout; xét chiều rộng vùng nhìn, không suy từ tên thiết bị hay chuỗi user-agent. Không tự thêm một breakpoint riêng cho mỗi màn hình. Container/padding/max-width theo đề xuất ở web layout cần review trước triển khai; không kéo giãn văn bản ra toàn màn hình lớn hoặc giữ một chiều rộng cố định làm tràn mobile.

Header/main/footer căn theo cùng trục. Main tăng theo nội dung; footer đứng sau nội dung, ở đáy khi trang ngắn nhưng không phủ lên trang dài. Header sticky phải chừa khoảng để tiêu đề, thông báo và điểm focus đọc được. Không ép toàn bộ trang vào một chiều cao cố định rồi cắt phần còn lại.

## 3. Hành vi cho đủ các màn hình

| Mã / màn hình | Desktop | Tablet | Mobile / nội dung cần giữ |
|---|---|---|---|
| KH-01 Home | Grid 3 cột; nhóm phim và phân trang gần danh sách | Grid 2 cột; tên/nút có thể xuống dòng | Grid 1 cột; poster theo container, tên phim/tuổi/trạng thái rõ. Không thêm card giả để đủ cột. |
| KH-02 Movie Detail | Poster trái, thông tin phải; mô tả theo chiều dài thật | Một hoặc hai cột tùy không gian thực là đề xuất UI | Poster → tiêu đề/thông tin → nội dung → chọn suất theo thứ tự đọc; không cắt mô tả hoặc mất nút. Trailer chỉ khi có dữ liệu. |
| KH-03 Showtime | Nhóm ngày và suất rõ; giờ/phòng/giá cùng ngữ cảnh | Suất xuống hàng khi không đủ chỗ | Bộ ngày cuộn ngang trong khối hoặc Select theo thiết kế; danh sách suất xếp dọc, không mất trạng thái không còn bán. |
| KH-04 Seat Selection | Sơ đồ trái rộng, tóm tắt phải; chú thích đi cùng sơ đồ | Sơ đồ toàn chiều rộng, tóm tắt bên dưới | Cuộn ngang riêng sơ đồ; tóm tắt ngắn và nút tiếp tục có thể bám đáy theo mục 4, chi tiết vẫn trong nội dung chính. |
| KH-09 Ticket Type | Một hàng cho một đơn vị ghế, nhãn và Select gần nhau | Hàng dài có thể xếp dọc | Nhãn ghế/loại/số chỗ → lựa chọn loại vé → giá/điều kiện nếu có. Couple một đơn vị, không tách thành hai ô do màn hình nhỏ. |
| KH-06 Booking Confirmation | Một cột rà soát, nút xác nhận sau thông tin | Giữ một cột, không tạo sidebar trống | Thông tin draft xuống dòng; giá dự kiến có nhãn; Back/xác nhận phân biệt rõ. Chưa tạo không có countdown/QR. |
| KH-05 Payment | Một cột gọn căn giữa, tóm tắt trước thao tác thanh toán | Giữ thứ tự nội dung | Một cột: mã/thông tin đơn → giá chốt → hạn/trạng thái → phương thức → hành động. Không tự thêm bottom bar thanh toán che thông báo. |
| KH-10 Payment Result | Thông báo, mã/thông tin giao dịch và hành động theo trạng thái | Giữ một cột | Lời giải thích dài xuống dòng, không rút unknown thành icon; nút kiểm tra lại/xem vé/lịch sử chỉ theo điều kiện thực. |
| KH-07 Login/Register | Form hẹp căn giữa, input một cột | Form vừa vùng nhìn | Một cột có nhãn/lỗi; khi bàn phím mở vẫn cuộn tới field/nút. Không ẩn điện thoại/validation chỉ để giảm chiều dài. |
| KH-08 Booking History | Table hoặc danh sách thẻ là đề xuất; chi tiết gắn đúng đơn | Cho nội dung xuống dòng/chuyển thẻ nếu bảng không đủ chỗ | Ưu tiên thẻ có mã, ngày, trạng thái, tiền và thao tác. Nếu giữ bảng, chỉ cuộn ngang trong khối; không giấu cột quan trọng. |
| KH-11 My Tickets | Nhóm theo đơn/suất, mỗi vé một thẻ và QR riêng | Giữ mã/QR đủ diện tích | Thẻ xếp dọc, Couple hiện đủ hai vé; không carousel giấu mã còn lại. Trạng thái valid/used/voided có chữ. |
| KH-12 Profile | Form/vùng đọc hẹp, tách lịch sử; chỉ field được duyệt cho sửa | Một cột, lỗi cạnh field | Input và hành động trong vùng cuộn chính; không che “Lưu”/lỗi bởi bàn phím. Thiếu contract không biến thành form lưu giả. |

Nếu dùng sidebar tài khoản trên desktop, mobile chuyển thành tabs/menu trong vùng tài khoản, giữ mục đang xem. Tabs dài chỉ cuộn trong khối; không tạo sidebar rỗng trên trang không dùng. Các mục My Tickets/Profile mới cần phối hợp Navigation, responsive không tự đặt URL hoặc mở rộng quyền.

## 4. Sơ đồ ghế và thanh hành động mobile

### 4.1. Giữ hình học và thao tác

- Giữ thứ tự hàng, số ghế và tương quan vị trí theo dữ liệu đã được chốt. Không tự bẻ một hàng ghế thành hai hàng khi thiếu chiều rộng vì sẽ sai sơ đồ phòng.
- Vùng bấm ghế tối thiểu 44px theo đặc tả layout/component; không co toàn sơ đồ đến mức không chạm được. Couple có bề rộng phù hợp hai chỗ nhưng vẫn một vùng chọn/seatId.
- Khi sơ đồ rộng hơn container, cuộn ngang **bên trong khối sơ đồ**; body, tóm tắt, nút, thông báo không bị kéo ngang cùng sơ đồ. Có dấu hiệu đọc được rằng còn nội dung bên cạnh.
- Hàng/nhãn ghế và hướng màn hình phải còn nhận diện khi cuộn. Cách ghim nhãn hàng là đề xuất cần thử với dữ liệu hình học; không tự tạo lối đi/hướng màn hình từ seed hoặc màu nền.
- Ghế available/selected/held/sold và bảo trì/inactive nếu API hỗ trợ có chú giải, nhãn/biểu tượng ngoài màu sắc. Không làm held thành chọn được khi đổi breakpoint.
- Bàn phím phải đi tới được ghế khả dụng và đưa ghế focus vào vùng nhìn. Đề xuất lựa chọn cách điều hướng bàn phím cho Seat Map cần chốt khi triển khai; không giả đã có thư viện hỗ trợ.
- Giữ selected và loại vé khi resize/xoay. Refresh/xung đột có thể làm mất tính hợp lệ của ghế theo backend; thông báo và cập nhật lựa chọn theo nghiệp vụ, không quy lỗi cho responsive.

### 4.2. Thanh tóm tắt và tiếp tục

Trên mobile, dùng một thanh bám đáy theo web layout cho tóm tắt ngắn và hành động chính của bước chọn ghế. Trước tạo đơn, nút này chuyển Ticket Type/rà soát theo luồng, chưa phải thanh toán hoặc giữ ghế. Tổng chỉ xuất hiện nếu có nguồn hợp lệ và phải có nhãn dự kiến khi chưa chốt; nếu chưa có báo giá, ưu tiên số ghế/số chỗ và liên kết xem tóm tắt.

Phải chừa khoảng cuối nội dung ít nhất bằng chiều cao thực của thanh cùng vùng an toàn thiết bị nếu có. Thanh không che hàng ghế cuối, chú thích, lỗi, footer hoặc nút khác. Nội dung dài được xuống dòng và thanh có thể tăng chiều cao; không cắt tổng hoặc nhãn nút để giữ một chiều cao giả định.

Khi bàn phím nhập liệu/lớp phủ làm vùng nhìn thấp, điều chỉnh hoặc đưa thanh về luồng nội dung để vẫn xem được field và hành động. Đây là đề xuất hành vi UI cần kiểm tra thiết bị thực. Không sinh hai nút submit với state/request độc lập giữa bản desktop và mobile; mọi cách trình bày dùng cùng điều kiện enabled/loading.

## 5. Thanh toán, kết quả và QR trên vùng hẹp

### 5.1. Thông tin tiền và thông báo

- Giữ mã đơn, tổng tiền, VND, trạng thái và hạn server gần phần thao tác. Tên phim/ghế dài xuống dòng; tổng tiền không nằm trong khung phải cuộn ngang mới đọc được.
- Countdown chỉ hiển thị sau tạo booking; resize, mở drawer hoặc xoay không reset đồng hồ. 0:00 không tự expired/released; created/pending/unknown vẫn theo trạng thái server.
- Không thu gọn thông báo đang xác nhận/đối soát thành toast tự tắt hoặc chỉ icon. Nội dung đủ chiều cao cho lời giải thích và hành động được phép; màn hình nhỏ vẫn chặn attempt mới khi lần cũ đang mở.
- Nút thanh toán, kiểm tra lại và hủy được phân biệt theo chức năng; sắp dọc nếu không đủ chiều rộng. Giữ lời giải thích khi disabled; không ẩn hủy để thay đổi điều kiện nghiệp vụ.
- PayUrl/cổng ngoài có cách hiển thị riêng, chưa chốt provider. Không cam kết cổng hỗ trợ iframe/app switch hoặc responsive theo layout dự án. Quay về phải đọc backend, không tin kết quả trên URL.

### 5.2. Vé và mã QR

QR giữ tỉ lệ, không bị crop, kéo giãn, làm mờ để vừa card hoặc phủ bằng nhãn trạng thái. Không cố định một số pixel cho mọi QR khi chưa có quy cách sinh/quét; cần nghiệm thu khả năng quét với mã thật sau tích hợp. Mã chữ luôn có thể đọc và xuống dòng an toàn nếu dài; ảnh lỗi vẫn giữ mã, ghế, trạng thái và thao tác đọc lại phù hợp.

Couple có hai mã/thẻ độc lập; mobile dùng chiều dài trang để hiển thị đủ, không ghép QR hoặc chỉ hiện một mã đại diện. Xem vé không chuyển valid sang used; breakpoint không thay dữ liệu đã phát hành. Không bổ sung in/PDF/tải về hoặc gửi email chỉ để xử lý màn hình nhỏ.

## 6. Form, menu và lớp phủ

| Thành phần | Hành vi theo không gian / cách nhập |
|---|---|
| Input và Select | Chiếm chiều rộng vùng form, có nhãn rõ; lỗi tăng chiều cao tự nhiên. Kiểu bàn phím phù hợp email/phone nếu field được chốt, không tự áp regex/format nghiệp vụ mới. |
| Form dài | Cuộn trang tới field lỗi/nút submit; không nhốt toàn form vào khung chiều cao cố định. Password không lộ trong URL hay dữ liệu khôi phục. |
| Drawer mobile | Đủ điều hướng và tài khoản, có nút đóng; nội dung dài cuộn trong drawer. Khi mở giữ focus trong lớp, khi đóng trả focus về nút mở theo đề xuất component. |
| Modal xác nhận | Vừa viewport cả ngang/dọc, có lề; nội dung dài cuộn trong lớp và vẫn tới được hành động. Chỉ một modal; X/Escape không đồng nghĩa hủy booking. |
| Dropdown | Không mở ra ngoài vùng nhìn; tên tài khoản dài không đẩy logo/menu. Có cách đọc tên đầy đủ trong menu nếu nhãn đầu bị ellipsis. |
| Tabs / chọn ngày | Cuộn cục bộ hoặc lựa chọn thay thế theo thiết kế; mục active có chữ/kiểu rõ. Đổi cách hiển thị không đổi ngày hay tự submit form. |
| Toast / Inline Feedback | Không che tổng tiền, QR, input hoặc nút chính; nội dung nghiệp vụ quan trọng có vùng phản hồi bền trong trang. |

Đổi từ drawer sang menu desktop khi resize phải đóng lớp phủ/giải phóng chặn cuộn nền phù hợp, giữ focus vào điểm còn tồn tại. Không để lớp backdrop vô hình chặn trang. Các quy tắc focus/bàn phím ở đây là đề xuất UI cần nghiệm thu khi triển khai, không khẳng định đã có component code.

## 7. Nội dung tràn, thứ tự đọc và bảo toàn state

1. Không cuộn ngang toàn trang tại các kích thước hỗ trợ. Ngoại lệ cục bộ có chủ đích là sơ đồ ghế, bộ ngày/tabs hoặc bảng khi cần; có dấu hiệu và thao tác bàn phím/chạm để tiếp cận phần còn lại.
2. Tên phim, loại vé, thông báo lỗi, mã và giá trị quan trọng được xuống dòng. Ellipsis chỉ cho nội dung phụ có cách đọc đầy đủ; không cắt mất ý của tổng tiền, trạng thái hoặc mã vé.
3. Poster giữ tỉ lệ theo container và có vùng thay ảnh lỗi; không dùng ảnh làm toàn bộ thông tin card. Loading/empty/error chiếm đúng vùng nội dung, không cần card giả để giữ số cột.
4. Thứ tự DOM/focus cần có nghĩa khi nhiều cột thành một cột: ngữ cảnh → lựa chọn/dữ liệu → tóm tắt → hành động. Không dùng cách sắp xếp trực quan khiến focus đi ngược thứ tự đọc.
5. Resize/xoay/phóng to giữ ngày, ghế, loại vé, draft form, bookingId/paymentId và trạng thái loading/disabled. Không gọi POST mới hoặc tạo requestKey mới chỉ vì thay layout.
6. Rời trang/login/logout vẫn theo quy tắc phiên và ownership; responsive không phải lý do giữ cache private sau đổi tài khoản. Back/refresh cần đọc lại trạng thái như đặc tả màn hình.
7. **Đề xuất kiểm tra:** phóng to chữ/trang đến 200%, viewport thấp, thiết bị xoay ngang và bàn phím mở; cần tiếp cận được thông tin/hành động mà không dựa vào hover hoặc màu sắc.

## 8. Tiêu chí nghiệm thu responsive

Bảng dưới là các kịch bản **cần chạy khi có UI**, chưa phải biên bản kiểm thử thiết bị. Kiểm tra tối thiểu 320px, 768px, 1024px, 1440px; bổ sung ranh giới 767/768px, 1023/1024px, chiều cao thấp, xoay và bàn phím theo đề xuất web layout. Không chỉ kiểm tra trang có dữ liệu đẹp.

| Mã | Kịch bản | Kết quả cần đạt |
|---|---|---|
| RP-01 | Khung trang ở bốn kích thước tối thiểu; main ngắn/dài | Header/main/footer cùng trục, có padding; không tràn ngang body hoặc footer đè nội dung. |
| RP-02 | Header/menu với tên tài khoản dài; đổi breakpoint khi drawer mở | Đủ mục được phép, đóng/mở/focus được; không còn backdrop chặn trang hoặc mất dữ liệu phiên. |
| RP-03 | Home ít/nhiều phim, poster lỗi; chi tiết tên/mô tả dài | Grid 3/2/1, thông tin/nút đọc được; không méo ảnh, card giả hoặc cắt nội dung chính. |
| RP-04 | Showtime nhiều ngày/suất, ngày rỗng và GET lỗi | Cuộn/chọn ngày rõ, giờ/phòng/giá đủ; empty khác lỗi; không mất trạng thái đóng bán. |
| RP-05 | Sơ đồ rộng, ghế ở hai mép, hàng cuối, Couple và ghế không khả dụng | Hình học không bẻ hàng, vùng bấm ≥44px, chỉ sơ đồ cuộn ngang; Couple nguyên đơn vị và chú giải rõ. |
| RP-06 | Thanh tiếp tục mobile, tóm tắt dài và vùng an toàn | Không che ghế/lỗi/footer; tiếp tục chưa tạo booking; không có hai submit độc lập. |
| RP-07 | Ticket Type/rà soát nhiều ghế, thiếu danh mục/báo giá/liên hệ chưa chốt | Hàng xếp dọc đọc được, lỗi đúng hàng; không có giá cuối/countdown/QR giả trước tạo. |
| RP-08 | Payment đang gửi, attempt mở, countdown 0, lời giải thích dài | Giá/trạng thái và hành động thấy được; responsive không bật retry hoặc tự hủy/nhả ghế. |
| RP-09 | Payment Result success/failed/unknown/GET lỗi | Có chữ giải thích, nút theo điều kiện; thông báo không bị cắt hoặc chỉ nằm trong toast. |
| RP-10 | History nhiều đơn/mã dài và My Tickets nhiều vé/Couple | Đủ dữ liệu quan trọng, từng mã/QR rõ; trạng thái vé độc lập, không mất vé thứ hai. |
| RP-11 | Login/Register/Profile với lỗi trường, bàn phím và draft chưa lưu | Input/lỗi/nút tiếp cận được, resize giữ draft; không tự lưu hoặc báo đã lưu khi chưa có contract. |
| RP-12 | Modal dài, dropdown sát mép, tabs dài, zoom và bàn phím | Không vượt vùng nhìn, thao tác đóng/focus tiếp cận được; X/Escape không gửi cancel. |
| RP-13 | Resize/xoay trong lúc chọn ghế, POST chưa rõ hoặc payment đang chờ | Không mất state, đổi key, resubmit hoặc đổi booking/payment status. Phải đọc server khi cần xác minh. |

Nghiệm thu triển khai còn phụ thuộc contract dữ liệu sơ đồ, báo giá, danh sách vé/hồ sơ và quy cách QR/cổng thanh toán. Thiếu dữ liệu/API không được che bằng responsive hoặc coi là dữ liệu rỗng.
