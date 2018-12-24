using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Xml.Serialization;
using System.Xml;
using System.Collections;
using EMPIMS.BLL.IndexManage;
using System.Threading.Tasks;
using System.Text;
using System.Configuration;
using EMPIMS.Code;

namespace EMPIMS.Web.WebService
{
    /// <summary>
    /// IndexSearchService 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://www.caradigm.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消注释以下行。 
    [System.Web.Script.Services.ScriptService]
    public class IndexSearchService : System.Web.Services.WebService
    {

        Log log = LogFactory.GetLogger("IndexSearchService");


        /// <summary>
        /// 主索引号检索
        /// </summary>
        /// <param name="name">姓名</param>
        /// <param name="id_no">身份证号</param>
        /// <param name="medical_insurance_card_no">医保卡号</param>
        /// <param name="hospital_card_no">门诊卡号</param>
        /// <param name="gender_name">性别名称</param>
        /// <param name="bod_s">出生日期（起始）</param>
        /// <param name="bod_e">出生日期（截止）</param>
        /// <returns></returns>
        [WebMethod]
        public string IndexSearch(string name, string id_no, string medical_insurance_card_no, string hospital_card_no, string gender_name, string bod_s, string bod_e)
        {

            string isOn = ConfigurationManager.AppSettings["IsIndexSearch"].ToString();

            string xmlDocStr = string.Empty;

            if (isOn != "on")
            {
                return "<response><code>-1</code><state>error</state><message>检索失败，主索引系统未开启检索服务。</message><count>0</count><data>null</data></response>";
            }

            StringBuilder sb = new StringBuilder();
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(id_no) && string.IsNullOrEmpty(medical_insurance_card_no) && string.IsNullOrEmpty(hospital_card_no) && string.IsNullOrEmpty(gender_name) && string.IsNullOrEmpty(bod_s) && string.IsNullOrEmpty(bod_e))
            {
                xmlDocStr = "<response><code>-1</code><state>error</state><message>请至少输入一个检索条件</message><count>0</count><data>null</data></response>";
                return xmlDocStr;
            }
            else
            {
                try
                {
                   
                    int count = 0;
                    Hashtable hs = new EMPI_PERSON_SBR_Bll().GetIndex(name, id_no, medical_insurance_card_no, hospital_card_no, gender_name, bod_s, bod_e, out count);

                    sb.Append(@"{response:{code:1,state:'success',message:'检索成功。',count:" + count + ",data:" + hs.ToJson() + "}}");
                    XmlDocument xml = sb.ToString().ToXmlDocumentByJson();
                    xmlDocStr = xml.OuterXml;
                    log.Debug(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "搜索结果:" + xmlDocStr);

                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                    sb.Append(@"{response:{code:-1,state:'error',message:'程序发生异常，请联系主索引服务厂商。',count:0,data:null}}");
                    XmlDocument xml = sb.ToString().ToXmlDocumentByJson();
                    xmlDocStr = xml.OuterXml;
                }
                return xmlDocStr;
            }
        }


        /// <summary>
        /// 获取主索引
        /// </summary>
        /// <param name="empi_id">主索引号</param>
        /// <returns></returns>
        [WebMethod]
        public string GetIndex(string empi_id)
        {
            string isOn = ConfigurationManager.AppSettings["IsIndexSearch"].ToString();

            if (isOn != "on")
            {
                return "<response><code>-1</code><state>error</state><message>检索失败，主索引系统未开启检索服务。</message><count>0</count><data>null</data></response>";
            }

            string result = string.Empty;
            if (!string.IsNullOrEmpty(empi_id))
            {
                var data = new EMPI_PERSON_SBR_Bll().Get_EMPI_PERSON_SBR_BY_EMPI_ID(empi_id);

                if (data != null)
                {
                    result = "{response:{code:1,state:'success',message:'获取成功！',data:" + data.ToJson() + "}}";
                    XmlDocument xml = result.ToXmlDocumentByJson();
                    result = xml.OuterXml;
                }
                else
                {

                    result = "<response><code>1</code><state>success</state><message>该主索引不存在！</message><data>null</data></response>";
                }
            }
            else
            {
                result = "<response><code>-1</code><state>error</state><message>获取失败，请传入主索引号参数（empi_id）！</message><data>null</data></response>";
            }
            return result;
        }
    }
}
