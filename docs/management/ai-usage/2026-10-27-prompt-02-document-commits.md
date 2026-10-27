# Prompt 02 — Chia phần và commit tài liệu

**Ngày ghi nhận:** 27/10/2026 · **Branch tác vụ:** `docs/customer-screen-specification`

**Phạm vi:** Cách sửa và commit `customer-screen-specification.md`.

## Prompt của người dùng — diễn đạt lại

> Mỗi lần viết xong một phần thì commit giúp tui nha. Chỉ sửa file đang làm, đừng thêm file mới hay để mấy file linh tinh trong thư mục project.

## Phản hồi của AI — tóm tắt công việc và kết quả

AI tiếp tục tác vụ đặc tả màn hình và chia nội dung thành từng phần có ý nghĩa để commit:

1. Phạm vi và căn cứ của tài liệu.
2. Danh sách phim, chi tiết phim và lịch chiếu.
3. Chọn ghế, loại vé và tạo booking.
4. Thanh toán, hết hạn và hủy booking.
5. Vé điện tử, xác thực và booking cá nhân.
6. Các quy tắc chung và phần còn thiếu của contract.
7. Điểm cần review và tiêu chí nghiệm thu.

Trước khi commit, AI kiểm tra diff, staged file và `git diff --check`; chỉ stage file đặc tả, không đưa thay đổi sẵn có ở `.vscode/settings.json` vào commit. Công cụ trích xuất và kiểm tra dùng `/tmp`, không thêm artifact vào repository.

Kết quả là **7 commit cục bộ** của bản đặc tả đầu, được liệt kê trong [Prompt 01](2026-10-27-prompt-01-screen-specification.md). Không chuyển branch, viết lại lịch sử hoặc push. Đây là yêu cầu về cách thực hiện Git, không phải một bản thảo nội dung mới do người dùng cung cấp.

Các subject ban đầu bằng tiếng Việt; yêu cầu đổi sang message ngắn bằng tiếng Anh được người dùng bổ sung ở Prompt 03 và áp dụng cho commit mới.
