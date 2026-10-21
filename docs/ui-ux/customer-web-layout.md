# Customer Web Layout

## 1. Mục đích

Mô tả bố cục tổng thể của website Customer. Tài liệu trả lời câu hỏi: nội dung được sắp xếp trong khung trang như thế nào?

## 2. Cấu trúc khung trang

```text
┌──────────────────────────────────────────────┐
│ Header (sticky)                              │
├──────────────────────────────────────────────┤
│ Main Content                                 │
│ ┌──────────────────────────────────────────┐ │
│ │ Container (max-width, căn giữa)          │ │
│ │ Nội dung từng màn hình                   │ │
│ └──────────────────────────────────────────┘ │
├──────────────────────────────────────────────┤
│ Footer                                       │
└──────────────────────────────────────────────┘
```

### 2.1. Header

- Chiều cao cố định, sticky khi cuộn.
- Bố cục ngang: logo (trái) — menu chính (giữa/trái) — khu vực tài khoản (phải).
- Trên mobile: thu gọn thành logo + nút menu (hamburger), menu mở dạng drawer.

### 2.2. Main content

- Container căn giữa với `max-width` cố định (ví dụ 1200px) và padding hai bên.
- Mỗi màn hình render vào một vùng nội dung duy nhất; không dùng nhiều khung cuộn độc lập.
- Sidebar (nếu cần cho trang tài khoản): cột trái hẹp, nội dung chính bên phải; trên mobile sidebar chuyển thành tabs ngang.

### 2.3. Footer

- Nằm cuối trang, luôn ở đáy viewport kể cả khi nội dung ngắn (sticky footer layout bằng flexbox: header — main (flex: 1) — footer).

## 3. Quy tắc lưới và căn chỉnh

- Dùng grid 12 cột cho bố cục dạng khối (danh sách phim: card 3 cột desktop, 2 cột tablet, 1 cột mobile — chi tiết responsive tại `customer-responsive-specification.md`).
- Khoảng cách giữa các khối dùng thang spacing thống nhất (ví dụ 4/8/16/24/32px).
- Nội dung văn bản căn trái; số tiền căn phải; nhãn/nút căn theo ngữ cảnh.
- Giá, thời gian hiển thị theo định dạng thống nhất (VND, `Asia/Ho_Chi_Minh`).

## 4. Vùng nội dung theo màn hình

| Màn hình | Bố cục vùng nội dung |
|---|---|
| Home | Danh sách phim dạng grid card; phân nhóm "Đang chiếu" / "Sắp chiếu" |
| Movie Detail | Hai cột: poster (trái) — thông tin phim + nút hành động (phải) |
| Showtime | Bộ chọn ngày (tabs ngang) — danh sách suất xếp dọc theo giờ |
| Seat Selection | Hai cột: sơ đồ ghế (trái, chiếm phần lớn) — bảng tóm tắt ghế/tổng tiền (phải); mobile: sơ đồ trước, tóm tắt dạng bottom bar |
| Payment | Một cột hẹp (form thanh toán) tóm tắt booking phía trên |
| Booking Confirmation | Một cột: mã booking — vé dạng danh sách thẻ có QR |

## 5. Quy tắc chung

- Mọi màn hình dùng chung Header/Footer layout; chỉ vùng main thay đổi.
- Không có horizontal scroll ở mọi breakpoint; nội dung tràn xử lý theo `customer-responsive-specification.md`.
- Trạng thái toàn màn hình (loading/error/empty) hiển thị bên trong vùng main, giữ nguyên Header/Footer (xem `customer-ui-state.md`).
