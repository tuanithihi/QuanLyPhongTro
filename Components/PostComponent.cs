using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Components
{
[ViewComponent(Name = "Post")]
    public class PostComponent : ViewComponent
    {
        private readonly DataContext _context;

        public PostComponent(DataContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Chỉ lấy một số bài viết gần nhất (ví dụ: 3 bài)
            var listPost = (from p in _context.viewPostMenus
                            where (p.IsActive == true)
                            orderby p.CreatedDate descending, p.PostID descending
                            select p).Take(3).ToList();

            return await Task.FromResult((IViewComponentResult)View("Default", listPost));
        }
    }
}