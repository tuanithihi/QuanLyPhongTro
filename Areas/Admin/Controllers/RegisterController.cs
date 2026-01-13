using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Models;
using QuanLyThuVien.Utilities;
using QuanLyThuVien.Attributes;

namespace QuanLyThuVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class RegisterController : Controller
    {
        private readonly DataContext _context;
        public RegisterController(DataContext context)
        {
            _context = context;
        }
        
        public IActionResult Index()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(tblUser user)
        {
            if (user == null)
            {
                ModelState.AddModelError("", "Dữ liệu không hợp lệ");
                return View(user);
            }

            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                ModelState.AddModelError("UserName", "Tên đăng nhập là bắt buộc");
            }
            else
            {
                // Ràng buộc định dạng đơn giản
                var userNamePattern = new Regex("^[a-z0-9]{4,100}$");
                if (!userNamePattern.IsMatch(user.UserName.Trim().ToLower()))
                {
                    ModelState.AddModelError("UserName", "Tên đăng nhập chỉ gồm chữ thường và số (4-100 ký tự)");
                }
                else
                {
                    // Kiểm tra trùng username
                    bool userNameTaken = _context.Users.Any(u => u.UserName != null && u.UserName.ToLower() == user.UserName.Trim().ToLower());
                    if (userNameTaken)
                    {
                        ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError("Email", "Email là bắt buộc");
            }
            else
            {
                // Kiểm tra định dạng email
                var emailPattern = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                if (!emailPattern.IsMatch(user.Email.Trim()))
                {
                    ModelState.AddModelError("Email", "Email không hợp lệ");
                }
                else
                {
                    // Kiểm tra trùng email
                    bool emailTaken = _context.Users.Any(u => u.Email != null && u.Email.ToLower() == user.Email.Trim().ToLower());
                    if (emailTaken)
                    {
                        ModelState.AddModelError("Email", "Email đã được sử dụng");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                ModelState.AddModelError("Password", "Mật khẩu là bắt buộc");
            }
            else if (user.Password.Length < 6)
            {
                ModelState.AddModelError("Password", "Mật khẩu tối thiểu 6 ký tự");
            }

            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                ModelState.AddModelError("FullName", "Họ và tên là bắt buộc");
            }

            if (!ModelState.IsValid)
            {
                // Xóa password trước khi trả về view để bảo mật
                if (user != null)
                {
                    user.Password = null;
                }
                return View(user);
            }

            // Chuẩn hóa dữ liệu trước khi lưu
            if (!string.IsNullOrWhiteSpace(user.UserName))
            {
                user.UserName = user.UserName.Trim().ToLower();
            }
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                user.Email = user.Email.Trim().ToLower();
            }
            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                user.FullName = user.FullName.Trim();
            }
            
            // Lưu password gốc trước khi hash
            string originalPassword = user.Password ?? string.Empty;

            // Hash password sau khi pass validation
            user.Password = Functions.MD5Password(originalPassword);
            user.IsActive = true;
            user.Role = "Admin"; // Tạo tài khoản Admin
            user.CreatedDate = DateTime.Now;
            
            _context.Users.Add(user);
            _context.SaveChanges();
            
            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Index", "Login");
        }
    }
}
