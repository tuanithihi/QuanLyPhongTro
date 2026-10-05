using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services
{
    public interface IRoomSearchService
    {
        Task<RoomSearchResultViewModel> SearchRoomsAsync(RoomFilterCriteria criteria);
        Task<tblRoom?> GetRoomDetailAsync(int roomId, string? slug = null);
        Task<LandlordPublicProfileViewModel?> GetLandlordPublicProfileAsync(string slugOrId);
        Task<List<RoomCardViewModel>> GetFeaturedRoomsAsync(int count = 6);
        Task<List<RoomCardViewModel>> GetRecentRoomsAsync(int count = 6);
        Task<List<tblLandlord>> GetFeaturedLandlordsAsync(int count = 4);
        Task<List<tblProvince>> GetPopularProvincesAsync(int count = 6);
        Task<List<tblRoomType>> GetActiveRoomTypesAsync();
        Task<List<tblAmenity>> GetAllAmenitiesAsync();
        Task<List<tblDistrict>> GetDistrictsByProvinceAsync(int provinceId);
        Task<List<tblWard>> GetWardsByDistrictAsync(int districtId);
    }
}
