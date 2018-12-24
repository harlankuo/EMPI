using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMPIMS.Module.SystemManage
{
    public class EMPI_USER
    {
        public ObjectId _id { get; set; }
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string UserPassword { get; set; }
        public string UserType { get; set; }
        public string EmpNo { get; set; }
        public string PID { get; set; }
        public int? Status { get; set; }
        public string CreateTime { get; set; }
        public string CreateBy { get; set; }
        public string UpdateTime { get; set; }
        public string UpdateBy { get; set; }

        public void Create()
        {
            this._id = new ObjectId();
            this.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
