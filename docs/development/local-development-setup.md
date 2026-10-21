# Local Development Setup

## 1. Mục đích

Hướng dẫn thành viên cài đặt, cấu hình và chạy toàn bộ dự án trên máy cá nhân, để có thể chạy được theo hướng dẫn mà không phải hỏi lại từng bước.

## 2. Yêu cầu phần mềm

| Phần mềm | Yêu cầu tối thiểu | Ghi chú |
|---|---|---|
| .NET SDK | .NET 8 | Build backend ASP.NET Core |
| Node.js + npm | Node 20 LTS | Build frontend React + Vite |
| PostgreSQL | 16+ | Trình chạy local hoặc Docker |
| Docker Desktop | Tùy chọn | Dùng `docker-compose.yml` nếu không cài PostgreSQL local |
| Git | 2.40+ | Clone repository |

## 3. Clone repository

```bash
git clone <REPO_URL>
cd "Movie Theater Management and Online Ticket Booking System"
```

## 4. Cấu hình biến môi trường

1. Copy `.env.example` thành `.env` (root) và điền `DATABASE PASSWORD` của bạn.
2. Copy `src/frontend/cinema-ticket-booking-ui/.env.example` thành `.env` và điền URL backend.
3. Không commit file `.env` thật (đã có `.gitignore`).

## 5. Khởi chạy PostgreSQL

Cách 1 — Docker (khuyến nghị):

```bash
docker compose up -d
```

Cách 2 — PostgreSQL local: tạo database `postgres` theo connection string trong `.env` (Supabase Session Pooler hoặc local tùy cấu hình).

## 6. Chạy backend

```bash
cd src/backend/CinemaTicketBooking.Api
dotnet restore
dotnet run
```

- Cấu hình môi trường phát triển: `appsettings.Development.json`.
- Migration/seed: khi có migration EF Core, chạy `dotnet ef database update`. Trong giai đoạn script-first, chạy `database/database-schema.sql` rồi `database/seed-data.sql` (hướng dẫn chi tiết tại `database/seed-README.md`).
- Kiểm tra backend chạy: mở Swagger hoặc gọi `GET /api/movies`.

## 7. Chạy frontend

```bash
cd src/frontend/cinema-ticket-booking-ui
npm install
npm run dev
```

- Mở địa chỉ dev server in ra (mặc định `http://localhost:5173`).
- URL backend được đọc từ biến môi trường trong `.env` của frontend.

## 8. Kiểm tra hoạt động

1. Backend phản hồi endpoint công khai (VD danh sách phim).
2. Frontend tải trang Home và hiển thị dữ liệu từ backend.
3. Đăng nhập bằng tài khoản seed (nếu có) và thử luồng đặt vé: chọn phim → suất → ghế → booking giữ ghế 5 phút.
4. Dữ liệu seed xuất hiện đầy đủ (phim, phòng, ghế, suất chiếu).

## 9. Xử lý lỗi thường gặp

| Lỗi | Nguyên nhân thường gặp | Cách xử lý |
|---|---|---|
| Kết nối database thất bại | Sai mật khẩu/mật khẩu chưa điền trong `.env`, host sai | Đối chiếu `.env` với connection string; kiểm tra Docker container `docker ps` |
| Frontend không gọi được backend | Sai URL backend trong `.env` frontend, CORS | Kiểm tra URL; đảm bảo backend cho phép origin dev server |
| Migration/seed lỗi trùng dữ liệu | Chạy seed lần 2 không idempotent | Chạy lại từ `database/seed-README.md`; seed được thiết kế idempotent |
| Port bị chiếm | Backend 5000/7000 hoặc frontend 5173 đang được dùng | Đổi port trong cấu hình chạy; cập nhật `.env` tương ứng |
| `dotnet ef` không tìm thấy | Thiếu tool EF | `dotnet tool install --global dotnet-ef` |

## 10. Liên quan

- Hạ tầng chạy bằng Docker: `docker-compose.yml` (root).
- Mẫu biến môi trường: `.env.example` (root) và `.env.example` (frontend).
- Dữ liệu và schema: `database/` (`database-schema.sql`, `seed-data.sql`, `seed-README.md`).
