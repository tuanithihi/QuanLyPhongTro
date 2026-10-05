using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using QuanLyPhongTro.Areas.Admin.Controllers;
using QuanLyPhongTro.Areas.Admin.Models;
using QuanLyPhongTro.Services;
using Xunit;

namespace QuanLyPhongTro.Tests
{
    public class DashboardTests
    {
        [Fact]
        public async Task SuperAdminDashboard_ShowsPlatformWideTotals()
        {
            // Arrange
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);

            var landlordServiceMock = new Mock<ICurrentLandlordService>();
            landlordServiceMock.Setup(s => s.IsSuperAdmin()).Returns(true);
            landlordServiceMock.Setup(s => s.GetCurrentLandlordId()).Returns((int?)null);

            var controller = new HomeController(context, landlordServiceMock.Object);

            // Act
            var result = await controller.Index(period: "year", year: DateTime.UtcNow.Year) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<DashboardViewModel>(result.Model);

            Assert.True(model.IsSuperAdmin);
            Assert.Equal(3, model.TotalLandlords); // 3 chủ trọ
            Assert.Equal(1, model.PendingLandlords); // 1 chủ trọ pending
            Assert.Equal(6, model.TotalRooms); // 6 phòng toàn sàn
            Assert.Equal(1, model.PendingRooms); // 1 phòng pending duyệt (r3)
            Assert.Equal(610, model.TotalRoomViews); // 150+80+10+40+210+120 = 610

            // Doanh thu toàn sàn (inv1 + inv3 đã thanh toán)
            Assert.Equal(11150000m, model.TotalRevenueThisMonth);
            Assert.Equal(2, model.PaidInvoicesThisMonth); // inv1 & inv3
            Assert.Equal(1, model.UnpaidInvoicesThisMonth); // inv2

            // Phân bố khu vực
            Assert.NotEmpty(model.RoomsByProvince);
            Assert.Equal(2, model.RoomsByProvince.Count); // Hà Nội và Hồ Chí Minh
        }

        [Fact]
        public async Task LandlordDashboard_IsStrictlyIsolated_ForLandlord1()
        {
            // Arrange
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);

            var landlordServiceMock = new Mock<ICurrentLandlordService>();
            landlordServiceMock.Setup(s => s.IsSuperAdmin()).Returns(false);
            landlordServiceMock.Setup(s => s.GetCurrentLandlordId()).Returns(1);

            var controller = new HomeController(context, landlordServiceMock.Object);

            // Act
            var result = await controller.Index(period: "year", year: DateTime.UtcNow.Year) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<DashboardViewModel>(result.Model);

            Assert.False(model.IsSuperAdmin);
            Assert.Equal(4, model.TotalRooms); // Chỉ có 4 phòng của Landlord 1
            Assert.Equal(280, model.TotalRoomViews); // 150 + 80 + 10 + 40 = 280 (không tính 330 views của Landlord 2)
            Assert.Equal(1, model.ExpiringContractsIn30Days); // c1 sắp hết hạn
            Assert.Equal(1, model.PendingBookingRequests); // bk1 gửi cho Landlord 1
            Assert.Equal(1, model.ActiveContracts); // c1

            // Doanh thu chỉ của Landlord 1: inv1 (4.000.000 đã TT), inv2 (3.950.000 chưa TT)
            Assert.Equal(4000000m, model.TotalRevenueThisMonth);
            Assert.Equal(1, model.PaidInvoicesThisMonth);
            Assert.Equal(1, model.UnpaidInvoicesThisMonth);
        }

        [Fact]
        public async Task LandlordDashboard_IsStrictlyIsolated_ForLandlord2()
        {
            // Arrange
            var context = TestHelper.CreateInMemoryContext();
            TestHelper.SeedStandardTestData(context);

            var landlordServiceMock = new Mock<ICurrentLandlordService>();
            landlordServiceMock.Setup(s => s.IsSuperAdmin()).Returns(false);
            landlordServiceMock.Setup(s => s.GetCurrentLandlordId()).Returns(2);

            var controller = new HomeController(context, landlordServiceMock.Object);

            // Act
            var result = await controller.Index(period: "year", year: DateTime.UtcNow.Year) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<DashboardViewModel>(result.Model);

            Assert.False(model.IsSuperAdmin);
            Assert.Equal(2, model.TotalRooms); // Chỉ có 2 phòng của Landlord 2
            Assert.Equal(330, model.TotalRoomViews); // 210 + 120 = 330
            Assert.Equal(0, model.ExpiringContractsIn30Days); // c2 còn 10 tháng, không nằm trong 30 ngày
            Assert.Equal(1, model.PendingBookingRequests); // bk2 gửi cho Landlord 2
            Assert.Equal(1, model.ActiveContracts); // c2

            // Doanh thu chỉ của Landlord 2: inv3 (7.150.000 đã TT), 0 hóa đơn chưa TT
            Assert.Equal(7150000m, model.TotalRevenueThisMonth);
            Assert.Equal(1, model.PaidInvoicesThisMonth);
            Assert.Equal(0, model.UnpaidInvoicesThisMonth);
        }
    }
}
