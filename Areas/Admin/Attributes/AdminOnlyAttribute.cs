using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Areas.Admin.Attributes
{
    /// <summary>
    /// ActionFilter bảo vệ khu vực quản trị Admin / Landlord.
    /// Cho phép SuperAdmin và Landlord; Chặn Tenant và người chưa đăng nhập; Chặn Landlord bị Suspended.
    /// </summary>
    public class AdminOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var landlordService = context.HttpContext.RequestServices.GetService<ICurrentLandlordService>();
            string? sessionAdmin = null;
            try
            {
                sessionAdmin = context.HttpContext.Session?.GetString("AdminUser");
            }
            catch
            {
                // Session chưa kích hoạt hoặc không khả dụng
            }
            var role = landlordService?.GetCurrentUserRole();

            // 1. Kiểm tra chưa đăng nhập
            if (string.IsNullOrEmpty(sessionAdmin) && string.IsNullOrEmpty(role))
            {
                context.Result = new RedirectToActionResult("Index", "Home", new { area = "" });
                return;
            }

            // 2. Chặn khách thuê (Tenant) không được vào khu vực chủ trọ
            if (string.Equals(role, "Tenant", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            // 3. Nếu là chủ trọ (Landlord) -> kiểm tra trạng thái hoạt động (Suspended bị khóa)
            if (string.Equals(role, "Landlord", StringComparison.OrdinalIgnoreCase) && landlordService != null)
            {
                var landlord = landlordService.GetCurrentLandlordAsync().GetAwaiter().GetResult();
                if (landlord != null && landlord.Status == LandlordStatus.Suspended)
                {
                    try { context.HttpContext.Session?.Clear(); } catch { }
                    if (context.Controller is Controller c && c.TempData != null)
                    {
                        c.TempData["Error"] = "Tài khoản chủ trọ của bạn đã bị tạm khóa / đình chỉ. Vui lòng liên hệ ban quản trị.";
                    }
                    context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
