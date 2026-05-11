using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class PostController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _env;
        private const string IMAGE_FOLDER = "images/posts";

        public PostController(DataContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env     = env;
        }

        private static string ToSlug(string text)
        {
            var map = new Dictionary<string, string>
            {
                {"đ","d"},{"Đ","d"},
                {"à","a"},{"á","a"},{"ả","a"},{"ã","a"},{"ạ","a"},
                {"ă","a"},{"ắ","a"},{"ặ","a"},{"ẳ","a"},{"ẵ","a"},{"ằ","a"},
                {"â","a"},{"ấ","a"},{"ầ","a"},{"ẫ","a"},{"ẩ","a"},{"ậ","a"},
                {"è","e"},{"é","e"},{"ẻ","e"},{"ẽ","e"},{"ẹ","e"},
                {"ê","e"},{"ề","e"},{"ế","e"},{"ể","e"},{"ễ","e"},{"ệ","e"},
                {"ì","i"},{"í","i"},{"ỉ","i"},{"ĩ","i"},{"ị","i"},
                {"ò","o"},{"ó","o"},{"ỏ","o"},{"õ","o"},{"ọ","o"},
                {"ô","o"},{"ồ","o"},{"ố","o"},{"ổ","o"},{"ỗ","o"},{"ộ","o"},
                {"ơ","o"},{"ờ","o"},{"ớ","o"},{"ở","o"},{"ỡ","o"},{"ợ","o"},
                {"ù","u"},{"ú","u"},{"ủ","u"},{"ũ","u"},{"ụ","u"},
                {"ư","u"},{"ừ","u"},{"ứ","u"},{"ử","u"},{"ữ","u"},{"ự","u"},
                {"ỳ","y"},{"ý","y"},{"ỷ","y"},{"ỹ","y"},{"ỵ","y"},
            };
            var sb = new StringBuilder(text.ToLower().Trim());
            foreach (var (from, to) in map) sb.Replace(from, to);
            var s = Regex.Replace(sb.ToString(), @"[^a-z0-9\s-]", "");
            s = Regex.Replace(s, @"\s+", "-");
            return Regex.Replace(s, @"-+", "-").Trim('-');
        }

        private async Task<string> UniqueSlug(string slug, int excludeId = 0)
        {
            var candidate = slug; var i = 1;
            while (await _context.Posts.AnyAsync(p => p.Slug == candidate && p.PostId != excludeId))
                candidate = $"{slug}-{i++}";
            return candidate;
        }

        private async Task<string?> SaveImage(IFormFile file)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowed.Contains(ext)) return null;
            var dir = Path.Combine(_env.WebRootPath, IMAGE_FOLDER);
            Directory.CreateDirectory(dir);
            var name = $"{Guid.NewGuid()}{ext}";
            using var stream = new FileStream(Path.Combine(dir, name), FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/{IMAGE_FOLDER}/{name}";
        }

        private void DeleteImage(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var full = Path.Combine(_env.WebRootPath, path.TrimStart('/'));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }

        public async Task<IActionResult> Index(string? search, string? published, int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Posts.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Title.Contains(search) ||
                                        (p.Category != null && p.Category.Contains(search)));
            if (published == "1") query = query.Where(p => p.IsPublished);
            if (published == "0") query = query.Where(p => !p.IsPublished);

            var total = await query.CountAsync();
            var posts = await query
                .OrderByDescending(p => p.IsPinned)
                .ThenByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search; ViewBag.Published = published;
            ViewBag.Page = page; ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = total;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            return View(posts);
        }

        public IActionResult Create() => View(new tblPost());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(tblPost model, IFormFile? thumbnailFile)
        {
            ModelState.Remove("Slug"); ModelState.Remove("ThumbnailImage");
            if (!ModelState.IsValid) return View(model);
            model.Slug = await UniqueSlug(ToSlug(model.Title));
            model.CreatedAt = DateTime.Now;
            if (thumbnailFile != null) model.ThumbnailImage = await SaveImage(thumbnailFile);
            if (model.IsPublished) model.PublishedAt = DateTime.Now;
            _context.Posts.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã thêm bài viết \"{model.Title}\".";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            return post == null ? NotFound() : View(post);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, tblPost model,
            IFormFile? thumbnailFile, bool removeThumb = false)
        {
            if (id != model.PostId) return BadRequest();
            ModelState.Remove("Slug"); ModelState.Remove("ThumbnailImage");
            if (!ModelState.IsValid) return View(model);
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            post.Slug = post.Title != model.Title ? await UniqueSlug(ToSlug(model.Title), id) : post.Slug;
            if (removeThumb) { DeleteImage(post.ThumbnailImage); post.ThumbnailImage = null; }
            else if (thumbnailFile != null) { DeleteImage(post.ThumbnailImage); post.ThumbnailImage = await SaveImage(thumbnailFile); }

            post.Title = model.Title; post.Summary = model.Summary;
            post.Content = model.Content; post.Category = model.Category;
            post.IsPinned = model.IsPinned;
            post.MetaTitle = model.MetaTitle; post.MetaDescription = model.MetaDescription;
            post.UpdatedAt = DateTime.Now;
            if (model.IsPublished && !post.IsPublished) post.PublishedAt = DateTime.Now;
            else if (!model.IsPublished)                post.PublishedAt = null;
            post.IsPublished = model.IsPublished;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật bài viết \"{post.Title}\".";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublished(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();
            post.IsPublished = !post.IsPublished;
            post.PublishedAt = post.IsPublished ? DateTime.Now : null;
            post.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["Success"] = post.IsPublished ? $"Đã đăng bài \"{post.Title}\"." : $"Đã ẩn bài \"{post.Title}\".";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePinned(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();
            post.IsPinned = !post.IsPinned; post.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();
            DeleteImage(post.ThumbnailImage);
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa bài viết \"{post.Title}\".";
            return RedirectToAction(nameof(Index));
        }
    }
}
