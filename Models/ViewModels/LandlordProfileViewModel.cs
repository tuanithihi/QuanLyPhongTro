using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace QuanLyPhongTro.Models.ViewModels
{
    public class BankOption
    {
        public string Bin { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class LandlordProfileViewModel
    {
        public int LandlordId { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên không vượt quá 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Số CCCD/CMND tối đa 20 ký tự.")]
        [Display(Name = "Số CCCD/CMND")]
        public string? IdentityNumber { get; set; }

        [StringLength(255)]
        [Display(Name = "Địa chỉ liên hệ")]
        public string? Address { get; set; }

        [Display(Name = "Giới thiệu bản thân / Khu trọ")]
        public string? Description { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string? Avatar { get; set; }

        public IFormFile? AvatarFile { get; set; }

        // ── THÔNG TIN TÀI KHOẢN NGÂN HÀNG (VIETQR) ─────────────────────
        [Display(Name = "Mã ngân hàng (BIN)")]
        public string? BankId { get; set; }

        [Display(Name = "Tên ngân hàng")]
        public string? BankName { get; set; }

        [Display(Name = "Số tài khoản nhận tiền")]
        public string? AccountNumber { get; set; }

        [Display(Name = "Tên chủ tài khoản (in hoa không dấu)")]
        public string? AccountName { get; set; }

        public LandlordStatus Status { get; set; }

        /// <summary>
        /// Danh sách các ngân hàng hỗ trợ VietQR phổ biến tại Việt Nam
        /// </summary>
        public static readonly List<BankOption> PopularBanks = new()
        {
            new BankOption { Bin = "970422", ShortName = "MB Bank", FullName = "Ngân hàng Quân Đội (MB)" },
            new BankOption { Bin = "970436", ShortName = "Vietcombank", FullName = "Ngân hàng Ngoại Thương Việt Nam (VCB)" },
            new BankOption { Bin = "970415", ShortName = "VietinBank", FullName = "Ngân hàng Công Thương Việt Nam" },
            new BankOption { Bin = "970418", ShortName = "BIDV", FullName = "Ngân hàng Đầu tư và Phát triển Việt Nam" },
            new BankOption { Bin = "970407", ShortName = "Techcombank", FullName = "Ngân hàng Kỹ Thương Việt Nam" },
            new BankOption { Bin = "970405", ShortName = "Agribank", FullName = "Ngân hàng Nông nghiệp và Phát triển Nông thôn" },
            new BankOption { Bin = "970432", ShortName = "VPBank", FullName = "Ngân hàng Việt Nam Thịnh Vượng" },
            new BankOption { Bin = "970416", ShortName = "ACB", FullName = "Ngân hàng Á Châu" },
            new BankOption { Bin = "970423", ShortName = "TPBank", FullName = "Ngân hàng Tiên Phong" },
            new BankOption { Bin = "970403", ShortName = "Sacombank", FullName = "Ngân hàng Sài Gòn Thương Tín" },
            new BankOption { Bin = "970437", ShortName = "HDBank", FullName = "Ngân hàng Phát triển TP.HCM" },
            new BankOption { Bin = "970441", ShortName = "VIB", FullName = "Ngân hàng Quốc tế" },
            new BankOption { Bin = "970443", ShortName = "SHB", FullName = "Ngân hàng Sài Gòn - Hà Nội" },
            new BankOption { Bin = "970426", ShortName = "MSB", FullName = "Ngân hàng Hàng Hải" },
            new BankOption { Bin = "970440", ShortName = "SeABank", FullName = "Ngân hàng Đông Nam Á" },
            new BankOption { Bin = "970448", ShortName = "OCB", FullName = "Ngân hàng Phương Đông" }
        };
    }
}
