using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Utilities;
using QuanLyThuVien.Attributes;
using QuanLyThuVien.Models;
using Microsoft.EntityFrameworkCore;

namespace QuanLyThuVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class HomeController : Controller
    {    
        private readonly DataContext _context;

        public HomeController(DataContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Thống kê tổng quan
            ViewBag.TotalBooks = _context.Books.Count(b => b.IsActive == true);
            ViewBag.TotalAuthors = _context.Authors.Count(a => a.IsActive == true);
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalBorrows = _context.Borrows.Count();

            // Sách được mượn nhiều nhất
            var topBorrowedBooks = (from bd in _context.BorrowDetails
                                   join b in _context.Books on bd.BookID equals b.BookID
                                   where b.IsActive == true
                                   group bd by new { b.BookID, b.Title } into g
                                   orderby g.Count() descending
                                   select new
                                   {
                                       BookName = g.Key.Title,
                                       BorrowCount = g.Count()
                                   }).Take(5).ToList();

            ViewBag.TopBorrowedBooks = topBorrowedBooks;

            return View();
        }

        public IActionResult Logout()
        {
            Functions.ClearSession();
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}