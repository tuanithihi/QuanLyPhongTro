using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    [Table("tblMenu")]
    public class tblMenu
    {
        [Key]
        public int MenuId { get; set; }

        [Required(ErrorMessage = "Tên menu không được để trống.")]
        [StringLength(100)]
        [Display(Name = "Tên menu")]
        public string MenuName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Đường dẫn")]
        public string? Url { get; set; }

        [StringLength(50)]
        [Display(Name = "Icon (CSS class)")]
        public string? Icon { get; set; }

        [Display(Name = "Menu cha")]
        public int? ParentMenuId { get; set; }

        [Display(Name = "Thứ tự")]
        public int SortOrder { get; set; } = 0;

        [StringLength(30)]
        [Display(Name = "Vị trí")]
        public string? Position { get; set; }  // header, footer, sidebar

        [Display(Name = "Mở tab mới")]
        public bool OpenNewTab { get; set; } = false;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Self-referencing navigation
        [ForeignKey(nameof(ParentMenuId))]
        public virtual tblMenu? ParentMenu { get; set; }

        public virtual ICollection<tblMenu> ChildMenus { get; set; } = new List<tblMenu>();
    }
}
