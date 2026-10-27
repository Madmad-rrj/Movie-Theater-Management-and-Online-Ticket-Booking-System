# Bố cục website khách hàng — Hệ thống đặt vé xem phim

## 1. Mục đích và cách đọc

Tài liệu mô tả cách sắp xếp các khối giao diện của website Customer: khung trang, header, footer, vùng nội dung, lưới, thứ tự đọc, bố cục từng màn hình và cách thích ứng với thiết bị. Trọng tâm là **nội dung nằm ở đâu và thay đổi bố cục như thế nào**.

Mã `KH-01` đến `KH-08` tương ứng các nhóm màn hình trong [đặc tả màn hình khách hàng](customer-screen-specification.md). Tài liệu này không định nghĩa lại API, quy tắc tính tiền hoặc điều kiện chuyển trạng thái booking/payment.

Các phần ghi **đề xuất bố cục** là hướng thiết kế cần review; các kích thước minh họa không phải token đã được phê duyệt. Chưa chốt màu thương hiệu, font, kích thước logo, tỉ lệ poster và hệ thống giá trị z-index cụ thể.

Sơ đồ Mermaid biểu diễn quan hệ giữa các vùng hoặc cách chọn bố cục; khung chữ minh họa vị trí tương đối. Chúng không phải bản vẽ theo tỉ lệ pixel và không quy định tên component trong mã nguồn.

## 2. Khung trang dùng chung

### 2.1. Cấu trúc tổng thể

Mỗi màn hình nằm trong một khung thống nhất gồm header, main và footer. Nội dung chính thay đổi theo màn hình; các thành phần điều hướng toàn cục giữ cách tổ chức nhất quán.

```text
┌────────────────────────────────────────────────────┐
│ Header: logo — menu chính — tài khoản               │
├────────────────────────────────────────────────────┤
│ Main                                               │
│ ┌────────────────────────────────────────────────┐ │
│ │ Container căn giữa                             │ │
│ │ Tiêu đề và thông tin ngữ cảnh                   │ │
│ │ Nội dung màn hình                              │ │
│ │ Hành động theo ngữ cảnh                         │ │
│ └────────────────────────────────────────────────┘ │
├────────────────────────────────────────────────────┤
│ Footer: thông tin rạp và liên kết tĩnh              │
└────────────────────────────────────────────────────┘
```

```mermaid
flowchart TD
    page["Khung website Customer"] --> header["Header dùng chung"]
    page --> main["Vùng nội dung chính"]
    page --> footer["Footer dùng chung"]
    header --> brand["Logo và điều hướng chính"]
    header --> account["Khu vực tài khoản"]
    main --> container["Container căn giữa"]
    container --> title["Tiêu đề và ngữ cảnh"]
    container --> screen["Nội dung màn hình"]
    container --> actions["Hành động theo ngữ cảnh"]
    footer --> cinema["Thông tin rạp"]
    footer --> links["Liên kết tĩnh"]
```

Các mũi tên trong sơ đồ trên thể hiện quan hệ chứa, không phải trình tự thao tác hay gọi API. Hành động theo ngữ cảnh có thể nằm trong khối nội dung tương ứng; không bắt buộc mọi màn hình có một thanh nút riêng ở cuối.

### 2.2. Header bám đầu trang và footer cuối trang

| Vùng | Cách bố trí | Giới hạn |
|---|---|---|
| Header | Nằm đầu trang, sticky khi cuộn | Có vùng nền đủ rõ; không che nội dung hoặc điểm focus bên dưới. Chiều cao cụ thể cần chốt khi thiết kế. |
| Main | Chiếm phần chiều cao còn lại khi nội dung ngắn; tăng theo nội dung khi dài | Không ép chiều cao cố định làm mất nội dung; không biến toàn bộ màn hình thành khung cuộn nhỏ trong main. |
| Footer | Sau main; ở đáy viewport nếu nội dung ngắn, ở cuối tài liệu nếu nội dung dài | Không cố định đè lên nội dung khi cuộn. “Sticky footer layout” không có nghĩa footer luôn phủ đáy màn hình. |
| Thanh hành động chọn ghế trên mobile | Bám đáy màn hình theo bố cục KH-04 | Phải có phần chừa chỗ tương ứng để không che ghế, ghi chú hoặc footer; xem mục 5.4. |
| Modal / menu dạng ngăn kéo | Lớp tạm thời phía trên khung trang | Không thay thế cấu trúc main/header/footer; quy tắc xếp lớp tại mục 7. |

