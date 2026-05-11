# CLAUDE.md — Dự án Quản Lý Phòng Trọ

> File này được đọc tự động khi bắt đầu mỗi session Claude Code mới.
> Cập nhật file này sau mỗi buổi làm việc.

---

## 1. Tổng quan dự án

- **Tên dự án:** QuanLyPhongTro — Website Quản lý Phòng Trọ
- **Framework:** ASP.NET Core MVC (.NET 8.0)
- **Database:** SQL Server (EF Core 9 — Database First / tạo thủ công)
- **Namespace gốc:** `QuanLyPhongTro`
- **Trạng thái:** Build thành công ✅ — `dotnet run` hoạt động ✅, Admin area hoạt động ✅, Database tạo thủ công trong SQL Server ✅

---

## 2. Tech Stack & NuGet Packages

```xml
Microsoft.EntityFrameworkCore.SqlServer   9.0.10
Microsoft.VisualStudio.Web.CodeGeneration.Design  9.0.0
PagedList.Core.Mvc                        3.0.0    ← phân trang
MailKit + MimeKit                         4.x      ← gửi email hóa đơn
SlugGenerator                            2.0.2    ← tạo slug cho bài viết
elFinder.NetCore                          1.4.0    ← file manager admin
PdfPig                                   0.1.9
SendGrid                                  9.29.3
```

---

## 3. Cấu trúc thư mục hiện tại

