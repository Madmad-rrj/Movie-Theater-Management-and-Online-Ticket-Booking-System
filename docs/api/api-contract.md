# API Contract

## 1. Mục đích

Quy định chung về cách frontend và backend trao đổi dữ liệu. Tài liệu trả lời câu hỏi: "Các API phải tuân theo những quy tắc giao tiếp nào?".

## 2. Quy ước JSON

- Request/response dùng `Content-Type: application/json; charset=utf-8`.
- Tên trường dùng `camelCase` ở mức API (DTO), không phụ thuộc tên cột `snake_case` của database.
- Thời gian truyền theo UTC, định dạng ISO 8601 (ví dụ `2026-10-21T08:30:00Z`); frontend chuyển `Asia/Ho_Chi_Minh` khi hiển thị.
- Tiền tệ VND: truyền số nguyên đồng (integer) kèm `currency: "VND"`; không dùng số thập phân.
- Định danh công khai trong URL dùng mã (VD `bookingCode`, `ticketCode`) hoặc UUID; id nội bộ không bị lộ khi không cần.
- Trạng thái (status) truyền dưới dạng chuỗi trong tập giá trị đã quy định (VD `pending`, `confirmed`, `held`, `sold`); không dùng số.

## 3. Cấu trúc response

### 3.1. Thành công

```json
{
  "data": { },
  "pagination": { "page": 1, "pageSize": 20, "totalItems": 100, "totalPages": 5 }
}
```

- `data` là đối tượng hoặc mảng; `pagination` chỉ xuất hiện với danh sách phân trang.

### 3.2. Lỗi

```json
{
  "error": {
    "code": "SEAT_ALREADY_TAKEN",
    "message": "Ghế A5 vừa được người khác chọn",
    "details": [ ]
  }
}
```

- `code` là mã lỗi nghiệp vụ ổn định dùng để frontend phân nhánh; `message` là mô tả hiển thị được; `details` chứa thông tin bổ sung (VD danh sách trường vi phạm validation).
- Validation error: HTTP 400/422, `error.code = "VALIDATION_ERROR"`, `details` là mảng `{ field, message }`.

## 4. Pagination

- Tham số: `page` (bắt đầu 1), `pageSize` (mặc định 20, tối đa 100).
- Response gồm `pagination` theo cấu trúc 3.1; danh sách rỗng trả mảng `[]` với `totalItems: 0`, không coi là lỗi.

## 5. Validation

- Backend luôn kiểm tra lại mọi dữ liệu; client validation chỉ là trải nghiệm, không phải bảo mật.
- Client không gửi: giá (`unitPrice`, `totalAmount`), mức giảm (`discountPercent`), trạng thái (`status`) — các trường này do server quyết định.
- Vi phạm miền giá trị/trạng thái: HTTP 422 với mã `VALIDATION_ERROR` hoặc mã nghiệp vụ tương ứng.

## 6. Authentication & Authorization

- Xác thực: đăng nhập trả token phiên; các request cần xác thực gửi token trong header `Authorization: Bearer <token>`.
- Token hết hạn: HTTP 401 với `error.code = "UNAUTHENTICATED"`; frontend chuyển hướng đăng nhập và quay lại sau thành công.
- Ủy quyền: backend kiểm tra role và ownership; thiếu quyền: HTTP 403 với `error.code = "FORBIDDEN"`.
- Đăng ký công khai chỉ tạo role `CUSTOMER`; role quản trị do backend cấp.
- Authorization không dựa vào việc biết mã công khai (VD `bookingCode`, `ticketCode`): biết mã không đủ quyền truy cập, backend vẫn kiểm tra ownership/role.

## 7. Idempotency

- Tạo booking: bắt buộc `requestKey` (bắt đầu giữ idempotency) trong body; lặp cùng `requestKey` với cùng dữ liệu trả về booking cũ (200), khác dữ liệu → HTTP 409 `REQUEST_KEY_CONFLICT`.
- Callback thanh toán: nhận diện lặp qua `eventKey`; callback lặp trả 200 và không phát hành vé/applied lần hai.
- Soát vé: chỉ chuyển `valid` → `used`; lặp callback yêu cầu soát vé trả 409 `TICKET_ALREADY_USED`.

## 8. Quy tắc đặt tên và mã lỗi

- Mã lỗi nghiệp vụ dùng `SCREAMING_SNAKE_CASE`, ổn định và được liệt kê trong tài liệu (VD `SEAT_ALREADY_TAKEN`, `BOOKING_EXPIRED`, `PAYMENT_ALREADY_APPLIED`).
- Endpoint dùng danh từ số nhiều (`/api/movies`); thao tác dùng sub-resource hoặc động từ nêu rõ (`/cancel`, `/validate`).
- Nhiều mã lỗi ở mức **Proposed**; cần nhóm chốt danh sách cuối cùng trước khi triển khai.

## 9. Mối liên hệ

- Danh mục endpoint: `api-endpoints.md`.
- Chi tiết DTO (request/response từng endpoint): `dto-specification.md`.
- Dữ liệu từng màn hình cần: `docs/ui-ux/customer-screen-api-mapping.md`.
- Giá và tổng tiền: luôn lấy từ API (đã snapshot khi tạo booking), frontend không tự tính.
