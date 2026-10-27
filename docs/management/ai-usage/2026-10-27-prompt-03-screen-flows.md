# Prompt 03 — Thêm Mermaid vào đặc tả màn hình

**Ngày ghi nhận:** 27/10/2026 · **Branch tác vụ:** `docs/customer-screen-specification`

**File:** `docs/ui-ux/customer-screen-specification.md`

## Prompt của người dùng — diễn đạt lại

> Thêm flowchart Mermaid vào ngay file này giúp tui nha. Nội dung vẫn viết tiếng Việt, bỏ phần phân tích hiện trạng với bối cảnh từ các thư mục khác đi. Tiếp tục commit từng phần, nhưng message viết ngắn gọn bằng tiếng Anh thôi.

## Phản hồi của AI — tóm tắt công việc và kết quả

AI chỉnh lại file để tập trung vào đặc tả màn hình: bỏ bảng nguồn đối chiếu, phần hiện trạng mã nguồn, phân tích seed và các bảng phân tích ngoài trọng tâm. Những lưu ý chưa chốt ảnh hưởng trực tiếp đến màn hình vẫn được giữ.

AI đã viết và nhúng **6 flowchart tiếng Việt**:

1. Luồng tổng quan của khách hàng.
2. Chọn ghế và tạo booking.
3. Thanh toán và kiểm tra kết quả.
4. Hủy booking / đổi ghế.
5. Đăng nhập và đăng ký.
6. Lịch sử booking và xem vé.

Các sơ đồ giữ rõ: chọn ghế chưa có nghĩa đã được giữ; đăng nhập không tự gửi lại yêu cầu đặt vé/thanh toán; payment thành công riêng lẻ chưa đủ để báo booking thành công; timeout cần kiểm tra kết quả thay vì tạo giao dịch khác.

Bản kết quả có **745 dòng**, **8 nhóm màn hình** và **29 tiêu chí nghiệm thu**. AI kiểm tra bảng, code fence, các mã màn hình/tiêu chí, tham chiếu đã bỏ và `git diff --check`; tất cả đạt. Sau các lần thử parser/môi trường ban đầu gặp lỗi, cả 6 sơ đồ đã parse và render SVG thành công bằng Mermaid trong Chromium cục bộ, kèm xem ảnh kết quả. Công cụ và ảnh nằm trong `/tmp/cinema-screen-review/`.

Chỉ sửa file được yêu cầu, tạo **6 commit tiếng Anh**, không push:

`5367343`, `6c0333d`, `4ddb38b`, `239aa13`, `857ecd2`, `57d758e`.

Sơ đồ và phần chỉnh sửa do AI tạo theo yêu cầu của người dùng. Kiểm tra render không phải kiểm thử ứng dụng hoặc API; bản lịch sử xem bằng `git show 57d758e:docs/ui-ux/customer-screen-specification.md`.
