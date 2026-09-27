using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CS66_WebApplication1.Filters
{
    // อนุญาตเฉพาะพนักงาน (Role 2) และแอดมิน (Role 9)
    public class StaffAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;

            http.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            http.Response.Headers["Pragma"] = "no-cache";
            http.Response.Headers["Expires"] = "0";

            var role = http.Session.GetInt32("Role");

            if (!role.HasValue)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (role != 2 && role != 9)
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
