# Local Development Setup — Promethe-Phos

Ngày rà soát: 09/11/2026. Phạm vi đã kiểm chứng trong nhiệm vụ này: PostgreSQL local bằng Docker Compose, bootstrap schema, seed và truy vấn phim. Backend/frontend, Supabase, booking và payment chưa thuộc demo.

## 1. Phạm vi và mức độ xác minh

Hướng dẫn dành cho Windows PowerShell tại repository của nhóm. Phần PostgreSQL demo trong Mục 4 đã được chạy kiểm chứng trên repository hiện tại; backend/frontend và các integration khác chưa được chạy trong phạm vi này.

| Nội dung | Căn cứ / trạng thái |
|---|---|
| Stack | React + Vite + TypeScript; ASP.NET Core Web API + Identity + EF Core/Npgsql + PostgreSQL theo SAD |
| Local DB | PostgreSQL 16.4 bằng Docker Compose; demo đã chạy trên port 55433 vì port mặc định 5433 đang bận |
| Cloud DB | Supabase PostgreSQL; không đồng nghĩa sử dụng Supabase Auth |
| Email | Nhóm báo đã thử Brevo API và gửi thành công; chưa chứng minh app outbox/retry đã tích hợp |
| Payment | MoMo hướng tới demo, chưa thử; phải ghi rõ mock hoặc sandbox khi chạy |
| Hosting | Vercel frontend / Render backend là lựa chọn của nhóm; không xác nhận deployment hoạt động |
| Phiên bản dependency | Phải đọc manifest/lockfile hiện tại; không mặc định .NET 8, Node 20 hay PostgreSQL 16 là yêu cầu đã chốt |
| Database lifecycle | Demo dùng `database-schema.sql` bootstrap trên volume mới và `seed-data.sql`; không chạy EF migration |

Hướng dẫn thiết lập không thay thế [architecture.md](../analysis%20and%20design/architecture.md), [use-cases.md](../requirements/use-cases.md), hoặc SAD Deployment/Implementation View cần hoàn thiện ở PA4.

## 2. Kiểm tra repository và công cụ

Mở PowerShell, dùng các đường dẫn dự kiến sau; nếu repo đổi tên thư mục, điều chỉnh theo file thực tế. Không tạo project mới đè lên project đã có.

```powershell
$cinemaRepo = 'D:\HCMUS\Nhập môn cnpm\Movie Theater Management and Online Ticket Booking System'
Set-Location -LiteralPath $cinemaRepo
$apiProject = Join-Path $cinemaRepo 'src/backend/CinemaTicketBooking.Api/CinemaTicketBooking.Api.csproj'
$uiRoot = Join-Path $cinemaRepo 'src/frontend/cinema-ticket-booking-ui'
Test-Path -LiteralPath $apiProject
Test-Path -LiteralPath (Join-Path $uiRoot 'package.json')
git status --short
dotnet --list-sdks
node --version
npm --version
docker version
docker compose version
Get-Content -LiteralPath $apiProject
Get-Content -LiteralPath (Join-Path $uiRoot 'package.json')
```

Đọc `AGENTS.md`, `mandatory-instruction.md` và hướng dẫn repository trước khi sửa code. Nếu có `global.json`, dùng SDK theo version/roll-forward đã quy định. Đối chiếu `TargetFramework`, EF Core/Npgsql package versions, `package.json` engines, package-manager declaration, lockfile và các file pin Node nếu có. Không tự nâng dependency để làm lệnh mẫu chạy được.

| Công cụ | Cách chọn |
|---|---|
| .NET SDK | Khớp global.json/TargetFramework và package compatibility |
| Node/package manager | Khớp package.json và lockfile của repo; ví dụ npm bên dưới chỉ dùng khi repo dùng npm |
| Docker Desktop / Engine | Docker daemon chạy, Linux containers và Compose v2 có sẵn |
| PostgreSQL | Major version được nhóm chọn và tương thích migration/extensions; pin image, không dùng latest mơ hồ |
| EF CLI | Ưu tiên local tool manifest; version tương thích EF Core của project |

