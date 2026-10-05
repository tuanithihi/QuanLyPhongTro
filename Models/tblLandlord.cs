using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    public enum LandlordStatus
    {
        Pending = 0,
        Approved = 1,
        Suspended = 2,
        Rejected = 3
    }

    [Table("tblLandlord")]
    public class tblLandlord
    {
        [Key]
        public int LandlordId { get; set; }

        public int? UserId { get; set; }

        [Required(ErrorMessage = "Họ tên chủ trọ không được để trống.")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [StringLength(150)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(20)]
        [Display(Name = "Số CCCD/CMND")]
        public string? IdentityNumber { get; set; }

        [StringLength(255)]
        [Display(Name = "Ảnh đại diện")]
        public string? Avatar { get; set; }

        [StringLength(255)]
        [Display(Name = "Địa chỉ liên hệ")]
        public string? Address { get; set; }

        [Display(Name = "Giới thiệu / Mô tả")]
        public string? Description { get; set; }

        [StringLength(50)]
        [Display(Name = "Mã định danh ngân hàng")]
        public string? BankId { get; set; }

        [StringLength(50)]
        [Display(Name = "Số tài khoản ngân hàng")]
        public string? AccountNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Tên chủ tài khoản")]
        public string? AccountName { get; set; }

        [StringLength(100)]
        [Display(Name = "Tên ngân hàng")]
        public string? BankName { get; set; }

        [Display(Name = "Trạng thái phê duyệt")]
        public LandlordStatus Status { get; set; } = LandlordStatus.Approved;

        [StringLength(500)]
        [Display(Name = "Lý do từ chối / Ghi chú")]
        public string? RejectReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(UserId))]
        public virtual tblUser? User { get; set; }

        public virtual ICollection<tblProperty> Properties { get; set; } = new List<tblProperty>();
        public virtual ICollection<tblTenant> Tenants { get; set; } = new List<tblTenant>();
        public virtual ICollection<tblContract> Contracts { get; set; } = new List<tblContract>();
        public virtual ICollection<tblInvoice> Invoices { get; set; } = new List<tblInvoice>();
        public virtual ICollection<tblService> Services { get; set; } = new List<tblService>();
        public virtual ICollection<tblBookingRequest> BookingRequests { get; set; } = new List<tblBookingRequest>();
        public virtual ICollection<tblChatSession> ChatSessions { get; set; } = new List<tblChatSession>();
    }
}
