# Nhật ký sử dụng AI — Prompt 03: Mermaid và tinh gọn đặc tả màn hình

## Thông tin ghi nhận

| Trường | Nội dung |
|---|---|
| Ngày ghi nhận | 2026-10-27, múi giờ Asia/Ho_Chi_Minh. |
| Thứ tự trong session | Yêu cầu công việc thứ ba; chỉnh sửa kết quả của Prompt 01. |
| Branch thực hiện tác vụ | `docs/customer-screen-specification`. |
| Branch ghi nhật ký | `docs/agent-log-0235`. |
| Tác vụ | Nhúng flowchart Mermaid và điều chỉnh trọng tâm tài liệu màn hình Customer. |
| File thay đổi của tác vụ | Chỉ `docs/ui-ux/customer-screen-specification.md`. |
| Trạng thái | Đã hoàn thành nội dung, kiểm tra sơ đồ và tạo các commit cục bộ. |

## Tóm tắt yêu cầu thực tế

Người dùng yêu cầu thêm flowchart Mermaid trực tiếp vào file Markdown, tiếp tục chia phần để commit, dùng message ngắn bằng tiếng Anh, giữ nội dung tài liệu tiếng Việt và bỏ phần phân tích hiện trạng/bối cảnh từ các thư mục khác. Đây là bản tóm tắt yêu cầu, không phải trích nguyên văn.

## Phân chia phần việc

| Bên thực hiện | Phần việc thể hiện trong hội thoại |
|---|---|
| Người dùng | Chỉ định bổ sung Mermaid, điều chỉnh phạm vi nội dung và quy cách commit message. |
| AI | Chỉnh bản đặc tả đã soạn, loại bỏ phần audit ngoài trọng tâm, viết sáu sơ đồ và phần diễn giải; kiểm tra Markdown và render sơ đồ. |

Không có đoạn Mermaid hoặc bản sửa nội dung do người dùng gửi trong prompt này. Các sơ đồ và phần chỉnh sửa được AI tạo theo yêu cầu; không ghi thành người dùng đã tự vẽ rồi nhờ AI kiểm tra.

## Công việc đã thực hiện

- Bỏ bảng nguồn đối chiếu, báo cáo hiện trạng mã nguồn, phân tích seed và bảng mâu thuẫn/contract riêng khỏi trọng tâm của file.
- Giữ các lưu ý chưa chốt ảnh hưởng trực tiếp đến màn hình, các điều kiện an toàn của đặt vé/thanh toán và 29 tiêu chí nghiệm thu.
- Nhúng 6 sơ đồ tiếng Việt: luồng tổng quan; chọn ghế/tạo booking; thanh toán/kiểm tra kết quả; hủy/đổi ghế; đăng nhập/đăng ký; lịch sử booking/xem vé.
- Làm rõ trên sơ đồ rằng chọn ghế chưa tạo quyền giữ, đăng nhập không tự gửi lại mutation, payment thành công riêng lẻ chưa đủ để hiển thị booking thành công.

## Kết quả và bằng chứng

| Trường | Nội dung |
|---|---|
| Bản kết quả | 745 dòng, 8 nhóm màn hình, 6 block Mermaid, 29 tiêu chí nghiệm thu tại commit `57d758e`. |
| Các commit | `5367343` — focus screen specification; `6c0333d` — customer flow; `4ddb38b` — seat booking flow; `239aa13` — payment flow; `857ecd2` — cancellation flow; `57d758e` — account flows. |
| Kiểm tra tài liệu đã chạy | Kiểm tra code fence, số cột bảng, đủ mã màn hình/tiêu chí, không còn mã tham chiếu nguồn đã bỏ và `git diff --check`; kết quả đạt. |
| Kiểm tra sơ đồ đã chạy | Parse và render SVG cả 6 sơ đồ bằng Mermaid trong Chromium cục bộ; cả 6 đạt. Có xem ảnh render để kiểm tra trình bày. |
| Artifact kiểm tra | Script, profile Chromium và ảnh nằm trong `/tmp/cinema-screen-review/`, không tạo file phụ trong repository. |
| Git | 6 commit mới với subject ngắn bằng tiếng Anh; không push do AI thực hiện; bảo toàn `.vscode/settings.json`. |

## Giới hạn và cách đối chiếu

Lần thử parser trong Node không có môi trường DOM gặp lỗi phụ thuộc; lần thử mở Chromium trong sandbox bị chặn socket. Sau khi dùng môi trường công cụ phù hợp để mở Chromium cục bộ, kiểm tra parse/render thành công. Không coi các lần thử ban đầu là kiểm tra đã đạt.

Render sơ đồ không phải kiểm thử API, booking, payment hoặc giao diện ứng dụng đã triển khai. Bản kết quả không được sao chép vào branch ghi nhật ký; có thể đọc lịch sử bằng `git show 57d758e:docs/ui-ux/customer-screen-specification.md`.
