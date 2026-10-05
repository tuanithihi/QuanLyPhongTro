using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.ViewModels
{
    public class PropertyCreateEditViewModel
    {
        public int PropertyId { get; set; }

        public int LandlordId { get; set; }

        [Required(ErrorMessage = "Tên khu trọ không được để trống.")]
        [StringLength(150, ErrorMessage = "Tên khu trọ tối đa 150 ký tự.")]
        [Display(Name = "Tên khu trọ")]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Slug đường dẫn")]
        public string? Slug { get; set; }

        [Required(ErrorMessage = "Địa chỉ chi tiết không được để trống.")]
        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
        [Display(Name = "Địa chỉ chi tiết (Số nhà, tên đường...)")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Tỉnh / Thành phố")]
        public int? ProvinceId { get; set; }

        [Display(Name = "Quận / Huyện")]
        public int? DistrictId { get; set; }

        [Display(Name = "Phường / Xã")]
        public int? WardId { get; set; }

        [Display(Name = "Vĩ độ (Latitude)")]
        public double? Latitude { get; set; }

        [Display(Name = "Kinh độ (Longitude)")]
        public double? Longitude { get; set; }

        [Display(Name = "Mô tả giới thiệu khu trọ")]
        public string? Description { get; set; }

        [Display(Name = "Trạng thái")]
        public PropertyStatus Status { get; set; } = PropertyStatus.Active;
    }

    public class PropertyListItemViewModel
    {
        public int PropertyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? ProvinceName { get; set; }
        public string? DistrictName { get; set; }
        public string? WardName { get; set; }
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public PropertyStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PropertyDetailViewModel
    {
        public tblProperty Property { get; set; } = null!;
        public List<tblRoom> Rooms { get; set; } = new();
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }
    }
}
