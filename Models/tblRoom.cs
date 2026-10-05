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

    /// <summary>
    /// Trạng thái phê duyệt tin đăng phòng
    /// </summary>
    public enum RoomApprovalStatus
    {
        Draft = 0,
        Pending = 1,
        Published = 2,
        Rejected = 3
    }

    [Table("tblRoom")]
    public class tblRoom
    {
        [Key]
        public int RoomId { get; set; }

        // ── KHU TRỌ LIÊN KẾT ──────────────────────────────────────────
        [Display(Name = "Khu trọ / Tòa nhà")]
        public int? PropertyId { get; set; }

        // ── THÔNG TIN CƠ BẢN ──────────────────────────────────────────
        [Required(ErrorMessage = "Mã phòng không được để trống.")]
        [StringLength(20, ErrorMessage = "Mã phòng tối đa 20 ký tự.")]
        [Display(Name = "Mã phòng")]
        public string RoomCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên phòng không được để trống.")]
        [StringLength(150)]
        [Display(Name = "Tên phòng")]
        public string RoomName { get; set; } = string.Empty;

        [StringLength(250)]
        [Display(Name = "Tiêu đề tin đăng")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Slug không được để trống.")]
        [StringLength(200)]
        [Display(Name = "Slug đường dẫn")]
        public string Slug { get; set; } = string.Empty;

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

        [Range(1, 50)]
        [Display(Name = "Sức chứa")]
        public int Capacity { get; set; } = 2;

        // ── MÔ TẢ & MEDIA ─────────────────────────────────────────────
        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; }

        [StringLength(300)]
        [Display(Name = "Ảnh đại diện phòng")]
        public string? ThumbnailImage { get; set; }

        [Display(Name = "Ảnh mô tả thêm")]
        public string? GalleryImages { get; set; }

        [Display(Name = "Vật dụng / đồ dùng có sẵn")]
        public string? IncludedAmenities { get; set; }

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

        // ── TRẠNG THÁI & HIỂN THỊ ─────────────────────────────────────
        [Display(Name = "Trạng thái phòng")]
        public RoomStatus Status { get; set; } = RoomStatus.Available;

        [Display(Name = "Hiển thị trên website")]
        public bool IsPublished { get; set; } = true;

        [Display(Name = "Trạng thái phê duyệt")]
        public RoomApprovalStatus ApprovalStatus { get; set; } = RoomApprovalStatus.Published;

        [Display(Name = "Tin nổi bật")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Lượt xem")]
        public int ViewCount { get; set; } = 0;

        [Display(Name = "Ngày xuất bản")]
        public DateTime? PublishedAt { get; set; }

        [StringLength(500)]
        [Display(Name = "Lý do từ chối / Gỡ tin")]
        public string? RejectReason { get; set; }

        // ── AUDIT ──────────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────
        [ForeignKey(nameof(RoomTypeId))]
        public virtual tblRoomType? RoomType { get; set; }

        [ForeignKey(nameof(PropertyId))]
        public virtual tblProperty? Property { get; set; }

        public virtual ICollection<tblContract> Contracts { get; set; } = new List<tblContract>();
        public virtual ICollection<tblInvoice> Invoices { get; set; } = new List<tblInvoice>();
        public virtual ICollection<tblRoomReview> RoomReviews { get; set; } = new List<tblRoomReview>();
        public virtual ICollection<tblRoomImage> RoomImages { get; set; } = new List<tblRoomImage>();
        public virtual ICollection<tblRoomAmenity> RoomAmenities { get; set; } = new List<tblRoomAmenity>();
        public virtual ICollection<tblFavorite> Favorites { get; set; } = new List<tblFavorite>();
    }
}
