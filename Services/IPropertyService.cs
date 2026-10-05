using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services
{
    public interface IPropertyService
    {
        Task<List<PropertyListItemViewModel>> GetPropertiesByLandlordAsync(int? landlordId, string? search = null);

        Task<PropertyDetailViewModel?> GetPropertyDetailAsync(int propertyId, int? landlordId = null);

        Task<PropertyCreateEditViewModel?> GetPropertyForEditAsync(int propertyId, int? landlordId = null);

        Task<(bool Success, string? Error, int PropertyId)> CreatePropertyAsync(PropertyCreateEditViewModel model, int landlordId);

        Task<(bool Success, string? Error)> UpdatePropertyAsync(PropertyCreateEditViewModel model, int? landlordId = null);

        Task<(bool Success, string? Error)> DeletePropertyAsync(int propertyId, int? landlordId = null);

        Task<List<tblProvince>> GetProvincesAsync();

        Task<List<tblDistrict>> GetDistrictsAsync(int provinceId);

        Task<List<tblWard>> GetWardsAsync(int districtId);
    }
}
