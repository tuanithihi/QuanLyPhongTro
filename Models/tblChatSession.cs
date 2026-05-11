using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    [Table("tblChatSession")]
    public class tblChatSession
    {
        [Key]
        public int SessionId { get; set; }

        /// <summary>GUID lưu trong cookie của khách để nhận dạng phiên.</summary>
        [Required]
        [StringLength(36)]
        public string SessionKey { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string GuestName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string GuestPhone { get; set; } = string.Empty;

        public bool IsOpen { get; set; } = true;

        public DateTime LastMsgAt { get; set; } = DateTime.Now;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Liên kết người thuê đã đăng nhập (nullable — anonymous nếu null).</summary>
        public int? TenantId { get; set; }

        /// <summary>Liên kết người dùng website đã đăng nhập (nullable).</summary>
        public int? UserId { get; set; }

        // Navigation
        public virtual ICollection<tblChatMessage> Messages { get; set; } = new List<tblChatMessage>();
        public virtual tblTenant? Tenant { get; set; }
        public virtual tblUser?   User   { get; set; }
    }
}
