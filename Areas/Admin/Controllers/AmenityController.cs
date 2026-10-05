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
    public class AmenityController : Controller
    {
        private readonly DataContext _context;

        public AmenityController(DataContext context)
        {
            _context = context;
        }

        // GET: /Admin/Amenity
        public async Task<IActionResult> Index()
        {
            var amenities = await _context.Amenities
                .Include(a => a.RoomAmenities)
                .OrderBy(a => a.Name)
                .ToListAsync();
            return View(amenities);
        }

        // POST: /Admin/Amenity/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(tblAmenity model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Tên tiện ích không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            model.Name = model.Name.Trim();
            if (await _context.Amenities.AnyAsync(a => a.Name == model.Name))
            {
                TempData["Error"] = $"Tiện ích \"{model.Name}\" đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            _context.Amenities.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm tiện ích \"{model.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Amenity/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(tblAmenity model)
        {
            var existing = await _context.Amenities.FindAsync(model.AmenityId);
            if (existing == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Tên tiện ích không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            model.Name = model.Name.Trim();
            if (await _context.Amenities.AnyAsync(a => a.Name == model.Name && a.AmenityId != model.AmenityId))
            {
                TempData["Error"] = $"Tiện ích \"{model.Name}\" đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            existing.Name = model.Name;
            existing.Icon = model.Icon?.Trim();
            existing.Description = model.Description?.Trim();
            existing.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật tiện ích \"{existing.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Amenity/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var amenity = await _context.Amenities.FindAsync(id);
            if (amenity == null) return NotFound();

            amenity.IsActive = !amenity.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã {(amenity.IsActive ? "kích hoạt" : "vô hiệu hóa")} tiện ích \"{amenity.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Amenity/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var amenity = await _context.Amenities
                .Include(a => a.RoomAmenities)
                .FirstOrDefaultAsync(a => a.AmenityId == id);
            if (amenity == null) return NotFound();

            if (amenity.RoomAmenities.Any())
            {
                TempData["Error"] = $"Không thể xóa tiện ích \"{amenity.Name}\" vì đang được gán cho {amenity.RoomAmenities.Count} phòng. Bạn có thể vô hiệu hóa tiện ích này.";
                return RedirectToAction(nameof(Index));
            }

            _context.Amenities.Remove(amenity);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa tiện ích \"{amenity.Name}\".";
            return RedirectToAction(nameof(Index));
        }
    }
}
