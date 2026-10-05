using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class ServiceController : Controller
    {
        private readonly DataContext _context;
        private readonly ICurrentLandlordService _currentLandlordService;

        public ServiceController(DataContext context, ICurrentLandlordService currentLandlordService)
        {
            _context = context;
            _currentLandlordService = currentLandlordService;
        }

        // GET: /Admin/Service
        public async Task<IActionResult> Index()
        {
            var query = _context.Services.AsQueryable();

            if (!_currentLandlordService.IsSuperAdmin())
            {
                int currentLandlordId = _currentLandlordService.GetCurrentLandlordId() ?? 0;
                query = query.Where(s => s.LandlordId == currentLandlordId || s.LandlordId == null);
            }

            var services = await query
                .OrderBy(s => s.ServiceType)
                .ThenBy(s => s.ServiceName)
                .ToListAsync();

            return View(services);
        }

        // GET: /Admin/Service/Create
        public IActionResult Create() => View(new tblService());

        // POST: /Admin/Service/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(tblService model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.LandlordId = _currentLandlordService.GetCurrentLandlordId();
            model.CreatedAt = DateTime.Now;
            _context.Services.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm dịch vụ \"{model.ServiceName}\" thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Service/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            if (!_currentLandlordService.IsSuperAdmin() && service.LandlordId.HasValue && service.LandlordId != _currentLandlordService.GetCurrentLandlordId())
                return StatusCode(StatusCodes.Status403Forbidden);

            return View(service);
        }

        // POST: /Admin/Service/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, tblService model)
        {
            if (id != model.ServiceId) return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            if (!_currentLandlordService.IsSuperAdmin() && service.LandlordId.HasValue && service.LandlordId != _currentLandlordService.GetCurrentLandlordId())
                return StatusCode(StatusCodes.Status403Forbidden);

            service.ServiceName   = model.ServiceName;
            service.ServiceType   = model.ServiceType;
            service.PricingMethod = model.PricingMethod;
            service.UnitPrice     = model.UnitPrice;
            service.Unit          = model.Unit;
            service.Description   = model.Description;
            service.IsActive      = model.IsActive;
            service.UpdatedAt     = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật dịch vụ \"{service.ServiceName}\".";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Service/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            if (!_currentLandlordService.IsSuperAdmin() && service.LandlordId.HasValue && service.LandlordId != _currentLandlordService.GetCurrentLandlordId())
                return StatusCode(StatusCodes.Status403Forbidden);

            service.IsActive  = !service.IsActive;
            service.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = service.IsActive
                ? $"Đã kích hoạt dịch vụ \"{service.ServiceName}\"."
                : $"Đã tắt dịch vụ \"{service.ServiceName}\".";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Service/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _context.Services
                .Include(s => s.InvoiceDetails)
                .FirstOrDefaultAsync(s => s.ServiceId == id);

            if (service == null) return NotFound();

            if (!_currentLandlordService.IsSuperAdmin() && service.LandlordId.HasValue && service.LandlordId != _currentLandlordService.GetCurrentLandlordId())
                return StatusCode(StatusCodes.Status403Forbidden);

            if (service.InvoiceDetails.Any())
            {
                TempData["Error"] = $"Không thể xóa dịch vụ \"{service.ServiceName}\" vì đã được dùng trong {service.InvoiceDetails.Count} hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa dịch vụ \"{service.ServiceName}\".";
            return RedirectToAction(nameof(Index));
        }
    }
}
