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

namespace EMPIMS.BLL.IndexManage
{
    /// <summary>
    /// 患者--系统映射
    /// </summary>
    public class EMPI_MAPPING_Bll
    {

        /// <summary>
        /// 根据患者主索引ID获取映射集合
        /// </summary>
        /// <param name="person_sbr_id"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public IList<EMPI_MAPPING> GetListByPERSON_SBR_ID(string person_sbr_id, params string[] fields)
        {
            IMongoQuery query = Query.EQ("PERSON_SBR_ID", person_sbr_id);
            return new MongoDBHelper<EMPI_MAPPING>().Find(query, fields);
        }
    }
}
