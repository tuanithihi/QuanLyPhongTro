using Microsoft.AspNetCore.Mvc;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private const string SESSION_KEY = "AdminUser";

        // GET: /Admin/Account/Login
        // → Redirect về trang chủ (modal login xử lý), giữ returnUrl
        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString(SESSION_KEY)))
                return RedirectToAction("Index", "Home", new { area = "Admin" });

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        // GET: /Admin/Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Remove(SESSION_KEY);
            TempData["Success"] = "Đã đăng xuất thành công.";
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}
