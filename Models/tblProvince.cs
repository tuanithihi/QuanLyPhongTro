using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    [Table("tblProvince")]
    public class tblProvince
    {
        [Key]
        public int ProvinceId { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Mã tỉnh/thành")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên tỉnh/thành")]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Tên đầy đủ")]
        public string FullName { get; set; } = string.Empty;

        // Navigation
        public virtual ICollection<tblDistrict> Districts { get; set; } = new List<tblDistrict>();
        public virtual ICollection<tblProperty> Properties { get; set; } = new List<tblProperty>();
    }
}
