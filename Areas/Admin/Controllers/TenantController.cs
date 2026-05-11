using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
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
        private const string IMAGE_FOLDER = "images/tenants";

        public TenantController(DataContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env     = env;
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
            return View(tenant);
        }

        // ================================================================
        //  CREATE
        // ================================================================
        public async Task<IActionResult> Create()
        {
            await LoadUserList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(tblTenant model,
            IFormFile? avatarFile, IFormFile? frontFile, IFormFile? backFile,
            string? password, int? linkedUserId)
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
                await LoadUserList();
                return View(model);
            }

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

            // Avatar: nếu không upload mới nhưng có linked user → dùng avatar của user
            if (avatarFile != null)
                model.Avatar = await SaveImage(avatarFile);
            else if (linkedUserId.HasValue && string.IsNullOrEmpty(model.Avatar))
            {
                var linkedUser = await _context.Users.FindAsync(linkedUserId.Value);
                model.Avatar = linkedUser?.Avatar;
            }
            else
                model.Avatar = null;

            model.IdentityFrontImage = await SaveImage(frontFile);
            model.IdentityBackImage  = await SaveImage(backFile);
            model.CreatedAt          = DateTime.Now;

            _context.Tenants.Add(model);
            try
            {
                await _context.SaveChangesAsync();

                // Xóa tài khoản người dùng sau khi đã chuyển thành người thuê
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
                await LoadUserList();
                return View(model);
            }
        }

        // AJAX: lấy thông tin user để điền vào form
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

            if (avatarFile != null) { DeleteImage(existing.Avatar); existing.Avatar = await SaveImage(avatarFile); }
            if (frontFile  != null) { DeleteImage(existing.IdentityFrontImage); existing.IdentityFrontImage = await SaveImage(frontFile); }
            if (backFile   != null) { DeleteImage(existing.IdentityBackImage);  existing.IdentityBackImage  = await SaveImage(backFile); }

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
        private async Task LoadUserList()
        {
            // Chỉ lấy user thường, chưa có tenant trùng username
            var usedUsernames = await _context.Tenants
                .Where(t => t.Username != null)
                .Select(t => t.Username!)
                .ToListAsync();

            var users = await _context.Users
                .Where(u => u.Role == "User" && u.IsActive && !usedUsernames.Contains(u.Username))
                .OrderBy(u => u.FullName ?? u.Username)
                .Select(u => new { u.UserId, Display = $"{(u.FullName ?? u.Username)} ({u.Username})" })
                .ToListAsync();

            ViewBag.UserList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                users, "UserId", "Display");
        }

        private async Task<string?> SaveImage(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            var folder = Path.Combine(_env.WebRootPath, IMAGE_FOLDER);
            Directory.CreateDirectory(folder);
            var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var path     = Path.Combine(folder, fileName);
            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/{IMAGE_FOLDER}/{fileName}";
        }

        private void DeleteImage(string? imgPath)
        {
            if (string.IsNullOrEmpty(imgPath)) return;
            var full = Path.Combine(_env.WebRootPath, imgPath.TrimStart('/'));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }
    }
}
