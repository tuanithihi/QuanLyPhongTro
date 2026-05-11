using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models
{
    public class ProfileViewModel
    {
        /// <summary>"Tenant" hoặc "User"</summary>
        public string UserType { get; set; } = "";

        // ── Thông tin chung ──────────────────────────────────────────
        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = "";

        [StringLength(15)]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [StringLength(200)]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        public string? Avatar { get; set; }

        // ── Chỉ Tenant ───────────────────────────────────────────────
        [Display(Name = "Số CCCD/CMND")]
        public string? IdentityNumber { get; set; }  // read-only

        [Display(Name = "Tên đăng nhập")]
        public string? TenantUsername { get; set; }  // read-only (admin set)

        [Display(Name = "Ngày sinh")]
        public DateOnly? DateOfBirth { get; set; }

        [StringLength(10)]
        [Display(Name = "Giới tính")]
        public string? Gender { get; set; }

        [StringLength(500)]
        [Display(Name = "Địa chỉ thường trú")]
        public string? PermanentAddress { get; set; }

        // ── Chỉ User thường ──────────────────────────────────────────
        [Display(Name = "Tên đăng nhập")]
        public string? Username { get; set; }  // read-only

        // ── Đổi mật khẩu (dùng chung) ────────────────────────────────
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string? CurrentPassword { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Xác nhận mật khẩu không khớp.")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string? ConfirmPassword { get; set; }
    }
}
