using EMPIMS.Code;
using System.Web.Mvc;

namespace EMPIMS.Web
{
    [HandlerLogin]
    public abstract class ControllerBase : AsyncController
    {
        public Log FileLog
        {
            get { return LogFactory.GetLogger(this.GetType().ToString()); }
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            FileLog.Error(filterContext.Exception.Message);
            base.OnException(filterContext);
        }
        protected virtual ActionResult Success(string message)
        {
            return Content(new AjaxResult { state = ResultType.success.ToString(), message = message }.ToJson());
        }
        protected virtual ActionResult Success(string message, object data)
        {
            return Content(new AjaxResult { state = ResultType.success.ToString(), message = message, data = data }.ToJson());
        }
        protected virtual ActionResult Error(string message)
        {
            return Content(new AjaxResult { state = ResultType.error.ToString(), message = message }.ToJson());
        }
    }
}
