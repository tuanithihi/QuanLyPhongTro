using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services
{
    public class RoomService : IRoomService
    {
        private readonly DataContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly IConfiguration _config;
        private readonly ILogger<RoomService> _logger;

        public RoomService(
            DataContext context,
            IFileUploadService fileUploadService,
            IConfiguration config,
            ILogger<RoomService> logger)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _config = config;
            _logger = logger;
        }

        public async Task<List<RoomListItemViewModel>> GetRoomsByLandlordAsync(
            int? landlordId,
            int? propertyId = null,
            string? search = null,
            RoomStatus? status = null,
            RoomApprovalStatus? approvalStatus = null)
        {
            var query = _context.Rooms
                .Include(r => r.Property)
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .AsQueryable();

            if (landlordId.HasValue)
            {
                query = query.Where(r => r.Property != null && r.Property.LandlordId == landlordId.Value);
            }

            if (propertyId.HasValue)
            {
                query = query.Where(r => r.PropertyId == propertyId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (approvalStatus.HasValue)
            {
                query = query.Where(r => r.ApprovalStatus == approvalStatus.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(r => r.RoomCode.Contains(s)
                                      || r.RoomName.Contains(s)
                                      || (r.Title != null && r.Title.Contains(s))
                                      || (r.Property != null && r.Property.Name.Contains(s)));
            }

            var rooms = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return rooms.Select(r => new RoomListItemViewModel
            {
                RoomId = r.RoomId,
                RoomCode = r.RoomCode,
                RoomName = r.RoomName,
                PropertyName = r.Property?.Name,
                RoomTypeName = r.RoomType?.RoomTypeName,
                RoomPrice = r.RoomPrice,
                Area = r.Area,
                Status = r.Status,
                IsPublished = r.IsPublished,
                ApprovalStatus = r.ApprovalStatus,
                IsFeatured = r.IsFeatured,
                ThumbnailImage = r.ThumbnailImage ?? r.RoomImages.FirstOrDefault(i => i.IsPrimary)?.Url ?? r.RoomImages.FirstOrDefault()?.Url,
                ImageCount = r.RoomImages.Count,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<tblRoom?> GetRoomByIdAsync(int roomId, int? landlordId = null)
        {
            var query = _context.Rooms
                .Include(r => r.Property).ThenInclude(p => p!.Landlord)
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages.OrderBy(i => i.SortOrder))
                .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
                .AsQueryable();

            if (landlordId.HasValue)
            {
                query = query.Where(r => r.Property != null && r.Property.LandlordId == landlordId.Value);
            }

            return await query.FirstOrDefaultAsync(r => r.RoomId == roomId);
        }

        public async Task<RoomCreateEditViewModel?> GetRoomForEditAsync(int roomId, int? landlordId = null)
        {
            var room = await GetRoomByIdAsync(roomId, landlordId);
            if (room == null) return null;

            return new RoomCreateEditViewModel
            {
                RoomId = room.RoomId,
                PropertyId = room.PropertyId ?? 0,
                RoomCode = room.RoomCode,
                RoomName = room.RoomName,
                Title = room.Title,
                Slug = room.Slug,
                RoomTypeId = room.RoomTypeId,
                RoomPrice = room.RoomPrice,
                DefaultDeposit = room.DefaultDeposit,
                Area = room.Area,
                Floor = room.Floor,
                MaxOccupants = room.MaxOccupants,
                Capacity = room.Capacity,
                Description = room.Description,
                Address = room.Address ?? room.Property?.Address,
                Latitude = room.Latitude ?? room.Property?.Latitude,
                Longitude = room.Longitude ?? room.Property?.Longitude,
                Status = room.Status,
                IsPublished = room.IsPublished,
                IsFeatured = room.IsFeatured,
                ApprovalStatus = room.ApprovalStatus,
                SelectedAmenityIds = room.RoomAmenities.Select(a => a.AmenityId).ToList(),
                ExistingImages = room.RoomImages.OrderBy(i => i.SortOrder).Select(i => new RoomImageItemViewModel
                {
                    ImageId = i.ImageId,
                    Url = i.Url,
                    IsPrimary = i.IsPrimary,
                    SortOrder = i.SortOrder
                }).ToList(),
                PrimaryImageId = room.RoomImages.FirstOrDefault(i => i.IsPrimary)?.ImageId
            };
        }

        public async Task<(bool Success, string? Error, int RoomId)> CreateRoomAsync(RoomCreateEditViewModel model, int landlordId)
        {
            try
            {
                // Kiểm tra Property có thuộc về landlord này không
                var property = await _context.Properties.FirstOrDefaultAsync(p => p.PropertyId == model.PropertyId && p.LandlordId == landlordId);
                if (property == null)
                {
                    return (false, "Khu trọ không hợp lệ hoặc bạn không có quyền sở hữu.", 0);
                }

                // Kiểm tra trùng RoomCode trong cùng Property
                bool codeExists = await _context.Rooms.AnyAsync(r => r.PropertyId == model.PropertyId && r.RoomCode == model.RoomCode.Trim());
                if (codeExists)
                {
                    return (false, $"Mã phòng '{model.RoomCode.Trim()}' đã tồn tại trong khu trọ này.", 0);
                }

                // Tự động duyệt dựa trên cấu hình ListingSettings:AutoApprove
                bool autoApprove = false;
                if (_config != null && bool.TryParse(_config["ListingSettings:AutoApprove"], out bool parsedAuto))
                {
                    autoApprove = parsedAuto;
                }
                var approvalStatus = autoApprove ? RoomApprovalStatus.Published : RoomApprovalStatus.Pending;
                DateTime? publishedAt = autoApprove ? DateTime.Now : null;

                string baseSlug = string.IsNullOrWhiteSpace(model.Slug)
                    ? GenerateSlug($"{model.RoomCode}-{model.RoomName}")
                    : GenerateSlug(model.Slug);
                string uniqueSlug = await EnsureUniqueSlugAsync(baseSlug);

                var room = new tblRoom
                {
                    PropertyId = model.PropertyId,
                    RoomCode = model.RoomCode.Trim(),
                    RoomName = model.RoomName.Trim(),
                    Title = string.IsNullOrWhiteSpace(model.Title) ? $"{model.RoomName} - {property.Name}" : model.Title.Trim(),
                    Slug = uniqueSlug,
                    RoomTypeId = model.RoomTypeId,
                    RoomPrice = model.RoomPrice,
                    DefaultDeposit = model.DefaultDeposit,
                    Area = model.Area,
                    Floor = model.Floor,
                    MaxOccupants = model.MaxOccupants,
                    Capacity = model.Capacity,
                    Description = HtmlSanitizerHelper.Sanitize(model.Description),
                    Address = string.IsNullOrWhiteSpace(model.Address) ? property.Address : model.Address.Trim(),
                    Latitude = model.Latitude ?? property.Latitude,
                    Longitude = model.Longitude ?? property.Longitude,
                    Status = model.Status,
                    IsPublished = model.IsPublished,
                    IsFeatured = model.IsFeatured,
                    ApprovalStatus = approvalStatus,
                    PublishedAt = publishedAt,
                    CreatedAt = DateTime.Now
                };

                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();

                // Lưu Tiện ích
                if (model.SelectedAmenityIds != null && model.SelectedAmenityIds.Count > 0)
                {
                    foreach (var amenityId in model.SelectedAmenityIds.Distinct())
                    {
                        _context.RoomAmenities.Add(new tblRoomAmenity
                        {
                            RoomId = room.RoomId,
                            AmenityId = amenityId
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // Xử lý upload nhiều ảnh (kèm tạo thumbnail)
                if (model.NewPhotos != null && model.NewPhotos.Count > 0)
                {
                    int sortOrder = 0;
                    string? firstThumbnail = null;

                    foreach (var file in model.NewPhotos)
                    {
                        if (file.Length == 0) continue;

                        var (isValid, _, relativePath, thumbRelativePath) = await _fileUploadService.UploadImageWithThumbnailAsync(
                            file, "images/rooms", 400, 300);

                        if (isValid && !string.IsNullOrEmpty(relativePath))
                        {
                            bool isPrimary = (sortOrder == 0);
                            _context.RoomImages.Add(new tblRoomImage
                            {
                                RoomId = room.RoomId,
                                Url = relativePath,
                                IsPrimary = isPrimary,
                                SortOrder = sortOrder,
                                CreatedAt = DateTime.Now
                            });

                            if (isPrimary)
                            {
                                firstThumbnail = thumbRelativePath ?? relativePath;
                            }

                            sortOrder++;
                        }
                    }

                    if (!string.IsNullOrEmpty(firstThumbnail))
                    {
                        room.ThumbnailImage = firstThumbnail;
                    }

                    await _context.SaveChangesAsync();
                }

                return (true, null, room.RoomId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo phòng mới.");
                return (false, $"Đã xảy ra lỗi khi tạo phòng mới: {ex.Message} {(ex.InnerException != null ? " -> " + ex.InnerException.Message : "")}", 0);
            }
        }

        public async Task<(bool Success, string? Error)> UpdateRoomAsync(RoomCreateEditViewModel model, int? landlordId = null)
        {
            try
            {
                var query = _context.Rooms
                    .Include(r => r.Property)
                    .Include(r => r.RoomImages)
                    .Include(r => r.RoomAmenities)
                    .AsQueryable();

                if (landlordId.HasValue)
                {
                    query = query.Where(r => r.Property != null && r.Property.LandlordId == landlordId.Value);
                }

                var room = await query.FirstOrDefaultAsync(r => r.RoomId == model.RoomId);
                if (room == null)
                {
                    return (false, "Không tìm thấy phòng hoặc bạn không có quyền chỉnh sửa.");
                }

                // Kiểm tra Property mới (nếu đổi khu trọ)
                if (model.PropertyId != room.PropertyId)
                {
                    if (landlordId.HasValue)
                    {
                        bool validProperty = await _context.Properties.AnyAsync(p => p.PropertyId == model.PropertyId && p.LandlordId == landlordId.Value);
                        if (!validProperty)
                        {
                            return (false, "Khu trọ được chọn không thuộc quyền quản lý của bạn.");
                        }
                    }
                    room.PropertyId = model.PropertyId;
                }

                // Kiểm tra trùng RoomCode
                bool codeExists = await _context.Rooms.AnyAsync(r => r.PropertyId == model.PropertyId && r.RoomCode == model.RoomCode.Trim() && r.RoomId != model.RoomId);
                if (codeExists)
                {
                    return (false, $"Mã phòng '{model.RoomCode.Trim()}' đã tồn tại trong khu trọ này.");
                }

                string baseSlug = string.IsNullOrWhiteSpace(model.Slug)
                    ? GenerateSlug($"{model.RoomCode}-{model.RoomName}")
                    : GenerateSlug(model.Slug);
                string uniqueSlug = await EnsureUniqueSlugAsync(baseSlug, model.RoomId);

                room.RoomCode = model.RoomCode.Trim();
                room.RoomName = model.RoomName.Trim();
                room.Title = model.Title?.Trim();
                room.Slug = uniqueSlug;
                room.RoomTypeId = model.RoomTypeId;
                room.RoomPrice = model.RoomPrice;
                room.DefaultDeposit = model.DefaultDeposit;
                room.Area = model.Area;
                room.Floor = model.Floor;
                room.MaxOccupants = model.MaxOccupants;
                room.Capacity = model.Capacity;
                room.Description = HtmlSanitizerHelper.Sanitize(model.Description);
                room.Address = model.Address?.Trim();
                room.Latitude = model.Latitude;
                room.Longitude = model.Longitude;
                room.Status = model.Status;
                room.IsPublished = model.IsPublished;
                room.IsFeatured = model.IsFeatured;
                room.UpdatedAt = DateTime.Now;

                // Cập nhật tiện ích
                _context.RoomAmenities.RemoveRange(room.RoomAmenities);
                if (model.SelectedAmenityIds != null && model.SelectedAmenityIds.Count > 0)
                {
                    foreach (var amenityId in model.SelectedAmenityIds.Distinct())
                    {
                        _context.RoomAmenities.Add(new tblRoomAmenity
                        {
                            RoomId = room.RoomId,
                            AmenityId = amenityId
                        });
                    }
                }

                // Cập nhật ảnh đại diện nếu chọn từ ExistingImages
                if (model.PrimaryImageId.HasValue)
                {
                    foreach (var img in room.RoomImages)
                    {
                        bool isPri = (img.ImageId == model.PrimaryImageId.Value);
                        img.IsPrimary = isPri;
                        if (isPri)
                        {
                            room.ThumbnailImage = img.Url;
                        }
                    }
                }

                // Upload thêm ảnh mới nếu có
                if (model.NewPhotos != null && model.NewPhotos.Count > 0)
                {
                    int currentMaxSort = room.RoomImages.Any() ? room.RoomImages.Max(i => i.SortOrder) : 0;
                    bool hasPrimary = room.RoomImages.Any(i => i.IsPrimary);

                    foreach (var file in model.NewPhotos)
                    {
                        if (file.Length == 0) continue;

                        var (isValid, _, relativePath, thumbRelativePath) = await _fileUploadService.UploadImageWithThumbnailAsync(
                            file, "images/rooms", 400, 300);

                        if (isValid && !string.IsNullOrEmpty(relativePath))
                        {
                            currentMaxSort++;
                            bool isPrimary = !hasPrimary;
                            _context.RoomImages.Add(new tblRoomImage
                            {
                                RoomId = room.RoomId,
                                Url = relativePath,
                                IsPrimary = isPrimary,
                                SortOrder = currentMaxSort,
                                CreatedAt = DateTime.Now
                            });

                            if (isPrimary)
                            {
                                hasPrimary = true;
                                room.ThumbnailImage = thumbRelativePath ?? relativePath;
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật phòng {RoomId}.", model.RoomId);
                return (false, "Đã xảy ra lỗi khi cập nhật phòng.");
            }
        }

        public async Task<(bool Success, string? Error)> DeleteRoomAsync(int roomId, int? landlordId = null)
        {
            var query = _context.Rooms
                .Include(r => r.Property)
                .Include(r => r.RoomImages)
                .Include(r => r.RoomAmenities)
                .Include(r => r.Contracts)
                .AsQueryable();

            if (landlordId.HasValue)
            {
                query = query.Where(r => r.Property != null && r.Property.LandlordId == landlordId.Value);
            }

            var room = await query.FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null)
            {
                return (false, "Không tìm thấy phòng hoặc bạn không có quyền xóa.");
            }

            // Kiểm tra hợp đồng đang hiệu lực
            if (room.Contracts.Any(c => c.Status == ContractStatus.Active))
            {
                return (false, "Phòng này đang có hợp đồng thuê hiệu lực. Không thể xóa.");
            }

            // Xóa file ảnh vật lý (bao gồm cả thumbnail)
            foreach (var img in room.RoomImages)
            {
                _fileUploadService.DeleteFile(img.Url);
            }

            if (!string.IsNullOrEmpty(room.ThumbnailImage))
            {
                _fileUploadService.DeleteFile(room.ThumbnailImage);
            }

            _context.RoomAmenities.RemoveRange(room.RoomAmenities);
            _context.RoomImages.RemoveRange(room.RoomImages);
            _context.Rooms.Remove(room);

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> PublishListingAsync(int roomId, int? landlordId = null)
        {
            var room = await GetRoomEntityAsync(roomId, landlordId);
            if (room == null) return (false, "Không tìm thấy phòng hoặc không có quyền.");

            bool autoApprove = false;
            if (_config != null && bool.TryParse(_config["ListingSettings:AutoApprove"], out bool parsedAuto))
            {
                autoApprove = parsedAuto;
            }
            room.IsPublished = true;
            if (room.ApprovalStatus == RoomApprovalStatus.Draft || autoApprove)
            {
                room.ApprovalStatus = autoApprove ? RoomApprovalStatus.Published : RoomApprovalStatus.Pending;
                if (autoApprove) room.PublishedAt = DateTime.Now;
            }
            room.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> HideListingAsync(int roomId, int? landlordId = null)
        {
            var room = await GetRoomEntityAsync(roomId, landlordId);
            if (room == null) return (false, "Không tìm thấy phòng hoặc không có quyền.");

            room.IsPublished = false;
            room.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ApproveListingAsync(int roomId)
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null) return (false, "Không tìm thấy phòng.");

            room.ApprovalStatus = RoomApprovalStatus.Published;
            room.IsPublished = true;
            room.PublishedAt = DateTime.Now;
            room.RejectReason = null;
            room.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> RejectListingAsync(int roomId, string? reason)
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null) return (false, "Không tìm thấy phòng.");

            room.ApprovalStatus = RoomApprovalStatus.Rejected;
            room.IsPublished = false;
            room.RejectReason = string.IsNullOrWhiteSpace(reason) ? "Không đạt tiêu chuẩn kiểm duyệt tin đăng." : reason.Trim();
            room.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UnpublishListingAsync(int roomId, string? reason, int? landlordId = null)
        {
            var room = await GetRoomEntityAsync(roomId, landlordId);
            if (room == null) return (false, "Không tìm thấy phòng hoặc không có quyền.");

            room.IsPublished = false;
            room.ApprovalStatus = RoomApprovalStatus.Draft;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                room.RejectReason = reason.Trim();
            }
            room.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> MarkAsOccupiedAsync(int roomId, int? landlordId = null)
        {
            var room = await GetRoomEntityAsync(roomId, landlordId);
            if (room == null) return (false, "Không tìm thấy phòng hoặc không có quyền.");

            room.Status = RoomStatus.Occupied;
            room.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> MarkAsAvailableAsync(int roomId, int? landlordId = null)
        {
            var room = await GetRoomEntityAsync(roomId, landlordId);
            if (room == null) return (false, "Không tìm thấy phòng hoặc không có quyền.");

            room.Status = RoomStatus.Available;
            room.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> SetPrimaryImageAsync(int roomId, int imageId, int? landlordId = null)
        {
            var room = await _context.Rooms
                .Include(r => r.Property)
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.RoomId == roomId && (!landlordId.HasValue || (r.Property != null && r.Property.LandlordId == landlordId.Value)));

            if (room == null) return (false, "Không tìm thấy phòng hoặc không có quyền.");

            var targetImg = room.RoomImages.FirstOrDefault(i => i.ImageId == imageId);
            if (targetImg == null) return (false, "Ảnh không tồn tại trong phòng này.");

            foreach (var img in room.RoomImages)
            {
                img.IsPrimary = (img.ImageId == imageId);
            }

            room.ThumbnailImage = targetImg.Url;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeleteImageAsync(int roomId, int imageId, int? landlordId = null)
        {
            var room = await _context.Rooms
                .Include(r => r.Property)
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.RoomId == roomId && (!landlordId.HasValue || (r.Property != null && r.Property.LandlordId == landlordId.Value)));

            if (room == null) return (false, "Không tìm thấy phòng hoặc không có quyền.");

            var targetImg = room.RoomImages.FirstOrDefault(i => i.ImageId == imageId);
            if (targetImg == null) return (false, "Ảnh không tồn tại trong phòng này.");

            bool wasPrimary = targetImg.IsPrimary;
            _fileUploadService.DeleteFile(targetImg.Url);
            _context.RoomImages.Remove(targetImg);
            room.RoomImages.Remove(targetImg);

            if (wasPrimary)
            {
                var next = room.RoomImages.OrderBy(i => i.SortOrder).FirstOrDefault();
                if (next != null)
                {
                    next.IsPrimary = true;
                    room.ThumbnailImage = next.Url;
                }
                else
                {
                    room.ThumbnailImage = null;
                }
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateImageSortOrderAsync(int roomId, List<int> imageIds, int? landlordId = null)
        {
            var room = await _context.Rooms
                .Include(r => r.Property)
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.RoomId == roomId && (!landlordId.HasValue || (r.Property != null && r.Property.LandlordId == landlordId.Value)));

            if (room == null) return (false, "Không tìm thấy phòng hoặc không có quyền.");

            for (int i = 0; i < imageIds.Count; i++)
            {
                var img = room.RoomImages.FirstOrDefault(x => x.ImageId == imageIds[i]);
                if (img != null)
                {
                    img.SortOrder = i;
                }
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        // ── Helpers ──────────────────────────────────────────────────────
        private async Task<tblRoom?> GetRoomEntityAsync(int roomId, int? landlordId)
        {
            var query = _context.Rooms
                .Include(r => r.Property)
                .AsQueryable();

            if (landlordId.HasValue)
            {
                query = query.Where(r => r.Property != null && r.Property.LandlordId == landlordId.Value);
            }

            return await query.FirstOrDefaultAsync(r => r.RoomId == roomId);
        }

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

        private async Task<string> EnsureUniqueSlugAsync(string baseSlug, int? excludeRoomId = null)
        {
            string slug = baseSlug;
            int counter = 1;

            while (await _context.Rooms.AnyAsync(r => r.Slug == slug && (!excludeRoomId.HasValue || r.RoomId != excludeRoomId.Value)))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }
    }
}
