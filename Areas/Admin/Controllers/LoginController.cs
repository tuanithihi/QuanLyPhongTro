using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Models;
using QuanLyThuVien.Utilities;

namespace QuanLyThuVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class LoginController : Controller
    {
        private readonly DataContext _context;
        
        public LoginController(DataContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Nếu đã đăng nhập và là Admin thì redirect về trang admin
            if (Functions.IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(tblUser user)
        {
            if (user == null)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ!";
                return RedirectToAction("Index", "Login");
            }

            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrWhiteSpace(user.UserName) || string.IsNullOrWhiteSpace(user.Password))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!";
                return RedirectToAction("Index", "Login");
            }

            // Mã hóa mật khẩu trước khi kiểm tra
            string pw = Functions.MD5Password(user.Password);
            var check = _context.Users
                .Where(u => u.UserName != null && u.UserName.ToLower() == user.UserName.Trim().ToLower() && u.Password == pw)
                .FirstOrDefault();

            if (check == null)
            {
                TempData["ErrorMessage"] = "Tên đăng nhập hoặc mật khẩu không đúng!";
                return RedirectToAction("Index", "Login");
            }

            // Kiểm tra tài khoản có bị khóa không
            if (check.IsActive == false)
            {
                TempData["ErrorMessage"] = "Tài khoản của bạn đã bị khóa!";
                return RedirectToAction("Index", "Login");
            }

            // Lưu thông tin user vào session
            Functions._UserID = check.UserID;
            Functions._UserName = check.UserName ?? string.Empty;
            Functions._FullName = check.FullName ?? string.Empty;
            Functions._Email = check.Email ?? string.Empty;
            Functions._Role = check.Role ?? "User";
            Functions._Avatar = check.Avatar ?? string.Empty;
            Functions._Message = string.Empty;
            Functions._IsAdmin = Functions._Role.ToLower() == "admin";

            // Redirect theo Role
            if (Functions._Role.ToLower() == "admin")
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                // User thông thường không được vào Admin, redirect về trang chủ
                Functions.ClearSession();
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang quản trị!";
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }
    }
}
