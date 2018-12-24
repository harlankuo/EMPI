using EMPIMS.Module.SystemManage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMPIMS.BLL.DBHelper;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using EMPIMS.Code;
using System.Text.RegularExpressions;
using System.Data;
using System.Reflection;
using EMPIMS.Module.IndexManage;
using System.ComponentModel;
using System.Collections;

namespace EMPIMS.BLL.SystemManage
{
    public class EMPI_MATCHCONFIG_Bll
    {

        /// <summary>
        /// 保存匹配规则
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public AjaxResult SubmitForm(DataTable dt)
        {
            AjaxResult msg = new AjaxResult();
            if (dt.Rows.Count > 0)
            {
                dt.Columns.Remove("__RequestVerificationToken");

                IList<EMPI_MATCHCONFIG> matchLi = new List<EMPI_MATCHCONFIG>();
                var dbhelper_match = new MongoDBHelper<EMPI_MATCHCONFIG>();

                //获取主索引的字段名和描述
                Hashtable hs = GetEMPI_PERSON_SBRField();
                //列名
                string columName = "";
                int standard = 0;
                int fieldSum = 0;
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (dt.Rows[0][i].ToString() != "" && dt.Rows[0][i].ToString() != "0")
                    {
                        int tmp = 0;
                        if (!int.TryParse(dt.Rows[0][i].ToString(), out tmp))
                        {
                            msg.state = "error";
                            msg.message = "保存失败，权重值只能是整数";
                            return msg;
                        }
                        EMPI_MATCHCONFIG entity = new EMPI_MATCHCONFIG();
                        entity.MATCHCONFIG_ID = Guid.NewGuid().ToString();
                        columName = dt.Columns[i].ColumnName;

                        if (columName == "Standard" || columName == "Standard_1")
                        {
                            entity.Type = 0;
                            entity.Name = "标准";

                            if (columName == "Standard")
                            {
                                standard = tmp;
                            }
                        }
                        else
                        {
                            entity.Type = 1;
                            entity.Name = hs[columName].ToString();
                            fieldSum += tmp;
                        }

                        entity.Code = columName;
                        entity.Value = tmp;
                        matchLi.Add(entity);
                    }
                }

                if (fieldSum != 0 && fieldSum < standard)
                {
                    msg.state = "error";
                    msg.message = "保存失败，字段权重值总和必须大于或等于潜在重复区间最小值";
                }
                else
                {
                    //先删除所有配置
                    dbhelper_match.Remove(Query.NE("Value", BsonNull.Value));

                    //重新添加
                    dbhelper_match.BatchInsert(matchLi);

                    msg.state = "success";
                    msg.message = "保存成功";
                }
            }
            else
            {
                msg.state = "error";
                msg.message = "保存失败，参数错误";
            }
            return msg;
        }


