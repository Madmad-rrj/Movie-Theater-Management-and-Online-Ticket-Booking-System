# Project Structure

Tài liệu này mô tả cấu trúc thực tế của repository tại thời điểm hiện tại. Các thư mục hoặc file được ghi là “dự kiến” chưa được xem là một phần của implementation hiện tại.

```text
Movie Theater Management and Online Ticket Booking System/
│
├── AGENTS.md                                  # Quy tắc ghi log và làm việc của AI Agent
├── README.md                                  # Tài liệu giới thiệu dự án (đang được hoàn thiện)
├── PROJECT_STRUCTURE.md                       # Tài liệu này
├── mandatory-instruction.md                   # Quy tắc bắt buộc khi làm việc với agent
├── .env.example                               # Mẫu biến môi trường ở root, không chứa secret
├── .gitignore                                 # Danh sách file/thư mục bị Git bỏ qua
├── docker-compose.yml                         # PostgreSQL local, named volume và init scripts
├── docker-compose.be2.yml                     # Arch/Linux: DB trên Linux home và API trong SDK container
│
├── .vscode/
│   └── settings.json                          # Cấu hình VS Code dùng chung
│
├── database/
│   ├── database-design.md                     # Quyết định và nguyên tắc thiết kế database
│   ├── database-schema.md                     # Mô tả bảng, cột, khóa, ràng buộc và index
│   ├── database-schema.sql                    # Script tạo schema PostgreSQL
│   ├── demo-movies.sql                        # Truy vấn read-only kiểm chứng dữ liệu phim
│   ├── movie-research.md                      # Dữ liệu nghiên cứu phục vụ seed phim
│   ├── seed-data.sql                          # Script seed dữ liệu mẫu
│   └── seed-README.md                         # Hướng dẫn chạy và xác minh seed
│
├── docs/
│   ├── Cinema_292_Tasks_Replanned_5_Members_5_Sprints.xlsx
│   │                                            # Bảng phân công công việc theo sprint
│   │
│   ├── analysis and design/
│   │   ├── architecture.md                    # Kiến trúc tổng thể hệ thống
│   │   ├── business-system-design.md          # Thiết kế hệ thống và luồng tương tác
│   │   └── relational-model.md                # Mô hình dữ liệu quan hệ
│   │
│   ├── api/
│   │   ├── api-contract.md                    # Quy ước giao tiếp frontend/backend
│   │   ├── api-endpoints.md                   # Danh mục REST API
│   │   └── dto-specification.md               # Đặc tả request/response DTO
│   │
│   ├── development/
│   │   ├── local-development-setup.md         # Hướng dẫn cài đặt và chạy local
│   │   └── guest-checkout.md                 # Guest bootstrap, cookie/CSRF và kiểm thử BE2
│   │
│   ├── management/
│   │   ├── git-workflow.md                    # Quy trình branch, commit và pull request
│   │   └── ai-usage/                          # Log làm việc theo member/agent ID
│   │       ├── 0098.md
│   │       ├── 0235.md
│   │       ├── 0244.md
│   │       ├── 0255.md
│   │       └── 0271.md
│   │
│   ├── requirements/
│   │   ├── business-system-analysis.md        # Phạm vi, actor và yêu cầu hệ thống
│   │   └── use-cases.md                       # Đặc tả các use case
│   │
│   └── ui-ux/
│       ├── customer-component-specification.md # Đặc tả component tái sử dụng
│       ├── customer-navigation.md             # Quy tắc điều hướng
│       ├── customer-responsive-specification.md # Responsive desktop/tablet/mobile
│       ├── customer-screen-api-mapping.md     # Ánh xạ màn hình với API
│       ├── customer-screen-specification.md   # Đặc tả từng màn hình Customer
│       ├── customer-ui-state.md               # Trạng thái và phản hồi của UI
│       ├── customer-user-flow.md              # Luồng đặt vé của Customer
│       └── customer-web-layout.md             # Bố cục website
│
├── scripts/
│   ├── demo-local-db.ps1                      # Khởi động PostgreSQL và chạy truy vấn demo
│   └── test-guest-context.py                 # .NET/PostgreSQL tests trong Docker biệt lập
│
├── tests/
│   └── CinemaTicketBooking.Api.Tests/
│       ├── CinemaTicketBooking.Api.Tests.csproj
│       ├── GuestApiFixture.cs                # Fresh local PostgreSQL và HTTP test host
│       └── GuestContextTests.cs              # Lifecycle, idempotency, CSRF và cookie tests
│
├── pa/
│   ├── PA0/
│   │   └── 24120235-24120271-24120098-24120244-24120255.pdf
│   ├── PA1/
│   │   ├── #Weekly Report Template W0.docx
│   │   ├── #Weekly Report Template W1.docx
│   │   ├── 01. Software Development Plan.docx
│   │   ├── 02. rup_vision_sp.docx
│   │   └── Backlog Sprint 1.pdf
│   ├── PA2/
│   │   ├── 01. Software Development Plan.docx
│   │   ├── 02. rup_vision_sp.docx
│   │   ├── Lịch backlog jira sprint 2.csv
│   │   ├── Weekly Report W2.docx
│   │   ├── Weekly Report W3.docx
│   │   └── rup_ucspec-v2.4_final.docx
│   ├── PA3/
│   │   └── rup_sad.docx
│   ├── PA1-Group15.zip
│   └── PA2-Group15.zip
│
└── src/
    ├── backend/
    │   └── CinemaTicketBooking.Api/
    │       ├── CinemaTicketBooking.Api.csproj # Project ASP.NET Core Web API
    │       ├── Program.cs                    # Entry point, DI và middleware
    │       ├── appsettings.Development.json  # Cấu hình môi trường development
    │       ├── Controllers/
    │       │   ├── MoviesController.cs        # GET /api/movies
    │       │   ├── GuestContextController.cs  # POST /api/guest-context
    │       │   ├── GuestCheckoutAttribute.cs  # Guest identity và CSRF filter
    │       │   └── CheckoutExceptionFilter.cs # Checkout error envelope
    │       ├── Data/
    │       │   ├── ApplicationDbContext.cs    # Movie/genre và Guest checkout mappings
    │       │   └── GuestCheckoutMapping.cs    # checkout_identity và operation_receipt
    │       ├── DTOs/
    │       │   ├── Movies/
    │       │   │   ├── MovieDtos.cs           # Movie, Genre, pagination và error DTO
    │       │   │   └── MovieQuery.cs          # Query/filter/pagination input
    │       │   ├── Checkout/
    │       │   │   └── GuestContextDto.cs     # Public context ID và expiry
    │       │   └── Common/
    │       │       └── ApiErrorResponse.cs    # code/message/details/traceId
    │       ├── Models/
    │       │   ├── Movie.cs                   # Entity cinema.movie
    │       │   ├── Genre.cs                   # Entity cinema.genre
    │       │   ├── MovieGenre.cs              # Entity cinema.movie_genre
    │       │   ├── CheckoutIdentity.cs        # Guest/User checkout identity
    │       │   └── OperationReceipt.cs        # Durable idempotency metadata
    │       └── Services/
    │           ├── IMovieService.cs           # Service contract
    │           ├── MovieService.cs            # Read-only movie query
    │           ├── IGuestContextService.cs    # Bootstrap và current Guest contract
    │           ├── GuestContextService.cs     # DB time, reuse và bootstrap idempotency
    │           ├── GuestCredential.cs         # Random credential, hash và CSRF binding
    │           ├── GuestRequestSecurity.cs    # Origin, CSRF và cookie transport
    │           ├── GuestCheckoutOptions.cs    # Demo lifetime và security configuration
    │           └── CheckoutException.cs       # Checkout application errors
    │
    └── frontend/
        └── cinema-ticket-booking-ui/
            ├── .env.example                   # Mẫu biến môi trường frontend
            ├── index.html                    # HTML entry point của Vite
            ├── package.json                   # Dependency và script frontend
            ├── package-lock.json              # Lockfile npm của frontend
            ├── tsconfig.json                  # Cấu hình TypeScript
            ├── tsconfig.app.json              # Type-check mã React
            ├── tsconfig.node.json             # Type-check cấu hình Vite
            ├── vite.config.ts                 # Cấu hình Vite và proxy API
            └── src/
                ├── App.tsx                    # Component gốc
                ├── index.css                  # CSS toàn cục trang chủ
                ├── main.tsx                   # Entry point khởi chạy React
                ├── components/
                │   ├── MovieCard.tsx           # Thẻ phim
                │   └── StatePanel.tsx          # Empty/error state
                ├── pages/
                │   └── HomePage.tsx            # Trang chủ
                ├── services/
                │   └── moviesApi.ts            # Client GET /api/movies
                └── types/
                    └── movies.ts               # Kiểu response Movie API
```

