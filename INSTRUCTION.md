# PROMETHE-PHOS — Hướng dẫn chạy toàn bộ hệ thống

Tài liệu này hướng dẫn chạy hệ thống từ PostgreSQL, ASP.NET Core API, React frontend đến quy trình đặt vé và thanh toán MockPay.

## 1. Yêu cầu môi trường

Cần cài đặt:

- Docker Desktop.
- .NET SDK tương thích với project.
- Node.js và npm.
- Merchant API key hợp lệ của MockPay.

Mở PowerShell tại thư mục repository:

```powershell
Set-Location 'D:\HCMUS\Nhập môn cnpm\Movie Theater Management and Online Ticket Booking System'
```

Kiểm tra công cụ:

```powershell
dotnet --version
node --version
npm --version
docker version
```

## 2. Cấu hình PostgreSQL

Nếu chưa có file `.env` ở thư mục gốc:

```powershell
Copy-Item '.env.example' '.env'
```

Các biến local cần có trong `.env`:

```env
POSTGRES_DB=cinema_demo
POSTGRES_USER=cinema_demo
POSTGRES_PASSWORD=cinema_demo_local_only
POSTGRES_HOST_PORT=5433

MockPay__BaseUrl=https://promethe-mock-payment.promethe-phos.workers.dev
MockPay__Provider=MockPay
MockPay__MerchantApiKey=<MOCKPAY_MERCHANT_API_KEY>
```

Không commit `.env`, mật khẩu database hoặc merchant key vào Git.

Khởi động PostgreSQL:

```powershell
docker compose up -d postgres
docker compose ps
```

PostgreSQL mặc định chạy tại:

```text
127.0.0.1:5433
```

Nếu port `5433` bị chiếm, đổi `POSTGRES_HOST_PORT` trong `.env` và dùng port mới trong connection string của backend.

## 3. Chạy Backend

Mở một PowerShell riêng:

```powershell
Set-Location 'D:\HCMUS\Nhập môn cnpm\Movie Theater Management and Online Ticket Booking System'
```

ASP.NET Core không tự đọc file `.env`. Nạp các biến cấu hình vào process hiện tại:

```powershell
$config = Get-Content '.env' |
  Where-Object { $_ -match '^[^#].*=' } |
  ConvertFrom-StringData

$env:ConnectionStrings__DefaultConnection = `
  "Host=127.0.0.1;Port=$($config.POSTGRES_HOST_PORT);Database=$($config.POSTGRES_DB);Username=$($config.POSTGRES_USER);Password=$($config.POSTGRES_PASSWORD)"

$env:MockPay__BaseUrl = $config.MockPay__BaseUrl
$env:MockPay__Provider = $config.MockPay__Provider
$env:MockPay__MerchantApiKey = $config.MockPay__MerchantApiKey
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5080'
```

Restore và chạy API:

```powershell
dotnet restore '.\src\backend\CinemaTicketBooking.Api\CinemaTicketBooking.Api.csproj'

dotnet run `
  --project '.\src\backend\CinemaTicketBooking.Api\CinemaTicketBooking.Api.csproj' `
  --no-launch-profile `
  --urls 'http://127.0.0.1:5080'
```

Giữ cửa sổ này mở.

Kiểm tra API:

```powershell
Invoke-RestMethod 'http://127.0.0.1:5080/api/movies'
```

Nếu response có trường `data`, backend đã sẵn sàng.

## 4. Chạy Frontend

Mở PowerShell thứ hai:

```powershell
Set-Location 'D:\HCMUS\Nhập môn cnpm\Movie Theater Management and Online Ticket Booking System\src\frontend\cinema-ticket-booking-ui'
```

Cài dependency nếu cần:

```powershell
npm install
```

Chạy Vite:

```powershell
npm run dev -- --host 127.0.0.1
```

Mở trình duyệt tại:

```text
http://127.0.0.1:5173
```

Vite proxy chuyển các request:

```text
Frontend /api/* → http://127.0.0.1:5080/api/*
```

Nên dùng `127.0.0.1` thay vì `localhost` vì backend Development hiện cho phép origin:

```text
http://127.0.0.1:5173
```

## 5. Kiểm tra dữ liệu phim và suất chiếu

Lấy danh sách phim:

```powershell
$movies = Invoke-RestMethod 'http://127.0.0.1:5080/api/movies'
$movies.data | Select-Object movieId, title, status
```

Dữ liệu seed mặc định bắt đầu từ ngày `2026-11-14`, vì vậy ngày `2026-11-13` có thể không có suất chiếu.

