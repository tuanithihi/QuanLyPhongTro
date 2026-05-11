using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    [Table("tblRoomType")]
    public class tblRoomType
    {
        [Key]
        public int RoomTypeId { get; set; }

        [Required(ErrorMessage = "Tên loại phòng không được để trống.")]
        [StringLength(100)]
        [Display(Name = "Tên loại phòng")]
        public string RoomTypeName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Thứ tự hiển thị")]
        public int SortOrder { get; set; } = 0;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ICollection<tblRoom> Rooms { get; set; } = new List<tblRoom>();
    }
}
