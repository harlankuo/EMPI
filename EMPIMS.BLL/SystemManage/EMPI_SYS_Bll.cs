using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMPIMS.Module.IndexManage;
using EMPIMS.Code;
using EMPIMS.BLL.DBHelper;
using System.Data;
using MongoDB.Bson;
using System.Collections;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Text.RegularExpressions;
using EMPIMS.Module.SystemManage;

namespace EMPIMS.BLL.SystemManage
{
    public class EMPI_SYS_Bll
    {

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <param name="keywords">搜索关键字</param>
        /// <param name="page"></param>
        /// <returns></returns>
        public IEnumerable<EMPI_SYS> GetList(string keywords, Pagination page)
        {
            IMongoQuery query = null;
            List<IMongoQuery> querylist = new List<IMongoQuery>();
            if (!string.IsNullOrEmpty(keywords))
            {
                querylist.Add(Query.Matches("SYS_CODE", new BsonRegularExpression(new Regex(keywords))));
                querylist.Add(Query.Matches("SYS_NAME", new BsonRegularExpression(new Regex(keywords))));
                query = Query.Or(querylist.ToArray());
            }
            return new MongoDBHelper<EMPI_SYS>().Find(querylist.ToArray(), page).AsEnumerable();
        }

        /// <summary>
        /// 提交表单
        /// </summary>
        /// <param name="keyValue"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public AjaxResult SubmitForm(string keyValue, EMPI_SYS entity)
        {
            AjaxResult msg = new AjaxResult();
            var dbhelper = new MongoDBHelper<EMPI_SYS>();

            if (string.IsNullOrEmpty(keyValue))
            {
                int count = dbhelper.Find(Query.EQ("SYS_CODE", entity.SYS_CODE)).Count;
                if (count <= 0)
                {
                    entity.CREATE_BY = OperatorProvider.Provider.GetCurrent().UserID.ToString();
                    entity.CREATE_TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    dbhelper.Insert(entity);
                    msg.state = "success";
                    msg.message = "新增成功";
                }
                else
                {
                    msg.state = "error";
                    msg.message = "新增失败，该机构已存在";
                }
            }
            else
            {
                entity.LAST_UPDATE_BY = OperatorProvider.Provider.GetCurrent().UserID.ToString();
                entity.LAST_UPDATE_TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                dbhelper.Update(Query.EQ("SYS_CODE", keyValue), entity, new string[] { "SYS_NAME", "LAST_UPDATE_BY", "LAST_UPDATE_TIME" });
                msg.state = "success";
                msg.message = "修改成功";
            }
            return msg;
        }


        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public AjaxResult Delete(string keyValue)
        {
            AjaxResult msg = new AjaxResult();
            var dbhelper_person = new MongoDBHelper<EMPI_PERSON>();
            var dbhelper_SYS = new MongoDBHelper<EMPI_SYS>();
            if (dbhelper_person.Find(Query.EQ("SYS_CODE", keyValue)).Count <= 0)
            {
                dbhelper_SYS.Remove(Query.EQ("SYS_CODE", keyValue));
                msg.state = "success";
                msg.message = "删除成功";
            }
            else
            {
                msg.state = "error";
                msg.message = "删除失败，该系统已存在患者信息中";
            }
            return msg;
        }


        /// <summary>
        /// 获取一个实体
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public EMPI_SYS GetEntity(string keyValue)
        {
            return new MongoDBHelper<EMPI_SYS>().FindOne(Query.EQ("SYS_CODE", keyValue));
        }
    }
}
