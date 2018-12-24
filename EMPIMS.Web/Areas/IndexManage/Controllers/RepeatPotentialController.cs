using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EMPIMS.BLL.IndexManage;
using EMPIMS.Code;
using EMPIMS.Module.IndexManage;
using System.Threading.Tasks;

namespace EMPIMS.Web.Areas.IndexManage.Controllers
{

    [HandlerLogin]
    public class RepeatPotentialController : ControllerBase
    {


        private EMPI_POTENTIAL_DUPLICATE_Bll potential_dup_bll = new EMPI_POTENTIAL_DUPLICATE_Bll();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Handle()
        {
            return View();
        }

        /// <summary>
        /// 获取潜在重复列表
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="start_Date"></param>
        /// <param name="end_Date"></param>
        /// <param name="resolved_Status"></param>
        /// <param name="pagination"></param>
        /// <returns></returns>
        [HttpGet]
        [HandlerAjaxOnly]
        public async Task<ActionResult> GetGridJson(string keywords, string start_Date, string end_Date, string resolved_Status, Pagination pagination)
        {
            var list = await potential_dup_bll.GetList(keywords, start_Date, end_Date, resolved_Status, pagination);

            var data = new
            {
                rows = list,
                records = pagination.records,
                page = pagination.page,
                total = pagination.total
            };

            return Content(data.ToJson());
        }

        /// <summary>
        /// 获取潜在重复信息
        /// </summary>
        /// <param name="empi_ID_1"></param>
        /// <param name="empi_ID_2"></param>
        /// <returns></returns>
        [HttpPost]
        [HandlerAjaxOnly]
        public ActionResult GetIndexJson(string keyValue)
        {
            var data = potential_dup_bll.GetIndexJson(keyValue);
            return Content(data);
        }


        /// <summary>
        /// 放弃合并
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [HandlerAjaxOnly]
        public ActionResult AbandonMerge()
        {
            string keyValue = Request["keyValue"];
            AjaxResult result = potential_dup_bll.AbandonMerge(keyValue);
            return Content(result.ToJson());
        }


        /// <summary>
        /// 合并
        /// </summary>
        /// <param name="keyValue"></param>
        /// <param name="targetId"></param>
        /// <param name="sourceId"></param>
        /// <returns></returns>
        [HttpPost]
        [HandlerAjaxOnly]
        public ActionResult Merge(string keyValue, string targetId, string sourceId)
        {
            AjaxResult result = potential_dup_bll.Merge(keyValue, targetId, sourceId);
            return Content(result.ToJson());
        }

        /// <summary>
        /// 拆分
        /// </summary>
        /// <param name="operation_id"></param>
        /// <returns></returns>
        [HttpPost]
        [HandlerAjaxOnly]
        public ActionResult Split(string operation_id)
        {
            var msg = potential_dup_bll.Split(operation_id);
            return Content(msg.ToJson());
        }
    }
}
