using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Services;
using System.Security.Cryptography;
using System.Text;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class TenantController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ICurrentLandlordService _currentLandlordService;
        private readonly IFileUploadService _fileUploadService;
        private const string IMAGE_FOLDER = "images/tenants";

        public TenantController(
            DataContext context, 
            IWebHostEnvironment env,
            ICurrentLandlordService currentLandlordService,
            IFileUploadService fileUploadService)
        {
            _context = context;
            _env     = env;
            _currentLandlordService = currentLandlordService;
            _fileUploadService = fileUploadService;
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password + "PhongTro@2026#Salt");
            return Convert.ToHexString(sha256.ComputeHash(bytes)).ToLower();
        }

        // ================================================================
        //  INDEX
        // ================================================================
        public async Task<IActionResult> Index(string? search, string? isActive, int page = 1)
        {
            const int pageSize = 15;
            var query = _context.Tenants.Include(t => t.Contracts).AsQueryable();

            if (!_currentLandlordService.IsSuperAdmin())
            {
                int currentLandlordId = _currentLandlordService.GetCurrentLandlordId() ?? 0;
                query = query.Where(t => t.LandlordId == currentLandlordId);
            }

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.FullName.Contains(search)
                                      || (t.Phone != null && t.Phone.Contains(search))
                                      || t.IdentityNumber.Contains(search)
                                      || (t.Email != null && t.Email.Contains(search)));

            if (isActive == "1")      query = query.Where(t => t.IsActive);
            else if (isActive == "0") query = query.Where(t => !t.IsActive);

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search    = search ?? "";
            ViewBag.IsActive  = isActive ?? "";
            ViewBag.Page      = page;
            ViewBag.PageSize  = pageSize;
            ViewBag.TotalItems= total;
            ViewBag.TotalPages= (int)Math.Ceiling(total / (double)pageSize);
            return View(items);
        }

        // ================================================================
        //  DETAILS
        // ================================================================
        public async Task<IActionResult> Details(int id)
        {
            var tenant = await _context.Tenants
                .Include(t => t.Contracts).ThenInclude(c => c.Room)
                .FirstOrDefaultAsync(t => t.TenantId == id);
            if (tenant == null) return NotFound();

            if (!_currentLandlordService.IsSuperAdmin() && tenant.LandlordId != _currentLandlordService.GetCurrentLandlordId())
                return StatusCode(StatusCodes.Status403Forbidden);

            return View(tenant);
        }

        // ================================================================
        //  CREATE
        // ================================================================
        //  CREATE
        // ================================================================
        public async Task<IActionResult> Create(int? bookingId = null)
        {
            await LoadBookingList(bookingId);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(tblTenant model,
            IFormFile? avatarFile, IFormFile? frontFile, IFormFile? backFile,
            string? password, int? linkedUserId, int? bookingId)
        {
            // Validate username unique
            if (!string.IsNullOrWhiteSpace(model.Username))
            {
                bool taken = await _context.Tenants.AnyAsync(t => t.Username == model.Username.Trim());
                if (taken) ModelState.AddModelError("Username", "Tên đăng nhập đã được sử dụng.");
            }

            ModelState.Remove("PasswordHash");
            if (!ModelState.IsValid)
            {
                await LoadBookingList(bookingId);
                return View(model);
            }

            model.LandlordId = _currentLandlordService.GetCurrentLandlordId();
            model.Username = string.IsNullOrWhiteSpace(model.Username) ? null : model.Username.Trim();

            // Nếu liên kết từ tài khoản website → dùng PasswordHash của user đó
            if (linkedUserId.HasValue && model.Username != null)
            {
                var linkedUser = await _context.Users.FindAsync(linkedUserId.Value);
                if (linkedUser != null)
                    model.PasswordHash = linkedUser.PasswordHash;
            }
            else
            {
                model.PasswordHash = (model.Username != null && !string.IsNullOrWhiteSpace(password))
                                     ? HashPassword(password) : null;
            }

            // Upload ảnh an toàn
            if (avatarFile != null)
            {
                var up = await _fileUploadService.UploadImageAsync(avatarFile, "tenants");
                if (up.IsValid) model.Avatar = up.RelativePath;
                else ModelState.AddModelError("", up.ErrorMessage ?? "Lỗi upload ảnh đại diện.");
            }
            else if (linkedUserId.HasValue && string.IsNullOrEmpty(model.Avatar))
            {
                var linkedUser = await _context.Users.FindAsync(linkedUserId.Value);
                model.Avatar = linkedUser?.Avatar;
            }
            else
                model.Avatar = null;

            if (frontFile != null)
            {
                var up = await _fileUploadService.UploadImageAsync(frontFile, "tenants");
                if (up.IsValid) model.IdentityFrontImage = up.RelativePath;
            }

            if (backFile != null)
            {
                var up = await _fileUploadService.UploadImageAsync(backFile, "tenants");
                if (up.IsValid) model.IdentityBackImage = up.RelativePath;
            }

            model.CreatedAt = DateTime.Now;

            _context.Tenants.Add(model);
            try
            {
                await _context.SaveChangesAsync();

                // Nếu tạo từ lịch hẹn xem phòng -> cập nhật trạng thái lịch hẹn thành Accepted (nếu đang Pending)
                if (bookingId.HasValue)
                {
                    var booking = await _context.BookingRequests.FindAsync(bookingId.Value);
                    if (booking != null)
                    {
                        if (booking.Status == BookingRequestStatus.Pending)
                        {
                            booking.Status = BookingRequestStatus.Accepted;
                        }
                        string noteAdd = "Đã tạo hồ sơ người thuê";
                        if (string.IsNullOrEmpty(booking.AdminNote))
                            booking.AdminNote = noteAdd;
                        else if (!booking.AdminNote.Contains(noteAdd))
                        {
                            var combined = booking.AdminNote + " | " + noteAdd;
                            booking.AdminNote = combined.Length > 500 ? combined[..500] : combined;
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                // Xóa tài khoản người dùng sau khi đã chuyển thành người thuê (nếu có linkedUserId)
                if (linkedUserId.HasValue)
                {
                    var linkedUser = await _context.Users.FindAsync(linkedUserId.Value);
                    if (linkedUser != null)
                    {
                        await _context.ChatSessions
                            .Where(s => s.UserId == linkedUserId.Value)
                            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UserId, (int?)null));
                        _context.Users.Remove(linkedUser);
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["Success"] = "Đã thêm người thuê thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
            {
                ModelState.AddModelError("IdentityNumber", "Số CCCD/CMND đã tồn tại trong hệ thống.");
                await LoadBookingList(bookingId);
                return View(model);
            }
        }

        // AJAX: lấy thông tin lịch hẹn xem phòng để điền vào form người thuê
        [HttpGet]
        public async Task<IActionResult> GetBookingInfo(int bookingId)
        {
            var booking = await _context.BookingRequests
                .Include(b => b.Room)
                    .ThenInclude(r => r.Property)
                .FirstOrDefaultAsync(b => b.RequestId == bookingId);

            if (booking == null) return NotFound();

            if (!_currentLandlordService.IsSuperAdmin())
            {
                int currentLandlordId = _currentLandlordService.GetCurrentLandlordId() ?? 0;
                bool isOwner = booking.LandlordId == currentLandlordId ||
                               (booking.Room != null && booking.Room.Property != null && booking.Room.Property.LandlordId == currentLandlordId);
                if (!isOwner) return StatusCode(StatusCodes.Status403Forbidden);
            }

            tblUser? matchedUser = null;
            string cleanPhone = booking.Phone?.Trim() ?? "";
            string cleanEmail = booking.Email?.Trim() ?? "";

            if (!string.IsNullOrEmpty(cleanPhone))
            {
                matchedUser = await _context.Users.FirstOrDefaultAsync(u => u.Phone == cleanPhone && u.Role == "User");
            }
            if (matchedUser == null && !string.IsNullOrEmpty(cleanEmail))
            {
                matchedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == cleanEmail && u.Role == "User");
            }

            string suggestedUsername = matchedUser?.Username ?? (!string.IsNullOrEmpty(cleanPhone) ? cleanPhone : "");
            bool usernameTaken = false;
            if (!string.IsNullOrEmpty(suggestedUsername))
            {
                usernameTaken = await _context.Tenants.AnyAsync(t => t.Username == suggestedUsername);
            }

            return Json(new
            {
                requestId = booking.RequestId,
                fullName = booking.FullName ?? "",
                phone = booking.Phone ?? "",
                email = booking.Email ?? (matchedUser?.Email ?? ""),
                preferredDate = booking.PreferredDate ?? (booking.ViewingTime.HasValue ? booking.ViewingTime.Value.ToString("dd/MM/yyyy HH:mm") : ""),
                message = booking.Message ?? "",
                roomId = booking.RoomId,
                roomCode = booking.Room?.RoomCode ?? "",
                roomName = booking.Room?.RoomName ?? "",
                propertyName = booking.Room?.Property?.Name ?? "",
                hasUserAccount = matchedUser != null,
                linkedUserId = matchedUser?.UserId,
                username = usernameTaken ? "" : suggestedUsername,
                avatar = matchedUser?.Avatar ?? ""
            });
        }

        // AJAX: lấy thông tin user để điền vào form (giữ lại hỗ trợ cũ nếu có)
        [HttpGet]
        public async Task<IActionResult> GetUserInfo(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();
            return Json(new
            {
                fullName = user.FullName ?? "",
                phone    = user.Phone    ?? "",
                email    = user.Email,
                username = user.Username,
                avatar   = user.Avatar   ?? ""
            });
        }

        // ================================================================
        //  EDIT
        // ================================================================
        public async Task<IActionResult> Edit(int id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null) return NotFound();

            if (!_currentLandlordService.IsSuperAdmin() && tenant.LandlordId != _currentLandlordService.GetCurrentLandlordId())
                return StatusCode(StatusCodes.Status403Forbidden);

            return View(tenant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, tblTenant model,
            IFormFile? avatarFile, IFormFile? frontFile, IFormFile? backFile,
            string? password)
        {
            if (id != model.TenantId) return BadRequest();

            var existing = await _context.Tenants.FindAsync(id);
            if (existing == null) return NotFound();

            if (!_currentLandlordService.IsSuperAdmin() && existing.LandlordId != _currentLandlordService.GetCurrentLandlordId())
                return StatusCode(StatusCodes.Status403Forbidden);

            if (!string.IsNullOrWhiteSpace(model.Username))
            {
                bool taken = await _context.Tenants.AnyAsync(t => t.Username == model.Username.Trim() && t.TenantId != id);
                if (taken) ModelState.AddModelError("Username", "Tên đăng nhập đã được sử dụng.");
            }

            ModelState.Remove("PasswordHash");
            if (!ModelState.IsValid) return View(model);

            existing.FullName          = model.FullName;
            existing.IdentityNumber    = model.IdentityNumber;
            existing.Phone             = model.Phone;
            existing.Email             = model.Email;
            existing.DateOfBirth       = model.DateOfBirth;
            existing.Gender            = model.Gender;
            existing.PermanentAddress  = model.PermanentAddress;
            existing.IsActive          = model.IsActive;
            existing.Username          = string.IsNullOrWhiteSpace(model.Username) ? null : model.Username.Trim();

            if (!string.IsNullOrWhiteSpace(password))
                existing.PasswordHash = HashPassword(password);
            else if (string.IsNullOrEmpty(existing.Username))
                existing.PasswordHash = null;

            if (avatarFile != null)
            {
                DeleteImage(existing.Avatar);
                var up = await _fileUploadService.UploadImageAsync(avatarFile, "tenants");
                if (up.IsValid) existing.Avatar = up.RelativePath;
            }
            if (frontFile != null)
            {
                DeleteImage(existing.IdentityFrontImage);
                var up = await _fileUploadService.UploadImageAsync(frontFile, "tenants");
                if (up.IsValid) existing.IdentityFrontImage = up.RelativePath;
            }
            if (backFile != null)
            {
                DeleteImage(existing.IdentityBackImage);
                var up = await _fileUploadService.UploadImageAsync(backFile, "tenants");
                if (up.IsValid) existing.IdentityBackImage = up.RelativePath;
            }

            existing.UpdatedAt = DateTime.Now;
            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật thông tin người thuê.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
            {
                ModelState.AddModelError("IdentityNumber", "Số CCCD/CMND đã tồn tại trong hệ thống.");
                return View(model);
            }
        }

        // ================================================================
        //  DELETE
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tenant = await _context.Tenants
                .Include(t => t.Contracts)
                .FirstOrDefaultAsync(t => t.TenantId == id);
            if (tenant == null) return NotFound();

            if (!_currentLandlordService.IsSuperAdmin() && tenant.LandlordId != _currentLandlordService.GetCurrentLandlordId())
                return StatusCode(StatusCodes.Status403Forbidden);

            if (tenant.Contracts.Any())
            {
                TempData["Error"] = "Không thể xóa người thuê đã có lịch sử hợp đồng. Dữ liệu được giữ lại để lưu trữ.";
                return RedirectToAction(nameof(Index));
            }

            DeleteImage(tenant.Avatar);
            DeleteImage(tenant.IdentityFrontImage);
            DeleteImage(tenant.IdentityBackImage);

            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa người thuê.";
            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ──────────────────────────────────────────────────────
        private async Task LoadBookingList(int? selectedBookingId = null)
        {
            var query = _context.BookingRequests
                .Include(b => b.Room)
                    .ThenInclude(r => r.Property)
                .Where(b => b.Status != BookingRequestStatus.Rejected &&
                            (b.RequestType == BookingRequestType.ViewingRequest || b.RequestType == BookingRequestType.Viewing || b.RequestType == BookingRequestType.Deposit))
                .AsQueryable();

            if (!_currentLandlordService.IsSuperAdmin())
            {
                int currentLandlordId = _currentLandlordService.GetCurrentLandlordId() ?? 0;
                query = query.Where(b => b.LandlordId == currentLandlordId ||
                                         (b.Room != null && b.Room.Property != null && b.Room.Property.LandlordId == currentLandlordId));
            }

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            // Lọc loại trừ những khách đã là người thuê của chủ trọ này (trừ khi chính là booking được chọn)
            var existingPhonesQuery = _context.Tenants.AsQueryable();
            if (!_currentLandlordService.IsSuperAdmin())
            {
                int currentLandlordId = _currentLandlordService.GetCurrentLandlordId() ?? 0;
                existingPhonesQuery = existingPhonesQuery.Where(t => t.LandlordId == currentLandlordId);
            }
            var existingPhones = await existingPhonesQuery
                .Where(t => t.Phone != null)
                .Select(t => t.Phone!.Trim())
                .ToListAsync();

            var filteredBookings = bookings
                .Where(b => b.RequestId == selectedBookingId || string.IsNullOrWhiteSpace(b.Phone) || !existingPhones.Contains(b.Phone.Trim()))
                .Select(b => new
                {
                    b.RequestId,
                    Display = $"{b.FullName} - {b.Phone} ({(b.Room != null ? b.Room.RoomName : "Phòng")}{(string.IsNullOrEmpty(b.PreferredDate) ? "" : " - Ngày hẹn: " + b.PreferredDate)})"
                })
                .ToList();

            ViewBag.BookingList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                filteredBookings, "RequestId", "Display", selectedBookingId);
            ViewBag.SelectedBookingId = selectedBookingId;
            ViewBag.UserList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                Enumerable.Empty<object>(), "UserId", "Display");
        }

        private void DeleteImage(string? imgPath)
        {
            if (string.IsNullOrEmpty(imgPath)) return;
            var full = Path.Combine(_env.WebRootPath, imgPath.TrimStart('/'));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }
    }
}
