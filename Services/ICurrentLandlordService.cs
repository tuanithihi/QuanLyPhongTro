using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Services
{
    public interface ICurrentLandlordService
    {
        int? GetCurrentLandlordId();
        int? GetCurrentUserId();
        string? GetCurrentUserRole();
        bool IsSuperAdmin();
        bool IsLandlord();
        bool IsTenant();
        bool IsApprovedLandlord();
        Task<tblLandlord?> GetCurrentLandlordAsync();
    }
}
