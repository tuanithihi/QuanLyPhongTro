using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;
using PagedList.Core;

namespace QuanLyThuVien.Controllers
{
    public class BlogController : Controller
    {
        private readonly DataContext _context;

        public BlogController(DataContext context)
        {
            _context = context;
        }

        // GET: Blog/Index - Lấy tất cả bài viết từ CSDL và phân trang chuẩn
        [HttpGet]
        public IActionResult Index(int page = 1, string? search = "")
        {
            int pageSize = 6;

            // Lấy TẤT CẢ bài viết từ viewPostMenu (CSDL)
            var posts = _context.viewPostMenus
                .Where(p => p.IsActive == true)
                .AsQueryable();

            // Tìm kiếm theo tiêu đề
            if (!string.IsNullOrWhiteSpace(search))
            {
                posts = posts.Where(p => p.Title != null && p.Title.Contains(search));
            }

            // Sắp xếp theo ngày mới nhất
            posts = posts.OrderByDescending(p => p.CreatedDate);

            // Phân trang chuẩn với PagedList.Core
            var models = new PagedList<viewPostMenu>(posts, page, pageSize);

            ViewBag.SearchString = search;
            

            return View(models);
        }

        // GET: Blog/Details/{id}

        [Route("/post-{slug}-{id:int}.html", Name = "BlogDetails")]
        [HttpGet("post/{slug}-{id}")]
public IActionResult Details(string slug, int id)
{
    var post = _context.viewPostMenus
        .FirstOrDefault(p => p.PostID == id && p.IsActive == true);

    if (post == null)
        return NotFound();

    return View(post);
}

    }
}