Nếu cần clone, lấy URL repository thật của nhóm. Tài liệu không đoán remote URL hoặc branch triển khai.

## 3. Quy ước cấu hình và secret

### 3.1 Ba nơi đọc cấu hình khác nhau

| Nơi | Cách nhận cấu hình | Lưu ý |
|---|---|---|
| ASP.NET Core | Configuration providers trong Program.cs; mặc định có appsettings, development User Secrets, environment, command line | Không mặc định đọc file `.env` ở root |
| Docker Compose | `.env`/`--env-file` phục vụ nội suy Compose; `environment`/service `env_file` truyền biến vào container | Không tự truyền các biến đó vào backend chạy bằng dotnet trên host |
| Vite | env files trong env directory cấu hình, thường frontend root; `VITE_*` dùng phía browser | Giá trị VITE được công khai trong bundle, không chứa secret |

Với `WebApplication.CreateBuilder` mặc định, ưu tiên **application configuration** từ cao xuống thấp: command-line arguments → environment variables → User Secrets khi Development → appsettings.Development.json → appsettings.json → host fallback. Program.cs có thể thay đổi provider/thứ tự; phải kiểm tra thực tế. Đây không phải mô tả đầy đủ precedence của host bootstrap/launch profiles.

Nguồn: [Microsoft configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/), [Docker interpolation](https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/), [Vite env](https://vite.dev/guide/env-and-mode).

### 3.2 Giá trị public và secret

Commit file mẫu chỉ có placeholders. Kiểm `.gitignore` thực tế thay vì giả định đã bỏ qua `.env`, `.env.local` hoặc `.env.docker.local`. Không đưa DB password, Brevo key, MoMo secret, JWT signing key, refresh/Guest credentials vào VITE variables, URL query hoặc log.

User Secrets chỉ dành cho development, **không mã hóa như một secret vault**. Tránh đưa mật khẩu thật trực tiếp vào command history. Có thể dùng editor để điền User Secrets hoặc biến môi trường của tiến trình từ cơ chế secret local được nhóm chọn. Không in toàn bộ config/connection string để debug.

| Nhóm cấu hình | Ví dụ tên key | Trạng thái |
|---|---|---|
| DB | `ConnectionStrings:DefaultConnection` / env `ConnectionStrings__DefaultConnection` | Chỉ dùng nếu Program.cs thực sự đọc key này |
| Public API URL | `VITE_API_BASE_URL` | Đề xuất; phải khớp frontend API client |
| Allowed origins | Tên option lấy từ code CORS thực tế | Không tự tạo key rồi coi middleware đã đọc |
| JWT/session/refresh | Issuer/audience/signing material và lifetime theo C03 implementation | Key/lifetime cụ thể cần đối chiếu code, không đặt tùy ý trong tài liệu |
| Brevo/MoMo | Server-side options theo adapter implementation | Không phải VITE; tách test và live credentials |
| Workers | Interval/lease/retry/batch theo options thực tế | Không tự ghi một config key chưa được bind |

## 4. PostgreSQL Docker local

### 4.1 Demo database đầu tiên

Demo hiện tại chỉ khởi động PostgreSQL, chạy bootstrap schema/seed và truy vấn danh sách phim. Chưa có frontend, Backend API, Supabase, booking hoặc payment.

Điều kiện cần:

- Docker Desktop đang chạy, Linux containers và Docker Compose v2 khả dụng.
- PowerShell trên Windows.
- Repository checkout ở trạng thái có `docker-compose.yml`, `.env` và các SQL script.

Tạo `.env` từ `.env.example`, sau đó thay password local nếu cần. Không ghi password thật vào Git và không in `.env` ra terminal:

```powershell
Copy-Item -LiteralPath .env.example -Destination .env
```

Chạy demo từ bất kỳ thư mục nào:

```powershell
.\scripts\demo-local-db.ps1
```

Script tự xác định project root từ vị trí script, kiểm tra Docker/Compose và file đầu vào, khởi động service `postgres`, chờ readiness tối đa 120 giây, xác nhận `cinema` và `cinema.movie`, rồi chạy `database/demo-movies.sql` qua `psql` trong container.

Compose dùng PostgreSQL `16.4`, named volume `cinema_demo_postgres_data`, host binding `127.0.0.1:${POSTGRES_HOST_PORT:-5433}:5432`, và mount read-only theo thứ tự:

1. `database/database-schema.sql` → `/docker-entrypoint-initdb.d/01-schema.sql`
2. `database/seed-data.sql` → `/docker-entrypoint-initdb.d/02-seed.sql`

`database-schema.sql` là bootstrap cho database/schema trống: nó tạo namespace `cinema` và các bảng, không phải migration nâng cấp database cũ. Init scripts chỉ chạy khi data directory của volume mới còn trống. Restart hoặc chạy lại Compose không chạy seed lần nữa; seed hiện có transaction, advisory lock và kiểm tra drift nên chạy lại cùng fixture/date không tạo phim trùng.

Kết quả seed mới dự kiến có **6 phim**, **18 movie–genre links**, **9 genres**, **1 cinema**, **2 rooms**, **100 seats**, **2 cinema prices** và **48 showtimes**. Demo query in 6 phim theo `cinema.movie.id`, duration, status, language và genres.

Backend chạy trên Windows host (khi được triển khai sau này) sẽ dùng `Host=localhost;Port=5433`; container cùng Compose network dùng host `postgres;Port=5432`. Demo hiện chưa khởi động backend.

### 4.2 Schema, migrations và seed

Demo này chọn trực tiếp `database/database-schema.sql` làm bootstrap duy nhất cho volume mới và `database/seed-data.sql` làm fixture. Không chạy EF migrations hoặc `EnsureCreated` trong phạm vi demo.

Không dùng `docker compose down -v`, `docker volume rm`, `TRUNCATE`, `DROP` hoặc script tự reset để vượt lỗi. Nếu init thất bại, xem log và sửa nguyên nhân; restart container không tiếp tục một init script đã lỗi trên data directory đã có dữ liệu.

Xem log:

```powershell
docker compose logs --tail 200 postgres
docker compose ps
```

Dừng container nhưng giữ dữ liệu:

```powershell
docker compose stop postgres
```

Khởi động lại volume hiện tại:

```powershell
docker compose start postgres
```

Nếu muốn đổi schema/seed, đó là thay đổi bootstrap/fixture riêng, không phải migration. Chỉ thử trên database/volume demo mới được đặt tên rõ ràng; không xóa volume đang chứa dữ liệu người dùng.

Xử lý lỗi thường gặp:

| Triệu chứng | Cách xử lý |
|---|---|
| Docker chưa chạy | Khởi động Docker Desktop rồi chạy lại script. |
| Port 5433 đã dùng | Đổi `POSTGRES_HOST_PORT` trong `.env` sang port trống, ví dụ `55433`; không tự dừng tiến trình khác. |
| Thiếu `.env` hoặc biến bắt buộc | Copy `.env.example` thành `.env` và điền `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`. |
| Schema/seed lỗi | Đọc `docker compose logs postgres`; không reset volume và không cho rằng restart sẽ tiếp tục init. |
| Volume đã tồn tại với schema khác | Giữ nguyên volume; tạo project/volume demo khác có tên rõ ràng hoặc xử lý migration có chủ đích. |

### 4.3 Supabase là profile khác

Khi cần cloud test, lấy host/port/username/TLS requirements từ project Supabase thật; direct và pooler có thể khác nhau. Không đoán pooler username/port từ ví dụ localhost. Kiểm Npgsql/provider compatibility, pooler mode và migration connection phù hợp. Không bật local worker hoặc chạy seed lên shared/cloud DB nếu chưa xác định phạm vi; không thay local default âm thầm bằng cloud connection.

## 5. Backend ASP.NET Core

### 5.1 Restore và cấu hình connection

```powershell
Set-Location -LiteralPath $cinemaRepo
dotnet restore $apiProject
dotnet build $apiProject --no-restore
```

Đọc `Program.cs`, options binding và project `UserSecretsId`. Nếu chưa có UserSecretsId và nhóm chọn dùng User Secrets, `dotnet user-secrets init --project $apiProject` là thay đổi project cần lưu theo workflow repo. Sau đó dùng editor để thiết lập key thật mà backend đọc, ví dụ:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=cinema_local;Username=cinema_local;Password=<LOCAL_PASSWORD>"
  }
}
```

Đây là placeholder, không phải credential chạy được. Backend phải đăng ký DbContext với Npgsql và đọc đúng configuration key; việc thêm JSON không tự cấu hình dependency injection. Môi trường Development phải được thiết lập trước khi chạy EF nếu cần User Secrets.

### 5.2 Chạy backend

Ưu tiên launch profile đã review trong `Properties/launchSettings.json`. Phương án explicit local URLs sau chỉ dùng khi app/Kestrel hỗ trợ cấu hình đó và certificate dev đã sẵn sàng:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet dev-certs https --trust
dotnet run --project $apiProject --no-launch-profile --urls "https://localhost:7001;http://localhost:5000"
```

