# Project Structure

```text
Movie Theater Management and Online Ticket Booking System/
│
├── AGENTS.md                               # Hướng dẫn làm việc cho AI Agent trong dự án
├── docker-compose.yml                     # Cấu hình chạy các service bằng Docker
├── .gitignore                             # Danh sách file/thư mục bị Git bỏ qua
├── .env                                   # Biến môi trường thật (KHÔNG commit - chứa mật khẩu)
├── .env.example                           # Mẫu biến môi trường, không chứa secret
├── PROJECT_STRUCTURE.md                   # Mô tả cấu trúc thư mục và mục đích từng thành phần
├── README.md                              # Giới thiệu dự án, công nghệ và hướng dẫn cài đặt/chạy
├── identity.md                            # Định danh/nhân sự nhóm (dùng cho agent)
├── mandatory-instruction.md               # Quy tắc bắt buộc khi làm việc với agent
│
├── .vscode/                               # Cấu hình editor dùng chung cho nhóm
│   └── settings.json                      # Cài đặt workspace VS Code
│
├── database/                              # Tài liệu thiết kế và script cơ sở dữ liệu
│   ├── database-design.md                 # Mô tả thiết kế database và các quyết định thiết kế
│   ├── database-schema.md                 # Mô tả schema: bảng, cột, khóa và quan hệ
│   ├── database-schema.sql                # SQL tạo cấu trúc database
│   ├── seed-data.sql                      # Script seed dữ liệu demo (idempotent)
│   ├── seed-README.md                     # Hướng dẫn chạy seed và báo cáo xác minh
│   └── movie-research.md                  # Kết quả tra cứu thông tin phim cho seed
│
├── docs/                                  # Tài liệu phân tích, thiết kế, quản lý và kiểm thử
│   ├── Cinema_292_Tasks_Replanned_5_Members_5_Sprints.xlsx  # Bảng phân công công việc theo sprint
│   │
│   ├── analysis and design/               # Tài liệu phân tích chi tiết và thiết kế hệ thống
│   │   ├── architecture.md                # Kiến trúc tổng thể và cách các thành phần phối hợp
│   │   ├── business-system-design.md      # Thiết kế hệ thống, UML và luồng tương tác
│   │   └── relational-model.md            # Mô hình dữ liệu quan hệ, bảng và khóa
│   │
│   ├── api/                               # Hợp đồng giao tiếp giữa frontend và backend
│   │   ├── api-endpoints.md               # Danh mục REST API theo nhóm nghiệp vụ (method, URL, tham số, status code)
│   │   ├── api-contract.md                # Quy ước chung: JSON, lỗi, pagination, auth, định dạng ngày giờ/tiền tệ
│   │   └── dto-specification.md           # Đặc tả chi tiết Request DTO / Response DTO
│   │
│   ├── ui-ux/                             # Đặc tả trải nghiệm người dùng Customer trước khi triển khai frontend
│   │   ├── customer-user-flow.md          # Hành trình Customer: từ truy cập website đến đặt vé thành công
│   │   ├── customer-screen-specification.md # Chức năng và nội dung từng màn hình Customer
│   │   ├── customer-navigation.md         # Quy tắc di chuyển giữa các màn hình (header, menu, back/forward)
│   │   ├── customer-web-layout.md         # Bố cục tổng thể website: header, main content, footer, grid
│   │   ├── customer-component-specification.md # Đặc tả UI component tái sử dụng (button, card, modal...)
│   │   ├── customer-ui-state.md           # Trạng thái UI: loading, error, empty, disabled, selected...
│   │   ├── customer-responsive-specification.md # Breakpoint desktop/tablet/mobile và cách bố cục thích ứng
│   │   └── customer-screen-api-mapping.md # Liên kết màn hình với API và dữ liệu frontend cần dùng
│   │
│   ├── development/                       # Hướng dẫn phát triển cho thành viên nhóm
│   │   └── local-development-setup.md     # Cài đặt, cấu hình và chạy toàn bộ dự án trên máy cá nhân
│   │
│   ├── management/                        # Tài liệu quản lý dự án và quy trình làm việc
│   │   ├── git-workflow.md                # Quy tắc branch, commit, pull request và merge
│   │   └── ai-usage/                     # Log làm việc của từng AI Agent/thành viên
│   │       ├── 0098.md                    # Log làm việc của agent 0098
│   │       ├── 0235.md                    # Log làm việc của agent 0235
│   │       ├── 0244.md                    # Log làm việc của agent 0244
│   │       ├── 0255.md                    # Log làm việc của agent 0255
│   │       └── 0271.md                    # Log làm việc của agent 0271
│   │
│   ├── requirements/                      # Tài liệu yêu cầu và phân tích nghiệp vụ
│   │   ├── business-system-analysis.md    # Phạm vi, actor, quy trình và yêu cầu hệ thống
│   │   └── use-cases.md                   # Use Case Diagram và/hoặc đặc tả các Use Case
│   │
│   └── test/                              # Tài liệu kiểm thử (chưa tạo)
│       # Dự kiến bổ sung test plan, test cases và test reports
│
├── pa/                                    # Các tài liệu và sản phẩm nộp theo từng PA
│   ├── PA0/                               # Sản phẩm của PA0
│   │   └── 24120235-24120271-24120098-24120244-24120255.pdf # Báo cáo nộp PA0 (nhóm)
│   └── PA1/                               # Sản phẩm của PA1
│       ├── #Weekly Report Template PA0.docx   # Báo cáo tuần (mẫu PA0)
│       ├── #Weekly Report Template PA1.docx   # Báo cáo tuần (mẫu PA1)
│       ├── 01. Software Development Plan.docx # Kế hoạch phát triển phần mềm
│       ├── 02. rup_vision_sp.docx             # Tài liệu Vision theo RUP
│       └── PA1.pdf                            # Báo cáo nộp PA1 (nhóm)
│
└── src/                                   # Mã nguồn ứng dụng
    │
    ├── backend/                           # Mã nguồn backend
    │   └── CinemaTicketBooking.Api/       # ASP.NET Core Web API
    │       ├── appsettings.Development.json # Cấu hình backend cho môi trường phát triển
    │       ├── CinemaTicketBooking.Api.csproj # Khai báo project và dependency .NET
    │       ├── Program.cs                 # Khởi tạo ứng dụng, DI, middleware và cấu hình API
    │       │
    │       ├── Data/                      # DbContext và các thành phần truy cập dữ liệu
    │       │   └── ApplicationDbContext.cs # Khai báo DbSet và cấu hình model EF Core
    │       │
    │       # Các thư mục dự kiến khi phát triển (chưa tạo): Controllers/,
    │       # Database/ (Configurations, Migrations, Seed), DTOs/, Entities/,
    │       # Middleware/, Properties/, Services/, Validators/
    │
    └── frontend/                          # Mã nguồn giao diện người dùng
        └── cinema-ticket-booking-ui/      # Ứng dụng React + Vite + TypeScript
            ├── .env.example               # Mẫu biến môi trường, không chứa secret thật
            ├── index.html                 # HTML gốc được Vite sử dụng
            ├── package.json               # Scripts, dependency và metadata của frontend
            ├── tsconfig.json              # Cấu hình TypeScript
            ├── vite.config.ts             # Cấu hình công cụ build/dev server Vite
            │
            └── src/                       # Mã nguồn giao diện
                ├── App.tsx                # Component gốc của ứng dụng
                ├── index.css              # CSS toàn cục
                └── main.tsx               # Entry point khởi chạy React
                    # Các thư mục dự kiến khi phát triển (chưa tạo): assets/,
                    # components/, contexts/, hooks/, layouts/, pages/, routes/,
                    # services/, types/, utils/ và public/ (tài nguyên tĩnh)
```

