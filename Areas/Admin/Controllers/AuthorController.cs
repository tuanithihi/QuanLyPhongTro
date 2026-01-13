using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyThuVien.Models;
using QuanLyThuVien.Attributes;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace QuanLyThuVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class AuthorController : Controller
    {
        private readonly DataContext _context;
        public AuthorController(DataContext context)
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

     
        [HttpGet] 
        [Route("Admin/Author/Index/{page?}")]
        public IActionResult Index(int page = 1)
        {
            int pageSize = 5;
            var authors = _context.Authors.OrderBy(a => a.AuthorID);
            var models = new PagedList.Core.PagedList<tblAuthor>(authors, page, pageSize);

            return View(models);
        }
        [HttpGet]
        [Route("Admin/Author/Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var author = _context.Authors.Find(id);
            if (author == null) return NotFound();
            return View(author);
        }

        [HttpPost, ActionName("Delete")]
        [Route("Admin/Author/Delete/{id}")]
        public IActionResult DeleteConfirmed(int id)
        {
            var author = _context.Authors.Find(id);
            if (author == null) return NotFound();
            _context.Authors.Remove(author);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("Admin/Author/Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Author/Create")]
        public IActionResult Create(tblAuthor author, IFormFile? AvatarFile, string? AvatarPath)
        {
            
            author.AuthorName = NormalizeName(author.AuthorName);
            var name = author.AuthorName?.Trim();
            if (string.IsNullOrWhiteSpace(author.Email))
            {
                ModelState.AddModelError("Email", "Email là bắt buộc");
            }
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (string.IsNullOrWhiteSpace(author.AuthorName))
            {
                ModelState.AddModelError("AuthorName", "Tên tác giả là bắt buộc");
            }

                var existSameNameAndDob = _context.Authors.Any(a =>
                    a.AuthorName != null && a.AuthorName.ToLower() == name.ToLower() &&
                    a.DateOfBirth.HasValue && author.DateOfBirth.HasValue &&
                    a.DateOfBirth.Value.Date == author.DateOfBirth.Value.Date);
                if (existSameNameAndDob)
                {
                    ModelState.AddModelError("AuthorName", "Đã tồn tại tác giả cùng tên và ngày sinh");
                }
            }
            if (!string.IsNullOrWhiteSpace(author.Email))
            {
                var existEmail = _context.Users.FirstOrDefault(u => u.Email == author.Email);
                if (existEmail != null)
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng");
                }
            }


            if (ModelState.IsValid)
            {
                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "authors");
                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AvatarFile.FileName);
                    string filePath = Path.Combine(uploadPath, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    AvatarFile.CopyTo(stream);
                    author.Avatar = "/files/authors/" + fileName;
                }
                else if (!string.IsNullOrEmpty(AvatarPath))
                {
                    author.Avatar = AvatarPath;
                }

                // Chuẩn hóa lại tên tác giả trước khi lưu
                author.AuthorName = NormalizeName(author.AuthorName);
                author.CreatedAt = DateTime.Now;
                author.IsActive = author.IsActive ?? true; // Mặc định là true nếu không có giá trị

                _context.Authors.Add(author);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(author);
        }

        //  Details
        [HttpGet]
        [Route("Admin/Author/Details/{id}")]
        public IActionResult Details(int id)
        {
            var author = _context.Authors.FirstOrDefault(a => a.AuthorID == id);
            if (author == null) return NotFound();
            return View(author);
        }

        [HttpGet]
        [Route("Admin/Author/Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var author = _context.Authors.Find(id);
            if (author == null) return NotFound();
            return View(author);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(tblAuthor author, IFormFile? AvatarFile, string? AvatarPath, string? RemoveAvatar)
        {
            var existingAuthor = _context.Authors.Find(author.AuthorID);
            if (existingAuthor == null) return NotFound();

            // Kiểm tra trùng tên/email khi chỉnh sửa (nếu thay đổi)
            if (string.IsNullOrWhiteSpace(author.Email))
            {
                ModelState.AddModelError("Email", "Email là bắt buộc");
            }
            author.AuthorName = NormalizeName(author.AuthorName);
            var incomingName = author.AuthorName?.Trim();
            var currentName = existingAuthor.AuthorName?.Trim();
            if (!string.IsNullOrWhiteSpace(incomingName))
            {
                // Chỉ chặn khi có bản ghi trùng cả tên và ngày sinh (khác id hiện tại)
                var sameNameDobExists = _context.Authors.Any(a =>
                    a.AuthorID != author.AuthorID &&
                    a.AuthorName != null && a.AuthorName.ToLower() == incomingName.ToLower() &&
                    a.DateOfBirth.HasValue && author.DateOfBirth.HasValue &&
                    a.DateOfBirth.Value.Date == author.DateOfBirth.Value.Date);
                if (sameNameDobExists)
                {
                    ModelState.AddModelError("AuthorName", "Đã tồn tại tác giả cùng tên và ngày sinh");
                }
            }
            var incomingEmail = author.Email?.Trim();
            var currentEmail = existingAuthor.Email?.Trim();
            if (!string.IsNullOrWhiteSpace(incomingEmail) && !string.Equals(incomingEmail, currentEmail, StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = _context.Authors.Any(a => a.AuthorID != author.AuthorID && a.Email == incomingEmail);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng");
                }
            }

            // Không ràng buộc ngày sinh cho tác giả (có thể là tác giả cổ xưa hoặc đã qua đời)

            if (ModelState.IsValid)
            {
                try
                {
                    // ===== CẬP NHẬT THÔNG TIN =====
                    existingAuthor.AuthorName = author.AuthorName;
                    existingAuthor.DateOfBirth = author.DateOfBirth;
                    existingAuthor.Biography = author.Biography;
                    existingAuthor.Email = author.Email;
                    existingAuthor.IsActive = author.IsActive ?? false;

                    // ===== XỬ LÝ ẢNH =====
                    // Ưu tiên ảnh mới (upload hoặc từ File Manager). Chỉ xóa khi không có ảnh mới.
                    if (AvatarFile != null && AvatarFile.Length > 0)
                    {
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "authors");
                        if (!Directory.Exists(uploadPath))
                            Directory.CreateDirectory(uploadPath);

                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AvatarFile.FileName);
                        string filePath = Path.Combine(uploadPath, fileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        AvatarFile.CopyTo(stream);
                    existingAuthor.Avatar = "/files/authors/" + fileName;
                    }
                    // Nếu chọn ảnh từ File Manager
                    else if (!string.IsNullOrEmpty(AvatarPath))
                    {
                        existingAuthor.Avatar = AvatarPath;
                    }
                    else if (!string.IsNullOrEmpty(RemoveAvatar) && RemoveAvatar == "true")
                    {
                        existingAuthor.Avatar = null;
                    }

                    _context.Authors.Update(existingAuthor);
                    _context.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật: " + ex.Message);
                }
            }

            // Nếu có lỗi validation hoặc exception, trả về view với dữ liệu hiện tại
            return View(existingAuthor);
        }

    }
}
