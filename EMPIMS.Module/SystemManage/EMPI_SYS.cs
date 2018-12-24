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
    public class EMPI_SYS
    {

        [JsonConverter(typeof(ObjectIdConverter))]
        public ObjectId _id { get; set; }

        public string SYS_CODE { get; set; }

        public string SYS_NAME { get; set; }

        public string CREATE_BY { get; set; }

        public string CREATE_TIME { get; set; }

        public string LAST_UPDATE_BY { get; set; }

        public string LAST_UPDATE_TIME { get; set; }
    }
}