## Quy ước về file và thư mục chưa có

Các thành phần sau chưa tồn tại trong repository hiện tại và chỉ nên được bổ sung khi có yêu cầu triển khai:

- Backend: `Entities/`, `Middleware/`, `Migrations/`, `Validators/` và các thư mục liên quan chưa có trong cây trên.
- Frontend: `assets/`, `contexts/`, `hooks/`, `layouts/`, `routes/`, `utils/` và `public/`.
- Tài liệu: `docs/test/` và `docs/management/weekly-reports/`.

Không ghi các file chứa secret thật vào repository. Sử dụng `.env.example` làm mẫu và cấu hình giá trị thật trong môi trường local.

## Bảng mô tả các nhóm tài liệu

| Nhóm | Vị trí | Mục đích |
| --- | --- | --- |
| Database | `database/` | Thiết kế schema, script tạo dữ liệu và hướng dẫn seed |
| Requirements | `docs/requirements/` | Phân tích nghiệp vụ và use case |
| Analysis & design | `docs/analysis and design/` | Kiến trúc, thiết kế hệ thống và mô hình quan hệ |
| API | `docs/api/` | Endpoint, contract và DTO giữa frontend/backend |
| UI/UX | `docs/ui-ux/` | User flow, màn hình, điều hướng, component và trạng thái UI |
| Development | `docs/development/` | Hướng dẫn thiết lập môi trường local |
| Management | `docs/management/` | Git workflow và log sử dụng AI |
| PA deliverables | `pa/` | Tài liệu, báo cáo và sản phẩm nộp theo từng PA |

