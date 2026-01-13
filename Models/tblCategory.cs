using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyThuVien.Models
{
    [Table("tblCategory")]
    public class tblCategory
    {
        [Key]
        public int CategoryID { get; set; }

        public string? CategoryName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public string? Description { get; set; }

    }
}
