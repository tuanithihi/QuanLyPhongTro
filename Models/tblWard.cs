using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    [Table("tblWard")]
    public class tblWard
    {
        [Key]
        public int WardId { get; set; }

        [Required]
        public int DistrictId { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Mã phường/xã")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên phường/xã")]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Tên đầy đủ")]
        public string FullName { get; set; } = string.Empty;

        // Navigation
        [ForeignKey(nameof(DistrictId))]
        public virtual tblDistrict? District { get; set; }

        public virtual ICollection<tblProperty> Properties { get; set; } = new List<tblProperty>();
    }
}
