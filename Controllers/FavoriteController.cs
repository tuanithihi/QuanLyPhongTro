using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Controllers
{
    public class FavoriteController : Controller
    {
        private readonly DataContext _db;
        private readonly IRoomSearchService _roomSearchService;
        private readonly ILogger<FavoriteController> _logger;

        public FavoriteController(DataContext db, IRoomSearchService roomSearchService, ILogger<FavoriteController> logger)
        {
            _db = db;
            _roomSearchService = roomSearchService;
            _logger = logger;
        }

        // ── GET: /phong-da-luu ─────────────────────────────────────────────
        [HttpGet("/phong-da-luu")]
        [HttpGet("/Favorites")]
        public async Task<IActionResult> Index()
        {
            int? tenantId = int.TryParse(HttpContext.Session.GetString("TenantUser"), out var tid) ? tid : null;
            int? userId = int.TryParse(HttpContext.Session.GetString("NormalUser"), out var uid) ? uid : null;

            var favoriteRooms = new List<RoomCardViewModel>();

            if (tenantId.HasValue || userId.HasValue)
            {
                var query = _db.Favorites
                    .AsNoTracking()
                    .Where(f => (tenantId.HasValue && f.TenantId == tenantId) || (userId.HasValue && f.UserId == userId))
                    .Select(f => f.RoomId);

                var roomIds = await query.ToListAsync();
                if (roomIds.Any())
                {
                    var rooms = await _db.Rooms
                        .AsNoTracking()
                        .Include(r => r.Property).ThenInclude(p => p!.Province)
                        .Include(r => r.Property).ThenInclude(p => p!.District)
                        .Include(r => r.Property).ThenInclude(p => p!.Landlord)
                        .Include(r => r.RoomType)
                        .Include(r => r.RoomImages)
                        .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
                        .Where(r => roomIds.Contains(r.RoomId))
                        .ToListAsync();

                    favoriteRooms = rooms.Select(r =>
                    {
                        var primaryImg = r.RoomImages.OrderBy(img => img.SortOrder).FirstOrDefault(img => img.IsPrimary)
                                      ?? r.RoomImages.OrderBy(img => img.SortOrder).FirstOrDefault();

                        return new RoomCardViewModel
                        {
                            RoomId = r.RoomId,
                            Title = r.Title ?? r.RoomName,
                            RoomName = r.RoomName,
                            RoomCode = r.RoomCode,
                            Slug = r.Slug,
                            RoomPrice = r.RoomPrice,
                            FormattedPrice = DisplayHelper.FormatPrice(r.RoomPrice),
                            FormattedPriceCompact = DisplayHelper.FormatPriceCompact(r.RoomPrice),
                            Area = r.Area,
                            FormattedArea = DisplayHelper.FormatArea(r.Area),
                            Floor = r.Floor,
                            Capacity = r.Capacity,
                            Status = r.Status,
                            ApprovalStatus = r.ApprovalStatus,
                            IsFeatured = r.IsFeatured,
                            PublishedAt = r.PublishedAt,
                            CreatedAt = r.CreatedAt,
                            ThumbnailUrl = primaryImg?.Url ?? "/images/room-placeholder.jpg",
                            ImageCount = r.RoomImages.Count,
                            PropertyId = r.PropertyId ?? 0,
                            PropertyName = r.Property?.Name ?? "",
                            FullAddress = r.Property?.Address,
                            ShortAddress = DisplayHelper.ShortenAddress(r.Property?.Address, r.Property?.District?.Name, r.Property?.Province?.Name),
                            LandlordId = r.Property?.LandlordId ?? 0,
                            LandlordName = r.Property?.Landlord?.FullName ?? "Chủ trọ",
                            LandlordAvatar = r.Property?.Landlord?.Avatar,
                            LandlordPhone = r.Property?.Landlord?.Phone,
                            LandlordSlug = SlugHelper.ToSlug(r.Property?.Landlord?.FullName ?? "chu-tro"),
                            AmenityNames = r.RoomAmenities.Select(ra => ra.Amenity?.Name).Where(n => !string.IsNullOrEmpty(n)).Cast<string>().Take(3).ToList(),
                            IsFavorite = true
                        };
                    }).ToList();
                }
            }

            ViewBag.IsLoggedIn = tenantId.HasValue || userId.HasValue;
            return View(favoriteRooms);
        }

        // ── POST: /api/favorites/toggle ───────────────────────────────────
        [HttpPost("/api/favorites/toggle")]
        public async Task<IActionResult> Toggle([FromBody] FavoriteToggleRequest req)
        {
            if (req == null || req.RoomId <= 0)
                return BadRequest(new { success = false, message = "Phòng không hợp lệ." });

            int? tenantId = int.TryParse(HttpContext.Session.GetString("TenantUser"), out var tid) ? tid : null;
            int? userId = int.TryParse(HttpContext.Session.GetString("NormalUser"), out var uid) ? uid : null;

            if (!tenantId.HasValue && !userId.HasValue)
            {
                return Ok(new
                {
                    success = false,
                    requireLogin = true,
                    message = "Vui lòng đăng nhập để lưu phòng vào danh sách yêu thích."
                });
            }

            var existing = await _db.Favorites.FirstOrDefaultAsync(f =>
                f.RoomId == req.RoomId &&
                ((tenantId.HasValue && f.TenantId == tenantId) || (userId.HasValue && f.UserId == userId)));

            bool isFavorite;
            if (existing != null)
            {
                _db.Favorites.Remove(existing);
                await _db.SaveChangesAsync();
                isFavorite = false;
            }
            else
            {
                // Đảm bảo không trùng lặp
                var newFav = new tblFavorite
                {
                    RoomId = req.RoomId,
                    TenantId = tenantId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Favorites.Add(newFav);
                await _db.SaveChangesAsync();
                isFavorite = true;
            }

            int totalCount = await _db.Favorites.CountAsync(f =>
                (tenantId.HasValue && f.TenantId == tenantId) || (userId.HasValue && f.UserId == userId));

            return Ok(new
            {
                success = true,
                isFavorite = isFavorite,
                totalCount = totalCount,
                message = isFavorite ? "Đã lưu phòng vào danh sách yêu thích." : "Đã bỏ lưu phòng yêu thích."
            });
        }

        // ── POST: /api/favorites/sync ─────────────────────────────────────
        [HttpPost("/api/favorites/sync")]
        public async Task<IActionResult> Sync([FromBody] FavoriteSyncRequest req)
        {
            if (req == null || req.RoomIds == null || !req.RoomIds.Any())
                return Ok(new { success = true, addedCount = 0 });

            int? tenantId = int.TryParse(HttpContext.Session.GetString("TenantUser"), out var tid) ? tid : null;
            int? userId = int.TryParse(HttpContext.Session.GetString("NormalUser"), out var uid) ? uid : null;

            if (!tenantId.HasValue && !userId.HasValue)
                return Unauthorized(new { success = false, message = "Chưa đăng nhập." });

            var existingIds = await _db.Favorites
                .Where(f => (tenantId.HasValue && f.TenantId == tenantId) || (userId.HasValue && f.UserId == userId))
                .Select(f => f.RoomId)
                .ToListAsync();

            var toAddIds = req.RoomIds.Distinct().Except(existingIds).ToList();
            int addedCount = 0;

            foreach (var rId in toAddIds)
            {
                bool roomExists = await _db.Rooms.AnyAsync(r => r.RoomId == rId);
                if (roomExists)
                {
                    _db.Favorites.Add(new tblFavorite
                    {
                        RoomId = rId,
                        TenantId = tenantId,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    });
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true, addedCount = addedCount });
        }
    }

    public class FavoriteToggleRequest
    {
        public int RoomId { get; set; }
    }

    public class FavoriteSyncRequest
    {
        public List<int> RoomIds { get; set; } = new List<int>();
    }
}
