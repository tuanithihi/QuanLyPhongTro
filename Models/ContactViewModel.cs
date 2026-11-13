using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên của bạn.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại để chúng tôi liên hệ.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hoặc nhập tiêu đề/chủ đề.")]
        [StringLength(200, ErrorMessage = "Chủ đề không được vượt quá 200 ký tự.")]
        [Display(Name = "Chủ đề liên hệ")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung cần hỗ trợ hoặc phản hồi.")]
        [StringLength(2000, ErrorMessage = "Nội dung không được vượt quá 2000 ký tự.")]
        [Display(Name = "Nội dung tin nhắn")]
        public string Message { get; set; } = string.Empty;
    }
}
