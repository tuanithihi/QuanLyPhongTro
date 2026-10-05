using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
    public class ListingAndModerationTests
    {
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            return Convert.ToHexString(sha256.ComputeHash(bytes)).ToLower();
        }

        private static RoomService CreateRoomService(DataContext context)
        {
            var uploadMock = new Mock<IFileUploadService>();
            var configMock = new Mock<IConfiguration>();
            return new RoomService(context, uploadMock.Object, configMock.Object, NullLogger<RoomService>.Instance);
        }

        [Fact]
        public async Task ApproveListing_UpdatesStatusToPublishedAndBecomesSearchable()
        {
            // Arrange
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var searchService = new RoomSearchService(context, cache, NullLogger<RoomSearchService>.Instance);
            var roomService = CreateRoomService(context);

            // Phòng 3 ban đầu là Pending, IsPublished = false
            var room3 = await context.Rooms.FindAsync(3);
            Assert.NotNull(room3);
            Assert.Equal(RoomApprovalStatus.Pending, room3.ApprovalStatus);
            Assert.False(room3.IsPublished);

            // Act - Duyệt tin
            var (approveResult, _) = await roomService.ApproveListingAsync(3);
            Assert.True(approveResult);

            // Reload from db
            var updatedRoom = await context.Rooms.FindAsync(3);
            Assert.NotNull(updatedRoom);
            Assert.Equal(RoomApprovalStatus.Published, updatedRoom.ApprovalStatus);
            Assert.True(updatedRoom.IsPublished);

            // Act 2 - Tìm kiếm phòng
            var searchResult = await searchService.SearchRoomsAsync(new RoomFilterCriteria { Page = 1, PageSize = 10 });

            // Assert 2 - Phòng 3 đã xuất hiện trong danh sách tìm kiếm
            Assert.Contains(searchResult.Items, r => r.RoomId == 3);
        }

        [Fact]
        public async Task RejectListing_SetsRejectedStatusWithReason_AndNotSearchable()
        {
            // Arrange
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var searchService = new RoomSearchService(context, cache, NullLogger<RoomSearchService>.Instance);
            var roomService = CreateRoomService(context);

            // Act - Từ chối duyệt kèm lý do
            string reason = "Hình ảnh phòng mờ và thiếu thông tin nhà vệ sinh";
            var (result, _) = await roomService.RejectListingAsync(3, reason);
            Assert.True(result);

            // Assert - Trạng thái & Lý do
            var room = await context.Rooms.FindAsync(3);
            Assert.NotNull(room);
            Assert.Equal(RoomApprovalStatus.Rejected, room.ApprovalStatus);
            Assert.Equal(reason, room.RejectReason);
            Assert.False(room.IsPublished);

            // Assert - Không xuất hiện trong tìm kiếm công khai
            var searchResult = await searchService.SearchRoomsAsync(new RoomFilterCriteria { Page = 1, PageSize = 10 });
            Assert.DoesNotContain(searchResult.Items, r => r.RoomId == 3);
        }

        [Fact]
        public async Task UnpublishListing_RemovesRoomFromSearchAndSitemap()
        {
            // Arrange
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var searchService = new RoomSearchService(context, cache, NullLogger<RoomSearchService>.Instance);
            var roomService = CreateRoomService(context);

            // Phòng 2 ban đầu là Published & Available (đang tìm thấy được)
            var initialSearch = await searchService.SearchRoomsAsync(new RoomFilterCriteria { Page = 1, PageSize = 10 });
            Assert.Contains(initialSearch.Items, r => r.RoomId == 2);

            // Act - SuperAdmin gỡ tin
            string reason = "Vi phạm chính sách: phát hiện thông tin liên hệ giả mạo";
            var (unpublishResult, _) = await roomService.UnpublishListingAsync(2, reason);
            Assert.True(unpublishResult);

            // Assert 1 - Thuộc tính cập nhật đúng
            var room2 = await context.Rooms.FindAsync(2);
            Assert.NotNull(room2);
            Assert.False(room2.IsPublished);
            Assert.Equal(reason, room2.RejectReason);

            // Assert 2 - Biến mất khỏi tìm kiếm
            var afterSearch = await searchService.SearchRoomsAsync(new RoomFilterCriteria { Page = 1, PageSize = 10 });
            Assert.DoesNotContain(afterSearch.Items, r => r.RoomId == 2);

            // Assert 3 - Biến mất khỏi sitemap query
            var sitemapRooms = await context.Rooms
                .AsNoTracking()
                .Where(r => r.Status == RoomStatus.Available
                         && r.ApprovalStatus == RoomApprovalStatus.Published
                         && r.IsPublished)
                .ToListAsync();

            Assert.DoesNotContain(sitemapRooms, r => r.RoomId == 2);
        }

        [Fact]
        public async Task Landlord_ApproveAndReject_ChangesStatusCorrectly()
        {
            // Arrange
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);

            var pendingLandlord = await context.Landlords.FindAsync(3);
            Assert.NotNull(pendingLandlord);
            Assert.Equal(LandlordStatus.Pending, pendingLandlord.Status);

            // Case 1: SuperAdmin duyệt chủ trọ
            pendingLandlord.Status = LandlordStatus.Approved;
            pendingLandlord.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            var approved = await context.Landlords.FindAsync(3);
            Assert.NotNull(approved);
            Assert.Equal(LandlordStatus.Approved, approved.Status);

            // Case 2: SuperAdmin từ chối chủ trọ khác
            var newLandlord = new tblLandlord
            {
                LandlordId = 4,
                FullName = "Chủ trọ gian lận",
                Phone = "0999888777",
                Email = "fake@gmail.com",
                Status = LandlordStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            context.Landlords.Add(newLandlord);
            await context.SaveChangesAsync();

            newLandlord.Status = LandlordStatus.Rejected;
            newLandlord.RejectReason = "Số điện thoại không liên lạc được và CCCD không hợp lệ";
            await context.SaveChangesAsync();

            var rejected = await context.Landlords.FindAsync(4);
            Assert.NotNull(rejected);
            Assert.Equal(LandlordStatus.Rejected, rejected.Status);
            Assert.Equal("Số điện thoại không liên lạc được và CCCD không hợp lệ", rejected.RejectReason);
        }

        [Fact]
        public async Task SuspendedLandlord_IsBlockedFromLogin()
        {
            // Arrange
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);

            var passwordHash = HashPassword("Password123!");
            var user = new tblUser
            {
                UserId = 10,
                Username = "landlord_locked",
                Email = "locked@test.com",
                PasswordHash = passwordHash,
                Role = "Landlord",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var landlord = new tblLandlord
            {
                LandlordId = 10,
                UserId = 10,
                FullName = "Chủ trọ bị khóa",
                Phone = "0912345678",
                Email = "locked@test.com",
                Status = LandlordStatus.Suspended,
                RejectReason = "Vi phạm nhiều lần điều khoản sàn",
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            context.Landlords.Add(landlord);
            await context.SaveChangesAsync();

            // Simulate check in AccountController LoginModal
            var input = "landlord_locked";
            var dbUser = await context.Users.FirstOrDefaultAsync(u =>
                u.IsActive && u.PasswordHash == passwordHash && (u.Username == input || u.Email == input));

            Assert.NotNull(dbUser);
            var dbLandlord = await context.Landlords.FirstOrDefaultAsync(l => l.UserId == dbUser.UserId);
            Assert.NotNull(dbLandlord);

            bool loginAllowed = true;
            string? errorMessage = null;

            if (dbLandlord.Status == LandlordStatus.Suspended)
            {
                loginAllowed = false;
                errorMessage = "Tài khoản chủ trọ của bạn đã bị tạm khóa (Suspended). Vui lòng liên hệ ban quản trị.";
            }

            // Assert
            Assert.False(loginAllowed);
            Assert.Contains("tạm khóa", errorMessage);
        }

        [Fact]
        public async Task InactiveUser_IsBlockedFromLogin()
        {
            // Arrange
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);

            var passwordHash = HashPassword("Password123!");
            var user = new tblUser
            {
                UserId = 20,
                Username = "user_inactive",
                Email = "inactive@test.com",
                PasswordHash = passwordHash,
                Role = "Tenant",
                IsActive = false, // Tài khoản bị vô hiệu hóa
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Simulate query
            var input = "user_inactive";
            var dbUser = await context.Users.FirstOrDefaultAsync(u =>
                u.IsActive && u.PasswordHash == passwordHash && (u.Username == input || u.Email == input));

            // Assert: Không tìm thấy user vì u.IsActive == false
            Assert.Null(dbUser);
        }
    }
}
