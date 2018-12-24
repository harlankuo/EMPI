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
using System.Threading;

namespace EMPIMS.BLL.IndexManage
{

    public class EMPI_PERSON_SBR_Bll
    {

        Log log = LogFactory.GetLogger(typeof(EMPI_PERSON_SBR_Bll));

        /// <summary>
        /// 普通检索
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="start_Birthday"></param>
        /// <param name="end_Birthday"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<IList<EMPI_PERSON_SBR>> GetList(string keywords, string status, Pagination page)
        {

            IMongoQuery query = null;

            List<IMongoQuery> querylist = new List<IMongoQuery>();

            if (!string.IsNullOrEmpty(keywords))
            {
                //判断是否包含汉字
                if (Regex.IsMatch(keywords, @"^[\u4e00-\u9fa5]+$"))
                {
                    querylist.Add(Query.Matches("NAME", new BsonRegularExpression(new Regex(keywords))));
                }
                else
                {
                    querylist.Add(Query.Matches("ID_NO", new BsonRegularExpression(new Regex(keywords))));
                }
            }

            if (!string.IsNullOrEmpty(status))
            {
                querylist.Add((Query.EQ("STATUS", status)));
                query = Query.And(querylist);
            }

            var task1 = Task.Run(() =>
            {
                page.records = new MongoDBHelper<EMPI_PERSON_SBR>().Count(query).ToInt();
            });

            IList<EMPI_PERSON_SBR> data = null;
            var task2 = Task.Run(() =>
            {
                data = new MongoDBHelper<EMPI_PERSON_SBR>().Find(querylist.ToArray(), page);
            });

            await Task.WhenAll(task1, task2);
            return data;
        }


        /// <summary>
        /// 高级检索
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="pagination"></param>
        /// <returns></returns>
        public async Task<IList<EMPI_PERSON_SBR>> GetListBySup(Dictionary<string, object> dict, Pagination pagination)
        {
            List<IMongoQuery> queryList = new List<IMongoQuery>();
            if (dict.Count > 0)
            {
                foreach (var item in dict)
                {
                    if (!string.IsNullOrEmpty(item.Value.ToString()))
                    {
                        string value = item.Value.ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (item.Key.Contains("Start_"))
                            {
                                queryList.Add(Query.GTE(item.Key.ToString().Replace("Start_", ""), value.ToDate()));
                            }
                            else if (item.Key.Contains("End_"))
                            {
                                queryList.Add(Query.LTE(item.Key.ToString().Replace("End_", ""), value.ToDate()));
                            }
                            else
                            {
                                queryList.Add(Query.Matches(item.Key.ToString(), new BsonRegularExpression(new Regex(value))));
                            }
                        }
                    }
                }
            }

            IList<EMPI_PERSON_SBR> data = null;

            var task1 = Task.Run(() =>
            {
                data = new MongoDBHelper<EMPI_PERSON_SBR>().Find(queryList.ToArray(), pagination);
            });

            var task2 = Task.Run(() =>
            {
                pagination.records = new MongoDBHelper<EMPI_PERSON_SBR>().Count(Query.And(queryList.ToArray())).ToInt();
            });

            await Task.WhenAll(task1, task2);
            return data;
        }


        /// <summary>
        /// 获取一个实体
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public EMPI_PERSON_SBR GetForm(string keyValue)
        {
            IMongoQuery query = Query.EQ("PERSON_SBR_ID", keyValue);
            return new MongoDBHelper<EMPI_PERSON_SBR>().FindOne(query);
        }

        /// <summary>
        /// 获取主索引明细数据
        /// </summary>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public async Task<Hashtable> GetDetailsJson(string keyValue)
        {
            Hashtable hs = new Hashtable();
            BsonDocument doc = new BsonDocument();
            IMongoQuery query = Query.EQ("PERSON_SBR_ID", keyValue);
            var task1 = Task.Run(() =>
            {
                var person_sbr = new MongoDBHelper<EMPI_PERSON_SBR>().FindOne(query);

                var dbhelper_operation = new MongoDBHelper<EMPI_OPERATION>();

                hs.Add("person", person_sbr);

                query = Query.EQ("PERSON_SBR_ID_LIST", keyValue);
                var operation = dbhelper_operation.Find(query, "OPERATE_TIME", "OPERATION_TYPE", "SYS_REC_LIST", "PERSON_SBR_ID_LIST");


                //相关合并日志
                List<string> str_li = new List<string>();

                foreach (var item in operation)
                {
                    if (item.OPERATION_TYPE == "MERGE")
                    {
                        str_li.AddRange(item.PERSON_SBR_ID_LIST);
                    }
                }

                var distinct = str_li.Distinct();
                List<BsonValue> li_bsonVal = new List<BsonValue>();

                foreach (var item in distinct)
                {
                    li_bsonVal.Add((BsonValue)item);
                }

                operation.OrderByDescending(a => Convert.ToDateTime(a.OPERATE_TIME)).Take(15);
                hs.Add("operation", operation);

                if (li_bsonVal.Count() > 0)
                {
                    query = Query.In("PERSON_SBR_ID", li_bsonVal);
                }
                else
                {
                    query = Query.EQ("PERSON_SBR_ID", keyValue);
                }

                var mapping = new MongoDBHelper<EMPI_MAPPING>().Find(query, "ORG_CODE", "SYS_CODE", "SYS_REC_ID");

                //1.根据当前主索引主键获取相关合并日志
                //2.获取合并日志的另外一个主索引主键
                //3.根据另外一个主索引主键获取映射关系

                hs.Add("mapping", mapping);

            });
            //var task2 = Task.Run(() =>
            //{
            //    var mapping = new MongoDBHelper<EMPI_MAPPING>().Find(query, "ORG_CODE", "SYS_CODE", "SYS_REC_ID");
            //    hs.Add("mapping", mapping);
            //});


            //var task3 = Task.Run(() =>
            //{
            //    query = Query.EQ("PERSON_SBR_ID_LIST", keyValue);
            //    var operation = new MongoDBHelper<EMPI_OPERATION>().Find(query, "OPERATE_TIME", "OPERATION_TYPE", "SYS_REC_LIST");
            //    hs.Add("operation", operation);
            //    operation.OrderByDescending(a => Convert.ToDateTime(a.OPERATE_TIME)).Take(15);
            //});
            //await Task.WhenAll(task1, task2, task3);

            await Task.WhenAll(task1);
            return hs;
        }


