using EMPIMS.BLL.IndexManage;
using EMPIMS.Module.IndexManage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EMPIMS.Code;
using System.Threading.Tasks;

namespace EMPIMS.Web.Areas.IndexManage.Controllers
{
    [HandlerLogin]
    public class OperationController : ControllerBase
    {

        private EMPI_OPERATION_Bll operation_bll = new EMPI_OPERATION_Bll();
        // 
        // GET: /IndexManage/Operation/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Form()
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
        public async Task<ActionResult> GetGridJson(string keywords, string start_Date, string end_Date, Pagination pagination)
        {
            string operation_type = Request["operation_type"];
            var list = await Task.Run(() => operation_bll.GetList(keywords, start_Date, end_Date, operation_type, pagination));

            //list = list.Where(a => a.OPERATION_TYPE == "ADD");
            var tmp = from item in list
                      select new { item._id, item.OPERATION_ID, item.PERSON_SBR_ID_LIST, item.EMPI_ID_LIST, item.OPERATE_BY, item.OPERATE_TIME, item.OPERATION_TYPE };

            var data = new
            {
                rows = tmp.ToList(),
                records = pagination.records,
                page = pagination.page,
                total = pagination.total
            };

            return Content(data.ToJson("yyyy-MM-dd HH:mm:ss"));
        }

        /// <summary>
        /// 获取日志详情
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public ActionResult GetForm(string keyValue)
        {
            AjaxResult msg = new AjaxResult();

            if (!string.IsNullOrEmpty(keyValue))
            {
                EMPI_OPERATION operation = operation_bll.GetForm(keyValue);
                operation.DELTA = null;
                operation.SYS_REC_LIST = null;
                return Content(operation.ToJson());
            }
            else
            {
                msg.state = "error";
                msg.message = "参数错误！";
                return Content(msg.ToJson());
            }

        }

    }
}
