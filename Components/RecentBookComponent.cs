using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Components
{
    [ViewComponent(Name = "RecentBook")]
    public class RecentBookComponent : ViewComponent
    {
        private readonly DataContext _context;

        public RecentBookComponent(Models.DataContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy 3 sách gần nhất, sắp xếp theo CreatedAt (ngày tạo mới nhất)
            var listRecentBooks = (from b in _context.viewRecentBooks
                                   where (b.IsActive == true)
                                   orderby b.CreatedAt descending, b.BookID descending
                                   select b).Take(3).ToList();
            return await Task.FromResult((IViewComponentResult)View("Default", listRecentBooks));
        }
    }
}