using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EMPIMS.BLL.SystemManage;
using EMPIMS.Code;

namespace EMPIMS.Web.Controllers.Login
{
    public class LoginController : Controller
    {
        //
        // GET: /Login/

        /// <summary>
        /// 登录页面
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            OperatorProvider.Provider.RemoveCurrent();
            return View();
        }


        /// <summary>
        /// 退出登录
        /// </summary>
        /// <returns></returns>
        public ActionResult LoginOut()
        {
            new OperatorProvider().RemoveCurrent();
            return Content(new AjaxResult() { state = ResultType.success }.ToJson());
        }


        /// <summary>
        /// 设置当前登录人
        /// </summary>
        /// <returns></returns>
        public ActionResult SetCurrentUser()
        {
            string userdata = HttpContext.Server.UrlDecode(Request["postData"]);
            AjaxResult result = new AjaxResult() { state = ResultType.error };
            OperatorModel user = userdata.ToObject<OperatorModel>();
            new OperatorProvider().AddCurrent(user);
            result.state = ResultType.success;
            return Content(result.ToJson());
        }

    }
}
