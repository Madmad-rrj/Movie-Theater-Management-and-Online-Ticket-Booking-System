# Prompt 01 — Đặc tả màn hình Customer

**Ngày ghi nhận:** 27/10/2026 · **Branch tác vụ:** `docs/customer-screen-specification`

**File:** `docs/ui-ux/customer-screen-specification.md`

## Prompt của người dùng — diễn đạt lại

> Đọc giúp tui tài liệu và code trong project rồi viết lại file `customer-screen-specification.md` bằng tiếng Việt nha. Viết kỹ từng màn hình, bám sát dự án và đừng tự thêm những gì chưa có căn cứ.

## Phản hồi của AI — tóm tắt công việc và kết quả

AI đã khảo sát cấu trúc dự án, đọc các tài liệu liên quan và soạn bản đặc tả chi tiết theo yêu cầu. Nội dung gồm:

- 8 nhóm màn hình: danh sách phim, chi tiết phim, lịch chiếu, chọn ghế, thanh toán, xác nhận/vé điện tử, đăng nhập/đăng ký và booking cá nhân.
- Dữ liệu hiển thị, thao tác, validation, loading/rỗng/lỗi, điều kiện chuyển bước và tiêu chí nghiệm thu cho từng màn hình.
- Các tình huống cần xử lý như xung đột ghế, retry cùng `requestKey`, payment chưa rõ kết quả, hết hạn giữ ghế và hai vé độc lập của ghế Couple.
- Phân biệt yêu cầu đã chốt với đề xuất; bản đầu ghi nhận 9 khoảng trống contract và 12 điểm cần review.

Bản đầu dài **707 dòng**, có **29 tiêu chí nghiệm thu**, chưa có Mermaid. Sau đó người dùng yêu cầu bổ sung sơ đồ và tinh gọn nội dung ở [Prompt 03](2026-10-27-prompt-03-screen-flows.md).

AI đã kiểm tra liên kết cục bộ, tham chiếu FR/BR/OQ, mã màn hình, tiêu chí nghiệm thu, bảng Markdown, code fence và `git diff --check`; kết quả đạt. Không chạy kiểm thử ứng dụng, database hoặc cổng thanh toán.

Chỉ sửa file đặc tả, giữ nguyên thay đổi có sẵn ở `.vscode/settings.json`. Việc chia phần để commit được bổ sung trong [Prompt 02](2026-10-27-prompt-02-document-commits.md). Các commit của bản đầu:

`d10e171`, `cb1b4a2`, `5ae945a`, `c87f9c1`, `a0dc80e`, `20c4897`, `456771c`.

Nội dung chi tiết do AI soạn theo yêu cầu; người dùng chọn tài liệu và đặt các ràng buộc. Bản lịch sử có thể xem bằng `git show 456771c:docs/ui-ux/customer-screen-specification.md`; không sao chép bản đó sang branch ghi log.
