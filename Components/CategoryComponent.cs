using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Components
{
    [ViewComponent(Name= "Category")]
    public class CategoryComponent : ViewComponent
    {
        private readonly DataContext _context;
        public CategoryComponent(DataContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = (from c in _context.Categories
                              where c.IsActive == true
                              orderby c.CategoryID descending
                              select c).Take(3).ToList();
            return await Task.FromResult((IViewComponentResult)View("Default", categories));
        }
        
    }
}