using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Components
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
            var menus = _context.Menus
                .Where(m => m.IsActive && m.Position == "header")
                .OrderBy(m => m.SortOrder)
                .ToList();
            return await Task.FromResult((IViewComponentResult)View("Default", menus));
        }
    }
}