**Đề xuất bố cục:** dùng khung dọc có chiều cao tối thiểu bằng vùng nhìn thấy, để main co giãn giữa header và footer. Khi chiều cao trình duyệt thay đổi, đặc biệt trên điện thoại, ưu tiên giữ nội dung và nút trong vùng có thể tiếp cận thay vì ép trang theo một chiều cao màn hình cố định.

### 2.3. Thứ tự đọc và phạm vi cuộn

- Thứ tự đọc mặc định: header → tiêu đề trang → thông tin ngữ cảnh → nội dung chính → hành động liên quan → footer.
- Chỉ có một vùng cuộn dọc chính của trang. Cho phép cuộn cục bộ với sơ đồ ghế, bộ chọn ngày hoặc lớp phủ khi nội dung thật sự vượt vùng chứa.
- Không để body và một khối main có chiều cao cố định cùng cuộn dọc cho cùng nội dung.
- Không phát sinh cuộn ngang ở mức trang. Nếu sơ đồ ghế rộng hơn màn hình, cuộn trong chính khối sơ đồ và giữ các vùng còn lại theo chiều rộng trang.
- Thứ tự focus bàn phím cần theo thứ tự đọc; đổi từ nhiều cột thành một cột không làm nút đặt nằm trước thông tin cần kiểm tra.
- **Đề xuất bố cục:** có một tiêu đề trang rõ ràng; các khối con dùng tiêu đề cấp dưới. Không dùng cỡ chữ lớn thay cho cấu trúc tiêu đề có nghĩa.

### 2.4. Nội dung ngắn, dài và dữ liệu thay đổi

Khung trang phải dùng được với danh sách ít phim, mô tả nhiều đoạn, tên phim dài, nhiều vé và lỗi không tải được dữ liệu. Không căn giữa dọc toàn bộ main chỉ để một màn hình ít nội dung trông đầy; cách căn giữa đó dễ khiến các trang dài thay đổi vị trí tiêu đề.

Nếu ảnh đang tải hoặc không tải được, giữ vùng ảnh theo cách bố trí đã chọn để tránh đẩy các nút sang vị trí bất ngờ. Khi thêm lỗi validation hoặc thông báo nghiệp vụ, khối có thể tăng chiều cao; không cắt thông báo để giữ bố cục bằng nhau.

## 3. Header, điều hướng và footer

### 3.1. Header trên desktop và tablet

| Vùng | Nội dung | Căn chỉnh và xử lý tràn |
|---|---|---|
| Trái | Logo, dẫn về trang chủ | Giữ tỉ lệ ảnh; không kéo giãn logo để lấp khoảng trống. Nếu chưa có tài sản logo, cách thay bằng tên ứng dụng là đề xuất thiết kế. |
| Giữa / cạnh logo | “Phim đang chiếu”, “Sắp chiếu” | Các mục nằm cùng hàng nếu đủ rộng; đánh dấu mục hiện tại theo thông tin đang xem. |
| Phải, chưa đăng nhập | “Đăng nhập”, “Đăng ký” | Nhóm thao tác rõ ràng, không chèn giữa các mục phim. |
| Phải, đã đăng nhập | Tên người dùng và menu tài khoản | Menu có “Booking của tôi”, “Đăng xuất”; không tự thêm mục quản trị vào header Customer. |

Header dùng cùng trục căn trái/phải với container nội dung. Nếu nền header trải toàn chiều rộng, phần nội dung bên trong vẫn căn theo container để logo và tiêu đề trang không lệch trục.

Trên tablet, giữ menu chính khi đủ chỗ; nếu tràn, thu gọn mục vào menu theo đặc tả responsive. Không giảm cỡ chữ đến mức khó đọc hoặc ép logo và tài khoản đè lên nhau để giữ một hàng.

**Đề xuất bố cục:** tên tài khoản dài được giới hạn vùng hiển thị và dùng ellipsis khi cần; tên đầy đủ có thể đọc trong menu mở. Không để chuỗi tên dài làm các nút xác thực hoặc menu phim ra ngoài viewport.

### 3.2. Header trên mobile

Header thu gọn thành logo và nút menu. Menu mở dưới dạng ngăn kéo (drawer); bên trong có điều hướng phim và khu vực tài khoản tương ứng với phiên hiện tại.

