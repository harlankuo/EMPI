using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMPIMS.Module
{

    public class BaseModel
    {
        public virtual ObjectId _id { get; set; }
    }

}
