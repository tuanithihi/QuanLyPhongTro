# 🏡 HỆ THỐNG QUẢN LÝ PHÒNG TRỌ THÔNG MINH - QuanLyPhongTro

> **Đồ Án Chuyên Ngành**  
> Dự án xây dựng website quản lý phòng trọ hiện đại tích hợp trợ lý ảo Trí tuệ Nhân tạo (AI Chatbot) và hệ thống tạo hợp đồng, hóa đơn điện tử tự động.

---

## 🌟 Tổng Quan Dự Án

**QuanLyPhongTro** là một ứng dụng web toàn diện được xây dựng trên nền tảng **ASP.NET Core 8.0 MVC** và **SQL Server**. Hệ thống phục vụ hai đối tượng người dùng chính: **Chủ trọ (Landlord/Admin)** trong việc tối ưu hóa quản lý phòng, người thuê, hợp đồng, dịch vụ, tài chính; và **Khách thuê (Tenants/Clients)** trong việc tìm kiếm phòng, gửi yêu cầu đặt chỗ, thanh toán hóa đơn nhanh chóng bằng mã QR và tương tác trực tuyến qua Live Chat cũng như Trợ lý ảo AI 24/7.

---

## 🛠️ Công Nghệ Sử Dụng

Dự án áp dụng các công nghệ, thư viện và mô hình thiết kế hiện đại nhằm đảm bảo hiệu năng, tính bảo mật và trải nghiệm người dùng cao nhất:

### 1. Backend (Core Logic)
*   **Framework:** ASP.NET Core 8.0 (MVC Pattern)
*   **Database ORM:** Entity Framework Core (EF Core) SqlServer
*   **Database Engine:** Microsoft SQL Server
*   **AI Integration:** **Groq API Client** (tích hợp mô hình ngôn ngữ lớn LLM Llama3/Mixtral để làm Chatbot tự động trả lời khách hàng).
*   **File Template Engine:** **MiniWord** (hỗ trợ tạo và xuất file hợp đồng/hóa đơn Microsoft Word `.docx` cực nhanh bằng cách điền thông tin tự động vào mẫu có sẵn).

### 2. Frontend & UI/UX
*   **Theme & Styling:** Bootstrap 5, Custom Vanilla CSS.
*   **Icons:** Bootstrap Icons.
*   **WYSIWYG Editor:** Summernote (dành cho viết bài viết tin tức, mô tả phòng trọ).
*   **Media Manager:** **elFinder.NetCore** (giao diện quản lý tệp tin chuyên nghiệp như Google Drive tích hợp trực tiếp vào trang quản trị).

### 3. Payment & Automation
*   **VietQR Code Generator:** Tích hợp cơ chế tự động tạo mã QR chuyển khoản nhanh qua ngân hàng (theo chuẩn NAPAS VietQR) dựa trên hóa đơn tiền phòng hàng tháng của khách thuê.

---

## ✨ Các Tính Năng Nổi Bật

### 🧑‍💻 Dành Cho Khách Thuê (Client Portal)
1.  **Tìm Kiếm Phòng Trọ Thông Minh:** Giao diện trực quan hiển thị danh sách phòng, chi tiết phòng (diện tích, giá cả, dịch vụ đi kèm, tiện ích).
2.  **Đặt Phòng Tiện Lợi:** Gửi yêu cầu đặt phòng trực tuyến thông qua form đăng ký (`tblBookingRequest`).
3.  **Trợ Lý Ảo AI Tư Vấn 24/7:** Tích hợp AI Chatbot sử dụng mô hình AI của **Groq** giúp tư vấn giá phòng, nội quy nhà trọ và giải đáp thắc mắc của khách tức thời.
4.  **Hộp Thoại Live Chat:** Khách hàng có thể nhắn tin trực tiếp trong thời gian thực (Real-time Chat) với chủ trọ khi có câu hỏi sâu hơn.
5.  **Quản Lý Tài Khoản Khách Thuê:** Xem thông tin cá nhân, cập nhật hồ sơ, đổi mật khẩu bảo mật và xem lịch sử hóa đơn.

### 🛡️ Dành Cho Chủ Trọ (Admin Dashboard)
1.  **Trang Tổng Quan (Dashboard Analytics):** Thống kê doanh thu theo tháng, số lượng phòng trống/đang thuê, số lượng khách thuê đang hoạt động và biểu đồ tài chính trực quan.
2.  **Quản Lý Phòng & Loại Phòng:** 
    *   Thêm mới, sửa, xóa phòng trọ (`tblRoom`).
    *   Phân loại phòng (`tblRoomType`) như phòng đơn, phòng đôi, căn hộ dịch vụ,...