**Đề xuất bố cục cho drawer:**

1. Phần đầu có tiêu đề menu và nút đóng dễ nhận biết.
2. Phần điều hướng chứa nhóm phim đang chiếu/sắp chiếu, giữ cùng ý nghĩa với desktop.
3. Phần tài khoản chứa đăng nhập/đăng ký hoặc tên tài khoản, booking của tôi và đăng xuất.
4. Nếu nội dung dài hơn chiều cao thiết bị, cuộn trong drawer; nút đóng luôn tiếp cận được.

Không đưa form đăng ký, thông tin thanh toán hoặc toàn bộ lịch sử booking vào drawer toàn cục. Drawer là nơi điều hướng; dữ liệu và thao tác chính nằm tại màn hình tương ứng.

Khi đổi từ mobile sang kích thước lớn hơn, **đề xuất bố cục** đóng drawer và khôi phục cuộn nền nếu nó không còn được dùng. Việc đổi kích thước không làm đăng xuất hoặc mất booking đang xem.

### 3.3. Footer

Footer gồm thông tin rạp được cung cấp và các liên kết tĩnh có nội dung thực, chẳng hạn giới thiệu hoặc điều khoản khi đã có trang đích. Không tạo số điện thoại, địa chỉ, liên kết mạng xã hội hoặc chính sách giả để làm đầy footer.

- Desktop/tablet: các nhóm thông tin có thể nằm cùng hàng nếu đủ rộng; nhãn nhóm và liên kết căn theo cùng container.
- Mobile: xếp dọc, cho địa chỉ hoặc tên dài xuống dòng; khoảng cách giữa các liên kết đủ để không bấm nhầm.
- Footer không chứa nút “Đặt vé” hoặc “Thanh toán” bắt buộc của màn hình; các hành động đó nằm gần dữ liệu cần xác nhận.
- Footer không phải menu đáy cố định. Nếu có bottom bar ở KH-04, vẫn phải cuộn tới được nội dung footer.
- Nếu thiếu một nhóm thông tin tùy chọn, bỏ nhóm hoặc hiển thị thông báo phù hợp; không giữ nhiều cột rỗng gây mất cân đối.

## 4. Container, lưới và căn chỉnh

### 4.1. Chiều rộng và khoảng đệm

Main dùng container căn giữa, có giới hạn chiều rộng và padding hai bên. Bề rộng thực tế không vượt vùng nhìn thấy; các khối con phải co được trong container, kể cả khi chứa mã booking hoặc tên phim dài.

| Thông số | Quy tắc / đề xuất |
|---|---|
| Chiều rộng tối đa | Có giới hạn thống nhất. **Đề xuất để review:** `1200px`, là giá trị minh họa, không phải token đã chốt. |
| Padding ngang | Luôn có ở mọi thiết bị. **Đề xuất để review:** khoảng `16px` trên mobile, `24px` khi có nhiều chỗ hơn. |
| Nhịp khoảng cách | Dùng thang nhất quán giữa các khối. Các giá trị `4/8/16/24/32px` là ví dụ để review; không tự coi là toàn bộ hệ token. |
| Khoảng cách với header | Tiêu đề và điểm focus đầu trang không bị header sticky che; tính theo chiều cao header thực. |
| Khoảng cách cuối main | Đủ tách nội dung khỏi footer; nếu có bottom bar, chừa thêm đúng phần không gian thanh này chiếm. |

Không bắt mọi loại nội dung phải trải rộng bằng container. Form đăng nhập, thanh toán và thẻ vé có thể nằm trong vùng hẹp hơn, căn giữa. Chiều rộng hẹp cụ thể cần review, không tự áp một con số pixel cho mọi form.

### 4.2. Lưới trang và các cột nội dung

Lưới 12 cột là cơ sở căn chỉnh vùng nội dung trên màn hình rộng; nó không có nghĩa hiển thị 12 card phim trong một hàng. Danh sách phim có 3 card/hàng desktop, 2 tablet và 1 mobile.