Kiểm tra suất chiếu của một phim:

```powershell
Invoke-RestMethod `
  'http://127.0.0.1:5080/api/movies/<MOVIE_ID>/showtimes?date=2026-11-14&page=1&pageSize=100'
```

Trong frontend, hãy chọn ngày `14/11/2026` hoặc một ngày tiếp theo có dữ liệu.

## 6. Quy trình đặt vé đầy đủ

1. Mở trang chủ.
2. Chọn phim đang chiếu.
3. Nhấn **Đặt vé**.
4. Chọn rạp.
5. Chọn ngày có suất chiếu.
6. Chọn suất chiếu.
7. Chọn loại vé:
   - Người lớn.
   - Học sinh/sinh viên.
8. Chọn số ghế đúng bằng tổng số vé.
9. Nhấn tiếp tục.
10. Nhập họ tên, số điện thoại và email.
11. Chấp nhận điều khoản.
12. Tạo booking pending.
13. Backend khởi tạo payment MockPay.
14. Frontend hiển thị QR từ `redirectUrl` do backend trả về.
15. Quét QR hoặc mở payment URL trên thiết bị.
16. Hoàn tất thanh toán mô phỏng trên MockPay.
17. Frontend polling `GET /api/payments/{paymentId}`.
18. Khi payment là `succeeded` và booking là `confirmed`, trang xác nhận booking được hiển thị.

Frontend không gọi trực tiếp merchant API và không chứa merchant API key.

## 7. Các endpoint chính

### Public data

```text
GET /api/movies
GET /api/movies/{movieId}
GET /api/cinemas
GET /api/movies/{movieId}/showtimes?date=YYYY-MM-DD
GET /api/showtimes/{showtimeId}
GET /api/showtimes/{showtimeId}/seats
GET /api/ticket-categories
GET /api/cinemas/{cinemaId}/ticket-pricing
```

### Guest checkout

```text
POST /api/guest-context
POST /api/selections
PUT /api/selections/{selectionId}
POST /api/bookings
GET /api/bookings/{bookingId}
POST /api/bookings/{bookingId}/payments
GET /api/payments/{paymentId}
```

Các mutation request cần cookie guest, CSRF token và `Idempotency-Key` theo hợp đồng backend.

## 8. Xử lý lỗi thường gặp

### Thiếu connection string

Lỗi:

```text
Missing ConnectionStrings__DefaultConnection
```

Nguyên nhân là biến môi trường chưa được đặt trong cửa sổ PowerShell chạy backend. Thực hiện lại phần cấu hình ở mục 3.

### Không có suất chiếu

Kiểm tra:

```powershell
docker compose ps
Invoke-RestMethod 'http://127.0.0.1:5080/api/cinemas'
```

Chọn ngày từ `2026-11-14`.

Seed script chỉ chạy tự động khi PostgreSQL volume mới được tạo. Không tự ý xóa volume nếu database có dữ liệu cần giữ.

### MockPay provider error

Lỗi:

```text
[PAYMENT_PROVIDER_ERROR] MockPay không thể hoàn tất yêu cầu.
```

Kiểm tra merchant key đã được truyền vào backend:

```powershell
if ([string]::IsNullOrWhiteSpace($env:MockPay__MerchantApiKey)) {
  Write-Output 'MockPay key is missing'
} else {
  Write-Output 'MockPay key is configured'
}
```

Nếu key bị thiếu hoặc không hợp lệ:

1. Dừng backend bằng `Ctrl+C`.
2. Nạp lại `MockPay__MerchantApiKey` từ secret store hoặc `.env` local.
3. Khởi động backend lại.
4. Tạo booking mới rồi thử thanh toán lại.

Không tiếp tục dùng payment cũ được tạo khi backend chưa có merchant key.

### Trang payment bị loading vô hạn

1. Kiểm tra Vite đã chạy đúng port `5173`.
2. Dùng `Ctrl + F5`.
3. Kiểm tra backend vẫn chạy ở port `5080`.
4. Nếu booking cũ hết hạn, tạo booking mới.

## 9. Kiểm tra build frontend

```powershell
Set-Location 'src\frontend\cinema-ticket-booking-ui'

npm run lint
npm run build
```

## 10. Dừng hệ thống

Trong cửa sổ Backend và Frontend:

```text
Ctrl+C
```

Dừng PostgreSQL nhưng giữ nguyên dữ liệu:

```powershell
docker compose stop postgres
```

Khởi động lại PostgreSQL:

```powershell
docker compose start postgres
```

