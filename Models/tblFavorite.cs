using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    [Table("tblFavorite")]
    public class tblFavorite
    {
        [Key]
        public int FavoriteId { get; set; }

        [Required]
        public int RoomId { get; set; }

        public int? UserId { get; set; }

        public int? TenantId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey(nameof(RoomId))]
        public virtual tblRoom? Room { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual tblUser? User { get; set; }

        [ForeignKey(nameof(TenantId))]
        public virtual tblTenant? Tenant { get; set; }
    }
}