| Kiểu nội dung | Tổ chức desktop | Quy tắc co giãn |
|---|---|---|
| Danh sách phim | Grid 3 cột | Các card có chiều rộng tương đương; hàng cuối có ít card vẫn giữ cùng trục. |
| Chi tiết phim | Poster trái, thông tin phải | Cột thông tin nhận phần diện tích còn lại; không ép mô tả vào một cột quá hẹp. |
| Chọn ghế | Sơ đồ chiếm phần lớn, tóm tắt bên phải | Sơ đồ có thể rộng hơn vùng nhìn; khi cần cuộn thì cuộn trong khối. |
| Form / thanh toán / vé | Một cột hẹp hơn container khi phù hợp | Chiều rộng co theo viewport, không vượt padding hai bên. |
| Tài khoản có điều hướng phụ | Nếu dùng sidebar, bên trái hẹp và nội dung bên phải rộng | Sidebar là tùy chọn; trên mobile chuyển thành nhóm chọn ngang hoặc menu phù hợp. |

Tỉ lệ cột poster/thông tin hoặc sơ đồ/tóm tắt là **đề xuất bố cục cần review**. Không áp tỉ lệ cố định khiến nội dung dài bị cắt hoặc ghế trở nên quá nhỏ. Cùng một nội dung không bị nhân đôi chỉ để lấp đủ cột.

### 4.3. Căn chữ, giá, nhãn và nút

- Văn bản tiếng Việt căn trái; cho phép xuống dòng ở tên phim, mô tả, địa chỉ và thông báo lỗi.
- Nhãn và số tiền trong một hàng giá có thể đặt hai phía; khi nhiều hàng, số tiền căn cùng trục để dễ so sánh.
- Tổng tiền, ngày/giờ suất và mã ghế phải đọc đầy đủ. Không dùng ellipsis cho số tiền hoặc cắt mất phần ngày khiến khách nhầm suất.
- Mã booking/vé dài được ngắt dòng phù hợp, không kéo rộng container và không mất ký tự. Cách hiển thị không được thay đổi giá trị mã.
- Nút chính nằm gần khối dữ liệu nó xác nhận. Nút hủy có vùng riêng hoặc khoảng cách phân biệt để tránh bấm nhầm.
- **Đề xuất bố cục:** thứ bậc chữ gồm tiêu đề trang, tiêu đề khối, nội dung, nhãn và ghi chú; không tự chốt font hoặc bảng cỡ chữ khi chưa có thiết kế được duyệt.

### 4.4. Ảnh, card và chiều cao

Poster trong danh sách dùng khung có tỉ lệ nhất quán để grid không bị lệch; tỉ lệ cụ thể cần review. Giữ đúng tỉ lệ ảnh gốc khi hiển thị, không kéo giãn hoặc cắt phần quan trọng để ép khớp một chiều cao. Cách đặt ảnh trong khung cần nhất quán giữa các card.

Card cần dành chỗ cho tiêu đề và nhãn; tên dài được xuống dòng hoặc có cách đọc đầy đủ. Không ép toàn bộ card có chiều cao cố định nếu điều đó cắt mất nội dung quan trọng. **Đề xuất bố cục:** có thể căn hàng nút cùng đáy trong một hàng grid nếu vẫn cho vùng chữ giãn tự nhiên.

Ảnh thay thế khi thiếu poster giữ kích thước khối tương đương. QR được giữ nguyên tỉ lệ vuông và vùng trắng quanh mã; không dùng cách cắt ảnh poster cho QR, không phủ badge hoặc icon lên phần mã cần quét.

## 5. Bố cục theo từng màn hình

### 5.1. KH-01 — Trang chủ / danh sách phim

Thứ tự trong main: tiêu đề danh sách → nhóm đang chiếu/sắp chiếu → vùng danh sách phim → điều khiển phân trang hoặc tải thêm. Nếu có banner với nội dung hợp lệ, banner là vùng giới thiệu tùy chọn; không thay thế danh sách phim và không che menu phân nhóm.

```text
┌───────────────────────────────────────────────────┐
│ Tiêu đề danh sách phim                            │
│ [Đang chiếu] [Sắp chiếu]                           │
├────────────────┬────────────────┬─────────────────┤
│ Poster         │ Poster         │ Poster          │
│ Tên phim       │ Tên phim       │ Tên phim        │
│ Nhãn tuổi      │ Nhãn tuổi      │ Nhãn tuổi       │
│ Xem chi tiết   │ Xem chi tiết   │ Xem chi tiết    │
├────────────────┴────────────────┴─────────────────┤
│ Phân trang hoặc tải thêm                          │
└───────────────────────────────────────────────────┘
```

Khung trên minh họa desktop. Tablet có 2 card/hàng; mobile có 1 card/hàng, giữ thứ tự đọc và thao tác xem chi tiết. Đây không phải yêu cầu luôn phải đủ ba phim hoặc có một nút tách khỏi vùng card.