3.  **Quản Lý Khách Thuê (Tenants):** Lưu trữ thông tin định danh cá nhân, số điện thoại, tiền đặt cọc và quản lý tài khoản của khách thuê.
4.  **Tự Động Xuất Hợp Đồng Thuê Nhà (DOCX):**
    *   Hỗ trợ điền tự động dữ liệu từ hệ thống vào file mẫu Word chuẩn của chủ trọ.
    *   Tải xuống file `.docx` chỉ bằng 1 cú click để ký kết.
5.  **Tự Động Tính Hóa Đơn & Xuất Hóa Đơn:**
    *   Nhập số điện, số nước tiêu thụ hàng tháng của từng phòng.
    *   Hệ thống tự tính toán tổng tiền dựa trên đơn giá dịch vụ cài đặt (`tblService`).
    *   Tự tạo mã **VietQR** động chứa số tiền, nội dung chuyển khoản và số tài khoản của chủ trọ để khách thuê quét mã thanh toán ngay trên điện thoại.
    *   Hỗ trợ xuất hóa đơn thanh toán ra file Word `.docx` chuyên nghiệp gửi khách hàng.
6.  **Hệ Thống Trò Chuyện Trực Tuyến (Live Chat Dashboard):** Kênh hỗ trợ khách hàng tập trung giúp chủ trọ trả lời tất cả các phiên chat của khách thuê ngay lập tức.
7.  **Quản Lý Bài Viết & Tin Tức (Blog Management):** Viết bài đăng tin tức, khuyến mãi, kinh nghiệm thuê trọ tích hợp trình quản lý ảnh elFinder.
8.  **Quản Lý Quản Trị Viên (System Users):** Quản lý quyền truy cập của các admin phụ trách hệ thống.

---

## 📂 Cấu Trúc Thư Mục Dự Án

```text
QuanLyPhongTro/
├── Areas/
│   └── Admin/                    # Phân vùng trang quản trị (Dashboard, Quản lý nghiệp vụ)
│       ├── Controllers/          # Các bộ điều hướng dành cho Admin (Hóa đơn, Hợp đồng, Phòng trọ...)
│       ├── Data/                 # Lớp ngữ cảnh DB (DataContext.cs) và Migrations
│       ├── Models/               # ViewModel phục vụ trang Admin
│       └── Views/                # Giao diện quản trị Admin (Razor Views)
├── Components/                   # Các ViewComponent dùng chung (Menus, RecentPosts...)
├── Controllers/                  # Các bộ điều hướng cho trang Client (Home, Account, AIChat, Chat...)
├── Models/                       # Các thực thể cơ sở dữ liệu (tblRoom, tblContract, tblInvoice...) và ViewModels khách
├── Services/                     # Các dịch vụ bên ngoài (GroqService.cs kết nối AI)
├── Utilities/                    # Các hàm tiện ích dùng chung
├── Views/                        # Giao diện dành cho người dùng cuối (Client Razor Views)
├── wwwroot/                      # Các tài nguyên tĩnh (CSS, JS, Hình ảnh, Thư viện Frontend)
├── appsettings.json              # Cấu hình kết nối DB, Ngân hàng nhận tiền, API AI
├── Program.cs                    # Điểm khởi chạy cấu hình Service & Middleware của ASP.NET Core
└── QuanLyPhongTro.sln            # Solution file của dự án
```

---

## 🛢️ Thiết Kế Cơ Sở Dữ Liệu (Database Schema)

Các bảng dữ liệu cốt lõi hỗ trợ luồng vận hành của hệ thống:

| Tên Bảng | Vai Trò chính | Các Trường Quan Trọng |
| :--- | :--- | :--- |
| **`tblUser`** | Tài khoản quản trị viên hệ thống | `Username`, `PasswordHash`, `FullName`, `Role`, `IsActive` |
| **`tblTenant`** | Hồ sơ khách thuê phòng | `TenantId`, `FullName`, `IdentityNumber`, `Phone`, `Email`, `IsActive` |
| **`tblRoomType`** | Loại phòng trọ | `TypeId`, `TypeName`, `Description`, `BasePrice` |
| **`tblRoom`** | Thông tin phòng trọ | `RoomId`, `RoomNumber`, `Area`, `Price`, `Status` (Available/Rented/Maintenance) |
| **`tblContract`** | Hợp đồng thuê phòng trọ | `ContractId`, `RoomId`, `TenantId`, `StartDate`, `EndDate`, `Deposit`, `MonthlyRent` |
| **`tblInvoice`** | Hóa đơn tiền phòng hàng tháng | `InvoiceId`, `ContractId`, `InvoiceDate`, `TotalAmount`, `IsPaid`, `PaymentDate` |
| **`tblService`** | Biểu giá dịch vụ (Điện, Nước...) | `ServiceId`, `ServiceName`, `UnitPrice`, `UnitType` (kWh, m3, người...) |
| **`tblBookingRequest`** | Yêu cầu đặt thuê phòng của khách | `RequestId`, `FullName`, `Phone`, `Email`, `RoomId`, `Status` (Pending/Approved) |
| **`tblChatSession`** | Phiên trò chuyện khách trọ - chủ trọ | `SessionId`, `SessionKey`, `RenterName`, `IsOpen`, `CreatedAt` |
| **`tblChatMessage`** | Tin nhắn trong phiên chat | `MessageId`, `SessionId`, `Sender`, `Content`, `SentAt` |

