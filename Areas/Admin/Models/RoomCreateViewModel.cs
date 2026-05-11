using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Areas.Admin.Models
{
    /// <summary>
    /// ViewModel dùng chung cho cả Create và Edit phòng trọ.
    /// Tách khỏi Entity để controller chủ động kiểm soát dữ liệu đầu vào.
    /// </summary>
    public class RoomCreateViewModel
    {
        public int RoomId { get; set; }  // = 0 khi Create, > 0 khi Edit

        [Required(ErrorMessage = "Mã phòng không được để trống.")]
        [StringLength(20)]
        [Display(Name = "Mã phòng")]
        public string RoomCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên phòng không được để trống.")]
        [StringLength(150)]
        [Display(Name = "Tên phòng")]
        public string RoomName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại phòng.")]
        [Display(Name = "Loại phòng")]
        public int RoomTypeId { get; set; }

        [Required(ErrorMessage = "Giá thuê không được để trống.")]
        [Range(1_000, 100_000_000, ErrorMessage = "Giá thuê từ 1.000 đến 100.000.000 VNĐ.")]
        [Display(Name = "Giá thuê (VNĐ/tháng)")]
        public decimal RoomPrice { get; set; }

        [Range(0, 500_000_000)]
        [Display(Name = "Tiền đặt cọc mặc định")]
        public decimal DefaultDeposit { get; set; }

        [Range(0, 10000)]
        [Display(Name = "Diện tích (m²)")]
        public double Area { get; set; }

        [Range(1, 100)]
        [Display(Name = "Tầng")]
        public int Floor { get; set; } = 1;

        [Range(1, 50)]
        [Display(Name = "Số người tối đa")]
        public int MaxOccupants { get; set; } = 2;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Trạng thái")]
        public RoomStatus Status { get; set; } = RoomStatus.Available;

        [Display(Name = "Hiển thị trên website")]
        public bool IsPublished { get; set; } = true;

        // ── Upload ảnh mới (không bắt buộc khi Edit) ───────────────────
        [Display(Name = "Ảnh đại diện phòng")]
        public IFormFile? ThumbnailFile { get; set; }

        // Lưu đường dẫn ảnh hiện tại (dùng khi Edit)
        public string? CurrentThumbnail { get; set; }

        // ── Dropdown data (populate trong controller) ──────────────────
        public SelectList? RoomTypeSelectList { get; set; }
    }
}
