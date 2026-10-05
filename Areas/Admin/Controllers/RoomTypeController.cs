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
    public class RoomTypeController : Controller
    {
        private readonly DataContext _context;

        public RoomTypeController(DataContext context)
        {
            _context = context;
        }

        // GET: /Admin/RoomType
        public async Task<IActionResult> Index()
        {
            var types = await _context.RoomTypes
                .Include(rt => rt.Rooms)
                .OrderBy(rt => rt.SortOrder)
                .ThenBy(rt => rt.RoomTypeName)
                .ToListAsync();
            return View(types);
        }

        // POST: /Admin/RoomType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(tblRoomType model)
        {
            if (string.IsNullOrWhiteSpace(model.RoomTypeName))
            {
                TempData["Error"] = "Tên loại phòng không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            model.RoomTypeName = model.RoomTypeName.Trim();
            if (await _context.RoomTypes.AnyAsync(rt => rt.RoomTypeName == model.RoomTypeName))
            {
                TempData["Error"] = $"Loại phòng \"{model.RoomTypeName}\" đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            model.CreatedAt = DateTime.Now;
            _context.RoomTypes.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo loại phòng \"{model.RoomTypeName}\".";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/RoomType/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(tblRoomType model)
        {
            var existing = await _context.RoomTypes.FindAsync(model.RoomTypeId);
            if (existing == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.RoomTypeName))
            {
                TempData["Error"] = "Tên loại phòng không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            model.RoomTypeName = model.RoomTypeName.Trim();
            if (await _context.RoomTypes.AnyAsync(rt => rt.RoomTypeName == model.RoomTypeName && rt.RoomTypeId != model.RoomTypeId))
            {
                TempData["Error"] = $"Loại phòng \"{model.RoomTypeName}\" đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            existing.RoomTypeName = model.RoomTypeName;
            existing.Description = model.Description?.Trim();
            existing.SortOrder = model.SortOrder;
            existing.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật loại phòng \"{existing.RoomTypeName}\".";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/RoomType/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var roomType = await _context.RoomTypes.FindAsync(id);
            if (roomType == null) return NotFound();

            roomType.IsActive = !roomType.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã {(roomType.IsActive ? "kích hoạt" : "vô hiệu hóa")} loại phòng \"{roomType.RoomTypeName}\".";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/RoomType/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var roomType = await _context.RoomTypes
                .Include(rt => rt.Rooms)
                .FirstOrDefaultAsync(rt => rt.RoomTypeId == id);
            if (roomType == null) return NotFound();

            if (roomType.Rooms.Any())
            {
                TempData["Error"] = $"Không thể xóa loại phòng \"{roomType.RoomTypeName}\" vì đang có {roomType.Rooms.Count} phòng thuộc loại này.";
                return RedirectToAction(nameof(Index));
            }

            _context.RoomTypes.Remove(roomType);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa loại phòng \"{roomType.RoomTypeName}\".";
            return RedirectToAction(nameof(Index));
        }
    }
}
