using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services
{
    public interface IRoomService
    {
        Task<List<RoomListItemViewModel>> GetRoomsByLandlordAsync(
            int? landlordId,
            int? propertyId = null,
            string? search = null,
            RoomStatus? status = null,
            RoomApprovalStatus? approvalStatus = null);

        Task<tblRoom?> GetRoomByIdAsync(int roomId, int? landlordId = null);

        Task<RoomCreateEditViewModel?> GetRoomForEditAsync(int roomId, int? landlordId = null);

        Task<(bool Success, string? Error, int RoomId)> CreateRoomAsync(RoomCreateEditViewModel model, int landlordId);

        Task<(bool Success, string? Error)> UpdateRoomAsync(RoomCreateEditViewModel model, int? landlordId = null);

        Task<(bool Success, string? Error)> DeleteRoomAsync(int roomId, int? landlordId = null);

        Task<(bool Success, string? Error)> PublishListingAsync(int roomId, int? landlordId = null);

        Task<(bool Success, string? Error)> HideListingAsync(int roomId, int? landlordId = null);

        Task<(bool Success, string? Error)> ApproveListingAsync(int roomId);

        Task<(bool Success, string? Error)> RejectListingAsync(int roomId, string? reason);

        Task<(bool Success, string? Error)> UnpublishListingAsync(int roomId, string? reason, int? landlordId = null);

        Task<(bool Success, string? Error)> MarkAsOccupiedAsync(int roomId, int? landlordId = null);

        Task<(bool Success, string? Error)> MarkAsAvailableAsync(int roomId, int? landlordId = null);

        Task<(bool Success, string? Error)> SetPrimaryImageAsync(int roomId, int imageId, int? landlordId = null);

        Task<(bool Success, string? Error)> DeleteImageAsync(int roomId, int imageId, int? landlordId = null);

        Task<(bool Success, string? Error)> UpdateImageSortOrderAsync(int roomId, List<int> imageIds, int? landlordId = null);
    }
}