        /// <summary>
        /// 提交实体
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="keyValue"></param>
        /// <param name="updateField"></param>
        /// <param name="isService">是否通过外部服务调用，默认不是</param>
        /// <returns></returns>
        public AjaxResult SubmitForm(EMPI_PERSON_SBR entity, string keyValue, string[] updateField, string isService = "")
        {
            AjaxResult msg = new Code.AjaxResult();
            var dbhelper_sbr = new MongoDBHelper<EMPI_PERSON_SBR>();
            var dbhelper_operation = new MongoDBHelper<EMPI_OPERATION>();
            EMPI_OPERATION operation = new EMPI_OPERATION();
            operation.Create();
            if (string.IsNullOrEmpty(isService))
            {
                operation.OPERATE_BY = OperatorProvider.Provider.GetCurrent().DisplayName.ToString();
            }
            else
            {
                operation.OPERATE_BY = entity.CREATE_BY;
            }
            operation.RESOLVED_STATUS = "R";

            #region 判断潜在重复参数
            string person_sbr_id_2 = "";
            int weight = 0;
            string operation_type = "";
            #endregion

            IMongoQuery query = Query.EQ("PERSON_SBR_ID", keyValue);
            BsonDocument bson_mapping = new BsonDocument();
            bool flag = false;

            EMPI_PERSON_SBR entity_sbr_merge = new EMPI_PERSON_SBR();
            //存在患者EMPI_ID => 修改
            if (!string.IsNullOrEmpty(keyValue))
            {
                //修改之前的实体
                EMPI_PERSON_SBR entity_befor = dbhelper_sbr.FindOne(query);
                entity._id = entity_befor._id;
                entity.EMPI_ID = entity_befor.EMPI_ID;


                //只有激活的主索引才执行匹配计算
                if (entity_befor.STATUS == "A")
                {
                    new EMPI_POTENTIAL_DUPLICATE_Bll().IsDuplicate(entity, out person_sbr_id_2, out weight, out operation_type);
                }
                else
                {
                    entity.STATUS = "I";
                }

                #region 获取系统映射
                EMPI_MAPPING mapping = new MongoDBHelper<EMPI_MAPPING>().FindOne(query, "ORG_CODE", "SYS_CODE", "SYS_REC_ID", "PERSON_SBR_ID");

                List<BsonDocument> bsonLi = new List<BsonDocument>();
                if (mapping != null)
                {
                    bson_mapping = mapping.ToBsonDocument();
                    bson_mapping.Remove("_id");
                    bsonLi.Add(bson_mapping);
                }
                #endregion

                //写修改操作日志
                operation.SYS_REC_LIST = bsonLi.ToArray();
                operation.PERSON_SBR_ID_LIST = new string[] { keyValue };
                operation.OPERATION_TYPE = "UPDATE";
                BsonDocument bson = new BsonDocument();
                BsonDocument bson_befor = entity_befor.ToBsonDocument();
                bson_befor.Remove("_id");
                bson.Add("BEFORE_UPDATE", bson_befor);

                if (operation_type == "merge")
                {
                    entity.STATUS = "I";
                }

                //修改主索引
                dbhelper_sbr.Update(query, entity, updateField);

                operation.EMPI_ID_LIST = new string[] { entity_befor.EMPI_ID };
                //修改之后的实体
                EMPI_PERSON_SBR entity_after = dbhelper_sbr.FindOne(query);
                BsonDocument bson_after = entity_after.ToBsonDocument();
                bson_after.Remove("_id");
                bson.Add("AFTER_UPDATE", bson_after);
                operation.DELTA = bson;
                //写入操作日志
                dbhelper_operation.Insert(operation);

                #region 处理潜在重复
                if (!string.IsNullOrEmpty(person_sbr_id_2) && weight != 0 && operation_type == "duplicate")
                {

                    EMPI_PERSON_SBR entity_sbr_dupli = dbhelper_sbr.FindOne(Query.EQ("PERSON_SBR_ID", person_sbr_id_2));

                    operation = new EMPI_OPERATION();
                    operation.Create();

                    //插入潜在重复
                    EMPI_POTENTIAL_DUPLICATE entity_dupli = new EMPI_POTENTIAL_DUPLICATE();
                    //entity_dupli._id = new ObjectId();
                    entity_dupli.POTENTIAL_DUPLICATE_ID = Common.CreateNo();
                    entity_dupli.RESOLVED_STATUS = "U";
                    entity_dupli.CREATE_TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    entity_dupli.EMPI_ID_1 = entity.EMPI_ID;
                    entity_dupli.EMPI_ID_2 = entity_sbr_dupli.EMPI_ID;

                    entity_dupli.PERSON_SBR_ID_1 = entity.PERSON_SBR_ID;
                    entity_dupli.PERSON_SBR_ID_2 = person_sbr_id_2;
                    entity_dupli.MATCH_WEIGHT = weight;
                    entity_dupli.OPERATION_ID = operation.OPERATION_ID;
                    new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>().Insert(entity_dupli);

                    //写潜在重复操作日志                                       
                    //operation._id = new ObjectId();
                    operation.EMPI_ID_LIST = new string[] { entity.EMPI_ID, entity_sbr_dupli.EMPI_ID };
                    operation.MATCH_WEIGHT = weight.ToString();
                    operation.OPERATION_TYPE = "POTENTIAL_DUPLICATE";
                    operation.RESOLVED_STATUS = "U";
                    if (string.IsNullOrEmpty(isService))
                    {
                        operation.OPERATE_BY = OperatorProvider.Provider.GetCurrent().DisplayName;
                    }
                    else
                    {
                        operation.OPERATE_BY = entity.CREATE_BY;
                    }

                    #region 获取潜在重复主索引的系统映射
                    mapping = new MongoDBHelper<EMPI_MAPPING>().FindOne(Query.EQ("PERSON_SBR_ID", person_sbr_id_2));
                    if (mapping != null)
                    {
                        bson_mapping = mapping.ToBsonDocument();
                        bson_mapping.Remove("_id");
                        bsonLi.Add(bson_mapping);
                        operation.SYS_REC_LIST = bsonLi.ToArray();
                    }
                    #endregion

                    BsonDocument bson_delte = new BsonDocument();
                    BsonDocument bson_dupli_0 = entity.ToBsonDocument();
                    BsonDocument bson_dupli_1 = entity_sbr_dupli.ToBsonDocument();

                    if (bson_dupli_1 != null)
                    {
                        //移除ObjectId，便于Json处理
                        bson_dupli_0.Remove("_id");
                        bson_dupli_1.Remove("_id");

                        bson_delte.Add("DUPLICATE_0", bson_dupli_0);
                        bson_delte.Add("DUPLICATE_1", bson_dupli_1);

                        operation.DELTA = bson_delte;
                    }
                    else
                    {
                        msg.state = "error";
                        msg.message = "修改失败。";

                    }
                    dbhelper_operation.Insert(operation);
                    msg.state = "info";
                    msg.message = "修改成功，根据匹配结果，新增主索引与【" + person_sbr_id_2 + "】发生潜在重复,请及时解决";
                }
                #endregion
                #region 自动合并
                else if (!string.IsNullOrEmpty(person_sbr_id_2) && weight != 0 && operation_type == "merge")
                {

                    entity_sbr_merge = dbhelper_sbr.FindOne(Query.EQ("PERSON_SBR_ID", person_sbr_id_2));

                    //写自动合并操作日志  
                    operation = new EMPI_OPERATION();
                    operation.Create();
                    operation.EMPI_ID_LIST = new string[] { entity_sbr_merge.EMPI_ID, entity.EMPI_ID };
                    operation.PERSON_SBR_ID_LIST = new string[] { person_sbr_id_2, entity.PERSON_SBR_ID };
                    operation.MATCH_WEIGHT = weight.ToString();
                    operation.OPERATION_TYPE = "MERGE";
                    operation.RESOLVED_STATUS = "R";
                    if (string.IsNullOrEmpty(isService))
                    {
                        operation.OPERATE_BY = OperatorProvider.Provider.GetCurrent().DisplayName;
                    }
                    else
                    {
                        operation.OPERATE_BY = entity.CREATE_BY;
                    }

                    #region 系统映射
                    mapping = new MongoDBHelper<EMPI_MAPPING>().FindOne(Query.EQ("PERSON_SBR_ID", person_sbr_id_2));
                    bson_mapping = mapping.ToBsonDocument();
                    bson_mapping.Remove("_id");
                    bsonLi.Add(bson_mapping);
                    bsonLi.Reverse();
                    operation.SYS_REC_LIST = bsonLi.ToArray();
                    #endregion

                    BsonDocument bson_delte = new BsonDocument();
                    BsonDocument bson_dupli_0 = entity_sbr_merge.ToBsonDocument();
                    BsonDocument bson_dupli_1 = entity.ToBsonDocument();
                    if (bson_dupli_1 != null)
                    {

                        if (entity_sbr_merge.EMPI_ID != entity.EMPI_ID)
                        {
                            flag = true;
                            dbhelper_sbr.Update(Query.EQ("PERSON_SBR_ID", entity.PERSON_SBR_ID), Update.Set("EMPI_ID", entity_sbr_merge.EMPI_ID));
                        }

                        //移除ObjectId，便于Json处理
                        bson_dupli_0.Remove("_id");
                        bson_dupli_1.Remove("_id");
                        bson_delte.Add("MATCH_0", bson_dupli_0);
                        bson_delte.Add("MATCH_1", bson_dupli_1);
                        operation.DELTA = bson_delte;
                        dbhelper_operation.Insert(operation);
                        msg.state = "info";
                        msg.message = "修改成功，根据匹配结果，修改后主索引【<b>" + entity.PERSON_SBR_ID + "</b>】与【<b>" + person_sbr_id_2 + "</b>】高度匹配,已自动合并";
                    }
                    else
                    {
                        msg.state = "error";
                        msg.message = "修改失败。";

                    }
                }
                #endregion
                else
                {
                    msg.state = "info";
                    msg.message = "修改成功";
                }
                return msg;
            }
            else//不存在主索引数据 => 新增
            {
                //判断潜在重复
                new EMPI_POTENTIAL_DUPLICATE_Bll().IsDuplicate(entity, out person_sbr_id_2, out weight, out operation_type);

                if (string.IsNullOrEmpty(isService))
                {
                    entity.Create();
                    entity.CREATE_BY = OperatorProvider.Provider.GetCurrent().UserID.ToString();//当前人
                    entity.IP_MEDICAL_RECORD_NO = Common.GetIP() + "_" + Common.getMNum();//IP和机器码
                }

                query = Query.EQ("PERSON_SBR_ID", entity.PERSON_SBR_ID);

                #region 获取新注册索引系统映射
                EMPI_MAPPING mapping = new MongoDBHelper<EMPI_MAPPING>().FindOne(query);
                List<BsonDocument> bsonLi = new List<BsonDocument>();

                if (mapping != null)
                {
                    bson_mapping = mapping.ToBsonDocument();
                    bson_mapping.Remove("_id");
                    bsonLi.Add(bson_mapping);
                }
                #endregion

                #region 写操作日志(新增操作日志)
                operation.SYS_REC_LIST = bsonLi.ToArray();
                operation.EMPI_ID_LIST = new string[] { entity.EMPI_ID };
                operation.PERSON_SBR_ID_LIST = new string[] { entity.PERSON_SBR_ID };
                operation.OPERATION_TYPE = "ADD";
                BsonDocument bson = new BsonDocument();
                bson.Add("ADD", new BsonDocument() { { "TYPE", "ADD" }, { "PERSON_SBR_ID", entity.PERSON_SBR_ID }, { "CATEGORY_CODE", entity.CATEGORY_CODE } });
                operation.DELTA = bson;
                //插入操作日志
                dbhelper_operation.Insert(operation);
                #endregion


                if (operation_type == "merge")
                {
                    entity.STATUS = "I";
                }
                //插入主索引数据
                dbhelper_sbr.Insert(entity);

                #region 处理潜在重复
                if (!string.IsNullOrEmpty(person_sbr_id_2) && weight != 0 && operation_type == "duplicate")
                {

                    EMPI_PERSON_SBR entity_sbr_dupli = dbhelper_sbr.FindOne(Query.EQ("PERSON_SBR_ID", person_sbr_id_2));

                    operation = new EMPI_OPERATION();
                    operation.Create();

                    //插入潜在重复
                    EMPI_POTENTIAL_DUPLICATE entity_dupli = new EMPI_POTENTIAL_DUPLICATE();
                    //entity_dupli._id = new ObjectId();
                    entity_dupli.POTENTIAL_DUPLICATE_ID = Common.CreateNo();
                    entity_dupli.RESOLVED_STATUS = "U";
                    entity_dupli.CREATE_TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    entity_dupli.EMPI_ID_1 = entity.EMPI_ID;
                    entity_dupli.EMPI_ID_2 = entity_sbr_dupli.EMPI_ID;

                    entity_dupli.PERSON_SBR_ID_1 = entity.PERSON_SBR_ID;
                    entity_dupli.PERSON_SBR_ID_2 = person_sbr_id_2;
                    entity_dupli.MATCH_WEIGHT = weight;
                    entity_dupli.OPERATION_ID = operation.OPERATION_ID;
                    new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>().Insert(entity_dupli);

                    //写潜在重复操作日志                                       
                    //operation._id = new ObjectId();
                    operation.EMPI_ID_LIST = new string[] { entity.EMPI_ID, entity_sbr_dupli.EMPI_ID };
                    operation.PERSON_SBR_ID_LIST = new string[] { entity.PERSON_SBR_ID, person_sbr_id_2 };
                    operation.MATCH_WEIGHT = weight.ToString();
                    operation.OPERATION_TYPE = "POTENTIAL_DUPLICATE";
                    operation.RESOLVED_STATUS = "U";
                    if (string.IsNullOrEmpty(isService))
                    {
                        operation.OPERATE_BY = OperatorProvider.Provider.GetCurrent().DisplayName;
                    }
                    else
                    {
                        operation.OPERATE_BY = entity.CREATE_BY;
                    }

                    #region 获取潜在重复主索引的系统映射
                    mapping = new MongoDBHelper<EMPI_MAPPING>().FindOne(Query.EQ("PERSON_SBR_ID", person_sbr_id_2));
                    if (mapping != null)
                    {
                        bson_mapping = mapping.ToBsonDocument();
                        bson_mapping.Remove("_id");
                        bsonLi.Add(bson_mapping);
                    }
                    operation.SYS_REC_LIST = bsonLi.ToArray();
                    #endregion

                    BsonDocument bson_delte = new BsonDocument();
                    BsonDocument bson_dupli_0 = entity.ToBsonDocument();
                    BsonDocument bson_dupli_1 = entity_sbr_dupli.ToBsonDocument();
                    if (bson_dupli_1 != null)
                    {
                        //移除ObjectId，便于Json处理
                        bson_dupli_0.Remove("_id");
                        bson_dupli_1.Remove("_id");

                        bson_delte.Add("DUPLICATE_0", bson_dupli_0);
                        bson_delte.Add("DUPLICATE_1", bson_dupli_1);

                        operation.DELTA = bson_delte;
                    }
                    else
                    {
                        msg.state = "error";
                        msg.message = "新增失败。";
                    }
                    dbhelper_operation.Insert(operation);
                    msg.state = "info";
                    msg.message = "新增成功,根据匹配结果，新增主索引【<b>" + entity.PERSON_SBR_ID + "</b>】与【<b>" + person_sbr_id_2 + "</b>】发生潜在重复,请及时解决";
                }
                #endregion
                #region 自动合并
                else if (!string.IsNullOrEmpty(person_sbr_id_2) && weight != 0 && operation_type == "merge")
                {
                    EMPI_OPERATION _operation = new EMPI_OPERATION();
                    _operation.Create();

                    //写自动合并操作日志                                       
                    _operation.PERSON_SBR_ID_LIST = new string[] { person_sbr_id_2, entity.PERSON_SBR_ID };
                    _operation.MATCH_WEIGHT = weight.ToString();
                    _operation.OPERATION_TYPE = "MERGE";
                    _operation.RESOLVED_STATUS = "R";
                    if (string.IsNullOrEmpty(isService))
                    {
                        _operation.OPERATE_BY = OperatorProvider.Provider.GetCurrent().DisplayName;
                    }
                    else
                    {
                        _operation.OPERATE_BY = entity.CREATE_BY;
                    }
                    #region 系统映射
                    mapping = new MongoDBHelper<EMPI_MAPPING>().FindOne(Query.EQ("PERSON_SBR_ID", person_sbr_id_2));
                    if (mapping != null)
                    {
                        bson_mapping = mapping.ToBsonDocument();
                        bson_mapping.Remove("_id");
                        bsonLi.Add(bson_mapping);
                        //翻转一下排序，新注册的主索引映射排在后面
                        bsonLi.Reverse();
                        _operation.SYS_REC_LIST = bsonLi.ToArray();
                    }
                    #endregion

                    entity_sbr_merge = dbhelper_sbr.FindOne(Query.EQ("PERSON_SBR_ID", person_sbr_id_2));
                    BsonDocument bson_delte = new BsonDocument();
                    BsonDocument bson_dupli_0 = entity_sbr_merge.ToBsonDocument();
                    BsonDocument bson_dupli_1 = entity.ToBsonDocument();
                    _operation.EMPI_ID_LIST = new string[] { entity_sbr_merge.EMPI_ID, entity.EMPI_ID };

                    if (bson_dupli_1 != null)
                    {
                        if (entity_sbr_merge.EMPI_ID != entity.EMPI_ID)
                        {
                            flag = true;
                            dbhelper_sbr.Update(Query.EQ("PERSON_SBR_ID", entity.PERSON_SBR_ID), Update.Set("EMPI_ID", entity_sbr_merge.EMPI_ID));
                        }
                        //移除ObjectId，便于Json处理
                        bson_dupli_0.Remove("_id");
                        bson_dupli_1.Remove("_id");
                        bson_delte.Add("MATCH_0", bson_dupli_0);
                        bson_delte.Add("MATCH_1", bson_dupli_1);
                        _operation.DELTA = bson_delte;
                        new MongoDBHelper<EMPI_OPERATION>().Insert(_operation);
                        msg.state = "info";
                        msg.message = "新增成功，根据匹配结果，新增主索引与【" + person_sbr_id_2 + "】高度匹配,已自动合并";
                    }
                    else
                    {
                        msg.state = "error";
                        msg.message = "新增失败。";
                    }
                }
                #endregion
                else
                {
                    msg.state = "info";
                    msg.message = "新增成功。<br/>主索引号：<b>" + entity.EMPI_ID + "</b>";

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
                                                       bsonLi.Last()["ORG_CODE"], bsonLi.Last()["SYS_CODE"], bsonLi.Last()["SYS_REC_ID"],
                                                      entity_sbr_merge.EMPI_ID, entity.ID_TYPE_CODE, entity.ID_TYPE_NAME, entity.ID_NO, entity.NAME);

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
                return msg;
            }
        }


