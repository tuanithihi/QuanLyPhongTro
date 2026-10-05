using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    public enum BookingRequestType
    {
        Viewing = 1,
        ViewingRequest = 1,  // Đặt lịch xem phòng (backward compatibility)
        ChatMessage = 2,     // Tin nhắn liên hệ
        Deposit = 3          // Đặt cọc giữ phòng
    }

    public enum BookingRequestStatus
    {
        Pending  = 0,  // Chờ xử lý
        Accepted = 1,  // Chấp nhận
        Rejected = 2   // Từ chối
    }

    [Table("tblBookingRequest")]
    public class tblBookingRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Display(Name = "Chủ trọ")]
        public int? LandlordId { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(50)]
        [Display(Name = "Ngày muốn xem")]
        public string? PreferredDate { get; set; }

        [Display(Name = "Thời gian xem phòng")]
        public DateTime? ViewingTime { get; set; }

        [StringLength(1000)]
        [Display(Name = "Nội dung / Tin nhắn")]
        public string? Message { get; set; }

        [Display(Name = "Loại yêu cầu")]
        public BookingRequestType RequestType { get; set; } = BookingRequestType.Viewing;

        [Display(Name = "Trạng thái")]
        public BookingRequestStatus Status { get; set; } = BookingRequestStatus.Pending;

        [StringLength(500)]
        [Display(Name = "Ghi chú của quản trị viên")]
        public string? AdminNote { get; set; }

        [Display(Name = "Đã thông báo cho khách")]
        public bool IsGuestNotified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey(nameof(LandlordId))]
        public virtual tblLandlord? Landlord { get; set; }

        [ForeignKey(nameof(RoomId))]
        public virtual tblRoom? Room { get; set; }
    }
}
