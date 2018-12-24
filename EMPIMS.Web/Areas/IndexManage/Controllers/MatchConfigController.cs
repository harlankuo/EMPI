using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EMPIMS.Code;
using EMPIMS.BLL.SystemManage;

namespace EMPIMS.Web.Areas.IndexManage.Controllers
{
    [HandlerLogin]
    public class MatchConfigController : ControllerBase
    {
        //
        // GET: /IndexManage/MatchConfig/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Form()
        {
            return View();
        }

        public ActionResult SubmitForm()
        {
            AjaxResult msg = new AjaxResult();
            string postData = "[" + HttpContext.Server.UrlDecode(Request["postData"]).ToString() + "]";
            DataTable dt = new DataTable();
            if (!string.IsNullOrEmpty(postData))
            {
                dt = postData.ToTable();
                msg = new EMPI_MATCHCONFIG_Bll().SubmitForm(dt);
            }
            else
            {
                msg.state = "error";
                msg.message = "参数错误";
            }

            return Content(msg.ToJson());
        }


        /// <summary>
        /// 获取所有配置数据
        /// </summary>
        /// <returns></returns>
        public ActionResult GetAll()
        {
            var data = new EMPI_MATCHCONFIG_Bll().FindAll();
            return Content(data.ToJson());
        }


        /// <summary>
        /// 获取潜在重复和自动合并组合
        /// </summary>
        /// <returns></returns>
        public ActionResult GetPreview()
        {
            var data = new EMPI_MATCHCONFIG_Bll().GetPreview();
            return Content(data.ToJson());
        }

    }
}