        /// <summary>
        /// 启用/停用索引
        /// </summary>
        /// <param name="keyValue"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public AjaxResult SetIndexStatus(string keyValue, string status)
        {
            string tmp = "";//记录未解决的潜在重复主索引主键
            AjaxResult msg = new AjaxResult();
            string[] arr = keyValue.Split(',');
            List<IMongoQuery> queryLi = new List<IMongoQuery>();
            List<IMongoQuery> queryLi_tmp = new List<IMongoQuery>();
            IMongoQuery query_tmp = null;
            List<IMongoQuery> queryLi_final = new List<IMongoQuery>();

            var dbhelper_dupli = new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>();
            EMPI_POTENTIAL_DUPLICATE entity_dup = null;
            for (int i = 0; i < arr.Length; i++)
            {
                queryLi.Add(Query.EQ("PERSON_SBR_ID", arr[i]));

                #region 判断是否存在潜在重复且未解决的
                queryLi_tmp.Clear();
                queryLi_final.Clear();

                queryLi_tmp.Add(Query.EQ("PERSON_SBR_ID_1", arr[i]));
                queryLi_tmp.Add(Query.EQ("PERSON_SBR_ID_2", arr[i]));
                query_tmp = Query.Or(queryLi_tmp.ToArray());
                queryLi_final.Add(query_tmp);

                queryLi_tmp.Clear();
                queryLi_tmp.Add(Query.EQ("RESOLVED_STATUS", "U"));
                query_tmp = Query.And(queryLi_tmp);
                queryLi_final.Add(query_tmp);

                entity_dup = dbhelper_dupli.FindOne(Query.And(queryLi_final.ToArray()));

                //如果存在未解决的潜在重复记录，不可启用或停用
                if (entity_dup != null)
                {
                    tmp += arr[i] + ",";
                    queryLi.Remove(Query.EQ("PERSON_SBR_ID", arr[i]));
                }
                #endregion
            }

            IMongoQuery query = null;
            bool isOk = false;
            if (queryLi.Count > 0)
            {
                query = Query.Or(queryLi.ToArray());
                UpdateBuilder update = new UpdateBuilder();
                update.Set("STATUS", status);

                isOk = new MongoDBHelper<EMPI_PERSON_SBR>().Update(query, update, UpdateFlags.Multi);

                BsonDocument bson = new BsonDocument();
                BsonDocument bson_0 = new BsonDocument();
                BsonDocument bson_1 = new BsonDocument();
                var dbhelper_sbr = new MongoDBHelper<EMPI_PERSON_SBR>();
                var dbhelper_mapping = new MongoDBHelper<EMPI_MAPPING>();
                var dbhelper_operation = new MongoDBHelper<EMPI_OPERATION>();
                IList<EMPI_MAPPING> mappingLi = null;
                List<BsonDocument> bsonLi = new List<BsonDocument>();

                for (int i = 0; i < arr.Length; i++)
                {
                    //潜在重复的主索引不添加日志
                    if (tmp.IndexOf(arr[i]) >= 0)
                    {
                        continue;
                    }

                    #region 获取潜在重复索引系统映射
                    mappingLi = dbhelper_mapping.Find(Query.EQ("PERSON_SBR_ID", arr[i]), new string[] { "ORG_CODE", "SYS_CODE", "SYS_REC_ID" });
                    bsonLi = new List<BsonDocument>();
                    foreach (var item in mappingLi)
                    {
                        bsonLi.Add(item.ToBsonDocument());
                    }
                    #endregion

                    EMPI_PERSON_SBR entity = dbhelper_sbr.FindOne(Query.EQ("PERSON_SBR_ID", arr[i]));
                    //修改之前的                   
                    bson_0 = entity.ToBsonDocument();
                    bson_0.Set("STATUS", status == "A" ? "I" : "A");
                    bson_0.Remove("_id");
                    //修改之后的
                    bson_1 = entity.ToBsonDocument();
                    bson_1.Remove("_id");

                    #region 写操作日志
                    EMPI_OPERATION operation = new EMPI_OPERATION();
                    operation.Create();
                    operation.OPERATION_TYPE = status == "A" ? "ACTIVE" : "DISABLE";
                    operation.PERSON_SBR_ID_LIST = new string[] { arr[i] };
                    operation.EMPI_ID_LIST = new string[] { entity.EMPI_ID };
                    operation.SYS_REC_LIST = bsonLi.ToArray();
                    operation.OPERATE_BY = OperatorProvider.Provider.GetCurrent().DisplayName;
                    operation.OPERATE_TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                    operation.RESOLVED_STATUS = "R";
                    operation.DELTA = new BsonDocument().Add("MATCH_0", bson_0).Add("MATCH_1", bson_1);
                    dbhelper_operation.Insert(operation);
                    #endregion
                }

            }

            if (isOk && tmp == "")//所选项没有未解决的潜在重复
            {
                msg.state = "success";
                msg.message = "操作成功。";
            }
            else if (isOk && tmp != "")//所选项部分存在未解决的潜在重复项
            {
                msg.state = "info";
                msg.message = "部分操作成功,其中【" + Common.DelLastComma(tmp) + "】主索引存在未解决的潜在重复，请先处理";
                msg.data = Common.DelLastComma(tmp);
            }
            else if (!isOk && tmp != "")//所选项全部为未解决的潜在重复
            {
                msg.state = "info";
                msg.message = "操作失败,所选主索引存在未解决的潜在重复，请先处理";
            }
            else
            {
                msg.state = "fail";
                msg.message = "操作失败";
            }
            return msg;
        }


