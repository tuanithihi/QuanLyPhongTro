using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    public enum InvoiceStatus
    {
        Unpaid = 0,     // Chưa thanh toán
        Paid = 1,       // Đã thanh toán
        Overdue = 2     // Quá hạn
    }

    [Table("tblInvoice")]
    public class tblInvoice
    {
        [Key]
        public int InvoiceId { get; set; }

        // ── ĐỊNH DANH ─────────────────────────────────────────────────
        [Required]
        [StringLength(30)]
        [Display(Name = "Số hóa đơn")]
        public string InvoiceCode { get; set; } = string.Empty;

        // ── LIÊN KẾT ─────────────────────────────────────────────────
        [Required]
        [Display(Name = "Phòng")]
        public int RoomId { get; set; }

        [Required]
        [Display(Name = "Hợp đồng")]
        public int ContractId { get; set; }

        // ── KỲ THANH TOÁN ─────────────────────────────────────────────
        [Display(Name = "Tháng")]
        [Range(1, 12)]
        public int BillingMonth { get; set; }

        [Display(Name = "Năm")]
        public int BillingYear { get; set; }

        [Display(Name = "Hạn thanh toán")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        // ── CHỈ SỐ ĐIỆN NƯỚC ─────────────────────────────────────────
        [Display(Name = "Chỉ số điện đầu kỳ (kWh)")]
        public double ElectricIndexStart { get; set; }

        [Display(Name = "Chỉ số điện cuối kỳ (kWh)")]
        public double ElectricIndexEnd { get; set; }

        [Display(Name = "Chỉ số nước đầu kỳ (m³)")]
        public double WaterIndexStart { get; set; }

        [Display(Name = "Chỉ số nước cuối kỳ (m³)")]
        public double WaterIndexEnd { get; set; }

        // ── TIỀN ──────────────────────────────────────────────────────
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tiền thuê phòng")]
        public decimal RoomRentAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tổng tiền dịch vụ")]
        public decimal TotalServiceAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giảm giá")]
        public decimal Discount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tổng tiền phải trả")]
        public decimal TotalAmount { get; set; }

        // ── THANH TOÁN ────────────────────────────────────────────────
        [Display(Name = "Trạng thái")]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;

        [Display(Name = "Ngày thanh toán")]
        [DataType(DataType.Date)]
        public DateTime? PaidDate { get; set; }

        [StringLength(200)]
        [Display(Name = "Phương thức thanh toán")]
        public string? PaymentMethod { get; set; }

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        // ── AUDIT ──────────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────
        [ForeignKey(nameof(RoomId))]
        public virtual tblRoom? Room { get; set; }

        [ForeignKey(nameof(ContractId))]
        public virtual tblContract? Contract { get; set; }

        public virtual ICollection<tblInvoiceDetail> InvoiceDetails { get; set; } = new List<tblInvoiceDetail>();
    }
}
