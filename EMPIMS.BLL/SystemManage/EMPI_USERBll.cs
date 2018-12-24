using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMPIMS.Code;

namespace EMPIMS.BLL.SystemManage
{
    public class EMPI_USERBll
    {

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="acount">账号</param>
        /// <param name="pwd">密码</param>
        /// <returns>是否成功</returns>
        public AjaxResult LoginCheck(string acount, string pwd)
        {
            AjaxResult msg = new AjaxResult() { state = ResultType.success };
            IMongoQuery query = Query.EQ("acount", acount);

            if (true)
            {

            }
            return msg;
        }
    }
}
