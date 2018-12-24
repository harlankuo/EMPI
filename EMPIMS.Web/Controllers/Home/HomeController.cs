using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EMPIMS.BLL.SystemManage;
using EMPIMS.Code;
using EMPIMS.BLL.IndexManage;
using EMPIMS.Module.IndexManage;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;

namespace EMPIMS.Web.Controllers.Home
{
    [HandlerLogin]
    public class HomeController : ControllerBase
    {
        //
        // GET: /Home/


        [HttpGet]      
        public async Task<ActionResult> Index()
        {

            if (EMPIMS.Code.OperatorProvider.Provider.GetCurrent() == null)
            {
                return Redirect("/Login/Index");
            }
            else
            {
                ViewBag.DisplayName = EMPIMS.Code.OperatorProvider.Provider.GetCurrent().DisplayName;
                ViewBag.UserID = EMPIMS.Code.OperatorProvider.Provider.GetCurrent().UserID;
            }

            return await Task.Run(() => View());
        }


        public async Task<ActionResult> Default()
        {
            return await Task.Run(() => View());
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <returns></returns>
        public ActionResult ChangePassword()
        {
            return View();
        }


        /// <summary>
        /// 获取字典数据
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetClientsDataJson()
        {
            DC_DICTION_Bll dic_bll = new DC_DICTION_Bll();
            var data = await Task.Run(() => dic_bll.GetClientData());
            return Content(data.Replace("ObjectId(", "").Replace(")", ""));
        }

        /// <summary>
        /// 获取首页Dashboard统计记录
        /// </summary>
        /// <returns></returns>        
        public async Task<ActionResult> GetDashboardRecord()
        {
            var data = await new DefaultPage_Bll().GetDashboardRecord();
            return Content(data.ToJson());
        }


        /// <summary>
        /// 获取本年度拆分、合并处理统计
        /// </summary>
        /// <returns></returns>
        [HandlerAjaxOnly]
        public async Task<ActionResult> GetSalaryChart()
        {
            var data = await new DefaultPage_Bll().GetSalaryChart();
            return Content(data.ToJson());
        }

        /// <summary>
        /// 获取潜在重复处理统计
        /// </summary>
        /// <returns></returns>
        [HandlerAjaxOnly]
        public async Task<ActionResult> GetLeaveChart()
        {
            var data = await new DefaultPage_Bll().GetLeaveChart();
            return Content(data.ToJson());
        }

        /// <summary>
        /// 获取未解决潜在重复前10条记录
        /// </summary>
        /// <returns></returns>
        [HandlerAjaxOnly]
        public async Task<ActionResult> GetDuplicate()
        {
            var data = await new DefaultPage_Bll().GetDuplicate();
            return Content(data.ToJson());
        }
    }
}
