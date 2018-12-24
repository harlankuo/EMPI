using EMPIMS.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EMPIMS.BLL.SystemManage;
using EMPIMS.Module.SystemManage;

namespace EMPIMS.Web.Areas.IndexManage.Controllers
{

    [HandlerLogin]
    public class SystemManageController : ControllerBase
    {
        //
        // GET: /IndexManage/SystemManage/
        private static EMPI_ORG_Bll dbhelper_org = new EMPI_ORG_Bll();

        private static EMPI_SYS_Bll dbhelper_sys = new EMPI_SYS_Bll();

        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 机构管理
        /// </summary>
        /// <returns></returns>
        public ActionResult IndexOrg()
        {
            return View("IndexOrg");
        }

        /// <summary>
        /// 系统管理
        /// </summary>
        /// <returns></returns>
        public ActionResult IndexSys()
        {
            return View();
        }

        #region 机构
        /// <summary>
        /// 获取列表（机构）
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="pagination"></param>
        /// <returns></returns>
        [HandlerAjaxOnly]
        public ActionResult GetGridJson_Org(string keywords, Pagination pagination)
        {
            var list = dbhelper_org.GetList(keywords, pagination);

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
        /// 获取一个实体（机构）
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        [HandlerAjaxOnly]
        public ActionResult GetFormJson_org(string keyValue)
        {
            var data = dbhelper_org.GetEntity(keyValue);
            return Content(data.ToJson());
        }

        /// <summary>
        /// 删除机构
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public ActionResult DeleteForm_org(string keyValue)
        {
            var data = dbhelper_org.Delete(keyValue);
            return Content(data.ToJson());
        }

        /// <summary>
        /// 提交（机构）
        /// </summary>
        /// <param name="keyValue"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public ActionResult SubmitForm_org(string keyValue, EMPI_ORG entity)
        {
            var data = dbhelper_org.SubmitForm(keyValue, entity);
            return Content(data.ToJson());
        }
        #endregion




        #region 系统
        /// <summary>
        /// 获取列表（系统）
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="pagination"></param>
        /// <returns></returns>
        [HandlerAjaxOnly]
        public ActionResult GetGridJson_Sys(string keywords, Pagination pagination)
        {
            var list = dbhelper_sys.GetList(keywords, pagination);

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
        /// 获取一个实体（系统）
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        [HandlerAjaxOnly]
        public ActionResult GetFormJson_sys(string keyValue)
        {
            var data = dbhelper_sys.GetEntity(keyValue);
            return Content(data.ToJson());
        }

        /// <summary>
        /// 删除机构
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public ActionResult DeleteForm_sys(string keyValue)
        {
            var data = dbhelper_sys.Delete(keyValue);
            return Content(data.ToJson());
        }

        /// <summary>
        /// 提交（系统）
        /// </summary>
        /// <param name="keyValue"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public ActionResult SubmitForm_sys(string keyValue, EMPI_SYS entity)
        {
            var data = dbhelper_sys.SubmitForm(keyValue, entity);
            return Content(data.ToJson());
        }
        #endregion

    }
}
