# Nhật ký sử dụng AI — Prompt 04: Bố cục website Customer

## Thông tin ghi nhận

| Trường | Nội dung |
|---|---|
| Ngày ghi nhận | 2026-10-27, múi giờ Asia/Ho_Chi_Minh. |
| Thứ tự trong session | Yêu cầu công việc thứ tư; chuyển sang tài liệu layout với các yêu cầu tương tự đã thống nhất trong hội thoại. |
| Branch thực hiện tác vụ | `docs/customer-screen-specification`. |
| Branch ghi nhật ký | `docs/agent-log-0235`. |
| Tác vụ | Viết chi tiết bố cục website Customer, nhúng Mermaid và commit theo từng phần. |
| File được yêu cầu / thay đổi | Chỉ `docs/ui-ux/customer-web-layout.md`. |
| Trạng thái | Đã hoàn thành tài liệu, kiểm tra và 6 commit cục bộ. |

## Tóm tắt yêu cầu thực tế

Người dùng chỉ định `customer-web-layout.md` và yêu cầu tiếp tục thực hiện các yêu cầu tương tự với file này. Theo ngữ cảnh trước đó, các yêu cầu gồm nội dung tiếng Việt chi tiết, phù hợp dự án, Mermaid nhúng trong Markdown, không đưa phân tích hiện trạng/bối cảnh ngoài trọng tâm vào file và commit từng phần với message ngắn bằng tiếng Anh.

Phần trên là diễn giải yêu cầu dựa vào ngữ cảnh, không phải một prompt nguyên văn dài do người dùng đã gửi. Người dùng không gửi kèm bản thảo layout chi tiết mới trong yêu cầu này.

## Phân chia phần việc

| Bên thực hiện | Phần việc thể hiện trong hội thoại |
|---|---|
| Người dùng | Chọn file layout và yêu cầu áp dụng các ràng buộc đã nêu ở tác vụ màn hình trước. |
| AI | Đọc layout hiện có và các đặc tả UI liên quan; soạn lại tài liệu chi tiết, vẽ Mermaid, tạo tiêu chí nghiệm thu, kiểm tra và commit. |

## Công việc đã thực hiện

- Đối chiếu nội dung layout, responsive, component, navigation và đặc tả màn hình đã được viết trong session.
- Mô tả khung header/main/footer, quan hệ container, menu desktop/mobile, drawer, footer, lưới, căn chỉnh chữ/giá/nút và cách xử lý nội dung dài.
- Bổ sung bố cục cho 8 nhóm màn hình: danh sách phim, chi tiết phim, lịch chiếu, chọn ghế, thanh toán, vé, xác thực và tài khoản.
- Làm rõ cuộn ngang trong sơ đồ ghế, thanh đặt vé mobile không che nội dung, cách trình bày hai vé Couple và QR độc lập.
- Phân biệt kích thước minh họa/đề xuất với quy định đã chốt; không tự tạo token màu, font hoặc z-index chính thức.
- Thêm 6 sơ đồ Mermaid: khung trang; thứ tự đọc trang duyệt phim; bố cục ghế theo thiết bị; nội dung thanh toán/vé; chọn bố cục theo viewport; lớp phủ/menu.
- Bổ sung 20 tiêu chí nghiệm thu layout và liên kết tới các tài liệu UI cùng thư mục.

## Kết quả và bằng chứng

| Trường | Nội dung |
|---|---|
| Bản kết quả | 555 dòng, 8 nhóm màn hình, 6 sơ đồ Mermaid và 20 tiêu chí `BL-01` đến `BL-20` tại commit `78f6aaa`. |
| Các commit | `896cef8` — page shell; `b39a07f` — shared layout; `8a58ece` — browsing layouts; `0f54e6b` — booking layouts; `ff09bf7` — responsive layouts; `78f6aaa` — layout checks. |
| Kiểm tra tài liệu đã chạy | Kiểm tra các section, đủ mã màn hình/tiêu chí, 5 liên kết cục bộ, code fence, số cột bảng, phạm vi file và `git diff --check`; kết quả đạt. |
| Kiểm tra sơ đồ đã chạy | Parse và render SVG thành công cả 6 sơ đồ bằng Mermaid trong Chromium cục bộ; xem ảnh kết quả để kiểm tra bố trí sơ đồ. |
| Artifact kiểm tra | Công cụ và ảnh kiểm tra nằm trong `/tmp/cinema-screen-review/`; không thêm artifact vào repository. |
| Git | 6 commit có message ngắn bằng tiếng Anh; không push do AI thực hiện; không sửa file sản phẩm khác hoặc `.vscode/settings.json`. |

## Giới hạn và cách đối chiếu

Đây là đặc tả bố cục và kiểm tra tài liệu/sơ đồ. Không tạo frontend, không kiểm thử giao diện ứng dụng thật ở các viewport và không xác nhận các tiêu chí `BL-*` đã được triển khai đạt yêu cầu.

Các kích thước minh họa và lựa chọn đánh dấu đề xuất còn cần review thiết kế. Branch ghi nhật ký không có bản layout này trong working tree; đọc bằng `git show 78f6aaa:docs/ui-ux/customer-web-layout.md` nếu cần đối chiếu lịch sử, không tự chuyển branch hoặc sao chép tài liệu.
