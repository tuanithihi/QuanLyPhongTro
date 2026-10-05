using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Areas.Admin.Attributes
{
    /// <summary>
    /// Chỉ cho phép SuperAdmin truy cập. Landlord và Tenant bị trả về 403 Forbidden.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class SuperAdminOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var landlordService = context.HttpContext.RequestServices.GetService<ICurrentLandlordService>();
            if (landlordService == null || !landlordService.IsSuperAdmin())
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
