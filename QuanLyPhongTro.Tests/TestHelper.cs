using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Tests
{
    public static class TestHelper
    {
        public static DataContext CreateInMemoryContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
                .Options;

            var context = new DataContext(options);
            return context;
        }

        public static void SeedStandardTestData(DataContext context)
        {
            // Seed Vị trí
            var p1 = new tblProvince { ProvinceId = 1, Name = "Hà Nội", Code = "HN", FullName = "Thành phố Hà Nội" };
            var p2 = new tblProvince { ProvinceId = 2, Name = "Hồ Chí Minh", Code = "HCM", FullName = "Thành phố Hồ Chí Minh" };
            context.Provinces.AddRange(p1, p2);

            var d1 = new tblDistrict { DistrictId = 1, ProvinceId = 1, Name = "Cầu Giấy", Code = "CG", FullName = "Quận Cầu Giấy" };
            var d2 = new tblDistrict { DistrictId = 2, ProvinceId = 2, Name = "Quận 1", Code = "Q1", FullName = "Quận 1" };
            context.Districts.AddRange(d1, d2);

            var w1 = new tblWard { WardId = 1, DistrictId = 1, Name = "Dịch Vọng", Code = "DV", FullName = "Phường Dịch Vọng" };
            var w2 = new tblWard { WardId = 2, DistrictId = 2, Name = "Bến Nghé", Code = "BN", FullName = "Phường Bến Nghé" };
            context.Wards.AddRange(w1, w2);

            // Seed Loại phòng
            var rt1 = new tblRoomType { RoomTypeId = 1, RoomTypeName = "Phòng trọ khép kín", IsActive = true };
            var rt2 = new tblRoomType { RoomTypeId = 2, RoomTypeName = "Căn hộ Studio", IsActive = true };
            context.RoomTypes.AddRange(rt1, rt2);

            // Seed Tiện ích
            var a1 = new tblAmenity { AmenityId = 1, Name = "Điều hòa", Icon = "bi-snow", Description = "Điều hòa 2 chiều", IsActive = true };
            var a2 = new tblAmenity { AmenityId = 2, Name = "Nóng lạnh", Icon = "bi-thermometer-half", Description = "Bình nóng lạnh", IsActive = true };
            var a3 = new tblAmenity { AmenityId = 3, Name = "Wifi", Icon = "bi-wifi", Description = "Wifi tốc độ cao", IsActive = true };
            context.Amenities.AddRange(a1, a2, a3);

            // Seed Chủ trọ 1
            var l1 = new tblLandlord
            {
                LandlordId = 1,
                FullName = "Nguyễn Văn Chủ Trọ 1",
                Phone = "0901111222",
                Email = "chutro1@gmail.com",
                Status = LandlordStatus.Approved,
                BankId = "VCB",
                BankName = "Vietcombank",
                AccountNumber = "1234567890",
                AccountName = "NGUYEN VAN A",
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            };

            // Seed Chủ trọ 2
            var l2 = new tblLandlord
            {
                LandlordId = 2,
                FullName = "Trần Thị Chủ Trọ 2",
                Phone = "0903333444",
                Email = "chutro2@gmail.com",
                Status = LandlordStatus.Approved,
                BankId = "MB",
                BankName = "MB Bank",
                AccountNumber = "9876543210",
                AccountName = "TRAN THI B",
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            };

            // Seed Chủ trọ 3 (Pending duyệt)
            var l3 = new tblLandlord
            {
                LandlordId = 3,
                FullName = "Lê Văn Pending",
                Phone = "0905555666",
                Email = "pending@gmail.com",
                Status = LandlordStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            context.Landlords.AddRange(l1, l2, l3);

            // Seed Khu trọ của Chủ trọ 1
            var prop1 = new tblProperty
            {
                PropertyId = 1,
                LandlordId = 1,
                Name = "Khu trọ Thăng Long - Cầu Giấy",
                ProvinceId = 1,
                DistrictId = 1,
                WardId = 1,
                Address = "Số 12 Dịch Vọng, Cầu Giấy, Hà Nội",
                Status = PropertyStatus.Active,
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            };

            // Seed Khu trọ của Chủ trọ 2
            var prop2 = new tblProperty
            {
                PropertyId = 2,
                LandlordId = 2,
                Name = "Khu trọ Bến Nghé - Q1",
                ProvinceId = 2,
                DistrictId = 2,
                WardId = 2,
                Address = "Số 45 Bến Nghé, Quận 1, TP HCM",
                Status = PropertyStatus.Active,
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            };

            context.Properties.AddRange(prop1, prop2);

            // Seed Phòng trọ của Chủ trọ 1 (4 phòng)
            var r1 = new tblRoom
            {
                RoomId = 1,
                PropertyId = 1,
                RoomTypeId = 1,
                RoomCode = "P101",
                RoomName = "Phòng 101",
                Title = "Phòng trọ cao cấp Dịch Vọng full đồ",
                Slug = "phong-tro-cao-cap-dich-vong-full-do-1",
                RoomPrice = 3500000,
                Area = 25,
                MaxOccupants = 2,
                Status = RoomStatus.Occupied,
                ApprovalStatus = RoomApprovalStatus.Published,
                IsPublished = true,
                ViewCount = 150,
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            };

            var r2 = new tblRoom
            {
                RoomId = 2,
                PropertyId = 1,
                RoomTypeId = 1,
                RoomCode = "P102",
                RoomName = "Phòng 102",
                Title = "Phòng trọ thoáng mát có ban công",
                Slug = "phong-tro-thoang-mat-co-ban-cong-2",
                RoomPrice = 3800000,
                Area = 28,
                MaxOccupants = 2,
                Status = RoomStatus.Available,
                ApprovalStatus = RoomApprovalStatus.Published,
                IsPublished = true,
                ViewCount = 80,
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            };

            var r3 = new tblRoom
            {
                RoomId = 3,
                PropertyId = 1,
                RoomTypeId = 2,
                RoomCode = "P201",
                RoomName = "Phòng 201",
                Title = "Studio hiện đại Cầu Giấy",
                Slug = "studio-hien-dai-cau-giay-3",
                RoomPrice = 4500000,
                Area = 32,
                MaxOccupants = 2,
                Status = RoomStatus.Available,
                ApprovalStatus = RoomApprovalStatus.Pending,
                IsPublished = false,
                ViewCount = 10,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };

            var r4 = new tblRoom
            {
                RoomId = 4,
                PropertyId = 1,
                RoomTypeId = 1,
                RoomCode = "P202",
                RoomName = "Phòng 202",
                Title = "Phòng trọ giá rẻ sinh viên",
                Slug = "phong-tro-gia-re-sinh-vien-4",
                RoomPrice = 2800000,
                Area = 20,
                MaxOccupants = 2,
                Status = RoomStatus.Maintenance,
                ApprovalStatus = RoomApprovalStatus.Published,
                IsPublished = false,
                ViewCount = 40,
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            };

            // Seed Phòng trọ của Chủ trọ 2 (2 phòng)
            var r5 = new tblRoom
            {
                RoomId = 5,
                PropertyId = 2,
                RoomTypeId = 2,
                RoomCode = "P101",
                RoomName = "Phòng 101 Q1",
                Title = "Studio trung tâm Q1 đầy đủ tiện nghi",
                Slug = "studio-trung-tam-q1-day-du-tien-nghi-5",
                RoomPrice = 6500000,
                Area = 35,
                MaxOccupants = 2,
                Status = RoomStatus.Occupied,
                ApprovalStatus = RoomApprovalStatus.Published,
                IsPublished = true,
                ViewCount = 210,
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            };

            var r6 = new tblRoom
            {
                RoomId = 6,
                PropertyId = 2,
                RoomTypeId = 1,
                RoomCode = "P102",
                RoomName = "Phòng 102 Q1",
                Title = "Phòng trọ tiện đi lại Bến Nghé Q1",
                Slug = "phong-tro-tien-di-lai-ben-nghe-q1-6",
                RoomPrice = 5000000,
                Area = 30,
                MaxOccupants = 2,
                Status = RoomStatus.Available,
                ApprovalStatus = RoomApprovalStatus.Published,
                IsPublished = true,
                ViewCount = 120,
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            };

            context.Rooms.AddRange(r1, r2, r3, r4, r5, r6);

            // Seed Khách thuê
            var t1 = new tblTenant
            {
                TenantId = 1,
                FullName = "Hoàng Văn Thuê 1",
                Phone = "0988111222",
                IdentityNumber = "001234567890",
                IsActive = true
            };
            var t2 = new tblTenant
            {
                TenantId = 2,
                FullName = "Lê Thị Thuê 2",
                Phone = "0988333444",
                IdentityNumber = "001234567891",
                IsActive = true
            };
            context.Tenants.AddRange(t1, t2);

            // Seed Hợp đồng
            // Hợp đồng của Chủ trọ 1 (phòng 1)
            var c1 = new tblContract
            {
                ContractId = 1,
                ContractCode = "HD01",
                LandlordId = 1,
                RoomId = 1,
                TenantId = 1,
                StartDate = DateTime.UtcNow.AddMonths(-2),
                EndDate = DateTime.UtcNow.AddDays(20), // Sắp hết hạn trong 30 ngày
                MonthlyRent = 3500000,
                Deposit = 3500000,
                Status = ContractStatus.Active
            };

            // Hợp đồng của Chủ trọ 2 (phòng 5)
            var c2 = new tblContract
            {
                ContractId = 2,
                ContractCode = "HD02",
                LandlordId = 2,
                RoomId = 5,
                TenantId = 2,
                StartDate = DateTime.UtcNow.AddMonths(-2),
                EndDate = DateTime.UtcNow.AddMonths(10), // Chưa hết hạn
                MonthlyRent = 6500000,
                Deposit = 6500000,
                Status = ContractStatus.Active
            };
            context.Contracts.AddRange(c1, c2);

            // Seed Hóa đơn
            // Hóa đơn của Chủ trọ 1
            var inv1 = new tblInvoice
            {
                InvoiceId = 1,
                InvoiceCode = "HD001",
                LandlordId = 1,
                RoomId = 1,
                ContractId = 1,
                BillingMonth = DateTime.UtcNow.Month,
                BillingYear = DateTime.UtcNow.Year,
                RoomRentAmount = 3500000,
                TotalServiceAmount = 500000,
                TotalAmount = 4000000,
                Status = InvoiceStatus.Paid,
                PaidDate = DateTime.UtcNow.AddDays(-5),
                DueDate = DateTime.UtcNow.AddDays(5)
            };

            var inv2 = new tblInvoice
            {
                InvoiceId = 2,
                InvoiceCode = "HD002",
                LandlordId = 1,
                RoomId = 1,
                ContractId = 1,
                BillingMonth = DateTime.UtcNow.AddMonths(1).Month,
                BillingYear = DateTime.UtcNow.AddMonths(1).Year,
                RoomRentAmount = 3500000,
                TotalServiceAmount = 450000,
                TotalAmount = 3950000,
                Status = InvoiceStatus.Unpaid, // Chưa thanh toán
                DueDate = DateTime.UtcNow.AddDays(5)
            };

            // Hóa đơn của Chủ trọ 2
            var inv3 = new tblInvoice
            {
                InvoiceId = 3,
                InvoiceCode = "HD003",
                LandlordId = 2,
                RoomId = 5,
                ContractId = 2,
                BillingMonth = DateTime.UtcNow.Month,
                BillingYear = DateTime.UtcNow.Year,
                RoomRentAmount = 6500000,
                TotalServiceAmount = 650000,
                TotalAmount = 7150000,
                Status = InvoiceStatus.Paid,
                PaidDate = DateTime.UtcNow.AddDays(-2),
                DueDate = DateTime.UtcNow.AddDays(5)
            };
            context.Invoices.AddRange(inv1, inv2, inv3);

            // Seed Booking Request
            // Booking gửi cho phòng của Chủ trọ 1
            var bk1 = new tblBookingRequest
            {
                RequestId = 1,
                RoomId = 2,
                LandlordId = 1,
                FullName = "Trần Người Xem",
                Phone = "0977111222",
                PreferredDate = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-dd"),
                ViewingTime = DateTime.UtcNow.AddDays(2),
                Message = "Muốn xem phòng chiều thứ 7",
                Status = BookingRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            };

            // Booking gửi cho phòng của Chủ trọ 2
            var bk2 = new tblBookingRequest
            {
                RequestId = 2,
                RoomId = 6,
                LandlordId = 2,
                FullName = "Phạm Khách Tiềm Năng",
                Phone = "0977333444",
                PreferredDate = DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd"),
                ViewingTime = DateTime.UtcNow.AddDays(3),
                Message = "Muốn xem studio buổi sáng",
                Status = BookingRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };
            context.BookingRequests.AddRange(bk1, bk2);

            context.SaveChanges();
        }
    }
}
