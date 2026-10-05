using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    public enum PropertyStatus
    {
        Inactive = 0,
        Active = 1
    }

    [Table("tblProperty")]
    public class tblProperty
    {
        [Key]
        public int PropertyId { get; set; }

        [Required(ErrorMessage = "Chủ trọ không được để trống.")]
        [Display(Name = "Chủ trọ")]
        public int LandlordId { get; set; }

        [Required(ErrorMessage = "Tên khu trọ không được để trống.")]
        [StringLength(150)]
        [Display(Name = "Tên khu trọ")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Slug không được để trống.")]
        [StringLength(150)]
        [Display(Name = "Slug đường dẫn")]
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống.")]
        [StringLength(255)]
        [Display(Name = "Địa chỉ chi tiết")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Tỉnh/Thành phố")]
        public int? ProvinceId { get; set; }

        [Display(Name = "Quận/Huyện")]
        public int? DistrictId { get; set; }

        [Display(Name = "Phường/Xã")]
        public int? WardId { get; set; }

        [Display(Name = "Vĩ độ")]
        public double? Latitude { get; set; }

        [Display(Name = "Kinh độ")]
        public double? Longitude { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Trạng thái")]
        public PropertyStatus Status { get; set; } = PropertyStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(LandlordId))]
        public virtual tblLandlord? Landlord { get; set; }

        [ForeignKey(nameof(ProvinceId))]
        public virtual tblProvince? Province { get; set; }

        [ForeignKey(nameof(DistrictId))]
        public virtual tblDistrict? District { get; set; }

        [ForeignKey(nameof(WardId))]
        public virtual tblWard? Ward { get; set; }

        public virtual ICollection<tblRoom> Rooms { get; set; } = new List<tblRoom>();
    }
}
