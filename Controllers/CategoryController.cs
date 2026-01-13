using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;
using PagedList.Core;

namespace QuanLyThuVien.Controllers
{
    public class CategoryController : Controller
    {
        private readonly DataContext _context;

        public CategoryController(DataContext context)
        {
            _context = context;
        }

        // GET: Category/Index
        [HttpGet]
        public IActionResult Index(int page = 1, string searchString = "")
        {
            int pageSize = 6;
            var categories = _context.Categories
                .Where(c => c.IsActive == true)
                .AsQueryable();

            // Tìm kiếm theo tên danh mục
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                categories = categories.Where(c => c.CategoryName != null && c.CategoryName.Contains(searchString));
            }

            categories = categories.OrderBy(c => c.CategoryName);
            var models = new PagedList<tblCategory>(categories, page, pageSize);

            ViewBag.SearchString = searchString;

            return View(models);
        }

        // GET: Category/Details/{id}
        [HttpGet]
        public IActionResult Details(int id)
        {
            var category = _context.Categories
                .FirstOrDefault(c => c.CategoryID == id && c.IsActive == true);

            if (category == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục!";
                return RedirectToAction("Index");
            }

            // Lấy danh sách sách của danh mục
            var books = _context.Books
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Where(b => b.CategoryID == id && b.IsActive == true)
                .ToList();
            
            ViewBag.Books = books;

            return View(category);
        }
    }
}
