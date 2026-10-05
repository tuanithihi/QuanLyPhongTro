using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    [SuperAdminOnly]
    public class LandlordController : Controller
    {
        private readonly DataContext _context;

        public LandlordController(DataContext context)
        {
            _context = context;
        }

        // GET: /Admin/Landlord
        public async Task<IActionResult> Index(LandlordStatus? status, string? search, int page = 1)
        {
            const int pageSize = 15;
            var query = _context.Landlords
                .Include(l => l.User)
                .Include(l => l.Properties)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(l => l.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(l => l.FullName.Contains(s)
                                      || (l.Phone != null && l.Phone.Contains(s))
                                      || (l.Email != null && l.Email.Contains(s))
                                      || l.IdentityNumber.Contains(s));
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Status = status;
            ViewBag.Search = search ?? "";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = total;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.PendingCount = await _context.Landlords.CountAsync(l => l.Status == LandlordStatus.Pending);

            return View(items);
        }

        // GET: /Admin/Landlord/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var landlord = await _context.Landlords
                .Include(l => l.User)
                .Include(l => l.Properties)
                    .ThenInclude(p => p.Rooms)
                .FirstOrDefaultAsync(l => l.LandlordId == id);

            if (landlord == null) return NotFound();
            return View(landlord);
        }

        // POST: /Admin/Landlord/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var landlord = await _context.Landlords.FindAsync(id);
            if (landlord == null) return NotFound();

            landlord.Status = LandlordStatus.Approved;
            landlord.RejectReason = null;
            landlord.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã phê duyệt chủ trọ \"{landlord.FullName}\". Chủ trọ đã có quyền đăng tin cho thuê.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Landlord/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            var landlord = await _context.Landlords.FindAsync(id);
            if (landlord == null) return NotFound();

            landlord.Status = LandlordStatus.Rejected;
            landlord.RejectReason = string.IsNullOrWhiteSpace(reason) ? "Không đạt yêu cầu xác minh hồ sơ." : reason.Trim();
            landlord.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã từ chối chủ trọ \"{landlord.FullName}\". Lý do: {landlord.RejectReason}";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Landlord/Suspend/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(int id, string? reason)
        {
            var landlord = await _context.Landlords.FindAsync(id);
            if (landlord == null) return NotFound();

            landlord.Status = LandlordStatus.Suspended;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                landlord.RejectReason = reason.Trim();
            }
            landlord.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tạm khóa / đình chỉ chủ trọ \"{landlord.FullName}\".";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Landlord/Reactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var landlord = await _context.Landlords.FindAsync(id);
            if (landlord == null) return NotFound();

            landlord.Status = LandlordStatus.Approved;
            landlord.RejectReason = null;
            landlord.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã kích hoạt lại chủ trọ \"{landlord.FullName}\".";
            return RedirectToAction(nameof(Index));
        }
    }
}
