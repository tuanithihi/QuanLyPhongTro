using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Areas.Admin.Attributes
{
    /// <summary>
    /// Chặn chủ trọ chưa được phê duyệt (Pending hoặc Suspended) không được thực hiện các chức năng đăng tin / tạo phòng.
    /// SuperAdmin được phép bỏ qua.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class RequireApprovedLandlordAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var landlordService = context.HttpContext.RequestServices.GetService<ICurrentLandlordService>();
            if (landlordService == null)
            {
                base.OnActionExecuting(context);
                return;
            }

            if (landlordService.IsSuperAdmin())
            {
                base.OnActionExecuting(context);
                return;
            }

            var landlord = landlordService.GetCurrentLandlordAsync().GetAwaiter().GetResult();
            if (landlord == null || landlord.Status != LandlordStatus.Approved)
            {
                if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                    context.HttpContext.Request.Path.StartsWithSegments("/api"))
                {
                    context.Result = new JsonResult(new { success = false, message = "Tài khoản chủ trọ chưa được phê duyệt để thực hiện thao tác này." })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }

                if (context.Controller is Controller controller)
                {
                    controller.TempData["Error"] = "Tài khoản chủ trọ của bạn đang chờ Ban Quản Trị phê duyệt trước khi có thể đăng tin tạo phòng mới.";
                }
                context.Result = new RedirectToActionResult("LandlordStatus", "Account", new { area = "" });
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
