using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;
using QuanLyThuVien.Utilities;
using QuanLyThuVien.Attributes;
using PagedList.Core;

namespace QuanLyThuVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class BorrowController : Controller
    {
        private readonly DataContext _context;

        public BorrowController(DataContext context)
        {
            _context = context;
        }

        // GET: Admin/Borrow/Index
        [HttpGet]
        [Route("Admin/Borrow/Index/{page?}")]
        [Route("Admin/Borrow/")]
        public IActionResult Index(int page = 1, int? userId = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!Functions.IsAdmin())
                return RedirectToAction("Index", "Login");

            int pageSize = 10;
            var borrows = _context.Borrows
                .Include(b => b.User)
                .Include(b => b.BorrowDetails)
                    .ThenInclude(bd => bd.Book)
                .AsQueryable();

            // Lọc theo User
            if (userId.HasValue && userId.Value > 0)
            {
                borrows = borrows.Where(b => b.UserID == userId.Value);
            }

            // Lọc theo trạng thái (chuẩn xác)
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Borrowing")
                {
                    // Đang mượn: tất cả phiếu chưa trả
                    borrows = borrows.Where(b => b.Status == "Borrowing");
                }
                else if (status == "Overdue")
                {
                    // Quá hạn: phiếu chưa trả và đã quá hạn
                    borrows = borrows.Where(b => b.Status == "Borrowing" && b.DueDate < DateTime.Now);
                }
                else if (status == "Returned")
                {
                    borrows = borrows.Where(b => b.Status == "Returned");
                }
            }

            // Lọc theo khoảng ngày
            if (fromDate.HasValue)
            {
                borrows = borrows.Where(b => b.BorrowDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                borrows = borrows.Where(b => b.BorrowDate <= toDate.Value.AddDays(1).AddSeconds(-1));
            }

            // Đã xử lý logic trạng thái ở trên, bỏ đoạn này để tránh lặp điều kiện sai

            borrows = borrows.OrderByDescending(b => b.BorrowDate);
            var models = new PagedList<tblBorrow>(borrows, page, pageSize);
            
            // Tính toán trạng thái quá hạn cho các item đã phân trang (chỉ để hiển thị)
            foreach (var borrow in models)
            {
                if (borrow.Status != "Returned" && DateTime.Now > borrow.DueDate)
                {
                    borrow.Status = "Overdue";
                }
            }

            // ViewBag cho filters
            ViewBag.Users = new SelectList(_context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName), "UserID", "FullName", userId);
            ViewBag.Statuses = new SelectList(new[]
            {
                new { Value = "Borrowing", Text = "Đang mượn" },
                new { Value = "Returned", Text = "Đã trả" },
                new { Value = "Overdue", Text = "Quá hạn" }
            }, "Value", "Text", status);
            ViewBag.UserId = userId;
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(models);
        }

        // GET: Admin/Borrow/Details/{id}
        [HttpGet]
        [Route("Admin/Borrow/Details/{id}")]
        public IActionResult Details(int id)
        {
            if (!Functions.IsAdmin())
                return RedirectToAction("Index", "Login");

            var borrow = _context.Borrows
                .Include(b => b.User)
                .Include(b => b.BorrowDetails)
                    .ThenInclude(bd => bd.Book)
                        .ThenInclude(book => book.Category)
                .Include(b => b.BorrowDetails)
                    .ThenInclude(bd => bd.Book)
                        .ThenInclude(book => book.Publisher)
                .FirstOrDefault(b => b.BorrowID == id);

            if (borrow == null)
                return NotFound();

            // Tính toán trạng thái quá hạn
            if (borrow.Status != "Returned" && DateTime.Now > borrow.DueDate)
            {
                borrow.Status = "Overdue";
            }

            return View(borrow);
        }

        // POST: Admin/Borrow/MarkReturned - Đánh dấu trả sách
        [HttpPost]
        [Route("Admin/Borrow/MarkReturned")]
        [ValidateAntiForgeryToken]
        public IActionResult MarkReturned(int borrowId, int? borrowDetailId, bool returnAll = false)
        {
            if (!Functions.IsAdmin())
                return RedirectToAction("Index", "Login");

            var borrow = _context.Borrows
                .Include(b => b.BorrowDetails)
                    .ThenInclude(bd => bd.Book)
                .FirstOrDefault(b => b.BorrowID == borrowId);

            if (borrow == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiếu mượn!";
                return RedirectToAction("Index");
            }

            if (returnAll)
            {
                // Trả tất cả sách chưa trả
                var notReturnedDetails = borrow.BorrowDetails?.Where(bd => bd.BorrowStatus != "Returned").ToList() ?? new List<tblBorrowDetail>();
                
                foreach (var detail in notReturnedDetails)
                {
                    detail.BorrowStatus = "Returned";
                    detail.ReturnDate = DateTime.Now;

                    // Cộng lại số lượng sách
                    if (detail.Book != null)
                    {
                        detail.Book.Quantity += detail.Quantity;
                    }
                }

                // Kiểm tra nếu tất cả đã trả thì cập nhật status của Borrow
                if (borrow.BorrowDetails != null && borrow.BorrowDetails.All(bd => bd.BorrowStatus == "Returned"))
                {
                    borrow.Status = "Returned";
                }

                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Đã đánh dấu trả {notReturnedDetails.Count} cuốn sách thành công!";
            }
            else if (borrowDetailId.HasValue)
            {
                // Trả từng cuốn sách
                var detail = borrow.BorrowDetails?.FirstOrDefault(bd => bd.BorrowDetailID == borrowDetailId.Value);
                
                if (detail == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy chi tiết mượn!";
                    return RedirectToAction("Details", new { id = borrowId });
                }

                if (detail.BorrowStatus == "Returned")
                {
                    TempData["ErrorMessage"] = "Sách này đã được trả rồi!";
                    return RedirectToAction("Details", new { id = borrowId });
                }

                detail.BorrowStatus = "Returned";
                detail.ReturnDate = DateTime.Now;

                // Cộng lại số lượng sách
                if (detail.Book != null)
                {
                    detail.Book.Quantity += detail.Quantity;
                }

                // Kiểm tra nếu tất cả đã trả thì cập nhật status của Borrow
                if (borrow.BorrowDetails != null && borrow.BorrowDetails.All(bd => bd.BorrowStatus == "Returned"))
                {
                    borrow.Status = "Returned";
                }

                _context.SaveChanges();
                TempData["SuccessMessage"] = "Đã đánh dấu trả sách thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Vui lòng chọn sách cần trả!";
            }

            return RedirectToAction("Details", new { id = borrowId });
        }
    }
}

