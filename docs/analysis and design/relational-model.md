# Relational Model

## 1. Overview

<!--
Mô tả mục đích của relational model.

- Mô hình quan hệ dùng để làm gì?
- Phạm vi dữ liệu được mô hình hóa.
- Mối liên hệ giữa relational model, database-design.md và database-schema.md.
-->

## 2. List of Relations

<!--
Liệt kê tất cả các relation (bảng logic) trong hệ thống.

Với mỗi relation, ghi:
- Tên relation.
- Mục đích sử dụng.
- Entity tương ứng trong database design.

Đảm bảo các relation phù hợp với yêu cầu nghiệp vụ và thiết kế entity.
-->

## 3. Relational Schema

<!--
Biểu diễn schema logic của từng relation theo dạng:

RelationName(Attribute1, Attribute2, ..., AttributeN)

Với mỗi relation, xác định:
- Danh sách thuộc tính.
- Primary Key (PK).
- Foreign Key (FK).
- Candidate Key (nếu có).
- Composite Key (nếu có).

Ví dụ:
Movie(MovieID, Title, Duration, Status)

PK: MovieID

Không cần trình bày chi tiết kiểu dữ liệu SQL ở phần này.
-->

## 4. Primary Keys

<!--
Tổng hợp Primary Key của các relation.

Với mỗi relation, xác định:
- Thuộc tính hoặc nhóm thuộc tính làm PK.
- PK đơn hay PK ghép.
- Lý do lựa chọn PK.
- Đảm bảo tính duy nhất và không NULL.
-->

## 5. Foreign Keys and Referential Integrity

<!--
Tổng hợp các Foreign Key quan trọng.

Với mỗi FK, xác định:
- Relation chứa FK.
- Thuộc tính FK.
- Relation và thuộc tính được tham chiếu.
- Mục đích của mối liên kết.
- Quy tắc tham chiếu và xử lý khi cập nhật hoặc xóa dữ liệu.
-->

## 6. Relationships and Cardinality

<!--
Mô tả quan hệ giữa các entity/relation.

Với mỗi quan hệ, xác định:
- Hai entity/relation tham gia.
- Loại quan hệ: 1:1, 1:N hoặc M:N.
- Ý nghĩa nghiệp vụ của quan hệ.
- Cách biểu diễn quan hệ trong relational model.

Đối với quan hệ M:N, xác định relation trung gian dùng để biểu diễn quan hệ đó.
-->

## 7. Candidate Keys and Uniqueness

<!--
Xác định các Candidate Key và quy tắc duy nhất ngoài PK nếu có.

Với mỗi trường hợp, ghi:
- Relation liên quan.
- Thuộc tính hoặc tổ hợp thuộc tính cần duy nhất.
- Ý nghĩa nghiệp vụ.
- Lý do cần đảm bảo tính duy nhất.

Ví dụ: Email của User hoặc mã vé của Ticket, nếu yêu cầu nghiệp vụ quy định duy nhất.
-->

## 8. Normalization

<!--
Mô tả việc chuẩn hóa relational model.

- First Normal Form (1NF).
- Second Normal Form (2NF).
- Third Normal Form (3NF).
- Các trường hợp phụ thuộc hàm đáng chú ý.
- Những quyết định phi chuẩn hóa (denormalization), nếu có, và lý do.

Giải thích cách mô hình hạn chế dữ liệu dư thừa và các bất thường khi thêm, sửa hoặc xóa dữ liệu.
-->

## 9. Integrity Constraints

<!--
Tổng hợp các quy tắc toàn vẹn dữ liệu ở mức logic.

Có thể bao gồm:
- Entity integrity: PK phải duy nhất và không NULL.
- Referential integrity: FK phải tham chiếu hợp lệ.
- Domain integrity: thuộc tính phải nhận giá trị hợp lệ.
- Business rules: các quy tắc nghiệp vụ liên quan đến nhiều relation.

Phân biệt các quy tắc có thể đảm bảo bằng khóa/ràng buộc dữ liệu với các quy tắc cần xử lý bằng nghiệp vụ hoặc transaction.
-->

## 10. Assumptions and Design Decisions

<!--
Ghi lại các giả định và quyết định ảnh hưởng đến relational model.

Ví dụ:
- Lựa chọn PK đơn hay PK ghép.
- Cách biểu diễn quan hệ M:N.
- Quy tắc duy nhất của các thuộc tính.
- Cách lưu trữ dữ liệu lịch sử.
- Các vấn đề chưa được thống nhất cần xác nhận.

Không ghi quyết định chưa được nhóm thống nhất như thể đó là thiết kế chính thức.
-->

## 11. References

<!--
Liệt kê các tài liệu liên quan được dùng làm cơ sở xây dựng relational model.

Ví dụ:
- Requirements analysis.
- Use cases.
- Database design.
- Database schema.
- Database schema SQL.
-->