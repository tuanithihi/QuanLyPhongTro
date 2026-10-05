using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class ProfileController : Controller
    {
        private readonly ILandlordService _landlordService;
        private readonly ICurrentLandlordService _currentLandlordService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            ILandlordService landlordService,
            ICurrentLandlordService currentLandlordService,
            ILogger<ProfileController> logger)
        {
            _landlordService = landlordService;
            _currentLandlordService = currentLandlordService;
            _logger = logger;
        }

        // GET: /Admin/Profile
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? landlordId = _currentLandlordService.GetCurrentLandlordId();
            if (!landlordId.HasValue && _currentLandlordService.IsSuperAdmin())
            {
                landlordId = 1; // Fallback chủ trọ mặc định cho SuperAdmin
            }

            if (!landlordId.HasValue)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ chủ trọ liên kết với tài khoản này.";
                return RedirectToAction("Index", "Home");
            }

            var profile = await _landlordService.GetProfileAsync(landlordId.Value);
            if (profile == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin hồ sơ chủ trọ.";
                return RedirectToAction("Index", "Home");
            }

            return View(profile);
        }

        // POST: /Admin/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LandlordProfileViewModel model, IFormFile? AvatarFile, IFormFile? avatarFile)
        {
            int? landlordId = _currentLandlordService.GetCurrentLandlordId();
            if (!landlordId.HasValue && _currentLandlordService.IsSuperAdmin())
            {
                landlordId = 1;
            }

            if (!landlordId.HasValue)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ chủ trọ liên kết.";
                return RedirectToAction("Index", "Home");
            }

            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                model.Phone = model.Phone.Replace(" ", "").Replace("-", "").Replace(".", "").Trim();
            }

            ModelState.Remove("AvatarFile");
            ModelState.Remove("avatarFile");
            ModelState.Remove("Status");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var fileToUpload = AvatarFile ?? avatarFile ?? model.AvatarFile;
            var (success, error) = await _landlordService.UpdateProfileAsync(landlordId.Value, model, fileToUpload);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Cập nhật hồ sơ thất bại.");
                return View(model);
            }

            TempData["Success"] = "Đã cập nhật hồ sơ chủ trọ và tài khoản ngân hàng nhận tiền thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
