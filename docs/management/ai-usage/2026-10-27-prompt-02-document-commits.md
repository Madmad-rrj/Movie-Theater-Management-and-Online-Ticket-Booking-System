# Nhật ký sử dụng AI — Prompt 02: Chia phần và commit tài liệu

## Thông tin ghi nhận

| Trường | Nội dung |
|---|---|
| Ngày ghi nhận | 2026-10-27, múi giờ Asia/Ho_Chi_Minh; không ghi giờ thực hiện khi hội thoại không cung cấp mốc chính xác. |
| Thứ tự trong session | Yêu cầu công việc thứ hai, gửi trong lúc AI đang thực hiện Prompt 01. |
| Branch thực hiện tác vụ | `docs/customer-screen-specification`. |
| Branch ghi nhật ký | `docs/agent-log-0235`. |
| Tác vụ | Bổ sung cách chia thay đổi và thực hiện Git trong quá trình viết tài liệu. |
| Phạm vi file lúc yêu cầu | `docs/ui-ux/customer-screen-specification.md`; không thêm file mới hoặc artifact kiểm tra vào repository. |
| Trạng thái | Đã áp dụng vào các phần nội dung được ghi sau yêu cầu. |

## Tóm tắt yêu cầu thực tế

Người dùng yêu cầu commit sau mỗi lần ghi một phần nội dung, đồng thời không tạo file mới hoặc làm phát sinh file không liên quan trong thư mục dự án. Đây là tóm tắt phạm vi và thao tác được yêu cầu, không phải lời nhắc nguyên văn.

Yêu cầu này điều chỉnh cách thực hiện tác vụ đang chạy; không phải một lần người dùng gửi thêm bản thảo màn hình để AI sửa.

## Phân chia phần việc

| Bên thực hiện | Phần việc thể hiện trong hội thoại |
|---|---|
| Người dùng | Bổ sung yêu cầu commit theo từng phần và giới hạn file/artifact được phép tạo trong repository. |
| AI | Chia nội dung thành các phần có ý nghĩa, kiểm tra diff, stage đúng file và tạo commit tương ứng. |

## Công việc và kiểm tra đã thực hiện

- Kiểm tra branch, working tree và staged diff trước khi stage; không đưa `.vscode/settings.json` có thay đổi sẵn vào commit tài liệu.
- Duy trì phạm vi một file đặc tả, không tạo log hoặc báo cáo phụ trong repository trong tác vụ đó.
- Dùng `/tmp` cho nội dung trích xuất và công cụ kiểm tra; không đưa artifact này vào Git.
- Chạy `git diff --check` và kiểm tra danh sách staged trước các commit.
- Khi sandbox không cho ghi Git index, dùng cơ chế quyền công cụ để thực hiện thao tác đã được người dùng yêu cầu; không đổi branch hoặc thay lịch sử đã có.

## Kết quả và bằng chứng

| Trường | Nội dung |
|---|---|
| File thay đổi riêng cho chỉ dẫn này | Không tạo một tài liệu sản phẩm mới; chỉ dẫn được áp dụng vào quá trình sửa file của Prompt 01. |
| Commit liên quan | Bảy commit của bản đặc tả đầu tiên: `d10e171`, `cb1b4a2`, `5ae945a`, `c87f9c1`, `a0dc80e`, `20c4897`, `456771c`. |
| Nội dung từng phần | Căn cứ/phạm vi; duyệt phim; chọn ghế; thanh toán; vé/xác thực/tài khoản; trạng thái/giá/contract; điểm cần review và nghiệm thu. |
| Kiểm tra đã chạy | Kiểm tra diff và staged file; xác minh cuối tác vụ chỉ file đặc tả nằm trong các commit, không có file mới chưa theo dõi. |
| Giới hạn kiểm tra | Thao tác Git và kiểm tra Markdown không chứng minh tính đúng đắn runtime của đặt vé/thanh toán. |
| Push | Không có thao tác push do AI thực hiện trong tác vụ này. |

## Ghi chú về diễn tiến yêu cầu

Các commit đầu tiên dùng subject tiếng Việt. Ở Prompt 03, người dùng mới bổ sung yêu cầu message ngắn bằng tiếng Anh; AI áp dụng cho commit mới, không sửa lại message hoặc lịch sử của các commit trước.

Giới hạn không tạo file mới ở prompt này thuộc tác vụ viết đặc tả. Yêu cầu tạo từng file nhật ký ở Prompt 05 là phạm vi mới được người dùng chỉ định sau đó.
