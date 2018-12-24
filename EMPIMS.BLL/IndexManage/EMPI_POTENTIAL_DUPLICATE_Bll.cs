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

namespace EMPIMS.BLL.IndexManage
{
    public class EMPI_POTENTIAL_DUPLICATE_Bll
    {
        Log log = LogFactory.GetLogger(typeof(EMPI_POTENTIAL_DUPLICATE_Bll));

        /// <summary>
        /// 获取一个列表
        /// </summary>
        /// <param name="keywords">搜索关键词</param>
        /// <param name="start_Date">发生起始时间</param>
        /// <param name="end_Date">发生截止时间</param>
        /// <param name="resolved_Status">解决状态</param>
        /// <param name="page">分页排序参数</param>
        /// <returns></returns>
        public async Task<IList<EMPI_POTENTIAL_DUPLICATE>> GetList(string keywords, string start_Date, string end_Date, string resolved_Status, Pagination page)
        {
            IMongoQuery query = null;
            StringBuilder strBjs = new StringBuilder();

            List<IMongoQuery> querylist = new List<IMongoQuery>();
            List<IMongoQuery> listQuery = new List<IMongoQuery>();

            if (!string.IsNullOrEmpty(keywords))
            {
                listQuery.Add(Query.Matches("EMPI_ID_1", new BsonRegularExpression(new Regex(keywords))));
                listQuery.Add(Query.Matches("EMPI_ID_2", new BsonRegularExpression(new Regex(keywords))));
                query = Query.Or(listQuery);
                querylist.Add(query);
            }
            listQuery.Clear();
            if (!string.IsNullOrEmpty(start_Date))
            {
                listQuery.Add((Query.GTE("CREATE_TIME", start_Date)));
            }
            if (!string.IsNullOrEmpty(end_Date))
            {
                listQuery.Add(Query.LTE("CREATE_TIME", end_Date));
            }
            if (!string.IsNullOrEmpty(resolved_Status))
            {
                listQuery.Add(Query.EQ("RESOLVED_STATUS", resolved_Status));
            }
            if (listQuery.Count > 0)
            {
                query = Query.And(listQuery);
                querylist.Add(query);
            }

            var task2 = Task.Run(() =>
            {
                if (querylist.Count > 0)
                {
                    page.records = new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>().Count(Query.And(querylist.ToArray())).ToInt();
                }
                else
                {
                    page.records = new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>().Count(null).ToInt();
                }

            });

            IList<EMPI_POTENTIAL_DUPLICATE> data = null;
            var task1 = Task.Run(() =>
            {
                data = new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>().Find(querylist.ToArray(), page);
            });

            await Task.WhenAll(task1, task2);
            return data;
        }


        /// <summary>
        /// 获取潜在重复的两条记录
        /// </summary>
        /// <param name="empi_ID_1"></param>
        /// <param name="empi_ID_2"></param>
        /// <returns></returns>
        public string GetIndexJson(string keyValue)
        {
            EMPI_POTENTIAL_DUPLICATE entity = new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>().FindOne(Query.EQ("POTENTIAL_DUPLICATE_ID", keyValue));
            var data = "";
            if (entity != null)
            {
                BsonDocument doc = new MongoDBHelper<EMPI_OPERATION>().FindOne(Query.EQ("OPERATION_ID", entity.OPERATION_ID)).DELTA;
                data = doc.ToJson();
            }
            return data;
        }


