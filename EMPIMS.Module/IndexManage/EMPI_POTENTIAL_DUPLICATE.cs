using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;
using EMPIMS.Code;

namespace EMPIMS.Module.IndexManage
{
    /// <summary>
    /// 潜在重复
    /// </summary>
    public class EMPI_POTENTIAL_DUPLICATE
    {
        [JsonConverter(typeof(ObjectIdConverter))]
        public ObjectId _id { get; set; }
        public string POTENTIAL_DUPLICATE_ID { get; set; }

        public string EMPI_ID_1 { get; set; }
        public string EMPI_ID_2 { get; set; }

        public string PERSON_SBR_ID_1 { get; set; }
        public string PERSON_SBR_ID_2 { get; set; }


        public int MATCH_WEIGHT { get; set; }
        /// <summary>
        /// 解决状态((U:未处理；R:已处理)
        /// </summary>
        public string RESOLVED_STATUS { get; set; }
        public string RESOLVED_BY { get; set; }
        /// <summary>
        /// 解决时间
        /// </summary>
        public string RESOLVED_TIME { get; set; }
        public string OPERATION_ID { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public string CREATE_TIME { get; set; }
    }
}
