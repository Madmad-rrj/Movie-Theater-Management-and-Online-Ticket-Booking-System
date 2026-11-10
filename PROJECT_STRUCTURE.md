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
│   │   └── local-development-setup.md         # Hướng dẫn cài đặt và chạy local
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
│   └── demo-local-db.ps1                      # Khởi động PostgreSQL và chạy truy vấn demo
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
    │   ├── CinemaTicketBooking.Api/
    │   │   ├── CinemaTicketBooking.Api.csproj # Project ASP.NET Core Web API
    │   │   ├── GlobalExceptionHandler.cs      # Exception handler → error envelope {error:{code,message,details,traceId}}
    │   │   ├── Program.cs                    # Entry point, DI và middleware
    │   │   ├── Program.Public.cs              # `public partial class Program` (phục vụ integration test)
    │   │   ├── appsettings.Development.json  # Cấu hình môi trường development
    │   │   ├── Controllers/
    │   │   │   ├── CinemasController.cs       # GET /api/cinemas
    │   │   │   ├── MoviesController.cs        # GET /api/movies, GET /api/movies/{movieId}
    │   │   │   ├── PricingController.cs       # GET /api/ticket-categories, GET /api/cinemas/{cinemaId}/ticket-pricing
    │   │   │   ├── SeatMapController.cs       # GET /api/showtimes/{showtimeId}/seats
    │   │   │   └── ShowtimesController.cs     # GET /api/movies/{movieId}/showtimes, GET /api/showtimes/{showtimeId}
    │   │   ├── Data/
    │   │   │   └── ApplicationDbContext.cs   # EF Core DbContext và mapping schema cinema
    │   │   ├── DTOs/
    │   │   │   ├── CommonDtos.cs              # ApiResponse, PaginationDto, MoneyDto
    │   │   │   ├── Cinemas/
    │   │   │   │   └── CinemaDtos.cs          # Cinema DTO
    │   │   │   ├── Movies/
    │   │   │   │   ├── MovieDtos.cs           # Movie, Genre DTO
    │   │   │   │   └── MovieQuery.cs          # Query/filter/pagination input phim
    │   │   │   ├── Pricing/
    │   │   │   │   └── PricingDtos.cs         # TicketCategory, CinemaPricing DTO
    │   │   │   ├── Seats/
    │   │   │   │   └── SeatDtos.cs            # SeatType, Seat, SeatMap DTO
    │   │   │   └── Showtimes/
    │   │   │       └── ShowtimeDtos.cs        # Showtime DTO và query lịch chiếu
    │   │   ├── Models/
    │   │   │   ├── BookingSeat.cs             # Entity cinema.booking_seat (đọc allocation)
    │   │   │   ├── Cinema.cs                  # Entity cinema.cinema
    │   │   │   ├── CinemaRoom.cs              # Entity cinema.cinema_room
    │   │   │   ├── CinemaTicketPrice.cs       # Entity cinema.cinema_ticket_price
    │   │   │   ├── Genre.cs                   # Entity cinema.genre
    │   │   │   ├── Movie.cs                   # Entity cinema.movie
    │   │   │   ├── MovieGenre.cs              # Entity cinema.movie_genre
    │   │   │   ├── Seat.cs                    # Entity cinema.seat
    │   │   │   ├── SeatType.cs                # Entity cinema.seat_type
    │   │   │   ├── Showtime.cs                # Entity cinema.showtime
    │   │   │   └── TicketCategory.cs          # Entity cinema.ticket_category
    │   │   └── Services/
    │   │       ├── ApiExceptions.cs            # Exception nghiệp vụ/API dùng chung
    │   │       ├── CinemaService.cs           # Truy vấn rạp công khai
    │   │       ├── ICinemaService.cs          # Contract service rạp
    │   │       ├── IMovieService.cs           # Contract service phim
    │   │       ├── IPricingService.cs         # Contract service loại vé/giá
    │   │       ├── ISeatMapService.cs         # Contract service sơ đồ ghế
    │   │       ├── IShowtimeService.cs        # Contract service suất chiếu
    │   │       ├── MovieService.cs            # Truy vấn phim (browse/detail)
    │   │       ├── PricingService.cs          # Truy vấn loại vé và giá rạp
    │   │       ├── SeatMapService.cs          # Truy vấn sơ đồ ghế theo suất
    │   │       └── ShowtimeService.cs         # Truy vấn suất chiếu
    │   │
    │   └── CinemaTicketBooking.Api.Tests/
    │       ├── CinemaTicketBooking.Api.Tests.csproj # Project test (xUnit, WebApplicationFactory, Testcontainers)
    │       ├── Infrastructure/
    │       │   ├── ApiFactory.cs              # WebApplicationFactory<Program> trỏ DB test
    │       │   ├── DbCleaner.cs               # Helper tạo/dọn dữ liệu fixture theo test
    │       │   └── PostgresFixture.cs         # Collection fixture: PostgreSQL 16.4 + nạp schema 1 lần/lượt
    │       ├── Movies/
    │       │   └── MovieEndpointTests.cs      # Test browse/detail phim, ID/visibility
    │       ├── Pricing/
    │       │   └── PricingEndpointTests.cs    # Test ticket-categories và cinema pricing
    │       ├── Seats/
    │       │   └── SeatMapEndpointTests.cs    # Test seat map, availability, isolation
    │       ├── Showtimes/
    │       │   └── ShowtimeEndpointTests.cs   # Test showtimes, filter, timezone boundary
    │       └── Smoke/
    │           └── MoviesSmokeTests.cs        # Smoke GET /api/movies
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

- Backend: `Entities/`, `Middleware/`, `Migrations/`, `Validators/` và các thư mục mở rộng khác khi cần. (`Controllers/`, `DTOs/`, `Models/`, `Services/` đã có và đang được sử dụng.)
- Frontend: `assets/`, `contexts/`, `hooks/`, `layouts/`, `routes/`, `utils/` và `public/`. (Một số thư mục như `components/`, `pages/`, `services/`, `types/` đã có trong `cinema-ticket-booking-ui`.)
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
