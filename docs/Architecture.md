# Architecture

## 1. Tổng quan kiến trúc

// Mô tả kiến trúc tổng thể của hệ thống và cách các thành phần chính
// phối hợp với nhau.

## 2. Kiến trúc hệ thống

// Mô tả các layer/component chính của hệ thống.

// Ví dụ:
//
// Customer / Staff / Admin
//        ↓
// Frontend Web
//        ↓
// ASP.NET Core Web API
//        ↓
// Business Logic
//        ↓
// EF Core
//        ↓
// PostgreSQL

## 3. Các thành phần hệ thống

// Liệt kê các thành phần chính và trách nhiệm của từng thành phần.

// Ví dụ:
// - Frontend
// - Backend API
// - Business Logic
// - Database
// - Authentication
// - Payment Gateway
// - AI Service nếu có

## 4. Frontend Architecture

// Xác định cách tổ chức Frontend.

// Mô tả:
// - Pages
// - Components
// - Services / API Client
// - State Management
// - Models
// - Routing
// - Authentication State

## 5. Backend Architecture

// Xác định cách tổ chức Backend.

// Mô tả các layer/project chính.

// Ví dụ:
// - API / Controllers
// - Application / Services
// - Domain / Entities
// - Infrastructure / EF Core
// - Authentication
// - External Services

## 6. Database Architecture

// Mô tả cách Backend kết nối và làm việc với Database.

// Ví dụ:
// ASP.NET Core
//      ↓
// EF Core
//      ↓
// Npgsql
//      ↓
// PostgreSQL

## 7. Authentication & Authorization Architecture

// Mô tả cơ chế xác thực và phân quyền.

// Ví dụ:
// Login → Authentication → JWT → Authorization → Role

## 8. External Services

// Liệt kê các hệ thống bên ngoài mà hệ thống sử dụng.

// Ví dụ:
// - Payment Gateway
// - AI API
// - Email Service

## 9. Communication

// Xác định cách các thành phần giao tiếp với nhau.

// Ví dụ:
// Frontend → REST API → Backend
// Backend → PostgreSQL
// Backend → Payment Gateway
// Backend → AI API

## 10. Deployment Architecture

// Mô tả cách các thành phần được triển khai trên môi trường thực tế.

// Ví dụ:
//
// Browser
//    ↓
// Frontend Hosting
//    ↓
// ASP.NET Core API
//    ↓
// PostgreSQL

## 11. Project Structure

// Xác định cấu trúc repository và project.

// Ví dụ:
//
// /frontend
// /backend
// /docs
// /tests

## 12. Architecture Principles

// Xác định các nguyên tắc kiến trúc mà team phải tuân thủ.

// Ví dụ:
// - Frontend không truy cập Database trực tiếp.
// - Frontend chỉ giao tiếp với Backend thông qua API.
// - Business Logic không đặt trong Controller.
// - Database Access được thực hiện thông qua EF Core.
// - Secrets không được hard-code.
// - Các module phải có trách nhiệm rõ ràng.

## 13. Architecture Diagram

// Vẽ sơ đồ kiến trúc tổng thể của hệ thống.

// Có thể sử dụng:
// - draw.io
// - Mermaid
// - Công cụ khác