using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Areas.Admin.Components
{
    [ViewComponent(Name = "AdminMenu")]
    public class AdminMenuComponent : ViewComponent
    {
        public readonly DataContext _context;
        public AdminMenuComponent(DataContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var mnAdmin = (from m in _context.AdminMenus
                           where (m.IsActive == true)
                           select m).ToList();
            return await Task.FromResult((IViewComponentResult)View("Default", mnAdmin));
        }
    }
}