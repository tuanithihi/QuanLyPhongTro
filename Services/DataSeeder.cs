using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Services
{
    public class DataSeeder
    {
        private class ProvinceDto
        {
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public List<DistrictDto>? Districts { get; set; }
        }

        private class DistrictDto
        {
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
        }

        private class WardDto
        {
            public string DistrictCode { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
        }

        /// <summary>
        /// Seed danh mục hành chính: 63 Tỉnh/Thành phố và Quận/Huyện, cùng các Phường/Xã mẫu.
        /// </summary>
        public static async Task SeedAdministrativeDataAsync(DataContext context, string? seedFolderPath = null)
        {
            seedFolderPath ??= Path.Combine(AppContext.BaseDirectory, "Data", "Seed");

            // Nếu folder không tồn tại theo BaseDirectory, thử tìm từ thư mục làm việc hiện tại
            if (!Directory.Exists(seedFolderPath))
            {
                var fallback = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Seed");
                if (Directory.Exists(fallback))
                {
                    seedFolderPath = fallback;
                }
            }

            var provDistPath = Path.Combine(seedFolderPath, "provinces_districts.json");
            if (File.Exists(provDistPath))
            {
                var json = await File.ReadAllTextAsync(provDistPath);
                var provinces = JsonSerializer.Deserialize<List<ProvinceDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (provinces != null && provinces.Any())
                {
                    var existingProvinces = await context.Provinces
                        .Include(p => p.Districts)
                        .ToDictionaryAsync(p => p.Code);

                    foreach (var pDto in provinces)
                    {
                        if (!existingProvinces.TryGetValue(pDto.Code, out var province))
                        {
                            province = new tblProvince
                            {
                                Code = pDto.Code,
                                Name = pDto.Name,
                                FullName = pDto.FullName
                            };
                            context.Provinces.Add(province);
                            await context.SaveChangesAsync();
                            existingProvinces[province.Code] = province;
                        }

                        if (pDto.Districts != null && pDto.Districts.Any())
                        {
                            var existingDistricts = await context.Districts
                                .Where(d => d.ProvinceId == province.ProvinceId)
                                .ToDictionaryAsync(d => d.Code);

                            foreach (var dDto in pDto.Districts)
                            {
                                if (!existingDistricts.ContainsKey(dDto.Code))
                                {
                                    var district = new tblDistrict
                                    {
                                        ProvinceId = province.ProvinceId,
                                        Code = dDto.Code,
                                        Name = dDto.Name,
                                        FullName = dDto.FullName
                                    };
                                    context.Districts.Add(district);
                                }
                            }
                            await context.SaveChangesAsync();
                        }
                    }
                }
            }

            // Seed Phường/Xã
            var wardsPath = Path.Combine(seedFolderPath, "wards_sample.json");
            if (File.Exists(wardsPath))
            {
                var json = await File.ReadAllTextAsync(wardsPath);
                var wards = JsonSerializer.Deserialize<List<WardDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (wards != null && wards.Any())
                {
                    var districtCodes = wards.Select(w => w.DistrictCode).Distinct().ToList();
                    var districts = await context.Districts
                        .Where(d => districtCodes.Contains(d.Code))
                        .ToDictionaryAsync(d => d.Code);

                    var existingWards = (await context.Wards
                        .Select(w => w.Code)
                        .ToListAsync())
                        .ToHashSet();

                    foreach (var wDto in wards)
                    {
                        if (districts.TryGetValue(wDto.DistrictCode, out var district) && !existingWards.Contains(wDto.Code))
                        {
                            var ward = new tblWard
                            {
                                DistrictId = district.DistrictId,
                                Code = wDto.Code,
                                Name = wDto.Name,
                                FullName = wDto.FullName
                            };
                            context.Wards.Add(ward);
                            existingWards.Add(wDto.Code);
                        }
                    }
                    await context.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Seed danh sách 10 tiện ích tiêu chuẩn nếu chưa có
        /// </summary>
        public static async Task SeedAmenitiesAsync(DataContext context)
        {
            var defaultAmenities = new List<(string Name, string Icon, string Description)>
            {
                ("Điều hòa", "bi bi-snow", "Máy điều hòa nhiệt độ hai chiều / một chiều tiết kiệm điện"),
                ("Nóng lạnh", "bi bi-thermometer-high", "Bình nóng lạnh an toàn"),
                ("Gác lửng", "bi bi-layers", "Gác lửng thông thoáng kiên cố"),
                ("Chỗ để xe", "bi bi-p-circle", "Nhà xe an ninh rộng rãi có camera giám sát"),
                ("Wifi", "bi bi-wifi", "Internet cáp quang tốc độ cao"),
                ("Giờ giấc tự do", "bi bi-clock-history", "Không chung chủ, ra vào khóa vân tay / thẻ từ tự do"),
                ("Cho nuôi thú cưng", "bi bi-heart", "Cho phép nuôi chó mèo với điều kiện giữ vệ sinh"),
                ("Tủ lạnh", "bi bi-box", "Tủ lạnh có sẵn trong phòng"),
                ("Máy giặt", "bi bi-arrow-repeat", "Máy giặt dùng riêng hoặc khu giặt chung tiện lợi"),
                ("Ban công", "bi bi-sun", "Ban công thoáng mát đón gió và ánh sáng tự nhiên")
            };

            var existingNames = (await context.Amenities.Select(a => a.Name).ToListAsync())
                .Select(n => n.Trim().ToLowerInvariant())
                .ToHashSet();

            foreach (var item in defaultAmenities)
            {
                if (!existingNames.Contains(item.Name.Trim().ToLowerInvariant()))
                {
                    context.Amenities.Add(new tblAmenity
                    {
                        Name = item.Name,
                        Icon = item.Icon,
                        Description = item.Description,
                        IsActive = true
                    });
                    existingNames.Add(item.Name.Trim().ToLowerInvariant());
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Backfill dữ liệu cũ: Idempotent - Chạy nhiều lần vẫn chỉ tạo 1 Landlord mặc định và 1 Property mặc định
        /// </summary>
        public static async Task BackfillLegacyDataAsync(DataContext context, IConfiguration config)
        {
            // 1. Đọc thông tin từ appsettings.json
            var landlordName = config["Landlord:Name"] ?? "Ban Quản Trị Sàn";
            var landlordIdentity = config["Landlord:IdentityNumber"] ?? "001200000001";
            var landlordPhone = config["Landlord:Phone"] ?? "0901234567";
            var landlordAddress = config["Landlord:Address"] ?? "Hà Nội, Việt Nam";

            var bankId = config["BankPayment:BankId"] ?? "970422";
            var accountNumber = config["BankPayment:AccountNumber"] ?? "0123456789";
            var accountName = config["BankPayment:AccountName"] ?? "BAN QUAN TRI SAN";
            var bankName = config["BankPayment:BankName"] ?? "MB Bank";

            // 2. Tìm hoặc tạo tblLandlord mặc định (Idempotent theo IdentityNumber / Phone)
            var landlord = await context.Landlords
                .FirstOrDefaultAsync(l => l.IdentityNumber == landlordIdentity || l.Phone == landlordPhone);

            if (landlord == null)
            {
                landlord = new tblLandlord
                {
                    FullName = landlordName,
                    IdentityNumber = landlordIdentity,
                    Phone = landlordPhone,
                    Address = landlordAddress,
                    BankId = bankId,
                    AccountNumber = accountNumber,
                    AccountName = accountName,
                    BankName = bankName,
                    Status = LandlordStatus.Approved,
                    CreatedAt = DateTime.Now
                };
                context.Landlords.Add(landlord);
                await context.SaveChangesAsync();
            }

            // 3. Tìm hoặc tạo tblProperty mặc định (Idempotent theo Slug "khu-tro-ho-duc-tuan" hoặc LandlordId)
            const string defaultPropertySlug = "khu-tro-ho-duc-tuan";
            var property = await context.Properties
                .FirstOrDefaultAsync(p => p.Slug == defaultPropertySlug || p.LandlordId == landlord.LandlordId);

            if (property == null)
            {
                // Thử liên kết với Tỉnh Nghệ An (Code: 40) và TP Vinh (Code: 412) nếu có
                var province = await context.Provinces.FirstOrDefaultAsync(p => p.Code == "40" || p.Name.Contains("Nghệ An"));
                var district = province != null 
                    ? await context.Districts.FirstOrDefaultAsync(d => d.ProvinceId == province.ProvinceId && (d.Code == "412" || d.Name.Contains("Vinh")))
                    : null;

                property = new tblProperty
                {
                    LandlordId = landlord.LandlordId,
                    Name = string.Concat("Khu trọ ", landlord.FullName),
                    Slug = defaultPropertySlug,
                    Address = landlord.Address ?? "Thành Phố Vinh, Tỉnh Nghệ An",
                    ProvinceId = province?.ProvinceId,
                    DistrictId = district?.DistrictId,
                    Status = PropertyStatus.Active,
                    CreatedAt = DateTime.Now
                };
                context.Properties.Add(property);
                await context.SaveChangesAsync();
            }

            // 4. Backfill tblRoom
            var rooms = await context.Rooms.ToListAsync();
            var usedSlugs = new HashSet<string>(await context.Rooms.Where(r => !string.IsNullOrEmpty(r.Slug)).Select(r => r.Slug).ToListAsync());

            foreach (var r in rooms)
            {
                bool modified = false;

                if (!r.PropertyId.HasValue)
                {
                    r.PropertyId = property.PropertyId;
                    modified = true;
                }

                if (string.IsNullOrWhiteSpace(r.Title))
                {
                    r.Title = r.RoomName;
                    modified = true;
                }

                if (r.Capacity <= 0)
                {
                    r.Capacity = r.MaxOccupants > 0 ? r.MaxOccupants : 2;
                    modified = true;
                }

                if (r.ApprovalStatus != RoomApprovalStatus.Published)
                {
                    r.ApprovalStatus = RoomApprovalStatus.Published;
                    modified = true;
                }

                if (!r.PublishedAt.HasValue)
                {
                    r.PublishedAt = r.CreatedAt != default ? r.CreatedAt : DateTime.Now;
                    modified = true;
                }

                // Sinh Slug duy nhất cho phòng nếu chưa có hoặc là slug tạm dạng phong-{id}
                bool isTemporarySlug = !string.IsNullOrWhiteSpace(r.Slug) &&
                                       r.Slug.StartsWith("phong-") &&
                                       int.TryParse(r.Slug.Replace("phong-", ""), out _);

                if (string.IsNullOrWhiteSpace(r.Slug) || isTemporarySlug)
                {
                    var baseSlug = SlugHelper.GenerateSlug(r.RoomName);
                    var candidate = baseSlug;
                    int counter = 1;

                    while (usedSlugs.Contains(candidate) && candidate != r.Slug)
                    {
                        candidate = !string.IsNullOrWhiteSpace(r.RoomCode)
                            ? string.Concat(baseSlug, "-", SlugHelper.GenerateSlug(r.RoomCode))
                            : string.Concat(baseSlug, "-", counter++);
                    }

                    if (r.Slug != candidate)
                    {
                        if (!string.IsNullOrEmpty(r.Slug)) usedSlugs.Remove(r.Slug);
                        r.Slug = candidate;
                        usedSlugs.Add(candidate);
                        modified = true;
                    }
                }

                if (modified)
                {
                    context.Rooms.Update(r);
                }
            }

            // 5. Backfill LandlordId cho các bảng liên quan nếu đang null
            var tenants = await context.Tenants.Where(t => t.LandlordId == null).ToListAsync();
            foreach (var t in tenants)
            {
                t.LandlordId = landlord.LandlordId;
            }

            var contracts = await context.Contracts.Where(c => c.LandlordId == null).ToListAsync();
            foreach (var c in contracts)
            {
                c.LandlordId = landlord.LandlordId;
            }

            var invoices = await context.Invoices.Where(i => i.LandlordId == null).ToListAsync();
            foreach (var i in invoices)
            {
                i.LandlordId = landlord.LandlordId;
            }

            var services = await context.Services.Where(s => s.LandlordId == null).ToListAsync();
            foreach (var s in services)
            {
                s.LandlordId = landlord.LandlordId;
            }

            var bookingRequests = await context.BookingRequests.Where(b => b.LandlordId == null).ToListAsync();
            foreach (var b in bookingRequests)
            {
                b.LandlordId = landlord.LandlordId;
            }

            var chatSessions = await context.ChatSessions.Where(cs => cs.LandlordId == null).ToListAsync();
            foreach (var cs in chatSessions)
            {
                cs.LandlordId = landlord.LandlordId;
            }

            await context.SaveChangesAsync();
        }

        private static string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password + "PhongTro@2026#Salt");
            return Convert.ToHexString(sha256.ComputeHash(bytes)).ToLower();
        }

        /// <summary>
        /// Seed dữ liệu demo đa chủ trọ, nhiều khu trọ và 30+ phòng trọ phục vụ trình diễn đồ án.
        /// </summary>
        public static async Task SeedDemoDataAsync(DataContext context)
        {
            if (await context.Rooms.AnyAsync(r => r.RoomCode.StartsWith("HN-") || r.RoomCode.StartsWith("DN-") || r.RoomCode.StartsWith("SG-")))
            {
                return; // Đã có dữ liệu demo
            }

            // ── 1. Đảm bảo có Loại phòng ──────────────────────────────────────
            var roomTypes = await context.RoomTypes.ToListAsync();
            if (!roomTypes.Any())
            {
                roomTypes = new List<tblRoomType>
                {
                    new() { RoomTypeName = "Phòng trọ khép kín", Description = "Phòng trọ có WC riêng biệt, giờ giấc tự do", SortOrder = 1, IsActive = true },
                    new() { RoomTypeName = "Chung cư mini", Description = "Căn hộ mini 1 phòng ngủ hoặc studio, đầy đủ nội thất", SortOrder = 2, IsActive = true },
                    new() { RoomTypeName = "Homestay / Căn hộ dịch vụ", Description = "Không gian sống cao cấp, có dọn dẹp vệ sinh", SortOrder = 3, IsActive = true },
                    new() { RoomTypeName = "Ký túc xá cao cấp", Description = "Giường tầng riêng tư, đầy đủ máy lạnh tủ đồ", SortOrder = 4, IsActive = true }
                };
                context.RoomTypes.AddRange(roomTypes);
                await context.SaveChangesAsync();
            }

            // ── 2. Đảm bảo có Tiện ích ────────────────────────────────────────
            var amenities = await context.Amenities.ToListAsync();

            // ── 3. Lấy Tỉnh/Quận ──────────────────────────────────────────────
            var hn = await context.Provinces.Include(p => p.Districts).FirstOrDefaultAsync(p => p.Name.Contains("Hà Nội") || p.Code == "01");
            var dn = await context.Provinces.Include(p => p.Districts).FirstOrDefaultAsync(p => p.Name.Contains("Đà Nẵng") || p.Code == "48");
            var sg = await context.Provinces.Include(p => p.Districts).FirstOrDefaultAsync(p => p.Name.Contains("Hồ Chí Minh") || p.Code == "79");

            int hnProvId = hn?.ProvinceId ?? 1;
            int dnProvId = dn?.ProvinceId ?? (hnProvId > 0 ? hnProvId : 1);
            int sgProvId = sg?.ProvinceId ?? (dnProvId > 0 ? dnProvId : 1);

            int hnDist1 = hn?.Districts.FirstOrDefault(d => d.Name.Contains("Cầu Giấy"))?.DistrictId ?? hn?.Districts.FirstOrDefault()?.DistrictId ?? 1;
            int hnDist2 = hn?.Districts.FirstOrDefault(d => d.Name.Contains("Nam Từ Liêm"))?.DistrictId ?? hnDist1;

            int dnDist1 = dn?.Districts.FirstOrDefault(d => d.Name.Contains("Hải Châu"))?.DistrictId ?? dn?.Districts.FirstOrDefault()?.DistrictId ?? hnDist1;
            int dnDist2 = dn?.Districts.FirstOrDefault(d => d.Name.Contains("Sơn Trà"))?.DistrictId ?? dnDist1;

            int sgDist1 = sg?.Districts.FirstOrDefault(d => d.Name.Contains("1"))?.DistrictId ?? sg?.Districts.FirstOrDefault()?.DistrictId ?? hnDist1;
            int sgDist2 = sg?.Districts.FirstOrDefault(d => d.Name.Contains("Bình Thạnh"))?.DistrictId ?? sgDist1;

            // ── 4. Tạo Users và Chủ trọ Demo ─────────────────────────────────
            var defaultPw = HashPassword("Chutro@123");

            // Chủ trọ 1: Hà Nội
            var u1 = await context.Users.FirstOrDefaultAsync(u => u.Username == "chutro_hanoi");
            if (u1 == null)
            {
                u1 = new tblUser { Username = "chutro_hanoi", Email = "an.nguyen@phongtrohn.vn", PasswordHash = defaultPw, FullName = "Nguyễn Văn An", Phone = "0912345678", Role = "Landlord", IsActive = true };
                context.Users.Add(u1);
                await context.SaveChangesAsync();
            }
            var l1 = await context.Landlords.FirstOrDefaultAsync(l => l.UserId == u1.UserId);
            if (l1 == null)
            {
                l1 = new tblLandlord
                {
                    UserId = u1.UserId, FullName = "Nguyễn Văn An", Phone = "0912345678", Email = "an.nguyen@phongtrohn.vn",
                    IdentityNumber = "001200012345", Address = "Số 15 Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
                    Description = "Chủ hệ thống nhà trọ An Phát với hơn 5 năm kinh nghiệm quản lý phòng trọ sinh viên và người đi làm.",
                    BankId = "970422", BankName = "MB Bank", AccountNumber = "9999888877", AccountName = "NGUYEN VAN AN",
                    Status = LandlordStatus.Approved
                };
                context.Landlords.Add(l1);
                await context.SaveChangesAsync();
            }

            // Chủ trọ 2: Đà Nẵng
            var u2 = await context.Users.FirstOrDefaultAsync(u => u.Username == "chutro_danang");
            if (u2 == null)
            {
                u2 = new tblUser { Username = "chutro_danang", Email = "mai.tran@phongtrodn.vn", PasswordHash = defaultPw, FullName = "Trần Thị Mai", Phone = "0934567890", Role = "Landlord", IsActive = true };
                context.Users.Add(u2);
                await context.SaveChangesAsync();
            }
            var l2 = await context.Landlords.FirstOrDefaultAsync(l => l.UserId == u2.UserId);
            if (l2 == null)
            {
                l2 = new tblLandlord
                {
                    UserId = u2.UserId, FullName = "Trần Thị Mai", Phone = "0934567890", Email = "mai.tran@phongtrodn.vn",
                    IdentityNumber = "048200054321", Address = "Số 42 Bạch Đằng, Hải Châu, Đà Nẵng",
                    Description = "Hệ thống căn hộ mini và phòng trọ du lịch/lâu dài trung tâm TP Đà Nẵng view sông Hàn.",
                    BankId = "970436", BankName = "Vietcombank", AccountNumber = "8888777766", AccountName = "TRAN THI MAI",
                    Status = LandlordStatus.Approved
                };
                context.Landlords.Add(l2);
                await context.SaveChangesAsync();
            }

            // Chủ trọ 3: TP.HCM
            var u3 = await context.Users.FirstOrDefaultAsync(u => u.Username == "chutro_hcm");
            if (u3 == null)
            {
                u3 = new tblUser { Username = "chutro_hcm", Email = "long.le@phongtrohcm.vn", PasswordHash = defaultPw, FullName = "Lê Hoàng Long", Phone = "0978123456", Role = "Landlord", IsActive = true };
                context.Users.Add(u3);
                await context.SaveChangesAsync();
            }
            var l3 = await context.Landlords.FirstOrDefaultAsync(l => l.UserId == u3.UserId);
            if (l3 == null)
            {
                l3 = new tblLandlord
                {
                    UserId = u3.UserId, FullName = "Lê Hoàng Long", Phone = "0978123456", Email = "long.le@phongtrohcm.vn",
                    IdentityNumber = "079200098765", Address = "220 Điện Biên Phủ, Bình Thạnh, TP.HCM",
                    Description = "Căn hộ dịch vụ cao cấp Sài Gòn Pearl Riverside, gần các trường đại học HUTECH, Kinh tế Tài chính.",
                    BankId = "970407", BankName = "Techcombank", AccountNumber = "7777666655", AccountName = "LE HOANG LONG",
                    Status = LandlordStatus.Approved
                };
                context.Landlords.Add(l3);
                await context.SaveChangesAsync();
            }

            // Chủ trọ 4: Chờ duyệt (Pending)
            var u4 = await context.Users.FirstOrDefaultAsync(u => u.Username == "chutro_pending");
            if (u4 == null)
            {
                u4 = new tblUser { Username = "chutro_pending", Email = "bao.pham@demo.vn", PasswordHash = defaultPw, FullName = "Phạm Quốc Bảo", Phone = "0987654321", Role = "Landlord", IsActive = true };
                context.Users.Add(u4);
                await context.SaveChangesAsync();
            }
            var l4 = await context.Landlords.FirstOrDefaultAsync(l => l.UserId == u4.UserId);
            if (l4 == null)
            {
                l4 = new tblLandlord
                {
                    UserId = u4.UserId, FullName = "Phạm Quốc Bảo", Phone = "0987654321", Email = "bao.pham@demo.vn",
                    IdentityNumber = "036200033445", Address = "TP. Vinh, Nghệ An",
                    Description = "Đang chờ SuperAdmin xác minh hồ sơ đăng ký đối tác cho thuê.",
                    BankId = "970422", BankName = "MB Bank", AccountNumber = "1234567890", AccountName = "PHAM QUOC BAO",
                    Status = LandlordStatus.Pending
                };
                context.Landlords.Add(l4);
                await context.SaveChangesAsync();
            }

            // ── 5. Tạo Các Khu Trọ (Properties) ──────────────────────────────
            var prop1 = new tblProperty { LandlordId = l1.LandlordId, Name = "Khu trọ An Phát Cầu Giấy", Slug = "khu-tro-an-phat-cau-giay", ProvinceId = hnProvId, DistrictId = hnDist1, Address = "Số 15 Ngõ 86 Duy Tân, Cầu Giấy, Hà Nội", Latitude = 21.0313, Longitude = 105.7834, Description = "Tòa nhà 7 tầng có thang máy, bảo vệ 24/7, khóa vân tay cổng ra vào.", Status = PropertyStatus.Active };
            var prop2 = new tblProperty { LandlordId = l1.LandlordId, Name = "Chung cư mini Green House Nam Từ Liêm", Slug = "ccmn-green-house-nam-tu-liem", ProvinceId = hnProvId, DistrictId = hnDist2, Address = "Ngõ 199 Hồ Tùng Mậu, Cầu Diễn, Nam Từ Liêm, Hà Nội", Latitude = 21.0402, Longitude = 105.7645, Description = "Nhà mới xây 100%, full nội thất cao cấp, máy giặt riêng từng phòng.", Status = PropertyStatus.Active };

            var prop3 = new tblProperty { LandlordId = l2.LandlordId, Name = "Tòa nhà Sông Hàn Luxury", Slug = "toa-nha-song-han-luxury", ProvinceId = dnProvId, DistrictId = dnDist1, Address = "42 Bạch Đằng, P. Thạch Thang, Hải Châu, Đà Nẵng", Latitude = 16.0748, Longitude = 108.2240, Description = "View trực diện sông Hàn thơ mộng, không gian thoáng đãng yên tĩnh.", Status = PropertyStatus.Active };
            var prop4 = new tblProperty { LandlordId = l2.LandlordId, Name = "Căn hộ Biển Mỹ Khê", Slug = "can-ho-bien-my-khe", ProvinceId = dnProvId, DistrictId = dnDist2, Address = "120 Võ Nguyên Giáp, P. Phước Mỹ, Sơn Trà, Đà Nẵng", Latitude = 16.0620, Longitude = 108.2458, Description = "Cách bãi tắm Mỹ Khê 200m, thích hợp ở dài hạn nghỉ dưỡng.", Status = PropertyStatus.Active };

            var prop5 = new tblProperty { LandlordId = l3.LandlordId, Name = "Căn hộ Dịch vụ Bến Nghé Center", Slug = "can-ho-ben-nghe-center", ProvinceId = sgProvId, DistrictId = sgDist1, Address = "18 Nguyễn Bỉnh Khiêm, P. Bến Nghé, Quận 1, TP.HCM", Latitude = 10.7870, Longitude = 106.7020, Description = "Vị trí kim cương Quận 1, khu dân trí cao, an ninh tuyệt đối.", Status = PropertyStatus.Active };
            var prop6 = new tblProperty { LandlordId = l3.LandlordId, Name = "Chung cư mini Sài Gòn Pearl Riverside", Slug = "ccmn-saigon-pearl-riverside", ProvinceId = sgProvId, DistrictId = sgDist2, Address = "220 Điện Biên Phủ, Phường 22, Bình Thạnh, TP.HCM", Latitude = 10.7935, Longitude = 106.7160, Description = "Gần trường ĐH Ngoại Thương, HUTECH, Landmark 81, giờ giấc tự do.", Status = PropertyStatus.Active };

            var propList = new List<tblProperty> { prop1, prop2, prop3, prop4, prop5, prop6 };
            context.Properties.AddRange(propList);
            await context.SaveChangesAsync();

            // ── 6. Tạo 30+ Phòng Trọ (Rooms) ──────────────────────────────────
            var samplePhotos = new[]
            {
                "/images/rooms/12047932-d533-4c55-92ef-0f513b5f9148.jpg",
                "/images/rooms/14e65327-0b5a-4bbd-a567-8b77af254b0d.jpg",
                "/images/rooms/2ce94ca5-4ff9-4a47-bb7b-a974a3d0afe1.jpg",
                "/images/rooms/36aa30e7-e512-4b0d-971d-5f634007f51a.jpg",
                "/images/rooms/41f0b093-5762-40ef-b8c8-086d7e842b1d.jpg",
                "/images/rooms/424641bd-1867-48d8-a4c6-7bf0975f33c5.jpg",
                "/images/rooms/678c7198-2e0b-4d82-8da7-556cac7455d8.jpg",
                "/images/rooms/81b1ae99-79f6-48ae-a25e-3026fd60c522.jpg",
                "/images/rooms/85b70864-9bfe-4fe6-81a5-a3ceb86b96d8.jpg",
                "/images/rooms/8d366a42-d0ab-45c1-a2fe-a9313691ac60.jpg",
                "/images/rooms/d4157b2c-1e6d-405c-9376-89b92310e11e.jpg",
                "/images/rooms/ff377050-855d-4027-9c50-a509787a8197.jpg"
            };

            var roomsToInsert = new List<tblRoom>();
            int rIndex = 1;

            foreach (var prop in propList)
            {
                string prefix = prop.PropertyId switch
                {
                    1 => "HN-AP",
                    2 => "HN-GH",
                    3 => "DN-SH",
                    4 => "DN-MK",
                    5 => "SG-BN",
                    _ => "SG-SP"
                };

                for (int floor = 1; floor <= 6; floor++)
                {
                    string roomCode = $"{prefix}-{floor:D2}";
                    int rtId = roomTypes[(floor - 1) % roomTypes.Count].RoomTypeId;
                    decimal price = prop.ProvinceId == sgProvId
                        ? 4500000m + (floor * 400000m)
                        : (prop.ProvinceId == hnProvId ? 3200000m + (floor * 350000m) : 2800000m + (floor * 300000m));
                    double area = 20.0 + (floor * 3.5);

                    string photo = samplePhotos[(rIndex - 1) % samplePhotos.Length];
                    bool isFeatured = (floor % 2 == 0);
                    int views = 45 + (rIndex * 12);

                    // Một số phòng test các trạng thái khác nhau
                    var approvalStatus = RoomApprovalStatus.Published;
                    var roomStatus = RoomStatus.Available;
                    bool isPub = true;

                    if (rIndex == 3) { roomStatus = RoomStatus.Occupied; }
                    if (rIndex == 7) { approvalStatus = RoomApprovalStatus.Pending; isPub = false; }
                    if (rIndex == 11) { approvalStatus = RoomApprovalStatus.Draft; isPub = false; }
                    if (rIndex == 15) { roomStatus = RoomStatus.Occupied; }

                    var room = new tblRoom
                    {
                        PropertyId = prop.PropertyId,
                        RoomTypeId = rtId,
                        RoomCode = roomCode,
                        RoomName = $"Phòng {floor}0{floor} - {prop.Name}",
                        Title = $"Cho thuê phòng cao cấp {roomCode} tại {prop.Name} full đồ tiện nghi",
                        Slug = SlugHelper.GenerateSlug($"phong-{roomCode}-{prop.Name}"),
                        RoomPrice = price,
                        DefaultDeposit = price,
                        Area = area,
                        Floor = floor,
                        MaxOccupants = floor > 3 ? 4 : 2,
                        Capacity = floor > 3 ? 4 : 2,
                        Description = $"<p>Phòng trọ cao cấp tại <strong>{prop.Name}</strong>, địa chỉ {prop.Address}.</p><p>Trang bị sẵn: Điều hòa Inverter tiết kiệm điện, bình nóng lạnh Ariston, tủ lạnh, giường nệm cao cấp, tủ quần áo âm tường. Ban công đón gió tự nhiên, giờ giấc tự do không chung chủ.</p>",
                        Address = prop.Address,
                        Latitude = prop.Latitude,
                        Longitude = prop.Longitude,
                        ThumbnailImage = photo,
                        Status = roomStatus,
                        IsPublished = isPub,
                        ApprovalStatus = approvalStatus,
                        IsFeatured = isFeatured,
                        ViewCount = views,
                        PublishedAt = DateTime.Now.AddDays(-rIndex),
                        CreatedAt = DateTime.Now.AddDays(-rIndex)
                    };

                    roomsToInsert.Add(room);
                    rIndex++;
                }
            }

            context.Rooms.AddRange(roomsToInsert);
            await context.SaveChangesAsync();

            // ── 7. Gán Tiện ích và Ảnh phòng ──────────────────────────────────
            foreach (var r in roomsToInsert)
            {
                // Thêm 2 ảnh cho mỗi phòng
                context.RoomImages.Add(new tblRoomImage { RoomId = r.RoomId, Url = r.ThumbnailImage ?? samplePhotos[0], IsPrimary = true, SortOrder = 1 });
                string secondPhoto = samplePhotos[(r.RoomId * 3) % samplePhotos.Length];
                context.RoomImages.Add(new tblRoomImage { RoomId = r.RoomId, Url = secondPhoto, IsPrimary = false, SortOrder = 2 });

                // Gán 3 tiện ích ngẫu nhiên
                if (amenities.Any())
                {
                    int takeCount = Math.Min(4, amenities.Count);
                    for (int i = 0; i < takeCount; i++)
                    {
                        var am = amenities[(r.RoomId + i) % amenities.Count];
                        context.RoomAmenities.Add(new tblRoomAmenity { RoomId = r.RoomId, AmenityId = am.AmenityId });
                    }
                }
            }
            await context.SaveChangesAsync();

            // ── 8. Tạo Khách thuê, Hợp đồng & Hóa đơn Demo cho Dashboard ──────
            var tenantUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "khachthue_01");
            if (tenantUser == null)
            {
                tenantUser = new tblUser { Username = "khachthue_01", Email = "khach01@gmail.com", PasswordHash = HashPassword("Khach@123"), FullName = "Hoàng Minh Tuấn", Phone = "0988776655", Role = "User", IsActive = true };
                context.Users.Add(tenantUser);
                await context.SaveChangesAsync();
            }

            var t1 = new tblTenant { LandlordId = l1.LandlordId, FullName = "Hoàng Minh Tuấn", Phone = "0988776655", Email = "khach01@gmail.com", IdentityNumber = "001201099887", Username = "khachthue_01", PasswordHash = HashPassword("Khach@123"), IsActive = true };
            var t2 = new tblTenant { LandlordId = l2.LandlordId, FullName = "Đỗ Thu Hương", Phone = "0977665544", Email = "huong.do@gmail.com", IdentityNumber = "048201077665", IsActive = true };
            var t3 = new tblTenant { LandlordId = l3.LandlordId, FullName = "Nguyễn Quang Hải", Phone = "0966554433", Email = "hai.nguyen@gmail.com", IdentityNumber = "079201055443", IsActive = true };
            context.Tenants.AddRange(t1, t2, t3);
            await context.SaveChangesAsync();

            // Hợp đồng
            var occupiedRooms = roomsToInsert.Where(r => r.Status == RoomStatus.Occupied).ToList();
            if (occupiedRooms.Count >= 2)
            {
                var rOccupied1 = occupiedRooms[0];
                var c1 = new tblContract
                {
                    RoomId = rOccupied1.RoomId,
                    TenantId = t1.TenantId,
                    LandlordId = l1.LandlordId,
                    ContractCode = $"HD-{DateTime.Today:yyyyMM}-001",
                    StartDate = DateTime.Today.AddMonths(-3),
                    EndDate = DateTime.Today.AddDays(25), // Sắp hết hạn trong 30 ngày
                    MonthlyRent = rOccupied1.RoomPrice,
                    Deposit = rOccupied1.RoomPrice,
                    Status = ContractStatus.Active
                };
                context.Contracts.Add(c1);
                await context.SaveChangesAsync();

                // Hóa đơn tháng hiện tại
                var invPaid = new tblInvoice
                {
                    ContractId = c1.ContractId,
                    RoomId = rOccupied1.RoomId,
                    LandlordId = l1.LandlordId,
                    InvoiceCode = $"INV-{DateTime.Today:yyyyMM}-001",
                    BillingMonth = DateTime.Today.Month,
                    BillingYear = DateTime.Today.Year,
                    DueDate = DateTime.Today.AddDays(5),
                    TotalAmount = rOccupied1.RoomPrice + 450000m,
                    Status = InvoiceStatus.Paid,
                    PaidDate = DateTime.Today.AddDays(-2),
                    Notes = "Đã thanh toán qua VietQR"
                };
                var invUnpaid = new tblInvoice
                {
                    ContractId = c1.ContractId,
                    RoomId = rOccupied1.RoomId,
                    LandlordId = l1.LandlordId,
                    InvoiceCode = $"INV-{DateTime.Today:yyyyMM}-002",
                    BillingMonth = DateTime.Today.Month,
                    BillingYear = DateTime.Today.Year,
                    DueDate = DateTime.Today.AddDays(5),
                    TotalAmount = 3500000m,
                    Status = InvoiceStatus.Unpaid,
                    Notes = "Chưa nhận thanh toán"
                };
                context.Invoices.AddRange(invPaid, invUnpaid);

                // Yêu cầu xem phòng
                context.BookingRequests.Add(new tblBookingRequest
                {
                    RoomId = roomsToInsert[0].RoomId,
                    LandlordId = l1.LandlordId,
                    FullName = "Vũ Đình Trọng",
                    Phone = "0915667788",
                    Email = "trong.vu@gmail.com",
                    PreferredDate = "Thứ Bảy, 14:00",
                    ViewingTime = DateTime.Today.AddDays(2),
                    Message = "Muốn xem phòng chiều thứ Bảy tuần này",
                    RequestType = BookingRequestType.Viewing,
                    Status = BookingRequestStatus.Pending
                });
            }

            await context.SaveChangesAsync();
        }
    }
}

