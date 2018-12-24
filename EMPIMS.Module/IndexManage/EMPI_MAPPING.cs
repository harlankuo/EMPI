using EMPIMS.Code;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMPIMS.Module.IndexManage
{
    /// <summary>
    /// 患者系统映射
    /// </summary>
    public class EMPI_MAPPING
    {
        [JsonConverter(typeof(ObjectIdConverter))]
        public ObjectId _id { get; set; }
        public string ORG_CODE { get; set; }
        public string SYS_CODE { get; set; }
        public string SYS_REC_ID { get; set; }
        //public string EMPI_ID { get; set; }
        public string PERSON_SBR_ID { get; set; }
    }
}
