using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    [Table("tblDistrict")]
    public class tblDistrict
    {
        [Key]
        public int DistrictId { get; set; }

        [Required]
        public int ProvinceId { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Mã quận/huyện")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên quận/huyện")]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Tên đầy đủ")]
        public string FullName { get; set; } = string.Empty;

        // Navigation
        [ForeignKey(nameof(ProvinceId))]
        public virtual tblProvince? Province { get; set; }

        public virtual ICollection<tblWard> Wards { get; set; } = new List<tblWard>();
        public virtual ICollection<tblProperty> Properties { get; set; } = new List<tblProperty>();
    }
}
