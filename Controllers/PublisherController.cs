using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;
using PagedList.Core;

namespace QuanLyThuVien.Controllers
{
    public class PublisherController : Controller
    {
        private readonly DataContext _context;

        public PublisherController(DataContext context)
        {
            _context = context;
        }

        // GET: Publisher/Index
        [HttpGet]
        public IActionResult Index(int page = 1, string searchString = "")
        {
            int pageSize = 12;
            var publishers = _context.Publishers
                .Where(p => p.IsActive == true)
                .AsQueryable();

            // Tìm kiếm theo tên nhà xuất bản
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                publishers = publishers.Where(p => p.PublisherName != null && p.PublisherName.Contains(searchString));
            }

            publishers = publishers.OrderBy(p => p.PublisherName);
            var models = new PagedList<tblPublisher>(publishers, page, pageSize);

            ViewBag.SearchString = searchString;

            return View(models);
        }

        // GET: Publisher/Details/{id}
        [HttpGet]
        public IActionResult Details(int id)
        {
            var publisher = _context.Publishers
                .FirstOrDefault(p => p.PublisherID == id && p.IsActive == true);

            if (publisher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhà xuất bản!";
                return RedirectToAction("Index");
            }

            // Lấy danh sách sách của nhà xuất bản
            var books = _context.Books
                .Include(b => b.Category)
                .Where(b => b.PublisherID == id && b.IsActive == true)
                .ToList();
            
            ViewBag.Books = books;

            return View(publisher);
        }
    }
}
