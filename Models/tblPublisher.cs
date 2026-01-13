using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace QuanLyThuVien.Models
{
    [Table("tblPublisher")]
    public class tblPublisher
    {
        [Key]
        public int PublisherID { get; set; }
        public string? PublisherName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
        public DateTime CreatedAt { get; set; } 
        public string? Avatar { get; set; }
    }
}