Đọc dòng “Now listening on” để lấy URL thực tế. Kestrel/code config có thể thay đổi listener. Không tự khẳng định `/swagger`, `/health` hoặc endpoint API có tồn tại: đọc mapping trong code/OpenAPI đã triển khai. Build pass chỉ chứng minh compile, không chứng minh DB, authorization hoặc provider hoạt động.

Session test phải kiểm server revocation, current roles/assignments, explicit session-replacement challenge và refresh rotation theo SAD C03. Không bỏ các kiểm tra đó vì JWT signature còn hợp lệ.

## 6. Frontend React + Vite

Mở terminal PowerShell khác; khai báo lại `$cinemaRepo`/`$uiRoot` từ mục 2 nếu dùng terminal mới.

```powershell
Set-Location -LiteralPath $uiRoot
if (Test-Path -LiteralPath 'package-lock.json') {
    npm ci
} else {
    # Chỉ khi repo dùng npm và chưa có lockfile; review lockfile phát sinh
    npm install
}
npm run
```

Nếu repo dùng pnpm/yarn, dùng đúng lockfile/package manager thay vì chạy npm tạo lockfile thứ hai. `npm ci` không tự sửa mismatch giữa package.json và lockfile; xử lý dependency theo workflow của nhóm.

Tạo hoặc cập nhật `.env.local` phía frontend, không ghi đè file cũ. Ví dụ khi API client đọc đúng key này:

```dotenv
VITE_API_BASE_URL=https://localhost:7001
```

Kiểm code nối URL có tự thêm `/api` hay không để tránh `/api/api`. Giá trị VITE là public build-time configuration; đổi env phải restart dev server hoặc rebuild production bundle.

Nếu script `dev` chạy Vite và script `build` có trong package.json:

```powershell
npm run dev -- --host localhost --port 5173 --strictPort
# Chạy build ở terminal khác sau khi dependency/config hợp lệ:
npm run build
```

Frontend render được không chứng minh backend integration. Mở Browser Network để kiểm URL, status và payload thật; không suy ra booking confirmed từ success-page navigation.

## 7. Giao tiếp frontend/backend và authentication

| Hạng mục | Thiết lập cần kiểm |
|---|---|
| API URL | Browser truy cập được host/port; không dùng Docker service DNS trong VITE_API_BASE_URL |
| CORS | Allowlist origin thật, ví dụ `http://localhost:5173`; đúng scheme/host/port, không wildcard với credentials |
| Cookie/session | Nếu dùng cookie, client credentials và server cookie settings/CSRF protection phải tương thích; phương thức transport lấy từ implementation được chốt |
| HTTPS | Certificate dev được tin cậy; Secure cookie/test cross-site phải chạy trong topology phù hợp; không tắt validation để che lỗi |
| Guest proof | Truyền theo contract an toàn, không query-string/log plaintext; biết code/email không đủ ownership |
| Errors | Dùng envelope/mã lỗi đúng contract đã triển khai, không tự biến 401/403/409 thành success |
| Proxy | Nếu dùng Vite proxy, browser gọi relative `/api`; target do dev server truy cập, khác browser URL |