        /// <summary>
        /// 获取数据（对比）
        /// </summary>
        /// <param name="keyValue">患者主索引的主键，以多个以逗号分隔</param>
        /// <returns></returns>
        public IList<EMPI_PERSON_SBR> GetCompareIndexJson(string keyValue)
        {
            string[] arr = keyValue.Split(',');
            List<IMongoQuery> queryLi = new List<IMongoQuery>();
            for (int i = 0; i < arr.Length; i++)
            {
                queryLi.Add(Query.EQ("PERSON_SBR_ID", arr[i]));
            }
            return new MongoDBHelper<EMPI_PERSON_SBR>().Find(Query.Or(queryLi.ToArray()));
        }


        /// <summary>
        /// 判断主索引是否能拆分
        /// </summary>
        /// <param name="operation_id"></param>
        /// <returns></returns>
        public AjaxResult IsSplitable(string operation_id)
        {
            AjaxResult msg = new AjaxResult();
            //获取合并之前的两条主索引的ID
            EMPI_OPERATION entity_operation = new MongoDBHelper<EMPI_OPERATION>().FindOne(Query.EQ("OPERATION_ID", operation_id));
            string[] person_sbr_id_Arr = { };
            if (entity_operation != null)
            {
                person_sbr_id_Arr = entity_operation.PERSON_SBR_ID_LIST;
            }
            List<IMongoQuery> queryLi = new List<IMongoQuery>();
            if (person_sbr_id_Arr.Length > 0)
            {
                for (int i = 0; i < person_sbr_id_Arr.Length; i++)
                {
                    queryLi.Add(Query.EQ("PERSON_SBR_ID", person_sbr_id_Arr[i]));
                }

                long count = 0;
                //从主索引库查询两条数据  
                IList<EMPI_PERSON_SBR> person_sbr_li = new MongoDBHelper<EMPI_PERSON_SBR>().Find(Query.Or(queryLi));

                //判断两条数据的状态，只能有一条数据为停用状态                
                foreach (var item in person_sbr_li)
                {
                    if (item.STATUS == "I")//停用状态
                    {
                        count++;
                    }
                }

                if (count == 0)//两条数据都为启用状态，不用拆分
                {
                    msg.message = "该索引已被拆分,不能重复拆分";
                    msg.state = "error";
                }
                else if (count == 1)//有一条为合并状态，没有被拆分过，可以拆分
                {
                    msg.state = "success";
                }
                else
                {
                    msg.message = "数据被停用，无法拆分";
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
        ///创建主索引号
        /// </summary>
        /// <param name="idCard">身份证号码</param>
        /// <returns></returns>
        public string CreateEMPI_ID(string idCard)
        {
            string empi_id = string.Empty;

            //判断身份证是否合法
            if (Validate.CheckIDCard(idCard))
            {

                empi_id += Common.TenBinaryToXXBinary(idCard.Substring(0, 6).ToInt(), 36);

                if (idCard.Length == 15)
                {
                    //如果是15位身份证号码，取出生年时，取两位，且前面要加上19
                    empi_id += Common.TenBinaryToXXBinary(("19" + idCard.Substring(6, 2)).ToInt() - 1800, 36);
                    empi_id += Common.TenBinaryToXXBinary(idCard.Substring(8, 4).ToInt(), 36).PadLeft(2, '0');
                    empi_id += Common.TenBinaryToXXBinary(idCard.Substring(12, 2).ToInt(), 36).PadLeft(2, '0');
                }
                else
                {
                    empi_id += Common.TenBinaryToXXBinary(idCard.Substring(6, 4).ToInt() - 1800, 36);
                    empi_id += Common.TenBinaryToXXBinary(idCard.Substring(10, 4).ToInt(), 36).PadLeft(2, '0');
                    empi_id += Common.TenBinaryToXXBinary(idCard.Substring(14, 3).ToInt(), 36).PadLeft(2, '0');
                }
            }
            else
            {
                //截取0180117125903123
                string date = DateTime.Now.ToString("yyyyMMddHHmmssfff").Substring(1, 16);
                //获取顺序码--直接取两位随机数
                //string num = date + new Random().Next(1, 100);
                string num = date;
                string tmp = Common.TenBinaryToXXBinary(Convert.ToInt32(num.Substring(0, 3)), 36).PadLeft(2, '0');
                tmp += Common.TenBinaryToXXBinary(Convert.ToInt32(num.Substring(3, 4)), 36).PadLeft(2, '0');
                tmp += Common.TenBinaryToXXBinary(Convert.ToInt32(num.Substring(7, 3)), 36).PadLeft(2, '0');
                tmp += Common.TenBinaryToXXBinary(Convert.ToInt32(num.Substring(10, 3)), 36).PadLeft(2, '0');
                tmp += Common.TenBinaryToXXBinary(Convert.ToInt32(num.Substring(13, 3)), 36).PadLeft(2, '0');
                empi_id = tmp;
            }
            return empi_id;
        }


        /// <summary>
        /// 启用主索引
        /// </summary>
        /// <param name="person_sbr_id"></param>
        /// <returns></returns>
        public AjaxResult SetIndexStatus_Single(string person_sbr_id)
        {
            AjaxResult msg = new AjaxResult();

            var dbhelper_sbr = new MongoDBHelper<EMPI_PERSON_SBR>();
            var query = Query.EQ("PERSON_SBR_ID", person_sbr_id);
            EMPI_PERSON_SBR entity_sbr = dbhelper_sbr.FindOne(query);

            //1.判断当前状态是否为停用状态
            if (entity_sbr.STATUS == "I")
            {

                var dbhelper_operation = new MongoDBHelper<EMPI_OPERATION>();
                var dbhelper_match_config = new MongoDBHelper<EMPI_MATCHCONFIG>();
                var dbhelper_mapping = new MongoDBHelper<EMPI_MAPPING>();
                UpdateBuilder update = new UpdateBuilder();

                //获取mapping映射
                EMPI_MAPPING entity_mapping = dbhelper_mapping.FindOne(Query.EQ("PERSON_SBR_ID", person_sbr_id));

                //2.判断停用类型，1> 手工停用；2> 自动合并停用；3> 手动合并停用
                var data_operation = dbhelper_operation.Find(Query.EQ("PERSON_SBR_ID_LIST", person_sbr_id), SortBy.Descending("OPERATE_TIME"));

                if (data_operation.Count > 0)
                {
                    EMPI_MATCHCONFIG matchconfig = dbhelper_match_config.FindOne(Query.EQ("Code", "Standard_1"));

                    EMPI_MATCHCONFIG matchconfig_0 = dbhelper_match_config.FindOne(Query.EQ("Code", "Standard"));
                    int standard_1 = matchconfig.Value;
                    int standard = matchconfig_0.Value;
                    EMPI_OPERATION operation_ = data_operation.First();
                    string operation_type = operation_.OPERATION_TYPE;//操作类型
                    string match_weight = operation_.MATCH_WEIGHT;//匹配权重


                    string last_update_by = OperatorProvider.Provider.GetCurrent().DisplayName;
                    string last_update_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    //自动合并的，不允许启用
                    if (operation_type == "MERGE" && match_weight.ToInt() > standard_1)
                    {
                        msg.message = "该主索引为自动合并，不允许启用！";
                        msg.state = "error";
                    }
                    //潜在重复手工合并，允许启用，但需要生成潜在重复记录
                    else if (operation_type == "MERGE" && match_weight.ToInt() <= standard_1 && match_weight.ToInt() >= standard)
                    {
                        List<IMongoQuery> query_li = new List<IMongoQuery>();
                        query_li.Add(Query.EQ("PERSON_SBR_ID_LIST", person_sbr_id));
                        query_li.Add(Query.EQ("OPERATION_TYPE", "ADD"));
                        var operation_add = dbhelper_operation.FindOne(Query.And(query_li.ToArray()));

                        //启用操作
                        update.Set("EMPI_ID", operation_add.EMPI_ID_LIST[0]);//更换主索引号为初始号
                        update.Set("STATUS", "A");
                        update.Set("LAST_UPDATE_BY", last_update_by);
                        update.Set("LAST_UPDATE_TIME", last_update_time);
                        dbhelper_sbr.Update(Query.EQ("PERSON_SBR_ID", person_sbr_id), update);

                        //启用操作日志
                        EMPI_OPERATION operation = new EMPI_OPERATION();
                        operation.Create();
                        operation.OPERATION_TYPE = "ACTIVE";
                        operation.PERSON_SBR_ID_LIST = new string[] { person_sbr_id };
                        operation.EMPI_ID_LIST = new string[] { entity_sbr.EMPI_ID };

                        BsonDocument mapping_bson = new BsonDocument();
                        mapping_bson = entity_mapping.ToBsonDocument();
                        mapping_bson.Remove("_id");
                        operation.SYS_REC_LIST = new BsonDocument[] { mapping_bson };
                        operation.OPERATE_BY = OperatorProvider.Provider.GetCurrent().DisplayName;
                        operation.OPERATE_TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                        operation.RESOLVED_STATUS = "R";

                        //修改之前的
                        BsonDocument bson_0 = entity_sbr.ToBsonDocument();
                        bson_0.Remove("_id");

                        //修改之后的
                        entity_sbr.STATUS = "I";
                        entity_sbr.LAST_UPDATE_BY = last_update_by;
                        entity_sbr.LAST_UPDATE_TIME = last_update_time;
                        BsonDocument bson_1 = entity_sbr.ToBsonDocument();
                        bson_1.Remove("_id");
                        operation.DELTA = new BsonDocument().Add("MATCH_0", bson_0).Add("MATCH_1", bson_1);
                        dbhelper_operation.Insert(operation);

                        string operation_id = Common.CreateNo();

                        //潜在重复记录
                        EMPI_PERSON_SBR person_sbr_merge = dbhelper_sbr.FindOne(Query.EQ("PERSON_SBR_ID", operation_.PERSON_SBR_ID_LIST[1]));
                        EMPI_POTENTIAL_DUPLICATE entity_dupli = new EMPI_POTENTIAL_DUPLICATE();
                        entity_dupli.POTENTIAL_DUPLICATE_ID = Common.CreateNo();
                        entity_dupli.CREATE_TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        entity_dupli.EMPI_ID_1 = operation_.EMPI_ID_LIST[0];
                        entity_dupli.EMPI_ID_2 = operation_.EMPI_ID_LIST[1];
                        entity_dupli.PERSON_SBR_ID_1 = operation_.PERSON_SBR_ID_LIST[0];
                        entity_dupli.PERSON_SBR_ID_2 = operation_.PERSON_SBR_ID_LIST[1];
                        entity_dupli.MATCH_WEIGHT = operation_.MATCH_WEIGHT.ToInt();
                        entity_dupli.RESOLVED_STATUS = "U";
                        entity_dupli.OPERATION_ID = operation_id;
                        new MongoDBHelper<EMPI_POTENTIAL_DUPLICATE>().Insert(entity_dupli);


                        #region 5.重新写潜在重复日志
                        EMPI_OPERATION _operation = new EMPI_OPERATION();
                        _operation.OPERATION_ID = operation_id;
                        _operation.EMPI_ID_LIST = operation_.EMPI_ID_LIST;
                        _operation.PERSON_SBR_ID_LIST = operation_.PERSON_SBR_ID_LIST;
                        _operation.RESOLVED_STATUS = "U";
                        _operation.OPERATION_TYPE = "POTENTIAL_DUPLICATE";
                        _operation.OPERATE_TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                        _operation.OPERATE_BY = OperatorProvider.Provider.GetCurrent().DisplayName.ToString();
                        _operation.SYS_REC_LIST = operation_.SYS_REC_LIST;
                        _operation.MATCH_WEIGHT = operation_.MATCH_WEIGHT;
                        BsonDocument _bson_delte = new BsonDocument();

                        var entity_0 = dbhelper_sbr.FindOne(Query.EQ("PERSON_SBR_ID", operation_.PERSON_SBR_ID_LIST[0]));
                        BsonDocument doc_match_0 = entity_0.ToBsonDocument();
                        doc_match_0.Remove("_id");
                        var entity_1 = dbhelper_sbr.FindOne(Query.EQ("PERSON_SBR_ID", operation_.PERSON_SBR_ID_LIST[1]));
                        BsonDocument doc_match_1 = entity_1.ToBsonDocument();
                        doc_match_1.Remove("_id");
                        _bson_delte.Add("DUPLICATE_0", doc_match_0);
                        _bson_delte.Add("DUPLICATE_1", doc_match_1);
                        _operation.DELTA = _bson_delte;
                        new MongoDBHelper<EMPI_OPERATION>().Insert(_operation);
                        #endregion

                        msg.state = "success";
                        msg.message = "启用成功！但存在潜在重复记录，请及时处理。";

                        //主索引号码发生了变更，需通知业务系统
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
                                                   entity_mapping.ORG_CODE, entity_mapping.SYS_CODE, entity_mapping.SYS_REC_ID,
                                                   entity_sbr.EMPI_ID, entity_sbr.ID_TYPE_CODE, entity_sbr.ID_TYPE_NAME, entity_sbr.ID_NO, entity_sbr.NAME);

                                url += "?REQUEST=" + para.ToString();
                                string response = WebHelper.HttpWebRequest(url);
                                log.Debug(response);
                            }
                        }

                    }
                    //直接启用
                    else
                    {
                        //启用操作
                        update.Set("STATUS", "A");
                        update.Set("LAST_UPDATE_BY", last_update_by);
                        update.Set("LAST_UPDATE_TIME", last_update_time);
                        dbhelper_sbr.Update(Query.EQ("PERSON_SBR_ID", person_sbr_id), update);

                        //启用操作日志
                        EMPI_OPERATION operation = new EMPI_OPERATION();
                        operation.Create();
                        operation.OPERATION_TYPE = "ACTIVE";
                        operation.PERSON_SBR_ID_LIST = new string[] { person_sbr_id };
                        operation.EMPI_ID_LIST = new string[] { entity_sbr.EMPI_ID };

                        BsonDocument mapping_bson = new BsonDocument();
                        mapping_bson = entity_mapping.ToBsonDocument();
                        mapping_bson.Remove("_id");
                        operation.SYS_REC_LIST = new BsonDocument[] { mapping_bson };
                        operation.OPERATE_BY = OperatorProvider.Provider.GetCurrent().DisplayName;
                        operation.OPERATE_TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                        operation.RESOLVED_STATUS = "R";

                        //修改之前的
                        BsonDocument bson_0 = entity_sbr.ToBsonDocument();
                        bson_0.Remove("_id");

                        //修改之后的
                        entity_sbr.STATUS = "I";
                        entity_sbr.LAST_UPDATE_BY = last_update_by;
                        entity_sbr.LAST_UPDATE_TIME = last_update_time;
                        BsonDocument bson_1 = entity_sbr.ToBsonDocument();
                        bson_1.Remove("_id");
                        operation.DELTA = new BsonDocument().Add("MATCH_0", bson_0).Add("MATCH_1", bson_1);
                        dbhelper_operation.Insert(operation);

                        msg.state = "success";
                        msg.message = "启用成功！";
                    }

                }
            }

            return msg;
        }

