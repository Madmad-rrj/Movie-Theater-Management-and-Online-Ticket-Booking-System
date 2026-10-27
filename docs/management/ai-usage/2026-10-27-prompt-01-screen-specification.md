# Nhật ký sử dụng AI — Prompt 01: Đặc tả màn hình Customer

## Thông tin ghi nhận

| Trường | Nội dung |
|---|---|
| Ngày ghi nhận | 2026-10-27, múi giờ Asia/Ho_Chi_Minh; không suy diễn giờ bắt đầu/kết thúc. |
| Thứ tự trong session | Yêu cầu công việc thứ nhất; không tính thông tin môi trường và chỉ dẫn AGENTS.md là prompt công việc riêng. |
| Branch thực hiện tác vụ | `docs/customer-screen-specification`. |
| Branch ghi nhật ký | `docs/agent-log-0235`. |
| Tác vụ | Đọc dự án và soạn lại đặc tả màn hình Customer bằng tiếng Việt, chi tiết, phù hợp với dự án và tránh nội dung không có căn cứ. |
| File được yêu cầu | `docs/ui-ux/customer-screen-specification.md`. |
| Trạng thái tác vụ | Đã hoàn thành bản đặc tả đầu tiên trong session; sau đó được chỉnh sửa tiếp ở Prompt 03. |

## Tóm tắt yêu cầu thực tế

Người dùng chỉ định file `customer-screen-specification.md`, yêu cầu AI đọc dự án và viết nội dung tiếng Việt thật chi tiết, liên quan trực tiếp đến dự án, không bịa thông tin. Đây là bản tóm tắt yêu cầu, không phải trích nguyên văn lời nhắc.

## Phân chia phần việc

| Bên thực hiện | Phần việc thể hiện trong hội thoại |
|---|---|
| Người dùng | Chọn tài liệu cần viết; đặt yêu cầu về ngôn ngữ, mức độ chi tiết, tính liên quan và độ chính xác. |
| AI | Khảo sát file, đối chiếu tài liệu và mã nguồn liên quan; soạn nội dung chi tiết của file; bổ sung bảng dữ liệu, thao tác, lỗi, tiêu chí nghiệm thu và ghi nhận điểm còn thiếu. |

Bản chi tiết được AI soạn theo yêu cầu. Session không có căn cứ để ghi rằng người dùng đã tự viết phần nội dung này trước khi nhờ AI chỉnh câu chữ. Những tài liệu có sẵn trong repository được dùng làm đầu vào, không tự gán tác giả của chúng.

## Công việc đã thực hiện

- Kiểm tra cấu trúc repository, branch và working tree; bảo toàn thay đổi có sẵn ở `.vscode/settings.json`.
- Đọc và đối chiếu các tài liệu nghiệp vụ, use case, thiết kế, database, API và UI/UX liên quan; kiểm tra mã nguồn hiện có và tham khảo tài liệu PA/kế hoạch trong phạm vi phục vụ đặc tả.
- Soạn đặc tả 8 nhóm màn hình: danh sách phim, chi tiết phim, lịch chiếu, chọn ghế, thanh toán, xác nhận/vé, xác thực và booking cá nhân.
- Mô tả dữ liệu, validation, trạng thái giao diện, xử lý lỗi, retry/idempotency và điều kiện chuyển màn hình; phân biệt đề xuất với yêu cầu đã chốt.
- Ghi nhận 29 tiêu chí nghiệm thu, 9 khoảng trống contract và 12 điểm cần review trong bản đầu tiên.

## Kết quả và bằng chứng

| Trường | Nội dung |
|---|---|
| File thay đổi của tác vụ | Chỉ `docs/ui-ux/customer-screen-specification.md`; không tạo file mới trong repository cho tác vụ đặc tả. |
| Bản kết quả đầu tiên | 707 dòng tại commit `456771c`; bản này chưa có Mermaid. |
| Lịch sử thay đổi | `d10e171`, `cb1b4a2`, `5ae945a`, `c87f9c1`, `a0dc80e`, `20c4897`, `456771c`. Quy tắc chia phần/commit được người dùng bổ sung ở Prompt 02. |
| Kiểm tra đã chạy | Kiểm tra liên kết cục bộ, tham chiếu FR/BR/OQ, mã màn hình và tiêu chí nghiệm thu, số cột bảng, code fence và `git diff --check`; kết quả đạt ở lần kiểm tra kết thúc tác vụ. |
| Kiểm tra chưa chạy | Không chạy kiểm thử ứng dụng, database hoặc cổng thanh toán. Kiểm tra tài liệu không phải bằng chứng luồng nghiệp vụ đã được triển khai. |
| Git trong tác vụ | Tạo 7 commit cục bộ, không push trong thao tác do AI thực hiện. |

## Giới hạn của bản ghi

Nhật ký được lập hồi cứu từ hội thoại và đối chiếu commit, không phải transcript đầy đủ. Hiện branch ghi nhật ký không có thư mục `docs/ui-ux/`; nội dung kết quả được dẫn theo commit lịch sử, không được sao chép sang branch này.

Chỉ cần đọc lại bản lịch sử có thể dùng `git show 456771c:docs/ui-ux/customer-screen-specification.md`, không cần chuyển branch. Các thay đổi tiếp theo không bị mô tả nhầm là đã nằm trong bản 707 dòng ban đầu.
