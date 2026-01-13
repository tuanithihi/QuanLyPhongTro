using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Components
{
    [ViewComponent(Name = "Author")]
    public class AuthorComponent : ViewComponent
    {
        private readonly DataContext _context;

        public AuthorComponent(DataContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy danh sách tác giả hoạt động
            var listAuthor = (from a in _context.Authors
                              where a.IsActive == true
                              orderby a.CreatedAt descending, a.AuthorID descending
                            select a).Take(3).ToList();

            return await Task.FromResult((IViewComponentResult)View("Default", listAuthor));
        }
    }
}