using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Services
{
    public class CurrentLandlordService : ICurrentLandlordService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public CurrentLandlordService(
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        private HttpContext? HttpContext => _httpContextAccessor.HttpContext;

        public int? GetCurrentUserId()
        {
            var context = HttpContext;
            if (context == null) return null;

            // 1. Thử đọc từ Claims
            var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? context.User?.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdClaim, out int uid)) return uid;

            // 2. Thử đọc từ Session
            var sessionUser = context.Session?.GetString("NormalUser");
            if (int.TryParse(sessionUser, out int sUid)) return sUid;

            return null;
        }

        public string? GetCurrentUserRole()
        {
            var context = HttpContext;
            if (context == null) return null;

            // 1. Thử đọc từ Claims
            var roleClaim = context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(roleClaim)) return roleClaim;

            // 2. Thử đọc từ Session
            if (!string.IsNullOrEmpty(context.Session?.GetString("AdminUser")))
            {
                var role = context.Session?.GetString("AdminRole");
                return !string.IsNullOrEmpty(role) ? role : "SuperAdmin";
            }

            if (!string.IsNullOrEmpty(context.Session?.GetString("TenantUser")))
            {
                return "Tenant";
            }

            if (!string.IsNullOrEmpty(context.Session?.GetString("NormalUser")))
            {
                return "User";
            }

            return null;
        }

        public bool IsSuperAdmin()
        {
            var role = GetCurrentUserRole();
            return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsLandlord()
        {
            var role = GetCurrentUserRole();
            return string.Equals(role, "Landlord", StringComparison.OrdinalIgnoreCase) || IsSuperAdmin();
        }

        public bool IsTenant()
        {
            var role = GetCurrentUserRole();
            return string.Equals(role, "Tenant", StringComparison.OrdinalIgnoreCase);
        }

        public int? GetCurrentLandlordId()
        {
            var context = HttpContext;
            if (context == null) return null;

            // 1. Kiểm tra Claim LandlordId
            var landlordClaim = context.User?.FindFirst("LandlordId")?.Value;
            if (int.TryParse(landlordClaim, out int lid)) return lid;

            // 2. Kiểm tra Session LandlordId
            var sessionLid = context.Session?.GetString("LandlordId");
            if (int.TryParse(sessionLid, out int sLid)) return sLid;

            // 3. Tra cứu từ CSDL theo UserId hoặc Admin Session
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            int? userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                var landlord = db.Landlords.AsNoTracking().FirstOrDefault(l => l.UserId == userId.Value);
                if (landlord != null)
                {
                    context.Session?.SetString("LandlordId", landlord.LandlordId.ToString());
                    return landlord.LandlordId;
                }
            }

            // Nếu là Admin cũ đăng nhập qua session AdminUser
            var adminUser = context.Session?.GetString("AdminUser");
            if (!string.IsNullOrEmpty(adminUser))
            {
                var user = db.Users.AsNoTracking().FirstOrDefault(u => u.Username == adminUser);
                if (user != null)
                {
                    var landlord = db.Landlords.AsNoTracking().FirstOrDefault(l => l.UserId == user.UserId);
                    if (landlord != null) return landlord.LandlordId;
                }

                // Gán mặc định vào Landlord đầu tiên (Chủ trọ mặc định hệ thống)
                var defaultLandlord = db.Landlords.AsNoTracking().OrderBy(l => l.LandlordId).FirstOrDefault();
                if (defaultLandlord != null) return defaultLandlord.LandlordId;
            }

            return null;
        }

        public bool IsApprovedLandlord()
        {
            if (IsSuperAdmin()) return true;

            var landlordId = GetCurrentLandlordId();
            if (!landlordId.HasValue) return false;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            var landlord = db.Landlords.AsNoTracking().FirstOrDefault(l => l.LandlordId == landlordId.Value);

            return landlord?.Status == LandlordStatus.Approved;
        }

        public async Task<tblLandlord?> GetCurrentLandlordAsync()
        {
            var landlordId = GetCurrentLandlordId();
            if (!landlordId.HasValue) return null;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            return await db.Landlords.AsNoTracking().FirstOrDefaultAsync(l => l.LandlordId == landlordId.Value);
        }
    }
}
