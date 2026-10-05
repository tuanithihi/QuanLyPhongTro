# 🏢 SÀN TÌM KIẾM & CHO THUÊ PHÒNG TRỌ ĐA CHỦ TRỌ (QuanLyPhongTro)

> **Dự án Đồ Án Chuyên Ngành**  
> Nền tảng công nghệ kết nối người thuê và nhiều chủ trọ trên toàn quốc, tích hợp Trợ lý Trí tuệ Nhân tạo (Groq LLM AI Chatbot), thanh toán VietQR động và hệ thống tự động xuất hợp đồng, hóa đơn Microsoft Word (.docx).

---

## 🌟 1. Tổng Quan Dự Án

**QuanLyPhongTro** là nền tảng thương mại điện tử chuyên biệt cho lĩnh vực cho thuê nhà trọ, căn hộ dịch vụ và chung cư mini đa chủ trọ. Hệ thống giải quyết bài toán tìm kiếm phòng trọ minh bạch, tiện lợi cho người thuê, đồng thời cung cấp bộ công cụ vận hành số hóa khép kín dành cho từng chủ trọ và ban quản trị sàn.

### 👥 Mô Hình 3 Vai Trò (Roles & Permissions)

1. **Super Admin (Quản trị viên sàn toàn quyền):**
   - Bảng điều khiển toàn sàn: thống kê tổng số chủ trọ, tổng số tin phòng, tin phòng chờ duyệt, lượt xem tin toàn sàn, người dùng mới theo tuần/tháng, biểu đồ phân bố phòng và khu trọ theo 63 tỉnh/thành phố.
   - Kiểm duyệt đối tác: Phê duyệt hoặc từ chối đơn đăng ký chủ trọ kèm lý do chi tiết; tạm khóa / mở khóa đối tác vi phạm quy chế sàn.
   - Kiểm duyệt tin đăng: Duyệt tin đăng phòng mới (`ApprovalStatus = Published`), từ chối tin vi phạm (`ApprovalStatus = Rejected`) hoặc gỡ tin khỏi sàn kèm lý do.
   - Quản trị danh mục sàn: Quản lý danh mục Đơn vị hành chính (Tỉnh/Quận/Phường), Tiện ích phòng trọ (`tblAmenity`), Loại phòng (`tblRoomType`) và Blog tin tức (Summernote + elFinder).
   - Quản lý tài khoản: Khóa / mở khóa tài khoản người dùng (`tblUser`).

2. **Chủ trọ (Landlord - Đối tác cho thuê):**
   - Dữ liệu hoàn toàn cô lập (Tenant Isolation): Chỉ quản lý và xem số liệu thống kê thuộc quyền sở hữu của chính mình (chống rò rỉ IDOR).
   - Dashboard kinh doanh: Tỷ lệ lấp đầy phòng, doanh thu theo tháng/quý/năm, hóa đơn chưa thanh toán, hợp đồng thuê sắp hết hạn trong 30 ngày, lượt xem tin và yêu cầu đặt lịch hẹn mới.
   - Quản lý khu trọ & phòng: CRUD khu trọ với định vị bản đồ Leaflet, chọn tỉnh/quận/phường cascading dropdown; đăng tin phòng kèm kéo thả nhiều ảnh và tự động tạo thumbnail.
   - Vận hành khách thuê & Tài chính: Tạo khách thuê, lập hợp đồng thuê và xuất file `.docx` tự động qua MiniWord; tính hóa đơn điện/nước hàng tháng, sinh mã VietQR động theo tài khoản ngân hàng riêng của chủ trọ và xuất hóa đơn thanh toán `.docx`.
   - Chăm sóc khách hàng: Tiếp nhận yêu cầu xem phòng (`tblBookingRequest`), trò chuyện trực tiếp qua hộp thư Chat theo từng chủ trọ.

