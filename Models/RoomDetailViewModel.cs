using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models
{
    public class RoomDetailViewModel
    {
        // ── Dữ liệu phòng ─────────────────────────────────────────────
        public tblRoom           Room     { get; set; } = null!;
        public List<tblService>  Services { get; set; } = new();

        // ── Đánh giá phòng ────────────────────────────────────────────
        public List<tblRoomReview> RoomReviews    { get; set; } = new();
        public double              AverageRating  { get; set; } = 0;
        public int                 ReviewCount    { get; set; } = 0;

        /// <summary>Đánh giá hiện tại của người đang đăng nhập (null nếu chưa đánh giá)</summary>
        public tblRoomReview?      MyReview       { get; set; }

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

        // ── Form đánh giá phòng ───────────────────────────────────────
        [Display(Name = "Họ và tên")]
        public string? ReviewName { get; set; }

        [Range(1, 5)]
        public int ReviewRating { get; set; } = 5;

        [Display(Name = "Nhận xét")]
        public string? ReviewComment { get; set; }
    }
}