API documents hiện là thiết kế Proposed nếu chưa có evidence implementation. Các draft cần đồng bộ explicit replacement challenge, refresh, withdrawal và lifecycle đầy đủ; không làm code yếu hơn SAD chỉ để khớp draft cũ.

## 8. Brevo, MoMo và background execution

### 8.1 Brevo

API key chỉ nằm backend. Dùng sender đã được Brevo chấp nhận và người nhận test được nhóm kiểm soát. Không ghi quota “300/tháng” hoặc coi lần gửi thử là bằng chứng giới hạn gói hiện tại; kiểm dashboard khi lập kế hoạch test.

Kiểm riêng ba tầng: API test độc lập đã được nhóm báo thành công; app tạo persisted notification request; worker gửi/retry và cập nhật delivery state. Queue request trong business transaction, gửi sau commit. Email lỗi không rollback confirmed booking. Tránh gửi hàng loạt khi seed/restart worker trên DB dùng chung.

### 8.2 MoMo

Chọn và ghi rõ mode trước test:

| Mode | Mục đích | Không được kết luận |
|---|---|---|
| Mock adapter local | Kiểm workflow/state/idempotency bằng dữ liệu giả có kiểm soát | Không phải MoMo sandbox đã tích hợp |
| MoMo sandbox | Kiểm contract/signature/callback/query với test account/config thật | Không chứng minh live payments |

Mock chỉ bật theo Development/test configuration được kiểm soát; không thêm public “mark paid” endpoint hoặc bỏ chữ ký webhook để demo. Chọn MoMo trên UI không tự tạo payment; chỉ explicit confirmation mới initiation. Window 10 phút và grace default 10 giây phải khớp nguồn, không giữ cấu hình cũ 2–3 phút.

Provider bên ngoài không gọi callback vào `localhost` của developer. Sandbox cần callback HTTPS reachable do nhóm cấu hình trên môi trường test hoặc tunnel phù hợp; return URL và webhook URL là hai vai trò khác nhau. Sau khi chốt provider contract, kiểm raw payload/signature, verified reference/amount/currency, durable receipt và ACK. Không công bố đã thử MoMo nếu chỉ dùng mock.

### 8.3 Workers và AI

Theo SAD, C14 có thể chạy BackgroundService trong backend modular monolith, không bắt buộc một microservice hay public worker endpoint. Cần kiểm đăng ký worker, options, durable claims/lease/fencing và shutdown/retry trong code. Hai instance local cùng DB không được xử lý tài chính/admission trùng.

Promethe-Nous informational-only. Bounded history ở backend memory theo C12; không yêu cầu pgvector/RAG hoặc persistence hội thoại khi nguồn chưa chốt. Nếu chưa cấu hình provider, hiển thị trạng thái chưa khả dụng thay vì trả dữ liệu bịa.

## 9. Trình tự khởi động và bằng chứng cần ghi

1. Kiểm branch, manifest, tool versions, configuration keys và database đích.
2. Start PostgreSQL, kiểm readiness và connection từ backend.
3. Review/apply nguồn schema đã chọn; seed local dữ liệu phù hợp baseline.
4. Build/start backend, ghi actual URL và endpoint đang có; kiểm worker/provider mode.
5. Install/start frontend, kiểm API URL và CORS/auth transport.
6. Chạy smoke tests dưới đây, lưu actual result/evidence theo từng mục. Mục chưa implement ghi **Not implemented/Blocked**, không ghi Pass.

