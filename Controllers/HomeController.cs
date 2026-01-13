using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Controllers;

public class HomeController : Controller
{
    private readonly DataContext _context;

    public HomeController( DataContext context)
    {
        _context = context;
    }
    public IActionResult Index()
    {

                // Lấy số liệu thống kê cho Hero Section
        var booksCurrentlyBorrowed = _context.BorrowDetails.Sum(x => x.ReturnDate == null ? x.Quantity : 0);
        var availableBooks = _context.Books.Sum(x => x.Quantity);
        var totalBooks = availableBooks + booksCurrentlyBorrowed;
        var totalUsers = _context.Users.Count(x => x.IsActive == true);
        var totalAuthors = _context.Authors.Count(x => x.IsActive == true);
        var totalCategories = _context.Categories.Count(x => x.IsActive == true);

        ViewBag.TotalBooks = totalBooks;
        ViewBag.TotalUsers = totalUsers;
        ViewBag.totalAuthors = totalAuthors;
        ViewBag.TotalCategories = totalCategories;
        ViewBag.BooksCurrentlyBorrowed = booksCurrentlyBorrowed;

        // Lấy top 3 danh mục sách phổ biến nhất (theo số lượng sách)
        var topCategoriesWithCount = (from c in _context.Categories
                                      where c.IsActive == true
                                      join b in _context.Books on c.CategoryID equals b.CategoryID
                                      where b.IsActive == true
                                      group new { c, b } by new { c.CategoryID, c.CategoryName, c.Description } into g
                                      orderby g.Count() descending
                                      select new
                                      {
                                          Category = new tblCategory
                                          {
                                              CategoryID = g.Key.CategoryID,
                                              CategoryName = g.Key.CategoryName,
                                              Description = g.Key.Description
                                          },
                                          BookCount = g.Count()
                                      })
                                      .Take(3)
                                      .ToList();

        ViewBag.TopCategories = topCategoriesWithCount.Select(x => x.Category).ToList();
        ViewBag.TopCategoriesWithCount = topCategoriesWithCount.ToDictionary(x => x.Category.CategoryID, x => x.BookCount);

         // Lấy 4 tác giả mới nhất
        var latestAuthors = _context.Authors
            .Where(a => a.IsActive == true)
            .OrderByDescending(a => a.CreatedAt)
            .Take(4)
            .ToList();
        // Lấy tất cả danh mục đang hoạt động
        var categories = _context.Categories
            .Where(c => c.IsActive == true)
            .OrderBy(c => c.CategoryName)
            .ToList();

        // Lấy 4 nhà xuất bản mới nhất
        var latestPublishers = _context.Publishers
            .Where(p => p.IsActive == true)
            .OrderByDescending(p => p.CreatedAt)
            .Take(4)
            .ToList();

        var latestPosts = _context.viewPostMenus
        .Where(p => p.IsActive == true)
        .OrderByDescending(p => p.CreatedDate)
        .Take(3)
        .ToList();

         ViewBag.LatestPosts = latestPosts;
        ViewBag.LatestAuthors = latestAuthors;
        ViewBag.Categories = categories;
        ViewBag.LatestPublishers = latestPublishers;


        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
