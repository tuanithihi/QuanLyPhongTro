using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Models;
using QuanLyThuVien.Attributes;
using Microsoft.AspNetCore.Mvc.Rendering;
using PagedList.Core;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace QuanLyThuVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class PublisherController : Controller
    {
        private readonly DataContext _context;
        public PublisherController(DataContext context)
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

        // Index with pagination (like Author)
        [HttpGet]
        [Route("Admin/Publisher/Index/{page?}")]
        public IActionResult Index(int page = 1)
        {
            int pageSize = 5;
            var pubs = _context.Publishers.OrderBy(p => p.PublisherID);
            var models = new PagedList<tblPublisher>(pubs, page, pageSize);
            return View(models);
        }

        [HttpGet]
        [Route("Admin/Publisher/Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Publisher/Create")]
        public IActionResult Create(tblPublisher publisher, IFormFile? AvatarFile, string? AvatarPath)
        {
            publisher.PublisherName = NormalizeName(publisher.PublisherName);

            // Required email
            if (string.IsNullOrWhiteSpace(publisher.Email))
            {
                ModelState.AddModelError("Email", "Email là bắt buộc");
            }

            // Duplicate: same name AND same email is considered duplicate; allow same name with different email
            if (!string.IsNullOrWhiteSpace(publisher.PublisherName))
            {
                var existSameNameEmail = _context.Publishers.Any(p =>
                    p.PublisherName != null && p.PublisherName.ToLower() == publisher.PublisherName.ToLower() &&
                    p.Email == publisher.Email);
                if (existSameNameEmail)
                {
                    ModelState.AddModelError("PublisherName", "Đã tồn tại NXB cùng tên và email");
                }
            }

            if (!string.IsNullOrWhiteSpace(publisher.Email))
            {
                var existEmail = _context.Publishers.Any(p => p.Email == publisher.Email);
                if (existEmail)
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng");
                }
            }

            if (ModelState.IsValid)
            {
                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "publishers");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AvatarFile.FileName);
                    string filePath = Path.Combine(uploadPath, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    AvatarFile.CopyTo(stream);
                    publisher.Avatar = "/files/publishers/" + fileName;
                }
                else if (!string.IsNullOrEmpty(AvatarPath))
                {
                    publisher.Avatar = AvatarPath;
                }

                publisher.PublisherName = NormalizeName(publisher.PublisherName);
                publisher.CreatedAt = DateTime.Now;
                publisher.IsActive = publisher.IsActive ?? true;
                _context.Publishers.Add(publisher);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(publisher);
        }

        [HttpGet]
        [Route("Admin/Publisher/Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var pub = _context.Publishers.Find(id);
            if (pub == null) return NotFound();
            return View(pub);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(tblPublisher publisher, IFormFile? AvatarFile, string? AvatarPath, string? RemoveAvatar)
        {
            var existing = _context.Publishers.Find(publisher.PublisherID);
            if (existing == null) return NotFound();

            publisher.PublisherName = NormalizeName(publisher.PublisherName);

            // Email required
            if (string.IsNullOrWhiteSpace(publisher.Email))
            {
                ModelState.AddModelError("Email", "Email là bắt buộc");
            }

            // Duplicate rules: block same name + same email (excluding current)
            if (!string.IsNullOrWhiteSpace(publisher.PublisherName))
            {
                var dup = _context.Publishers.Any(p => p.PublisherID != publisher.PublisherID &&
                    p.PublisherName != null && p.PublisherName.ToLower() == publisher.PublisherName.ToLower() &&
                    p.Email == publisher.Email);
                if (dup)
                {
                    ModelState.AddModelError("PublisherName", "Đã tồn tại NXB cùng tên và email");
                }
            }

            // Email unique
            var incomingEmail = publisher.Email?.Trim();
            var currentEmail = existing.Email?.Trim();
            if (!string.IsNullOrWhiteSpace(incomingEmail) && !string.Equals(incomingEmail, currentEmail, StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = _context.Publishers.Any(p => p.PublisherID != publisher.PublisherID && p.Email == incomingEmail);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existing.PublisherName = publisher.PublisherName;
                    existing.Email = publisher.Email;
                    existing.Address = publisher.Address;
                    existing.IsActive = publisher.IsActive ?? false;

                    if (AvatarFile != null && AvatarFile.Length > 0)
                    {
                        string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "publishers");
                        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AvatarFile.FileName);
                        string filePath = Path.Combine(uploadPath, fileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        AvatarFile.CopyTo(stream);
                        existing.Avatar = "/files/publishers/" + fileName;
                    }
                    else if (!string.IsNullOrEmpty(AvatarPath))
                    {
                        existing.Avatar = AvatarPath;
                    }
                    else if (!string.IsNullOrEmpty(RemoveAvatar) && RemoveAvatar == "true")
                    {
                        existing.Avatar = null;
                    }

                    _context.Publishers.Update(existing);
                    _context.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật: " + ex.Message);
                }
            }
            return View(existing);
        }

        [HttpGet]
        [Route("Admin/Publisher/Details/{id}")]
        public IActionResult Details(int id)
        {
            var pub = _context.Publishers.FirstOrDefault(p => p.PublisherID == id);
            if (pub == null) return NotFound();
            return View(pub);
        }

        [HttpGet]
        [Route("Admin/Publisher/Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var pub = _context.Publishers.Find(id);
            if (pub == null) return NotFound();
            return View(pub);
        }

        [HttpPost, ActionName("Delete")]
        [Route("Admin/Publisher/Delete/{id}")]
        public IActionResult DeleteConfirmed(int id)
        {
            var pub = _context.Publishers.Find(id);
            if (pub == null) return NotFound();
            _context.Publishers.Remove(pub);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}