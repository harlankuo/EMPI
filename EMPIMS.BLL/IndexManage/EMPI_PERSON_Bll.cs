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
using Newtonsoft.Json;

namespace EMPIMS.BLL.IndexManage
{
    public class EMPI_PERSON_Bll
    {
        /// <summary>
        /// 查询所有
        /// </summary>
        /// <returns></returns>
        public IEnumerable<EMPI_PERSON> Find(string sys_code, string sys_rec_code, string org_code)
        {
            List<IMongoQuery> queryLi = new List<IMongoQuery>();
            queryLi.Add(Query.EQ("SYS_CODE", sys_code));
            queryLi.Add(Query.EQ("SYS_REC_ID", sys_rec_code));
            queryLi.Add(Query.EQ("ORG_CODE", org_code));
            return new MongoDBHelper<EMPI_PERSON>().Find(Query.And(queryLi.ToArray()));
        }


        /// <summary>
        /// 新增EMPI_PERSON
        /// </summary>
        /// <param name="entity_person"></param>
        /// <returns></returns>
        public AjaxResult Create(EMPI_PERSON entity_person, out string empi_id)
        {
            AjaxResult msg = new AjaxResult();
            MongoDBHelper<EMPI_PERSON> dbhelper_person = new MongoDBHelper<EMPI_PERSON>();
            MongoDBHelper<EMPI_MAPPING> dbhelper_mapping = new MongoDBHelper<EMPI_MAPPING>();

            EMPI_PERSON_SBR_Bll dbhelper_sbr = new EMPI_PERSON_SBR_Bll();
            EMPI_PERSON_SBR entity_sbr = new EMPI_PERSON_SBR();
            //entity_sbr.EMPI_ID = Common.CreateNo();
            BsonDocument doc_person = entity_person.ToBsonDocument();
            BsonDocument doc_sbr = new BsonDocument();

            List<string> fieldLi = new List<string>();
            doc_person.Remove("_id");
            doc_person.Remove("ORG_CODE");
            doc_person.Remove("SYS_REC_ID");
            doc_sbr = doc_person;

            fieldLi = doc_sbr.Names.ToList();

            //插入主索引库
            entity_sbr = JsonConvert.DeserializeObject<EMPI_PERSON_SBR>(doc_sbr.ToJson());

            entity_sbr.EMPI_ID = dbhelper_sbr.CreateEMPI_ID(entity_sbr.ID_NO);
            entity_sbr.Create();

            //插入mapping数据
            EMPI_MAPPING entity_mapping = new EMPI_MAPPING();
            //entity_mapping.EMPI_ID = entity_sbr.EMPI_ID;
            entity_mapping.PERSON_SBR_ID = entity_sbr.PERSON_SBR_ID;
            entity_mapping.SYS_CODE = entity_person.SYS_CODE;
            entity_mapping.ORG_CODE = entity_person.ORG_CODE;
            entity_mapping.SYS_REC_ID = entity_person.SYS_REC_ID;
            dbhelper_mapping.Insert(entity_mapping);

            msg = dbhelper_sbr.SubmitForm(entity_sbr, "", fieldLi.ToArray(), "service");

            if (msg.state == "info" || msg.state == "success")
            {

                //插入患者信息
                dbhelper_person.Insert(entity_person);

                msg.state = "success";
                msg.message = "注册成功";
                empi_id = entity_sbr.EMPI_ID;
            }
            else
            {
                //注册失败了，就要将之前插入的映射给删除掉
                List<IMongoQuery> queryLi = new List<IMongoQuery>();
                queryLi.Add(Query.EQ("PERSON_SBR_ID", entity_person.SYS_REC_ID));
                queryLi.Add(Query.EQ("SYS_CODE", entity_person.SYS_CODE));
                queryLi.Add(Query.EQ("ORG_CODE", entity_person.ORG_CODE));
                dbhelper_mapping.Remove(Query.And(queryLi));

                msg.state = "error";
                msg.message = "注册失败，" + msg.message;
                empi_id = "";
            }
            return msg;
        }