3. **Khách thuê & Người tìm phòng (Tenant / Guest Client):**
   - Tìm kiếm & Bộ lọc nâng cao: Lọc đa tiêu chí theo Tỉnh/Thành phố, Quận/Huyện, Khoảng giá, Diện tích, Loại phòng, Tiện ích (wifi, máy lạnh, máy giặt, ban công...) và sắp xếp thông minh.
   - Chi tiết phòng trực quan: Thư viện ảnh kéo lướt, danh sách tiện ích, bản đồ vị trí, thông tin chủ trọ xác minh và đánh giá từ khách thuê thực tế.
   - Tính năng tiện ích: Lưu phòng yêu thích (AJAX + đồng bộ LocalStorage khi đăng nhập), so sánh tối đa 3 phòng trọ song song (`/so-sanh`), lịch sử phòng xem gần đây.
   - Tương tác & Đặt phòng: Đặt lịch xem phòng trực tuyến (validate số điện thoại Việt Nam, chống spam rate-limit), Live Chat với chủ trọ.
   - Trợ lý ảo Groq AI Chatbot: Tư vấn tìm phòng theo nhu cầu tự nhiên, trả lời chính xác dựa trên dữ liệu phòng thực tế của sàn (RAG) và chống sinh thông tin ảo (Anti-hallucination).
   - Cổng khách thuê (Tenant Portal): Khách thuê có tài khoản xem hóa đơn tháng, quét mã VietQR thanh toán tiền phòng, đổi mật khẩu và đánh giá chất lượng phòng sau khi thuê.

---

## 🛠️ 2. Công Nghệ Áp Dụng

| Hạng Mục | Công Nghệ / Thư Viện | Mục Đích Sử Dụng |
| :--- | :--- | :--- |
| **Backend Framework** | ASP.NET Core 8.0 MVC | Kiến trúc Web MVC hiện đại, hiệu năng cao |
| **Database ORM** | Entity Framework Core 8 | Code-First, LINQ, Global Query Filtering, SQL Server |
| **AI LLM Client** | Groq API (Llama 3 / Mixtral) | Chatbot tư vấn ngữ cảnh RAG, phản hồi siêu tốc |
| **Document Engine** | MiniWord .NET | Sinh tự động hợp đồng và hóa đơn Microsoft Word `.docx` |
| **Payment Gateway** | VietQR (NAPAS 247) | Tạo mã QR chuẩn ngân hàng tự động điền STK và số tiền |
| **Image Processing** | SixLabors.ImageSharp | Tự động resize và tối ưu ảnh thumbnail khi upload |
| **Security & Antiforgery** | AutoValidateAntiforgeryToken | Bảo vệ CSRF cho mọi HTTP POST, Regex HTML Sanitizer chống XSS |
| **Caching & Performance** | Microsoft.Extensions.Caching.Memory | Cache danh mục Tỉnh/Quận/Tiện ích/Loại phòng, giảm tải DB |
| **Frontend UI** | Bootstrap 5, Vanilla CSS Variables | Design System chuẩn thương mại điện tử (đỏ cam Batdongsan `#e03c31`) |
| **Icons & Maps** | Bootstrap Icons, Leaflet OpenStreetMap | Biểu tượng thống nhất, bản đồ tương tác tọa độ GPS |
| **Rich Text & Files** | Summernote, elFinder.NetCore | Soạn thảo blog tin tức và quản lý file media |

---

## 🛢️ 3. Sơ Đồ Cơ Sở Dữ Liệu (Database Schema)

```text
[tblUser] ──────────┐ (1-1)
                    ▼
              [tblLandlord] ────────────┐ (1-n)
                    │ (1-n)             │
                    ▼                   ▼
             [tblProperty]         [tblTenant]
                    │ (1-n)             │
                    ▼                   │
                [tblRoom]               │
              /    │     \              │
             /     │      \             │
            ▼      ▼       ▼            ▼
 [tblRoomImage] [tblRoomAmenity]   [tblContract]
                           ▲            │ (1-n)
                           │            ▼
                     [tblAmenity]  [tblInvoice] ──► [tblInvoiceDetail]
                                        │
                                        ▼
                                  [tblRoomReview]
```

### Chi tiết các bảng dữ liệu:

