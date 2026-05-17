using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    /// <summary>
    /// Đánh giá của khách hàng dành riêng cho từng phòng trọ.
    /// </summary>
    [Table("tblRoomReview")]
    public class tblRoomReview
    {
        [Key]
        public int RoomReviewId { get; set; }

        // ── Liên kết phòng ────────────────────────────────────────────
        [Required]
        public int RoomId { get; set; }

        // ── Liên kết người đánh giá (1 trong 2 sẽ có giá trị) ──────
        public int? TenantId { get; set; }
        public int? UserId   { get; set; }

        // ── Thông tin người đánh giá ──────────────────────────────────
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        // ── Nội dung đánh giá ─────────────────────────────────────────
        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [StringLength(1000)]
        public string? Comment { get; set; }

        // ── Trạng thái & thời gian ────────────────────────────────────
        public bool IsApproved { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ── Navigation ────────────────────────────────────────────────
        [ForeignKey(nameof(RoomId))]
        public virtual tblRoom? Room { get; set; }
    }
}
