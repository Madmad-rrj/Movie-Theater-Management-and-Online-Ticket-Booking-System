# Customer Responsive Specification

## 1. Mục đích

Quy định cách giao diện thích ứng với các kích thước màn hình và thiết bị khác nhau. Tài liệu trả lời câu hỏi: bố cục thay đổi ra sao theo từng breakpoint?

## 2. Breakpoint

| Tên | Kích thước | Thiết bị tham chiếu |
|---|---|---|
| Desktop | ≥ 1024px | Máy tính, laptop |
| Tablet | 768px – 1023px | Máy tính bảng |
| Mobile | < 768px | Điện thoại |

## 3. Thay đổi bố cục theo breakpoint

### 3.1. Desktop

- Dùng bố cục đầy đủ theo `customer-web-layout.md`: header ngang đầy đủ, menu chính hiển thị, grid card phim 3 cột.
- Seat Selection: sơ đồ ghế và bảng tóm tắt hai cột song song.

### 3.2. Tablet

- Menu chính hiển thị; nếu tràn thì thu gọn mục ít quan trọng vào dropdown.
- Grid card phim 2 cột.
- Seat Selection: sơ đồ ghế chiếm toàn chiều rộng; bảng tóm tắt chuyển xuống dưới sơ đồ.

### 3.3. Mobile

- Header thu gọn: logo + nút hamburger; menu mở dạng drawer; khu vực tài khoản nằm trong drawer.
- Grid card phim 1 cột (hoặc danh sách xếp dọc).
- Showtime: bộ chọn ngày dạng dropdown/cuộn ngang.
- Seat Selection: sơ đồ ghế cuộn ngang với chỉ báo có thể cuộn; nút "Đặt vé" và tổng tiền trong bottom bar cố định.
- Payment/Confirmation: một cột duy nhất.

## 4. Xử lý nội dung tràn

- Sơ đồ ghế trên mobile: cuộn ngang trong khối, giữ kích thước ô ghế tối thiểu để dễ bấm (target bấm ≥ 44px); không co ghế quá nhỏ.
- Tiêu đề/dòng dài: cắt bằng ellipsis hoặc xuống dòng; dòng quan trọng (tên phim, tổng tiền) không được cắt mất ý nghĩa.
- Bảng (nếu có): trên mobile chuyển thành danh sách thẻ hoặc cuộn ngang trong khối.
- Luôn tránh horizontal scroll ở mức trang; chỉ cuộn trong khối được thiết kế cho việc đó.

## 5. Quy tắc chung

- Cùng một dữ liệu và chức năng trên mọi breakpoint; responsive chỉ thay đổi bố cục, không ẩn chức năng cốt lõi.
- Menu responsive, danh sách phim, sơ đồ ghế là ba khối ưu tiên kiểm tra khi nghiệm thu trên từng breakpoint.
- Ảnh (poster) dùng kích thước theo container, tỉ lệ gốc giữ nguyên.
- Kiểm tra tối thiểu: 320px (mobile nhỏ), 768px, 1024px, 1440px.
