using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PagedList.Core;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Models
{
    // Class này ánh xạ với bảng trong DB, giữ nguyên để không gây lỗi
    [Table("tblStatBookRank")]
    public class tblStatBookRank
    {
        [Key]
        public int RankID { get; set; }
        public int BookID { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int BorrowCount { get; set; }

        [ForeignKey("BookID")]
        public virtual required tblBook Book { get; set; }
    }

    // --- CÁC CLASS MỚI PHỤC VỤ HIỂN THỊ (Không tạo bảng trong DB) ---

    // Class chứa thông tin Top Người Mượn
    public class TopUserRank
    {
        public int UserID { get; set; }
        public string? FullName { get; set; }
        public string? UserCode { get; set; } // Mã sinh viên/nhân viên
        public string? ClassName { get; set; } // Lớp/Khoa
        public int BorrowCount { get; set; }
        public string? Avatar { get; set; }
    }

    // ViewModel tổng hợp để gửi dữ liệu sang View
    public class StatisticViewModel
    {
        // Paginated collections
        public IPagedList<tblStatBookRank>? PagedTopBooks { get; set; }
        public IPagedList<TopUserRank>? PagedTopUsers { get; set; }
        
        // Lưu giá trị bộ lọc để hiển thị lại trên giao diện
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        
        // Page info
        public int BookPage { get; set; } = 1;
        public int UserPage { get; set; } = 1;
    }
}