using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    /// <summary>
    /// Loại dịch vụ tính phí
    /// </summary>
    public enum ServiceType
    {
        Electric = 1,   // Điện (tính theo kWh)
        Water = 2,      // Nước (tính theo m³)
        Garbage = 3,    // Rác (thu cố định/tháng)
        Wifi = 4,       // Wifi (thu cố định/tháng)
        Parking = 5,    // Xe máy / ô tô
        Other = 99      // Dịch vụ khác
    }

    /// <summary>
    /// Cách tính phí dịch vụ
    /// </summary>
    public enum PricingMethod
    {
        PerUnit = 1,    // Theo đơn vị tiêu thụ (điện, nước)
        FixedMonthly = 2 // Cố định mỗi tháng (rác, wifi)
    }

    [Table("tblService")]
    public class tblService
    {
        [Key]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Tên dịch vụ không được để trống.")]
        [StringLength(100)]
        [Display(Name = "Tên dịch vụ")]
        public string ServiceName { get; set; } = string.Empty;

        [Display(Name = "Loại dịch vụ")]
        public ServiceType ServiceType { get; set; }

        [Display(Name = "Cách tính phí")]
        public PricingMethod PricingMethod { get; set; } = PricingMethod.FixedMonthly;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        [Display(Name = "Đơn giá (VNĐ / đơn vị hoặc / tháng)")]
        public decimal UnitPrice { get; set; }

        [StringLength(20)]
        [Display(Name = "Đơn vị tính")]
        public string? Unit { get; set; }  // kWh, m³, tháng, xe...

        [StringLength(200)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual ICollection<tblInvoiceDetail> InvoiceDetails { get; set; } = new List<tblInvoiceDetail>();
    }
}
