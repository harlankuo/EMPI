using MongoDB.Bson;
using System;

namespace EMPIMS.Code
{
    public class OperatorModel
    {
        #region 用户ID
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserID { get; set; }
        #endregion

        #region 用户名称
        /// <summary>
        /// 用户名称
        /// </summary>
        public string UserName { get; set; }
        #endregion

        #region 显示名称
        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }
        #endregion

        #region 用户密码
        /// <summary>
        /// 用户密码
        /// </summary>
        public string UserPassword { get; set; }
        #endregion

        #region 用户类型
        /// <summary>
        /// 用户类型
        /// </summary>
        public string UserType { get; set; }
        #endregion

        #region 用户类型名称
        /// <summary>
        /// 用户类型名称
        /// </summary>
        public string UserTypeName { get; set; }
        #endregion

        #region 工号
        /// <summary>
        /// 工号
        /// </summary>
        public string EmpNo { get; set; }
        #endregion

        #region 患者ID
        /// <summary>
        /// 患者ID
        /// </summary>
        public string PID { get; set; }
        #endregion

        #region 状态
        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }
        #endregion

        #region 创建时间
        /// <summary>
        /// 创建时间
        /// </summary>
        public string CreateTime { get; set; }
        #endregion

        #region 创建人
        /// <summary>
        /// 创建人
        /// </summary>
        public string CreateBy { get; set; }
        #endregion

        #region 修改时间
        /// <summary>
        /// 修改时间
        /// </summary>
        public string UpdateTime { get; set; }
        #endregion

        #region 修改人
        /// <summary>
        /// 修改人
        /// </summary>
        public string UpdateBy { get; set; }
        #endregion
    }
}