| Vùng | Quy tắc bố trí |
|---|---|
| Tiêu đề và phân nhóm | Cùng trục với grid; nhóm đang chọn rõ ràng. Trên mobile cho nhãn đủ chỗ, không đẩy tab ra ngoài trang. |
| Poster | Cùng hệ khung ảnh giữa các card; giữ vị trí khi loading hoặc ảnh lỗi. |
| Tên và nhãn | Tên có ưu tiên đọc trước; badge tuổi/trạng thái không phủ lên tên hoặc nút. Nếu đặt badge trên poster thì không che nội dung cần nhận diện. |
| Hành động xem chi tiết | Toàn card có thể là vùng điều hướng theo thiết kế; tránh các vùng bấm lồng nhau gây khó thao tác. |
| Điều khiển danh sách | Nằm sau grid và trước footer; không nổi cố định che hàng phim cuối. Chỉ chọn một kiểu phân trang/tải thêm được thống nhất. |

Không có phim hoặc lỗi tải: thay vùng grid bằng thông báo và hành động phù hợp, vẫn giữ tiêu đề/phân nhóm. Không lấp card bằng dữ liệu giả để giữ số cột. Tìm kiếm/lọc nâng cao chỉ có vùng riêng khi đã chốt dữ liệu và chức năng; không tự thêm thanh tìm kiếm chỉ để lấp khoảng trắng.

### 5.2. KH-02 — Chi tiết phim

Desktop chia hai vùng: poster bên trái; thông tin và nút “Chọn suất chiếu” bên phải. Tiêu đề phim nằm trong vùng thông tin, không dùng tiêu đề trùng ở cả hai cột.

```text
┌───────────────────┬───────────────────────────────┐
│ Poster            │ Tên phim                      │
│                   │ Tuổi · Thời lượng · Thể loại  │
│                   │ Ngày phát hành · Ngôn ngữ     │
│                   │ Mô tả                         │
│                   │ Xem trailer khi có dữ liệu    │
│                   │ [Chọn suất chiếu]             │
└───────────────────┴───────────────────────────────┘
```

- Poster không tự kéo cao theo một mô tả rất dài; phần mô tả có thể giãn xuống theo luồng trang.
- Thông tin ngắn được nhóm gần tên phim; mô tả là một khối văn bản riêng, không trộn với nhãn tuổi hoặc giá.
- Trailer chỉ có vùng hiển thị khi có dữ liệu. Nhúng video hay dùng liên kết/nút mở trailer là **đề xuất bố cục cần review**; không để khung video trống lớn nếu chưa có trailer.
- Nút xem lịch nằm cạnh vùng thông tin quyết định; không chỉ đặt ở footer hoặc một banner khác.
- Mobile: poster, thông tin và hành động xếp một cột; giữ tên phim đầy đủ và không co poster làm chữ/nút bị chen sang hai bên.
- **Đề xuất bố cục:** tablet có thể dùng một hoặc hai cột tùy độ dài nội dung và bề rộng thực, miễn nút và mô tả vẫn đọc được; không tạo breakpoint mới để mô tả lựa chọn này như quy định đã chốt.

Nếu chi tiết phim và lịch chiếu được gộp trong một trang, khối lịch nằm sau thông tin phim và tuân thủ KH-03. Cách gộp trang là lựa chọn thiết kế; không hiển thị hai bộ chọn lịch khác nhau cùng điều khiển một nội dung mà không có lý do.

### 5.3. KH-03 — Lịch chiếu

Thứ tự đọc: phim đang xem → ngày đang chọn → danh sách suất → phản hồi không có suất/lỗi nếu có. Thông tin rạp chỉ đặt khi có dữ liệu phù hợp; không tạo bộ chọn chuỗi rạp/tỉnh cho bố cục một rạp.

| Vùng | Desktop / tablet | Mobile |
|---|---|---|
| Ngữ cảnh phim | Tên phim, có thể kèm poster thu nhỏ | Giữ tên phim rõ; poster thu nhỏ là tùy chọn. |
| Chọn ngày | Tabs nằm trên danh sách, có nhãn ngày đọc đầy đủ | Dropdown hoặc hàng ngày cuộn ngang trong khối; ngày đang chọn luôn nhận biết được. |
| Suất | Danh sách xếp dọc theo giờ; mỗi item nhóm giờ, phòng và giá cơ sở | Item đủ rộng cho chữ; nếu cần, xuống dòng trong item thay vì ép thành hàng nút nhỏ. |
| Phản hồi | Ngay dưới vùng ngày, tại vị trí danh sách suất | Giữ điều khiển ngày để khách đổi ngày khi danh sách rỗng. |

