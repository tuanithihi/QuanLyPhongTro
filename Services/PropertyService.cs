using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly DataContext _context;
        private readonly ILogger<PropertyService> _logger;

        public PropertyService(DataContext context, ILogger<PropertyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<PropertyListItemViewModel>> GetPropertiesByLandlordAsync(int? landlordId, string? search = null)
        {
            var query = _context.Properties
                .Include(p => p.Province)
                .Include(p => p.District)
                .Include(p => p.Ward)
                .Include(p => p.Rooms)
                .AsQueryable();

            if (landlordId.HasValue)
            {
                query = query.Where(p => p.LandlordId == landlordId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(p => p.Name.Contains(s) || p.Address.Contains(s));
            }

            var properties = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return properties.Select(p => new PropertyListItemViewModel
            {
                PropertyId = p.PropertyId,
                Name = p.Name,
                Slug = p.Slug,
                Address = p.Address,
                ProvinceName = p.Province?.Name,
                DistrictName = p.District?.Name,
                WardName = p.Ward?.Name,
                TotalRooms = p.Rooms.Count,
                AvailableRooms = p.Rooms.Count(r => r.Status == RoomStatus.Available),
                OccupiedRooms = p.Rooms.Count(r => r.Status == RoomStatus.Occupied),
                Status = p.Status,
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        public async Task<PropertyDetailViewModel?> GetPropertyDetailAsync(int propertyId, int? landlordId = null)
        {
            var query = _context.Properties
                .Include(p => p.Province)
                .Include(p => p.District)
                .Include(p => p.Ward)
                .Include(p => p.Landlord)
                .Include(p => p.Rooms).ThenInclude(r => r.RoomType)
                .Include(p => p.Rooms).ThenInclude(r => r.RoomImages)
                .AsQueryable();

            if (landlordId.HasValue)
            {
                query = query.Where(p => p.LandlordId == landlordId.Value);
            }

            var property = await query.FirstOrDefaultAsync(p => p.PropertyId == propertyId);
            if (property == null) return null;

            return new PropertyDetailViewModel
            {
                Property = property,
                Rooms = property.Rooms.OrderBy(r => r.RoomCode).ToList(),
                TotalRooms = property.Rooms.Count,
                AvailableRooms = property.Rooms.Count(r => r.Status == RoomStatus.Available),
                OccupiedRooms = property.Rooms.Count(r => r.Status == RoomStatus.Occupied)
            };
        }

        public async Task<PropertyCreateEditViewModel?> GetPropertyForEditAsync(int propertyId, int? landlordId = null)
        {
            var query = _context.Properties.AsQueryable();
            if (landlordId.HasValue)
            {
                query = query.Where(p => p.LandlordId == landlordId.Value);
            }

            var property = await query.FirstOrDefaultAsync(p => p.PropertyId == propertyId);
            if (property == null) return null;

            return new PropertyCreateEditViewModel
            {
                PropertyId = property.PropertyId,
                LandlordId = property.LandlordId,
                Name = property.Name,
                Slug = property.Slug,
                Address = property.Address,
                ProvinceId = property.ProvinceId,
                DistrictId = property.DistrictId,
                WardId = property.WardId,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                Description = property.Description,
                Status = property.Status
            };
        }

        public async Task<(bool Success, string? Error, int PropertyId)> CreatePropertyAsync(PropertyCreateEditViewModel model, int landlordId)
        {
            try
            {
                string baseSlug = string.IsNullOrWhiteSpace(model.Slug)
                    ? GenerateSlug(model.Name)
                    : GenerateSlug(model.Slug);

                string uniqueSlug = await EnsureUniqueSlugAsync(baseSlug);

                var property = new tblProperty
                {
                    LandlordId = landlordId,
                    Name = model.Name.Trim(),
                    Slug = uniqueSlug,
                    Address = model.Address.Trim(),
                    ProvinceId = model.ProvinceId,
                    DistrictId = model.DistrictId,
                    WardId = model.WardId,
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    Description = model.Description?.Trim(),
                    Status = model.Status,
                    CreatedAt = DateTime.Now
                };

                _context.Properties.Add(property);
                await _context.SaveChangesAsync();

                return (true, null, property.PropertyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo khu trọ.");
                return (false, "Đã xảy ra lỗi khi lưu khu trọ vào hệ thống.", 0);
            }
        }

        public async Task<(bool Success, string? Error)> UpdatePropertyAsync(PropertyCreateEditViewModel model, int? landlordId = null)
        {
            try
            {
                var query = _context.Properties.AsQueryable();
                if (landlordId.HasValue)
                {
                    query = query.Where(p => p.LandlordId == landlordId.Value);
                }

                var property = await query.FirstOrDefaultAsync(p => p.PropertyId == model.PropertyId);
                if (property == null)
                {
                    return (false, "Không tìm thấy khu trọ hoặc bạn không có quyền chỉnh sửa.");
                }

                string baseSlug = string.IsNullOrWhiteSpace(model.Slug)
                    ? GenerateSlug(model.Name)
                    : GenerateSlug(model.Slug);

                string uniqueSlug = await EnsureUniqueSlugAsync(baseSlug, model.PropertyId);

                property.Name = model.Name.Trim();
                property.Slug = uniqueSlug;
                property.Address = model.Address.Trim();
                property.ProvinceId = model.ProvinceId;
                property.DistrictId = model.DistrictId;
                property.WardId = model.WardId;
                property.Latitude = model.Latitude;
                property.Longitude = model.Longitude;
                property.Description = model.Description?.Trim();
                property.Status = model.Status;
                property.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật khu trọ {PropertyId}.", model.PropertyId);
                return (false, "Đã xảy ra lỗi khi cập nhật khu trọ.");
            }
        }

        public async Task<(bool Success, string? Error)> DeletePropertyAsync(int propertyId, int? landlordId = null)
        {
            var query = _context.Properties
                .Include(p => p.Rooms)
                .AsQueryable();

            if (landlordId.HasValue)
            {
                query = query.Where(p => p.LandlordId == landlordId.Value);
            }

            var property = await query.FirstOrDefaultAsync(p => p.PropertyId == propertyId);
            if (property == null)
            {
                return (false, "Không tìm thấy khu trọ hoặc bạn không có quyền xóa.");
            }

            if (property.Rooms.Any())
            {
                return (false, $"Khu trọ đang có {property.Rooms.Count} phòng. Vui lòng chuyển hoặc xóa các phòng trước khi xóa khu trọ.");
            }

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<tblProvince>> GetProvincesAsync()
        {
            return await _context.Provinces
                .OrderBy(p => p.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<tblDistrict>> GetDistrictsAsync(int provinceId)
        {
            return await _context.Districts
                .Where(d => d.ProvinceId == provinceId)
                .OrderBy(d => d.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<tblWard>> GetWardsAsync(int districtId)
        {
            return await _context.Wards
                .Where(w => w.DistrictId == districtId)
                .OrderBy(w => w.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        // ── Helper Slug ──────────────────────────────────────────────────
        private static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("N")[..8];

            string str = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in str)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            string clean = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            clean = clean.Replace('đ', 'd').Replace('Đ', 'd');
            clean = Regex.Replace(clean, @"[^a-z0-9\s-]", "");
            clean = Regex.Replace(clean, @"\s+", "-").Trim('-');

            return string.IsNullOrEmpty(clean) ? Guid.NewGuid().ToString("N")[..8] : clean;
        }

        private async Task<string> EnsureUniqueSlugAsync(string baseSlug, int? excludePropertyId = null)
        {
            string slug = baseSlug;
            int counter = 1;

            while (await _context.Properties.AnyAsync(p => p.Slug == slug && (!excludePropertyId.HasValue || p.PropertyId != excludePropertyId.Value)))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }
    }
}
