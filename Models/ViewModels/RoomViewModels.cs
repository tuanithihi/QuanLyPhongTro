using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace QuanLyPhongTro.Models.ViewModels
{
    public class RoomImageItemViewModel
    {
        public int ImageId { get; set; }
        public string Url { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }

    public class RoomCreateEditViewModel
    {
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khu trọ.")]
        [Display(Name = "Khu trọ / Tòa nhà")]
        public int PropertyId { get; set; }

        [Required(ErrorMessage = "Mã phòng không được để trống.")]
        [StringLength(20, ErrorMessage = "Mã phòng tối đa 20 ký tự.")]
        [Display(Name = "Mã phòng (vd: P101, P202)")]
        public string RoomCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên phòng không được để trống.")]
        [StringLength(150, ErrorMessage = "Tên phòng tối đa 150 ký tự.")]
        [Display(Name = "Tên phòng")]
        public string RoomName { get; set; } = string.Empty;

        [StringLength(250)]
        [Display(Name = "Tiêu đề tin đăng (nếu để trống sẽ tự sinh)")]
        public string? Title { get; set; }

        [StringLength(200)]
        [Display(Name = "Slug đường dẫn")]
        public string? Slug { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại phòng.")]
        [Display(Name = "Loại phòng")]
        public int RoomTypeId { get; set; }

        [Required(ErrorMessage = "Giá thuê không được để trống.")]
        [Range(0, 1000000000, ErrorMessage = "Giá thuê phải từ 0 đến 1.000.000.000 VNĐ.")]
        [Display(Name = "Giá thuê (VNĐ / tháng)")]
        public decimal RoomPrice { get; set; }

        [Range(0, 1000000000, ErrorMessage = "Tiền đặt cọc phải >= 0.")]
        [Display(Name = "Tiền đặt cọc mặc định (VNĐ)")]
        public decimal DefaultDeposit { get; set; }

        [Range(1, 10000, ErrorMessage = "Diện tích phải lớn hơn 0.")]
        [Display(Name = "Diện tích (m²)")]
        public double Area { get; set; } = 20;

        [Display(Name = "Tầng")]
        public int Floor { get; set; } = 1;

        [Range(1, 50, ErrorMessage = "Số người tối đa từ 1 đến 50.")]
        [Display(Name = "Số người ở tối đa")]
        public int MaxOccupants { get; set; } = 2;

        [Range(1, 50, ErrorMessage = "Sức chứa từ 1 đến 50.")]
        [Display(Name = "Sức chứa")]
        public int Capacity { get; set; } = 2;

        [Display(Name = "Mô tả chi tiết phòng")]
        public string? Description { get; set; }

        [Display(Name = "Địa chỉ chi tiết")]
        public string? Address { get; set; }

        [Display(Name = "Vĩ độ (Latitude)")]
        public double? Latitude { get; set; }

        [Display(Name = "Kinh độ (Longitude)")]
        public double? Longitude { get; set; }

        [Display(Name = "Trạng thái phòng")]
        public RoomStatus Status { get; set; } = RoomStatus.Available;

        [Display(Name = "Hiển thị trên website")]
        public bool IsPublished { get; set; } = true;

        [Display(Name = "Tin nổi bật")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Trạng thái phê duyệt")]
        public RoomApprovalStatus ApprovalStatus { get; set; } = RoomApprovalStatus.Pending;

        // Tiện ích đã chọn
        [Display(Name = "Tiện ích phòng")]
        public List<int> SelectedAmenityIds { get; set; } = new();

        // Upload ảnh mới
        [Display(Name = "Tải lên ảnh mới (chọn hoặc kéo thả nhiều ảnh)")]
        public List<IFormFile>? NewPhotos { get; set; }

        // Danh sách ảnh đã có
        public List<RoomImageItemViewModel> ExistingImages { get; set; } = new();

        public int? PrimaryImageId { get; set; }
    }

    public class RoomListItemViewModel
    {
        public int RoomId { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string? PropertyName { get; set; }
        public string? RoomTypeName { get; set; }
        public decimal RoomPrice { get; set; }
        public double Area { get; set; }
        public RoomStatus Status { get; set; }
        public bool IsPublished { get; set; }
        public RoomApprovalStatus ApprovalStatus { get; set; }
        public bool IsFeatured { get; set; }
        public string? ThumbnailImage { get; set; }
        public int ImageCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
