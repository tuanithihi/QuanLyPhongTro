using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    [Table("tblPost")]
    public class tblPost
    {
        [Key]
        public int PostId { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [StringLength(300)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(350)]
        [Display(Name = "Đường dẫn (Slug)")]
        public string Slug { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Tóm tắt")]
        public string? Summary { get; set; }

        [Display(Name = "Nội dung")]
        public string? Content { get; set; }

        [StringLength(300)]
        [Display(Name = "Ảnh đại diện")]
        public string? ThumbnailImage { get; set; }

        [StringLength(100)]
        [Display(Name = "Danh mục")]
        public string? Category { get; set; }

        [Display(Name = "Ghim lên đầu")]
        public bool IsPinned { get; set; } = false;

        [Display(Name = "Trạng thái đăng")]
        public bool IsPublished { get; set; } = false;

        [Display(Name = "Ngày đăng")]
        public DateTime? PublishedAt { get; set; }

        [Display(Name = "Lượt xem")]
        public int ViewCount { get; set; } = 0;

        // ── SEO ────────────────────────────────────────────────────────
        [StringLength(300)]
        [Display(Name = "Meta Title")]
        public string? MetaTitle { get; set; }

        [StringLength(500)]
        [Display(Name = "Meta Description")]
        public string? MetaDescription { get; set; }

        // ── AUDIT ──────────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