Một item suất phải nhận diện được ngày/giờ, phòng và giá cơ sở nếu có. Không dùng màu hoặc chỉ ghi một giờ mà thiếu ngữ cảnh khi danh sách gồm nhiều ngày. Không hiển thị “còn X ghế” chỉ để điền vào item khi không có dữ liệu.

**Đề xuất bố cục:** các suất có cùng thông tin có thể dùng nhịp căn chỉnh thống nhất để so sánh giờ/phòng/giá; trạng thái không thể chọn đi kèm nhãn giải thích nếu item đó đang được hiển thị. Không phải yêu cầu tải và dựng thêm suất đóng bán.

### 5.3.1. Sơ đồ thứ tự đọc của các trang duyệt phim

```mermaid
flowchart TD
    browsePage["Vùng nội dung trang duyệt phim"] --> screenKind{"Loại màn hình?"}
    screenKind -->|"Danh sách phim"| listTitle["Tiêu đề và nhóm phim"]
    listTitle --> movieGrid["Grid card phim"]
    movieGrid --> pagination["Điều khiển danh sách"]
    screenKind -->|"Chi tiết phim"| movieIntro["Poster và thông tin phim"]
    movieIntro --> movieDescription["Mô tả và trailer nếu có"]
    movieDescription --> showtimeAction["Hành động xem lịch"]
    screenKind -->|"Lịch chiếu"| movieContext["Ngữ cảnh phim"]
    movieContext --> datePicker["Chọn ngày"]
    datePicker --> showtimeList["Danh sách suất hoặc phản hồi"]
```

Sơ đồ mô tả thứ tự nhóm nội dung bên trong từng màn hình. Nó không yêu cầu poster desktop phải nằm trên thông tin thay vì bên trái; khi chia hai cột, thứ tự đọc vẫn phải có nghĩa khi trở thành một cột.

### 5.4. KH-04 — Chọn ghế và loại vé

#### 5.4.1. Desktop

Thông tin phim/suất/phòng nằm đầu main. Bên dưới là hai cột: sơ đồ ghế bên trái chiếm phần lớn chiều rộng, tóm tắt lựa chọn bên phải. Chú thích ghế đi cùng sơ đồ, không nằm riêng ở footer.

```text
┌───────────────────────────────────────────────────┐
│ Phim — ngày/giờ — phòng — quay lại lịch            │
├────────────────────────────────┬──────────────────┤
│ Hướng màn hình                 │ Ghế đang chọn    │
│ Nhãn hàng và sơ đồ ghế         │ Loại vé từng ghế │
│                                │ Số ghế / số chỗ  │
│                                │ Giá có dữ liệu   │
│ Chú thích loại / trạng thái    │ [Đặt vé]         │
└────────────────────────────────┴──────────────────┘
```

Khối tóm tắt rộng đủ cho nhãn ghế, loại vé và tiền xuống dòng hợp lý. Không cho nó chiếm gần hết chiều rộng làm sơ đồ còn một dải hẹp. Cách cho tóm tắt bám theo cuộn trên desktop là **đề xuất bố cục**, chưa bắt buộc; nếu dùng, không làm các dòng cuối hoặc nút hủy bị khuất dưới header.

#### 5.4.2. Tablet và mobile

- Tablet: sơ đồ toàn chiều rộng container, tóm tắt nằm dưới; không tiếp tục ép hai cột như desktop.
- Mobile: thứ tự là thông tin suất → sơ đồ → chú thích → chi tiết lựa chọn; thanh đáy chứa tổng tiền khi có dữ liệu và nút “Đặt vé”.
- Thanh đáy là nơi tóm tắt hành động, không chứa toàn bộ danh sách ghế hoặc form loại vé. Những nội dung dài vẫn nằm trong luồng cuộn của trang.
- Nếu chưa có giá từ server, vùng giá hiển thị thông báo phù hợp theo đặc tả màn hình; không tự tính tổng để lấp chỗ trống trong thanh đáy.
- Chừa khoảng trống ở cuối nội dung bằng phần chiều cao thanh đáy thực tế và khoảng cách cần thiết; không dùng một padding cố định quá nhỏ khi nhãn nút hoặc ghi chú xuống dòng.
- **Đề xuất bố cục:** tính cả vùng an toàn ở mép dưới thiết bị; khi bàn phím ảo mở cho input, thanh đáy không che trường nhập, lỗi hoặc nút đang focus.