        /// <summary>
        /// 修改EMPI_PERSON
        /// </summary>
        /// <param name="entity_person"></param>
        /// <returns></returns>
        public AjaxResult Update(EMPI_PERSON entity_person, out string empi_id)
        {
            AjaxResult msg = new AjaxResult();
            EMPI_PERSON_SBR entity_sbr = new EMPI_PERSON_SBR();
            BsonDocument doc_person = entity_person.ToBsonDocument();
            BsonDocument doc_sbr = new BsonDocument();
            MongoDBHelper<EMPI_MAPPING> dbhelper_mapping = new MongoDBHelper<EMPI_MAPPING>();
            MongoDBHelper<EMPI_PERSON> dbhelper_person = new MongoDBHelper<EMPI_PERSON>();

            List<string> fieldLi = new List<string>();

            doc_person.Remove("_id");
            doc_person.Remove("ORG_CODE");
            doc_person.Remove("SYS_REC_ID");
            doc_sbr = doc_person;
            fieldLi = doc_sbr.Names.ToList();

            List<IMongoQuery> queryLi = new List<IMongoQuery>();
            queryLi.Add(Query.EQ("SYS_REC_ID", entity_person.SYS_REC_ID));
            queryLi.Add(Query.EQ("ORG_CODE", entity_person.ORG_CODE));
            queryLi.Add(Query.EQ("SYS_CODE", entity_person.SYS_CODE));
            EMPI_MAPPING entity_mapping = new EMPI_MAPPING();

            entity_mapping = dbhelper_mapping.FindOne(Query.And(queryLi.ToArray()));

            //先要判断患者信息是否有改变，没有改变时不需要更新操作            
            EMPI_PERSON entity_2 = dbhelper_person.FindOne(Query.And(queryLi.ToArray()));

            doc_person.Remove("_id");

            BsonDocument doc__2 = entity_2.ToBsonDocument();
            doc__2.Remove("_id");
            doc__2.Remove("CREATE_BY");
            doc__2.Remove("CREATE_TIME");
            doc__2.Remove("LAST_UPDATE_BY");
            doc__2.Remove("LAST_UPDATE_TIME");
            doc__2.Remove("IP_MEDICAL_RECORD_NO");

            int flag = 0;
            string bsonVal_pers = "", bsonVal_2 = "";
            foreach (var name in doc__2.Names)
            {
                if (doc_person.Names.Contains(name) && doc_person[name] != doc__2[name])
                {
                    bsonVal_pers = doc_person[name].ToString();
                    bsonVal_2 = doc__2[name].ToString();
                    flag++;
                    if (!string.IsNullOrEmpty(bsonVal_pers) && bsonVal_pers != "BsonNull" && (string.IsNullOrEmpty(bsonVal_2) || bsonVal_2 == "BsonNull"))
                    {
                        doc_sbr.Set(name, bsonVal_pers);
                    }
                }
            }

            if (flag == 0)
            {
                string _empi_id = new MongoDBHelper<EMPI_PERSON_SBR>().FindOne(Query.EQ("PERSON_SBR_ID", entity_mapping.PERSON_SBR_ID)).EMPI_ID;
                msg.state = "success";
                msg.message = "操作成功，提交的数据和数据库完全一致，未做任何修改。";
                empi_id = _empi_id;
                return msg;
            }

            entity_sbr = JsonConvert.DeserializeObject<EMPI_PERSON_SBR>(doc_sbr.ToJson());
            entity_sbr.PERSON_SBR_ID = entity_mapping.PERSON_SBR_ID;
            //更改主索引
            msg = new EMPI_PERSON_SBR_Bll().SubmitForm(entity_sbr, entity_mapping.PERSON_SBR_ID, fieldLi.ToArray(), "service");

            if (msg.state.ToString() == "info" || msg.state.ToString() == "success")
            {
                //更新患者
                dbhelper_person.Update(Query.And(queryLi.ToArray()), entity_person, fieldLi.ToArray());

                empi_id = entity_sbr.EMPI_ID;
                msg.state = "success";
                msg.message = "修改成功";
            }
            else
            {
                empi_id = entity_sbr.EMPI_ID;
                msg.state = "error";
                msg.message = "修改失败，" + msg.message;
            }
            return msg;
        }


    }
}