# Bảng mô tả mục đích các file Markdown

| File                               | Mục đích chính                                                                           | Vị trí hiện tại                   |
| ---------------------------------- | ---------------------------------------------------------------------------------------- | --------------------------------- |
| `AGENTS.md`                         | Hướng dẫn làm việc cho AI Agent (phạm vi, quy trình, quy tắc code)                       | Root                              |
| `README.md`                        | Giới thiệu dự án, công nghệ, yêu cầu môi trường và hướng dẫn chạy                        | Root                              |
| `PROJECT_STRUCTURE.md`             | Giải thích cấu trúc repository                                                           | Root                              |
| `database-design.md`               | Lý do lựa chọn thiết kế, quy tắc nghiệp vụ dữ liệu, tính toàn vẹn, concurrency và các quyết định thiết kế                              | `database/`                       |
| `database-schema.md`               | Bảng thực tế, cột, kiểu dữ liệu PostgreSQL, PK, FK, constraints và index                                  | `database/`                       |
| `architecture.md`                  | Mô tả kiến trúc tổng thể, các tầng và cách chúng giao tiếp                               | `docs/analysis and design/`       |
| `business-system-analysis.md`      | Phân tích vấn đề, phạm vi, actor, quy trình nghiệp vụ và yêu cầu chức năng/phi chức năng | `docs/requirements/`              |
| `business-system-design.md`        | Mô tả thiết kế giải pháp, sơ đồ UML và luồng tương tác                                   | `docs/analysis and design/`       |
| `relational-model.md`              | Mô hình quan hệ dữ liệu, khóa chính, khóa ngoại và quan hệ giữa các bảng                 | `docs/analysis and design/`       |
| `use-cases.md`                     | Mô tả các Use Case, actor, điều kiện trước/sau và luồng chính/ngoại lệ                   | `docs/requirements/`              |
| `api-endpoints.md`                 | Danh mục REST API: HTTP method, URL, mục đích, parameters, request/response, status code và quyền truy cập | `docs/api/`                       |
| `api-contract.md`                  | Quy ước giao tiếp frontend/backend: JSON, cấu trúc lỗi, pagination, validation, authentication, authorization, định dạng ngày giờ/tiền tệ | `docs/api/`                       |
| `dto-specification.md`             | Đặc tả chi tiết Request DTO / Response DTO cho từng API                                  | `docs/api/`                       |
| `customer-user-flow.md`            | Hành trình Customer từ truy cập website đến đặt vé thành công (trình tự bước, nhánh thành công/thất bại) | `docs/ui-ux/`                     |
| `customer-screen-specification.md` | Chức năng, nội dung, dữ liệu hiển thị, nút thao tác, validation và tiêu chí nghiệm thu từng màn hình | `docs/ui-ux/`                     |
| `customer-navigation.md`           | Quy tắc điều hướng giữa các màn hình: header, menu, footer, back/forward, khi chưa đăng nhập | `docs/ui-ux/`                     |
| `customer-web-layout.md`           | Bố cục tổng thể website Customer: header, main content, footer, container, grid, sidebar | `docs/ui-ux/`                     |
| `customer-component-specification.md` | Đặc tả UI component tái sử dụng: button, input, movie card, seat, modal, dropdown, loading | `docs/ui-ux/`                     |
| `customer-ui-state.md`             | Trạng thái giao diện và phản ứng UI: loading, success, error, empty, disabled, selected, giữ ghế | `docs/ui-ux/`                     |
| `customer-responsive-specification.md` | Cách giao diện thích ứng theo màn hình: breakpoint, bố cục desktop/tablet/mobile     | `docs/ui-ux/`                     |
| `customer-screen-api-mapping.md`   | Liên kết từng màn hình với API endpoint, thời điểm gọi, dữ liệu trả về và xử lý lỗi      | `docs/ui-ux/`                     |
| `local-development-setup.md`       | Hướng dẫn cài đặt, cấu hình và chạy toàn bộ dự án: phần mềm, biến môi trường, migration, seed, xử lý lỗi | `docs/development/`               |
| `git-workflow.md`                  | Quy định cách đặt tên branch, viết commit, tạo PR và merge code                          | `docs/management/`                |
| Các file trong `ai-usage/`         | Ghi lại log làm việc của từng AI Agent/thành viên theo ID                                | `docs/management/ai-usage/`       |
| `docs/management/weekly-reports/`  | Báo cáo tuần (chưa tạo, dự kiến bổ sung theo quy trình nhóm)                             | `docs/management/`                |
| Các file trong `pa/PA0`–`PA5`      | Lưu sản phẩm, báo cáo và tài liệu tương ứng từng bài PA                                  | `pa/`                             |
| Các file trong `docs/test/`        | Lưu kế hoạch kiểm thử, test case, kết quả và báo cáo lỗi (chưa tạo)                      | `docs/test/`                      |
| `seed-data.sql`                    | Script seed dữ liệu demo: role, genre, seat type, cinema, phòng, ghế, phim, suất chiếu   | `database/`                       |
| `seed-README.md`                   | Hướng dẫn chạy seed, thứ tự thực hiện và báo cáo xác minh dữ liệu                        | `database/`                       |
| `movie-research.md`                | Ghi chú kết quả tra cứu thông tin phim (thời lượng, poster, trailer) dùng cho seed       | `database/`                       |
| `identity.md` / `mandatory-instruction.md` | Định danh nhóm và quy tắc bắt buộc khi làm việc với AI agent                     | Root                              |
| `Cinema_292_Tasks_..._Sprints.xlsx`| Bảng phân công công việc nhóm theo 5 thành viên / 5 sprint                               | `docs/`                           |