---

## 🚀 Hướng Dẫn Cài Đặt & Khởi Chạy

Làm theo các bước sau để thiết lập dự án trên máy tính cá nhân của bạn:

### 1. Yêu Cầu Cài Đặt Hệ Thống

Dự án này là một ứng dụng Web chạy trên nền tảng **ASP.NET Core**, để cài đặt và khởi chạy bạn chỉ cần chuẩn bị các công cụ sau:

*   **.NET 8.0 SDK** ([Tải xuống tại đây](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)) - Trình biên dịch và chạy ứng dụng ASP.NET Core.
*   **Microsoft SQL Server** ([Tải xuống bản Express tại đây](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)) - Hệ quản trị cơ sở dữ liệu để lưu trữ thông tin hệ thống.
*   **SQL Server Management Studio (SSMS)** ([Tải xuống tại đây](https://learn.microsoft.com/en-us/sql-server/ssms/download-sql-server-management-studio-ssms)) - Công cụ quản trị và trực quan hóa cơ sở dữ liệu trực quan.
*   **IDE Phát triển:** Visual Studio 2022 (chọn workload *ASP.NET and web development*) hoặc VS Code (cài extension *C# Dev Kit*).

### 2. Cấu Hình Ứng Dụng (`appsettings.json`)
Mở file [appsettings.json](file:///e:/VSCode/Đồ%20Án%20Chuyên%20Ngành/QuanLyPhongTro/appsettings.json) và điều chỉnh cấu hình phù hợp với máy của bạn:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=YOUR_SERVER_NAME;Initial Catalog=QUANLYPHONGTRO;Integrated Security=True;TrustServerCertificate=True"
  },
  "BankPayment": {
    "BankId": "970422",                  // Mã ngân hàng MB Bank (theo VietQR)
    "AccountNumber": "08052005666888",     // Số tài khoản của chủ trọ nhận tiền
    "AccountName": "HO DUC TUAN",         // Tên chủ tài khoản
    "BankName": "MB Bank"
  },
  "GroqApi": {
    "KeyFilePath": "E:\\VSCode\\DemoApp\\API\\groq_api_key.txt" // File chứa API Key của Groq AI
  },
  "Landlord": {
    "Name": "Hồ Đức Tuấn",
    "IdentityNumber": "047203003298",
    "Phone": "0354296138",
    "Address": "Thành Phố Vinh, Tỉnh Nghệ An"
  }
}
```

### 3. Tạo Cơ Sở Dữ Liệu & Migrations
Mở terminal tại thư mục gốc của dự án (`QuanLyPhongTro/`) và chạy lệnh để khởi tạo database từ Entity Framework Core:

```powershell
# Cài đặt công cụ dotnet-ef nếu chưa có
dotnet tool install --global dotnet-ef

# Áp dụng các Migration có sẵn vào cơ sở dữ liệu SQL Server
dotnet ef database update
```

### 4. Khởi Chạy Hệ Thống
Chạy lệnh sau để khởi động máy chủ Web Development cục bộ:

```powershell
dotnet run
```

Sau khi chạy thành công, mở trình duyệt web và truy cập địa chỉ mặc định được hiển thị trên console:
*   Trang khách hàng: `https://localhost:7082` hoặc `http://localhost:5082`
*   Trang quản trị Admin: Truy cập thông qua menu Đăng nhập của Admin.

---

## 👤 Thông Tin Tác Giả & Bản Quyền

*   **Tác giả (Chủ trọ):** Hồ Đức Tuấn
*   **Địa chỉ:** Thành Phố Vinh, Tỉnh Nghệ An
*   **Số điện thoại:** 0354296138
*   **Ngân hàng nhận thanh toán:** MB Bank - 08052005666888 (Tên TK: HO DUC TUAN)
*   **Bản quyền:** Đồ án chuyên ngành thuộc Hồ Đức Tuấn. Nghiêm cấm sao chép thương mại khi chưa được sự cho phép.

---
*Chúc các bạn có những trải nghiệm tuyệt vời với Hệ thống Quản lý Phòng trọ Thông minh!*
