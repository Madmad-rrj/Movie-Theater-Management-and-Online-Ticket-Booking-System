# Project Structure

```text
Movie Theater Management and Online Ticket Booking System/
│
├── docker-compose.yml                  # Cấu hình chạy các service bằng Docker
├── PROJECT_STRUCTURE.md                # Mô tả cấu trúc thư mục và mục đích từng thành phần
├── README.md                           # Giới thiệu dự án, công nghệ và hướng dẫn cài đặt/chạy
│
├── database/                           # Tài liệu thiết kế và script cơ sở dữ liệu
│   ├── database-design.md               # Mô tả thiết kế database và các quyết định thiết kế
│   ├── database-schema.md               # Mô tả schema: bảng, cột, khóa và quan hệ
│   └── database-schema.sql              # SQL tạo cấu trúc database
│
├── docs/                               # Tài liệu phân tích, thiết kế, quản lý và kiểm thử
│   │
│   ├── analysis and design/             # Tài liệu phân tích chi tiết và thiết kế hệ thống
│   │   ├── architecture.md              # Kiến trúc tổng thể và cách các thành phần phối hợp
│   │   ├── business-system-design.md    # Thiết kế hệ thống, UML và luồng tương tác
│   │   └── relational-model.md          # Mô hình dữ liệu quan hệ, bảng và khóa
│   │
│   ├── management/                      # Tài liệu quản lý dự án và quy trình làm việc
│   │   ├── git-workflow.md              # Quy tắc branch, commit, pull request và merge
│   │   └── weekly-reports/              # Báo cáo công việc và tiến độ theo tuần
│   │
│   ├── requirements/                    # Tài liệu yêu cầu và phân tích nghiệp vụ
│   │   ├── business-system-analysis.md  # Phạm vi, actor, quy trình và yêu cầu hệ thống
│   │   └── use-cases.md                 # Use Case Diagram và/hoặc đặc tả các Use Case
│   │
│   └── test/                            # Tài liệu kiểm thử
│       # Dự kiến bổ sung test plan, test cases và test reports
│
├─── pa/                                 # Các tài liệu và sản phẩm nộp theo từng PA
│    ├── PA1/                            # Tài liệu/sản phẩm của PA1
│    ├── PA2/                            # Tài liệu/sản phẩm của PA2
│    ├── PA3/                            # Tài liệu/sản phẩm của PA3
│    ├── PA4/                            # Tài liệu/sản phẩm của PA4
│    └── PA5/                            # Tài liệu/sản phẩm của PA5
└── src/                                # Mã nguồn ứng dụng
    │
    ├── backend/                        # Mã nguồn backend
    │   └── CinemaTicketBooking.Api/    # ASP.NET Core Web API
    │       ├── appsettings.Development.json # Cấu hình backend cho môi trường phát triển
    │       ├── CinemaTicketBooking.Api.csproj # Khai báo project và dependency .NET
    │       ├── Program.cs               # Khởi tạo ứng dụng, DI, middleware và cấu hình API
    │       │
    │       ├── Controllers/             # Nhận HTTP request và trả HTTP response
    │       ├── Data/                    # DbContext và các thành phần truy cập dữ liệu
    │       │   └── ApplicationDbContext.cs # Khai báo DbSet và cấu hình model EF Core
    │       ├── Database/                # Cấu hình và dữ liệu khởi tạo liên quan database
    │       │   ├── Configurations/      # Cấu hình mapping entity bằng Fluent API
    │       │   ├── Migrations/          # Các migration thay đổi schema bằng EF Core
    │       │   └── Seed/                # Dữ liệu mẫu/khởi tạo ban đầu
    │       ├── DTOs/                    # Đối tượng dữ liệu dùng trong request/response API
    │       ├── Entities/                # Các entity ánh xạ dữ liệu nghiệp vụ
    │       ├── Middleware/              # Xử lý request/response xuyên suốt pipeline
    │       ├── Properties/              # Cấu hình thuộc tính chạy/debug của project
    │       ├── Services/                # Xử lý nghiệp vụ và phối hợp các thành phần
    │       └── Validators/              # Kiểm tra tính hợp lệ của dữ liệu đầu vào
    │
    └── frontend/                       # Mã nguồn giao diện người dùng
        └── cinema-ticket-booking-ui/    # Ứng dụng React + Vite + TypeScript
            ├── .env.example             # Mẫu biến môi trường, không chứa secret thật
            ├── index.html               # HTML gốc được Vite sử dụng
            ├── package.json             # Scripts, dependency và metadata của frontend
            ├── tsconfig.json            # Cấu hình TypeScript
            ├── vite.config.ts            # Cấu hình công cụ build/dev server Vite
            │
            ├── public/                  # Tài nguyên tĩnh được phục vụ trực tiếp
            └── src/                     # Mã nguồn giao diện
                ├── App.tsx               # Component gốc của ứng dụng
                ├── index.css             # CSS toàn cục
                ├── main.tsx              # Entry point khởi chạy React
                │
                ├── assets/               # Ảnh, icon và tài nguyên được import vào code
                ├── components/           # Component giao diện có thể tái sử dụng
                ├── contexts/             # React Context dùng chung state/toàn cục
                ├── hooks/                # Custom React hooks
                ├── layouts/              # Khung bố cục chung cho các trang
                ├── pages/                # Các trang/màn hình của ứng dụng
                ├── routes/               # Khai báo và điều hướng giữa các route
                ├── services/             # Gọi API backend và xử lý giao tiếp HTTP
                ├── types/                # Kiểu dữ liệu và interface TypeScript
                └── utils/                # Hàm tiện ích dùng chung
```
# Bảng mô tả mục đích các file Markdown

