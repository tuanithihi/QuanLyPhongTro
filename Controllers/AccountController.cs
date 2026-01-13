using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using QuanLyThuVien.Models;
using QuanLyThuVien.Utilities;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace QuanLyThuVien.Controllers
{
    public class AccountController : Controller
    {
        private readonly DataContext _context;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public AccountController(DataContext context, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(tblUser user, IFormFile? AvatarFile)
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
                // Ràng buộc định dạng username
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

            // Kiểm tra số điện thoại nếu có
            if (!string.IsNullOrWhiteSpace(user.Phone))
            {
                var vnPhonePattern = new Regex(@"^(0\d{9}|\+84\d{9})$");
                if (!vnPhonePattern.IsMatch(user.Phone.Trim()))
                {
                    ModelState.AddModelError("Phone", "Số điện thoại không hợp lệ (định dạng: 0xxxxxxxxx hoặc +84xxxxxxxxx)");
                }
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
            if (!string.IsNullOrWhiteSpace(user.Phone))
            {
                user.Phone = user.Phone.Trim();
            }

            // Lưu password gốc trước khi hash
            string originalPassword = user.Password ?? string.Empty;

            // Xử lý upload avatar nếu có
            if (AvatarFile != null && AvatarFile.Length > 0)
            {
                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(AvatarFile.ContentType.ToLower()))
                {
                    ModelState.AddModelError("Avatar", "Chỉ chấp nhận file ảnh (JPG, PNG, GIF)");
                }
                // Validate file size (5MB)
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
                        
                        user.Avatar = "/files/avatars/" + fileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("Avatar", "Lỗi upload ảnh: " + ex.Message);
                    }
                }
            }

            // Hash password sau khi pass validation
            user.Password = Functions.MD5Password(originalPassword);
            user.IsActive = true;
            user.Role = "User"; // Mặc định là User
            user.CreatedDate = DateTime.Now;

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(tblUser user)
        {
            if (user == null)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ!";
                return RedirectToAction("Login");
            }

            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrWhiteSpace(user.UserName) || string.IsNullOrWhiteSpace(user.Password))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!";
                return RedirectToAction("Login");
            }

            // Mã hóa mật khẩu trước khi kiểm tra
            string pw = Functions.MD5Password(user.Password);
            var check = _context.Users
                .Where(u => u.UserName != null && u.UserName.ToLower() == user.UserName.Trim().ToLower() && u.Password == pw)
                .FirstOrDefault();

            if (check == null)
            {
                TempData["ErrorMessage"] = "Tên đăng nhập hoặc mật khẩu không đúng!";
                return RedirectToAction("Login");
            }

            // Kiểm tra tài khoản có bị khóa không
            if (check.IsActive == false)
            {
                TempData["ErrorMessage"] = "Tài khoản của bạn đã bị khóa!";
                return RedirectToAction("Login");
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
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Account/Logout
        public IActionResult Logout()
        {
            Functions.ClearSession();
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Info
        public IActionResult Info()
        {
            if (Functions._UserID == 0)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.Find(Functions._UserID);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Account/Settings
        public IActionResult Settings()
        {
            if (Functions._UserID == 0)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.Find(Functions._UserID);
            if (user == null)
            {
                return NotFound();
            }

            // Clear password for security
            user.Password = null;
            return View(user);
        }

        // POST: Account/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Settings(tblUser user, IFormFile? AvatarFile)
        {
            if (Functions._UserID == 0)
            {
                return RedirectToAction("Login");
            }

            var existingUser = _context.Users.Find(Functions._UserID);
            if (existingUser == null)
            {
                return NotFound();
            }

            // Validate input
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
                    // Check if email is taken by another user
                    bool emailTaken = _context.Users.Any(u => u.UserID != Functions._UserID && u.Email != null && u.Email.ToLower() == user.Email.Trim().ToLower());
                    if (emailTaken)
                    {
                        ModelState.AddModelError("Email", "Email đã được sử dụng");
                    }
                }
            }

            // Validate phone if provided
            if (!string.IsNullOrWhiteSpace(user.Phone))
            {
                var vnPhonePattern = new Regex(@"^(0\d{9}|\+84\d{9})$");
                if (!vnPhonePattern.IsMatch(user.Phone.Trim()))
                {
                    ModelState.AddModelError("Phone", "Số điện thoại không hợp lệ (định dạng: 0xxxxxxxxx hoặc +84xxxxxxxxx)");
                }
            }

            // Handle avatar upload
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

                        // Delete old avatar if exists
                        if (!string.IsNullOrEmpty(existingUser.Avatar))
                        {
                            string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingUser.Avatar.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        existingUser.Avatar = "/files/avatars/" + fileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("Avatar", "Lỗi upload ảnh: " + ex.Message);
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                // Reload user data
                user.UserID = existingUser.UserID;
                user.UserName = existingUser.UserName;
                user.Password = null;
                user.Role = existingUser.Role;
                user.CreatedDate = existingUser.CreatedDate;
                user.IsActive = existingUser.IsActive;
                return View(user);
            }

            // Update user information
            existingUser.FullName = user.FullName?.Trim();
            existingUser.Email = user.Email?.Trim().ToLower();
            existingUser.Phone = user.Phone?.Trim();
            existingUser.DateOfBirth = user.DateOfBirth;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Info");
        }

        // GET: Account/ChangePassword
        public IActionResult ChangePassword()
        {
            if (Functions._UserID == 0)
            {
                return RedirectToAction("Login");
            }
            return View(new ChangePasswordViewModel());
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (Functions._UserID == 0)
            {
                return RedirectToAction("Login");
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
            var user = _context.Users.Find(Functions._UserID);
            if (user == null)
            {
                return NotFound();
            }
            string currentPwHash = Functions.MD5Password(model.CurrentPassword);
            if (user.Password != currentPwHash)
            {
                ViewBag.Message = "Mật khẩu hiện tại không đúng.";
                return View(model);
            }
            user.Password = Functions.MD5Password(model.NewPassword);
            _context.SaveChanges();
            ViewBag.Message = "Đổi mật khẩu thành công!";
            return View(new ChangePasswordViewModel());
        }
                // GET: Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ViewBag.Message = "Vui lòng nhập email.";
                return View(model);
            }
            var user = _context.Users.FirstOrDefault(u => u.Email != null && u.Email.ToLower() == model.Email.Trim().ToLower());
            if (user == null)
            {
                ViewBag.Message = "Email không tồn tại trong hệ thống.";
                return View(model);
            }
            // Sinh token đơn giản
            string token = Guid.NewGuid().ToString();
            // Lưu token vào MemoryCache trong 15 phút
            _cache.Set($"ResetToken_{user.Email}", token, new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            });
            // Tạo link reset
            var resetLink = Url.Action("ResetPassword", "Account", new { email = user.Email, token = token }, Request.Scheme);
            // Gửi email thực tế bằng MailKit
            string subject = "Đặt lại mật khẩu LibraHub";
            string htmlMessage = $@"<p>Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản LibraHub.</p>
                <p>Nhấn vào liên kết sau để đặt lại mật khẩu:</p>
                <p><a href='{resetLink}'>{resetLink}</a></p>
                <p>Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>";
            try
            {
                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    EmailHelper.SendEmailAsync(user.Email, subject, htmlMessage).Wait();
                    ViewBag.Message = $"Đã gửi liên kết đặt lại mật khẩu tới email: {user.Email}. Vui lòng kiểm tra hộp thư.";
                }
                else
                {
                    ViewBag.Message = "Không tìm thấy email của người dùng.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Không gửi được email. Lỗi: {ex.Message}";
            }
            return View();
        }

        // GET: Account/ResetPassword
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("ForgotPassword");
            }
            if (!_cache.TryGetValue($"ResetToken_{email}", out object? tokenObj) || tokenObj is not string savedToken || savedToken != token)
            {
                ViewBag.Message = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return View(new ResetPasswordViewModel { Email = email, Token = token });
            }
            var model = new ResetPasswordViewModel { Email = email, Token = token };
            return View(model);
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Token))
            {
                ViewBag.Message = "Yêu cầu không hợp lệ.";
                return View(model);
            }
            // Kiểm tra token trong MemoryCache
            if (!_cache.TryGetValue($"ResetToken_{model.Email}", out object? tokenObj) || tokenObj is not string savedToken || savedToken != model.Token)
            {
                ViewBag.Message = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return View(model);
            }
            if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 6)
            {
                ViewBag.Message = "Mật khẩu mới tối thiểu 6 ký tự.";
                return View(model);
            }
            if (model.NewPassword != model.ConfirmPassword)
            {
                ViewBag.Message = "Xác nhận mật khẩu không khớp.";
                return View(model);
            }
            var user = _context.Users.FirstOrDefault(u => u.Email != null && u.Email.ToLower() == model.Email.Trim().ToLower());
            if (user == null)
            {
                ViewBag.Message = "Tài khoản không tồn tại.";
                return View(model);
            }
            user.Password = Functions.MD5Password(model.NewPassword);
            _context.SaveChanges();
            ViewBag.Message = "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập với mật khẩu mới.";
            return View(new ResetPasswordViewModel { Email = model.Email });
        }
    }
}

