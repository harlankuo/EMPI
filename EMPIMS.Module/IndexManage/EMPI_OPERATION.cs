using EMPIMS.Code;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMPIMS.Module.IndexManage
{
    /// <summary>
    /// 操作记录
    /// </summary>
    public class EMPI_OPERATION
    {

        //[BsonId]
        //[BsonRepresentation(BsonType.ObjectId)]
        [JsonConverter(typeof(ObjectIdConverter))]
        public ObjectId _id { get; set; }
        public string OPERATION_ID { get; set; }
        public string OPERATION_TYPE { get; set; }
        public string[] EMPI_ID_LIST { get; set; }
        public string[] PERSON_SBR_ID_LIST { get; set; }
        public BsonDocument[] SYS_REC_LIST { get; set; }
        public string OPERATE_BY { get; set; }
        public string OPERATE_TIME { get; set; }
        public string MATCH_WEIGHT { get; set; }
        /// <summary>
        /// 解决状态【R:已处理；U：未处理】
        /// </summary>
        public string RESOLVED_STATUS { get; set; }
        public string RESOLVED_BY { get; set; }
        public string RESOLVED_TIME { get; set; }
        public BsonDocument DELTA { get; set; }


        /// <summary>
        /// 设置默认值
        /// OPERATION_ID、OPERATE_TIME
        /// </summary>
        public void Create()
        {
            this.OPERATION_ID = EMPIMS.Code.Common.CreateNo();
            this.OPERATE_TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
        }
    }
}
