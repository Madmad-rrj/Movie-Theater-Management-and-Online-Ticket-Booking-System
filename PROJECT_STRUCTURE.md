# Project Structure

```text
Movie Theater Management and Online Ticket Booking System/
│
├── AGENTS.md                               # Hướng dẫn làm việc cho AI Agent trong dự án
├── docker-compose.yml                     # Cấu hình chạy các service bằng Docker
├── .gitignore                             # Danh sách file/thư mục bị Git bỏ qua
├── PROJECT_STRUCTURE.md                   # Mô tả cấu trúc thư mục và mục đích từng thành phần
├── README.md                              # Giới thiệu dự án, công nghệ và hướng dẫn cài đặt/chạy
│
├── .vscode/                               # Cấu hình editor dùng chung cho nhóm
│   └── settings.json                      # Cài đặt workspace VS Code
│
├── database/                              # Tài liệu thiết kế và script cơ sở dữ liệu
│   ├── database-design.md                 # Mô tả thiết kế database và các quyết định thiết kế
│   ├── database-schema.md                 # Mô tả schema: bảng, cột, khóa và quan hệ
│   └── database-schema.sql                # SQL tạo cấu trúc database
│
├── docs/                                  # Tài liệu phân tích, thiết kế, quản lý và kiểm thử
│   │
│   ├── analysis and design/               # Tài liệu phân tích chi tiết và thiết kế hệ thống
│   │   ├── architecture.md                # Kiến trúc tổng thể và cách các thành phần phối hợp
│   │   ├── business-system-design.md      # Thiết kế hệ thống, UML và luồng tương tác
│   │   └── relational-model.md            # Mô hình dữ liệu quan hệ, bảng và khóa
│   │
│   ├── management/                        # Tài liệu quản lý dự án và quy trình làm việc
│   │   ├── git-workflow.md                # Quy tắc branch, commit, pull request và merge
│   │   └── ai-usage/                     # Log sử dụng AI theo thành viên và theo prompt
│   │       ├── 0098.md                    # Log làm việc của agent 0098
│   │       ├── 0235.md                    # Log làm việc của agent 0235
│   │       ├── 0244.md                    # Log làm việc của agent 0244
│   │       ├── 0255.md                    # Log làm việc của agent 0255
│   │       ├── 0271.md                    # Log làm việc của agent 0271
│   │       └── YYYY-MM-DD-prompt-NN-*.md # Nhật ký từng yêu cầu; tóm tắt prompt, phần việc AI và kết quả
│   │
│   ├── requirements/                      # Tài liệu yêu cầu và phân tích nghiệp vụ
│   │   ├── business-system-analysis.md    # Phạm vi, actor, quy trình và yêu cầu hệ thống
│   │   └── use-cases.md                   # Use Case Diagram và/hoặc đặc tả các Use Case
│   │
│   └── test/                              # Tài liệu kiểm thử (chưa tạo)
│       # Dự kiến bổ sung test plan, test cases và test reports
│
├── pa/                                    # Các tài liệu và sản phẩm nộp theo từng PA
│   └── PA0/                               # Sản phẩm của PA0
│       └── 24120235-24120271-24120098-24120244-24120255.pdf # Báo cáo nộp PA0 (nhóm)
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
| `git-workflow.md`                  | Quy định cách đặt tên branch, viết commit, tạo PR và merge code                          | `docs/management/`                |
| Các file trong `ai-usage/`         | Ghi lại việc sử dụng AI theo thành viên hoặc từng prompt; phân biệt yêu cầu, phần AI thực hiện, kết quả và kiểm tra | `docs/management/ai-usage/` |
| `docs/management/weekly-reports/`  | Báo cáo tuần (chưa tạo, dự kiến bổ sung theo quy trình nhóm)                             | `docs/management/`                |
| Các file trong `pa/PA0`–`PA5`      | Lưu sản phẩm, báo cáo và tài liệu tương ứng từng bài PA                                  | `pa/`                             |
| Các file trong `docs/test/`        | Lưu kế hoạch kiểm thử, test case, kết quả và báo cáo lỗi                                 | `docs/test/`                      |
