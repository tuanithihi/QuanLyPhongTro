using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QuanLyPhongTro.Areas.Admin.Attributes
{
    /// <summary>
    /// Custom ActionFilter bảo vệ toàn bộ khu vực Admin.
    /// Kiểm tra session "AdminUser". Nếu chưa đăng nhập → redirect về trang Login.
    /// Dùng: đặt [AdminOnly] lên AdminBaseController (hoặc từng controller riêng).
    /// </summary>
    public class AdminOnlyAttribute : ActionFilterAttribute
    {
        private const string SESSION_KEY = "AdminUser";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session.GetString(SESSION_KEY);

            if (string.IsNullOrEmpty(session))
            {
                context.Result = new RedirectToActionResult(
                    actionName:     "Index",
                    controllerName: "Home",
                    routeValues:    new { area = "" }
                );
            }

            base.OnActionExecuting(context);
        }
    }
}
