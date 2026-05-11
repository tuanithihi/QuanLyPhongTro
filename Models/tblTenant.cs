using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    [Table("tblTenant")]
    public class tblTenant
    {
        [Key]
        public int TenantId { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số CCCD/CMND không được để trống.")]
        [StringLength(20)]
        [Display(Name = "Số CCCD/CMND")]
        public string IdentityNumber { get; set; } = string.Empty;

        [StringLength(15)]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [StringLength(200)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Ngày sinh")]
        public DateOnly? DateOfBirth { get; set; }

        [StringLength(10)]
        [Display(Name = "Giới tính")]
        public string? Gender { get; set; }  // Nam / Nữ / Khác

        [StringLength(500)]
        [Display(Name = "Địa chỉ thường trú")]
        public string? PermanentAddress { get; set; }

        [StringLength(200)]
        [Display(Name = "Ảnh CCCD mặt trước")]
        public string? IdentityFrontImage { get; set; }

        [StringLength(200)]
        [Display(Name = "Ảnh CCCD mặt sau")]
        public string? IdentityBackImage { get; set; }

        [StringLength(200)]
        [Display(Name = "Ảnh đại diện")]
        public string? Avatar { get; set; }

        // --- Auth fields ---
        [StringLength(50)]
        [Display(Name = "Tên đăng nhập")]
        public string? Username { get; set; }

        [StringLength(256)]
        public string? PasswordHash { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<tblContract> Contracts { get; set; } = new List<tblContract>();
    }
}