| Kiểm tra | Kết quả mong đợi |
|---|---|
| Public movie/showtime read | Frontend đọc dữ liệu backend thật, empty/error state đúng |
| Account/session | Customer chưa verify vẫn login theo policy; internal operation cần verify; logout/replacement/current rights đúng |
| Guest/Customer reservation | Guest proof/ownership đúng; selection không hold; Step 1→2 commit đủ seats và deadline gốc |
| Retry/seat race | Không duplicate booking; không bán trùng hoặc giữ một phần |
| Payment mode | Ghi rõ mock/sandbox; redirect không confirm; duplicate event không issue thêm QR |
| Email after commit | Delivery lỗi không đảo booking; retry có theo dõi |
| Admission | Một booking QR, category quotas, −5/+20 phút, đúng Staff assignment |
| Cancellation/refund | Pre-init cancel riêng; review/withdrawal/approval/completion effects đúng origin |
| Authorization | Manager không sửa movies/room identity; Staff không refund completion; không cross-cinema write |
| Recovery | Restart worker hoặc mất response không tạo tác động trùng |

Mỗi evidence ghi commit, môi trường, tool versions, mode provider, seed/test data, timestamp, expected/actual và trace ID đã lọc. Không chụp/copy secret, raw verification links hoặc Guest credentials.

## 10. Troubleshooting và dừng môi trường

| Triệu chứng | Kiểm tra / cách xử lý |
|---|---|
| DB connection refused | Docker daemon, service status, port mapping `POSTGRES_HOST_PORT`/5432; tránh nhầm localhost trong container |
| Password authentication failed | Backend config provider ưu tiên, password thực trong cluster cũ; env đổi không reset volume credential |
| Backend không đọc `.env` | Kiểm configuration providers; dùng User Secrets/env của process đúng key, không giả có dotenv loader |
| EF relation missing/already exists | Kiểm database/schema/migration history và SQL-vs-EF baseline; không drop DB để che mismatch |
| dotnet-ef thiếu/incompatible | Local tool manifest và version EF packages; không cài latest mù |
| Frontend gọi sai endpoint | Inspect Network, API base URL, `/api` prefix, restart Vite sau env change |
| CORS/cookie lỗi | Origin chính xác, credentials policy, scheme/certificate, cookie SameSite/Secure và CSRF design |
| Port 5173 bận | strictPort báo lỗi; dừng process đúng hoặc đổi port cùng CORS, không tự dùng origin mới chưa allow |
| Webhook không đến | localhost không public; kiểm reachable callback URL và sandbox registration |
| Unknown payment | Đọc reconciliation/evidence; không mark failed/release thủ công |
| Worker không xử lý | Registration, options, queue state, lease expiry/fencing, logs đã lọc; không chạy nhiều process vô kiểm soát |

Dừng frontend/backend bằng Ctrl+C trong terminal tương ứng. Dừng demo Compose mà giữ volume:

```powershell
docker compose stop postgres
```

Không thêm `-v` vào routine shutdown; thao tác đó xóa named volume. Reset dữ liệu là thao tác riêng, cần xác định database/backup và mục tiêu, không phải bước bắt buộc mỗi lần chạy.

## 11. Tài liệu tham chiếu và revision note

- [Requirements](../requirements/business-system-analysis.md), [Use cases](../requirements/use-cases.md), [Architecture](../analysis%20and%20design/architecture.md).
- [API contract](../api/api-contract.md), [Endpoint catalogue](../api/api-endpoints.md), [DTO specification](../api/dto-specification.md).
- [Database design](../../database/database-design.md), [Schema](../../database/database-schema.md), [Seed guide](../../database/seed-README.md): phải kiểm phiên bản thực tế trước khi chạy.
- [Microsoft Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/), [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets/).
- [Docker Compose interpolation](https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/), [PostgreSQL official image](https://hub.docker.com/_/postgres), [EF migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/).
- [Vite environment](https://vite.dev/guide/env-and-mode).

Demo verification on 09/11/2026: PostgreSQL 16.4 initialized the `cinema` schema and seed successfully in named volume `movietheatermanagementandonlineticketbookingsystem_cinema_demo_postgres_data`. The query returned 6 movies on the first run and 6 movies on the second run. Port 5433 was already occupied by an unrelated container, so verification used `POSTGRES_HOST_PORT=55433`; the default remains configurable at 5433. No volume reset or destructive command was used.
