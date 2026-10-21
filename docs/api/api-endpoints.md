# API Endpoints

## 1. Mục đích

Danh mục các REST API của hệ thống, được tổ chức theo nhóm nghiệp vụ. Tài liệu trả lời câu hỏi: "Hệ thống có những API nào?".

Quy ước giao tiếp chung (JSON, cấu trúc lỗi, pagination, auth, định dạng dữ liệu) được định nghĩa tại `api-contract.md`; DTO chi tiết tại `dto-specification.md`. Trạng thái: nhiều endpoint và DTO dưới đây ở mức **Proposed**, cần đối chiếu với `docs/api/api-contract.md` và quyết định của nhóm trước khi triển khai.

## 2. Auth

| Method | URL | Mục đích | Quyền | Status code chính |
|---|---|---|---|---|
| POST | `/api/auth/register` | Đăng ký tài khoản (role `CUSTOMER`) | Public | 201, 400, 409 |
| POST | `/api/auth/login` | Đăng nhập, trả token phiên | Public | 200, 401 |
| POST | `/api/auth/logout` | Đăng xuất | Authenticated | 204 |

## 3. Movies

| Method | URL | Mục đích | Quyền | Status code chính |
|---|---|---|---|---|
| GET | `/api/movies` | Danh sách phim (phân trang, lọc `status`) | Public | 200 |
| GET | `/api/movies/{movieId}` | Chi tiết phim | Public | 200, 404 |
| GET | `/api/movies/{movieId}/showtimes` | Suất chiếu của phim theo ngày | Public | 200, 404 |

## 4. Showtimes

| Method | URL | Mục đích | Quyền | Status code chính |
|---|---|---|---|---|
| GET | `/api/showtimes/{showtimeId}` | Chi tiết suất chiếu | Public | 200, 404 |
| GET | `/api/showtimes/{showtimeId}/seats` | Sơ đồ ghế và trạng thái phân bổ | Public | 200, 404 |

## 5. Bookings

| Method | URL | Mục đích | Quyền | Status code chính |
|---|---|---|---|---|
| POST | `/api/bookings` | Tạo booking giữ ghế (idempotent qua `requestKey`) | Authenticated | 201, 400, 409 (ghế bị chọn), 422 |
| GET | `/api/bookings/{bookingId}` | Chi tiết booking (chủ booking hoặc staff/manager) | Authenticated | 200, 403, 404 |
| POST | `/api/bookings/{bookingId}/cancel` | Hủy booking `pending` | Chủ booking | 200, 409 |
| GET | `/api/users/me/bookings` | Danh sách booking của user hiện tại | Authenticated | 200 |

## 6. Payments

| Method | URL | Mục đích | Quyền | Status code chính |
|---|---|---|---|---|
| POST | `/api/payments` | Khởi tạo payment attempt cho booking | Chủ booking | 201, 400, 409 (attempt đang mở) |
| GET | `/api/payments/{paymentId}` | Trạng thái payment attempt | Chủ booking / staff | 200, 403, 404 |
| POST | `/api/payments/callback` | Webhook/callback cổng thanh toán | Cổng (ký số) | 200, 400 |

## 7. Tickets

| Method | URL | Mục đích | Quyền | Status code chính |
|---|---|---|---|---|
| GET | `/api/tickets/{ticketCode}` | Tra cứu vé theo mã (chủ booking/staff) | Authenticated | 200, 403, 404 |
| POST | `/api/tickets/{ticketCode}/validate` | Soát vé (chuyển `valid` → `used`) | Staff/Manager | 200, 409 (đã dùng), 422 |

## 8. Quản trị (danh mục và suất chiếu)

| Method | URL | Mục đích | Quyền | Status code chính |
|---|---|---|---|---|
| GET | `/api/genres` | Danh sách thể loại | Public | 200 |
| GET | `/api/cinemas` | Danh sách chi nhánh | Public | 200 |
| GET | `/api/cinemas/{cinemaId}/rooms` | Danh sách phòng | Public | 200, 404 |
| GET | `/api/seat-types` | Danh sách loại ghế | Public | 200 |
| GET | `/api/ticket-types` | Danh sách loại vé | Public | 200 |
| POST | `/api/manage/movies` | Tạo phim | Manager/Admin | 201, 400 |
| PUT | `/api/manage/movies/{movieId}` | Cập nhật phim | Manager/Admin | 200, 404 |
| POST | `/api/manage/showtimes` | Tạo suất chiếu | Manager/Admin | 201, 400, 409 (chồng lịch phòng) |
| PUT | `/api/manage/showtimes/{showtimeId}` | Sửa suất (chỉ khi chưa có booking) | Manager/Admin | 200, 409 |
| POST | `/api/manage/showtimes/{showtimeId}/cancel` | Hủy suất chiếu | Manager/Admin | 200, 409 |
| DELETE | `/api/manage/movies/{movieId}` | Xóa/soft-delete phim | Admin | 204, 409 (đang được dùng) |

## 9. Lưu ý

- Endpoint và tham số ở trên mang tính **Proposed**; chi tiết request/response (body, DTO, mã lỗi nghiệp vụ) theo `dto-specification.md` và `api-contract.md`.
- Chỉ server mới được quyết định giá và điều kiện áp dụng loại vé; client không gửi giá.
- Mọi endpoint cần xác thực yêu cầu token phiên; authorization (role/ownership) được kiểm tra ở backend.
