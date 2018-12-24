using EMPIMS.Code;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMPIMS.Module.SystemManage
{
    public class EMPI_MATCHCONFIG
    {
        [JsonConverter(typeof(ObjectIdConverter))]
        public ObjectId _id { get; set; }

        public string MATCHCONFIG_ID { set; get; }
        /// <summary>
        /// 类型（0：字段；1：匹配权重标准）
        /// </summary>
        public int Type { set; get; }

        public string Code { set; get; }

        public string Name { set; get; }

        public int Value { set; get; }
    }
}
