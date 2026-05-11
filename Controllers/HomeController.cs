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

        public async Task<IActionResult> Index(string? area, int? roomTypeId, string? priceRange, string? areaRange, string? floorRange)
        {
            // ── Lọc phòng đang trống, đã hiển thị ───────────────────────
            var query = _db.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == RoomStatus.Available && r.IsPublished)
                .AsQueryable();

            // ── Áp dụng bộ lọc nếu có ────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(area))
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

            var vm = new HomeIndexViewModel
            {
                AvailableRooms = await query.OrderBy(r => r.RoomPrice).ToListAsync(),
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
            };

            return View(vm);
        }

        public async Task<IActionResult> AllRooms(string? area, int? roomTypeId, string? priceRange, string? areaRange, string? floorRange, int page = 1)
        {
            const int pageSize = 12;
            var query = _db.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == RoomStatus.Available && r.IsPublished)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(area))
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

            var vm = new HomeIndexViewModel
            {
                AvailableRooms = rooms,
                RoomTypes      = await _db.RoomTypes.Where(rt => rt.IsActive).OrderBy(rt => rt.SortOrder).ToListAsync(),
                Area           = area,
                RoomTypeId     = roomTypeId,
                PriceRange     = priceRange,
                AreaRange      = areaRange,
                FloorRange     = floorRange,
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

            // ── Kiểm tra đăng nhập để điền sẵn thông tin đặt lịch ──
            int? tenantId = int.TryParse(HttpContext.Session.GetString("TenantUser"), out var tid) ? tid : null;
            int? userId   = int.TryParse(HttpContext.Session.GetString("NormalUser"),  out var uid) ? uid : null;

            if (tenantId.HasValue)
            {
                var tenant = await _db.Tenants.FindAsync(tenantId.Value);
                if (tenant != null)
                {
                    ViewBag.IsLoggedIn    = true;
                    ViewBag.BookingName   = tenant.FullName;
                    ViewBag.BookingPhone  = tenant.Phone ?? "";
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
                }
            }
            else
            {
                ViewBag.IsLoggedIn = false;
            }

            return View(new RoomDetailViewModel { Room = room, Services = services });
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
    }
}
