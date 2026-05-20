using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using System.Security.Cryptography;
using System.Text;

namespace QuanLyPhongTro.Controllers
{
    public class AccountController : Controller
    {
        private const string SESSION_TENANT = "TenantUser";
        private const string SESSION_USER   = "NormalUser";
        private const string SESSION_ADMIN  = "AdminUser";

        private readonly DataContext _context;
        private readonly IConfiguration _config;

        public AccountController(DataContext context, IConfiguration config)
        {
            _context = context;
            _config  = config;
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password + "PhongTro@2026#Salt");
            return Convert.ToHexString(sha256.ComputeHash(bytes)).ToLower();
        }

        // ── Login (view đã bỏ → redirect về trang chủ) ──────────────────

        // GET: /Account/Login  — giữ lại để không bị 404 nếu còn link cũ
        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString(SESSION_ADMIN)))
                return RedirectToAction("Index", "Home", new { area = "Admin" });

            return RedirectToAction("Index", "Home");
        }

        // ── Register (view đã bỏ → redirect về trang chủ) ────────────────

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register() => RedirectToAction("Index", "Home");

        // ── LoginModal (AJAX) ────────────────────────────────────────────

        // POST: /Account/LoginModal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginModal(LoginViewModel model, string? returnUrl)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin." });

            var input = model.UsernameOrEmail.Trim();
            var hash  = HashPassword(model.Password);

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t =>
                t.IsActive && t.PasswordHash == hash &&
                (t.Username == input || t.Email == input));
            if (tenant != null)
            {
                TempData.Clear();
                HttpContext.Session.SetString(SESSION_TENANT, tenant.TenantId.ToString());
                HttpContext.Session.SetString("TenantName", tenant.FullName);
                var url = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("Index", "Home")!;
                return Json(new { success = true, redirectUrl = url });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.IsActive && u.Role == "User" && u.PasswordHash == hash &&
                (u.Username == input || u.Email == input));
            if (user != null)
            {
                TempData.Clear();
                HttpContext.Session.SetString(SESSION_USER, user.UserId.ToString());
                HttpContext.Session.SetString("UserName", user.Username);
                var url = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("Index", "Home")!;
                return Json(new { success = true, redirectUrl = url });
            }

            var admin = await _context.Users.FirstOrDefaultAsync(u =>
                u.IsActive && u.Role == "Admin" && u.PasswordHash == hash &&
                (u.Username == input || u.Email == input));
            if (admin != null)
            {
                TempData.Clear();
                HttpContext.Session.SetString(SESSION_ADMIN, admin.Username);
                var url = Url.Action("Index", "Home", new { area = "Admin" })!;
                return Json(new { success = true, redirectUrl = url });
            }

            return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không đúng." });
        }

        // ── RegisterModal (AJAX) ──────────────────────────────────────────

        // POST: /Account/RegisterModal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterModal(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Dữ liệu không hợp lệ.";
                return Json(new { success = false, message = firstError });
            }

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                return Json(new { success = false, message = "Tên đăng nhập đã được sử dụng." });

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                return Json(new { success = false, message = "Email đã được đăng ký." });

            var user = new tblUser
            {
                Username     = model.Username.Trim(),
                Email        = model.Email.Trim(),
                FullName     = model.FullName?.Trim(),
                Phone        = model.Phone?.Trim(),
                PasswordHash = HashPassword(model.Password),
                IsActive     = true,
                CreatedAt    = DateTime.Now
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true, switchToLogin = true, message = "Đăng ký thành công! Vui lòng đăng nhập." });
        }

        // ── Invoices (chỉ dành cho Tenant) ───────────────────────────────

        // GET: /Account/Invoices
        [HttpGet]
        public async Task<IActionResult> Invoices()
        {
            var tenantIdStr = HttpContext.Session.GetString(SESSION_TENANT);
            if (string.IsNullOrEmpty(tenantIdStr) || !int.TryParse(tenantIdStr, out int tenantId))
                return RedirectToAction("Index", "Home");

            var invoices = await _context.Invoices
                .Include(i => i.Room)
                .Include(i => i.Contract)
                .Include(i => i.InvoiceDetails).ThenInclude(d => d.Service)
                .Where(i => i.Contract != null && i.Contract.TenantId == tenantId)
                .OrderByDescending(i => i.BillingYear)
                .ThenByDescending(i => i.BillingMonth)
                .ToListAsync();

            ViewBag.BankId      = _config["BankPayment:BankId"]        ?? "970422";
            ViewBag.BankAccount = _config["BankPayment:AccountNumber"] ?? "";
            ViewBag.BankName    = _config["BankPayment:BankName"]      ?? "MB Bank";
            ViewBag.BankOwner   = _config["BankPayment:AccountName"]   ?? "";

            return View(invoices);
        }

        // ── Info ─────────────────────────────────────────────────────────

        // GET: /Account/Info
        [HttpGet]
        public async Task<IActionResult> Info()
        {
            var vm = await BuildProfileViewModel();
            if (vm == null) return RedirectToAction("Index", "Home");

            return View(vm);
        }

        // POST: /Account/Info  — lưu thông tin cá nhân
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Info(ProfileViewModel model, IFormFile? avatarFile)
        {
            foreach (var key in new[] { "CurrentPassword","NewPassword","ConfirmPassword",
                "IdentityNumber","TenantUsername","Username" })
                ModelState.Remove(key);

            if (!ModelState.IsValid)
                return View(model);

            var tenantIdStr = HttpContext.Session.GetString(SESSION_TENANT);
            var userIdStr   = HttpContext.Session.GetString(SESSION_USER);

            if (!string.IsNullOrEmpty(tenantIdStr) && int.TryParse(tenantIdStr, out int tenantId))
            {
                var tenant = await _context.Tenants.FindAsync(tenantId);
                if (tenant == null) return NotFound();

                if (!string.IsNullOrWhiteSpace(model.Email) &&
                    await _context.Tenants.AnyAsync(t => t.TenantId != tenantId && t.Email == model.Email.Trim()))
                {
                    ModelState.AddModelError("Email", "Email đã được dùng bởi tài khoản khác.");
                    return View(model);
                }

                tenant.FullName          = model.FullName.Trim();
                tenant.Phone             = model.Phone?.Trim();
                tenant.Email             = model.Email?.Trim();
                tenant.DateOfBirth       = model.DateOfBirth;
                tenant.Gender            = model.Gender;
                tenant.PermanentAddress  = model.PermanentAddress?.Trim();
                tenant.UpdatedAt         = DateTime.Now;
                if (avatarFile != null)
                    tenant.Avatar = await SaveAvatar(avatarFile, tenant.Avatar);

                await _context.SaveChangesAsync();
                HttpContext.Session.SetString("TenantName", tenant.FullName);
            }
            else if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return NotFound();

                if (!string.IsNullOrWhiteSpace(model.Email) &&
                    await _context.Users.AnyAsync(u => u.UserId != userId && u.Email == model.Email.Trim()))
                {
                    ModelState.AddModelError("Email", "Email đã được dùng bởi tài khoản khác.");
                    return View(model);
                }

                user.FullName  = model.FullName.Trim();
                user.Phone     = model.Phone?.Trim();
                user.Email     = string.IsNullOrWhiteSpace(model.Email) ? user.Email : model.Email.Trim();
                user.UpdatedAt = DateTime.Now;
                if (avatarFile != null)
                    user.Avatar = await SaveAvatar(avatarFile, user.Avatar);

                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Đã cập nhật thông tin thành công.";
            return RedirectToAction(nameof(Info));
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ProfileViewModel model)
        {
            foreach (var key in new[] { "FullName","Phone","Email","Gender","PermanentAddress",
                "DateOfBirth","IdentityNumber","TenantUsername","Username" })
                ModelState.Remove(key);

            if (!ModelState.IsValid)
            {
                TempData["ShowPasswordTab"] = "1";
                return RedirectToAction(nameof(Info));
            }

            var currentHash = HashPassword(model.CurrentPassword!);
            var newHash     = HashPassword(model.NewPassword!);

            var tenantIdStr = HttpContext.Session.GetString(SESSION_TENANT);
            var userIdStr   = HttpContext.Session.GetString(SESSION_USER);

            if (!string.IsNullOrEmpty(tenantIdStr) && int.TryParse(tenantIdStr, out int tenantId))
            {
                var tenant = await _context.Tenants.FindAsync(tenantId);
                if (tenant == null || tenant.PasswordHash != currentHash)
                {
                    TempData["Error"] = "Mật khẩu hiện tại không đúng.";
                    TempData["ShowPasswordTab"] = "1";
                    return RedirectToAction(nameof(Info));
                }
                tenant.PasswordHash = newHash;
                tenant.UpdatedAt    = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            else if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.PasswordHash != currentHash)
                {
                    TempData["Error"] = "Mật khẩu hiện tại không đúng.";
                    TempData["ShowPasswordTab"] = "1";
                    return RedirectToAction(nameof(Info));
                }
                user.PasswordHash = newHash;
                user.UpdatedAt    = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Đã đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Info));
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private async Task<ProfileViewModel?> BuildProfileViewModel()
        {
            var tenantIdStr = HttpContext.Session.GetString(SESSION_TENANT);
            var userIdStr   = HttpContext.Session.GetString(SESSION_USER);

            if (!string.IsNullOrEmpty(tenantIdStr) && int.TryParse(tenantIdStr, out int tenantId))
            {
                var t = await _context.Tenants.FindAsync(tenantId);
                if (t == null) return null;
                return new ProfileViewModel
                {
                    UserType         = "Tenant",
                    FullName         = t.FullName,
                    Phone            = t.Phone,
                    Email            = t.Email,
                    Avatar           = t.Avatar,
                    IdentityNumber   = t.IdentityNumber,
                    TenantUsername   = t.Username,
                    DateOfBirth      = t.DateOfBirth,
                    Gender           = t.Gender,
                    PermanentAddress = t.PermanentAddress
                };
            }

            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                var u = await _context.Users.FindAsync(userId);
                if (u == null) return null;
                return new ProfileViewModel
                {
                    UserType = "User",
                    FullName = u.FullName ?? u.Username,
                    Phone    = u.Phone,
                    Email    = u.Email,
                    Avatar   = u.Avatar,
                    Username = u.Username
                };
            }

            return null;
        }

        private async Task<string?> SaveAvatar(IFormFile file, string? oldPath)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowed.Contains(ext)) return oldPath;

            var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
            Directory.CreateDirectory(dir);

            if (!string.IsNullOrEmpty(oldPath))
            {
                var old = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldPath.TrimStart('/'));
                if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
            }

            var fileName = $"{Guid.NewGuid()}{ext}";
            using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/images/avatars/{fileName}";
        }

        // ── API: Hóa đơn chưa thanh toán / trễ hạn ──────────────────────
        [HttpGet]
        public async Task<IActionResult> GetOverdueInvoices()
        {
            var tenantIdStr = HttpContext.Session.GetString(SESSION_TENANT);
            if (string.IsNullOrEmpty(tenantIdStr) || !int.TryParse(tenantIdStr, out int tenantId))
                return Json(new { invoices = Array.Empty<object>() });

            var now = DateTime.Now;
            var unpaid = await _context.Invoices
                .Include(i => i.Room)
                .Where(i => i.Contract != null && i.Contract.TenantId == tenantId
                         && (i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Overdue))
                .OrderBy(i => i.DueDate)
                .Select(i => new
                {
                    i.InvoiceCode,
                    RoomName    = i.Room != null ? i.Room.RoomName : "N/A",
                    i.BillingMonth,
                    i.BillingYear,
                    DueDate     = i.DueDate.ToString("dd/MM/yyyy"),
                    IsOverdue   = i.DueDate < now,
                    TotalAmount = i.TotalAmount.ToString("N0"),
                    Status      = i.Status == InvoiceStatus.Overdue ? "Quá hạn" : "Chưa thanh toán"
                })
                .ToListAsync();

            return Json(new { invoices = unpaid });
        }

        // ── API: Thông báo phản hồi lịch hẹn xem phòng ─────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetBookingNotifications()
        {
            var phone = await GetLoggedInPhone();
            if (string.IsNullOrWhiteSpace(phone))
                return Json(new { bookings = Array.Empty<object>() });

            var normalizedPhone = NormalizePhone(phone);

            var rawBookings = await _context.BookingRequests
                .Include(b => b.Room)
                .Where(b => b.RequestType == BookingRequestType.ViewingRequest
                         && b.Status != BookingRequestStatus.Pending
                         && !b.IsGuestNotified
                         && (b.Phone.Trim() == phone
                             || b.Phone.Replace(" ", "").Replace(".", "").Replace("-", "") == normalizedPhone))
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new
                {
                    b.RequestId,
                    RoomName = b.Room != null ? b.Room.RoomName : $"Phòng #{b.RoomId}",
                    b.PreferredDate,
                    b.Status,
                    b.AdminNote,
                    b.CreatedAt
                })
                .ToListAsync();

            var bookings = rawBookings.Select(b => new
            {
                b.RequestId,
                b.RoomName,
                PreferredDate = string.IsNullOrWhiteSpace(b.PreferredDate) ? "Chưa chọn" : b.PreferredDate,
                Status = b.Status == BookingRequestStatus.Accepted ? "Accepted" : "Rejected",
                StatusText = b.Status == BookingRequestStatus.Accepted ? "Đã chấp nhận" : "Đã từ chối",
                AdminNote = string.IsNullOrWhiteSpace(b.AdminNote) ? "" : b.AdminNote,
                CreatedAt = b.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            }).ToList();

            return Json(new { bookings });
        }

        [HttpPost]
        public async Task<IActionResult> MarkBookingNotified([FromBody] BookingNotificationMarkRequest request)
        {
            if (request.RequestIds == null || request.RequestIds.Count == 0)
                return Json(new { success = true, updated = 0 });

            var phone = await GetLoggedInPhone();
            if (string.IsNullOrWhiteSpace(phone))
                return Unauthorized();

            var normalizedPhone = NormalizePhone(phone);
            var requestIds = request.RequestIds.Distinct().ToList();

            var bookings = await _context.BookingRequests
                .Where(b => requestIds.Contains(b.RequestId)
                         && b.RequestType == BookingRequestType.ViewingRequest
                         && b.Status != BookingRequestStatus.Pending
                         && !b.IsGuestNotified
                         && (b.Phone.Trim() == phone
                             || b.Phone.Replace(" ", "").Replace(".", "").Replace("-", "") == normalizedPhone))
                .ToListAsync();

            foreach (var booking in bookings)
                booking.IsGuestNotified = true;

            await _context.SaveChangesAsync();
            return Json(new { success = true, updated = bookings.Count });
        }

        // ── Logout ───────────────────────────────────────────────────────

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Remove(SESSION_ADMIN);
            HttpContext.Session.Remove(SESSION_TENANT);
            HttpContext.Session.Remove("TenantName");
            HttpContext.Session.Remove(SESSION_USER);
            HttpContext.Session.Remove("UserName");
            TempData["Success"] = "Đã đăng xuất thành công.";
            return RedirectToAction("Index", "Home");
        }

        private async Task<string?> GetLoggedInPhone()
        {
            var tenantIdStr = HttpContext.Session.GetString(SESSION_TENANT);
            if (!string.IsNullOrEmpty(tenantIdStr) && int.TryParse(tenantIdStr, out int tenantId))
                return (await _context.Tenants
                    .Where(t => t.TenantId == tenantId)
                    .Select(t => t.Phone)
                    .FirstOrDefaultAsync())?.Trim();

            var userIdStr = HttpContext.Session.GetString(SESSION_USER);
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                return (await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => u.Phone)
                    .FirstOrDefaultAsync())?.Trim();

            return null;
        }

        private static string NormalizePhone(string phone)
        {
            return phone.Trim()
                .Replace(" ", "")
                .Replace(".", "")
                .Replace("-", "");
        }
    }

    public sealed class BookingNotificationMarkRequest
    {
        public List<int> RequestIds { get; set; } = new();
    }
}
