using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    [Table("tblInvoiceDetail")]
    public class tblInvoiceDetail
    {
        [Key]
        public int InvoiceDetailId { get; set; }

        [Required]
        [Display(Name = "Hóa đơn")]
        public int InvoiceId { get; set; }

        [Display(Name = "Dịch vụ")]
        public int? ServiceId { get; set; }

        [StringLength(200)]
        [Display(Name = "Mô tả khoản thu")]
        public string? Description { get; set; }

        [Display(Name = "Số lượng / Số đơn vị tiêu thụ")]
        public double Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Đơn giá tại thời điểm lập hóa đơn")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Thành tiền")]
        public decimal Amount { get; set; }

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────
        [ForeignKey(nameof(InvoiceId))]
        public virtual tblInvoice? Invoice { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public virtual tblService? Service { get; set; }
    }
}
