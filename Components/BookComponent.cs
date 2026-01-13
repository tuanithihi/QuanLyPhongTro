using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Components
{
    [ViewComponent(Name = "Book")]
    public class BookComponent : ViewComponent
    {
        private readonly DataContext _context;
        public BookComponent(DataContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var book = (from b in _context.Books where (b.IsActive == true) orderby b.BookID descending select b).ToList();
            return await Task.FromResult((IViewComponentResult)View("Index", book));
        }
    }
}