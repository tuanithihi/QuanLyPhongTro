using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class ServiceController : Controller
    {
        private readonly DataContext _context;

        public ServiceController(DataContext context)
        {
            _context = context;
        }

        // GET: /Admin/Service
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services
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
