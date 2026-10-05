using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services
{
    public class LandlordService : ILandlordService
    {
        private readonly DataContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<LandlordService> _logger;

        public LandlordService(
            DataContext context,
            IFileUploadService fileUploadService,
            ILogger<LandlordService> logger)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        public async Task<LandlordProfileViewModel?> GetProfileAsync(int landlordId)
        {
            var landlord = await _context.Landlords
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.LandlordId == landlordId);

            if (landlord == null) return null;

            return new LandlordProfileViewModel
            {
                LandlordId = landlord.LandlordId,
                FullName = landlord.FullName,
                Phone = landlord.Phone ?? string.Empty,
                Email = landlord.Email ?? string.Empty,
                IdentityNumber = landlord.IdentityNumber,
                Address = landlord.Address,
                Description = landlord.Description,
                Avatar = landlord.Avatar ?? landlord.User?.Avatar,
                BankId = landlord.BankId,
                BankName = landlord.BankName,
                AccountNumber = landlord.AccountNumber,
                AccountName = landlord.AccountName,
                Status = landlord.Status
            };
        }

        public async Task<(bool Success, string? Error)> UpdateProfileAsync(int landlordId, LandlordProfileViewModel model, IFormFile? avatarFile)
        {
            var landlord = await _context.Landlords
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.LandlordId == landlordId);

            if (landlord == null)
            {
                return (false, "Không tìm thấy hồ sơ chủ trọ.");
            }

            // Kiểm tra trùng SĐT nếu thay đổi
            if (!string.IsNullOrWhiteSpace(model.Phone) && model.Phone != landlord.Phone)
            {
                bool phoneExists = await _context.Landlords.AnyAsync(l => l.LandlordId != landlordId && l.Phone == model.Phone.Trim());
                if (phoneExists)
                {
                    return (false, "Số điện thoại này đã được sử dụng bởi chủ trọ khác.");
                }
            }

            // Upload Avatar mới nếu có
            if (avatarFile != null && avatarFile.Length > 0)
            {
                var (isValid, errorMsg, relativePath) = await _fileUploadService.UploadImageAsync(avatarFile, "images/avatars");
                if (!isValid)
                {
                    return (false, errorMsg ?? "Tải ảnh đại diện thất bại.");
                }

                if (!string.IsNullOrEmpty(landlord.Avatar))
                {
                    _fileUploadService.DeleteFile(landlord.Avatar);
                }

                landlord.Avatar = relativePath;
                if (landlord.User != null)
                {
                    landlord.User.Avatar = relativePath;
                }
                else if (landlord.UserId.HasValue)
                {
                    var u = await _context.Users.FindAsync(landlord.UserId.Value);
                    if (u != null) u.Avatar = relativePath;
                }
            }

            landlord.FullName = model.FullName.Trim();
            landlord.Phone = model.Phone?.Trim();
            landlord.Email = model.Email?.Trim();
            landlord.IdentityNumber = model.IdentityNumber?.Trim();
            landlord.Address = model.Address?.Trim();
            landlord.Description = model.Description?.Trim();

            // Cập nhật thông tin ngân hàng
            landlord.BankId = model.BankId?.Trim();
            landlord.BankName = model.BankName?.Trim();
            landlord.AccountNumber = model.AccountNumber?.Trim();
            landlord.AccountName = model.AccountName?.Trim().ToUpperInvariant();
            landlord.UpdatedAt = DateTime.Now;

            // Đồng bộ sang bảng User nếu có liên kết
            if (landlord.User != null)
            {
                landlord.User.FullName = landlord.FullName;
                landlord.User.Phone = landlord.Phone;
                landlord.User.Email = landlord.Email ?? landlord.User.Email;
                landlord.User.UpdatedAt = DateTime.Now;
            }
            else if (landlord.UserId.HasValue)
            {
                var u = await _context.Users.FindAsync(landlord.UserId.Value);
                if (u != null)
                {
                    u.FullName = landlord.FullName;
                    u.Phone = landlord.Phone;
                    u.Email = landlord.Email ?? u.Email;
                    u.UpdatedAt = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(string? BankId, string? AccountNumber, string? AccountName, string? BankName)> GetBankInfoAsync(int landlordId)
        {
            var landlord = await _context.Landlords
                .Select(l => new { l.LandlordId, l.BankId, l.AccountNumber, l.AccountName, l.BankName })
                .FirstOrDefaultAsync(l => l.LandlordId == landlordId);

            if (landlord == null)
            {
                return (null, null, null, null);
            }

            return (landlord.BankId, landlord.AccountNumber, landlord.AccountName, landlord.BankName);
        }
    }
}
