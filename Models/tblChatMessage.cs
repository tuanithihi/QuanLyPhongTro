using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    public enum ChatSenderType
    {
        Guest  = 0,
        Admin  = 1,
        System = 2   // thông báo tự động (e.g. xác nhận lịch xem)
    }

    [Table("tblChatMessage")]
    public class tblChatMessage
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        public int SessionId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        public ChatSenderType SenderType { get; set; } = ChatSenderType.Guest;

        /// <summary>Admin đã đọc tin nhắn từ khách chưa.</summary>
        public bool IsReadByAdmin { get; set; } = false;

        /// <summary>Khách đã đọc tin nhắn từ admin/system chưa.</summary>
        public bool IsReadByGuest { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey(nameof(SessionId))]
        public virtual tblChatSession? Session { get; set; }
    }
}
