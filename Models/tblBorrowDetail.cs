using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyThuVien.Models
{
    [Table("tblBorrowDetail")]
    public class tblBorrowDetail
    {
        [Key]
        public int BorrowDetailID { get; set; }
        public int BorrowID { get; set; }

        public int BookID { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string BorrowStatus { get; set; } = "Borrowed";
        [ForeignKey("BorrowID")]
        public virtual tblBorrow Borrow { get; set; } = null!;
        [ForeignKey("BookID")]
        public virtual tblBook Book { get; set; } = null!;
        public int Quantity { get; set; }
        public string? Note { get; set; }
    }
}