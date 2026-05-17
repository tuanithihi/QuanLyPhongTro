using System.Diagnostics;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Controllers
{
    public class HomeController : Controller
    {
        private readonly DataContext _db;

        public HomeController(DataContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? area, int? roomTypeId, string? priceRange, string? areaRange, string? floorRange, double? userLat, double? userLng)
        {
            // ── Lọc phòng đang trống, đã hiển thị ───────────────────────
            var query = _db.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == RoomStatus.Available && r.IsPublished)
                .AsQueryable();

            // ── Áp dụng bộ lọc nếu có ────────────────────────────────────
            // Nếu có tọa độ (tìm theo khoảng cách), bỏ qua lọc text tuyệt đối theo area
            if (!string.IsNullOrWhiteSpace(area) && (!userLat.HasValue || !userLng.HasValue))
            {
                area = area.Trim();
                query = query.Where(r => r.RoomName.Contains(area) || (r.Description != null && r.Description.Contains(area)));
            }

            if (roomTypeId.HasValue)
                query = query.Where(r => r.RoomTypeId == roomTypeId.Value);

            if (!string.IsNullOrWhiteSpace(priceRange))
            {
                query = priceRange switch
                {
                    "under2m" => query.Where(r => r.RoomPrice < 2_000_000),
                    "2to3m"   => query.Where(r => r.RoomPrice >= 2_000_000 && r.RoomPrice < 3_000_000),
                    "3to5m"   => query.Where(r => r.RoomPrice >= 3_000_000 && r.RoomPrice <= 5_000_000),
                    "over5m"  => query.Where(r => r.RoomPrice > 5_000_000),
                    _         => query
                };
            }

            if (!string.IsNullOrWhiteSpace(areaRange))
            {
                query = areaRange switch
                {
                    "under15" => query.Where(r => r.Area > 0 && r.Area < 15),
                    "15to25"  => query.Where(r => r.Area >= 15 && r.Area <= 25),
                    "25to35"  => query.Where(r => r.Area > 25 && r.Area <= 35),
                    "over35"  => query.Where(r => r.Area > 35),
                    _         => query
                };
            }

            if (!string.IsNullOrWhiteSpace(floorRange))
            {
                query = floorRange switch
                {
                    "floor1"    => query.Where(r => r.Floor == 1),
                    "floor2"    => query.Where(r => r.Floor == 2),
                    "floor3plus"=> query.Where(r => r.Floor >= 3),
                    _           => query
                };
            }

            var rooms = await query.OrderBy(r => r.RoomPrice).ToListAsync();

            // ── Lấy phòng nổi bật (không bị ảnh hưởng bởi tìm kiếm) ──────
            var featuredRooms = await _db.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == RoomStatus.Available && r.IsPublished)
                .OrderByDescending(r => r.CreatedAt)
                .Take(3)
                .ToListAsync();

            // ── Tính khoảng cách nếu có tọa độ người dùng ──────────────
            if (userLat.HasValue && userLng.HasValue)
            {
                foreach (var room in rooms)
                {
                    if (room.Latitude.HasValue && room.Longitude.HasValue)
                        room.DistanceKm = HaversineKm(userLat.Value, userLng.Value, room.Latitude.Value, room.Longitude.Value);
                }
                rooms = rooms
                    .OrderBy(r => r.DistanceKm.HasValue ? 0 : 1)
                    .ThenBy(r => r.DistanceKm ?? double.MaxValue)
                    .ToList();
            }

            // ── Gắn rating thực từ DB cho mỗi phòng ─────────────────
            await PopulateRoomRatingsAsync(rooms);
            await PopulateRoomRatingsAsync(featuredRooms);

            var vm = new HomeIndexViewModel
            {
                AvailableRooms = rooms,
                FeaturedRooms  = featuredRooms,
                RoomTypes      = await _db.RoomTypes.Where(rt => rt.IsActive).OrderBy(rt => rt.SortOrder).ToListAsync(),
                RecentPosts    = await _db.Posts
                                     .Where(p => p.IsPublished)
                                     .OrderByDescending(p => p.IsPinned)
                                     .ThenByDescending(p => p.PublishedAt)
                                     .Take(3)
                                     .ToListAsync(),
                RecentReviews  = await _db.Reviews
                                     .Where(r => r.IsApproved)
                                     .OrderByDescending(r => r.CreatedAt)
                                     .Take(3)
                                     .ToListAsync(),
                Area           = area,
                RoomTypeId     = roomTypeId,
                PriceRange     = priceRange,
                AreaRange      = areaRange,
                FloorRange     = floorRange,
                UserLat        = userLat,
                UserLng        = userLng,
            };

            return View(vm);
        }

        public async Task<IActionResult> AllRooms(string? area, int? roomTypeId, string? priceRange, string? areaRange, string? floorRange, double? userLat, double? userLng, int page = 1)
        {
            const int pageSize = 12;
            var query = _db.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == RoomStatus.Available && r.IsPublished)
                .AsQueryable();

            // Nếu có tọa độ (tìm theo khoảng cách), bỏ qua lọc text tuyệt đối theo area
            if (!string.IsNullOrWhiteSpace(area) && (!userLat.HasValue || !userLng.HasValue))
            {
                area = area.Trim();
                query = query.Where(r => r.RoomName.Contains(area) || (r.Description != null && r.Description.Contains(area)));
            }

            if (roomTypeId.HasValue)
                query = query.Where(r => r.RoomTypeId == roomTypeId.Value);

            if (!string.IsNullOrWhiteSpace(priceRange))
            {
                query = priceRange switch
                {
                    "under2m" => query.Where(r => r.RoomPrice < 2_000_000),
                    "2to3m"   => query.Where(r => r.RoomPrice >= 2_000_000 && r.RoomPrice < 3_000_000),
                    "3to5m"   => query.Where(r => r.RoomPrice >= 3_000_000 && r.RoomPrice <= 5_000_000),
                    "over5m"  => query.Where(r => r.RoomPrice > 5_000_000),
                    _         => query
                };
            }

            if (!string.IsNullOrWhiteSpace(areaRange))
            {
                query = areaRange switch
                {
                    "under15" => query.Where(r => r.Area > 0 && r.Area < 15),
                    "15to25"  => query.Where(r => r.Area >= 15 && r.Area <= 25),
                    "25to35"  => query.Where(r => r.Area > 25 && r.Area <= 35),
                    "over35"  => query.Where(r => r.Area > 35),
                    _         => query
                };
            }

            if (!string.IsNullOrWhiteSpace(floorRange))
            {
                query = floorRange switch
                {
                    "floor1"    => query.Where(r => r.Floor == 1),
                    "floor2"    => query.Where(r => r.Floor == 2),
                    "floor3plus"=> query.Where(r => r.Floor >= 3),
                    _           => query
                };
            }

            var total = await query.CountAsync();
            var rooms = await query
                .OrderBy(r => r.RoomPrice)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ── Tính khoảng cách nếu có tọa độ người dùng ──────────────
            if (userLat.HasValue && userLng.HasValue)
            {
                foreach (var room in rooms)
                {
                    if (room.Latitude.HasValue && room.Longitude.HasValue)
                        room.DistanceKm = HaversineKm(userLat.Value, userLng.Value, room.Latitude.Value, room.Longitude.Value);
                }
                rooms = rooms
                    .OrderBy(r => r.DistanceKm.HasValue ? 0 : 1)
                    .ThenBy(r => r.DistanceKm ?? double.MaxValue)
                    .ToList();
            }

            // ── Gắn rating thực từ DB cho mỗi phòng ─────────────────
            await PopulateRoomRatingsAsync(rooms);

            var vm = new HomeIndexViewModel
            {
                AvailableRooms = rooms,
                RoomTypes      = await _db.RoomTypes.Where(rt => rt.IsActive).OrderBy(rt => rt.SortOrder).ToListAsync(),
                Area           = area,
                RoomTypeId     = roomTypeId,
                PriceRange     = priceRange,
                AreaRange      = areaRange,
                FloorRange     = floorRange,
                UserLat        = userLat,
                UserLng        = userLng,
            };

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = total;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            return View(vm);
        }

        public async Task<IActionResult> AllReviews(int page = 1)
        {
            const int pageSize = 12;
            if (page < 1) page = 1;

            var query = _db.Reviews
                .Where(r => r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .AsQueryable();

            var total = await query.CountAsync();
            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = total;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            return View(reviews);
        }

        public IActionResult Privacy() => View();

        // ── Chi tiết phòng ────────────────────────────────────────────

        // GET: /Home/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var room = await _db.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == id && r.IsPublished);

            if (room == null) return NotFound();

            var services = await _db.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.ServiceType)
                .ToListAsync();

            // ── Tải đánh giá phòng ─────────────────────────────────────
            var roomReviews = await _db.RoomReviews
                .Where(rv => rv.RoomId == id && rv.IsApproved)
                .OrderByDescending(rv => rv.CreatedAt)
                .ToListAsync();

            double avgRating  = roomReviews.Count > 0 ? roomReviews.Average(rv => rv.Rating) : 0;
            int    reviewCount = roomReviews.Count;

            // ── Kiểm tra đăng nhập để điền sẵn thông tin đặt lịch ──
            int? tenantId = int.TryParse(HttpContext.Session.GetString("TenantUser"), out var tid) ? tid : null;
            int? userId   = int.TryParse(HttpContext.Session.GetString("NormalUser"),  out var uid) ? uid : null;

            string prefillName = "";

            if (tenantId.HasValue)
            {
                var tenant = await _db.Tenants.FindAsync(tenantId.Value);
                if (tenant != null)
                {
                    ViewBag.IsLoggedIn    = true;
                    ViewBag.BookingName   = tenant.FullName;
                    ViewBag.BookingPhone  = tenant.Phone ?? "";
                    prefillName           = tenant.FullName;
                }
            }
            else if (userId.HasValue)
            {
                var user = await _db.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    ViewBag.IsLoggedIn    = true;
                    ViewBag.BookingName   = string.IsNullOrEmpty(user.FullName) ? user.Username : user.FullName;
                    ViewBag.BookingPhone  = user.Phone ?? "";
                    prefillName           = ViewBag.BookingName;
                }
            }
            else
            {
                ViewBag.IsLoggedIn = false;
            }

            // ── Tìm đánh giá hiện tại của user (nếu đã đánh giá) ──
            tblRoomReview? myReview = null;
            if (tenantId.HasValue)
                myReview = roomReviews.FirstOrDefault(rv => rv.TenantId == tenantId.Value);
            else if (userId.HasValue)
                myReview = roomReviews.FirstOrDefault(rv => rv.UserId == userId.Value);

            return View(new RoomDetailViewModel
            {
                Room          = room,
                Services      = services,
                RoomReviews   = roomReviews,
                AverageRating = avgRating,
                ReviewCount   = reviewCount,
                ReviewName    = prefillName,
                MyReview      = myReview,
                ReviewRating  = myReview?.Rating ?? 5,
                ReviewComment = myReview?.Comment
            });
        }

        // POST: /Home/SubmitRoomReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRoomReview(int roomId, string reviewName, int reviewRating, string? reviewComment)
        {
            int? tenantId = int.TryParse(HttpContext.Session.GetString("TenantUser"), out var tid) ? tid : (int?)null;
            int? userId   = int.TryParse(HttpContext.Session.GetString("NormalUser"),  out var uid) ? uid : (int?)null;
            if (!tenantId.HasValue && !userId.HasValue)
            {
                TempData["ReviewError"] = "Vui lòng đăng nhập để gửi đánh giá.";
                return RedirectToAction("Details", new { id = roomId });
            }

            reviewName = (reviewName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(reviewName))
            {
                TempData["ReviewError"] = "Vui lòng nhập tên của bạn.";
                return RedirectToAction("Details", new { id = roomId });
            }

            // Kiểm tra đã đánh giá chưa
            var existing = await _db.RoomReviews.FirstOrDefaultAsync(rv =>
                rv.RoomId == roomId &&
                ((tenantId.HasValue && rv.TenantId == tenantId) || (userId.HasValue && rv.UserId == userId)));
            if (existing != null)
            {
                TempData["ReviewError"] = "Bạn đã đánh giá phòng này rồi. Hãy dùng chức năng sửa đánh giá.";
                return RedirectToAction("Details", new { id = roomId });
            }

            reviewRating = Math.Clamp(reviewRating, 1, 5);

            _db.RoomReviews.Add(new tblRoomReview
            {
                RoomId     = roomId,
                TenantId   = tenantId,
                UserId     = userId,
                FullName   = reviewName,
                Rating     = reviewRating,
                Comment    = reviewComment?.Trim(),
                IsApproved = true,
                CreatedAt  = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["ReviewSuccess"] = "Cảm ơn bạn đã đánh giá phòng!";
            return RedirectToAction("Details", new { id = roomId });
        }

        // POST: /Home/UpdateRoomReview — Sửa đánh giá phòng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoomReview(int roomId, int reviewId, string reviewName, int reviewRating, string? reviewComment)
        {
            int? tenantId = int.TryParse(HttpContext.Session.GetString("TenantUser"), out var tid) ? tid : (int?)null;
            int? userId   = int.TryParse(HttpContext.Session.GetString("NormalUser"),  out var uid) ? uid : (int?)null;
            if (!tenantId.HasValue && !userId.HasValue)
            {
                TempData["ReviewError"] = "Vui lòng đăng nhập để sửa đánh giá.";
                return RedirectToAction("Details", new { id = roomId });
            }

            var review = await _db.RoomReviews.FindAsync(reviewId);
            if (review == null || review.RoomId != roomId)
            {
                TempData["ReviewError"] = "Không tìm thấy đánh giá.";
                return RedirectToAction("Details", new { id = roomId });
            }

            // Chỉ cho phép sửa đánh giá của chính mình
            bool isOwner = (tenantId.HasValue && review.TenantId == tenantId) ||
                           (userId.HasValue && review.UserId == userId);
            if (!isOwner)
            {
                TempData["ReviewError"] = "Bạn không có quyền sửa đánh giá này.";
                return RedirectToAction("Details", new { id = roomId });
            }

            review.FullName = (reviewName ?? string.Empty).Trim();
            review.Rating   = Math.Clamp(reviewRating, 1, 5);
            review.Comment  = reviewComment?.Trim();

            await _db.SaveChangesAsync();

            TempData["ReviewSuccess"] = "Đánh giá đã được cập nhật!";
            return RedirectToAction("Details", new { id = roomId });
        }

        // POST: /Home/BookViewing  — Đặt lịch xem phòng → lưu DB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookViewing(int roomId, string bookingName, string bookingPhone,
                                                     string? bookingDate, string? bookingNote)
        {
            // ── Yêu cầu đăng nhập ─────────────────────────────────────────
            bool isTenant = !string.IsNullOrEmpty(HttpContext.Session.GetString("TenantUser"));
            bool isUser   = !string.IsNullOrEmpty(HttpContext.Session.GetString("NormalUser"));
            if (!isTenant && !isUser)
            {
                TempData["BookingError"] = "Vui lòng đăng nhập để đặt lịch xem phòng.";
                return RedirectToAction("Details", new { id = roomId });
            }

            if (string.IsNullOrWhiteSpace(bookingName) || string.IsNullOrWhiteSpace(bookingPhone))
            {
                TempData["BookingError"] = "Vui lòng điền đầy đủ họ tên và số điện thoại.";
                return RedirectToAction("Details", new { id = roomId });
            }

            _db.BookingRequests.Add(new tblBookingRequest
            {
                RoomId        = roomId,
                FullName      = bookingName.Trim(),
                Phone         = bookingPhone.Trim(),
                PreferredDate = bookingDate,
                Message       = bookingNote?.Trim(),
                RequestType   = BookingRequestType.ViewingRequest,
                CreatedAt     = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["BookingSuccess"] = $"Đặt lịch thành công! Chúng tôi sẽ liên hệ lại với <strong>{bookingPhone}</strong> trong thời gian sớm nhất.";
            return RedirectToAction("Details", new { id = roomId });
        }

        public IActionResult AccessDenied() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(string name, string email, int rating, string title, string content)
        {
            name = (name ?? string.Empty).Trim();
            email = (email ?? string.Empty).Trim();
            title = (title ?? string.Empty).Trim();
            content = (content ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin đánh giá.";
                return RedirectToAction(nameof(Index), new { review = "1" });
            }

            if (!new EmailAddressAttribute().IsValid(email))
            {
                TempData["Error"] = "Email không hợp lệ.";
                return RedirectToAction(nameof(Index), new { review = "1" });
            }

            rating = Math.Clamp(rating, 1, 5);

            _db.Reviews.Add(new tblReview
            {
                FullName = name,
                Email = email,
                Title = title,
                Content = content,
                Rating = rating,
                IsApproved = true,
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            TempData["Success"] = "Cảm ơn bạn! Đánh giá đã được ghi nhận.";
            return RedirectToAction(nameof(Index), new { review = "1" });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
        // ── Haversine: tính khoảng cách (km) giữa hai tọa độ ───────────
        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        // ── Gắn AverageRating + ReviewCount vào danh sách phòng ─────
        private async Task PopulateRoomRatingsAsync(List<tblRoom> rooms)
        {
            if (!rooms.Any()) return;
            var roomIds = rooms.Select(r => r.RoomId).ToList();
            var stats = await _db.RoomReviews
                .Where(rv => roomIds.Contains(rv.RoomId) && rv.IsApproved)
                .GroupBy(rv => rv.RoomId)
                .Select(g => new { RoomId = g.Key, Avg = g.Average(rv => rv.Rating), Count = g.Count() })
                .ToListAsync();
            foreach (var room in rooms)
            {
                var s = stats.FirstOrDefault(x => x.RoomId == room.RoomId);
                if (s != null) { room.AverageRating = s.Avg; room.ReviewCount = s.Count; }
            }
        }
    }
}
