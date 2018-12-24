using EMPIMS.Code;
using System.Web.Mvc;
using System.Web.Routing;

namespace EMPIMS.Web
{
    public class HandlerLoginAttribute : AuthorizeAttribute
    {
        public bool Ignore = true;
        public HandlerLoginAttribute(bool ignore = true)
        {
            Ignore = ignore;
        }
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (Ignore == false)
            {
                return;
            }
            if (OperatorProvider.Provider.GetCurrent() == null)
            {
                WebHelper.WriteCookie("empims_login_error", "overdue");
                filterContext.HttpContext.Response.Redirect("/Login/Index");
                return;
            }
        }

    }
}