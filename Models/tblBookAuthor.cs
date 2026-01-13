using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuanLyThuVien.Models
{
    [Table("tblBookAuthor")]
    [PrimaryKey(nameof(BookID), nameof(AuthorID))] // đây là cách mới để định nghĩa khóa chính kép
    public class tblBookAuthor
    {
        public int BookID { get; set; }
        public int AuthorID { get; set; }

        [ForeignKey("BookID")]
        public virtual tblBook Book { get; set; } = null!;

        [ForeignKey("AuthorID")]
        public virtual tblAuthor Author { get; set; } = null!;
    }
}
