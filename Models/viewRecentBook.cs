using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyThuVien.Models
{
    [Table("viewRecentBook")]
    public class viewRecentBook
    {
        [Key]
        public int BookID { get; set; }
        public string? Title { get; set; }
        public string? CoverImage { get; set; }
        public string? CategoryName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int PublishedYear { get; set; }
        public bool? IsActive { get; set; }
        public string? AuthorName { get; set; }
    }
}