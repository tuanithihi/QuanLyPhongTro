using System.Diagnostics;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Controllers
{
    public class HomeController : Controller
    {
        private readonly DataContext _db;
        private readonly IRoomSearchService _roomSearchService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            DataContext db,
            IRoomSearchService roomSearchService,
            ILogger<HomeController> logger)
        {
            _db = db;
            _roomSearchService = roomSearchService;
            _logger = logger;
        }

        // ── 1. TRANG CHỦ ──────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var featuredCards = await _roomSearchService.GetFeaturedRoomsAsync(6);
            var recentCards = await _roomSearchService.GetRecentRoomsAsync(6);
            var popularProvinces = await _roomSearchService.GetPopularProvincesAsync(6);
            var featuredLandlords = await _roomSearchService.GetFeaturedLandlordsAsync(4);
            var roomTypes = await _roomSearchService.GetActiveRoomTypesAsync();
            var recentPosts = await _db.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.IsPinned)
                .ThenByDescending(p => p.PublishedAt)
                .Take(3)
                .ToListAsync();

            var vm = new HomeIndexViewModel
            {
                FeaturedCardRooms = featuredCards,
                RecentCardRooms = recentCards,
                PopularProvinces = popularProvinces,
                FeaturedLandlords = featuredLandlords,
                RoomTypes = roomTypes,
                RecentPosts = recentPosts
            };

            return View(vm);
        }

        // ── 2. TRANG DANH SÁCH TÌM KIẾM ───────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Listings(string? tinh, string? quan, [FromQuery] RoomFilterCriteria criteria)
        {
            criteria ??= new RoomFilterCriteria();

            if (!string.IsNullOrWhiteSpace(tinh))
                criteria.ProvinceSlug = tinh;
            if (!string.IsNullOrWhiteSpace(quan))
                criteria.DistrictSlug = quan;

            var result = await _roomSearchService.SearchRoomsAsync(criteria);
            return View(result);
        }

        // ── 3. TRANG CHI TIẾT PHÒNG (/phong/{slug}-{id}) ──────────────────
        [HttpGet]
        public async Task<IActionResult> RoomDetail(string slug, int id)
        {
            if (id <= 0)
            {
                Response.StatusCode = 404;
                return View("NotFound");
            }

            var room = await _roomSearchService.GetRoomDetailAsync(id, slug);
            if (room == null)
            {
                Response.StatusCode = 404;
                return View("NotFound");
            }

            // Đếm lượt xem phòng, chống tăng ảo cơ bản (1 lượt / phiên / phòng)
            string sessionViewKey = $"Viewed_Room_{id}";
            if (string.IsNullOrEmpty(HttpContext.Session.GetString(sessionViewKey)))
            {
                try
                {
                    var trackedRoom = await _db.Rooms.FindAsync(id);
                    if (trackedRoom != null)
                    {
                        trackedRoom.ViewCount++;
                        await _db.SaveChangesAsync();
                        HttpContext.Session.SetString(sessionViewKey, "1");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi cập nhật ViewCount cho phòng {RoomId}", id);
                }
            }

            // Lấy danh sách dịch vụ của chủ trọ sở hữu
            int? landlordId = room.Property?.LandlordId;
            var services = await _db.Services
                .AsNoTracking()
                .Where(s => s.IsActive && (s.LandlordId == landlordId || s.LandlordId == null))
                .OrderByDescending(s => s.LandlordId.HasValue)
                .ThenBy(s => s.ServiceType)
                .ToListAsync();

            // Lấy phòng tương tự trong cùng quận/huyện
            var similarResult = await _roomSearchService.SearchRoomsAsync(new RoomFilterCriteria
            {
                DistrictId = room.Property?.DistrictId,
                ProvinceId = room.Property?.ProvinceId,
                PageSize = 4
            });
            var similarRooms = similarResult.Items.Where(r => r.RoomId != room.RoomId).Take(3).ToList();

            // Đánh giá phòng
            var roomReviews = await _db.RoomReviews
                .AsNoTracking()
                .Where(rv => rv.RoomId == id && rv.IsApproved)
                .OrderByDescending(rv => rv.CreatedAt)
                .ToListAsync();

            double avgRating = roomReviews.Count > 0 ? roomReviews.Average(rv => rv.Rating) : 5.0;
            int reviewCount = roomReviews.Count;

            // Điền trước thông tin nếu đã đăng nhập
            int? tenantId = int.TryParse(HttpContext.Session.GetString("TenantUser"), out var tid) ? tid : null;
            int? userId = int.TryParse(HttpContext.Session.GetString("NormalUser"), out var uid) ? uid : null;

            string prefillName = string.Empty;
            string prefillPhone = string.Empty;

            if (tenantId.HasValue)
            {
                var tenant = await _db.Tenants.FindAsync(tenantId.Value);
                if (tenant != null)
                {
                    ViewBag.IsLoggedIn = true;
                    prefillName = tenant.FullName;
                    prefillPhone = tenant.Phone ?? string.Empty;
                }
            }
            else if (userId.HasValue)
            {
                var user = await _db.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    ViewBag.IsLoggedIn = true;
                    prefillName = string.IsNullOrEmpty(user.FullName) ? user.Username : user.FullName;
                    prefillPhone = user.Phone ?? string.Empty;
                }
            }
            else
            {
                ViewBag.IsLoggedIn = false;
            }

            tblRoomReview? myReview = null;
            if (tenantId.HasValue || userId.HasValue)
            {
                myReview = roomReviews.FirstOrDefault(rv =>
                    (tenantId.HasValue && rv.TenantId == tenantId) ||
                    (userId.HasValue && rv.UserId == userId));
            }

            var vm = new RoomDetailViewModel
            {
                Room = room,
                Services = services,
                SimilarRooms = similarRooms,
                RoomReviews = roomReviews,
                AverageRating = avgRating,
                ReviewCount = reviewCount,
                ReviewName = prefillName,
                MyReview = myReview,
                ReviewRating = myReview?.Rating ?? 5,
                ReviewComment = myReview?.Comment
            };

            return View("RoomDetail", vm);
        }

        // ── 4. TRANG HỒ SƠ CHỦ TRỌ CÔNG KHAI (/chu-tro/{slug}) ────────────
        [HttpGet]
        public async Task<IActionResult> LandlordProfile(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                Response.StatusCode = 404;
                return View("NotFound");
            }

            var profile = await _roomSearchService.GetLandlordPublicProfileAsync(slug);
            if (profile == null)
            {
                Response.StatusCode = 404;
                return View("NotFound");
            }

            // Tính điểm đánh giá trung bình của chủ trọ từ các hợp đồng
            var reviews = await _db.Reviews
                .Include(r => r.Contract)
                .Where(r => r.Contract != null && r.Contract.LandlordId == profile.Landlord.LandlordId && r.IsApproved)
                .ToListAsync();

            if (reviews.Any())
            {
                profile.AverageRating = Math.Round(reviews.Average(r => r.Rating), 1);
                profile.TotalReviews = reviews.Count;
            }

            return View("LandlordProfile", profile);
        }

        // ── 5. SO SÁNH PHÒNG TRỌ (/so-sanh) ───────────────────────────────
        [HttpGet("/so-sanh")]
        public async Task<IActionResult> Compare(string? ids)
        {
            var roomIds = (ids ?? "")
                .Split(new[] { ',', '-' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .Take(3)
                .ToList();

            var rooms = new List<RoomDetailViewModel>();
            foreach (var rId in roomIds)
            {
                var detail = await _roomSearchService.GetRoomDetailAsync(rId);
                if (detail != null)
                {
                    int? lId = detail.Property?.LandlordId;
                    var services = await _db.Services
                        .AsNoTracking()
                        .Where(s => s.IsActive && (s.LandlordId == lId || s.LandlordId == null))
                        .ToListAsync();

                    rooms.Add(new RoomDetailViewModel
                    {
                        Room = detail,
                        Services = services
                    });
                }
            }

            return View(rooms);
        }

        // ── 6. TƯƠNG THÍCH ĐƯỜNG DẪN CŨ ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var room = await _db.Rooms.FindAsync(id);
            if (room != null && !string.IsNullOrWhiteSpace(room.Slug))
            {
                return RedirectToActionPermanent(nameof(RoomDetail), new { slug = room.Slug, id = room.RoomId });
            }
            return await RoomDetail(room?.Slug ?? "phong", id);
        }

        [HttpGet]
        public IActionResult AllRooms(string? area, int? roomTypeId, string? priceRange, string? areaRange, int page = 1)
        {
            return RedirectToAction(nameof(Listings));
        }

        // ── 7. AJAX API CASCADING ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            var districts = await _roomSearchService.GetDistrictsByProvinceAsync(provinceId);
            return Json(districts.Select(d => new { id = d.DistrictId, name = d.Name, slug = SlugHelper.ToSlug(d.Name) }));
        }

        [HttpGet]
        public async Task<IActionResult> GetWards(int districtId)
        {
            var wards = await _roomSearchService.GetWardsByDistrictAsync(districtId);
            return Json(wards.Select(w => new { id = w.WardId, name = w.Name }));
        }

        // ── 8. BOOKING & REVIEWS VỚI PHÂN QUYỀN VÀ VALIDATE CHẶT CHẼ ───────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookViewing(int roomId, string bookingName, string bookingPhone,
                                                     string? bookingDate, string? bookingNote, BookingRequestType requestType = BookingRequestType.ViewingRequest)
        {
            bool isTenant = !string.IsNullOrEmpty(HttpContext.Session.GetString("TenantUser"));
            bool isUser = !string.IsNullOrEmpty(HttpContext.Session.GetString("NormalUser"));
            if (!isTenant && !isUser)
            {
                TempData["BookingError"] = "Vui lòng đăng nhập để gửi yêu cầu đặt lịch hoặc đặt cọc.";
                return RedirectToAction(nameof(Details), new { id = roomId });
            }

            if (string.IsNullOrWhiteSpace(bookingName) || string.IsNullOrWhiteSpace(bookingPhone))
            {
                TempData["BookingError"] = "Vui lòng điền đầy đủ họ tên và số điện thoại.";
                return RedirectToAction(nameof(Details), new { id = roomId });
            }

            string cleanPhone = bookingPhone.Trim();
            // Validate SĐT Việt Nam: 10 chữ số, đầu 03, 05, 07, 08, 09
            if (!Regex.IsMatch(cleanPhone, @"^(03|05|07|08|09)\d{8}$"))
            {
                TempData["BookingError"] = "Số điện thoại không hợp lệ. Vui lòng nhập số di động Việt Nam (10 số, bắt đầu bằng 03, 05, 07, 08, 09).";
                return RedirectToAction(nameof(Details), new { id = roomId });
            }

            // Validate ngày hẹn: không ở quá khứ
            if (!string.IsNullOrWhiteSpace(bookingDate) && DateTime.TryParse(bookingDate, out var prefDate))
            {
                if (prefDate.Date < DateTime.Today)
                {
                    TempData["BookingError"] = "Ngày hẹn xem phòng không thể ở trong quá khứ.";
                    return RedirectToAction(nameof(Details), new { id = roomId });
                }
            }

            // Chống spam: Tối đa 3 yêu cầu trong 10 phút từ cùng SĐT
            var recentCount = await _db.BookingRequests
                .Where(b => b.Phone == cleanPhone && b.CreatedAt >= DateTime.Now.AddMinutes(-10))
                .CountAsync();

            if (recentCount >= 3)
            {
                TempData["BookingError"] = "Bạn đã gửi quá nhiều yêu cầu trong thời gian ngắn. Vui lòng thử lại sau 10 phút.";
                return RedirectToAction(nameof(Details), new { id = roomId });
            }

            // Tự động gắn LandlordId của phòng
            var room = await _db.Rooms.Include(r => r.Property).FirstOrDefaultAsync(r => r.RoomId == roomId);
            int? landlordId = room?.Property?.LandlordId;

            var booking = new tblBookingRequest
            {
                RoomId = roomId,
                LandlordId = landlordId,
                FullName = bookingName.Trim(),
                Phone = cleanPhone,
                PreferredDate = bookingDate,
                Message = bookingNote?.Trim(),
                RequestType = requestType,
                Status = BookingRequestStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _db.BookingRequests.Add(booking);
            await _db.SaveChangesAsync();

            string actionLabel = requestType == BookingRequestType.Deposit ? "yêu cầu đặt cọc" : "lịch hẹn xem phòng";
            TempData["BookingSuccess"] = $"Gửi {actionLabel} thành công! Chủ trọ sẽ liên hệ lại với bạn qua số <strong>{cleanPhone}</strong>.";
            return RedirectToAction(nameof(Details), new { id = roomId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRoomReview(int roomId, string reviewName, int reviewRating, string? reviewComment)
        {
            int? tenantId = int.TryParse(HttpContext.Session.GetString("TenantUser"), out var tid) ? tid : (int?)null;
            int? userId = int.TryParse(HttpContext.Session.GetString("NormalUser"), out var uid) ? uid : (int?)null;

            if (!tenantId.HasValue && !userId.HasValue)
            {
                TempData["ReviewError"] = "Vui lòng đăng nhập để gửi đánh giá.";
                return RedirectToAction(nameof(Details), new { id = roomId });
            }

            reviewName = (reviewName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(reviewName))
            {
                TempData["ReviewError"] = "Vui lòng nhập tên của bạn.";
                return RedirectToAction(nameof(Details), new { id = roomId });
            }

            // Kiểm tra: Chỉ khách có hợp đồng thật với phòng/chủ trọ đó mới được đánh giá
            var contracts = await _db.Contracts
                .Where(c => c.RoomId == roomId && tenantId.HasValue && c.TenantId == tenantId.Value)
                .ToListAsync();

            if (!contracts.Any())
            {
                TempData["ReviewError"] = "Chỉ khách thuê đã có hợp đồng thuê phòng này mới được gửi đánh giá.";
                return RedirectToAction(nameof(Details), new { id = roomId });
            }

            // Mỗi hợp đồng một đánh giá
            var reviewedContractIds = await _db.Reviews
                .Where(r => r.ContractId.HasValue)
                .Select(r => r.ContractId!.Value)
                .ToListAsync();

            var eligibleContract = contracts.FirstOrDefault(c => !reviewedContractIds.Contains(c.ContractId));
            if (eligibleContract == null)
            {
                TempData["ReviewError"] = "Mỗi hợp đồng thuê phòng chỉ được gửi một đánh giá duy nhất.";
                return RedirectToAction(nameof(Details), new { id = roomId });
            }

            reviewRating = Math.Clamp(reviewRating, 1, 5);

            // 1. Lưu tblReview gắn ContractId
            var review = new tblReview
            {
                ContractId = eligibleContract.ContractId,
                FullName = reviewName,
                Email = "tenant@phongtromoi.vn",
                Title = $"Đánh giá phòng #{roomId}",
                Content = reviewComment?.Trim() ?? "Đánh giá chất lượng phòng trọ",
                Rating = reviewRating,
                IsApproved = true,
                CreatedAt = DateTime.Now
            };
            _db.Reviews.Add(review);

            // 2. Đồng bộ tblRoomReview để hiển thị trang chi tiết
            _db.RoomReviews.Add(new tblRoomReview
            {
                RoomId = roomId,
                TenantId = tenantId,
                UserId = userId,
                FullName = reviewName,
                Rating = reviewRating,
                Comment = reviewComment?.Trim(),
                IsApproved = true,
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            TempData["ReviewSuccess"] = "Cảm ơn bạn đã gửi đánh giá cho phòng trọ!";
            return RedirectToAction(nameof(Details), new { id = roomId });
        }
    }
}
