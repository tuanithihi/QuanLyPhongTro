using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyThuVien.Models
{
    [Table("tblBorrow")]
    public class tblBorrow
    {
        [Key]
        public int BorrowID { get; set; }
        public int UserID { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }  
        public string Status { get; set; } = "Borrowing";
        [ForeignKey("UserID")]
        public virtual tblUser User { get; set; } = null!;
        
        public virtual ICollection<tblBorrowDetail>? BorrowDetails { get; set; } = new List<tblBorrowDetail>();

    }
}