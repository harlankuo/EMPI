using EMPIMS.BLL.IndexManage;
using EMPIMS.Code;
using EMPIMS.Module.IndexManage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace EMPIMS.Web.Areas.IndexManage.Controllers
{
    public class PrimaryIndexController : ControllerBase
    {
        Log log = LogFactory.GetLogger(typeof(PrimaryIndexController));
        private EMPI_PERSON_SBR_Bll _person_sbr_Bll = new EMPI_PERSON_SBR_Bll();

        //
        // GET: /IndexManage/PrimaryIndex/

        /// <summary>
        /// 索引列表
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> Index()
        {
            return await Task.Run(() => View());
        }

        /// <summary>
        /// 索引详情
        /// </summary>
        /// <returns></returns>
        public ActionResult Form()
        {
            return View();
        }

        /// <summary>
        /// 查看索引
        /// </summary>
        /// <returns></returns>
        public ActionResult Details()
        {
            return View();
        }

        /// <summary>
        /// 主索引对比
        /// </summary>
        /// <returns></returns>
        public ActionResult Compare()
        {
            return View();
        }

        /// <summary>
        /// 变动明细
        /// </summary>
        /// <returns></returns>
        public ActionResult ChangeDetails()
        {
            return View();
        }



        [HandlerAjaxOnly]
        public async Task<ActionResult> GetGridJson(Pagination pagination, string keywords, string status)
        {
            var list = await _person_sbr_Bll.GetList(keywords, status, pagination);
            var data = new
            {
                rows = list,
                records = pagination.records,
                page = pagination.page,
                total = pagination.total
            };

            return Content(data.ToJson());
        }

        [HttpGet]
        [HandlerAjaxOnly]
        public async Task<ActionResult> GetGridJsonBySup(Pagination pagination, string supData)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(supData))
            {
                dict = supData.ToObject<Dictionary<string, object>>();

                //移除防伪数据
                dict.Remove("__RequestVerificationToken");
            }
            var list = await _person_sbr_Bll.GetListBySup(dict, pagination);

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
        /// 获取主索引详细数据
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        [HttpGet]
        [HandlerAjaxOnly]
        public ActionResult GetFormJson(string keyValue)
        {
            var data = _person_sbr_Bll.GetForm(keyValue);
            return Content(data.ToJson("yyyy-MM-dd"));
        }


        /// <summary>
        /// 查看数据(person_sbr、mapping、operation)
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>       
        [HandlerAjaxOnly]
        public async Task<ActionResult> GetDetailsJson(string keyValue)
        {
            string result = string.Empty;
            var data = await _person_sbr_Bll.GetDetailsJson(keyValue);
            result = data.ToJson();
            return Content(result);
        }

        /// <summary>
        /// 提交
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        [HttpPost]
        [HandlerAjaxOnly]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitForm(EMPI_PERSON_SBR entity, string keyValue)
        {
            AjaxResult msg = new AjaxResult();
            string[] updateField = Request.Form.AllKeys;

            //如果ID_NO不为空，转为大写。
            if (!string.IsNullOrEmpty(entity.ID_NO))
            {
                entity.ID_NO = entity.ID_NO.ToUpper();
            }

            if (!string.IsNullOrEmpty(keyValue))
            {
                entity.PERSON_SBR_ID = keyValue;
            }
            else
            {
                entity.Create();
                entity.EMPI_ID = _person_sbr_Bll.CreateEMPI_ID(entity.ID_NO);
            }
            msg = _person_sbr_Bll.SubmitForm(entity, keyValue, updateField);

            if (!string.IsNullOrEmpty(keyValue) && msg.state != "error")
            {
                string _switch = ConfigurationManager.AppSettings["IsIndexUpdatePush"];

                if (_switch == "on")
                {
                    try
                    {
                        EMPI_MAPPING mapping = _person_sbr_Bll.GetMappingByPerson_sbr_id(keyValue);
                        if (mapping != null)
                        {
                            string xml = CreateXml(mapping, entity);
                            SentHttpRequest(xml);
                            log.Debug("通知结果：");
                            log.Debug("=======================================");
                        }

                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message);
                    }

                }
            }
            return Content(msg.ToJson());
        }


        /// <summary>
        /// 启用/停用
        /// </summary>
        /// <param name="keyValue"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpPost]
        [HandlerAjaxOnly]
        public ActionResult SetIndexStatus(string keyValue, string status)
        {
            var data = _person_sbr_Bll.SetIndexStatus(keyValue, status);
            return Content(data.ToJson());
        }

        /// <summary>
        /// 启用主索引
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        [HttpPost]
        [HandlerAjaxOnly]
        public ActionResult SetIndexStatus_Single(string keyValue)
        {
            var data = _person_sbr_Bll.SetIndexStatus_Single(keyValue);
            return Content(data.ToJson());
        }

        /// <summary>
        /// 获取主索引记录
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        [HttpGet]
        [HandlerAjaxOnly]
        public ActionResult GetChangeDetails(string keyValue, string operation_ID)
        {
            var list = new EMPI_OPERATION_Bll().GetList(keyValue);
            return Content(list.ToJson("yyyy-MM-dd HH:mm:ss"));
        }

        /// <summary>
        /// 获取实体Json
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public ActionResult GetOperationEntityJson(string keyValue, string operation_ID)
        {
            var strEntity = new EMPI_OPERATION_Bll().GetDeltaStr(keyValue, operation_ID);
            //entity.DELTA = null;
            return Content(strEntity);
        }

        /// <summary>
        /// 获取对比的两条数据
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        [HttpPost]
        [HandlerAjaxOnly]
        public ActionResult GetCompareIndexJson(string keyValue)
        {
            var data = _person_sbr_Bll.GetCompareIndexJson(keyValue);

            return Content(data.ToJson("yyyy-MM-dd HH:mm:ss"));
        }

        /// <summary>
        /// 判断主索引能否拆分
        /// </summary>
        /// <param name="keValue">主索引号</param>
        /// <returns></returns>
        [HttpPost]
        [HandlerAjaxOnly]
        public ActionResult IsSplitable(string operation_id)
        {
            var msg = _person_sbr_Bll.IsSplitable(operation_id);
            return Content(msg.ToJson());
        }


        /// <summary>
        /// 将改变后的主索引推送出去
        /// </summary>
        /// <param name="xmlStr">主索引</param>
        /// <returns></returns>
        public string SentHttpRequest(string xmlStr)
        {
            //请求路径
            string url = ConfigurationManager.AppSettings["IndexPushAddress"];

            //定义request并设置request的路径
            WebRequest request = WebRequest.Create(url);

            //定义请求的方式
            request.Method = "POST";

            request.Timeout = 60000;

            //初始化request参数
            string postData = "payload=" + xmlStr;
            log.Debug("==============" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "=========================");
            log.Debug("传的参数：" + postData);
            //设置参数的编码格式，解决中文乱码
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            //设置request的MIME类型及内容长度
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;

            //打开request字符流
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            //定义responsee为前面的request响应
            WebResponse responsee = request.GetResponse();

            //获取相应的状态代码
            //Console.WriteLine(((HttpWebresponsee)responsee).StatusDescription);

            //定义responsee字符流
            dataStream = responsee.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseeFromServer = reader.ReadToEnd();//读取所有
            Console.WriteLine(responseeFromServer);

            //关闭资源
            reader.Close();
            dataStream.Close();
            responsee.Close();
            return responseeFromServer;
        }


        /// <summary>
        /// 创建xml字符串
        /// </summary>
        /// <param name="mapping"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public string CreateXml(EMPI_MAPPING mapping, EMPI_PERSON_SBR entity)
        {
            StringBuilder strBuilder = new StringBuilder();

            strBuilder.Append("<REQUEST><ORG_CODE>" + mapping.ORG_CODE + "</ORG_CODE><SYS_CODE>" + mapping.SYS_CODE + "</SYS_CODE><SYS_REC_ID>" + mapping.SYS_REC_ID + "</SYS_REC_ID>");
            PropertyInfo[] propertyInfo = entity.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            foreach (var item in propertyInfo)
            {
                object value = item.GetValue(entity);
                if (value != null)
                {
                    strBuilder.AppendFormat("<" + item.Name + ">{0}</" + item.Name + ">", value);
                }
            }

            //string[] field = new EMPI_PERSON_SBR().GetType().GetFields();
            strBuilder.Append("</REQUEST>");

            return strBuilder.ToString();
        }
    }
}

