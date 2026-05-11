using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Controllers
{
    public class BlogController : Controller
    {
        private readonly DataContext _db;

        public BlogController(DataContext db)
        {
            _db = db;
        }

        // GET: /Blog
        public async Task<IActionResult> Index(string? category, int page = 1)
        {
            const int pageSize = 9;
            var query = _db.Posts
                .Where(p => p.IsPublished)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category == category);

            var total = await query.CountAsync();
            var posts = await query
                .OrderByDescending(p => p.IsPinned)
                .ThenByDescending(p => p.PublishedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            var categories = await _db.Posts
                .Where(p => p.IsPublished && p.Category != null)
                .Select(p => p.Category!)
                .Distinct()
                .ToListAsync();

            ViewBag.Category   = category;
            ViewBag.Categories = categories;
            ViewBag.Page       = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalItems = total;
            return View(posts);
        }

        // GET: /Blog/{slug}
        public async Task<IActionResult> Detail(string slug)
        {
            var post = await _db.Posts
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
            if (post == null) return NotFound();

            post.ViewCount++;
            await _db.SaveChangesAsync();

            var related = await _db.Posts
                .Where(p => p.IsPublished && p.PostId != post.PostId &&
                            (p.Category == post.Category || post.Category == null))
                .OrderByDescending(p => p.PublishedAt)
                .Take(3)
                .ToListAsync();

            ViewBag.Related = related;
            return View(post);
        }
    }
}
