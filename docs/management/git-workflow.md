# Git Workflow

Tài liệu quy định cách nhóm sử dụng Git trong quá trình phát triển đồ án.

Mục tiêu là giúp mã nguồn được quản lý thống nhất và thể hiện được quá trình phát triển của đồ án.

## 1. Branch

Sử dụng `main` làm branch chính.

Không commit trực tiếp vào `main`. Mỗi thành viên tạo branch riêng để phát triển chức năng.

Cấu trúc:

```text
main
├── feature/...
├── fix/...
└── docs/...
```

### Đặt tên branch

**Chức năng mới:**

```text
feature/ten-chuc-nang
```

Ví dụ:

```text
feature/login
feature/product-management
feature/order-management
```

**Sửa lỗi:**

```text
fix/ten-loi
```

Ví dụ:

```text
fix/login-error
```

**Tài liệu:**

```text
docs/ten-tai-lieu
```

Ví dụ:

```text
docs/update-readme
```

Tên branch viết bằng tiếng anh, không dấu và dùng `-` để phân cách các từ.

---

## 2. Quy trình làm một chức năng

Mỗi chức năng thực hiện theo quy trình:

```text
Cập nhật main
    ↓
Tạo branch
    ↓
Code
    ↓
Commit
    ↓
Push
    ↓
Pull Request
    ↓
Review
    ↓
Merge vào main
```

Ví dụ:

```bash
git checkout main
git pull origin main

git checkout -b feature/dang-nhap
```

Sau khi hoàn thành:

```bash
git add .
git commit -m "feat: thêm chức năng đăng nhập"
git push -u origin feature/login
```

Sau đó tạo Pull Request:

```text
feature/login → main
```

---

## 3. Commit

Commit phải mô tả ngắn gọn thay đổi được thực hiện.

Sử dụng các prefix đơn giản:

```text
feat:     thêm chức năng
fix:      sửa lỗi
docs:     cập nhật tài liệu
refactor: chỉnh sửa cấu trúc code
```

### Ví dụ

```text
feat: thêm chức năng đăng nhập
feat: thêm quản lý sản phẩm
feat: thêm chức năng tạo đơn hàng

fix: sửa lỗi kiểm tra mật khẩu
fix: sửa lỗi tính tổng tiền

docs: cập nhật README

refactor: chỉnh sửa ProductService
```

Không nên sử dụng các commit message quá chung chung:

```text
update
fix
test
code
done
final
final2
```

Một commit nên tập trung vào **một thay đổi chính**.

Không cần cố tạo thật nhiều commit. Quan trọng là lịch sử commit thể hiện được quá trình phát triển.

---

## 4. Pull Request

Mỗi chức năng nên tạo một Pull Request riêng.

Ví dụ:

```text
feature/quan-ly-san-pham
        ↓
Pull Request
        ↓
main
```

Trước khi merge, nhóm trưởng sẽ kiểm tra:

* Code có chạy được không.
* Chức năng có hoạt động không.
* Có lỗi rõ ràng không.
* Có commit nhầm thông tin nhạy cảm không.
* Code có đúng quy ước chung không.

Sau khi kiểm tra, Pull Request có thể được merge vào `main`.

---

## 5. Cập nhật code trước khi làm việc

Trước khi bắt đầu chức năng mới:

```bash
git checkout main
git pull origin main
```

Sau đó tạo branch mới:

```bash
git checkout -b feature/ten-chuc-nang
```

Không nên tiếp tục phát triển trên branch đã hoàn thành chức năng trước đó.

---

## 6. Xử lý Conflict

Khi xảy ra conflict:

1. Kiểm tra phần code bị conflict.
2. Trao đổi với thành viên liên quan nếu cần.
3. Giữ lại phần code đúng.
4. Chạy lại chương trình để kiểm tra.
5. Commit và push phần đã giải quyết.

Không tự ý xóa thay đổi của thành viên khác nếu chưa kiểm tra.

---

## 7. Không commit thông tin nhạy cảm

Không commit:

* Password.
* API key.
* Secret key.
* Token.
* Connection string có thông tin đăng nhập.

Các file cấu hình cá nhân phải được thêm vào `.gitignore`.

File `pat.txt` dùng để nộp Moodle **không được đưa lên repository**.

---

## 8. Trước khi tạo Pull Request

Kiểm tra nhanh:

```text
[ ] Code chạy được
[ ] Chức năng đã được kiểm tra
[ ] Không còn code debug
[ ] Không có password/API key/secret
[ ] Commit message rõ ràng
[ ] Không có thay đổi không liên quan
```
**Mọi thành viên cần tuân thủ cùng một quy trình và repository thể hiện được quá trình phát triển thực tế của đồ án**.
