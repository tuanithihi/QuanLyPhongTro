using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Controllers
{
    [ViewComponent(Name ="User")]
    public class UserViewComponent :ViewComponent
    {
        private readonly DataContext _context;
        public UserViewComponent(DataContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = (from u in _context.Users where (u.IsActive == true) select u).ToList();
            return await Task.FromResult((IViewComponentResult)View(user));
        }
    }
}