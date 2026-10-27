# Prompt 05 — Ghi nhật ký sử dụng AI cho session

**Ngày ghi nhận:** 27/10/2026 · **Branch ghi log:** `docs/agent-log-0235`

**Phạm vi:** 5 file ai-usage và phần mô tả thư mục trong `PROJECT_STRUCTURE.md`.

## Prompt của người dùng — diễn đạt lại phần công việc

> Giúp tui ghi lại mấy prompt từ đầu session tới giờ vào ai-usage nha. Mỗi prompt một file Markdown, theo mẫu đang có, ghi đầy đủ một chút.

## Phản hồi của AI — tóm tắt công việc và kết quả

AI ghi lại các yêu cầu và kết quả có thể kiểm chứng, giữ rõ phần người dùng chỉ định và phần AI soạn; không đổi lịch sử thành việc người dùng đã tự viết các tài liệu chi tiết.

AI kiểm tra mẫu có sẵn và xác nhận thư mục thực tế là `docs/management/ai-usage/`. Đã tạo 5 bản ghi:

- [Prompt 01 — Đặc tả màn hình](2026-10-27-prompt-01-screen-specification.md).
- [Prompt 02 — Chia phần và commit](2026-10-27-prompt-02-document-commits.md).
- [Prompt 03 — Mermaid và tinh gọn nội dung](2026-10-27-prompt-03-screen-flows.md).
- [Prompt 04 — Bố cục website](2026-10-27-prompt-04-web-layout.md).
- Prompt 05 — Yêu cầu ghi nhật ký này.

Mỗi bản ghi nêu yêu cầu, phần AI thực hiện, kết quả và kiểm tra. AI đối chiếu 19 commit lịch sử cùng các bản tài liệu: **707 dòng** ở `456771c`, **745 dòng/6 sơ đồ** ở `57d758e`, **555 dòng/6 sơ đồ** ở `78f6aaa`.

Đã cập nhật phần ai-usage trong `PROJECT_STRUCTURE.md`, giữ nguyên log thành viên cũ và `.vscode/settings.json`. Kiểm tra tên file, các trường thông tin, bảng/code fence, liên kết nội bộ, commit dẫn và phạm vi thay đổi đều đạt; có kiểm tra staged file và `git diff --check`.

Bản nhật ký đầu tiên hoàn tất tại `c6ac656` với **5 commit cục bộ**, chưa push. Tác vụ ghi log không chạy build/test ứng dụng hoặc render lại Mermaid; các kết quả render trong Prompt 03/04 thuộc những tác vụ trước đó.

Các prompt trong log là bản diễn đạt lại, không phải transcript nguyên văn. Tài liệu sản phẩm được dẫn theo commit lịch sử vì branch ghi log không có thư mục `docs/ui-ux/`; AI không tự chuyển branch hoặc sao chép chúng sang đây.