## Quan hệ giữa các nhóm tài liệu

```text
Requirements
    │
    ├──► Analysis & design ──► Database
    │
    └──► UI/UX ──► Screen–API Mapping ──► API Contract & Endpoints
                                              │
                                              ▼
                              Local Development Setup
```

Đây là quan hệ tham chiếu giữa các nhóm tài liệu, không phải thứ tự bắt buộc tuyệt đối. Khi một quyết định nghiệp vụ hoặc kỹ thuật chưa được thống nhất, tài liệu liên quan cần ghi rõ trạng thái `Open` hoặc `Proposed`.

## BE2 task 2 additions — feat/be-selection-quote

- `src/backend/CinemaTicketBooking.Api/Models/SelectionSession.cs`: selection session and seat entities.
- `src/backend/CinemaTicketBooking.Api/Data/SelectionMapping.cs`: existing SQL tables/composite relationship.
- `src/backend/CinemaTicketBooking.Api/DTOs/Checkout/SelectionDtos.cs`: selection requests, quotes and responses.
- `src/backend/CinemaTicketBooking.Api/Controllers/SelectionsController.cs`: POST/GET/PUT selections.
- `src/backend/CinemaTicketBooking.Api/Services/SelectionService.cs`: ownership, transactions, deadline, idempotency and database quotation reads.
- `src/backend/CinemaTicketBooking.Api/Services/SelectionOptions.cs`: configured maximum ticket quantity.
- `tests/CinemaTicketBooking.Api.Tests/SelectionTests.cs`: PostgreSQL selection integration tests.
- `docs/development/selection-quote.md`: contract correction, integration boundary and Postman instructions.

