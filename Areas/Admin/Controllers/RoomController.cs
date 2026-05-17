using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Areas.Admin.Models;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    /// <summary>
    /// Quan ly phong tro: CRUD + tim kiem + loc + phan trang.
    /// Tat ca action yeu cau dang nhap Admin ([AdminOnly]).
    /// </summary>
    [Area("Admin")]
    [AdminOnly]
    public class RoomController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<RoomController> _logger;
        private const string IMAGE_FOLDER = "images/rooms";

        public RoomController(DataContext context, IWebHostEnvironment env, ILogger<RoomController> logger)
        {
            _context = context;
            _env     = env;
            _logger  = logger;
        }

        // ================================================================
        //  INDEX  -  Danh sach phong (tim kiem + loc + phan trang)
        // ================================================================

        // GET: /Admin/Room
        public async Task<IActionResult> Index(
            string? searchTerm, int? roomTypeId, int? status, int page = 1, int pageSize = 10)
        {
            var query = _context.Rooms.Include(r => r.RoomType).AsQueryable();

            // Loc theo tu khoa (ma phong hoac ten phong)
            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(r => r.RoomCode.Contains(searchTerm) || r.RoomName.Contains(searchTerm));

            // Loc theo loai phong
            if (roomTypeId.HasValue)
                query = query.Where(r => r.RoomTypeId == roomTypeId.Value);

            // Loc theo trang thai
            if (status.HasValue)
                query = query.Where(r => (int)r.Status == status.Value);

            // Phan trang
            int totalItems = await query.CountAsync();
            var rooms = await query
                .OrderBy(r => r.Floor).ThenBy(r => r.RoomCode)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            ViewBag.SearchTerm   = searchTerm;
            ViewBag.RoomTypeId   = roomTypeId;
            ViewBag.Status       = status;
            ViewBag.Page         = page;
            ViewBag.PageSize     = pageSize;
            ViewBag.TotalItems   = totalItems;
            ViewBag.TotalPages   = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.RoomTypeList = new SelectList(
                await _context.RoomTypes.Where(rt => rt.IsActive).ToListAsync(),
                "RoomTypeId", "RoomTypeName", roomTypeId);

            return View(rooms);
        }

        // ================================================================
        //  DETAIL  -  Xem chi tiet phong + lich su hop dong
        // ================================================================

        // GET: /Admin/Room/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.Contracts).ThenInclude(c => c.Tenant)
                .Include(r => r.Invoices)
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null) return NotFound();
            return View(room);
        }

        // ================================================================
        //  CREATE
        // ================================================================

        // GET: /Admin/Room/Create
        public async Task<IActionResult> Create()
        {
            var vm = new RoomCreateViewModel();
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        // POST: /Admin/Room/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomCreateViewModel vm)
        {
            // Guard: kiem tra ma phong trung
            if (await _context.Rooms.AnyAsync(r => r.RoomCode == vm.RoomCode))
                ModelState.AddModelError(nameof(vm.RoomCode), "Ma phong da ton tai.");

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(vm);
                return View(vm);
            }

            // Xu ly upload anh
            string? thumbnail = null;
            if (vm.ThumbnailFile != null && vm.ThumbnailFile.Length > 0)
                thumbnail = await SaveImageAsync(vm.ThumbnailFile);

            // Map ViewModel -> Entity
            var room = new tblRoom
            {
                RoomCode       = vm.RoomCode.Trim(),
                RoomName       = vm.RoomName.Trim(),
                RoomTypeId     = vm.RoomTypeId,
                RoomPrice      = vm.RoomPrice,
                DefaultDeposit = vm.DefaultDeposit,
                Area           = vm.Area,
                Floor          = vm.Floor,
                MaxOccupants   = vm.MaxOccupants,
                Description    = vm.Description,
                ThumbnailImage = thumbnail,
                Address        = vm.Address?.Trim(),
                Latitude       = vm.Latitude,
                Longitude      = vm.Longitude,
                Status         = vm.Status,
                IsPublished    = vm.IsPublished,
                CreatedAt      = DateTime.Now
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            TempData["Success"] = string.Concat("Them phong \"", room.RoomName, "\" thanh cong.");
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        //  EDIT
        // ================================================================

        // GET: /Admin/Room/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            // Map Entity -> ViewModel
            var vm = new RoomCreateViewModel
            {
                RoomId           = room.RoomId,
                RoomCode         = room.RoomCode,
                RoomName         = room.RoomName,
                RoomTypeId       = room.RoomTypeId,
                RoomPrice        = room.RoomPrice,
                DefaultDeposit   = room.DefaultDeposit,
                Area             = room.Area,
                Floor            = room.Floor,
                MaxOccupants     = room.MaxOccupants,
                Description      = room.Description,
                Address          = room.Address,
                Latitude         = room.Latitude,
                Longitude        = room.Longitude,
                Status           = room.Status,
                IsPublished      = room.IsPublished,
                CurrentThumbnail = room.ThumbnailImage
            };

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        // POST: /Admin/Room/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoomCreateViewModel vm)
        {
            if (id != vm.RoomId) return BadRequest();

            // Guard: kiem tra ma phong trung (tru ban than)
            if (await _context.Rooms.AnyAsync(r => r.RoomCode == vm.RoomCode && r.RoomId != id))
                ModelState.AddModelError(nameof(vm.RoomCode), "Ma phong da duoc su dung boi phong khac.");

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(vm);
                return View(vm);
            }

            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            // Neu co anh moi -> xoa anh cu, luu anh moi
            if (vm.ThumbnailFile != null && vm.ThumbnailFile.Length > 0)
            {
                DeleteImage(room.ThumbnailImage);
                room.ThumbnailImage = await SaveImageAsync(vm.ThumbnailFile);
            }

            // Cap nhat tung truong
            room.RoomCode       = vm.RoomCode.Trim();
            room.RoomName       = vm.RoomName.Trim();
            room.RoomTypeId     = vm.RoomTypeId;
            room.RoomPrice      = vm.RoomPrice;
            room.DefaultDeposit = vm.DefaultDeposit;
            room.Area           = vm.Area;
            room.Floor          = vm.Floor;
            room.MaxOccupants   = vm.MaxOccupants;
            room.Description    = vm.Description;
            room.Address        = vm.Address?.Trim();
            room.Latitude       = vm.Latitude;
            room.Longitude      = vm.Longitude;
            room.Status         = vm.Status;
            room.IsPublished    = vm.IsPublished;
            room.UpdatedAt      = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = string.Concat("Cap nhat phong \"", room.RoomName, "\" thanh cong.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Rooms.AnyAsync(r => r.RoomId == id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        //  DELETE  -  Chi dung POST de tranh CSRF (khong dung [HttpGet])
        // ================================================================

        // POST: /Admin/Room/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.Contracts)
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null) return NotFound();

            // Bao ve: khong cho xoa phong dang co hop dong hieu luc
            if (room.Contracts.Any(c => c.Status == ContractStatus.Active))
            {
                TempData["Error"] = "Khong the xoa phong dang co hop dong hieu luc!";
                return RedirectToAction(nameof(Index));
            }

            // Xoa anh vat ly tren server
            DeleteImage(room.ThumbnailImage);

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            TempData["Success"] = string.Concat("Da xoa phong \"", room.RoomName, "\".");
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        //  PRIVATE HELPERS
        // ================================================================

        /// <summary>Do du lieu cac dropdown truoc khi tra View.</summary>
        private async Task PopulateDropdownsAsync(RoomCreateViewModel vm)
        {
            var roomTypes = await _context.RoomTypes
                .Where(rt => rt.IsActive)
                .OrderBy(rt => rt.SortOrder)
                .ToListAsync();

            vm.RoomTypeSelectList = new SelectList(
                roomTypes, "RoomTypeId", "RoomTypeName", vm.RoomTypeId);
        }

        /// <summary>Luu file anh vao wwwroot/images/rooms, tra ve duong dan tuong doi.</summary>
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            string uploadDir = Path.Combine(_env.WebRootPath, IMAGE_FOLDER);
            Directory.CreateDirectory(uploadDir);

            string ext      = Path.GetExtension(file.FileName);
            string fileName = Guid.NewGuid().ToString() + ext;
            string filePath = Path.Combine(uploadDir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return "/" + IMAGE_FOLDER + "/" + fileName;
        }

        /// <summary>Xoa file anh vat ly khoi wwwroot (bo qua neu khong tim thay).</summary>
        private void DeleteImage(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            string sanitized = relativePath.TrimStart('/');
            string fullPath  = Path.Combine(_env.WebRootPath, sanitized);

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
    }
}
