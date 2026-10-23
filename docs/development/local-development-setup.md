# Local Development Setup

> Tài liệu này mô tả cách cài đặt, cấu hình và chạy toàn bộ dự án trên máy cá nhân.
> Nó bao gồm: PostgreSQL (CIN-SUB-222), Backend ASP.NET Core (CIN-SUB-223),
> Frontend React + Vite (CIN-SUB-224), Docker Compose (CIN-SUB-225),
> biến môi trường (CIN-SUB-226) và hướng dẫn tổng hợp (CIN-SUB-227).
>
> **Lưu ý trạng thái hiện tại (2026-10-23):** Backend và Frontend đang ở giai đoạn
> khung dự án ban đầu (placeholder files, chưa có logic). `docker-compose.yml` hiện
> còn trống. Tài liệu này mô tả thiết lập theo thiết kế dự kiến và ghi rõ phần nào
> chưa thể thực hiện cho đến khi code được triển khai.

## Mục lục

1. [Yêu cầu phần mềm](#1-yêu-cầu-phần-mềm)
2. [Clone repository](#2-clone-repository)
3. [Cấu hình biến môi trường](#3-cấu-hình-biến-môi-trường)
4. [PostgreSQL (CIN-SUB-222)](#4-postgresql-cin-sub-222)
5. [Backend ASP.NET Core (CIN-SUB-223)](#5-backend-aspnet-core-cin-sub-223)
6. [Frontend React + Vite (CIN-SUB-224)](#6-frontend-react--vite-cin-sub-224)
7. [Docker Compose (CIN-SUB-225)](#7-docker-compose-cin-sub-225)
8. [Biến môi trường chi tiết (CIN-SUB-226)](#8-biến-môi-trường-chi-tiết-cin-sub-226)
9. [Khởi động nhanh (CIN-SUB-227)](#9-khởi-động-nhanh-cin-sub-227)
10. [Xử lý lỗi thường gặp](#10-xử-lý-lỗi-thường-gặp)
11. [Liên quan](#11-liên-quan)

---

## 1. Yêu cầu phần mềm

| Phần mềm | Phiên bản tối thiểu | Mục đích | Bắt buộc |
|---|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | .NET 8 | Build và chạy backend ASP.NET Core | ✅ Bắt buộc |
| [Node.js](https://nodejs.org/) | 20 LTS | Build và chạy frontend React + Vite | ✅ Bắt buộc |
| npm | Đi kèm Node.js 20 | Quản lý dependency frontend | ✅ Bắt buộc |
| [Git](https://git-scm.com/) | 2.40+ | Clone và quản lý repository | ✅ Bắt buộc |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | 4.x | Chạy PostgreSQL (và các service) bằng container | Khuyến nghị (xem §4) |
| PostgreSQL | 16+ | Database; thay thế Docker nếu đã cài local | Một trong hai |
| [psql](https://www.postgresql.org/docs/current/app-psql.html) | Đi kèm PostgreSQL | Chạy script SQL (schema, seed) | Khuyến nghị |

> **Kiểm tra phiên bản trước khi bắt đầu:**
> ```bash
> dotnet --version     # Cần >= 8.x
> node --version       # Cần >= 20.x
> npm --version
> git --version
> docker --version     # Nếu dùng Docker
> ```

---

## 2. Clone repository

```bash
git clone <REPO_URL>
cd "Movie-Theater-Management-and-Online-Ticket-Booking-System"
```

> **Lưu ý tên thư mục:** Thư mục được clone có thể có dấu gạch ngang hoặc khoảng trắng tùy
> cách clone. Kiểm tra tên thực tế sau khi clone.

---

## 3. Cấu hình biến môi trường

### 3.1 File `.env` root (Backend connection string)

```bash
# Từ root repository
cp .env.example .env
```

Sau đó mở `.env` và điền thông tin kết nối PostgreSQL:

```ini
# .env (root) — KHÔNG commit file này
# Xem .env.example để biết format đầy đủ

# Kết nối Supabase (cloud):
ConnectionStrings__DefaultConnection=Host=<SUPABASE_HOST>;Port=5432;Database=postgres;Username=<USERNAME>;Password=<YOUR_PASSWORD>;SSL Mode=Require

# Hoặc kết nối PostgreSQL local / Docker (xem §4):
# ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=cinema_db;Username=cinema_user;Password=<LOCAL_PASSWORD>
```

> **Không commit file `.env` thật lên Git.** File `.gitignore` đã loại trừ `.env`.
> File `.env.example` (không chứa mật khẩu thật) mới được commit.

### 3.2 File `.env` frontend

```bash
# Từ root repository
cp src/frontend/cinema-ticket-booking-ui/.env.example \
   src/frontend/cinema-ticket-booking-ui/.env
```

Điền URL backend vào file `.env` của frontend:

```ini
# src/frontend/cinema-ticket-booking-ui/.env — KHÔNG commit
# Xem .env.example để biết key cụ thể
VITE_API_BASE_URL=http://localhost:5000
```

> **Lưu ý:** Key biến môi trường frontend Vite (`VITE_*`) sẽ được xác nhận khi
> `src/frontend/cinema-ticket-booking-ui/.env.example` được điền đầy đủ bởi team.
> Hiện tại file này đang trống (placeholder). Hỏi team để biết tên key chính xác trước
> khi điền.

---

## 4. PostgreSQL (CIN-SUB-222)

### 4.1 Lựa chọn môi trường PostgreSQL

Dự án hỗ trợ ba cách chạy PostgreSQL:

| Phương án | Mô tả | Phù hợp khi |
|---|---|---|
| **A — Supabase** (cloud) | Kết nối đến Supabase project của nhóm qua Session Pooler | Muốn dùng database chung của nhóm |
| **B — Docker local** | Chạy container `postgres:16` trên máy local | Muốn database độc lập, không phụ thuộc mạng |
| **C — PostgreSQL cài thẳng** | PostgreSQL cài trực tiếp trên máy | Đã cài PostgreSQL, muốn dùng luôn |

### 4.2 Phương án A — Supabase (cloud)

1. Lấy thông tin kết nối từ Supabase Dashboard → Project Settings → Database → Session Pooler (IPv4).
2. Điền vào `.env` root theo format ở §3.1.
3. Đảm bảo Supabase project đã enable extension `btree_gist` (cần cho constraint `ex_showtime_room_time`):
   ```sql
   -- Chạy trong Supabase SQL Editor hoặc qua psql
   CREATE EXTENSION IF NOT EXISTS btree_gist;
   ```

### 4.3 Phương án B — Docker local

> **Trạng thái:** `docker-compose.yml` hiện còn trống (placeholder). Khi team hoàn thiện
> file này, lệnh `docker compose up -d` sẽ khởi động PostgreSQL tự động. Dưới đây là
> cấu hình dự kiến để tham khảo.

Cấu hình dự kiến trong `docker-compose.yml` cho service PostgreSQL:

```yaml
# docker-compose.yml (cấu hình dự kiến — chưa chính thức)
services:
  postgres:
    image: postgres:16
    container_name: cinema_postgres
    environment:
      POSTGRES_DB: cinema_db
      POSTGRES_USER: cinema_user
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}   # đọc từ .env
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data  # dữ liệu bền vững
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U cinema_user -d cinema_db"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  postgres_data:
```

Khi `docker-compose.yml` được hoàn thiện, khởi động PostgreSQL bằng:

```bash
docker compose up -d postgres
```

Kiểm tra container đang chạy:
```bash
docker ps
docker compose logs postgres
```

Kết nối thử bằng psql:
```bash
psql -h localhost -p 5432 -U cinema_user -d cinema_db
```

Connection string tương ứng trong `.env`:
```ini
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=cinema_db;Username=cinema_user;Password=<LOCAL_PASSWORD>
```

> **Dữ liệu bền vững:** Volume `postgres_data` lưu dữ liệu ngay cả khi container bị
> xóa và khởi động lại. Để reset hoàn toàn: `docker compose down -v`.

### 4.4 Phương án C — PostgreSQL cài thẳng

1. Tạo database và user:
   ```sql
   CREATE DATABASE cinema_db;
   CREATE USER cinema_user WITH PASSWORD '<LOCAL_PASSWORD>';
   GRANT ALL PRIVILEGES ON DATABASE cinema_db TO cinema_user;
   ```
2. Bật extension:
   ```sql
   \c cinema_db
   CREATE EXTENSION IF NOT EXISTS btree_gist;
   ```
3. Điền connection string vào `.env` theo §4.3.

### 4.5 Khởi tạo schema và seed data

Sau khi PostgreSQL chạy và kết nối được, chạy theo thứ tự:

```bash
# Bước 1: Tạo schema (chạy từ root repository)
psql -h <HOST> -p <PORT> -U <USER> -d <DB> -v ON_ERROR_STOP=1 -f database/database-schema.sql

# Bước 2: Seed dữ liệu demo
psql -h <HOST> -p <PORT> -U <USER> -d <DB> -f database/seed-data.sql
```

Ví dụ với Docker local:
```bash
psql -h localhost -p 5432 -U cinema_user -d cinema_db -v ON_ERROR_STOP=1 -f database/database-schema.sql
psql -h localhost -p 5432 -U cinema_user -d cinema_db -f database/seed-data.sql
```

> Script `seed-data.sql` được thiết kế **idempotent** (chạy lại an toàn nhờ
> `ON CONFLICT DO NOTHING`), ngoại trừ showtimes — xem `database/seed-README.md`
> để biết chi tiết và lưu ý khi chạy lại.

---

## 5. Backend ASP.NET Core (CIN-SUB-223)

### 5.1 Trạng thái hiện tại

> Backend (`src/backend/CinemaTicketBooking.Api`) hiện là **khung dự án** (placeholder).
> Các file `Program.cs`, `CinemaTicketBooking.Api.csproj`, `appsettings.Development.json`
> và `Data/ApplicationDbContext.cs` đang trống. Chưa có Dockerfile riêng cho backend.
>
> Hướng dẫn dưới đây mô tả cách chạy khi code được triển khai. Một số bước
> (VD: `dotnet restore`, `dotnet run`) sẽ báo lỗi cho đến khi `.csproj` có nội dung.

### 5.2 Chạy backend (khi code sẵn sàng)

```bash
cd src/backend/CinemaTicketBooking.Api
dotnet restore
dotnet run
```

Backend sẽ lắng nghe trên port được cấu hình trong `appsettings.Development.json`
(dự kiến HTTP `5000` hoặc HTTPS `7000` — xác nhận khi file được điền đầy đủ).

### 5.3 Cấu hình môi trường

ASP.NET Core đọc cấu hình theo thứ tự ưu tiên:
1. Biến môi trường (bao gồm từ `.env` khi được load)
2. `appsettings.Development.json` (khi `ASPNETCORE_ENVIRONMENT=Development`)
3. `appsettings.json`
4. .NET User Secrets (khuyến nghị cho development thay vì `.env`)

**Cách dùng .NET User Secrets (thay thế an toàn cho `.env` với backend):**

```bash
cd src/backend/CinemaTicketBooking.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;..."
```

> User Secrets lưu local, không bao giờ được commit vào Git. Phù hợp cho môi trường
> development cá nhân. Xem §8 để biết danh sách đầy đủ các biến.

### 5.4 Migration EF Core

> EF Core migration chưa được tạo (backend chưa có code). Schema hiện do `database-schema.sql`
> định nghĩa. Khi migration được tạo:

```bash
# Cài EF tools nếu chưa có
dotnet tool install --global dotnet-ef

# Kiểm tra migration hiện có
dotnet ef migrations list

# Áp dụng migration
dotnet ef database update
```

> **Ưu tiên:** Trong giai đoạn hiện tại, dùng `database/database-schema.sql` trực tiếp
> (xem §4.5). Chuyển sang EF migration khi team thống nhất chiến lược migration.

### 5.5 Containerize backend (dự kiến)

> Docker hóa backend chưa được triển khai (chưa có Dockerfile). Cấu hình dự kiến:
> backend cần đọc `ConnectionStrings__DefaultConnection` từ biến môi trường khi chạy
> trong container. Sẽ bổ sung vào `docker-compose.yml` khi backend có Dockerfile.

---

## 6. Frontend React + Vite (CIN-SUB-224)

### 6.1 Trạng thái hiện tại

> Frontend (`src/frontend/cinema-ticket-booking-ui`) hiện là **khung dự án** (placeholder).
> Các file `package.json`, `vite.config.ts`, `tsconfig.json`, `index.html`, `src/App.tsx`,
> `src/main.tsx` đang trống. Chưa có Dockerfile riêng cho frontend.

### 6.2 Chạy frontend (khi code sẵn sàng)

```bash
cd src/frontend/cinema-ticket-booking-ui
npm install
npm run dev
```

Dev server Vite sẽ chạy trên `http://localhost:5173` (mặc định Vite).

Mở địa chỉ trên trình duyệt để kiểm tra.

### 6.3 Cấu hình API URL

Frontend đọc URL backend từ biến môi trường Vite (prefix `VITE_`):

- **Khi chạy local (không Docker):** Backend chạy trên `http://localhost:5000` (dự kiến).
  Cấu hình trong `src/frontend/cinema-ticket-booking-ui/.env`:
  ```ini
  VITE_API_BASE_URL=http://localhost:5000
  ```
- **Khi chạy trong Docker:** URL phụ thuộc vào network Docker Compose; xem §7.

> **Lưu ý CORS:** Backend phải cho phép origin của dev server (`http://localhost:5173`).
> Cấu hình CORS sẽ được mô tả khi backend có code.

### 6.4 Build production

```bash
cd src/frontend/cinema-ticket-booking-ui
npm run build
```

Kết quả build tĩnh xuất ra thư mục `dist/`. Có thể serve từ bất kỳ web server nào.

### 6.5 Containerize frontend (dự kiến)

> Chưa có Dockerfile cho frontend. Cấu hình dự kiến: multi-stage build (Node build
> stage → Nginx/serve stage), đọc `VITE_API_BASE_URL` khi build.
> Sẽ bổ sung vào `docker-compose.yml` khi Dockerfile được tạo.

---

## 7. Docker Compose (CIN-SUB-225)

### 7.1 Trạng thái hiện tại

> `docker-compose.yml` (root) hiện **trống** (placeholder 0 bytes).
> Khi file được hoàn thiện, mục tiêu là khởi động tất cả service bằng một lệnh.

### 7.2 Cấu trúc dự kiến

```yaml
# docker-compose.yml — cấu trúc dự kiến (chưa chính thức, chờ team hoàn thiện)
services:
  postgres:
    image: postgres:16
    container_name: cinema_postgres
    environment:
      POSTGRES_DB: ${POSTGRES_DB:-cinema_db}
      POSTGRES_USER: ${POSTGRES_USER:-cinema_user}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-cinema_user} -d ${POSTGRES_DB:-cinema_db}"]
      interval: 10s
      timeout: 5s
      retries: 5

  backend:
    build:
      context: ./src/backend/CinemaTicketBooking.Api
      dockerfile: Dockerfile                      # chưa tồn tại
    container_name: cinema_backend
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: >
        Host=postgres;Port=5432;Database=${POSTGRES_DB:-cinema_db};
        Username=${POSTGRES_USER:-cinema_user};Password=${POSTGRES_PASSWORD}
    ports:
      - "5000:5000"
    depends_on:
      postgres:
        condition: service_healthy
    # Không thể build cho đến khi Dockerfile backend tồn tại

  frontend:
    build:
      context: ./src/frontend/cinema-ticket-booking-ui
      dockerfile: Dockerfile                      # chưa tồn tại
      args:
        VITE_API_BASE_URL: http://backend:5000    # internal Docker network
    container_name: cinema_frontend
    ports:
      - "3000:80"                                 # hoặc port khác theo thỏa thuận team
    depends_on:
      - backend
    # Không thể build cho đến khi Dockerfile frontend tồn tại

volumes:
  postgres_data:
```

### 7.3 Quan hệ phụ thuộc

```
postgres (health: pg_isready)
    ↑
backend (depends_on: postgres healthy)
    ↑
frontend (depends_on: backend)
```

### 7.4 Lệnh khi docker-compose.yml sẵn sàng

```bash
# Khởi động tất cả service
docker compose up -d

# Xem logs
docker compose logs -f

# Xem trạng thái container
docker compose ps

# Dừng tất cả
docker compose down

# Dừng và xóa volume (reset database)
docker compose down -v
```

### 7.5 Các bước cần bổ sung trước khi Docker Compose hoạt động hoàn toàn

- [ ] Điền nội dung `docker-compose.yml`
- [ ] Tạo `Dockerfile` cho backend (`src/backend/CinemaTicketBooking.Api/Dockerfile`)
- [ ] Tạo `Dockerfile` cho frontend (`src/frontend/cinema-ticket-booking-ui/Dockerfile`)
- [ ] Thống nhất port mapping với team
- [ ] Test `docker compose up -d` trên máy thực tế

> **Hiện tại (2026-10-23):** Chỉ PostgreSQL là service có thể container hóa ngay
> (image chuẩn, không cần build). Backend và Frontend cần Dockerfile trước.

---

## 8. Biến môi trường chi tiết (CIN-SUB-226)

### 8.1 Backend (root `.env` / User Secrets / biến môi trường)

| Biến | Mục đích | Thành phần dùng | Giá trị ví dụ (an toàn) | Bắt buộc |
|---|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | Connection string PostgreSQL (Supabase hoặc local) | Backend (EF Core / Npgsql) | `Host=localhost;Port=5432;Database=cinema_db;Username=cinema_user;Password=CHANGE_ME` | ✅ |
| `ASPNETCORE_ENVIRONMENT` | Môi trường .NET (Development / Production) | Backend runtime | `Development` | Khuyến nghị |
| `POSTGRES_PASSWORD` | Mật khẩu PostgreSQL (dùng với Docker Compose) | Docker Compose (postgres service) | `CHANGE_ME_LOCALLY` | ✅ khi dùng Docker |
| `POSTGRES_DB` | Tên database PostgreSQL (Docker Compose) | Docker Compose | `cinema_db` | Tùy chọn (có default) |
| `POSTGRES_USER` | Username PostgreSQL (Docker Compose) | Docker Compose | `cinema_user` | Tùy chọn (có default) |

> **Biến chưa xác định:** Các biến cho JWT secret key, Payment Gateway API key,
> Expiry Worker interval sẽ được bổ sung khi backend có code và team thống nhất.
> Không bổ sung biến chưa được xác nhận vào `.env.example`.

### 8.2 Frontend (`src/frontend/cinema-ticket-booking-ui/.env`)

| Biến | Mục đích | Giá trị ví dụ | Bắt buộc |
|---|---|---|---|
| `VITE_API_BASE_URL` | URL gốc của Backend API | `http://localhost:5000` | ✅ (dự kiến) |

> **Lưu ý:** Tên biến `VITE_API_BASE_URL` là dự kiến dựa trên convention Vite.
> Tên thực tế sẽ được xác nhận khi `package.json` và `vite.config.ts` có nội dung.

### 8.3 Quy tắc quản lý secrets

1. **Không commit secrets:** File `.env` thật không được commit. Chỉ commit `.env.example`.
2. **`.env.example`** chứa key và giá trị mẫu an toàn (placeholder như `YOUR_PASSWORD`).
3. **User Secrets (.NET):** Khuyến nghị cho backend development local — không lưu trên disk có thể commit.
4. **Không dùng giá trị development trong production:** Mật khẩu local không được đặt làm default trong production config.
5. **Không log secrets:** Đảm bảo connection string không xuất hiện trong application log.

### 8.4 Cập nhật `.env.example`

File `.env.example` hiện tại (root) chứa:
```ini
# Connection String Supabase PostgreSQL (Session Pooler - IPv4 compatible)
# Copy file nay thanh .env va dien DATABASE PASSWORD cua project Supabase.
# Khong commit file .env chua mat khau that len Git.

ConnectionStrings__DefaultConnection=Host=aws-0-ap-northeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.gochgvgokxsjjkojywcs;Password=YOUR_DATABASE_PASSWORD;SSL Mode=Require
```

> Cần bổ sung vào `.env.example` khi các biến mới được xác nhận:
> - `POSTGRES_PASSWORD`, `POSTGRES_DB`, `POSTGRES_USER` (cho Docker local)
> - Các biến JWT, Payment Gateway key (khi backend có code)

---

## 9. Khởi động nhanh (CIN-SUB-227)

### 9.1 Prerequisites checklist

Trước khi bắt đầu, xác nhận:

- [ ] .NET SDK 8+ đã cài: `dotnet --version`
- [ ] Node.js 20 LTS đã cài: `node --version`
- [ ] Git đã cài: `git --version`
- [ ] (Tùy chọn) Docker Desktop đang chạy: `docker info`
- [ ] Đã có thông tin kết nối PostgreSQL (Supabase hoặc local)

### 9.2 Luồng thiết lập lần đầu

```bash
# Bước 1: Clone repository
git clone <REPO_URL>
cd "Movie-Theater-Management-and-Online-Ticket-Booking-System"

# Bước 2: Cấu hình biến môi trường
cp .env.example .env
# → Mở .env, điền mật khẩu PostgreSQL thật

cp src/frontend/cinema-ticket-booking-ui/.env.example \
   src/frontend/cinema-ticket-booking-ui/.env
# → Điền VITE_API_BASE_URL khi key được xác nhận

# Bước 3: Khởi tạo database schema và seed data
# (Thay <...> bằng thông tin kết nối thực tế)
psql -h <HOST> -p <PORT> -U <USER> -d <DB> -v ON_ERROR_STOP=1 -f database/database-schema.sql
psql -h <HOST> -p <PORT> -U <USER> -d <DB> -f database/seed-data.sql

# Bước 4: Chạy backend
cd src/backend/CinemaTicketBooking.Api
dotnet restore && dotnet run
# Để lại terminal này chạy; mở terminal mới

# Bước 5: Chạy frontend
cd src/frontend/cinema-ticket-booking-ui
npm install && npm run dev
```

> **Lưu ý:** Bước 4 và 5 sẽ báo lỗi cho đến khi backend/frontend có code thực.
> Giai đoạn hiện tại chỉ Bước 1-3 (database) có thể thực hiện được.

### 9.3 Luồng một lệnh (khi Docker Compose hoàn thiện)

Khi `docker-compose.yml` được điền đầy đủ và Dockerfile của backend/frontend đã có:

```bash
# Từ root repository
docker compose up -d
```

Theo dõi logs:
```bash
docker compose logs -f
```

### 9.4 Kiểm tra hoạt động

1. **Database:** Kết nối thử bằng `psql` hoặc client GUI — xem các bảng đã được tạo.
2. **Backend:** Truy cập Swagger UI hoặc gọi `GET http://localhost:5000/api/movies`.
3. **Frontend:** Mở `http://localhost:5173` — trang Home tải và hiển thị danh sách phim từ backend.
4. **Seed data:** Kiểm tra có 6 phim, 2 phòng chiếu, 82 ghế và 48 suất chiếu (theo `database/seed-README.md`).

### 9.5 Dừng môi trường

```bash
# Dừng backend (Ctrl+C trong terminal đang chạy dotnet run)
# Dừng frontend (Ctrl+C trong terminal đang chạy npm run dev)

# Hoặc dừng Docker services
docker compose down        # giữ volume (dữ liệu)
docker compose down -v     # xóa cả volume (reset hoàn toàn)
```

---

## 10. Xử lý lỗi thường gặp

| Lỗi / Triệu chứng | Nguyên nhân thường gặp | Cách xử lý |
|---|---|---|
| `psql: FATAL: password authentication failed` | Sai mật khẩu trong connection string | Kiểm tra `.env` — đối chiếu với Supabase Dashboard hoặc cấu hình local |
| `psql: could not connect to server` | Host/port sai, PostgreSQL chưa chạy | Kiểm tra `docker ps`; kiểm tra HOST trong `.env` |
| `ERROR: extension "btree_gist" is not available` | Extension chưa được enable | Chạy `CREATE EXTENSION IF NOT EXISTS btree_gist;` trong database trước khi chạy schema |
| `dotnet restore` lỗi (file not found) | `CinemaTicketBooking.Api.csproj` trống | Backend chưa có code — chờ team triển khai |
| `npm install` lỗi (không có package.json) | `package.json` trống | Frontend chưa có code — chờ team triển khai |
| Frontend không gọi được backend | CORS chưa cấu hình, sai URL | Kiểm tra `VITE_API_BASE_URL` trong `.env` frontend; kiểm tra CORS backend |
| Port 5432 bị chiếm | PostgreSQL local đang chạy trên cùng port | Dừng PostgreSQL local hoặc đổi port Docker mapping (`"5433:5432"`) |
| Port 5000/7000 bị chiếm | Ứng dụng khác hoặc backend đang chạy | Dừng process cũ; đổi port trong `launchSettings.json` |
| Port 5173 bị chiếm | Vite dev server khác đang chạy | Đổi port trong `vite.config.ts` hoặc dùng `--port` flag |
| Seed lỗi duplicate showtimes | Chạy seed lần 2 sau khi đã có showtimes | Xem `database/seed-README.md` — cần clear showtimes trước khi chạy lại |
| `dotnet ef` không tìm thấy | Thiếu EF tools | `dotnet tool install --global dotnet-ef` |
| `Cannot open database` / SSL error với Supabase | Thiếu `SSL Mode=Require` trong connection string | Thêm `SSL Mode=Require` vào connection string Supabase |

---

## 11. Liên quan

- **Schema và seed:** `database/database-schema.sql`, `database/seed-data.sql`, `database/seed-README.md`
- **Thiết kế database:** `database/database-design.md`, `database/database-schema.md`
- **Kiến trúc hệ thống:** `docs/analysis and design/architecture.md`
- **Mẫu biến môi trường:** `.env.example` (root), `src/frontend/cinema-ticket-booking-ui/.env.example`
- **Docker Compose:** `docker-compose.yml` (root — hiện trống, chờ hoàn thiện)
- **Git workflow:** `docs/management/git-workflow.md`
- **Cấu trúc dự án:** `PROJECT_STRUCTURE.md`