```
QuanLyPhongTro/
├── Areas/
│   └── Admin/
│       ├── Attributes/
│       │   └── AdminOnlyAttribute.cs       ✅ ActionFilterAttribute, kiểm tra Session["AdminUser"]
│       ├── Controllers/
│       │   ├── AccountController.cs        ✅ Login/Logout qua Session + appsettings credentials
│       │   ├── HomeController.cs           ✅ Dashboard — dùng DashboardViewModel
│       │   ├── RoomController.cs           ✅ CRUD đầy đủ (Index/Detail/Create/Edit/Delete)
│       │   ├── BookingRequestController.cs ✅ Index/UpdateStatus/Delete — inject sys msg chat khi accept/reject
│       │   ├── ChatController.cs           ✅ Index/Messages(GET)/Reply(POST)/UnreadCount
│       │   ├── LoginController.cs          ✅ Stub — redirect sang Account/Login
│       │   ├── PostController.cs           ✅ Stub — TODO implement
│       │   ├── UserController.cs           ✅ Stub — TODO implement
│       │   └── FileManagerController.cs    ✅ elFinder stub (đã fix namespace)
│       ├── Data/
│       │   └── DataContext.cs              ✅ EF Core context, tất cả entities đã register
│       ├── Models/
│       │   ├── RoomCreateViewModel.cs      ✅ dùng cho Create/Edit phòng
│       │   ├── DashboardViewModel.cs       ✅ dùng cho Dashboard
│       │   └── SummerNote.cs              ✅ helper TinyMCE (namespace đã sửa)
│       └── Views/
│           ├── Account/
│           │   ├── Login.cshtml            ✅ Trang đăng nhập mới (Session-based)
│           │   ├── ChangePassword.cshtml   ⚠️  Stub — TODO implement
│           │   ├── Info.cshtml             ⚠️  Stub — TODO implement
│           │   └── Settings.cshtml         ⚠️  Stub — TODO implement
│           ├── Home/
│           │   └── Index.cshtml            ✅ Dashboard PhongTro (4 stats + clock + shortcuts)
│           ├── Room/
│           │   ├── Index.cshtml            ✅ Danh sách + tìm kiếm + lọc + phân trang
│           │   ├── Create.cshtml           ✅ Form thêm phòng + upload ảnh preview
│           │   ├── Edit.cshtml             ✅ Form sửa phòng + thay ảnh
│           │   └── Detail.cshtml           ✅ Chi tiết + lịch sử HĐ + hóa đơn
│           ├── BookingRequest/
│           │   └── Index.cshtml            ✅ Filter + table + Accept/Reject modal
│           ├── Chat/
│           │   └── Index.cshtml            ✅ WhatsApp-style split layout (sessions + messages)
│           ├── Post/                       ⚠️  Stub (Index, Create, Edit, Delete)
│           ├── User/                       ⚠️  Stub (Index, Create, Edit, Details, Delete)
│           ├── Login/Index.cshtml          ✅ Stub redirect → Account/Login
│           ├── FileManager/Index.cshtml    (giữ lại từ cũ)
│           └── Shared/
│               ├── _LayoutAdmin.cshtml     ✅ Layout mới — Session auth, sidebar tĩnh PhongTro
│               ├── _SummerNote.cshtml      (helper)
│               ├── _ViewImports.cshtml     ✅ Namespace QuanLyPhongTro đã update
│               └── Components/AdminMenu/Default.cshtml  ✅ Stub (sidebar inline trong layout)
│
├── Components/
│   ├── MenuViewComponent.cs               ✅ Đã fix namespace → QuanLyPhongTro.Areas.Admin.Data
│   └── PostComponent.cs                   ✅ Đã fix namespace → dùng tblPost mới
│
├── Controllers/
│   ├── HomeController.cs                  ✅ Stub đơn giản (Index, Privacy, AccessDenied, Error)
│   └── ContactController.cs               ✅ Đã fix namespace → QuanLyPhongTro.Controllers
│
├── Controllers/
│   ├── HomeController.cs                  ✅
│   ├── ContactController.cs               ✅
│   └── AccountController.cs              ✅ Login (Tenant/User), Register (User), Logout
│
├── Models/                                ← CHỈ CÓ ENTITIES MỚI
│   ├── tblRoom.cs                         ✅
│   ├── tblRoomType.cs                     ✅
│   ├── tblTenant.cs                       ✅ + Username(nullable) + PasswordHash(nullable)
│   ├── tblUser.cs                         ✅ người dùng website (Username, Email, PasswordHash...)
│   ├── tblContract.cs                     ✅
│   ├── tblService.cs                      ✅
│   ├── tblInvoice.cs                      ✅
│   ├── tblInvoiceDetail.cs                ✅
│   ├── tblPost.cs                         ✅
│   ├── tblMenu.cs                         ✅
│   ├── tblBookingRequest.cs               ✅ + enum BookingRequestType, BookingRequestStatus
│   ├── tblChatSession.cs                  ✅ SessionKey(GUID), GuestName, GuestPhone, IsOpen, LastMsgAt
│   ├── tblChatMessage.cs                  ✅ + enum ChatSenderType(Guest=0/Admin=1/System=2)
│   ├── LoginViewModel.cs                  ✅ UsernameOrEmail + Password
│   ├── RegisterViewModel.cs               ✅ cho đăng ký người dùng thường
│   └── ErrorViewModel.cs                  ✅
│
├── Migrations/                            ❌ Đã xóa — DB tạo thủ công bằng SQL script
│
├── Services/                              ❌ Chưa tạo
│
├── Utilities/
│   ├── Functions.cs                       ✅ namespace đã fix → QuanLyPhongTro.Utilities
│   └── EmailHelper.cs                     ✅ namespace đã fix → QuanLyPhongTro.Utilities
│
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml                   ✅ Stub đơn giản PhongTro (trang chủ khách — cần làm đẹp sau)
│   │   ├── AccessDenied.cshtml            (giữ lại)
│   │   ├── Details.cshtml                 ⚠️  Stub
│   │   └── Privacy.cshtml                 (giữ lại)
│   ├── Account/                           ✅ Login (2 tabs) + Register (người dùng)
│   ├── Contact/Index.cshtml               (giữ lại)
│   ├── Shared/
│   │   ├── _Layout.cshtml                 ⚠️  Cũ — cần làm lại layout trang khách PhongTro (đã xóa _Chatbot partial)
│   │   ├── Error.cshtml                   ✅
│   │   ├── _ValidationScriptsPartial.cshtml ✅
│   │   └── Components/
│   │       ├── Category/Default.cshtml    ⚠️  Stub (cũ)
│   │       ├── MenuView/Default.cshtml    ✅ Static nav PhongTro (đơn giản)
│   │       └── Post/Default.cshtml        ✅ Stub — TODO implement
│   ├── _ViewImports.cshtml                ✅ Namespace QuanLyPhongTro đã update
│   └── _ViewStart.cshtml                  ✅
│
├── Program.cs                             ✅ Session + DataContext + Areas routing
├── appsettings.json                       ✅ DB: QUANLYPHONGTRO (không còn Admin credentials)
└── CLAUDE.md                              ← file này
```

---

## 4. Authentication — Tất cả 3 loại người dùng

