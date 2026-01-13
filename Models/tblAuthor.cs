using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyThuVien.Models
{
    [Table("tblAuthor")]
    public class tblAuthor
    {
        [Key]
        public int AuthorID { get; set; }
        public string? AuthorName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Biography { get; set; }
        public string? Email { get; set; }
        
        // Quan hệ many-to-many với Book
        public virtual ICollection<tblBookAuthor>? BookAuthors { get; set; }
        
        public bool? IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Avatar { get; set; }
    }
}