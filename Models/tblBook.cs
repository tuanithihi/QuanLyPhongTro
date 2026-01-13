using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyThuVien.Models
{
    [Table("tblBook")]
    public class tblBook
    {
        [Key]
        public int BookID { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int PublishedYear { get; set; }
        public int Quantity { get; set; }
        public int CategoryID { get; set; }
        public int PublisherID { get; set; }
        // ===== Quan hệ =====
        [ForeignKey("CategoryID")]
        public virtual tblCategory? Category { get; set; }

        [ForeignKey("PublisherID")]
        public virtual tblPublisher? Publisher { get; set; }

        // Quan hệ many-to-many với Author
        public virtual ICollection<tblBookAuthor> BookAuthors { get; set; } = new List<tblBookAuthor>();

        public bool? IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CoverImage { get; set; }
        public string? BookFile { get; set; }
    }
}