        /// <summary>
        /// 获取主索引的字段名称和描述
        /// </summary>
        /// <returns></returns>
        public Hashtable GetEMPI_PERSON_SBRField()
        {
            Hashtable hs = new Hashtable();
            PropertyInfo[] peroperties = typeof(EMPI_PERSON_SBR).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in peroperties)
            {
                object[] objs = property.GetCustomAttributes(typeof(DescriptionAttribute), true);
                if (objs.Length > 0)
                {
                    hs.Add(property.Name, ((DescriptionAttribute)objs[0]).Description);
                    //Console.WriteLine("{0}: {1}", property.Name, ((DescriptionAttribute)objs[0]).Description);
                }
            }
            return hs;
        }


        /// <summary>
        /// 获取所有配置
        /// </summary>
        /// <returns></returns>
        public IList<EMPI_MATCHCONFIG> FindAll()
        {
            return new MongoDBHelper<EMPI_MATCHCONFIG>().FindAll();
        }

        /// <summary>
        /// 获取所有潜在重复和自动合并配置组合
        /// </summary>
        /// <returns></returns>
        public Hashtable GetPreview()
        {
            #region //=======0.获取所有匹配设置数据=====================
            IList<EMPI_MATCHCONFIG> all_match = new List<EMPI_MATCHCONFIG>();
            all_match = new MongoDBHelper<EMPI_MATCHCONFIG>().FindAll();
            #endregion


            #region //=======1.获取匹配字段和权重值 =====================
            var field = all_match.Where(a => a.Type == 1).ToList();
            #endregion


            #region //=======2.获取标准值潜在重复区间============================
            int result_1 = 0;
            int result_2 = 0;
            var standard = all_match.Where(a => a.Type == 0).ToList();
            if (standard.Count > 0)
            {
                result_1 = standard.FirstOrDefault().Value;
                result_2 = standard.LastOrDefault().Value;
            }
            #endregion


            #region //=======3.生成所有大于等于匹配标准的组合 ============


            Dictionary<string, string> dict_1 = new Dictionary<string, string>();

            Dictionary<string, string> dict_2 = new Dictionary<string, string>();


            string[] currentArr = { };
            int curr_count2 = 0;

            for (int i = 1; i < 1 << field.Count; i++)//从1循环到2^N  
            {
                Hashtable hs = new Hashtable();
                int sum = 0;
                string group = "";

                for (int j = 0; j < field.Count; j++)
                {
                    if ((i & 1 << j) != 0)//用i与2^j进行位与运算，若结果不为0,则表示第j位不为0,从数组中取出第j个数  
                    {
                        sum += Convert.ToInt32(field[j].Value);

                        group += field[j].Name + "(<span style='color:blue'>" + field[j].Value + "</span>)+";
                    }
                }

                //符合自动合并组合
                if (sum > result_2)
                {
                    currentArr = group.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                    curr_count2 = currentArr.Length;

                    //去重
                    IsRepeat(ref dict_2, group, currentArr, curr_count2, sum);
                }
                else if (sum >= result_1 && sum <= result_2)
                {
                    currentArr = group.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                    curr_count2 = currentArr.Length;

                    //去重
                    IsRepeat(ref dict_1, group, currentArr, curr_count2, sum);
                }

            }

            //降序排序一下，匹配最大权重的组合排最前面            
            //dict_1 = dict_1.OrderByDescending(a => (Convert.ToInt32(a.Key.Split('_').Last()))).ToDictionary(p => p.Key, o => o.Value);

            //dict_2 = dict_2.OrderByDescending(a => (Convert.ToInt32(a.Key.Split('_').Last()))).ToDictionary(p => p.Key, o => o.Value);
            #endregion

            Hashtable hs_result = new Hashtable();
            hs_result.Add("duplit", dict_1);
            hs_result.Add("merge", dict_2);

            return hs_result;
        }


        /// <summary>
        /// 去掉父集，只留下符合条件的最小子集        
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="group"></param>
        /// <param name="previewArr"></param>
        /// <param name="currentArr"></param>
        /// <param name="pre_count1"></param>
        /// <param name="curr_count2"></param>
        /// <param name="dictValue"></param>
        /// <param name="flag"></param>
        private static void IsRepeat(ref Dictionary<string, string> dict, string group, string[] currentArr, int curr_count2, int sum)
        {
            string[] previewArr = { };
            int pre_count1 = 0;

            Hashtable hs_result = new Hashtable();
            var flag = "Both";
            foreach (var key in dict.Keys)
            {
                flag = "Both";
                previewArr = (dict[key] + "+").Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                pre_count1 = previewArr.Length;

                if (pre_count1 > 0)
                {
                    if (pre_count1 > curr_count2)
                    {
                        foreach (var item in currentArr)
                        {
                            flag = "Both";
                            if (!previewArr.Contains(item))
                            {                               
                                break;
                            }
                            else
                            {
                                flag = "Curr";
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in previewArr)
                        {
                            flag = "Both";
                            if (!currentArr.Contains(item))
                            {                                
                                break;
                            }
                            else
                            {
                                flag = "Previ";
                            }
                        }
                    }

                    hs_result.Add(key, flag);

                }
            }

            //取当前的，那么上一条的都必须删除掉
            if (hs_result.ContainsValue("Curr"))
            {
                var previ_dict = from item in dict
                                 where item.Value == "Curr"
                                 select new { key = item.Key };
                //删除上一组合
                foreach (var item in previ_dict)
                {
                    dict.Remove(item.ToString());
                }

                //再加上当前组合
                dict.Add(Guid.NewGuid().ToString() + "_" + sum, Common.DelLastChar(group, "+"));
            }
            else if (!hs_result.ContainsValue("Previ") && !hs_result.ContainsValue("Curr"))
            {
                dict.Add(Guid.NewGuid().ToString() + "_" + sum, Common.DelLastChar(group, "+"));
            }
        }
    }
}
