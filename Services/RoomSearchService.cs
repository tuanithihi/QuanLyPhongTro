using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;

using Microsoft.Extensions.Caching.Memory;

namespace QuanLyPhongTro.Services
{
    public class RoomSearchService : IRoomSearchService
    {
        private readonly DataContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RoomSearchService> _logger;

        public RoomSearchService(DataContext context, IMemoryCache cache, ILogger<RoomSearchService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<RoomSearchResultViewModel> SearchRoomsAsync(RoomFilterCriteria criteria)
        {
            criteria ??= new RoomFilterCriteria();
            if (criteria.Page < 1) criteria.Page = 1;
            if (criteria.PageSize < 1) criteria.PageSize = 12;

            // ── Base Query: BẮT BUỘC chỉ lấy phòng Available & Published ──
            var query = _context.Rooms
                .AsNoTracking()
                .Include(r => r.Property).ThenInclude(p => p!.Province)
                .Include(r => r.Property).ThenInclude(p => p!.District)
                .Include(r => r.Property).ThenInclude(p => p!.Ward)
                .Include(r => r.Property).ThenInclude(p => p!.Landlord)
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
                .Where(r => r.Status == RoomStatus.Available
                         && r.ApprovalStatus == RoomApprovalStatus.Published
                         && r.IsPublished);

            string? selectedProvinceName = null;
            string? selectedDistrictName = null;

            // ── 1. Lọc theo Tỉnh/Thành ──────────────────────────────────────
            if (criteria.ProvinceId.HasValue && criteria.ProvinceId > 0)
            {
                query = query.Where(r => r.Property != null && r.Property.ProvinceId == criteria.ProvinceId.Value);
                var prov = await _context.Provinces.FindAsync(criteria.ProvinceId.Value);
                selectedProvinceName = prov?.Name;
            }
            else if (!string.IsNullOrWhiteSpace(criteria.ProvinceSlug))
            {
                string pSlug = criteria.ProvinceSlug.Trim().ToLowerInvariant();
                var provs = await _context.Provinces.AsNoTracking().ToListAsync();
                var matchedProv = provs.FirstOrDefault(p => SlugHelper.ToSlug(p.Name) == pSlug || SlugHelper.ToSlug(p.FullName) == pSlug);
                if (matchedProv != null)
                {
                    criteria.ProvinceId = matchedProv.ProvinceId;
                    query = query.Where(r => r.Property != null && r.Property.ProvinceId == matchedProv.ProvinceId);
                    selectedProvinceName = matchedProv.Name;
                }
            }

            // ── 2. Lọc theo Quận/Huyện ──────────────────────────────────────
            if (criteria.DistrictId.HasValue && criteria.DistrictId > 0)
            {
                query = query.Where(r => r.Property != null && r.Property.DistrictId == criteria.DistrictId.Value);
                var dist = await _context.Districts.FindAsync(criteria.DistrictId.Value);
                selectedDistrictName = dist?.Name;
            }
            else if (!string.IsNullOrWhiteSpace(criteria.DistrictSlug))
            {
                string dSlug = criteria.DistrictSlug.Trim().ToLowerInvariant();
                var dists = await _context.Districts.AsNoTracking().ToListAsync();
                var matchedDist = dists.FirstOrDefault(d => SlugHelper.ToSlug(d.Name) == dSlug || SlugHelper.ToSlug(d.FullName) == dSlug);
                if (matchedDist != null)
                {
                    criteria.DistrictId = matchedDist.DistrictId;
                    query = query.Where(r => r.Property != null && r.Property.DistrictId == matchedDist.DistrictId);
                    selectedDistrictName = matchedDist.Name;
                }
            }

            // ── 3. Lọc theo Phường/Xã ───────────────────────────────────────
            if (criteria.WardId.HasValue && criteria.WardId > 0)
            {
                query = query.Where(r => r.Property != null && r.Property.WardId == criteria.WardId.Value);
            }

            // ── 4. Lọc theo Khoảng giá ─────────────────────────────────────
            if (criteria.MinPrice.HasValue && criteria.MinPrice.Value > 0)
            {
                query = query.Where(r => r.RoomPrice >= criteria.MinPrice.Value);
            }
            if (criteria.MaxPrice.HasValue && criteria.MaxPrice.Value > 0)
            {
                query = query.Where(r => r.RoomPrice <= criteria.MaxPrice.Value);
            }

            // ── 5. Lọc theo Diện tích ───────────────────────────────────────
            if (criteria.MinArea.HasValue && criteria.MinArea.Value > 0)
            {
                query = query.Where(r => r.Area >= criteria.MinArea.Value);
            }
            if (criteria.MaxArea.HasValue && criteria.MaxArea.Value > 0)
            {
                query = query.Where(r => r.Area <= criteria.MaxArea.Value);
            }

            // ── 6. Lọc theo Loại phòng ─────────────────────────────────────
            if (criteria.RoomTypeId.HasValue && criteria.RoomTypeId.Value > 0)
            {
                query = query.Where(r => r.RoomTypeId == criteria.RoomTypeId.Value);
            }

            // ── 7. Lọc theo Tiện ích ───────────────────────────────────────
            if (criteria.AmenityIds != null && criteria.AmenityIds.Any())
            {
                foreach (var amenityId in criteria.AmenityIds)
                {
                    query = query.Where(r => r.RoomAmenities.Any(ra => ra.AmenityId == amenityId));
                }
            }

            // ── 8. Lọc theo Từ khóa tìm kiếm ───────────────────────────────
            if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            {
                string kw = criteria.Keyword.Trim();
                query = query.Where(r =>
                    r.RoomName.Contains(kw) ||
                    (r.Title != null && r.Title.Contains(kw)) ||
                    r.RoomCode.Contains(kw) ||
                    (r.Address != null && r.Address.Contains(kw)) ||
                    (r.Property != null && r.Property.Name.Contains(kw)) ||
                    (r.Property != null && r.Property.Address.Contains(kw)));
            }

            // ── 9. Sắp xếp (Sort) ──────────────────────────────────────────
            query = criteria.SortBy switch
            {
                "price_asc"  => query.OrderBy(r => r.RoomPrice),
                "price_desc" => query.OrderByDescending(r => r.RoomPrice),
                "area_desc"  => query.OrderByDescending(r => r.Area),
                _            => query.OrderByDescending(r => r.PublishedAt ?? r.CreatedAt)
            };

            // ── 10. Phân trang phía Server (IQueryable.Skip/Take) ──────────
            int totalItems = await query.CountAsync();
            var rawRooms = await query
                .Skip((criteria.Page - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToListAsync();

            var items = rawRooms.Select(r => MapToCard(r)).ToList();

            // Danh mục dữ liệu cho bộ lọc
            var provinces = await _context.Provinces.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
            var districts = criteria.ProvinceId.HasValue
                ? await _context.Districts.AsNoTracking().Where(d => d.ProvinceId == criteria.ProvinceId.Value).OrderBy(d => d.Name).ToListAsync()
                : new List<tblDistrict>();
            var wards = criteria.DistrictId.HasValue
                ? await _context.Wards.AsNoTracking().Where(w => w.DistrictId == criteria.DistrictId.Value).OrderBy(w => w.Name).ToListAsync()
                : new List<tblWard>();
            var roomTypes = await _context.RoomTypes.AsNoTracking().Where(rt => rt.IsActive).OrderBy(rt => rt.SortOrder).ToListAsync();
            var amenities = await _context.Amenities.AsNoTracking().Where(a => a.IsActive).OrderBy(a => a.Name).ToListAsync();

            return new RoomSearchResultViewModel
            {
                Items = items,
                TotalItems = totalItems,
                Page = criteria.Page,
                PageSize = criteria.PageSize,
                Criteria = criteria,
                AvailableProvinces = provinces,
                AvailableDistricts = districts,
                AvailableWards = wards,
                AvailableRoomTypes = roomTypes,
                AvailableAmenities = amenities,
                SelectedProvinceName = selectedProvinceName,
                SelectedDistrictName = selectedDistrictName
            };
        }

        public async Task<tblRoom?> GetRoomDetailAsync(int roomId, string? slug = null)
        {
            var query = _context.Rooms
                .AsNoTracking()
                .Include(r => r.Property).ThenInclude(p => p!.Province)
                .Include(r => r.Property).ThenInclude(p => p!.District)
                .Include(r => r.Property).ThenInclude(p => p!.Ward)
                .Include(r => r.Property).ThenInclude(p => p!.Landlord)
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages.OrderBy(i => i.SortOrder))
                .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
                .Where(r => r.RoomId == roomId
                         && r.Status == RoomStatus.Available
                         && r.ApprovalStatus == RoomApprovalStatus.Published
                         && r.IsPublished);

            var room = await query.FirstOrDefaultAsync();
            if (room == null) return null;

            if (!string.IsNullOrWhiteSpace(slug) && !string.Equals(room.Slug, slug, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return room;
        }

        public async Task<LandlordPublicProfileViewModel?> GetLandlordPublicProfileAsync(string slugOrId)
        {
            tblLandlord? landlord = null;

            if (int.TryParse(slugOrId, out int landlordId))
            {
                landlord = await _context.Landlords
                    .AsNoTracking()
                    .Include(l => l.Properties)
                    .FirstOrDefaultAsync(l => l.LandlordId == landlordId && l.Status == LandlordStatus.Approved);
            }

            if (landlord == null)
            {
                var landlords = await _context.Landlords
                    .AsNoTracking()
                    .Include(l => l.Properties)
                    .Where(l => l.Status == LandlordStatus.Approved)
                    .ToListAsync();

                string targetSlug = slugOrId.Trim().ToLowerInvariant();
                landlord = landlords.FirstOrDefault(l =>
                    SlugHelper.ToSlug(l.FullName) == targetSlug ||
                    $"{SlugHelper.ToSlug(l.FullName)}-{l.LandlordId}" == targetSlug);
            }

            if (landlord == null) return null;

            var activeRoomsQuery = _context.Rooms
                .AsNoTracking()
                .Include(r => r.Property).ThenInclude(p => p!.Province)
                .Include(r => r.Property).ThenInclude(p => p!.District)
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .Where(r => r.Property != null
                         && r.Property.LandlordId == landlord.LandlordId
                         && r.Status == RoomStatus.Available
                         && r.ApprovalStatus == RoomApprovalStatus.Published
                         && r.IsPublished)
                .OrderByDescending(r => r.PublishedAt ?? r.CreatedAt);

            int totalRooms = await _context.Rooms.CountAsync(r => r.Property != null && r.Property.LandlordId == landlord.LandlordId);
            var activeRooms = await activeRoomsQuery.ToListAsync();

            return new LandlordPublicProfileViewModel
            {
                Landlord = landlord,
                TotalRooms = totalRooms,
                TotalProperties = landlord.Properties.Count,
                ActiveListings = activeRooms.Select(r => MapToCard(r)).ToList()
            };
        }

        public async Task<List<RoomCardViewModel>> GetFeaturedRoomsAsync(int count = 6)
        {
            var rooms = await _context.Rooms
                .AsNoTracking()
                .Include(r => r.Property).ThenInclude(p => p!.Province)
                .Include(r => r.Property).ThenInclude(p => p!.District)
                .Include(r => r.Property).ThenInclude(p => p!.Landlord)
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
                .Where(r => r.Status == RoomStatus.Available
                         && r.ApprovalStatus == RoomApprovalStatus.Published
                         && r.IsPublished
                         && r.IsFeatured)
                .OrderByDescending(r => r.PublishedAt ?? r.CreatedAt)
                .Take(count)
                .ToListAsync();

            if (rooms.Count < count)
            {
                var additional = await _context.Rooms
                    .AsNoTracking()
                    .Include(r => r.Property).ThenInclude(p => p!.Province)
                    .Include(r => r.Property).ThenInclude(p => p!.District)
                    .Include(r => r.Property).ThenInclude(p => p!.Landlord)
                    .Include(r => r.RoomType)
                    .Include(r => r.RoomImages)
                    .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
                    .Where(r => r.Status == RoomStatus.Available
                             && r.ApprovalStatus == RoomApprovalStatus.Published
                             && r.IsPublished
                             && !r.IsFeatured)
                    .OrderByDescending(r => r.PublishedAt ?? r.CreatedAt)
                    .Take(count - rooms.Count)
                    .ToListAsync();

                rooms.AddRange(additional);
            }

            return rooms.Select(r => MapToCard(r)).ToList();
        }

        public async Task<List<RoomCardViewModel>> GetRecentRoomsAsync(int count = 6)
        {
            var rooms = await _context.Rooms
                .AsNoTracking()
                .Include(r => r.Property).ThenInclude(p => p!.Province)
                .Include(r => r.Property).ThenInclude(p => p!.District)
                .Include(r => r.Property).ThenInclude(p => p!.Landlord)
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
                .Where(r => r.Status == RoomStatus.Available
                         && r.ApprovalStatus == RoomApprovalStatus.Published
                         && r.IsPublished)
                .OrderByDescending(r => r.PublishedAt ?? r.CreatedAt)
                .Take(count)
                .ToListAsync();

            return rooms.Select(r => MapToCard(r)).ToList();
        }

        public async Task<List<tblLandlord>> GetFeaturedLandlordsAsync(int count = 4)
        {
            return await _context.Landlords
                .AsNoTracking()
                .Include(l => l.Properties)
                .Where(l => l.Status == LandlordStatus.Approved)
                .OrderByDescending(l => l.Properties.Count)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<tblProvince>> GetPopularProvincesAsync(int count = 6)
        {
            string cacheKey = $"popular_provinces_{count}";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(30);
                return await _context.Provinces
                    .AsNoTracking()
                    .Include(p => p.Properties)
                    .OrderByDescending(p => p.Properties.Count)
                    .Take(count)
                    .ToListAsync();
            }) ?? new List<tblProvince>();
        }

        public async Task<List<tblRoomType>> GetActiveRoomTypesAsync()
        {
            const string cacheKey = "active_room_types";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(30);
                return await _context.RoomTypes
                    .AsNoTracking()
                    .Where(rt => rt.IsActive)
                    .OrderBy(rt => rt.SortOrder)
                    .ToListAsync();
            }) ?? new List<tblRoomType>();
        }

        public async Task<List<tblAmenity>> GetAllAmenitiesAsync()
        {
            const string cacheKey = "all_amenities";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(30);
                return await _context.Amenities
                    .AsNoTracking()
                    .Where(a => a.IsActive)
                    .OrderBy(a => a.Name)
                    .ToListAsync();
            }) ?? new List<tblAmenity>();
        }

