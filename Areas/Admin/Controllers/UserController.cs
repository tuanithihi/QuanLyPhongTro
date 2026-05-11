using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class UserController : Controller
    {
        private readonly DataContext _context;

        public UserController(DataContext context)
        {
            _context = context;
        }

        // ── INDEX ────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? search, string? isActive, int page = 1)
        {
            const int pageSize = 15;

            var query = _context.Users
                .Where(u => u.Role == "User")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(u =>
                    u.Username.Contains(search) ||
                    u.Email.Contains(search) ||
                    (u.FullName != null && u.FullName.Contains(search)) ||
                    (u.Phone != null && u.Phone.Contains(search)));
            }

            if (!string.IsNullOrEmpty(isActive) && bool.TryParse(isActive, out bool active))
                query = query.Where(u => u.IsActive == active);

            int totalItems = await query.CountAsync();
            var list = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page       = page;
            ViewBag.PageSize   = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Search     = search ?? "";
            ViewBag.IsActive   = isActive ?? "";

            return View(list);
        }

        // ── TOGGLE ACTIVE ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive  = !user.IsActive;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = user.IsActive
                ? $"Đã kích hoạt tài khoản {user.Username}."
                : $"Đã khóa tài khoản {user.Username}.";

            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ───────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Ngắt liên kết chat session (FK nullable) trước khi xóa
            await _context.ChatSessions
                .Where(s => s.UserId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.UserId, (int?)null));

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa tài khoản {user.Username}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
