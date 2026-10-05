using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Services;
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
        private readonly ICurrentLandlordService _currentLandlord;

        public AccountController(DataContext context, IConfiguration config, ICurrentLandlordService currentLandlord)
        {
            _context = context;
            _config  = config;
            _currentLandlord = currentLandlord;
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

                u.IsActive && u.PasswordHash == hash &&

                (u.Username == input || u.Email == input));

            if (user != null)

            {

                // Nếu là Chủ trọ (Landlord)

                if (string.Equals(user.Role, "Landlord", StringComparison.OrdinalIgnoreCase))
                {
                    var landlord = await _context.Landlords.FirstOrDefaultAsync(l => l.UserId == user.UserId);
                    if (landlord != null && landlord.Status == QuanLyPhongTro.Models.LandlordStatus.Suspended)
                    {
                        return Json(new { success = false, message = "Tài khoản chủ trọ của bạn đã bị tạm khóa (Suspended). Vui lòng liên hệ ban quản trị." });
                    }

                    if (landlord != null && landlord.Status == QuanLyPhongTro.Models.LandlordStatus.Rejected)
                    {
                        return Json(new { success = false, message = $"Tài khoản chủ trọ của bạn đã bị từ chối: {landlord.RejectReason ?? "Không đạt yêu cầu"}. Vui lòng liên hệ ban quản trị." });
                    }

                    TempData.Clear();
                    HttpContext.Session.SetString(SESSION_ADMIN, user.Username);
                    HttpContext.Session.SetString("AdminRole", "Landlord");
                    if (landlord != null)
                    {
                        HttpContext.Session.SetString("LandlordId", landlord.LandlordId.ToString());
                        HttpContext.Session.SetString("LandlordStatus", landlord.Status.ToString());
                    }
                    return Json(new { success = true, redirectUrl = Url.Action("Index", "Home", new { area = "Admin" })! });
                }

                // Nếu là SuperAdmin hoặc Admin cũ
                if (string.Equals(user.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    TempData.Clear();
                    HttpContext.Session.SetString(SESSION_ADMIN, user.Username);
                    HttpContext.Session.SetString("AdminRole", "SuperAdmin");

                    var landlord = await _context.Landlords.FirstOrDefaultAsync(l => l.UserId == user.UserId)
                                ?? await _context.Landlords.OrderBy(l => l.LandlordId).FirstOrDefaultAsync();
                    if (landlord != null)
                    {
                        HttpContext.Session.SetString("LandlordId", landlord.LandlordId.ToString());
                        HttpContext.Session.SetString("LandlordStatus", landlord.Status.ToString());
                    }
                    return Json(new { success = true, redirectUrl = Url.Action("Index", "Home", new { area = "Admin" })! });
                }

                // Người dùng thông thường
                TempData.Clear();
                HttpContext.Session.SetString(SESSION_USER, user.UserId.ToString());
                HttpContext.Session.SetString("UserName", user.Username);
                var url = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("Index", "Home")!;
                return Json(new { success = true, redirectUrl = url });
            }

            // Kiểm tra nếu tài khoản người dùng hoặc người thuê bị vô hiệu hóa
            var inactiveUser = await _context.Users.FirstOrDefaultAsync(u =>
                !u.IsActive && u.PasswordHash == hash && (u.Username == input || u.Email == input));
            if (inactiveUser != null)
            {
                return Json(new { success = false, message = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ ban quản trị." });
            }

            var inactiveTenant = await _context.Tenants.FirstOrDefaultAsync(t =>
                !t.IsActive && t.PasswordHash == hash && (t.Username == input || t.Email == input));
            if (inactiveTenant != null)
            {
                return Json(new { success = false, message = "Tài khoản người thuê của bạn đã bị khóa. Vui lòng liên hệ ban quản trị." });
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

            var adminUser = HttpContext.Session.GetString(SESSION_ADMIN);
            if (!string.IsNullOrEmpty(adminUser))
            {
                return RedirectToAction("Index", "Invoice", new { area = "Admin" });
            }

            var tenantIdStr = HttpContext.Session.GetString(SESSION_TENANT);
            int tenantId = 0;
            if (!string.IsNullOrEmpty(tenantIdStr) && int.TryParse(tenantIdStr, out int parsedTid))
            {
                tenantId = parsedTid;
            }
            else
            {
                var userIdStr = HttpContext.Session.GetString(SESSION_USER);
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int uid))
                {
                    var user = await _context.Users.FindAsync(uid);
                    if (user != null)
                    {
                        var matchingTenant = await _context.Tenants.FirstOrDefaultAsync(t => 
                            (!string.IsNullOrEmpty(user.Email) && t.Email == user.Email) ||
                            (!string.IsNullOrEmpty(user.Phone) && t.Phone == user.Phone) ||
                            t.Username == user.Username);
                        if (matchingTenant != null)
                        {
                            tenantId = matchingTenant.TenantId;
                        }
                    }
                }
            }

            if (tenantId <= 0)
                return RedirectToAction("Index", "Home");



            var invoices = await _context.Invoices

                .Include(i => i.Room).ThenInclude(r => r!.Property).ThenInclude(p => p!.Landlord)

                .Include(i => i.Contract).ThenInclude(c => c!.Landlord)

                .Include(i => i.Landlord)

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

            else if (!string.IsNullOrEmpty(HttpContext.Session.GetString(SESSION_ADMIN)))
            {
                var adminUserStr = HttpContext.Session.GetString(SESSION_ADMIN);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == adminUserStr);
                if (user == null) return NotFound();

                if (!string.IsNullOrWhiteSpace(model.Email) &&
                    await _context.Users.AnyAsync(u => u.UserId != user.UserId && u.Email == model.Email.Trim()))
                {
                    ModelState.AddModelError("Email", "Email đã được dùng bởi tài khoản khác.");
                    return View(model);
                }

                user.FullName  = model.FullName.Trim();
                user.Phone     = model.Phone?.Trim();
                user.Email     = string.IsNullOrWhiteSpace(model.Email) ? user.Email : model.Email.Trim();
                user.UpdatedAt = DateTime.Now;

                if (avatarFile != null)
                {
                    user.Avatar = await SaveAvatar(avatarFile, user.Avatar);
                }

                var landlord = await _context.Landlords.FirstOrDefaultAsync(l => l.UserId == user.UserId);
                if (landlord != null)
                {
                    landlord.FullName = user.FullName;
                    landlord.Phone = user.Phone;
                    if (!string.IsNullOrWhiteSpace(user.Email)) landlord.Email = user.Email;
                    if (avatarFile != null) landlord.Avatar = user.Avatar;
                    landlord.UpdatedAt = DateTime.Now;
                }

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

            else if (!string.IsNullOrEmpty(HttpContext.Session.GetString(SESSION_ADMIN)))
            {
                var adminUserStr = HttpContext.Session.GetString(SESSION_ADMIN);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == adminUserStr);
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

            var adminUser = HttpContext.Session.GetString(SESSION_ADMIN);
            if (!string.IsNullOrEmpty(adminUser))
            {
                var u = await _context.Users.FirstOrDefaultAsync(u => u.Username == adminUser);
                if (u != null)
                {
                    var landlord = await _context.Landlords.FirstOrDefaultAsync(l => l.UserId == u.UserId);
                    return new ProfileViewModel
                    {
                        UserType = u.Role ?? "Landlord",
                        FullName = landlord?.FullName ?? u.FullName ?? u.Username,
                        Phone    = landlord?.Phone ?? u.Phone,
                        Email    = landlord?.Email ?? u.Email,
                        Avatar   = landlord?.Avatar ?? u.Avatar,
                        Username = u.Username,
                        IdentityNumber = landlord?.IdentityNumber,
                        PermanentAddress = landlord?.Address
                    };
                }
            }

            return null;

        }



        private async Task<string?> SaveAvatar(IFormFile? file, string? oldPath)
        {
            if (file == null || file.Length == 0 || file.Length > 5 * 1024 * 1024) return oldPath;

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowed.Contains(ext)) return oldPath;

            var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
            Directory.CreateDirectory(dir);

            if (!string.IsNullOrEmpty(oldPath) && oldPath.StartsWith("/images/avatars/", StringComparison.OrdinalIgnoreCase))
            {
                var old = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldPath.TrimStart('/'));
                if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
            }

            var fileName = $"{Guid.NewGuid():N}{ext}";
            using (var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

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



        // POST: /Account/ClearAdminSession

        [HttpPost]

        public IActionResult ClearAdminSession()

        {

            HttpContext.Session.Remove(SESSION_ADMIN);

            return Json(new { success = true });

        }



        // ── Logout ───────────────────────────────────────────────────────





        // ── Đăng ký Chủ trọ mới (Gửi yêu cầu xét duyệt vào Admin) ─────────
        [HttpGet]
        [Route("dang-tin")]
        [Route("dang-ky-chu-tro")]
        [Route("Account/RegisterLandlord")]
        public async Task<IActionResult> RegisterLandlord()
        {
            // 1. Nếu đã là Chủ trọ đã được duyệt (Approved) hoặc SuperAdmin -> chuyển thẳng sang Đăng tin phòng mới
            if (_currentLandlord.IsApprovedLandlord())
            {
                return RedirectToAction("Create", "Room", new { area = "Admin" });
            }

            // 2. Nếu đã đăng ký nhưng đang chờ duyệt (Pending) -> hiển thị trang trạng thái hồ sơ
            var currentLandlord = await _currentLandlord.GetCurrentLandlordAsync();
            if (currentLandlord != null && currentLandlord.Status == QuanLyPhongTro.Models.LandlordStatus.Pending)
            {
                return RedirectToAction("LandlordStatus");
            }

            var model = new RegisterLandlordViewModel();

            // Nếu đã đăng nhập tài khoản thường (User), điền sẵn thông tin
            int? currentUserId = _currentLandlord.GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var user = await _context.Users.FindAsync(currentUserId.Value);
                if (user != null)
                {
                    model.Username = user.Username;
                    model.Email = user.Email;
                    model.FullName = user.FullName;
                    model.Phone = user.Phone ?? string.Empty;
                    model.IsExistingUser = true;
                }
            }

            ViewBag.Provinces = await _context.Provinces.OrderBy(p => p.Name).ToListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("dang-tin")]
        [Route("dang-ky-chu-tro")]
        [Route("Account/RegisterLandlord")]
        public async Task<IActionResult> RegisterLandlord(RegisterLandlordViewModel model)
        {
            int? currentUserId = _currentLandlord.GetCurrentUserId();
            tblUser? user = null;

            if (currentUserId.HasValue)
            {
                user = await _context.Users.FindAsync(currentUserId.Value);
                if (user != null)
                {
                    model.IsExistingUser = true;
                }
            }

            // Nếu là người dùng mới, validate Username & Password
            if (!model.IsExistingUser)
            {
                if (string.IsNullOrWhiteSpace(model.Username))
                    ModelState.AddModelError("Username", "Vui lòng nhập tên đăng nhập.");
                if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 6)
                    ModelState.AddModelError("Password", "Mật khẩu tối thiểu 6 ký tự.");

                if (await _context.Users.AnyAsync(u => u.Username == model.Username.Trim()))
                    ModelState.AddModelError("Username", "Tên đăng nhập đã được sử dụng. Vui lòng chọn tên khác.");

                if (await _context.Users.AnyAsync(u => u.Email == model.Email.Trim()))
                    ModelState.AddModelError("Email", "Email này đã được đăng ký tài khoản.");
            }

            if (await _context.Landlords.AnyAsync(l => l.IdentityNumber == model.IdentityNumber.Trim()))
            {
                ModelState.AddModelError("IdentityNumber", "Số CCCD/CMND này đã được đăng ký trên hệ thống.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await _context.Provinces.OrderBy(p => p.Name).ToListAsync();
                return View(model);
            }

            // 1. Tạo hoặc cập nhật User
            if (user == null)
            {
                user = new tblUser
                {
                    Username = model.Username.Trim(),
                    Email = model.Email.Trim(),
                    FullName = model.FullName.Trim(),
                    Phone = model.Phone.Trim(),
                    PasswordHash = HashPassword(model.Password!),
                    Role = "Landlord",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                user.Role = "Landlord";
                if (!string.IsNullOrWhiteSpace(model.Phone)) user.Phone = model.Phone.Trim();
                if (!string.IsNullOrWhiteSpace(model.FullName)) user.FullName = model.FullName.Trim();
                await _context.SaveChangesAsync();
            }

            // 2. Tạo tblLandlord với Status = Pending
            var landlord = new tblLandlord
            {
                UserId = user.UserId,
                FullName = model.FullName.Trim(),
                Phone = model.Phone.Trim(),
                Email = model.Email.Trim(),
                IdentityNumber = model.IdentityNumber.Trim(),
                Address = model.Address?.Trim(),
                BankName = model.BankName?.Trim(),
                AccountNumber = model.AccountNumber?.Trim(),
                AccountName = model.AccountName?.Trim(),
                Description = model.Description?.Trim(),
                Status = QuanLyPhongTro.Models.LandlordStatus.Pending,
                CreatedAt = DateTime.Now
            };
            _context.Landlords.Add(landlord);
            await _context.SaveChangesAsync();

            // 3. Nếu chủ trọ nhập thông tin khu trọ ban đầu -> tạo luôn tblProperty
            if (!string.IsNullOrWhiteSpace(model.InitialPropertyName))
            {
                var property = new tblProperty
                {
                    LandlordId = landlord.LandlordId,
                    Name = model.InitialPropertyName.Trim(),
                    Address = model.InitialPropertyAddress?.Trim() ?? model.Address?.Trim() ?? "Đang cập nhật",
                    ProvinceId = model.ProvinceId,
                    DistrictId = model.DistrictId,
                    Description = model.Description?.Trim(),
                    Status = PropertyStatus.Active,
                    CreatedAt = DateTime.Now
                };
                _context.Properties.Add(property);
                await _context.SaveChangesAsync();
            }

            // 4. Lưu session đăng nhập cho chủ trọ để họ theo dõi trạng thái
            HttpContext.Session.SetString(SESSION_ADMIN, user.Username);
            HttpContext.Session.SetString("AdminRole", "Landlord");
            HttpContext.Session.SetString("LandlordId", landlord.LandlordId.ToString());
            HttpContext.Session.SetString("LandlordStatus", landlord.Status.ToString());

            TempData["Success"] = "Đăng ký chủ trọ thành công! Yêu cầu của bạn đã được chuyển tới Ban Quản Trị để duyệt hồ sơ.";
            return RedirectToAction("LandlordStatus");
        }

        // ── Trang Trạng thái Hồ sơ Chủ trọ (Pending / Approved / Rejected) ──
        [HttpGet]
        [Route("Account/LandlordStatus")]
        public async Task<IActionResult> LandlordStatus()
        {
            var landlord = await _currentLandlord.GetCurrentLandlordAsync();
            if (landlord == null)
            {
                int? uid = _currentLandlord.GetCurrentUserId();
                if (uid.HasValue)
                {
                    landlord = await _context.Landlords.FirstOrDefaultAsync(l => l.UserId == uid.Value);
                }
            }

            if (landlord == null)
            {
                return RedirectToAction(nameof(RegisterLandlord));
            }

            return View(landlord);
        }

        // GET: /Account/Logout

        public IActionResult Logout()

        {

            HttpContext.Session.Remove(SESSION_ADMIN);

            HttpContext.Session.Remove("AdminRole");

            HttpContext.Session.Remove("LandlordId");

            HttpContext.Session.Remove("LandlordStatus");

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



    public class RegisterLandlordViewModel
    {
        public bool IsExistingUser { get; set; } = false;

        public string Username { get; set; } = string.Empty;

        public string? Password { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email không được để trống")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Họ và tên không được để trống")]
        public string FullName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Số điện thoại không được để trống")]
        public string Phone { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Số CCCD/CMND không được để trống")]
        public string IdentityNumber { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? BankName { get; set; }

        public string? AccountNumber { get; set; }

        public string? AccountName { get; set; }

        public string? Description { get; set; }

        // Initial Property Info
        public string? InitialPropertyName { get; set; }

        public string? InitialPropertyAddress { get; set; }

        public int? ProvinceId { get; set; }

        public int? DistrictId { get; set; }

        public int? EstimatedRooms { get; set; }
    }

}