        public async Task<List<tblDistrict>> GetDistrictsByProvinceAsync(int provinceId)
        {
            string cacheKey = $"districts_prov_{provinceId}";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(30);
                return await _context.Districts
                    .AsNoTracking()
                    .Where(d => d.ProvinceId == provinceId)
                    .OrderBy(d => d.Name)
                    .ToListAsync();
            }) ?? new List<tblDistrict>();
        }

        public async Task<List<tblWard>> GetWardsByDistrictAsync(int districtId)
        {
            string cacheKey = $"wards_dist_{districtId}";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(30);
                return await _context.Wards
                    .AsNoTracking()
                    .Where(w => w.DistrictId == districtId)
                    .OrderBy(w => w.Name)
                    .ToListAsync();
            }) ?? new List<tblWard>();
        }

        // ── Helper Mapping ───────────────────────────────────────────────
        private static RoomCardViewModel MapToCard(tblRoom r)
        {
            var primaryImage = r.RoomImages?.FirstOrDefault(i => i.IsPrimary)?.Url
                            ?? r.RoomImages?.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url
                            ?? r.ThumbnailImage;

            string districtName = r.Property?.District?.Name ?? string.Empty;
            string provinceName = r.Property?.Province?.Name ?? string.Empty;
            string fullAddress = r.Property?.Address ?? r.Address ?? string.Empty;

            string landlordName = r.Property?.Landlord?.FullName ?? "Chủ trọ";
            string landlordSlug = SlugHelper.ToSlug(landlordName);
            int landlordId = r.Property?.LandlordId ?? 0;

            var amenities = r.RoomAmenities?
                .Select(ra => ra.Amenity?.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Take(3)
                .ToList() ?? new List<string?>();

            return new RoomCardViewModel
            {
                RoomId = r.RoomId,
                Title = !string.IsNullOrWhiteSpace(r.Title) ? r.Title : r.RoomName,
                RoomName = r.RoomName,
                RoomCode = r.RoomCode,
                Slug = r.Slug,
                RoomPrice = r.RoomPrice,
                FormattedPrice = DisplayHelper.FormatPrice(r.RoomPrice),
                FormattedPriceCompact = DisplayHelper.FormatPriceCompact(r.RoomPrice),
                Area = r.Area,
                FormattedArea = DisplayHelper.FormatArea(r.Area),
                Floor = r.Floor,
                Capacity = r.Capacity > 0 ? r.Capacity : r.MaxOccupants,
                Status = r.Status,
                ApprovalStatus = r.ApprovalStatus,
                IsFeatured = r.IsFeatured,
                PublishedAt = r.PublishedAt,
                CreatedAt = r.CreatedAt,
                ThumbnailUrl = primaryImage,
                ImageCount = r.RoomImages?.Count ?? (string.IsNullOrEmpty(primaryImage) ? 0 : 1),
                PropertyId = r.PropertyId ?? 0,
                PropertyName = r.Property?.Name ?? string.Empty,
                FullAddress = fullAddress,
                ShortAddress = DisplayHelper.ShortenAddress(fullAddress, districtName, provinceName),
                Latitude = r.Latitude ?? r.Property?.Latitude,
                Longitude = r.Longitude ?? r.Property?.Longitude,
                LandlordId = landlordId,
                LandlordName = landlordName,
                LandlordAvatar = r.Property?.Landlord?.Avatar,
                LandlordPhone = r.Property?.Landlord?.Phone,
                LandlordSlug = landlordSlug,
                AmenityNames = amenities.Where(a => a != null).Select(a => a!).ToList(),
                IsFavorite = false
            };
        }
    }
}
