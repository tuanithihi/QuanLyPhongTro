using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Components
{
    [ViewComponent(Name = "MenuView")]
    public class MenuViewComponent : ViewComponent
    {
        private readonly DataContext _context;
        public MenuViewComponent(DataContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var menus = (from m in _context.Menus where (m.isActive == true) && (m.Position == 1)select m).ToList();
            return await Task.FromResult((IViewComponentResult)View("Default", menus));
        }
        
        
    }
}