#### 5.4.3. Bố trí trong sơ đồ ghế

| Thành phần | Quy tắc bố trí |
|---|---|
| Nhãn hàng / số ghế | Đọc rõ, gắn đúng vị trí; giữ thứ tự hàng và ghế theo dữ liệu bố trí được cung cấp. |
| Standard và VIP | Cùng đơn vị ghế một chỗ; hình thức phân biệt theo component, không thu hẹp vùng bấm để vừa trang. |
| Couple | Biểu diễn nguyên một đơn vị hai chỗ, rộng hơn ghế đơn theo thiết kế; không vẽ thành hai nút chọn nửa ghế. |
| Chú thích | Phân biệt loại ghế và khả năng chọn; không chỉ dựa vào màu. Khi nhiều mục, cho xuống dòng trong vùng chú thích. |
| Hướng màn hình / lối đi | Dựa trên bố trí đã được cung cấp. Nhãn hướng màn hình là đề xuất UI; không suy lối đi hoặc hàng Couple từ số ghế. |
| Vùng cuộn ngang | Chỉ bao sơ đồ cần thiết; có dấu hiệu cho biết còn nội dung ngang. Không kéo thông tin suất và bảng tóm tắt đi theo sơ đồ. |

Vùng bấm ghế trên mobile tối thiểu 44px theo đặc tả responsive; không giảm xuống chỉ để hiển thị toàn phòng cùng lúc. Khi cuộn ngang, tên hàng và ghế phải còn nhận diện được; cách giữ nhãn hàng ở mép trái là **đề xuất bố cục cần review**.

Thông tin liên hệ nếu được chốt đưa vào bước này nằm cùng vùng xác nhận lựa chọn, không chen giữa các hàng ghế. Countdown chỉ dành cho booking đã được tạo; không chừa một đồng hồ “giữ ghế” đang chạy ngay khi khách mới chọn cục bộ. Khi quay lại ngữ cảnh booking đã có, vùng thời gian và thông báo đặt cạnh tóm tắt đơn.

#### 5.4.4. Sơ đồ chọn bố cục ghế

```mermaid
flowchart TD
    seatPage["Nội dung màn hình chọn ghế"] --> seatContext["Thông tin phim, suất và phòng"]
    seatContext --> seatWidth{"Nhóm kích thước?"}
    seatWidth -->|"Desktop"| desktopSeats["Sơ đồ trái, tóm tắt phải"]
    desktopSeats --> desktopAction["Hành động trong vùng tóm tắt"]
    seatWidth -->|"Tablet"| tabletSeats["Sơ đồ toàn chiều rộng"]
    tabletSeats --> tabletSummary["Tóm tắt và hành động bên dưới"]
    seatWidth -->|"Mobile"| mobileSeats["Sơ đồ cuộn ngang trong khối"]
    mobileSeats --> mobileDetails["Chú thích và chi tiết lựa chọn"]
    mobileDetails --> mobileAction["Thanh đáy tổng hợp và nút đặt"]
    mobileAction --> reserveSpace["Chừa chỗ để không che nội dung"]
```

### 5.5. KH-05 — Thanh toán

Thanh toán dùng một cột, có thể hẹp hơn container chính và căn giữa. Không bổ sung một sidebar quảng cáo hoặc một bố cục nhiều cổng để lấp phần chiều rộng còn lại.

| Thứ tự | Khối nội dung | Quy tắc bố trí |
|---|---|---|
| 1 | Tiêu đề thanh toán và mã booking | Mã có thể xuống dòng; không đẩy tiêu đề hoặc nút ra ngoài khung. |
| 2 | Tóm tắt phim/suất/phòng/ghế | Xếp theo nhóm dễ kiểm tra, trước thao tác xác nhận tiền. |
| 3 | Chi tiết giá và tổng tiền | Nhãn/giá căn cùng trục; tổng nổi bật, đủ chỗ cho VND và số dài. |
| 4 | Hạn giữ và thông báo liên quan | Nằm gần hành động; khi nội dung dài, không bị đẩy vào tooltip hoặc toast duy nhất. |
| 5 | Cổng/phương thức đang hỗ trợ | Chỉ có thành phần thực được cung cấp; không tự dựng form nhập số thẻ hoặc danh sách ngân hàng. |
| 6 | Hành động theo điều kiện | Thanh toán, kiểm tra lại hoặc hủy/đổi ghế theo đặc tả màn hình; phân biệt nút hủy với nút chính. |
| 7 | Phản hồi sau thao tác | Đặt gần nút hoặc trong vùng thông báo ổn định, không làm biến mất tóm tắt đơn cần đối chiếu. |

