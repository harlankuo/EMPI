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
using System.Threading;
using System.Diagnostics;

namespace EMPIMS.BLL.SystemManage
{
    public class DefaultPage_Bll
    {

        Log log = LogFactory.GetLogger(typeof(DefaultPage_Bll));
        /// <summary>
        /// 获取首页
        /// </summary>
        /// <returns></returns>
        public async Task<Hashtable> GetDashboardRecord()
        {
            Hashtable hs = new Hashtable();

            //主索引记录
            var task1 = Task.Run(() =>
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                long indexCount = new MongoDBHelper<EMPI_PERSON_SBR>().Count(Query.EQ("STATUS", "A"));
                hs.Add("indexCount", indexCount);
                stopWatch.Stop();
                log.Debug("获取主索引记录计时：" + stopWatch.ElapsedMilliseconds + "ms\n");
            });

            //合并记录                      
            var task2 = Task.Run(() =>
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                var dbHelper_operation = new MongoDBHelper<EMPI_OPERATION>();
                long mergeCount = dbHelper_operation.Find(Query.EQ("OPERATION_TYPE", "MERGE"), "EMPI_ID_LIST").Distinct().Count();
                hs.Add("mergeCount", mergeCount);
                stopWatch.Stop();
                log.Debug("获取合并记录计时：" + stopWatch.ElapsedMilliseconds + "ms\n");
            });

            //潜在重复(未解决)
            var task3 = Task.Run(() =>
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                long duplicateCount = new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>().Count(Query.EQ("RESOLVED_STATUS", "U"));
                hs.Add("duplicateCount", duplicateCount);
                stopWatch.Stop();
                log.Debug("获取潜在重复计时：" + stopWatch.ElapsedMilliseconds + "ms\n");
            });
            await Task.WhenAll(task1, task2, task3);
            return hs;
        }


        /// <summary>
        /// 本年度拆分、合并处理统计
        /// </summary>
        /// <returns></returns>
        public async Task<Hashtable> GetSalaryChart()
        {
            Hashtable hs = new Hashtable();
            List<IMongoQuery> queryLi = new List<IMongoQuery>();
            IMongoQuery query = Query.EQ("OPERATION_TYPE", "MERGE");
            queryLi.Add(query);
            query = Query.EQ("OPERATION_TYPE", "SPLIT");
            queryLi.Add(query);
            //获取所有合并、拆分记录数据
            var data = await Task.Run(() => new MongoDBHelper<EMPI_OPERATION>().Find(Query.Or(queryLi.ToArray()), "OPERATION_ID", "OPERATION_TYPE", "OPERATE_TIME"));
            //获取本年度合并拆分记录
            var li = from item in data
                     where Convert.ToDateTime(item.OPERATE_TIME).Year == DateTime.Now.Year
                     group new { type = item.OPERATION_TYPE, month = Convert.ToDateTime(item.OPERATE_TIME).Month } by
                     new { type = item.OPERATION_TYPE, month = Convert.ToDateTime(item.OPERATE_TIME).Month } into g
                     orderby g.Key.type, g.Key.month ascending
                     select new { count = g.Count(), type = g.Key.type, month = g.Key.month };
            List<int> mergeLi = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            List<int> splitLi = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            try
            {
                foreach (var item in li)
                {
                    if (item.type == "MERGE")
                    {
                        mergeLi[item.month - 1] = item.count;
                    }
                    else
                    {
                        splitLi[item.month - 1] = item.count;
                    }

                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                throw;
            }

            hs.Add("mergeData", mergeLi.ToArray());
            hs.Add("splitData", splitLi.ToArray());
            return hs;
        }


        /// <summary>
        /// 获取潜在重复处理、未处理统计
        /// </summary>
        /// <returns></returns>
        public async Task<Hashtable> GetLeaveChart()
        {
            Hashtable hs = new Hashtable();
            var data = await Task.Run(() => new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>().FindAll());
            var tmp = from item in data
                      group item.RESOLVED_STATUS by new { status = item.RESOLVED_STATUS } into g
                      select new { count = g.Count(), status = g.Key.status };

            hs.Add("Resolved", 0);
            hs.Add("UnResolved", 0);
            foreach (var item in tmp)
            {
                if (item.status == "R")
                {
                    hs["Resolved"] = item.count;
                }
                else
                {
                    hs["UnResolved"] = item.count;
                }
            }
            return hs;
        }


        /// <summary>
        /// 获取未解决潜在重复
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<EMPI_POTENTIAL_DUPLICATE>> GetDuplicate()
        {
            var data = await Task.Run(() => new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>().Find(Query.EQ("RESOLVED_STATUS", "U"))
                .OrderBy(a => Convert.ToDateTime(a.CREATE_TIME)).Take(10));
            return data;
        }
    }
}