- **`tblUser`**: Tài khoản người dùng hệ thống (`Username`, `PasswordHash`, `Role` [SuperAdmin/Landlord/User], `IsActive`).
- **`tblLandlord`**: Hồ sơ chủ trọ đối tác (`FullName`, `Phone`, `Email`, `IdentityNumber`, `BankId`, `AccountNumber`, `AccountName`, `BankName`, `Status` [Pending/Approved/Suspended/Rejected], `RejectReason`).
- **`tblProperty`**: Khu trọ / Tòa nhà (`LandlordId`, `Name`, `Slug`, `Address`, `ProvinceId`, `DistrictId`, `WardId`, `Latitude`, `Longitude`).
- **`tblProvince`**, **`tblDistrict`**, **`tblWard`**: Đơn vị hành chính Việt Nam (63 tỉnh/thành phố và quận/huyện, phường/xã).
- **`tblRoom`**: Phòng trọ (`PropertyId`, `RoomTypeId`, `RoomCode`, `RoomName`, `Slug`, `RoomPrice`, `DefaultDeposit`, `Area`, `Status` [Available/Occupied/Maintenance], `ApprovalStatus` [Draft/Pending/Published/Rejected], `IsPublished`, `ViewCount`).
- **`tblRoomImage`**: Ảnh phòng trọ (`RoomId`, `Url`, `IsPrimary`, `SortOrder`).
- **`tblAmenity`** & **`tblRoomAmenity`**: Tiện ích phòng trọ (`Name`, `Icon`, `IsActive`) và bảng liên kết nhiều-nhiều.
- **`tblContract`**: Hợp đồng thuê phòng (`RoomId`, `TenantId`, `LandlordId`, `StartDate`, `EndDate`, `MonthlyRent`, `DepositAmount`, `Status`).
- **`tblInvoice`** & **`tblInvoiceDetail`**: Hóa đơn tiền phòng hàng tháng kèm chỉ số điện, nước (`ContractId`, `LandlordId`, `TotalAmount`, `Status` [Unpaid/Paid/Overdue]).
- **`tblBookingRequest`**: Yêu cầu đặt lịch xem phòng / đặt cọc của khách (`RoomId`, `LandlordId`, `FullName`, `Phone`, `AppointmentDate`, `Status`).
- **`tblFavorite`**: Phòng yêu thích của khách hàng (`RoomId`, `UserId`, `TenantId`).
- **`tblRoomReview`**: Đánh giá và chấm điểm sao của khách thuê phòng có hợp đồng thực tế.
- **`tblChatSession`** & **`tblChatMessage`**: Phiên chat và tin nhắn thời gian thực giữa khách thuê và chủ trọ.
- **`tblPost`**: Bài viết tin tức kinh nghiệm thuê trọ tích hợp Summernote.

---

## 🚀 4. Hướng Dẫn Cài Đặt & Khởi Chạy

