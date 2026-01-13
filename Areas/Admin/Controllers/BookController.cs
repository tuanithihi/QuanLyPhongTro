using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;
using QuanLyThuVien.Utilities;
using QuanLyThuVien.Attributes;
using PagedList.Core;
using System.IO;

namespace QuanLyThuVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class BookController : Controller
    {
        private readonly DataContext _context;

        public BookController(DataContext context)
        {
            _context = context;
        }

        // GET: Admin/Book/Index
        [HttpGet]
        [Route("Admin/Book/Index/{page?}")]
        [Route("Admin/Book/")]
        public IActionResult Index(int page = 1, string searchString = "", int? categoryId = null, int? publisherId = null, int? authorId = null)
        {
            if (!Functions.IsAdmin())
                return RedirectToAction("Index", "Login");

            int pageSize = 5;
            var books = _context.Books
    .Include(b => b.Category)
    .Include(b => b.Publisher)
    .Include(b => b.BookAuthors)
        .ThenInclude(ba => ba.Author)
    .AsQueryable();

            // Tìm kiếm theo tên sách
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                books = books.Where(b => b.Title != null && b.Title.Contains(searchString));
            }

            // Lọc theo danh mục
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                books = books.Where(b => b.CategoryID == categoryId.Value);
            }

            // Lọc theo nhà xuất bản
            if (publisherId.HasValue && publisherId.Value > 0)
            {
                books = books.Where(b => b.PublisherID == publisherId.Value);
            }

            // Lọc theo tác giả
            if (authorId.HasValue && authorId.Value > 0)
            {
                books = books.Where(b => b.BookAuthors != null && b.BookAuthors.Any(ba => ba.AuthorID == authorId.Value));
            }

            books = books.OrderByDescending(b => b.CreatedAt);
            var models = new PagedList<tblBook>(books, page, pageSize);

            // ViewBag cho dropdown filters
            ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.IsActive == true), "CategoryID", "CategoryName", categoryId);
            ViewBag.Publishers = new SelectList(_context.Publishers.Where(p => p.IsActive == true), "PublisherID", "PublisherName", publisherId);
            ViewBag.Authors = new SelectList(_context.Authors.Where(a => a.IsActive == true).OrderBy(a => a.AuthorName), "AuthorID", "AuthorName", authorId);
            ViewBag.SearchString = searchString;
            ViewBag.CategoryId = categoryId;
            ViewBag.PublisherId = publisherId;
            ViewBag.AuthorId = authorId;

            return View(models);
        }

        // GET: Admin/Book/Create
        [HttpGet]
        [Route("Admin/Book/Create")]
        public IActionResult Create()
        {
            if (!Functions.IsAdmin())
                return RedirectToAction("Index", "Login");

            ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.IsActive == true), "CategoryID", "CategoryName");
            ViewBag.Publishers = new SelectList(_context.Publishers.Where(p => p.IsActive == true), "PublisherID", "PublisherName");
            ViewBag.Authors = _context.Authors.Where(a => a.IsActive == true).OrderBy(a => a.AuthorName).ToList();
            return View();
        }

        // POST: Admin/Book/Create
        [HttpPost]
        [Route("Admin/Book/Create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(tblBook book, string? CoverImagePath, string? BookFilePath, int[]? AuthorIDs)
        {
            if (!Functions.IsAdmin())
                return RedirectToAction("Index", "Login");

            // Validation
            if (string.IsNullOrWhiteSpace(book.Title))
            {
                ModelState.AddModelError("Title", "Tên sách là bắt buộc");
            }
            else
            {
                // Kiểm tra trùng tên sách
                var exists = _context.Books.Any(b => b.Title != null && b.Title.Trim().ToLower() == book.Title.Trim().ToLower());
                if (exists)
                {
                    ModelState.AddModelError("Title", "Tên sách đã tồn tại");
                }
            }

            if (book.PublishedYear < 1000 || book.PublishedYear > DateTime.Now.Year + 1)
            {
                ModelState.AddModelError("PublishedYear", $"Năm xuất bản phải từ 1000 đến {DateTime.Now.Year + 1}");
            }

            if (book.Quantity < 0)
            {
                ModelState.AddModelError("Quantity", "Số lượng không được âm");
            }

            if (book.CategoryID <= 0)
            {
                ModelState.AddModelError("CategoryID", "Vui lòng chọn danh mục");
            }

            if (book.PublisherID <= 0)
            {
                ModelState.AddModelError("PublisherID", "Vui lòng chọn nhà xuất bản");
            }

            if (ModelState.IsValid)
            {
                // Xử lý ảnh bìa từ File Manager
                if (!string.IsNullOrEmpty(CoverImagePath))
                {
                    book.CoverImage = CoverImagePath;
                }

                // Xử lý file PDF từ File Manager
                if (!string.IsNullOrEmpty(BookFilePath))
                {
                    // Kiểm tra định dạng file từ đường dẫn
                    var fileExtension = Path.GetExtension(BookFilePath).ToLowerInvariant();
                    if (fileExtension != ".pdf")
                    {
                        ModelState.AddModelError("BookFilePath", "Chỉ chấp nhận file PDF");
                    }
                    else
                    {
                        book.BookFile = BookFilePath;
                    }
                }

                book.CreatedAt = DateTime.Now;
                // IsActive: checkbox checked = true, unchecked = false (từ hidden input)
                book.IsActive = book.IsActive ?? true;

                _context.Books.Add(book);
                _context.SaveChanges();

                // Xử lý quan hệ many-to-many với Author
                if (AuthorIDs != null && AuthorIDs.Length > 0)
                {
                    foreach (var authorId in AuthorIDs)
                    {
                        var bookAuthor = new tblBookAuthor
                        {
                            BookID = book.BookID,
                            AuthorID = authorId
                        };
                        _context.BookAuthors.Add(bookAuthor);
                    }
                    _context.SaveChanges();
                }

                TempData["SuccessMessage"] = "Thêm sách thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.IsActive == true), "CategoryID", "CategoryName", book.CategoryID);
            ViewBag.Publishers = new SelectList(_context.Publishers.Where(p => p.IsActive == true), "PublisherID", "PublisherName", book.PublisherID);
            ViewBag.Authors = _context.Authors.Where(a => a.IsActive == true).OrderBy(a => a.AuthorName).ToList();
            return View(book);
        }

        // GET: Admin/Book/Details/{id}
        [HttpGet]
        [Route("Admin/Book/Details/{id}")]
        public IActionResult Details(int id)
        {
            if (!Functions.IsAdmin())
                return RedirectToAction("Index", "Login");

            var book = _context.Books
       .Include(b => b.Category)
       .Include(b => b.Publisher)
       .Include(b => b.BookAuthors)
           .ThenInclude(ba => ba.Author)
       .FirstOrDefault(b => b.BookID == id);

            if (book == null)
                return NotFound();

            return View(book);
        }

        // GET: Admin/Book/Edit/{id}
        [HttpGet]
        [Route("Admin/Book/Edit/{id}")]
        public IActionResult Edit(int id)
        {
            if (!Functions.IsAdmin())
                return RedirectToAction("Index", "Login");

            var book = _context.Books
                .Include(b => b.BookAuthors)
                .FirstOrDefault(b => b.BookID == id);
            if (book == null)
                return NotFound();

            ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.IsActive == true), "CategoryID", "CategoryName", book.CategoryID);
            ViewBag.Publishers = new SelectList(_context.Publishers.Where(p => p.IsActive == true), "PublisherID", "PublisherName", book.PublisherID);
            ViewBag.Authors = _context.Authors.Where(a => a.IsActive == true).OrderBy(a => a.AuthorName).ToList();
            ViewBag.SelectedAuthorIDs = book.BookAuthors?.Select(ba => ba.AuthorID).ToList() ?? new List<int>();
            return View(book);
        }

        // POST: Admin/Book/Edit/{id}
        [HttpPost]
        [Route("Admin/Book/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, tblBook book, string? CoverImagePath, string? BookFilePath, string? RemoveBookFile, int[]? AuthorIDs)
        {
            if (!Functions.IsAdmin())
                return RedirectToAction("Index", "Login");

            var existingBook = _context.Books.Find(id);
            if (existingBook == null)
                return NotFound();

            // Validation
            if (string.IsNullOrWhiteSpace(book.Title))
            {
                ModelState.AddModelError("Title", "Tên sách là bắt buộc");
            }
            else
            {
                // Kiểm tra trùng tên sách (trừ bản ghi hiện tại)
                var exists = _context.Books.Any(b => b.BookID != id && b.Title != null && b.Title.Trim().ToLower() == book.Title.Trim().ToLower());
                if (exists)
                {
                    ModelState.AddModelError("Title", "Tên sách đã tồn tại");
                }
            }

            if (book.PublishedYear < 1000 || book.PublishedYear > DateTime.Now.Year + 1)
            {
                ModelState.AddModelError("PublishedYear", $"Năm xuất bản phải từ 1000 đến {DateTime.Now.Year + 1}");
            }

            if (book.Quantity < 0)
            {
                ModelState.AddModelError("Quantity", "Số lượng không được âm");
            }

            if (book.CategoryID <= 0)
            {
                ModelState.AddModelError("CategoryID", "Vui lòng chọn danh mục");
            }

            if (book.PublisherID <= 0)
            {
                ModelState.AddModelError("PublisherID", "Vui lòng chọn nhà xuất bản");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật thông tin
                    existingBook.Title = book.Title;
                    existingBook.Description = book.Description;
                    existingBook.PublishedYear = book.PublishedYear;
                    existingBook.Quantity = book.Quantity;
                    existingBook.CategoryID = book.CategoryID;
                    existingBook.PublisherID = book.PublisherID;
                    // IsActive: checkbox checked = true, unchecked = false (từ hidden input)
                    existingBook.IsActive = book.IsActive ?? false;

                    // Xử lý ảnh bìa từ File Manager
                    if (!string.IsNullOrEmpty(CoverImagePath))
                    {
                        existingBook.CoverImage = CoverImagePath;
                    }
                    // Nếu không có ảnh mới, giữ nguyên ảnh cũ

                    // Xử lý file PDF từ File Manager
                    if (!string.IsNullOrEmpty(BookFilePath))
                    {
                        // Kiểm tra định dạng file từ đường dẫn
                        var fileExtension = Path.GetExtension(BookFilePath).ToLowerInvariant();
                        if (fileExtension != ".pdf")
                        {
                            ModelState.AddModelError("BookFilePath", "Chỉ chấp nhận file PDF");
                        }
                        else
                        {
                            existingBook.BookFile = BookFilePath;
                        }
                    }
                    else if (!string.IsNullOrEmpty(RemoveBookFile) && RemoveBookFile == "true")
                    {
                        // Xóa file PDF nếu người dùng chọn xóa
                        existingBook.BookFile = null;
                    }
                    // Nếu không có file mới và không xóa, giữ nguyên file cũ

                    _context.Books.Update(existingBook);
                    _context.SaveChanges();

                    // Cập nhật quan hệ many-to-many với Author
                    // Xóa tất cả quan hệ cũ
                    var existingBookAuthors = _context.BookAuthors.Where(ba => ba.BookID == id).ToList();
                    _context.BookAuthors.RemoveRange(existingBookAuthors);
                    _context.SaveChanges();

                    // Thêm quan hệ mới
                    if (AuthorIDs != null && AuthorIDs.Length > 0)
                    {
                        foreach (var authorId in AuthorIDs)
                        {
                            var bookAuthor = new tblBookAuthor
                            {
                                BookID = id,
                                AuthorID = authorId
                            };
                            _context.BookAuthors.Add(bookAuthor);
                        }
                        _context.SaveChanges();
                    }

                    TempData["SuccessMessage"] = "Cập nhật sách thành công!";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Books.Any(e => e.BookID == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.IsActive == true), "CategoryID", "CategoryName", book.CategoryID);
            ViewBag.Publishers = new SelectList(_context.Publishers.Where(p => p.IsActive == true), "PublisherID", "PublisherName", book.PublisherID);
            ViewBag.Authors = _context.Authors.Where(a => a.IsActive == true).OrderBy(a => a.AuthorName).ToList();
            ViewBag.SelectedAuthorIDs = AuthorIDs?.ToList() ?? (existingBook.BookAuthors?.Select(ba => ba.AuthorID).ToList() ?? new List<int>());

            // Cần load lại BookAuthors cho existingBook
            existingBook = _context.Books
                .Include(b => b.BookAuthors)
                .FirstOrDefault(b => b.BookID == id) ?? existingBook;

            return View(existingBook);
        }

        // GET: Admin/Book/Delete/{id}
        [HttpGet]
        [Route("Admin/Book/Delete/{id}")]
        public IActionResult Delete(int id)
        {
            if (!Functions.IsAdmin())
                return RedirectToAction("Index", "Login");

            var book = _context.Books
       .Include(b => b.Category)
       .Include(b => b.Publisher)
       .Include(b => b.BookAuthors)
           .ThenInclude(ba => ba.Author)
       .FirstOrDefault(b => b.BookID == id);  

            if (book == null)
                return NotFound();

            return View(book); 
        }

        // POST: Admin/Book/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [Route("Admin/Book/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!Functions.IsAdmin())
                return RedirectToAction("Index", "Login");

            var book = _context.Books.Find(id);
            if (book == null)
                return NotFound();

            // Xóa ảnh bìa nếu có
            if (!string.IsNullOrEmpty(book.CoverImage))
            {
                var imagePath = book.CoverImage.Replace("/files/books/", "");
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "books", imagePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            _context.Books.Remove(book);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Xóa sách thành công!";
            return RedirectToAction("Index");
        }
    }
}
