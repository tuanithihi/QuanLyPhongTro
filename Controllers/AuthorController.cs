using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;
using PagedList.Core;

namespace QuanLyThuVien.Controllers
{
    public class AuthorController : Controller
    {
        private readonly DataContext _context;

        public AuthorController(DataContext context)
        {
            _context = context;
        }

        // GET: Author/Index
        [HttpGet]
        public IActionResult Index(int page = 1, string searchString = "")
        {
            int pageSize = 6;
            var authors = _context.Authors
                .Where(a => a.IsActive == true)
                .AsQueryable();

            // Tìm kiếm theo tên tác giả
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                authors = authors.Where(a => a.AuthorName != null && a.AuthorName.Contains(searchString));
            }

            authors = authors.OrderBy(a => a.AuthorName);
            var models = new PagedList<tblAuthor>(authors, page, pageSize);

            ViewBag.SearchString = searchString;

            return View(models);
        }

        // GET: Author/Details/{id}
        [HttpGet]
        public IActionResult Details(int id)
        {
            var author = _context.Authors
                .Include(a => a.BookAuthors)
                    .ThenInclude(ba => ba.Book)
                        .ThenInclude(b => b.Category)
                .FirstOrDefault(a => a.AuthorID == id && a.IsActive == true);

            if (author == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tác giả!";
                return RedirectToAction("Index");
            }

            return View(author);
        }
    }
}
