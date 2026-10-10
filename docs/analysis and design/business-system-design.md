# Business System Design

## 1. Xây dựng Use Case

<!--
Dựa trên Actor và chức năng đã xác định trong Business System Analysis
để xây dựng Use Case Diagram.

Cần:

- Xác định Actor.
- Xác định Use Case.
- Xác định quan hệ Actor - Use Case.
- Xác định include / extend nếu cần.
- Phân chia khu vực Customer và Admin/Staff nếu cần.

Sau Diagram, nên có bảng mô tả các Use Case quan trọng:

- Use Case Name.
- Actor.
- Preconditions.
- Main Flow.
- Alternative Flow.
- Exception Flow.
- Postconditions.
-->

## 2. Xây dựng Activity Diagram

<!--
Mô tả luồng xử lý của các nghiệp vụ quan trọng dưới dạng Activity Diagram.

Không cần vẽ mọi chức năng nếu không cần thiết.

Ưu tiên các nghiệp vụ có nhiều bước hoặc nhiều nhánh.

Ví dụ:

- Customer Booking.
- Payment.
- Showtime Creation.
- Ticket Validation.

Diagram cần thể hiện:

- Start.
- Activity.
- Decision.
- Branch / Merge.
- End.
- Các luồng xử lý chính và ngoại lệ.
-->

## 3. Xây dựng Sequence Diagram

<!--
Mô tả thứ tự tương tác giữa các Actor và các thành phần hệ thống
trong một nghiệp vụ cụ thể.

Cần xác định:

- Actor.
- Frontend.
- Backend/API.
- Service/Business Logic.
- Database.
- External System nếu có.

Ưu tiên xây dựng Sequence Diagram cho các flow quan trọng:

1. Authentication
2. Booking
3. Payment
4. Ticket Generation

Diagram phải thể hiện được:

- Request.
- Response.
- Thứ tự xử lý.
- Điều kiện / nhánh lỗi nếu có.
-->

## 4. Thiết kế kiến trúc hệ thống

<!--
Xác định kiến trúc tổng thể của hệ thống và cách các thành phần
giao tiếp với nhau.

Cần xác định:

- Frontend sử dụng công nghệ gì.
- Backend sử dụng công nghệ gì.
- Database sử dụng gì.
- Các layer/module chính của Backend.
- Cách Frontend giao tiếp với Backend.
- Cách Backend giao tiếp với Database.
- Các hệ thống bên ngoài như Payment Gateway / AI Service nếu có.

Kiến trúc dự kiến:

Frontend
    ↓
ASP.NET Core Web API
    ↓
Business Logic / Services
    ↓
EF Core
    ↓
PostgreSQL

Nếu có hệ thống bên ngoài:

ASP.NET Core API
    ├── Payment Gateway
    └── AI Service

Kết quả của phần này phải đủ rõ để làm cơ sở cho:

- Database Design.
- API Contract.
- Frontend Foundation.
- Backend Architecture.
-->