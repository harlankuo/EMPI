using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using EMPIMS.Code;
using EMPIMS.Module.IndexManage;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using EMPIMS.BLL.DBHelper;
using MongoDB.Bson;
using EMPIMS.BLL.IndexManage;
using System.Configuration;
using System.Text.RegularExpressions;

namespace EMPIMS.Web.WebService
{
    /// <summary>
    /// IndexRegisterService 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://www.caradigm.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消注释以下行。 
    [System.Web.Script.Services.ScriptService]
    public class IndexRegisterService : System.Web.Services.WebService
    {
        Log log = LogFactory.GetLogger("IndexRegisterService");

        /// <summary>
        /// 主索引注册
        /// </summary>
        /// <param name="xml">患者信息xml</param>
        /// <returns></returns>
        [WebMethod]
        public string IndexRegisterFunc(string xml)
        {
            /* 0:参数错误
             * 1:没问题
             *-1:服务器错误
             */
            string isOn = ConfigurationManager.AppSettings["IsIndexRegister"].ToString();
            string response = "";

            if (isOn != "on")
            {
                response = "<result><code>-1</code><state>error</state><message>失败，主索引未开启注册服务。</message></result>";
                log.Debug(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss :") + response);
                return response;
            }

            AjaxResult msg = new AjaxResult();


            int code = 1;
            Type type = typeof(EMPI_PERSON);

            try
            {
                EMPI_PERSON person = null;
                string empi_id = "";

                if (string.IsNullOrEmpty(xml))
                {
                    log.Error("xml参数为空字符串！");
                    response = "<result><code>0</code><state>error</state><message>传的xml参数不能为空或没有接收到！</message></result>";
                    log.Debug(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss :") + response);
                    return response;
                }

                log.Debug(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - 接收的XML字符串为：" + xml);
                using (StringReader sr = new StringReader(xml))
                {
                    XmlSerializer xmldes = new XmlSerializer(type);

                    person = (EMPI_PERSON)xmldes.Deserialize(sr);

                    if (person != null)
                    {
                        if (!string.IsNullOrEmpty(person.ID_NO))
                        {
                            person.ID_NO = person.ID_NO.ToUpper();
                        }
                        if (!CheckXmlData(person, out msg))
                        {
                            code = msg.data.ToInt();
                            log.Error(msg.message + ",输入XML：" + xml);
                        }
                        else
                        {
                            var person_bll = new EMPI_PERSON_Bll();

                            //1.先查询是否存在系统中
                            var data = person_bll.Find(person.SYS_CODE, person.SYS_REC_ID, person.ORG_CODE);

                            if (data.Any() == false)
                            {
                                //新增
                                msg = person_bll.Create(person, out empi_id);
                            }
                            else
                            {
                                //修改
                                msg = person_bll.Update(person, out empi_id);
                            }
                        }
                    }
                    else
                    {
                        code = 0;
                        msg.message = "参数错误";
                        msg.state = "error";
                    }

                }
                response = "<result><code>" + code + "</code><state>" + msg.state + "</state><message>" + msg.message + "</message><empi_id>" + empi_id + "</empi_id></result>";
                log.Debug(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss :") + response);
                return response;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message + " - xml字符串：" + xml);
                code = -1;
                response = "<result><code>" + code + "</code><state>error</state><message>程序发生异常，请联系管理员。</message></result>";
                log.Debug(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss :") + response);
                return response;
            }
        }


        /// <summary>
        /// 检查传入xml数据完整
        /// </summary>
        /// <param name="person"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool CheckXmlData(EMPI_PERSON person, out AjaxResult msg)
        {

            DateTime dt_tmp = new DateTime();
            msg = new AjaxResult();

            #region 机构编码、系统编码、源系统唯一主键不能为空
            if (string.IsNullOrEmpty(person.SYS_CODE) || string.IsNullOrEmpty(person.SYS_REC_ID) || string.IsNullOrEmpty(person.ORG_CODE))
            {
                msg.data = 0;
                msg.message = "参数错误，ORG_CODE、SYS_CODE和SYS_REC_ID不能为空。";
                msg.state = "error";
                return false;
            }
            #endregion

            #region 患者姓名
            if (string.IsNullOrEmpty(person.NAME))
            {
                msg.data = 0;
                msg.message = "参数错误，患者姓名(NAME)不能为空。";
                msg.state = "error";
                return false;
            }
            #endregion

            #region 身份证号码
            if (!string.IsNullOrEmpty(person.ID_NO))
            {
                string id_type_code = Configs.GetValue("ID_TYPE_CODE");
                if (person.ID_TYPE_CODE == id_type_code)
                {
                    if (!CheckIDCard(person.ID_NO))
                    {
                        msg.data = 0;
                        msg.message = "参数错误，患者身份证号码不合法！";
                        msg.state = "error";
                        return false;
                    }
                }
            }
            #endregion

            #region 出生日期
            if (!string.IsNullOrEmpty(person.DOB))
            {
                if (!DateTime.TryParse(person.DOB, out dt_tmp))
                {
                    if (person.DOB.Trim().Length == 8)
                    {
                        DateTime dt = FormatDateTime(person.DOB.Trim(), "yyyyMMdd");
                        person.DOB = dt.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        msg.data = 0;
                        msg.message = "参数错误，出生日期(DOB)必须为日期格式(yyyy-MM-dd)";
                        msg.state = "error";
                        return false;
                    }
                }
                else
                {
                    person.DOB = dt_tmp.ToString("yyyy-MM-dd");
                }
            }
            else
            {
                person.DOB = "";
            }
            #endregion

            #region 死亡日期
            if (!string.IsNullOrEmpty(person.DOD))
            {
                if (!DateTime.TryParse(person.DOD, out dt_tmp))
                {
                    if (person.DOD.Trim().Length == 8)
                    {
                        DateTime dt = FormatDateTime(person.DOD.Trim(), "yyyyMMdd");
                        person.DOD = dt.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        msg.data = 0;
                        msg.message = "参数错误，死亡日期(DOD)必须为日期格式(yyyy-MM-dd)";
                        msg.state = "error";
                        return false;
                    }
                }
                else
                {
                    person.DOD = dt_tmp.ToString("yyyy-MM-dd");
                }
            }
            else
            {
                person.DOD = "";
            }
            #endregion

            #region 创建时间
            if (!string.IsNullOrEmpty(person.CREATE_TIME))
            {
                if (!DateTime.TryParse(person.CREATE_TIME, out dt_tmp))
                {
                    if (person.CREATE_TIME.Trim().Length == 14)
                    {
                        DateTime dt = FormatDateTime(person.CREATE_TIME.Trim(), "yyyyMMddHHmmss");
                        person.CREATE_TIME = dt.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        msg.data = 0;
                        msg.message = "参数错误，创建时间(CREATE_TIME)必须为日期格式(yyyy-MM-dd HH:mm:ss)";
                        msg.state = "error";
                        return false;
                    }
                }
                else
                {
                    person.CREATE_TIME = dt_tmp.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            else
            {
                person.CREATE_TIME = "";
            }
            #endregion

            #region 最后修改时间
            if (!string.IsNullOrEmpty(person.LAST_UPDATE_TIME))
            {
                if (!DateTime.TryParse(person.LAST_UPDATE_TIME, out dt_tmp))
                {
                    if (person.LAST_UPDATE_TIME.Trim().Length == 14)
                    {
                        DateTime dt = FormatDateTime(person.LAST_UPDATE_TIME.Trim(), "yyyyMMddHHmmss");
                        person.LAST_UPDATE_TIME = dt.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        msg.data = 0;
                        msg.message = "参数错误，最后修改时间(LAST_UPDATE_TIME)必须为日期格式(yyyy-MM-dd HH:mm:ss)";
                        msg.state = "error";
                        return false;
                    }
                }
                else
                {
                    person.LAST_UPDATE_TIME = dt_tmp.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            else
            {
                person.LAST_UPDATE_TIME = "";
            }
            #endregion

            if (string.IsNullOrEmpty(person.STATUS))
            {
                //默认为启用
                person.STATUS = "A";
            }

            if (string.IsNullOrEmpty(person.CATEGORY_CODE))
            {
                //默认患者
                person.CATEGORY_CODE = "0";
            }

            return true;
        }


        /// <summary>
        /// 格式化日期
        /// </summary>
        /// <param name="dtStr"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public DateTime FormatDateTime(string dtStr, string format)
        {
            return DateTime.ParseExact(dtStr, format, System.Globalization.CultureInfo.InvariantCulture);
        }


        /// <summary>
        /// 判断身份证号码是否正确
        /// </summary>
        /// <param name="socialNo">身份证号码</param>
        /// <returns></returns>
        public bool CheckIDCard(string socialNo)
        {

            if (socialNo.Length != 15 && socialNo.Length != 18)
            {
                return (false);
            }

            int[] area = new int[] { 11, 12, 13, 14, 15, 21, 22, 23, 31, 32, 33, 34, 35, 36, 37, 41, 42, 43, 44, 45, 46, 50, 51, 52, 53, 54, 61, 62, 63, 64, 65, 71, 81, 82, 91 };

            if (!area.Contains(Convert.ToInt32(socialNo.Substring(0, 2))))
            {
                return (false);
            }

            if (socialNo.Length == 15)
            {
                Regex pattern = new Regex(@"^\d{15}$");
                if (pattern.Matches(socialNo).Count <= 0)
                {

                    return (false);
                }
                var birth = Convert.ToInt32("19" + socialNo.Substring(6, 2));
                string month = socialNo.Substring(8, 2);
                var day = Convert.ToInt32(socialNo.Substring(10, 2));
                switch (month)
                {
                    case "01":
                    case "03":
                    case "05":
                    case "07":
                    case "08":
                    case "10":
                    case "12":
                        if (day > 31)
                        {
                            return false;
                        }
                        break;
                    case "04":
                    case "06":
                    case "09":
                    case "11":
                        if (day > 30)
                        {
                            return false;
                        }
                        break;
                    case "02":
                        if ((birth % 4 == 0 && birth % 100 != 0) || birth % 400 == 0)
                        {
                            if (day > 29)
                            {

                                return false;
                            }
                        }
                        else
                        {
                            if (day > 28)
                            {

                                return false;
                            }
                        }
                        break;
                    default:

                        return false;
                }
                var nowYear = DateTime.Now.Year;
                if (nowYear - birth.ToInt() < 15 || nowYear - birth.ToInt() > 100)
                {

                    return false;
                }
                return (true);
            }

            int[] Wi = new int[] { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2, 1 };

            var lSum = 0;
            var nNum = 0;
            var nCheckSum = 0;

            for (int i = 0; i < 17; ++i)
            {
                if (socialNo.Substring(i, 1).ToInt() < 0 || socialNo.Substring(i, 1).ToInt() > 9)
                {
                    return (false);
                }
                else
                {
                    nNum = socialNo.Substring(i, 1).ToInt() - 0;
                }
                lSum += nNum * Wi[i];
            }

            if (socialNo.Substring(17, 1).ToUpper() == "X")
            {
                lSum += 10 * Wi[17];
            }
            else if (socialNo.Substring(17, 1).ToInt() < 0 || socialNo.Substring(17, 1).ToInt() > 9)
            {
                return (false);
            }
            else
            {
                lSum += (socialNo.Substring(17, 1).ToInt() - 0) * Wi[17];
            }
            if ((lSum % 11) == 1)
            {
                return true;
            }
            else
            {
                return (false);
            }
        }
    }
}
