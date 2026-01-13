using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Models;
using QuanLyThuVien.Utilities;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.IO;
using System;
using System.Linq;

namespace QuanLyThuVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly DataContext _context;

        public AccountController(DataContext context)
        {
            _context = context;
        }

        // GET: Admin/Account/Info
        public IActionResult Info()
        {
            if (!Functions.IsAdmin())
            {
                return Redirect("/Home/AccessDenied");
            }
            var admin = _context.Users.FirstOrDefault(u => u.UserID == Functions._UserID && u.Role != null && u.Role.ToLower() == "admin");
            if (admin == null)
            {
                return NotFound();
            }
            return View(admin);
        }

        // GET: Admin/Account/Settings
        public IActionResult Settings()
        {
            if (!Functions.IsAdmin())
            {
                return Redirect("/Home/AccessDenied");
            }
            var admin = _context.Users.FirstOrDefault(u => u.UserID == Functions._UserID && u.Role != null && u.Role.ToLower() == "admin");
            if (admin == null)
            {
                return NotFound();
            }
            admin.Password = null;
            return View(admin);
        }

        // POST: Admin/Account/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Settings(tblUser user, IFormFile? AvatarFile, string? password)
        {
            if (!Functions.IsAdmin())
            {
                return Redirect("/Home/AccessDenied");
            }
            var admin = _context.Users.FirstOrDefault(u => u.UserID == Functions._UserID && u.Role != null && u.Role.ToLower() == "admin");
            if (admin == null)
            {
                return NotFound();
            }

            // Validate
            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                ModelState.AddModelError("FullName", "Họ và tên là bắt buộc");
            }
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError("Email", "Email là bắt buộc");
            }
            else
            {
                var emailPattern = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                if (!emailPattern.IsMatch(user.Email.Trim()))
                {
                    ModelState.AddModelError("Email", "Email không hợp lệ");
                }
                else
                {
                    bool emailTaken = _context.Users.Any(u => u.UserID != admin.UserID && u.Email != null && u.Email.ToLower() == user.Email.Trim().ToLower());
                    if (emailTaken)
                    {
                        ModelState.AddModelError("Email", "Email đã được sử dụng");
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(user.Phone))
            {
                var vnPhonePattern = new Regex(@"^(0\d{9}|\+84\d{9})$");
                if (!vnPhonePattern.IsMatch(user.Phone.Trim()))
                {
                    ModelState.AddModelError("Phone", "Số điện thoại không hợp lệ (định dạng: 0xxxxxxxxx hoặc +84xxxxxxxxx)");
                }
            }

            // Avatar upload
            if (AvatarFile != null && AvatarFile.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(AvatarFile.ContentType.ToLower()))
                {
                    ModelState.AddModelError("Avatar", "Chỉ chấp nhận file ảnh (JPG, PNG, GIF)");
                }
                else if (AvatarFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Avatar", "Kích thước file không được vượt quá 5MB");
                }
                else
                {
                    try
                    {
                        string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "avatars");
                        if (!Directory.Exists(uploadPath))
                            Directory.CreateDirectory(uploadPath);
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AvatarFile.FileName);
                        string filePath = Path.Combine(uploadPath, fileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        AvatarFile.CopyTo(stream);
                        // Xóa avatar cũ nếu có
                        if (!string.IsNullOrEmpty(admin.Avatar))
                        {
                            string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", admin.Avatar.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                        admin.Avatar = "/files/avatars/" + fileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("Avatar", "Lỗi upload ảnh: " + ex.Message);
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                user.UserID = admin.UserID;
                user.UserName = admin.UserName;
                user.Role = admin.Role;
                user.CreatedDate = admin.CreatedDate;
                user.IsActive = admin.IsActive;
                user.Avatar = admin.Avatar;
                return View(user);
            }

            // Cập nhật thông tin
            admin.FullName = user.FullName?.Trim();
            admin.Email = user.Email?.Trim().ToLower();
            admin.Phone = user.Phone?.Trim();
            admin.DateOfBirth = user.DateOfBirth;

            // Đổi mật khẩu nếu nhập
            if (!string.IsNullOrWhiteSpace(password) && password.Length >= 6)
            {
                admin.Password = Functions.MD5Password(password);
            }

            _context.SaveChanges();

            // Cập nhật lại session
            Functions._FullName = admin.FullName ?? string.Empty;
            Functions._Email = admin.Email ?? string.Empty;
            Functions._Avatar = admin.Avatar ?? string.Empty;

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Info");
        }

        // GET: Admin/Account/ChangePassword
        public IActionResult ChangePassword()
        {
            if (!Functions.IsAdmin())
            {
                return Redirect("/Home/AccessDenied");
            }
            return View(new ChangePasswordViewModel());
        }

        // POST: Admin/Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!Functions.IsAdmin())
            {
                return Redirect("/Home/AccessDenied");
            }
            if (string.IsNullOrWhiteSpace(model.CurrentPassword) || string.IsNullOrWhiteSpace(model.NewPassword) || string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                ViewBag.Message = "Vui lòng nhập đầy đủ thông tin.";
                return View(model);
            }
            if (model.NewPassword.Length < 6)
            {
                ViewBag.Message = "Mật khẩu mới tối thiểu 6 ký tự.";
                return View(model);
            }
            if (model.NewPassword != model.ConfirmPassword)
            {
                ViewBag.Message = "Xác nhận mật khẩu không khớp.";
                return View(model);
            }
            var admin = _context.Users.FirstOrDefault(u => u.UserID == Functions._UserID && u.Role != null && u.Role.ToLower() == "admin");
            if (admin == null)
            {
                return NotFound();
            }
            string currentPwHash = Functions.MD5Password(model.CurrentPassword);
            if (admin.Password != currentPwHash)
            {
                ViewBag.Message = "Mật khẩu hiện tại không đúng.";
                return View(model);
            }
            admin.Password = Functions.MD5Password(model.NewPassword);
            _context.SaveChanges();
            ViewBag.Message = "Đổi mật khẩu thành công!";
            return View(new ChangePasswordViewModel());
        }
}}