Trên mobile vẫn giữ một cột. Hàng giá hoặc thông tin ghế dài có thể xuống dòng; không biến tổng thanh toán thành một ô cuộn ngang. Không mặc định dùng thanh hành động bám đáy của trang chọn ghế cho trang thanh toán.

Khi kết quả đang được xác nhận hoặc đã qua hạn danh nghĩa, vùng phản hồi có đủ diện tích cho lời giải thích và thao tác được phép. Bố cục không tự chuyển sang kiểu “thành công” chỉ vì có thông báo từ cổng; dữ liệu quyết định nội dung theo đặc tả màn hình.

### 5.6. KH-06 — Xác nhận đặt vé và vé điện tử

Bố cục một cột, ưu tiên khách nhận diện đơn rồi đọc từng vé: thông báo kết quả → mã booking → thông tin suất và ghế → danh sách thẻ vé → liên kết điều hướng.

| Khối | Quy tắc bố trí |
|---|---|
| Thông báo kết quả | Nổi bật đầu vùng nội dung; không dùng banner chiếm toàn viewport khiến mã booking/vé bị đẩy quá xa. |
| Thông tin booking | Mã, phim, ngày/giờ, phòng và tổng tiền theo dữ liệu có; nhóm riêng với mã của từng vé. |
| Thẻ vé | Mã vé dạng chữ, nhãn ghế, QR và trạng thái trong cùng một khối; mỗi vé nhận diện độc lập. |
| Couple | Hai thẻ vé độc lập cho cùng đơn vị ghế; **đề xuất bố cục:** gom vào một nhóm ghế, xếp dọc và đánh dấu vé 1/2, 2/2. |
| Hành động cuối trang | “Về trang chủ”, “Booking của tôi”; không bổ sung nút in/PDF/gửi email nếu chưa có chức năng. |

QR có diện tích đủ để nhìn và quét ở thiết bị kiểm tra; kích thước cụ thể cần review cùng cách sinh QR, không tự chốt một chuẩn pixel. Mã chữ vẫn đọc được khi ảnh QR lỗi. Không đặt hai QR Couple chồng nhau, ghép thành một ảnh mã chung hoặc để badge trạng thái che vùng mã.

Danh sách nhiều vé tăng chiều dài trang và dùng cuộn dọc chính. Không nhốt các vé vào carousel hoặc khung cuộn nhỏ khiến khách bỏ sót vé còn lại. Thẻ vé đã sử dụng/vô hiệu hóa vẫn có tên, mã và nhãn rõ theo dữ liệu, không chỉ đổi màu nền để biểu thị.

#### 5.6.1. Sơ đồ thứ tự đọc khi thanh toán và nhận vé

```mermaid
flowchart TD
    bookingPage["Nội dung theo booking"] --> bookingScreen{"Màn hình đang xem?"}
    bookingScreen -->|"Thanh toán"| summary["Tóm tắt đơn và ghế"]
    summary --> amount["Chi tiết giá và tổng tiền"]
    amount --> deadline["Hạn giữ và thông báo"]
    deadline --> method["Phương thức được hỗ trợ"]
    method --> paymentActions["Hành động và phản hồi"]
    bookingScreen -->|"Xác nhận và vé"| confirmation["Kết quả và mã booking"]
    confirmation --> showtimeInfo["Thông tin suất và ghế"]
    showtimeInfo --> ticketCards["Danh sách từng thẻ vé"]
    ticketCards --> navigation["Về trang chủ hoặc Booking của tôi"]
```

Đây là sơ đồ sắp xếp nội dung, không phải luồng áp dụng thanh toán. Kết quả đang chờ, lỗi tải vé hoặc không đủ quyền thay phần nội dung liên quan bằng phản hồi phù hợp; khung header/main/footer vẫn giữ nguyên.