### 1. Yêu Cầu Môi Trường
- **.NET 8.0 SDK** ([Tải về .NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Microsoft SQL Server** (Bản 2019/2022 hoặc SQL Server Express LocalDB)
- **Công cụ:** Visual Studio 2022 / VS Code / JetBrains Rider

### 2. Cấu Hình Ứng Dụng (User Secrets & CSDL)
Để đảm bảo an toàn bảo mật, hệ thống hỗ trợ lưu trữ chuỗi kết nối và API Key thông qua **.NET User Secrets** thay vì lưu trực tiếp trong mã nguồn:

```powershell
# Di chuyển vào thư mục dự án
cd QuanLyPhongTro

# Khởi tạo User Secrets cho dự án
dotnet user-secrets init

# Thiết lập chuỗi kết nối SQL Server của bạn (ví dụ LocalDB hoặc SQL Server cục bộ)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=QuanLyPhongTroDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

# Cấu hình API Key Groq AI (nếu dùng tính năng Chatbot AI)
dotnet user-secrets set "GroqApi:ApiKey" "gsk_your_groq_api_key_here"
```

*Ghi chú:* Bạn cũng có thể điều chỉnh chuỗi kết nối trực tiếp tại file `appsettings.json` trong môi trường phát triển nội bộ.

### 3. Khởi Tạo Cơ Sở Dữ Liệu (Migrations & Seed Dữ Liệu Demo)
Chạy lệnh migration để tạo toàn bộ bảng CSDL và tự động import danh mục hành chính 63 tỉnh/thành, tiện ích, tài khoản mẫu và dữ liệu demo:

```powershell
# Cài đặt công cụ dotnet-ef (nếu chưa cài)
dotnet tool install --global dotnet-ef

# Áp dụng Migration vào SQL Server
dotnet ef database update
```

Khi ứng dụng khởi chạy lần đầu tiên, hệ sinh thái dữ liệu demo phong phú sẽ tự động được gieo mầm (Seed) bao gồm:
- **Tài khoản SuperAdmin quản trị sàn:** `admin` / Mật khẩu: `Admin@123`
- **Tài khoản Đối tác Chủ trọ mẫu:** `chutro_hanoi`, `chutro_danang`, `chutro_hcm` / Mật khẩu: `Chutro@123`
- **Tài khoản Khách thuê mẫu:** `khachthue_01` / Mật khẩu: `Khach@123`
- Hơn 30+ tin phòng trọ hoàn chỉnh với hình ảnh, giá, diện tích, tiện ích và các khu trọ tại Hà Nội, Đà Nẵng, TP.HCM.

### 4. Khởi Chạy Ứng Dụng
```powershell
dotnet run
```
Mở trình duyệt và truy cập:
- **Trang chủ tìm kiếm phòng trọ:** `https://localhost:7082` (hoặc `http://localhost:5082`)
- **Đăng nhập quản trị viên / chủ trọ:** Bấm nút **Đăng nhập** ở góc trên thanh điều hướng.

---

## 🧪 5. Kiểm Thử Hệ Thống (Testing Suite)

Dự án đi kèm bộ kiểm thử tự động toàn diện kiểm tra đầy đủ các yêu cầu nghiệp vụ:
- Kiểm thử phân quyền & cô lập dữ liệu theo chủ trọ (Tenant Isolation).
- Kiểm thử luồng SuperAdmin duyệt, từ chối và gỡ tin đăng phòng.
- Kiểm thử cơ chế bảo mật (CSRF Antiforgery, XSS HTML Sanitizer, trang lỗi 500 không lộ stack trace).
- Kiểm thử hiệu năng truy vấn CSDL (chống N+1 query, Indexing, In-memory Caching).
- Kiểm thử hồi quy toàn diện End-to-End cho 6 luồng vận hành chính.

Để thực thi toàn bộ test suite:
```powershell
dotnet test
```

---

## 🔒 6. Dữ Liệu Mẫu Tham Khảo (Demo Data)

> **Lưu ý bảo mật:** Mọi thông tin danh tính, số giấy tờ định danh và số tài khoản ngân hàng dưới đây đều là dữ liệu giả lập hư cấu phục vụ mục đích kiểm thử và trình diễn đồ án:

| Chủ trọ đại diện | Tỉnh / Thành phố | Số điện thoại mẫu | Số CCCD mẫu | Tài khoản ngân hàng VietQR |
| :--- | :--- | :--- | :--- | :--- |
| **Nguyễn Văn An** | TP. Hà Nội (Cầu Giấy, Nam Từ Liêm) | `0912 345 678` | `001200012345` | MB Bank - STK: `9999888877` |
| **Trần Thị Mai** | TP. Đà Nẵng (Hải Châu, Sơn Trà) | `0934 567 890` | `048200054321` | Vietcombank - STK: `8888777766` |
| **Lê Hoàng Long** | TP. Hồ Chí Minh (Quận 1, Bình Thạnh) | `0978 123 456` | `079200098765` | Techcombank - STK: `7777666655` |

---

*Dự án được xây dựng và hoàn thiện phục vụ Đồ Án Chuyên Ngành CNTT.*
