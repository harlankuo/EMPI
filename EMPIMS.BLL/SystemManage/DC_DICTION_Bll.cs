using EMPIMS.Module.SystemManage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMPIMS.BLL.DBHelper;
using MongoDB.Bson;

namespace EMPIMS.BLL.SystemManage
{
    public class DC_DICTION_Bll
    {
        /// <summary>
        /// 获取所有字典数据
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetClientData()
        {
            string[] fields = { "type", "data" };
            IList<DC_DICTION> dicLi = await Task.Run(() => new MongoDBHelper<DC_DICTION>().FindAll());
            return dicLi.ToJson();
        }
    }
}
