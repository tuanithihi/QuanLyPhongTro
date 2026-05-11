using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    /// <summary>
    /// Trạng thái hợp đồng
    /// </summary>
    public enum ContractStatus
    {
        Active = 1,         // Đang hiệu lực
        Expired = 0,        // Hết hạn (tự nhiên)
        Terminated = 2      // Chấm dứt trước hạn
    }

    [Table("tblContract")]
    public class tblContract
    {
        [Key]
        public int ContractId { get; set; }

        // ── ĐỊNH DANH ─────────────────────────────────────────────────
        [Required(ErrorMessage = "Mã hợp đồng không được để trống.")]
        [StringLength(30)]
        [Display(Name = "Mã hợp đồng")]
        public string ContractCode { get; set; } = string.Empty;

        // ── LIÊN KẾT PHÒNG & NGƯỜI THUÊ ──────────────────────────────
        [Required(ErrorMessage = "Vui lòng chọn phòng.")]
        [Display(Name = "Phòng")]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn người thuê.")]
        [Display(Name = "Người thuê (đại diện)")]
        public int TenantId { get; set; }

        // ── THỜI HẠN HỢP ĐỒNG ────────────────────────────────────────
        [Required(ErrorMessage = "Ngày bắt đầu không được để trống.")]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        // ── TÀI CHÍNH ─────────────────────────────────────────────────
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tiền thuê phải >= 0.")]
        [Display(Name = "Tiền thuê/tháng (VNĐ)")]
        public decimal MonthlyRent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        [Display(Name = "Tiền đặt cọc (VNĐ)")]
        public decimal Deposit { get; set; }

        [Range(1, 28)]
        [Display(Name = "Ngày thanh toán hàng tháng")]
        public int PaymentDayOfMonth { get; set; } = 5;

        // ── CHỈ SỐ ĐIỆN NƯỚC BAN ĐẦU ─────────────────────────────────
        [Display(Name = "Chỉ số điện ban đầu (kWh)")]
        public double InitialElectricIndex { get; set; } = 0;

        [Display(Name = "Chỉ số nước ban đầu (m³)")]
        public double InitialWaterIndex { get; set; } = 0;

        // ── ĐIỀU KHOẢN ────────────────────────────────────────────────
        [Display(Name = "Điều khoản hợp đồng")]
        public string? Terms { get; set; }

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        // ── TRẠNG THÁI ────────────────────────────────────────────────
        [Display(Name = "Trạng thái")]
        public ContractStatus Status { get; set; } = ContractStatus.Active;

        [Display(Name = "Ngày chấm dứt thực tế")]
        [DataType(DataType.Date)]
        public DateTime? ActualEndDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Lý do chấm dứt")]
        public string? TerminationReason { get; set; }

        // ── AUDIT ──────────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────
        [ForeignKey(nameof(RoomId))]
        public virtual tblRoom? Room { get; set; }

        [ForeignKey(nameof(TenantId))]
        public virtual tblTenant? Tenant { get; set; }

        public virtual ICollection<tblInvoice> Invoices { get; set; } = new List<tblInvoice>();
    }
}
