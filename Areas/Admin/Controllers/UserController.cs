using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Models;
using QuanLyThuVien.Attributes;
using Microsoft.AspNetCore.Mvc.Rendering;
using PagedList.Core;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace QuanLyThuVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class UserController : Controller
    {
        private readonly DataContext _context;
        public UserController(DataContext context)
        {
            _context = context;
        }

        private static string NormalizeName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var collapsed = Regex.Replace(name.Trim(), "\\s+", " ");
            var textInfo = new CultureInfo("vi-VN").TextInfo;
            return textInfo.ToTitleCase(collapsed.ToLower());
        }

        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return phone;
            phone = phone.Trim();
            if (phone.StartsWith("+84")) return phone;
            if (phone.StartsWith("0") && phone.Length >= 9 && phone.Length <= 12)
            {
                return "+84" + phone.Substring(1);
            }
            return phone;
        }


        [HttpGet] 
        [Route("Admin/User/Index/{page?}")]
        public IActionResult Index(int page = 1)
        {
            int pageSize = 5;
            var users = _context.Users.OrderBy(u => u.UserID);
            var models = new PagedList<tblUser>(users, page, pageSize);

            return View(models);
        }


        [HttpGet]
        [Route("Admin/User/Create")]
        public IActionResult Create()
        {
            ViewBag.RoleList = new List<SelectListItem>()
            {
                new SelectListItem { Text = "Admin", Value = "Admin" },
                new SelectListItem { Text = "User", Value = "User" }
            };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/User/Create")]
        public IActionResult Create(tblUser user, IFormFile? AvatarFile, string? AvatarPath)
        {
            user.FullName = NormalizeName(user.FullName);
            if (string.IsNullOrWhiteSpace(user.Password))
            {
                ModelState.AddModelError("Password", "Mật khẩu là bắt buộc");
            }
            if (!string.IsNullOrWhiteSpace(user.UserName))
            {
                var userNamePattern = new Regex("^[a-z0-9]{4,100}$");
                if (!userNamePattern.IsMatch(user.UserName))
                {
                    ModelState.AddModelError("UserName", "Tên đăng nhập chỉ gồm chữ thường và số (4-100 ký tự)");
                }
            }
            if (!string.IsNullOrWhiteSpace(user.Password))
            {
                var passwordPattern = new Regex(@"^.{6,200}$");
                if (!passwordPattern.IsMatch(user.Password))
                {
                    ModelState.AddModelError("Password", "Mật khẩu tối thiểu 6 ký tự");
                }
            }
            if (!string.IsNullOrWhiteSpace(user.Phone))
            {
                var vnPhonePattern = new Regex(@"^(0\d{9}|\+84\d{9})$");
                if (!vnPhonePattern.IsMatch(user.Phone.Trim()))
                {
                    ModelState.AddModelError("Phone", "Số điện thoại không hợp lệ (0xxxxxxxxx hoặc +84xxxxxxxxx)");
                }
            }

            if (user.DateOfBirth.HasValue)
            {
                var today = DateTime.Today;
                var minDate = today.AddYears(-100);
                
                if (user.DateOfBirth.Value.Date > today)
                {
                    ModelState.AddModelError("DateOfBirth", "Ngày sinh không thể là ngày trong tương lai");
                }
                else if (user.DateOfBirth.Value.Date < minDate)
                {
                    ModelState.AddModelError("DateOfBirth", "Ngày sinh không hợp lệ (tối đa 100 tuổi)");
                }
            }

            if (!string.IsNullOrWhiteSpace(user.UserName))
            {
                var existUser = _context.Users.FirstOrDefault(u => u.UserName != null && u.UserName.ToLower() == user.UserName.ToLower());
                if (existUser != null)
                {
                    ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại");
                }
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var existEmail = _context.Users.FirstOrDefault(u => u.Email != null && u.Email.ToLower() == user.Email.ToLower());
                if (existEmail != null)
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng");
                }
            }

            if (ModelState.IsValid)
            {
                // Ưu tiên upload từ máy (AvatarFile) nếu có, nếu không thì dùng từ File Manager (AvatarPath)
                if (AvatarFile != null && AvatarFile.Length > 0)
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
                else if (!string.IsNullOrEmpty(AvatarPath))
                {
                    user.Avatar = AvatarPath;
                }

                if (!string.IsNullOrWhiteSpace(user.Phone))
                {
                    user.Phone = NormalizePhone(user.Phone);
                }
                user.FullName = NormalizeName(user.FullName);
                user.CreatedDate = DateTime.Now;
                user.IsActive = user.IsActive ?? true;
                
                // Hash password nếu có
                if (!string.IsNullOrWhiteSpace(user.Password))
                {
                    user.Password = QuanLyThuVien.Utilities.Functions.MD5Password(user.Password);
                }
                
                // Đảm bảo Role có giá trị
                if (string.IsNullOrWhiteSpace(user.Role))
                {
                    user.Role = "User";
                }
                
                _context.Users.Add(user);
                _context.SaveChanges();
                
                return RedirectToAction("Index");
            }
            ViewBag.RoleList = new List<SelectListItem>()
            {
                new SelectListItem { Text = "Admin", Value = "Admin" },
                new SelectListItem { Text = "User", Value = "User" }
            };
            return View(user);
        }

        //  Delete
        [HttpGet]
        [Route("Admin/User/Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [Route("Admin/User/Delete/{id}")]
        public IActionResult DeleteConfirmed(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            // Lấy danh sách các phiếu mượn chưa trả
            var borrows = _context.Borrows
                .Where(b => b.UserID == id && b.Status == "Borrowing")
                .Include(b => b.BorrowDetails!)
                    .ThenInclude(d => d.Book)
                .ToList()
                .Select(b => new {
                    b.BorrowID,
                    b.BorrowDate,
                    b.DueDate,
                    b.Status,
                    Details = b.BorrowDetails?.Select(d => new {
                        BookID = d.Book?.BookID ?? 0,
                        Title = d.Book?.Title ?? "Không rõ",
                        d.Quantity,
                        d.BorrowStatus,
                        d.ReturnDate,
                        d.Note
                    }).ToList()
                }).ToList();

            if (borrows.Any())
            {
                // Có sách đang mượn, không xóa, trả về view Delete với cảnh báo
                ViewBag.Borrowing = (object)borrows;
                ViewBag.Message = "Không thể xóa. Người dùng này đang mượn sách.";
                return View("Delete", user);
            }

            _context.Users.Remove(user);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        //  Details
        [HttpGet]
        [Route("Admin/User/Details/{id}")]
        public IActionResult Details(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserID == id);
            if (user == null) return NotFound();
            return View(user);
        }
        [HttpGet]
        [Route("Admin/User/Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            ViewBag.RoleList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Admin", Value = "admin" },
                new SelectListItem { Text = "User", Value = "user" }
            };

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(tblUser user, IFormFile? AvatarFile, string? AvatarPath, string? RemoveAvatar)
        {
            var existingUser = _context.Users.Find(user.UserID);
            if (existingUser == null) return NotFound();
            ModelState.Remove("Password");

            user.FullName = NormalizeName(user.FullName);

            if (!string.IsNullOrWhiteSpace(user.UserName) && !string.Equals(user.UserName, existingUser.UserName, StringComparison.Ordinal))
            {
                var userNamePattern = new Regex("^[a-z0-9]{4,100}$");
                if (!userNamePattern.IsMatch(user.UserName))
                {
                    ModelState.AddModelError("UserName", "Tên đăng nhập chỉ gồm chữ thường và số (4-100 ký tự)");
                }
            }
            if (!string.IsNullOrWhiteSpace(user.Phone))
            {
                var vnPhonePattern = new Regex(@"^(0\d{9}|\+84\d{9})$");
                if (!vnPhonePattern.IsMatch(user.Phone.Trim()))
                {
                    ModelState.AddModelError("Phone", "Số điện thoại không hợp lệ (0xxxxxxxxx hoặc +84xxxxxxxxx)");
                }
            }

            // Kiểm tra ngày sinh hợp lý
            if (user.DateOfBirth.HasValue)
            {
                var today = DateTime.Today;
                var minDate = today.AddYears(-100); // Tối đa 100 tuổi
                
                if (user.DateOfBirth.Value.Date > today)
                {
                    ModelState.AddModelError("DateOfBirth", "Ngày sinh không thể là ngày trong tương lai");
                }
                else if (user.DateOfBirth.Value.Date < minDate)
                {
                    ModelState.AddModelError("DateOfBirth", "Ngày sinh không hợp lệ (tối đa 100 tuổi)");
                }
            }

            var incomingEmail = user.Email?.Trim();
            var currentEmail = existingUser.Email?.Trim();
            if (!string.IsNullOrWhiteSpace(incomingEmail))
            {
                var isEmailChanged = !string.Equals(incomingEmail, currentEmail, StringComparison.OrdinalIgnoreCase);
                if (isEmailChanged)
                {
                    var emailOwner = _context.Users.FirstOrDefault(u => u.Email != null && u.Email.ToLower() == incomingEmail.ToLower() && u.UserID != user.UserID);
                    if (emailOwner != null)
                    {
                        ModelState.AddModelError("Email", "Email đã được sử dụng");
                    }
                }
            }

            // Kiểm tra trùng username nếu người dùng đổi sang username mới
            if (!string.IsNullOrWhiteSpace(user.UserName) && !string.Equals(user.UserName, existingUser.UserName, StringComparison.OrdinalIgnoreCase))
            {
                var existUserName = _context.Users.FirstOrDefault(u => u.UserName != null && u.UserName.ToLower() == user.UserName.ToLower() && u.UserID != user.UserID);
                if (existUserName != null)
                {
                    ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // ===== CẬP NHẬT THÔNG TIN =====
                    existingUser.FullName = user.FullName;
                    existingUser.Email = user.Email;
                    existingUser.Phone = user.Phone;
                    existingUser.Role = user.Role ?? "User";
                    existingUser.DateOfBirth = user.DateOfBirth;
                    existingUser.IsActive = user.IsActive ?? false;

                    if (AvatarFile != null && AvatarFile.Length > 0)
                    {
                        string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "avatars");
                        if (!Directory.Exists(uploadPath))
                            Directory.CreateDirectory(uploadPath);

                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AvatarFile.FileName);
                        string filePath = Path.Combine(uploadPath, fileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        AvatarFile.CopyTo(stream);
                        existingUser.Avatar = "/files/avatars/" + fileName;
                    }
                    else if (!string.IsNullOrEmpty(AvatarPath))
                    {
                        existingUser.Avatar = AvatarPath;
                    }
                    else if (!string.IsNullOrEmpty(RemoveAvatar) && RemoveAvatar == "true")
                    {
                        existingUser.Avatar = null;
                    }

                    if (!string.IsNullOrWhiteSpace(user.Phone))
                    {
                        existingUser.Phone = NormalizePhone(user.Phone);
                    }
                    
                    _context.Users.Update(existingUser);
                    _context.SaveChanges();
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật: " + ex.Message);
                }
            }

            ViewBag.RoleList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Admin", Value = "admin" },
                new SelectListItem { Text = "User", Value = "user" }
            };
            return View(existingUser);
        }

    }
}
