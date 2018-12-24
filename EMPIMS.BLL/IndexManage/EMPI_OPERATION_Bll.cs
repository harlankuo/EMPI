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

namespace EMPIMS.BLL.IndexManage
{
    public class EMPI_OPERATION_Bll
    {
        /// <summary>
        /// 获取操作列表
        /// </summary>
        /// <param name="keyValue">患者主索引主键</param>
        /// <returns></returns>
        public IEnumerable<EMPI_OPERATION> GetList(string keyValue)
        {
            IMongoQuery query = Query.EQ("PERSON_SBR_ID_LIST", keyValue);
            string[] param = { "OPERATION_ID", "OPERATION_TYPE", "EMPI_ID_LIST", "SYS_REC_LIST", "OPERATE_TIME", "OPERATE_BY" };
            return new MongoDBHelper<EMPI_OPERATION>().Find(query, param).OrderByDescending(a => a.OPERATE_TIME);
        }


        /// <summary>
        /// 获取操作实体
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public string GetEntityDoc(string keyValue, string operation_ID)
        {
            List<IMongoQuery> queryList = new List<IMongoQuery>();
            queryList.Add(Query.EQ("OPERATION_ID", operation_ID));
            queryList.Add(Query.EQ("PERSON_SBR_ID_LIST", keyValue));
            IMongoQuery query = Query.And(queryList);
            BsonDocument doc = new MongoDBHelper<EMPI_OPERATION>().FindOne(query).ToBsonDocument();
            doc.Remove("_id");
            return doc.ToJson("yyyy-MM-dd HH:mm:ss");
        }


        /// <summary>
        /// 获取DELTA详细信息
        /// </summary>
        /// <param name="keyValue"></param>
        /// <param name="operation_ID"></param>
        /// <returns></returns>
        public string GetDeltaStr(string keyValue, string operation_ID)
        {
            List<IMongoQuery> queryList = new List<IMongoQuery>();
            queryList.Add(Query.EQ("OPERATION_ID", operation_ID));
            queryList.Add(Query.EQ("PERSON_SBR_ID_LIST", keyValue));
            IMongoQuery query = Query.And(queryList);

            BsonDocument bson = new MongoDBHelper<EMPI_OPERATION>().FindOne(query).DELTA;

            //获取修改的详细信息
            return bson.ToJson();
        }


        /// <summary>
        /// 获取一个列表
        /// </summary>
        /// <param name="keywords">搜索关键词</param>
        /// <param name="start_Date">发生起始时间</param>
        /// <param name="end_Date">发生截止时间</param>
        /// <param name="resolved_Status">解决状态</param>
        /// <param name="page">分页排序参数</param>
        /// <returns></returns>
        public async Task<IList<EMPI_OPERATION>> GetList(string keywords, string start_Date, string end_Date, string operation_type, Pagination page)
        {
            List<IMongoQuery> querylist = new List<IMongoQuery>();

            if (!string.IsNullOrEmpty(keywords))
            {
                querylist.Add(Query.Matches("EMPI_ID_LIST", new BsonRegularExpression(new Regex(keywords))));
            }
            if (!string.IsNullOrEmpty(operation_type))
            {
                querylist.Add(Query.EQ("OPERATION_TYPE", operation_type));
            }
            if (!string.IsNullOrEmpty(start_Date))
            {
                querylist.Add(Query.GTE("OPERATE_TIME", start_Date));
            }
            if (!string.IsNullOrEmpty(end_Date))
            {
                querylist.Add(Query.LTE("OPERATE_TIME", end_Date));
            }

            var task2 = Task.Run(() =>
            {
                if (querylist.Count > 0)
                {
                    page.records = new MongoDBHelper<EMPI_OPERATION>().Count(Query.And(querylist.ToArray())).ToInt();
                }
                else
                {
                    page.records = new MongoDBHelper<EMPI_OPERATION>().Count(null).ToInt();
                }

            });

            IList<EMPI_OPERATION> data = null;
            var task1 = Task.Run(() =>
            {
                data = new MongoDBHelper<EMPI_OPERATION>().Find(querylist.ToArray(), page);
            });

            await Task.WhenAll(task1, task2);
            return data;
        }


        /// <summary>
        /// 获取实体
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public EMPI_OPERATION GetForm(string keyValue)
        {
            return new MongoDBHelper<EMPI_OPERATION>().FindOne(Query.EQ("OPERATION_ID", keyValue));
        }
    }
}