# Phân biệt các file dễ bị trùng lặp (nhóm UI/UX)

- `customer-user-flow.md` — Đi đâu, theo trình tự nào? Ví dụ: Home → Movie Detail → Showtime → Seat Selection → Booking Confirmation → Payment.
- `customer-screen-specification.md` — Mỗi màn hình có gì? Ví dụ: Movie Detail hiển thị poster, tên phim, thời lượng, mô tả và nút xem suất chiếu.
- `customer-navigation.md` — Chuyển giữa các màn hình bằng cách nào? Ví dụ: nhấn "Chọn suất chiếu" mở màn hình Showtime với phim đang chọn.
- `customer-component-specification.md` — Các thành phần giao diện hoạt động ra sao? Ví dụ: Movie Card có poster, tên phim, nhãn phân loại và nút xem chi tiết.
- `customer-ui-state.md` — Giao diện phản ứng thế nào theo trạng thái? Ví dụ: tải danh sách phim thì hiện loading; không có phim thì hiện empty state.
- `customer-screen-api-mapping.md` — Dữ liệu trên màn hình lấy từ đâu? Ví dụ: Movie List gọi API lấy danh sách phim rồi hiển thị dữ liệu trả về.

Phân biệt nhóm API: `api-endpoints.md` trả lời "Hệ thống có những API nào?", còn `api-contract.md` trả lời "Các API phải tuân theo những quy tắc giao tiếp nào?".

