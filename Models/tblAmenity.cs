using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    [Table("tblAmenity")]
    public class tblAmenity
    {
        [Key]
        public int AmenityId { get; set; }

        [Required(ErrorMessage = "Tên tiện ích không được để trống.")]
        [StringLength(100)]
        [Display(Name = "Tên tiện ích")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Icon hiển thị (FontAwesome/Bootstrap Icon)")]
        public string? Icon { get; set; }

        [StringLength(255)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<tblRoomAmenity> RoomAmenities { get; set; } = new List<tblRoomAmenity>();
    }
}
