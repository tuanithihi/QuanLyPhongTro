using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    /// <summary>
    /// Trạng thái phòng trọ
    /// </summary>
    public enum RoomStatus
    {
        Available = 0,      // Phòng trống
        Occupied = 1,       // Đang có người thuê
        Maintenance = 2     // Đang bảo trì / sửa chữa
    }

    [Table("tblRoom")]
    public class tblRoom
    {
        [Key]
        public int RoomId { get; set; }

        // ── THÔNG TIN CƠ BẢN ──────────────────────────────────────────
        [Required(ErrorMessage = "Mã phòng không được để trống.")]
        [StringLength(20, ErrorMessage = "Mã phòng tối đa 20 ký tự.")]
        [Display(Name = "Mã phòng")]
        public string RoomCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên phòng không được để trống.")]
        [StringLength(150)]
        [Display(Name = "Tên phòng")]
        public string RoomName { get; set; } = string.Empty;

        // ── PHÂN LOẠI ─────────────────────────────────────────────────
        [Required(ErrorMessage = "Vui lòng chọn loại phòng.")]
        [Display(Name = "Loại phòng")]
        public int RoomTypeId { get; set; }

        // ── GIÁ THUÊ ──────────────────────────────────────────────────
        [Required(ErrorMessage = "Giá thuê không được để trống.")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá thuê phải >= 0.")]
        [Display(Name = "Giá thuê (VNĐ/tháng)")]
        public decimal RoomPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        [Display(Name = "Tiền đặt cọc mặc định (VNĐ)")]
        public decimal DefaultDeposit { get; set; }

        // ── ĐẶC ĐIỂM VẬT LÝ ──────────────────────────────────────────
        [Range(0, 10000)]
        [Display(Name = "Diện tích (m²)")]
        public double Area { get; set; }

        [Display(Name = "Tầng")]
        public int Floor { get; set; } = 1;

        [Range(1, 50)]
        [Display(Name = "Số người tối đa")]
        public int MaxOccupants { get; set; } = 2;

        // ── MÔ TẢ & MEDIA ─────────────────────────────────────────────
        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; }

        [StringLength(300)]
        [Display(Name = "Ảnh đại diện phòng")]
        public string? ThumbnailImage { get; set; }

        // ── VỊ TRÍ ────────────────────────────────────────────────────────
        [StringLength(255)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Vĩ độ")]
        public double? Latitude { get; set; }

        [Display(Name = "Kinh độ")]
        public double? Longitude { get; set; }

        /// <summary>Khoảng cách (km) tính từ vị trí người dùng — chỉ dùng hiển thị, không lưu DB.</summary>
        [NotMapped]
        public double? DistanceKm { get; set; }

        /// <summary>Điểm đánh giá trung bình — chỉ dùng hiển thị, không lưu DB.</summary>
        [NotMapped]
        public double AverageRating { get; set; }

        /// <summary>Số lượng đánh giá — chỉ dùng hiển thị, không lưu DB.</summary>
        [NotMapped]
        public int ReviewCount { get; set; }

        // ── TRẠNG THÁI ────────────────────────────────────────────────
        [Display(Name = "Trạng thái")]
        public RoomStatus Status { get; set; } = RoomStatus.Available;

        [Display(Name = "Hiển thị trên website")]
        public bool IsPublished { get; set; } = true;

        // ── AUDIT ──────────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────
        [ForeignKey(nameof(RoomTypeId))]
        public virtual tblRoomType? RoomType { get; set; }

        public virtual ICollection<tblContract> Contracts { get; set; } = new List<tblContract>();
        public virtual ICollection<tblInvoice> Invoices { get; set; } = new List<tblInvoice>();
        public virtual ICollection<tblRoomReview> RoomReviews { get; set; } = new List<tblRoomReview>();
    }
}
