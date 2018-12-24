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
    /// <summary>
    /// 数据字典
    /// </summary>
    public class DC_DICTION
    {
        [JsonConverter(typeof(ObjectIdConverter))]
        public ObjectId _id { get; set; }
        public string type { get; set; }
        public BsonDocument[] data { set; get; }
    }
}
