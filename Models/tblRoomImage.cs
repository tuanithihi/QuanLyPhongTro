using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    [Table("tblRoomImage")]
    public class tblRoomImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Đường dẫn ảnh không được để trống.")]
        [StringLength(500)]
        [Display(Name = "Đường dẫn ảnh")]
        public string Url { get; set; } = string.Empty;

        [Display(Name = "Ảnh đại diện")]
        public bool IsPrimary { get; set; } = false;

        [Display(Name = "Thứ tự sắp xếp")]
        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey(nameof(RoomId))]
        public virtual tblRoom? Room { get; set; }
    }
}
