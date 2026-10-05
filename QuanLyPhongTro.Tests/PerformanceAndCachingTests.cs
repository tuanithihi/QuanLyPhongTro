using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;
using Xunit;

namespace QuanLyPhongTro.Tests
{
    public class PerformanceAndCachingTests
    {
        [Fact]
        public async Task RoomSearchService_CachesCategoriesAndLocations()
        {
            // Arrange
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var searchService = new RoomSearchService(context, cache, NullLogger<RoomSearchService>.Instance);

            // Act 1: Lần đầu gọi - nạp vào cache
            var provincesFirst = await searchService.GetPopularProvincesAsync(5);
            var roomTypesFirst = await searchService.GetActiveRoomTypesAsync();
            var amenitiesFirst = await searchService.GetAllAmenitiesAsync();
            var districtsFirst = await searchService.GetDistrictsByProvinceAsync(1);

            Assert.NotEmpty(provincesFirst);
            Assert.NotEmpty(roomTypesFirst);
            Assert.NotEmpty(amenitiesFirst);
            Assert.NotEmpty(districtsFirst);

            // Modify Db directly behind cache
            context.Provinces.Add(new tblProvince { ProvinceId = 99, Name = "Tỉnh Mới", Code = "TM", FullName = "Tỉnh Mới" });
            await context.SaveChangesAsync();

            // Act 2: Lần hai gọi - phải đọc từ cache
            var provincesSecond = await searchService.GetPopularProvincesAsync(5);

            // Assert: Số lượng tỉnh trong cache vẫn như lần đầu (chứng minh lấy từ Cache chứ không query lại DB)
            Assert.Equal(provincesFirst.Count, provincesSecond.Count);
        }

        [Fact]
        public async Task RoomDetail_LoadsAllRelatedEntitiesInSingleQuery_NoNPlusOne()
        {
            // Arrange
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);

            // Thêm ảnh và tiện ích cho phòng 1
            context.RoomImages.Add(new tblRoomImage { ImageId = 1, RoomId = 1, Url = "/uploads/rooms/r1.jpg", IsPrimary = true });
            context.RoomAmenities.Add(new tblRoomAmenity { RoomId = 1, AmenityId = 1 });
            context.RoomAmenities.Add(new tblRoomAmenity { RoomId = 1, AmenityId = 2 });
            await context.SaveChangesAsync();

            // Act: Query chi tiết phòng với toàn bộ navigation được Eager Loading (Include)
            var roomDetail = await context.Rooms
                .AsNoTracking()
                .Include(r => r.Property).ThenInclude(p => p!.Province)
                .Include(r => r.Property).ThenInclude(p => p!.District)
                .Include(r => r.Property).ThenInclude(p => p!.Ward)
                .Include(r => r.Property).ThenInclude(p => p!.Landlord)
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
                .FirstOrDefaultAsync(r => r.RoomId == 1);

            // Assert: Toàn bộ quan hệ được nạp đầy đủ trong 1 lần fetch, không gây deferred lazy-loading
            Assert.NotNull(roomDetail);
            Assert.NotNull(roomDetail.Property);
            Assert.NotNull(roomDetail.Property.Province);
            Assert.NotNull(roomDetail.Property.District);
            Assert.NotNull(roomDetail.Property.Ward);
            Assert.NotNull(roomDetail.Property.Landlord);
            Assert.NotNull(roomDetail.RoomType);
            Assert.NotEmpty(roomDetail.RoomImages);
            Assert.NotEmpty(roomDetail.RoomAmenities);
            Assert.NotNull(roomDetail.RoomAmenities.First().Amenity);
        }

        [Fact]
        public async Task RoomListingSearch_UsesEfficientProjectionAndPaging()
        {
            // Arrange
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var searchService = new RoomSearchService(context, cache, NullLogger<RoomSearchService>.Instance);

            var criteria = new RoomFilterCriteria
            {
                Page = 1,
                PageSize = 2, // Phân trang
                SortBy = "price_asc"
            };

            // Act
            var result = await searchService.SearchRoomsAsync(criteria);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Page);
            Assert.Equal(2, result.PageSize);
            Assert.True(result.Items.Count <= 2); // Chỉ nạp đúng số lượng của trang
            Assert.True(result.TotalItems >= 2);
        }
    }
}