`BE1.md` and `BE2.md` are local ignored briefs, removed from the Git index on this branch.

Postman: `docs/api/postman/Cinema-BE2.postman_collection.json`, `Cinema-BE2-Local.postman_environment.json` and `README.md` provide the importable Guest/Selection flow and blank local environment.

## BE2 task 3 additions — feat/be-booking-reservation

- `src/backend/CinemaTicketBooking.Api/Models/Booking.cs`: Booking, BookingCategory and the shared BookingSeat entity (reuse for BE1 allocation reads).
- `src/backend/CinemaTicketBooking.Api/Data/BookingMapping.cs`: existing SQL tables, composite allocation FK and active-seat unique-index mapping.
- `src/backend/CinemaTicketBooking.Api/DTOs/Checkout/BookingDtos.cs`: reservation request, explicit accepted money and booking snapshot/current-showtime responses.
- `src/backend/CinemaTicketBooking.Api/Controllers/BookingsController.cs`: protected POST `/api/bookings` only.
- `src/backend/CinemaTicketBooking.Api/Services/BookingReservationService.cs`: transaction, snapshots, audit, durable outcomes and rollback safety.
- `tests/CinemaTicketBooking.Api.Tests/BookingReservationTests.cs`: PostgreSQL transaction, idempotency, price, expiry and race cases.
- `docs/development/booking-reservation.md`: task-3 behavior, integration boundary and Postman flow.

The current Postman collection/environment includes tasks 1–5 (30 requests: 01–26 main flow and 27–30 optional natural expiry). SQL schema/seed files remain unchanged.

## BE2 task 4 additions — feat/be-checkout-recovery

- `src/backend/CinemaTicketBooking.Api/Services/BookingReadService.cs`: coherent owner-protected booking reads and recovery decisions; shared snapshot projection for reservation responses/replay.
- `src/backend/CinemaTicketBooking.Api/DTOs/Checkout/CheckoutRecoveryDto.cs`: recovery, protected payment and cancellation summaries.
- `src/backend/CinemaTicketBooking.Api/Controllers/BookingsController.cs`: additionally implements GET booking and GET booking/checkout.
- `tests/CinemaTicketBooking.Api.Tests/BookingRecoveryTests.cs`: partial reservation test class sharing its real PostgreSQL fixture/cleanup; ownership, reload, recovery-state and concurrent-read cases.
- `docs/development/checkout-recovery.md`: endpoint behavior, recovery-state mapping, integration limits and Arch/Postman instructions.

## BE2 task 5 additions — feat/be-booking-cancel-expiry

- `src/backend/CinemaTicketBooking.Api/Services/BookingCancellationService.cs`: protected cancellation, durable outcomes and shared atomic release/audit helpers.
- `src/backend/CinemaTicketBooking.Api/Services/BookingExpiryService.cs`: bounded cursor scans, nonblocking transaction claims, all-attempt payment safety and per-booking rollback/retry.
- `src/backend/CinemaTicketBooking.Api/Services/BookingExpiryWorker.cs`: internal hosted polling and shutdown cancellation; no public worker endpoint.
- `src/backend/CinemaTicketBooking.Api/Services/BookingExpiryOptions.cs`: enabled flag, poll interval and batch size.
- Existing BookingsController/BookingDtos/Program/Development config: POST cancel, required confirmation, DI and local worker setup.
- `tests/CinemaTicketBooking.Api.Tests/BookingExpiryTests.cs`: partial shared PostgreSQL harness for cancellation, expiry, payment guards, failure injection, hosted polling and races.
- `docs/development/booking-cancel-expiry.md`: API/worker behavior, setup, FE2 handoff and Postman instructions. `.env.example`/`docker-compose.be2.yml` expose optional local worker overrides.
