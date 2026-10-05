using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class RoomController : Controller
    {
        private readonly DataContext _context;
        private readonly IRoomService _roomService;
        private readonly IPropertyService _propertyService;
        private readonly ICurrentLandlordService _currentLandlordService;
        private readonly ILogger<RoomController> _logger;

        public RoomController(
            DataContext context,
            IRoomService roomService,
            IPropertyService propertyService,
            ICurrentLandlordService currentLandlordService,
            ILogger<RoomController> logger)
        {
            _context = context;
            _roomService = roomService;
            _propertyService = propertyService;
            _currentLandlordService = currentLandlordService;
            _logger = logger;
        }

        // ── INDEX ────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(
            string? searchTerm, int? propertyId, int? status, int? approvalStatus)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            if (!_currentLandlordService.IsSuperAdmin() && !landlordId.HasValue)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var rooms = await _roomService.GetRoomsByLandlordAsync(
                landlordId,
                propertyId,
                searchTerm,
                status.HasValue ? (RoomStatus)status.Value : null,
                approvalStatus.HasValue ? (RoomApprovalStatus)approvalStatus.Value : null);

            var properties = await _propertyService.GetPropertiesByLandlordAsync(landlordId);
            ViewBag.PropertyList = new SelectList(properties, "PropertyId", "Name", propertyId);
            ViewBag.SearchTerm = searchTerm ?? string.Empty;
            ViewBag.PropertyId = propertyId;
            ViewBag.Status = status;
            ViewBag.ApprovalStatus = approvalStatus;

            return View(rooms);
        }

        // ── DETAIL (Chống IDOR) ──────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            var room = await _roomService.GetRoomByIdAsync(id, landlordId);

            if (room == null)
            {
                var any = await _roomService.GetRoomByIdAsync(id, null);
                if (any != null)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return NotFound();
            }

            return View("Detail", room);
        }

        [HttpGet]
        [ActionName("Detail")]
        public Task<IActionResult> DetailAlias(int id) => Details(id);

        // ── CREATE GET ───────────────────────────────────────────────────
        [RequireApprovedLandlord]
        public async Task<IActionResult> Create(int? propertyId)
        {
            int? landlordId = _currentLandlordService.GetCurrentLandlordId();
            if (!landlordId.HasValue && !_currentLandlordService.IsSuperAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var model = new RoomCreateEditViewModel
            {
                PropertyId = propertyId ?? 0,
                Area = 20,
                Floor = 1,
                MaxOccupants = 2,
                Capacity = 2,
                Status = RoomStatus.Available,
                IsPublished = true
            };

            await PopulateDropdowns(landlordId, model.PropertyId, model.RoomTypeId);
            return View(model);
        }

        // ── CREATE POST ──────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireApprovedLandlord]
        public async Task<IActionResult> Create(RoomCreateEditViewModel model)
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
                await PopulateDropdowns(landlordId, model.PropertyId, model.RoomTypeId);
                return View(model);
            }

            var (success, error, roomId) = await _roomService.CreateRoomAsync(model, landlordId.Value);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Không thể tạo phòng mới.");
                await PopulateDropdowns(landlordId, model.PropertyId, model.RoomTypeId);
                return View(model);
            }

            TempData["Success"] = $"Đã tạo phòng \"{model.RoomCode} - {model.RoomName}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT GET (Chống IDOR) ────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            var model = await _roomService.GetRoomForEditAsync(id, landlordId);

            if (model == null)
            {
                var any = await _roomService.GetRoomByIdAsync(id, null);
                if (any != null)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return NotFound();
            }

            await PopulateDropdowns(landlordId, model.PropertyId, model.RoomTypeId);
            return View(model);
        }

        // ── EDIT POST (Chống IDOR) ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoomCreateEditViewModel model)
        {
            if (id != model.RoomId) return BadRequest();

            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(landlordId, model.PropertyId, model.RoomTypeId);
                return View(model);
            }

            var (success, error) = await _roomService.UpdateRoomAsync(model, landlordId);
            if (!success)
            {
                var any = await _roomService.GetRoomByIdAsync(id, null);
                if (any != null && landlordId.HasValue && any.Property?.LandlordId != landlordId.Value)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }

                ModelState.AddModelError(string.Empty, error ?? "Không thể cập nhật thông tin phòng.");
                await PopulateDropdowns(landlordId, model.PropertyId, model.RoomTypeId);
                return View(model);
            }

            TempData["Success"] = $"Đã cập nhật phòng \"{model.RoomName}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE POST (Chống IDOR) ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            var (success, error) = await _roomService.DeleteRoomAsync(id, landlordId);

            if (!success)
            {
                var any = await _roomService.GetRoomByIdAsync(id, null);
                if (any != null && landlordId.HasValue && any.Property?.LandlordId != landlordId.Value)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }

                TempData["Error"] = error ?? "Không thể xóa phòng.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Đã xóa phòng và toàn bộ ảnh thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ── ACTIONS TRẠNG THÁI (Chống IDOR) ──────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            var (success, error) = await _roomService.PublishListingAsync(id, landlordId);
            if (!success)
            {
                var any = await _roomService.GetRoomByIdAsync(id, null);
                if (any != null && landlordId.HasValue && any.Property?.LandlordId != landlordId.Value)
                    return StatusCode(StatusCodes.Status403Forbidden);

                TempData["Error"] = error;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Đã bật hiển thị / gửi duyệt đăng tin phòng!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide(int id)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            var (success, error) = await _roomService.HideListingAsync(id, landlordId);
            if (!success)
            {
                var any = await _roomService.GetRoomByIdAsync(id, null);
                if (any != null && landlordId.HasValue && any.Property?.LandlordId != landlordId.Value)
                    return StatusCode(StatusCodes.Status403Forbidden);

                TempData["Error"] = error;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Đã ẩn tin đăng phòng khỏi website.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            if (!_currentLandlordService.IsSuperAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var (success, error) = await _roomService.ApproveListingAsync(id);
            if (!success)
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Đã phê duyệt tin đăng phòng thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            if (!_currentLandlordService.IsSuperAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var (success, error) = await _roomService.RejectListingAsync(id, reason);
            if (!success)
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = $"Đã từ chối tin đăng phòng. Lý do: {reason}";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unpublish(int id, string? reason)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            var (success, error) = await _roomService.UnpublishListingAsync(id, reason, landlordId);
            if (!success)
            {
                var any = await _roomService.GetRoomByIdAsync(id, null);
                if (any != null && landlordId.HasValue && any.Property?.LandlordId != landlordId.Value)
                    return StatusCode(StatusCodes.Status403Forbidden);

                TempData["Error"] = error;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Đã gỡ tin đăng phòng khỏi sàn.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkOccupied(int id)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            var (success, error) = await _roomService.MarkAsOccupiedAsync(id, landlordId);
            if (!success)
            {
                var any = await _roomService.GetRoomByIdAsync(id, null);
                if (any != null && landlordId.HasValue && any.Property?.LandlordId != landlordId.Value)
                    return StatusCode(StatusCodes.Status403Forbidden);

                TempData["Error"] = error;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Đã chuyển trạng thái sang ĐÃ CÓ NGƯỜI THUÊ.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAvailable(int id)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            var (success, error) = await _roomService.MarkAsAvailableAsync(id, landlordId);
            if (!success)
            {
                var any = await _roomService.GetRoomByIdAsync(id, null);
                if (any != null && landlordId.HasValue && any.Property?.LandlordId != landlordId.Value)
                    return StatusCode(StatusCodes.Status403Forbidden);

                TempData["Error"] = error;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Đã chuyển trạng thái sang PHÒNG TRỐNG.";
            return RedirectToAction(nameof(Index));
        }

        // ── AJAX QUẢN LÝ ẢNH PHÒNG (Chống IDOR) ──────────────────────────
        [HttpPost]
        public async Task<IActionResult> SetPrimaryImage(int roomId, int imageId)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            var (success, error) = await _roomService.SetPrimaryImageAsync(roomId, imageId, landlordId);
            if (!success)
            {
                var any = await _roomService.GetRoomByIdAsync(roomId, null);
                if (any != null && landlordId.HasValue && any.Property?.LandlordId != landlordId.Value)
                    return StatusCode(StatusCodes.Status403Forbidden);

                return Json(new { success = false, message = error });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int roomId, int imageId)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            var (success, error) = await _roomService.DeleteImageAsync(roomId, imageId, landlordId);
            if (!success)
            {
                var any = await _roomService.GetRoomByIdAsync(roomId, null);
                if (any != null && landlordId.HasValue && any.Property?.LandlordId != landlordId.Value)
                    return StatusCode(StatusCodes.Status403Forbidden);

                return Json(new { success = false, message = error });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateImageOrder(int roomId, [FromBody] List<int> imageIds)
        {
            int? landlordId = _currentLandlordService.IsSuperAdmin() ? null : _currentLandlordService.GetCurrentLandlordId();
            var (success, error) = await _roomService.UpdateImageSortOrderAsync(roomId, imageIds, landlordId);
            if (!success)
            {
                var any = await _roomService.GetRoomByIdAsync(roomId, null);
                if (any != null && landlordId.HasValue && any.Property?.LandlordId != landlordId.Value)
                    return StatusCode(StatusCodes.Status403Forbidden);

                return Json(new { success = false, message = error });
            }

            return Json(new { success = true });
        }

        // ── Helper ───────────────────────────────────────────────────────
        private async Task PopulateDropdowns(int? landlordId, int? selectedPropertyId, int? selectedRoomTypeId)
        {
            var properties = await _propertyService.GetPropertiesByLandlordAsync(landlordId);
            ViewBag.PropertyList = new SelectList(properties, "PropertyId", "Name", selectedPropertyId);

            var roomTypes = await _context.RoomTypes
                .Where(rt => rt.IsActive)
                .OrderBy(rt => rt.SortOrder)
                .ToListAsync();
            ViewBag.RoomTypeList = new SelectList(roomTypes, "RoomTypeId", "RoomTypeName", selectedRoomTypeId);

            var amenities = await _context.Amenities
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();
            ViewBag.Amenities = amenities;
        }
    }
}
