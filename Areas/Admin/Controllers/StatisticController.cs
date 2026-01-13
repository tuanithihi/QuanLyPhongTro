using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PagedList.Core;
using QuanLyThuVien.Models;
using QuanLyThuVien.Utilities;

namespace QuanLyThuVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StatisticController : Controller
    {
        private readonly DataContext _context;

        public StatisticController(DataContext context)
        {
            _context = context;
        }

        public IActionResult Index(DateTime? fromDate, DateTime? toDate, int bookPage = 1, int userPage = 1)
        {
            // 1. XỬ LÝ BỘ LỌC NGÀY
            // Nếu không chọn, mặc định lấy từ đầu tháng này đến hiện tại
            if (!fromDate.HasValue) fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!toDate.HasValue) toDate = DateTime.Now;

            // Cộng thêm 1 ngày vào toDate để bao gồm cả ngày cuối cùng (lấy đến 23:59:59)
            var toDateEnd = toDate.Value.AddDays(1).AddSeconds(-1);

            // Truyền ngược lại View để hiển thị trên ô input
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            // 2. SỐ LIỆU TỔNG QUAN (DASHBOARD)
            // A. Sách đang mượn
            var booksCurrentlyBorrowed = _context.BorrowDetails.AsNoTracking().Sum(x => x.ReturnDate == null ? x.Quantity : 0);
            
            // B. Tổng sách
            var availableBooks = _context.Books.AsNoTracking().Sum(x => x.Quantity);
            var totalBooksEver = availableBooks + booksCurrentlyBorrowed;
            
            // C. Số người đang mượn
            var usersCurrentlyBorrowing = _context.Borrows.AsNoTracking()
                .Where(b => b.Status == "Borrowing")
                .Select(b => b.UserID)
                .Distinct()
                .Count();

            // D. Số phiếu quá hạn
            var overdueBooks = _context.Borrows.AsNoTracking()
                                .Where(b => b.Status == "Borrowing" && b.DueDate < DateTime.Now)
                                .Count();

            ViewBag.TotalBooks = totalBooksEver;
            ViewBag.BooksCurrentlyBorrowed = booksCurrentlyBorrowed;
            ViewBag.UsersCurrentlyBorrowing = usersCurrentlyBorrowing;
            ViewBag.Users = _context.Users.AsNoTracking().Count(x => x.IsActive == true);
            ViewBag.OverdueBooks = overdueBooks;

            // 3. THỐNG KÊ CHI TIẾT THEO NGÀY (TOP SÁCH & TOP NGƯỜI)
            
            // A. Top Sách (Lọc theo ngày mượn)
            var topBooksQuery = from b in _context.Borrows.AsNoTracking()
                                join d in _context.BorrowDetails.AsNoTracking() on b.BorrowID equals d.BorrowID
                                where b.BorrowDate >= fromDate && b.BorrowDate <= toDateEnd
                                group d by d.BookID into g
                                select new { BookID = g.Key, Count = g.Count() };

            var allTopBooks = topBooksQuery
                .OrderByDescending(x => x.Count)
                .Join(_context.Books.AsNoTracking().Include(b => b.Category),
                      stat => stat.BookID,
                      book => book.BookID,
                      (stat, book) => new tblStatBookRank
                      {
                          BookID = book.BookID,
                          Book = book,
                          BorrowCount = stat.Count
                      })
                .ToList();

            // B. Top Độc Giả (Lọc theo ngày mượn)
            var topUsersQuery = from b in _context.Borrows.AsNoTracking()
                                where b.BorrowDate >= fromDate && b.BorrowDate <= toDateEnd
                                group b by b.UserID into g
                                select new { UserID = g.Key, Count = g.Count() };

            var allTopUsers = topUsersQuery
                .OrderByDescending(x => x.Count)
                .Join(_context.Users.AsNoTracking(),
                      stat => stat.UserID,
                      user => user.UserID,
                      (stat, user) => new TopUserRank
                      {
                          UserID = user.UserID,
                          FullName = user.FullName,
                          UserCode = user.UserName, 
                          Avatar = user.Avatar,
                          BorrowCount = stat.Count
                      })
                .ToList();

            // 4. PHÂN TRANG
            int pageSize = 5;
            var pagedTopBooks = new PagedList<tblStatBookRank>(allTopBooks.AsQueryable(), bookPage, pageSize);
            var pagedTopUsers = new PagedList<TopUserRank>(allTopUsers.AsQueryable(), userPage, pageSize);

            // 5. ĐÓNG GÓI DỮ LIỆU
            var viewModel = new StatisticViewModel
            {
                PagedTopBooks = pagedTopBooks,
                PagedTopUsers = pagedTopUsers,
                FromDate = fromDate,
                ToDate = toDate,
                BookPage = bookPage,
                UserPage = userPage
            };

            return View(viewModel);
        }
    }
}