        /// <summary>
        /// 放弃合并
        /// </summary>
        /// <param name="keyValue">主索引主键</param>
        public AjaxResult AbandonMerge(string keyValue)
        {
            AjaxResult msg = new AjaxResult { state = ResultType.error, message = "操作失败" };
            var dbHelper = new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>();
            IMongoQuery query = Query.EQ("POTENTIAL_DUPLICATE_ID", keyValue);
            EMPI_POTENTIAL_DUPLICATE entity = dbHelper.FindOne(query);

            //1.更新潜在重复为【已解决】
            if (entity != null)
            {
                List<IMongoUpdate> updateLi = new List<IMongoUpdate>();
                updateLi.Add(Update.Set("RESOLVED_STATUS", "R"));
                updateLi.Add(Update.Set("RESOLVED_BY", OperatorProvider.Provider.GetCurrent().DisplayName));
                updateLi.Add(Update.Set("RESOLVED_TIME", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                IMongoUpdate update = Update.Combine(updateLi);
                dbHelper.Update(query, update);

                //2.更新操作日志为【已解决】 备注：EMPI_OPERATION需要更新的字段和上面相同。 
                query = Query.EQ("OPERATION_ID", entity.OPERATION_ID);
                new MongoDBHelper<EMPI_OPERATION>().Update(query, update);
                msg.message = "操作成功";
                msg.state = ResultType.success;

            }
            return msg;
        }


        /// <summary>
        /// 合并(sourceId合并到targetId)
        /// </summary>
        /// <param name="keyValue">潜在重复主键</param>
        /// <param name="targetId">目标索引主键</param>
        /// <param name="sourceId">源目标主键</param>
        /// <returns></returns>
        public AjaxResult Merge(string keyValue, string targetId, string sourceId)
        {
            AjaxResult msg = new AjaxResult { state = "error", message = "合并失败" };
            var db_operation = new MongoDBHelper<EMPI_OPERATION>();
            var db_person_sbr = new MongoDBHelper<EMPI_PERSON_SBR>();

            #region 操作前的准备
            IMongoQuery query = Query.EQ("POTENTIAL_DUPLICATE_ID", keyValue);
            //潜在重复实体
            EMPI_POTENTIAL_DUPLICATE entityDupli = new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>().FindOne(query);

            query = Query.EQ("PERSON_SBR_ID", targetId);
            //保留的主索引
            BsonDocument targetEntity = db_person_sbr.FindOne(query).ToBsonDocument();
            targetEntity.Remove("_id");

            query = Query.EQ("PERSON_SBR_ID", sourceId);
            //被合并主索引
            BsonDocument sourceEntity = db_person_sbr.FindOne(query).ToBsonDocument();
            sourceEntity.Remove("_id");
            #endregion

            #region ========1.更新被合并的主索引sourceId状态和主索引号】========
            UpdateBuilder _update = new UpdateBuilder();
            _update.Set("STATUS", "I");//变更为【I：已停用】

            bool flag = false;
            if ((sourceEntity["ID_NO"] == "" && targetEntity["ID_NO"] == "") || (targetEntity["ID_NO"] != sourceEntity["ID_NO"]))
            {
                flag = true;
                _update.Set("EMPI_ID", targetEntity["EMPI_ID"]);//变更主索引号为targetId的主索引号
            }

            //IMongoUpdate update = Update.Set("STATUS", "I");
            db_person_sbr.Update(query, _update);
            #endregion


            #region ========2.更新【EMPI_POTENTIAL_DUPLICATE】为 已解决========
            query = Query.EQ("POTENTIAL_DUPLICATE_ID", keyValue);
            List<IMongoUpdate> updateLi = new List<IMongoUpdate>();
            updateLi.Add(Update.Set("RESOLVED_STATUS", "R"));
            updateLi.Add(Update.Set("RESOLVED_BY", OperatorProvider.Provider.GetCurrent().DisplayName));
            updateLi.Add(Update.Set("RESOLVED_TIME", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            IMongoUpdate update = Update.Set("STATUS", "I");
            update = Update.Combine(updateLi);
            new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>().Update(query, update);
            #endregion


            #region ========3.更新【EMPI_OPERATION】为 已解决========
            query = Query.EQ("OPERATION_ID", entityDupli.OPERATION_ID);
            new MongoDBHelper<EMPI_OPERATION>().Update(query, update);
            #endregion


            #region ========4.写操作日志========
            EMPI_OPERATION operation = new EMPI_OPERATION();
            operation.Create();
            operation.OPERATE_BY = OperatorProvider.Provider.GetCurrent().DisplayName;
            operation.OPERATION_TYPE = "MERGE";
            operation.MATCH_WEIGHT = entityDupli.MATCH_WEIGHT.ToString();
            operation.EMPI_ID_LIST = new string[] { targetEntity["EMPI_ID"].ToString(), sourceEntity["EMPI_ID"].ToString() };
            operation.PERSON_SBR_ID_LIST = new string[] { targetId, sourceId };
            operation.RESOLVED_STATUS = "R";
            operation.OPERATE_TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
            operation.OPERATE_BY = OperatorProvider.Provider.GetCurrent().DisplayName;

            #region 获取两个主索引的系统映射
            EMPI_MAPPING_Bll mapping_bll = new EMPI_MAPPING_Bll();

            IList<EMPI_MAPPING> mapLi_target = mapping_bll.GetListByPERSON_SBR_ID(targetId, "ORG_CODE", "SYS_CODE", "SYS_REC_ID", "PERSON_SBR_ID");
            IList<EMPI_MAPPING> mapLi_source = mapping_bll.GetListByPERSON_SBR_ID(sourceId, "ORG_CODE", "SYS_CODE", "SYS_REC_ID", "PERSON_SBR_ID");
            List<EMPI_MAPPING> docLi = new List<EMPI_MAPPING>();
            docLi.AddRange(mapLi_target);
            docLi.AddRange(mapLi_source);
            List<BsonDocument> docLi_sys = new List<BsonDocument>();
            BsonDocument doc = new BsonDocument();
            foreach (var item in docLi)
            {
                doc = item.ToBsonDocument();
                doc.Remove("_id");
                //doc.Remove("PERSON_SBR_ID");
                docLi_sys.Add(doc);
            }

            operation.SYS_REC_LIST = docLi_sys.ToArray();
            #endregion

            BsonDocument delta = new BsonDocument();
            delta.Add("MATCH_0", targetEntity);
            delta.Add("MATCH_1", sourceEntity);
            operation.DELTA = delta;
            db_operation.Insert(operation);
            #endregion


            #region =======5.更新合并的目标索引状态 已解决======
            db_person_sbr.Update(Query.EQ("PERSON_SBR_ID", targetId), Update.Set("STATUS", "A"));
            #endregion

            msg.state = "success";
            msg.message = "合并成功";

            if (flag)
            {
                try
                {
                    if (Configs.GetValue("IsEmpi_IDChangePush") == "on")
                    {
                        string url = Configs.GetValue("Empi_IDChangePushAddress");
                        if (!string.IsNullOrEmpty(url))
                        {
                            StringBuilder para = new StringBuilder();
                            para.AppendFormat(@"<REQUEST><ORG_CODE>{0}</ORG_CODE><SYS_CODE>{1}</SYS_CODE>
                                                        <SYS_REC_ID>{2}</SYS_REC_ID><EMPI_ID>{3}</EMPI_ID>
	                                                    <ID_TYPE_CODE>{4}</ID_TYPE_CODE><ID_TYPE_NAME>{5}</ID_TYPE_NAME>
	                                                    <ID_NO>{6}</ID_NO><NAME>{7}</NAME></REQUEST>",
                                               mapLi_source.First().ORG_CODE, mapLi_source.First().SYS_CODE, mapLi_source.First().SYS_REC_ID,
                                               targetEntity["EMPI_ID"], sourceEntity["ID_TYPE_CODE"], sourceEntity["ID_TYPE_NAME"], sourceEntity["ID_NO"], sourceEntity["NAME"]);

                            string parameter = "REQUEST=" + para.ToString();
                            log.Debug(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":" + "主索引号码变更推送参数：" + parameter);
                            string response = WebHelper.HttpWebRequest(url, parameter);
                            log.Debug(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":" + response);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error("主索引号码变更推送异常：" + ex.Message);
                }

            }
            return msg;
        }


        /// <summary>
        /// 拆分
        /// </summary>
        /// <param name="operaton_id"></param>
        /// <returns></returns>
        public AjaxResult Split(string operation_id)
        {
            AjaxResult msg = new AjaxResult();
            //获取合并之前的两条主索引的ID
            var dbHelper_operation = new MongoDBHelper<EMPI_OPERATION>();
            EMPI_OPERATION entity = dbHelper_operation.FindOne(Query.EQ("OPERATION_ID", operation_id));
            string[] person_sbr_id_arr = entity.PERSON_SBR_ID_LIST;

            List<IMongoQuery> queryLi = new List<IMongoQuery>();
            if (person_sbr_id_arr.Length > 0)
            {
                for (int i = 0; i < person_sbr_id_arr.Length; i++)
                {
                    queryLi.Add(Query.EQ("PERSON_SBR_ID", person_sbr_id_arr[i]));
                }

                var dbHelper_person_sbr = new MongoDBHelper<EMPI_PERSON_SBR>();
                //从主索引库查询两条数据              
                IList<EMPI_PERSON_SBR> person_sbr_li = new MongoDBHelper<EMPI_PERSON_SBR>().Find(Query.Or(queryLi));
                int count = 0;
                //判断两条数据的状态，只能有一条数据为停用状态                
                foreach (var item in person_sbr_li)
                {
                    if (item.STATUS == "I")
                    {
                        count++;
                    }
                }

                if (count == 0)
                {
                    msg.message = "该索引已被拆分,不能重复拆分";
                    msg.state = "error";
                }
                else if (count == 1)//没有被拆分过，可以拆分
                {
                    BsonDocument doc_delta = entity.DELTA;

                    BsonDocument doc_match_ = doc_delta["MATCH_0"] as BsonDocument;
                    BsonDocument doc_match_1 = doc_delta["MATCH_1"] as BsonDocument;

                    if (doc_match_1 != null && doc_match_ != null)
                    {
                        string person_sbr_id_1 = doc_match_1["PERSON_SBR_ID"].ToString();

                        #region =====1.修改主索引库被合并的===========

                        UpdateBuilder _update = new UpdateBuilder();
                        //①更新为已启用 
                        _update.Set("STATUS", "A");
                        //②更新主索引号为原始主索引号

                        bool flag = false;
                        if ((doc_match_["ID_NO"] == "" && doc_match_1["ID_NO"] == "") || (doc_match_["ID_NO"] != doc_match_1["ID_NO"]))
                        {
                            flag = true;
                            _update.Set("EMPI_ID", doc_match_1["EMPI_ID"]);//变更主索引号为targetId的主索引号
                        }

                        dbHelper_person_sbr.Update(Query.EQ("PERSON_SBR_ID", person_sbr_id_1), _update);
                        #endregion

                        #region =====2.更新合并到的记录状态======
                        //A:已激活；I:已停用；M:已合并
                        dbHelper_person_sbr.Update(Query.EQ("PERSON_SBR_ID", entity.PERSON_SBR_ID_LIST.First()), Update.Set("STATUS", "A"));
                        #endregion

                        #region =====3.写操作日志===============
                        EMPI_OPERATION entity_operation = new EMPI_OPERATION();
                        entity_operation.OPERATION_ID = Common.CreateNo();
                        entity_operation.OPERATE_TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                        entity_operation.MATCH_WEIGHT = entity.MATCH_WEIGHT;
                        entity_operation.OPERATION_TYPE = "SPLIT";
                        entity_operation.PERSON_SBR_ID_LIST = entity.PERSON_SBR_ID_LIST;
                        entity_operation.EMPI_ID_LIST = entity.EMPI_ID_LIST;
                        entity_operation.SYS_REC_LIST = entity.SYS_REC_LIST;
                        entity_operation.OPERATE_BY = OperatorProvider.Provider.GetCurrent().DisplayName.ToString();
                        entity_operation.RESOLVED_STATUS = "R";
                        EMPI_PERSON_SBR entity_match_0 = dbHelper_person_sbr.FindOne(Query.EQ("PERSON_SBR_ID", person_sbr_id_arr.First()));
                        BsonDocument doc_match_0 = entity_match_0.ToBsonDocument();
                        doc_match_0.Remove("_id");
                        doc_match_1.Remove("_id");
                        doc_delta.Clear();
                        doc_delta.Add("MATCH_0", doc_match_0);
                        doc_delta.Add("MATCH_1", doc_match_1);
                        entity_operation.DELTA = doc_delta;
                        dbHelper_operation.Insert(entity_operation);
                        #endregion

                        string _operation_id = Common.CreateNo();

                        #region 4.重新写潜在重复记录
                        EMPI_POTENTIAL_DUPLICATE entity_dupli = new EMPI_POTENTIAL_DUPLICATE();
                        entity_dupli.POTENTIAL_DUPLICATE_ID = Common.CreateNo();
                        entity_dupli.RESOLVED_STATUS = "U";
                        entity_dupli.EMPI_ID_1 = entity.EMPI_ID_LIST.First();
                        entity_dupli.EMPI_ID_2 = entity.EMPI_ID_LIST.Last();
                        entity_dupli.PERSON_SBR_ID_1 = entity.PERSON_SBR_ID_LIST.First();
                        entity_dupli.PERSON_SBR_ID_2 = entity.PERSON_SBR_ID_LIST.Last();
                        entity_dupli.MATCH_WEIGHT = entity.MATCH_WEIGHT.ToInt();
                        entity_dupli.OPERATION_ID = _operation_id;
                        entity_dupli.CREATE_TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>().Insert(entity_dupli);
                        #endregion

                        #region 5.重新写潜在重复日志
                        EMPI_OPERATION _operation = new EMPI_OPERATION();
                        _operation.OPERATION_ID = _operation_id;
                        _operation.EMPI_ID_LIST = entity.EMPI_ID_LIST;
                        _operation.PERSON_SBR_ID_LIST = entity.PERSON_SBR_ID_LIST;
                        _operation.RESOLVED_STATUS = "U";
                        _operation.OPERATION_TYPE = "POTENTIAL_DUPLICATE";
                        _operation.OPERATE_TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                        _operation.OPERATE_BY = OperatorProvider.Provider.GetCurrent().DisplayName.ToString();
                        _operation.SYS_REC_LIST = entity.SYS_REC_LIST;
                        _operation.MATCH_WEIGHT = entity.MATCH_WEIGHT;
                        BsonDocument _bson_delte = new BsonDocument();
                        _bson_delte.Add("DUPLICATE_0", doc_match_0);
                        _bson_delte.Add("DUPLICATE_1", doc_match_1);
                        _operation.DELTA = _bson_delte;
                        new MongoDBHelper<EMPI_OPERATION>().Insert(_operation);
                        #endregion

                        msg.message = "拆分成功";
                        msg.state = "success";

                        if (flag)
                        {
                            try
                            {
                                if (Configs.GetValue("IsEmpi_IDChangePush") == "on")
                                {
                                    string url = Configs.GetValue("Empi_IDChangePushAddress");
                                    if (!string.IsNullOrEmpty(url))
                                    {
                                        StringBuilder para = new StringBuilder();
                                        para.AppendFormat(@"<REQUEST><ORG_CODE>{0}</ORG_CODE><SYS_CODE>{1}</SYS_CODE>
                                                        <SYS_REC_ID>{2}</SYS_REC_ID><EMPI_ID>{3}</EMPI_ID>
	                                                    <ID_TYPE_CODE>{4}</ID_TYPE_CODE><ID_TYPE_NAME>{5}</ID_TYPE_NAME>
	                                                    <ID_NO>{6}</ID_NO><NAME>{7}</NAME></REQUEST>",
                                                           entity.SYS_REC_LIST.Last()["ORG_CODE"], entity.SYS_REC_LIST.Last()["SYS_CODE"], entity.SYS_REC_LIST.Last()["SYS_REC_ID"],
                                                          doc_match_1["EMPI_ID"], doc_match_1["ID_TYPE_CODE"], doc_match_1["ID_TYPE_NAME"], doc_match_1["ID_NO"], doc_match_1["NAME"]);

                                        string parameter = "REQUEST=" + para.ToString();
                                        log.Debug(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":" + "主索引号码变更推送参数：" + parameter);
                                        string response = WebHelper.HttpWebRequest(url, parameter);
                                        log.Debug(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":" + response);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                log.Error("主索引号码变更推送异常：" + ex.Message);
                            }
                        }
                    }
                    else
                    {
                        msg.message = "被合并数据不存在操作日志中";
                        msg.state = "error";
                    }
                }
                else if (count == 2)//已经被拆分，不能拆分
                {
                    msg.message = "数据被停用，无法拆分";
                    msg.state = "warning";
                }
                else
                {
                    msg.message = "数据发生错误";
                    msg.state = "error";
                }
            }
            else
            {
                msg.message = "数据发生错误";
                msg.state = "error";
            }


            return msg;
        }


        /// <summary>
        /// 判断是否存在潜在重复
        /// </summary>
        /// <param name="entity">录入的主索引</param>
        /// <param name="empi_id_2">输出的潜在重复id</param>
        /// <param name="weight">权重值</param>
        /// <param name="operation">进行的操作</param>
        public void IsDuplicate(EMPI_PERSON_SBR entity, out string person_sbr_id, out int weight, out string operation)
        {
            operation = "";
            //empi_id_2 = "";
            weight = 0;
            person_sbr_id = "";

            #region //=======0.获取所有匹配设置数据=====================
            IList<EMPI_MATCHCONFIG> all_match = new List<EMPI_MATCHCONFIG>();
            all_match = new MongoDBHelper<EMPI_MATCHCONFIG>().FindAll();
            #endregion


            #region //=======1.获取匹配字段和权重值 =====================
            var field = all_match.Where(a => a.Type == 1).ToList();
            #endregion


            #region //=======2.获取标准值，潜在重复区间============================
            int result_1 = 0;//_1:潜在重复最小值
            int result_2 = 0;//_2:潜在重复最大值
            var standard = all_match.Where(a => a.Type == 0).ToList();
            if (standard.Count > 0)
            {
                result_1 = standard.FirstOrDefault().Value;
                result_2 = standard.LastOrDefault().Value;
            }
            #endregion


            #region //=======3.生成所有符合潜在重复区间的组合 ============
            //hs的key存字段，value存字段对应的权重
            //hsDict的key存组合的权重，value存上面的hs
            //潜在重复组合
            Dictionary<string, Hashtable> hsDict_duplit = new Dictionary<string, Hashtable>();

            //自动合并组合
            Dictionary<string, Hashtable> hsDict_merge = new Dictionary<string, Hashtable>();

            Hashtable hs_previw = new Hashtable();//上一次的

            for (int i = 1; i < 1 << field.Count; i++)//从1循环到2^N  
            {
                Hashtable hs = new Hashtable();
                int sum = 0;
                for (int j = 0; j < field.Count; j++)
                {
                    if ((i & 1 << j) != 0)//用i与2^j进行位与运算，若结果不为0,则表示第j位不为0,从数组中取出第j个数  
                    {
                        sum += Convert.ToInt32(field[j].Value);
                        hs.Add(field[j].Code, field[j].Value);
                    }
                }

                int count1 = 0;
                int count2 = hs.Keys.Count;
                string flag = "both";

                if (sum >= result_1 && sum <= result_2)
                {
                    IsRepeat(ref hsDict_duplit, ref hs_previw, ref hs, ref count1, count2, ref flag);
                    if (hs.Count > 0)
                    {
                        hsDict_duplit.Add(Guid.NewGuid().ToString() + "_" + sum, hs);
                    }
                }
                else if (sum > result_2)
                {
                    IsRepeat(ref hsDict_merge, ref hs_previw, ref hs, ref count1, count2, ref flag);
                    if (hs.Count > 0)
                    {
                        hsDict_merge.Add(Guid.NewGuid().ToString() + "_" + sum, hs);
                    }
                }
            }

            //降序排序一下，匹配最大权重的组合排最前面
            //hsDict_duplit = hsDict_duplit.OrderByDescending(a => (Convert.ToInt32(a.Key.Split('_').Last()))).ToDictionary(p => p.Key, o => o.Value);
            //hsDict_merge = hsDict_merge.OrderByDescending(a => (Convert.ToInt32(a.Key.Split('_').Last()))).ToDictionary(p => p.Key, o => o.Value);
            #endregion

            var data = hsDict_duplit.Concat(hsDict_merge);

            //降序排序
            data = data.OrderByDescending(a => (Convert.ToInt32(a.Key.Split('_').Last())));
            //key存匹配的权重值，value存主索引的实体
            Dictionary<string, EMPI_PERSON_SBR> entity_sbr_dict = new Dictionary<string, EMPI_PERSON_SBR>();

            if (data.Count() > 0)
            {
                #region //=======4.根据传入实体匹配相似数据=====================
                List<IMongoQuery> queryLi = new List<IMongoQuery>();
                string name = "";
                string value = "";

                BsonDocument doc = entity.ToBsonDocument();
                IMongoQuery query = null;
                EMPI_PERSON_SBR entity_sbr = null;
                var dbhelper_sbr = new MongoDBHelper<EMPI_PERSON_SBR>();
                List<BsonValue> empi_id_li = new List<BsonValue>();
                List<string> person_sbr_id_li = new List<string>();
                IEnumerable<BsonValue> ie_bson = null;
                IEnumerable<BsonValue> ie_string = null;
                //根据符合标准的组合拼接查询条件并找到对应数据
                foreach (var item in data)
                {
                    queryLi.Clear();

                    weight = item.Key.Split('_').Last().ToInt();//权重

                    foreach (var hs_key in item.Value.Keys)
                    {

                        name = hs_key.ToString();//字段

                        if (doc[name] != BsonNull.Value && doc[name] != "")
                        {
                            value = doc[name].ToString();

                            //判断是否是脏数据,如果是脏数据，生成一个全局唯一ID
                            if (Common.IsDirtyData(value))
                            {
                                value = Guid.NewGuid().ToString();
                            }
                            queryLi.Add(Query.EQ(name, value));//拼接匹配查询条件
                        }
                        else
                        {
                            queryLi.Add(Query.EQ("PERSON_SBR_ID", "##&#&#^#&#^"));
                        }
                    }

                    //if (empi_id_li.Count > 0)
                    //{
                    //    //已经查询到了的，排除掉
                    //    ie_bson = empi_id_li;
                    //    queryLi.Add(Query.NotIn("EMPI_ID", ie_bson));
                    //}

                    //if (person_sbr_id_li.Count > 0)
                    //{
                    //    ie_string = person_sbr_id_li as IEnumerable<BsonValue>;
                    //    queryLi.Add(Query.NotIn("PERSON_SBR_ID", ie_string));
                    foreach (var sbr_id in person_sbr_id_li)
                    {
                        queryLi.Add(Query.NE("PERSON_SBR_ID", sbr_id));
                    }
                    //}

                    //相似数据比较时，要将自己排除在外。【报错了！！！！！！！！！！！！！】
                    queryLi.Add(Query.NE("PERSON_SBR_ID", entity.PERSON_SBR_ID));

                    //必须是【已启用】的主索引
                    queryLi.Add(Query.NE("STATUS", "I"));

                    query = Query.And(queryLi.ToArray());

                    entity_sbr = dbhelper_sbr.FindOne(query);

                    if (entity_sbr != null)
                    {
                        //已经查询过了的主索引ID
                        //empi_id_li.Add(entity_sbr.EMPI_ID);
                        person_sbr_id_li.Add(entity_sbr.PERSON_SBR_ID);

                        //加一个GUID前缀，防止key重复导致报错
                        entity_sbr_dict.Add(Guid.NewGuid().ToString() + "_" + weight, entity_sbr);
                    }
                }
                #endregion

                #region //=======5.最后的录入结果判断====================
                var dbhelper_mapping = new MongoDBHelper<EMPI_MAPPING>();

                //只处理第一条数据，匹配权重最大的数据
                if (entity_sbr_dict.Count() > 0)
                {
                    weight = entity_sbr_dict.First().Key.Split('_').Last().ToInt();
                    //自动合并
                    if (weight > result_2)
                    {
                        operation = "merge";
                    }
                    else
                    {
                        operation = "duplicate";
                    }
                    //empi_id_2 = entity_sbr_dict.First().Value.EMPI_ID;
                    person_sbr_id = entity_sbr_dict.First().Value.PERSON_SBR_ID;
                }
                else//没有找到数据
                {
                    weight = 0;
                    //empi_id_2 = "";
                    person_sbr_id = "";
                }
                #endregion
            }
            else//自定义的匹配规则出现逻辑错误
            {
                throw new Exception("自定义的匹配规则不符合逻辑，请确保所有权重相加大于或等于潜在重复区间最小值。");
            }
        }

        /// <summary>
        /// 去掉父集，只留下最后最小子集
        /// </summary>
        /// <param name="hsDict"></param>
        /// <param name="hs_previw"></param>
        /// <param name="hs"></param>
        /// <param name="count1"></param>
        /// <param name="count2"></param>
        /// <param name="flag"></param>
        private static void IsRepeat(ref Dictionary<string, Hashtable> hsDict, ref Hashtable hs_previw, ref Hashtable hs, ref int count1, int count2, ref string flag)
        {
            foreach (var key_dict in hsDict.Keys)
            {
                hs_previw = hsDict[key_dict];
                count1 = hsDict[key_dict].Keys.Count;

                if (count1 > count2)
                {
                    foreach (var key in hs.Keys)
                    {
                        if (!hs_previw.ContainsKey(key))
                        {
                            flag = "both";
                            break;
                        }
                        else
                        {
                            flag = "curr";
                        }
                    }
                }
                else
                {
                    foreach (var key in hs_previw.Keys)
                    {
                        if (!hs.ContainsKey(key))
                        {
                            flag = "both";
                            break;
                        }
                        else
                        {
                            flag = "previ";
                        }
                    }
                }

                if (flag == "previ")
                {
                    //取上一个组合，当前组合去掉，直接跳到下次循环
                    hs.Clear();
                    continue;
                }
                else if (flag == "curr")
                {
                    //取当前的组合，上一个组合需要删除掉                            
                    hsDict.Remove(key_dict);
                }
            }
        }
    }
}
