# Prompt 04 — Bố cục website Customer

**Ngày ghi nhận:** 27/10/2026 · **Branch tác vụ:** `docs/customer-screen-specification`

**File:** `docs/ui-ux/customer-web-layout.md`

## Prompt của người dùng — diễn đạt lại

> Áp dụng các yêu cầu trước đó cho file `customer-web-layout.md`.

## Phản hồi của AI — tóm tắt công việc và kết quả

AI áp dụng các yêu cầu trước đó: viết chi tiết bằng tiếng Việt, nhúng Mermaid, giữ trọng tâm tài liệu, chỉ sửa file được chọn và commit từng phần với message ngắn bằng tiếng Anh.

Sau khi đọc layout hiện có và đối chiếu các đặc tả UI liên quan, AI soạn lại nội dung gồm:

- Khung header/main/footer, container, menu desktop/mobile, drawer và footer.
- Lưới, căn chỉnh chữ/giá/nút, poster, card và cách xử lý tên hoặc mã dài.
- Bố cục của 8 nhóm màn hình, gồm cả xác thực và tài khoản.
- Sơ đồ ghế cuộn ngang trong khối; thanh đặt vé mobile có phần chừa chỗ để không che nội dung.
- Hai thẻ vé Couple độc lập và cách giữ mã/QR đọc được.
- Bố cục desktop/tablet/mobile, lớp phủ, vị trí thông báo và **20 tiêu chí nghiệm thu layout**.

AI phân biệt các giá trị minh họa như max-width/padding với token đã được duyệt, không tự chốt màu, font hoặc z-index chính thức.

Tài liệu có **555 dòng** và **6 sơ đồ Mermaid**: khung trang, trang duyệt phim, bố cục ghế, nội dung thanh toán/vé, viewport và lớp phủ. Cả 6 đã parse/render SVG thành công trong Chromium cục bộ và được xem ảnh kết quả. Kiểm tra cấu trúc, 5 liên kết nội bộ, mã màn hình/tiêu chí, bảng, code fence và `git diff --check` đều đạt.

Chỉ sửa file layout, giữ công cụ/ảnh trong `/tmp` và bảo toàn thay đổi có sẵn. Đã tạo **6 commit**, chưa push:

`896cef8`, `b39a07f`, `8a58ece`, `0f54e6b`, `ff09bf7`, `78f6aaa`.

Nội dung và sơ đồ do AI soạn theo yêu cầu. Không tạo frontend hoặc kiểm thử giao diện ứng dụng thật; các đề xuất bố cục vẫn cần review. Bản lịch sử xem bằng `git show 78f6aaa:docs/ui-ux/customer-web-layout.md`.
