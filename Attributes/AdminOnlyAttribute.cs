using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QuanLyThuVien.Utilities;

namespace QuanLyThuVien.Attributes
{
    /// <summary>
    /// Attribute để chặn truy cập, chỉ cho phép Admin
    /// Nếu không phải Admin thì redirect về /Home/AccessDenied
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AdminOnlyAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Kiểm tra đã đăng nhập chưa
            if (!Functions.IsLogin())
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Kiểm tra có phải Admin không
            if (!Functions.IsAdmin())
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                return;
            }
        }
    }
}