> **Trang login duy nhất:** `GET/POST /Account/Login` — hệ thống tự nhận dạng loại tài khoản.
> Thứ tự kiểm tra: **Tenant → User → Admin**

### Admin (Chủ phòng / Quản trị viên)
- **Session key:** `"AdminUser"` (string — tên đăng nhập)
- **Credentials:** lưu trong `tblUser` với `Role = "Admin"` — **1 tài khoản duy nhất, seed trong DB**
- **Login URL:** `GET/POST /Account/Login` → sau khi đăng nhập redirect về `/Admin/Home`
- **Bảo vệ routes:** `[AdminOnly]` attribute → redirect về `/Account/Login` nếu chưa đăng nhập
- **Password:** SHA256 + salt, giống các loại tài khoản khác
- **Tài khoản mặc định:** `admin` / `admin@123` (seed bằng SQL script)

### Tenant (Người thuê)
- **Session keys:** `"TenantUser"` (TenantId as string), `"TenantName"` (FullName)
- **Lookup:** `tblTenant` — so khớp Username/Email + PasswordHash (SHA256)
- **URL:** `GET/POST /Account/Login`
- **Lưu ý:** Admin tạo tài khoản cho tenant qua TenantController (set Username + PasswordHash)

### Người dùng bình thường (Normal User)
- **Session keys:** `"NormalUser"` (UserId as string), `"UserName"` (Username)
- **Lookup:** `tblUser` với `Role = "User"` — so khớp Username/Email + PasswordHash (SHA256)
- **URL:** `GET/POST /Account/Login`, `GET/POST /Account/Register`
- **Tự đăng ký:** có trang Register riêng tại `/Account/Register`

### Password Hashing
- Algorithm: **SHA256** + static salt `"PhongTro@2026#Salt"`
- Helper: `private static string HashPassword(string password)` trong `Controllers/AccountController.cs`

---

## 5. Database Schema — Entities đã thiết kế

### `tblRoomType` (Loại phòng)
```
RoomTypeId (PK) | RoomTypeName (UNIQUE) | Description | SortOrder | IsActive | CreatedAt
```

### `tblRoom` (Phòng trọ)
```
RoomId (PK) | RoomCode (UNIQUE) | RoomName | RoomTypeId (FK) |
RoomPrice decimal(18,2) | DefaultDeposit decimal(18,2) |
Area double | Floor int | MaxOccupants int |
Description | ThumbnailImage |
Status enum(Available=0, Occupied=1, Maintenance=2) |
IsPublished bool | CreatedAt | UpdatedAt
```

### `tblTenant` (Người thuê)
```
TenantId (PK) | FullName | IdentityNumber (UNIQUE) |
Phone | Email | DateOfBirth DateOnly | Gender |
PermanentAddress | IdentityFrontImage | IdentityBackImage | Avatar |
Username (UNIQUE, nullable) | PasswordHash (nullable) |
IsActive | CreatedAt | UpdatedAt
```

### `tblUser` (Người dùng website)
```
UserId (PK) | Username (UNIQUE) | Email (UNIQUE) | PasswordHash |
FullName | Phone | Avatar |
Role NVARCHAR(20) DEFAULT 'User'  ← "Admin" | "User" |
IsActive | CreatedAt | UpdatedAt
```

### `tblContract` (Hợp đồng)
```
ContractId (PK) | ContractCode (UNIQUE) |
RoomId (FK→tblRoom) | TenantId (FK→tblTenant) |
StartDate | EndDate? |
MonthlyRent decimal(18,2) | Deposit decimal(18,2) |
PaymentDayOfMonth int | InitialElectricIndex | InitialWaterIndex |
Terms | Notes |
Status enum(Active=1, Expired=0, Terminated=2) |
ActualEndDate? | TerminationReason |
CreatedAt | UpdatedAt
```

### `tblService` (Dịch vụ)
```
ServiceId (PK) | ServiceName |
ServiceType enum(Electric=1,Water=2,Garbage=3,Wifi=4,Parking=5,Other=99) |
PricingMethod enum(PerUnit=1, FixedMonthly=2) |
UnitPrice decimal(18,2) | Unit (kWh/m³/tháng) |
Description | IsActive | CreatedAt | UpdatedAt
```

