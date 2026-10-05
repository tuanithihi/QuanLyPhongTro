using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class PropertyController : Controller
    {
        private readonly IPropertyService _propertyService;
        private readonly ICurrentLandlordService _currentLandlordService;
        private readonly ILogger<PropertyController> _logger;

        public PropertyController(
            IPropertyService propertyService,
            ICurrentLandlordService currentLandlordService,
            ILogger<PropertyController> logger)
        {
            _propertyService = propertyService;
            _currentLandlordService = currentLandlordService;
            _logger = logger;
        }

        // ── INDEX ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            if (!_currentLandlordService.IsSuperAdmin() && !landlordId.HasValue)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var list = await _propertyService.GetPropertiesByLandlordAsync(landlordId, search);
            ViewBag.Search = search ?? string.Empty;
            return View(list);
        }

        // ── DETAILS (Chống IDOR) ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            var detail = await _propertyService.GetPropertyDetailAsync(id, landlordId);

            if (detail == null)
            {
                // Nếu tồn tại nhưng không thuộc về chủ trọ này -> 403 Forbidden
                var anyProperty = await _propertyService.GetPropertyDetailAsync(id, null);
                if (anyProperty != null)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return NotFound();
            }

            return View(detail);
        }

        // ── CREATE GET ───────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            int? landlordId = _currentLandlordService.GetCurrentLandlordId();
            if (!landlordId.HasValue && !_currentLandlordService.IsSuperAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            await PopulateProvincesDropdown();
            var model = new PropertyCreateEditViewModel
            {
                LandlordId = landlordId ?? 1,
                Latitude = 21.0285,  // Tọa độ mặc định Hà Nội / trung tâm
                Longitude = 105.8542
            };

            return View(model);
        }

        // ── CREATE POST ──────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PropertyCreateEditViewModel model)
        {
            int? landlordId = _currentLandlordService.GetCurrentLandlordId();
            if (!landlordId.HasValue && _currentLandlordService.IsSuperAdmin())
            {
                landlordId = 1;
            }

            if (!landlordId.HasValue)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (!ModelState.IsValid)
            {
                await PopulateProvincesDropdown(model.ProvinceId, model.DistrictId, model.WardId);
                return View(model);
            }

            var (success, error, propertyId) = await _propertyService.CreatePropertyAsync(model, landlordId.Value);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Không thể lưu khu trọ.");
                await PopulateProvincesDropdown(model.ProvinceId, model.DistrictId, model.WardId);
                return View(model);
            }

            TempData["Success"] = $"Đã tạo khu trọ \"{model.Name}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT GET (Chống IDOR) ────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            var model = await _propertyService.GetPropertyForEditAsync(id, landlordId);

            if (model == null)
            {
                var exists = await _propertyService.GetPropertyForEditAsync(id, null);
                if (exists != null)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return NotFound();
            }

            await PopulateProvincesDropdown(model.ProvinceId, model.DistrictId, model.WardId);
            return View(model);
        }

        // ── EDIT POST (Chống IDOR) ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PropertyCreateEditViewModel model)
        {
            if (id != model.PropertyId) return NotFound();

            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();

            if (!ModelState.IsValid)
            {
                await PopulateProvincesDropdown(model.ProvinceId, model.DistrictId, model.WardId);
                return View(model);
            }

            var (success, error) = await _propertyService.UpdatePropertyAsync(model, landlordId);
            if (!success)
            {
                // Kiểm tra nếu là lỗi do không có quyền
                var exists = await _propertyService.GetPropertyForEditAsync(id, null);
                if (exists != null && landlordId.HasValue && exists.LandlordId != landlordId.Value)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }

                ModelState.AddModelError(string.Empty, error ?? "Không thể cập nhật khu trọ.");
                await PopulateProvincesDropdown(model.ProvinceId, model.DistrictId, model.WardId);
                return View(model);
            }

            TempData["Success"] = $"Đã cập nhật khu trọ \"{model.Name}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE POST (Chống IDOR) ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            var (success, error) = await _propertyService.DeletePropertyAsync(id, landlordId);

            if (!success)
            {
                var exists = await _propertyService.GetPropertyForEditAsync(id, null);
                if (exists != null && landlordId.HasValue && exists.LandlordId != landlordId.Value)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }

                TempData["Error"] = error ?? "Không thể xóa khu trọ.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Đã xóa khu trọ thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ── AJAX CASCADING DROPDOWNS ────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            var districts = await _propertyService.GetDistrictsAsync(provinceId);
            return Json(districts.Select(d => new { id = d.DistrictId, name = d.Name }));
        }

        [HttpGet]
        public async Task<IActionResult> GetWards(int districtId)
        {
            var wards = await _propertyService.GetWardsAsync(districtId);
            return Json(wards.Select(w => new { id = w.WardId, name = w.Name }));
        }

        // ── Helper ───────────────────────────────────────────────────────
        private async Task PopulateProvincesDropdown(int? selectedProvinceId = null, int? selectedDistrictId = null, int? selectedWardId = null)
        {
            var provinces = await _propertyService.GetProvincesAsync();
            ViewBag.Provinces = new SelectList(provinces, "ProvinceId", "Name", selectedProvinceId);

            if (selectedProvinceId.HasValue)
            {
                var districts = await _propertyService.GetDistrictsAsync(selectedProvinceId.Value);
                ViewBag.Districts = new SelectList(districts, "DistrictId", "Name", selectedDistrictId);
            }
            else
            {
                ViewBag.Districts = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            if (selectedDistrictId.HasValue)
            {
                var wards = await _propertyService.GetWardsAsync(selectedDistrictId.Value);
                ViewBag.Wards = new SelectList(wards, "WardId", "Name", selectedWardId);
            }
            else
            {
                ViewBag.Wards = new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }
    }
}
