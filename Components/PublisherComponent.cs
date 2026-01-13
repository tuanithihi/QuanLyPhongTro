using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Components
{
    [ViewComponent  (Name = "Publisher")]
    public class PublisherComponent : ViewComponent
    {
        private readonly DataContext _context;

        public PublisherComponent(DataContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var listPublisher = (from p in _context.Publishers
                            where (p.IsActive == true)
                            orderby p.CreatedAt descending, p.PublisherID descending
                            select p).Take(3).ToList();

            return await Task.FromResult((IViewComponentResult)View("Default", listPublisher));
        }
        
    }
}