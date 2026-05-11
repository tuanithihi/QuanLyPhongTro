using Microsoft.AspNetCore.Mvc;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class LoginController : Controller
    {
        // Giữ lại để không 404 nếu còn link cũ → redirect về trang chủ
        public IActionResult Index() =>
            RedirectToAction("Index", "Home", new { area = "" });
    }
}
