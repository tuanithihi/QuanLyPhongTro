using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models
{
    public class RoomDetailViewModel
    {
        // ── Dữ liệu phòng ─────────────────────────────────────────────
        public tblRoom           Room     { get; set; } = null!;
        public List<tblService>  Services { get; set; } = new();

        // ── Form đặt lịch xem ─────────────────────────────────────────
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [Display(Name = "Họ và tên")]
        public string? BookingName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Display(Name = "Số điện thoại")]
        public string? BookingPhone { get; set; }

        [Display(Name = "Ngày muốn xem")]
        public string? BookingDate { get; set; }

        [Display(Name = "Ghi chú")]
        public string? BookingNote { get; set; }
    }
}
