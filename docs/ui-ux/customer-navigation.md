# Customer Navigation

## 1. Mục đích và luồng

Tài liệu xác định navigation cho Guest và Customer. Luồng nghiệp vụ là:

```text
Home → Movie List/Movie Detail → Showtime → Seat Selection
  → Ticket Type → Checkout Information → Payment
  → Payment Result → My Tickets
```

Selection session bắt đầu khi chọn ghế đầu tiên và có thời hạn gốc 5 phút. Sau khi tạo booking `pending`, payment attempt có cửa sổ riêng 10 phút. Không thời hạn nào được reset bởi Back/Forward, refresh, redirect hoặc webhook retry.

## 2. Nguyên tắc

- Home, Movie List, Movie Detail, Showtime, Seat Selection, Ticket Type và Checkout Information là public.
- Guest Checkout không yêu cầu đăng ký hoặc đăng nhập. Guest dùng guest-safe session và cơ chế xác minh booking khi đọc dữ liệu sau đó.
- Payment, Payment Result và Booking Detail không phải dữ liệu công khai: Customer phải là chủ booking; Guest phải qua guest-safe verification.
- Account, My Tickets và Booking History là vùng của Customer đã đăng nhập. Guest không có danh sách tài khoản.
- Frontend không coi route guard là bảo mật; backend luôn kiểm tra authentication, authorization, ownership và trạng thái authoritative.
- ID trên route chỉ định vị tài nguyên; refresh/Back/Forward không tự gửi lại POST booking/payment.
- Không đặt booking payload, token, secret payment hoặc dữ liệu private vào URL.

## 3. Header và menu

| Thành phần | Guest | Customer | Đích/hành vi |
|---|---|---|---|
| Logo/Home | Hiển thị | Hiển thị | `/` |
| Phim đang chiếu | Hiển thị | Hiển thị | `/movies?status=now_showing` |
| Phim sắp chiếu | Hiển thị | Hiển thị | `/movies?status=coming_soon` |
| Đăng nhập/Đăng ký | Hiển thị | Ẩn hoặc trong menu | Login/Register |
| Menu tài khoản | Ẩn | Hiển thị | Account, My Tickets, Booking History, Logout |

Rời luồng khi booking `pending` không tự hủy đơn hoặc nhả ghế. Sau logout, xóa dữ liệu private khỏi UI; không coi logout là cancellation.

## 4. Route

| Màn hình | Route | Quyền | Context |
|---|---|---|---|
| Home | `/` | Public | — |
| Movie List | `/movies?status={status}&page={page}` | Public | `status`, `page` |
| Movie Detail | `/movies/:movieId` | Public | `movieId` |
| Showtime | `/movies/:movieId/showtimes?date={date}` | Public | `movieId`, ngày |
| Seat Selection | `/showtimes/:showtimeId/seats` | Public | `showtimeId` |
| Ticket Type | `/booking/ticket-types` | Public | draft ghế/số lượng |
| Checkout Information | `/booking/checkout` | Public | draft + contact + selection session |
| Payment | `/bookings/:bookingId/payment` | Guest/Customer đã xác minh | `bookingId` |
| Payment Result | `/bookings/:bookingId/payment-result` | Guest/Customer đã xác minh | `bookingId` |
| Account | `/account` | Customer | current user |
| My Tickets | `/account/tickets` | Customer | current user, `bookingId` tùy chọn |
| Booking History | `/account/bookings` | Customer | current user, phân trang |
| Booking Detail | `/account/bookings/:bookingId` | Customer/Guest đã xác minh | `bookingId` |
| Login/Register | `/login`, `/register` | Public | internal `returnUrl` tùy chọn |

## 5. Chuyển màn hình

| Từ | Hành động | Đến | Dữ liệu giữ lại |
|---|---|---|---|
| Home/Movie List | Chọn phim | Movie Detail | `movieId`, filter |
| Movie Detail | Chọn suất chiếu | Showtime | `movieId` |
| Showtime | Chọn suất còn bán | Seat Selection | `movieId`, `showtimeId` |
| Seat Selection | Chọn ghế | Ticket Type | `selectedSeatIds` draft |
| Ticket Type | Chọn số lượng vé hợp lệ | Checkout Information | ghế, quantity, selection session |
| Checkout Information | Xác nhận | Payment | `bookingId`, `expiresAt` |
| Payment | Tạo/theo dõi attempt | Payment Result | `bookingId`, `paymentId` |
| Payment Result | Đã applied/confirmed/ticket issued | My Tickets | `bookingId` |
| Payment Result | Pending/unknown/failed chưa chốt | Payment hoặc trạng thái đang xác minh | `bookingId` |

## 6. Guard, deep link và lỗi

- Guest deep link Payment/Result/Detail phải qua guest verification; Customer chưa đăng nhập đi qua Login với `returnUrl` nội bộ.
- `401` trên vùng Customer → Login; `403` → Access Denied; `404` → Not Found. Không lấy cache private để che lỗi.
- Hết selection session trước khi tạo booking → Seat Selection/Showtime; không có booking để hủy.
- Booking expired/cancelled → không quay lại Payment để dùng lại booking cũ.
- Payment `pending`/`unknown` → Payment Result hiển thị đang xác minh, không tạo attempt song song.
- Payment thành công và applied → My Tickets; `succeeded` nhưng chưa applied → không hiển thị vé/QR thành công.
- Back/Forward/refresh không resubmit mutation; timeout POST phải giữ request key và đọc lại trạng thái.

## 7. Tài liệu liên quan

- `customer-user-flow.md`
- `customer-screen-specification.md`
- `customer-ui-state.md`
- `customer-screen-api-mapping.md`
- `../api/api-contract.md`