### `tblInvoice` (Hóa đơn)
```
InvoiceId (PK) | InvoiceCode (UNIQUE) |
RoomId (FK) | ContractId (FK) |
BillingMonth int | BillingYear int | DueDate |
ElectricIndexStart | ElectricIndexEnd |
WaterIndexStart | WaterIndexEnd |
RoomRentAmount | TotalServiceAmount | Discount | TotalAmount — tất cả decimal(18,2) |
Status enum(Unpaid=0, Paid=1, Overdue=2) |
PaidDate? | PaymentMethod | Notes |
CreatedAt | UpdatedAt
```

### `tblInvoiceDetail` (Chi tiết hóa đơn)
```
InvoiceDetailId (PK) |
InvoiceId (FK→tblInvoice, CASCADE DELETE) |
ServiceId (FK→tblService, RESTRICT) |
Description | Quantity double |
UnitPrice decimal(18,2) | Amount decimal(18,2)
```

### `tblPost` (Bài viết thông báo)
```
PostId (PK) | Title | Slug (UNIQUE) | Summary |
Content | ThumbnailImage | Category |
IsPinned | IsPublished | PublishedAt? | ViewCount |
MetaTitle | MetaDescription | CreatedAt | UpdatedAt
```

### `tblMenu` (Menu điều hướng — self-referencing)
```
MenuId (PK) | MenuName | Url | Icon |
ParentMenuId (FK→self, RESTRICT) |
SortOrder | Position (header/footer/sidebar) |
OpenNewTab | IsActive | CreatedAt
```

---

## 6. DataContext — `Areas/Admin/Data/DataContext.cs`

Key decisions trong `OnModelCreating`:
- `tblRoom.RoomCode` → Unique index
- `tblRoomType.RoomTypeName` → Unique index
- `tblTenant.IdentityNumber` → Unique index
- `tblTenant.Username` → Unique index (filtered: IS NOT NULL)
- `tblUser.Username` → Unique index
- `tblUser.Email` → Unique index
- `tblContract.ContractCode` → Unique index
- `tblInvoice.InvoiceCode` → Unique index
- `tblPost.Slug` → Unique index
- `InvoiceDetail → Invoice`: **Cascade Delete**
- Tất cả FK còn lại: **Restrict**

---

## 7. RoomController — `Areas/Admin/Controllers/RoomController.cs`

Inject: `DataContext`, `IWebHostEnvironment`, `ILogger<RoomController>`

Actions đã implement đầy đủ:
- `Index` — list + search (RoomCode/RoomName) + filter (RoomTypeId, Status) + phân trang thủ công
- `Detail` — include RoomType, Contracts(+Tenant), Invoices
- `Create` GET/POST — upload ảnh → `wwwroot/images/rooms/`
- `Edit` GET/POST — xử lý thay ảnh (xóa cũ, lưu mới)
- `Delete` POST — **chặn xóa nếu có ContractStatus.Active**

Views tương ứng: `Areas/Admin/Views/Room/` — Index, Create, Edit, Detail ✅

---

## 8. Để chạy dự án

```bash
# ✅ Database "QUANLYPHONGTRO" tạo thủ công trong SQL Server bằng SQL script
# ✅ Migrations đã xóa — không dùng EF migrations nữa

# Chỉ cần chạy ứng dụng:
dotnet run
```

**URL Admin:** `http://localhost:5025/Admin/Account/Login`
**Tài khoản mặc định:** `admin` / `admin@123`
**Port mặc định:** `5025` (HTTP) — xem `Properties/launchSettings.json`

> ⚠️ Đổi password trong `appsettings.json` trước khi deploy!
> ⚠️ Connection string: `Data Source=LAPTOP-TUAN\\SQLEXPRESS` — đổi nếu chạy máy khác

---

## 9. Công việc CÒN LẠI (TODO)

### Ưu tiên cao — cần làm ngay để test được
- [x] **Tạo Database thủ công** — SQL script đã lập ✅ (tự chạy trong SQL Server)
- [x] **Fix `dotnet run`** ✅ Done
- [x] **Đăng nhập 3 loại người dùng** — Admin/Tenant/NormalUser ✅ Done
- [x] **BookingRequest** — Admin xem/accept/reject + thông báo chat ✅ Done
- [x] **Global Chat widget** — floating icon + two-way chat (guest ↔ admin) ✅ Done
  - SQL: `tblChatSession`, `tblChatMessage` cần tạo thủ công (xem SQL ở cuối file)
