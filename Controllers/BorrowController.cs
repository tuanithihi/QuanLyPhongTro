using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;
using QuanLyThuVien.Utilities;
using PagedList.Core;

namespace QuanLyThuVien.Controllers
{
    public class BorrowController : Controller
    {
        private readonly DataContext _context;

        public BorrowController(DataContext context)
        {
            _context = context;
        }

        // GET: Book/Borrow - Trang chọn sách để mượn
        [HttpGet]
        public IActionResult Borrow(int page = 1, string searchString = "", int? categoryId = null)
        {
            if (!Functions.IsLogin())
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để mượn sách!";
                return RedirectToAction("Login", "Account");
            }

            int pageSize = 12;
            var books = _context.Books
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Where(b => b.IsActive == true)
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

            books = books.OrderByDescending(b => b.CreatedAt);
            var models = new PagedList<tblBook>(books, page, pageSize);

            ViewBag.Categories = _context.Categories.Where(c => c.IsActive == true).OrderBy(c => c.CategoryName).ToList();
            ViewBag.SearchString = searchString;
            ViewBag.CategoryId = categoryId;

            return View(models);
        }

        // POST: Book/Borrow - Xử lý mượn sách
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Borrow(IFormCollection form)
        {
            if (!Functions.IsLogin())
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để mượn sách!";
                return RedirectToAction("Login", "Account");
            }

            // KIỂM TRA: Nếu user có sách quá hạn chưa trả thì không cho mượn
            var overdueDetails = _context.BorrowDetails
                .Include(bd => bd.Borrow)
                .Where(bd => bd.Borrow.UserID == Functions._UserID
                    && bd.BorrowStatus != "Returned"
                    && bd.ReturnDate == null
                    && bd.Borrow.DueDate < DateTime.Now)
                .ToList();

            if (overdueDetails.Any())
            {
                var overdueCount = overdueDetails.Count;
                var overdueBorrowIds = overdueDetails.Select(bd => bd.Borrow.BorrowID).Distinct().ToList();
                TempData["ErrorMessage"] = $"Bạn có {overdueCount} cuốn sách quá hạn chưa trả (trong {overdueBorrowIds.Count} phiếu mượn). " +
                    $"Vui lòng trả hết sách quá hạn trước khi mượn sách mới!<br>" +
                    $"<a href=\"{Url.Action("History", "Borrow")}\" class=\"btn btn-sm btn-warning mt-2\">Xem lịch sử mượn</a>";
                return RedirectToAction("Borrow");
            }

            // Lấy danh sách sách đã chọn từ form (chỉ lấy các checkbox đã checked)
            var selectedBooks = new Dictionary<int, int>();
            var selectedBookIds = form["selectedBooks"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var bookIdStr in selectedBookIds)
            {
                if (int.TryParse(bookIdStr, out int bookId))
                {
                    var quantityKey = $"quantity_{bookId}";
                    if (form.ContainsKey(quantityKey) && int.TryParse(form[quantityKey], out int quantity) && quantity > 0)
                    {
                        selectedBooks[bookId] = quantity;
                    }
                }
            }

            if (selectedBooks.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một cuốn sách để mượn!";
                return RedirectToAction("Borrow");
            }

            // Kiểm tra tồn kho và validate
            var errors = new List<string>();
            var borrowDetails = new List<tblBorrowDetail>();

            foreach (var item in selectedBooks)
            {
                var bookId = item.Key;
                var quantity = item.Value;

                if (quantity <= 0)
                    continue;

                var book = _context.Books.Find(bookId);
                if (book == null)
                {
                    errors.Add($"Sách ID {bookId} không tồn tại!");
                    continue;
                }

                if (book.Quantity < quantity)
                {
                    errors.Add($"Sách '{book.Title}' chỉ còn {book.Quantity} cuốn, không đủ số lượng bạn yêu cầu ({quantity} cuốn)!");
                    continue;
                }

                if (book.IsActive != true)
                {
                    errors.Add($"Sách '{book.Title}' hiện không khả dụng!");
                    continue;
                }

                // Tạo BorrowDetail (mỗi cuốn sách là 1 record)
                for (int i = 0; i < quantity; i++)
                {
                    borrowDetails.Add(new tblBorrowDetail
                    {
                        BookID = bookId,
                        BorrowStatus = "Borrowed",
                        Quantity = 1
                    });
                }
            }

            if (errors.Count > 0)
            {
                TempData["ErrorMessage"] = string.Join("<br>", errors);
                return RedirectToAction("Borrow");
            }

            if (borrowDetails.Count == 0)
            {
                TempData["ErrorMessage"] = "Không có sách nào hợp lệ để mượn!";
                return RedirectToAction("Borrow");
            }

            // Tạo phiếu mượn
            var borrow = new tblBorrow
            {
                UserID = Functions._UserID,
                BorrowDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14), // Mượn 14 ngày
                Status = "Borrowing",
                BorrowDetails = borrowDetails
            };

            // Trừ số lượng sách
            foreach (var item in selectedBooks)
            {
                var bookId = item.Key;
                var quantity = item.Value;

                if (quantity <= 0)
                    continue;

                var book = _context.Books.Find(bookId);
                if (book != null)
                {
                    book.Quantity -= quantity;
                }
            }

            _context.Borrows.Add(borrow);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Đã tạo phiếu mượn thành công! Mã phiếu: {borrow.BorrowID}. Bạn đã mượn {borrowDetails.Count} cuốn sách.";
            return RedirectToAction("History");
        }

        // GET: Borrow/History - Lịch sử mượn của user
        [HttpGet]
        public IActionResult History()
        {
            if (!Functions.IsLogin())
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem lịch sử mượn!";
                return RedirectToAction("Login", "Account");
            }

            var borrows = _context.Borrows
                .Include(b => b.BorrowDetails!)
                    .ThenInclude(bd => bd.Book)
                        .ThenInclude(book => book.Category)
                .Where(b => b.UserID == Functions._UserID)
                .OrderByDescending(b => b.BorrowDate)
                .ToList();

            // Tính toán trạng thái quá hạn cho mỗi phiếu mượn
            foreach (var borrow in borrows)
            {
                if (borrow.Status != "Returned" && DateTime.Now > borrow.DueDate)
                {
                    borrow.Status = "Overdue";
                }
            }

            return View(borrows);
        }
    }
}

