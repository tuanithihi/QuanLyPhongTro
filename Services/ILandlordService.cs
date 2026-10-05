using Microsoft.AspNetCore.Http;
using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services
{
    public interface ILandlordService
    {
        Task<LandlordProfileViewModel?> GetProfileAsync(int landlordId);

        Task<(bool Success, string? Error)> UpdateProfileAsync(int landlordId, LandlordProfileViewModel model, IFormFile? avatarFile);

        Task<(string? BankId, string? AccountNumber, string? AccountName, string? BankName)> GetBankInfoAsync(int landlordId);
    }
}