- [ ] **TenantController** — CRUD người thuê (khi create/edit → set Username + HashPassword cho tenant)
- [ ] **ContractController** — CRUD hợp đồng (skeleton + views)
- [ ] **Tạo dữ liệu mẫu** — seed RoomType, 1-2 phòng để test

### Ưu tiên trung bình
- [ ] **InvoiceController** — CRUD hóa đơn (skeleton + views)
- [ ] **ServiceController** — CRUD dịch vụ
- [ ] **Services/InvoiceService.cs** — tính tiền điện/nước/dịch vụ
- [ ] **Services/CalculateUtilityService.cs** — tính chỉ số điện/nước
- [x] **Stub `Views/Home/Index.cshtml`** — trang chủ PhongTro đơn giản ✅ Done (cần làm đẹp sau)
- [ ] **Làm lại `Views/Shared/_Layout.cshtml`** — layout trang khách

### Ưu tiên thấp
- [ ] **RoomTypeController** — CRUD loại phòng
- [ ] **PostController hoàn chỉnh** — CRUD thông báo/bài viết
- [ ] **ViewComponents:** `RoomCategory`, `RecentPosts`
- [ ] **Trang khách:** `Views/Room/Index.cshtml`, `Views/Room/Detail.cshtml`
- [ ] **SEO + phân trang** trang khách

---

## 10. Convention quan trọng

- **Namespace:** `QuanLyPhongTro.Areas.Admin.Controllers`, `QuanLyPhongTro.Models`, v.v.
- **Entity naming:** prefix `tbl` (tblRoom, tblContract...)
- **ViewModel naming:** `[Noun]CreateViewModel`, `[Noun]EditViewModel`
- **Controller in Admin:** luôn có `[Area("Admin")]` + `[AdminOnly]` (trừ AccountController)
- **Image upload:** lưu vào `wwwroot/images/[entity]/`, trả về path tương đối `/images/rooms/guid.ext`
- **Nullable:** `Nullable enable` — dùng `string?` cho optional strings
- **Delete:** chỉ dùng `[HttpPost]` + `[ValidateAntiForgeryToken]`, không dùng `[HttpGet]`
- **Session keys:** `"AdminUser"` (admin), `"TenantUser"` + `"TenantName"` (tenant), `"NormalUser"` + `"UserName"` (user thường)
- **TempData keys:** `"Success"` và `"Error"`
- **Phân trang:** thủ công qua ViewBag (Page, PageSize, TotalItems, TotalPages) — không dùng PagedList
- **Dropdown lọc:** dùng `asp-items="@((SelectList)ViewBag.XxxList)"` trên `<select>`, tránh C# ternary trong attribute `<option>` (gây RZ1031)

---

## 11. Lưu ý kỹ thuật đã xử lý

- **`@page` conflict:** Đổi tên biến `page` → `currentPage` trong Razor view để tránh Razor parser nhầm với `@page` directive của Razor Pages.
- **RZ1031:** Không dùng `@(expr ? "selected" : "")` trong `<option>` attribute → dùng `asp-items` với `SelectList` có sẵn selected value.
- **Session:** Phải gọi `UseSession()` **SAU** `UseRouting()` và **TRƯỚC** `UseAuthorization()` trong `Program.cs`.
- **DataContext location:** `Areas/Admin/Data/DataContext.cs` — không phải root Models.
- **ViewComponent bị xóa:** `RecentBook`, `Author`, `Publisher` đã bị xóa khỏi dự án — `Views/Home/Index.cshtml` đã được thay bằng stub mới, không còn gọi các component này.
- **`_Chatbot` partial bị xóa:** Đã xóa `@await Html.PartialAsync("_Chatbot")` khỏi `_Layout.cshtml` — file `_Chatbot.cshtml` không còn tồn tại.
- **Build warnings (không nghiêm trọng):**
  - NU1902: MimeKit 4.14.0 có moderate vulnerability (không ảnh hưởng runtime)
  - CS8618: nullable field trong EmailHelper
  - CS8602: nullable dereference trong Category/Default.cshtml (view cũ)

---

## 12. Cách Claude đọc file này

File `CLAUDE.md` **được đọc tự động** khi mở Claude Code trong thư mục dự án.
Không cần làm gì thêm — chỉ cần mở terminal tại thư mục này và gõ `claude`.