Phân biệt nhóm Database: `database-documentation.md` (nếu tạo) là tài liệu giải thích ở góc độ sử dụng/bảo trì, không thay thế `database-schema.sql` (script thực thi), `database-design.md` / `database-schema.md` (đặc tả thiết kế) và `seed-data.sql` (dữ liệu mẫu).

# Quan hệ giữa các nhóm tài liệu

```text
UI/UX (user flow, màn hình, component, trạng thái)
        │
        ▼
Screen–API Mapping   →   xác định dữ liệu mỗi màn hình cần
        │
        ▼
API Contract + API Endpoints   →   thống nhất endpoint, request, response và quyền truy cập
        │
        ▼
Architecture + Database   →   định hướng triển khai và xác định nguồn dữ liệu
        │
        ▼
Local Development Setup   →   hướng dẫn chạy và kiểm thử hệ thống
```

Đây là quan hệ phụ thuộc về mặt thiết kế, không phải thứ tự bắt buộc tuyệt đối — API và database cần được đối chiếu qua lại khi phát hiện nghiệp vụ chưa rõ. Với các quyết định nghiệp vụ chưa được nhóm thống nhất, ghi rõ trạng thái `Open` hoặc `Proposed` thay vì xem phương án tạm thời là đặc tả chính thức.