        /// <summary>
        /// 15位身份证号码转18位
        /// </summary>
        /// <param name="oldIDCard"></param>
        /// <returns></returns>
        private static string IDCard15To18(string oldIDCard)
        {
            int iS = 0;

            //加权因子常数
            int[] iW = new int[] { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
            //校验码常数
            string LastCode = "10X98765432";
            //新身份证号
            string newIDCard;

            newIDCard = oldIDCard.Substring(0, 6);
            //填在第6位及第7位上填上‘1’，‘9’两个数字
            newIDCard += "19";

            newIDCard += oldIDCard.Substring(6, 9);

            //进行加权求和
            for (int i = 0; i < 17; i++)
            {
                iS += int.Parse(newIDCard.Substring(i, 1)) * iW[i];
            }

            //取模运算，得到模值
            int iY = iS % 11;
            //从LastCode中取得以模为索引号的值，加到身份证的最后一位，即为新身份证号。
            newIDCard += LastCode.Substring(iY, 1);
            return newIDCard;
        }

        #region webService
        /// <summary>
        /// 主索引检索服务
        /// </summary>
        /// <param name="name">姓名</param>
        /// <param name="id_no">身份证号码</param>
        /// <param name="medical_insurance_card_no">医保卡号</param>
        /// <param name="hospital_card_no">就诊卡号</param>
        /// <param name="gender_name">性别名称</param>
        /// <param name="bod_s">生日起始时间</param>
        /// <param name="bod_e">生日截止时间</param>
        /// <param name="count">返回的总数量（输出参数）</param>
        /// <returns></returns>
        public Hashtable GetIndex(string name, string id_no, string medical_insurance_card_no, string hospital_card_no, string gender_name, string bod_s, string bod_e, out int count)
        {
            Hashtable hs = new Hashtable();
            List<IMongoQuery> queryLi = new List<IMongoQuery>();
            if (!string.IsNullOrEmpty(name))
            {
                queryLi.Add(Query.Matches("NAME", new BsonRegularExpression(new Regex(name))));
            }

            if (!string.IsNullOrEmpty(id_no))
            {
                queryLi.Add(Query.Matches("ID_NO", new BsonRegularExpression(new Regex(id_no))));
            }

            if (!string.IsNullOrEmpty(medical_insurance_card_no))
            {
                queryLi.Add(Query.Matches("MEDICAL_INSURANCE_CARD_NO", new BsonRegularExpression(new Regex(medical_insurance_card_no))));
            }
            if (!string.IsNullOrEmpty(hospital_card_no))
            {
                queryLi.Add(Query.Matches("HOSPITAL_CARD_NO", new BsonRegularExpression(new Regex(hospital_card_no))));
            }
            if (!string.IsNullOrEmpty(gender_name))
            {
                queryLi.Add(Query.Matches("GENDER_NAME", new BsonRegularExpression(new Regex(gender_name))));
            }
            if (!string.IsNullOrEmpty(bod_s))
            {
                queryLi.Add(Query.GTE("BOD", bod_s));
            }
            if (!string.IsNullOrEmpty(bod_s))
            {
                queryLi.Add(Query.LTE("BOD", bod_e));
            }
            var dbhelper_sbr = new MongoDBHelper<EMPI_PERSON_SBR>();

            IList<EMPI_PERSON_SBR> person_sbr_li = dbhelper_sbr.Find(Query.And(queryLi.ToArray()));
            hs.Add("EMPI_PERSON_SBR", person_sbr_li.Take(2000));
            count = person_sbr_li.Count();
            return hs;
        }


        /// <summary>
        /// 根据主索引号获取患者信息
        /// </summary>
        /// <param name="person_sbr_id">主索引号</param>
        /// <returns></returns>
        public EMPI_MAPPING GetMappingByPerson_sbr_id(string person_sbr_id)
        {
            EMPI_MAPPING mapping = new MongoDBHelper<EMPI_MAPPING>().FindOne(Query.EQ("PERSON_SBR_ID", person_sbr_id));
            return mapping;
        }


        /// <summary>
        /// 根据主索引号获取主索引信息
        /// </summary>
        /// <param name="empi_id"></param>
        /// <returns></returns>
        public EMPI_PERSON_SBR Get_EMPI_PERSON_SBR_BY_EMPI_ID(string empi_id)
        {
            IMongoQuery query = Query.EQ("EMPI_ID", empi_id);
            var result = new MongoDBHelper<EMPI_PERSON_SBR>().Find(query).ToList();
            var status_a = result.FindAll(a => a.STATUS == "A");
            if (status_a.Any())
            {
                return status_a.FirstOrDefault();
            }
            else
            {
                return result.FirstOrDefault();
            }
        }
        #endregion

    }
}