| File                               | Mục đích chính                                                                           | Vị trí hiện tại                   |
| ---------------------------------- | ---------------------------------------------------------------------------------------- | --------------------------------- |
| `README.md`                        | Giới thiệu dự án, công nghệ, yêu cầu môi trường và hướng dẫn chạy                        | Root                              |
| `PROJECT_STRUCTURE.md`             | Giải thích cấu trúc repository                                                           | Root                              |
| `database-design.md`               | Trình bày cách thiết kế database và các quyết định thiết kế                              | `database/`                       |
| `database-schema.md`               | Mô tả chi tiết các bảng, cột, kiểu dữ liệu và ràng buộc                                  | `database/`                       |
| `architecture.md`                  | Mô tả kiến trúc tổng thể, các tầng và cách chúng giao tiếp                               | `docs/analysis and design/`       |
| `business-system-analysis.md`      | Phân tích vấn đề, phạm vi, actor, quy trình nghiệp vụ và yêu cầu chức năng/phi chức năng | `docs/requirements/`              |
| `business-system-design.md`        | Mô tả thiết kế giải pháp, sơ đồ UML và luồng tương tác                                   | `docs/analysis and design/`       |
| `relational-model.md`              | Mô hình quan hệ dữ liệu, khóa chính, khóa ngoại và quan hệ giữa các bảng                 | `docs/analysis and design/`       |
| `use-cases.md`                     | Mô tả các Use Case, actor, điều kiện trước/sau và luồng chính/ngoại lệ                   | `docs/requirements/`              |
| `git-workflow.md`                  | Quy định cách đặt tên branch, viết commit, tạo PR và merge code                          | `docs/management/`                |
| Các file trong `weekly-reports/`   | Ghi lại công việc đã làm, kết quả, khó khăn và kế hoạch tuần tiếp theo                   | `docs/management/weekly-reports/` |
| Các file trong `docs/pa/PA1`–`PA5` | Lưu sản phẩm, báo cáo và tài liệu tương ứng từng bài PA                                  | `docs/pa/`                        |
| Các file trong `docs/test/`        | Lưu kế hoạch kiểm thử, test case, kết quả và báo cáo lỗi                                 | `docs/test/`                      |

