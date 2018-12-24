using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMPIMS.Code;
using Newtonsoft.Json;
using System.ComponentModel;

namespace EMPIMS.Module.IndexManage
{
    public class EMPI_PERSON
    {

        [JsonConverter(typeof(ObjectIdConverter))]
        public ObjectId _id { get; set; }

        [Description("机构编码")]
        public string ORG_CODE { get; set; }
        [Description("系统编码")]
        public string SYS_CODE { get; set; }
        [Description("系统唯一主键")]
        public string SYS_REC_ID { get; set; }
        [Description("人员类型")]
        public string CATEGORY_CODE { get; set; }
        [Description("姓城乡居民健康档案编号名")]
        public string HEALTH_RECORD_NO { get; set; }
        public string ID_TYPE_CODE { get; set; }
        [Description("身份证件类别名称")]
        public string ID_TYPE_NAME { get; set; }
        [Description("身份证件号码")]
        public string ID_NO { get; set; }
        [Description("居民健康卡号")]
        public string HEALTH_CARD_NO { get; set; }
        [Description("病案号")]
        public string MEDICAL_RECORD_NO { get; set; }
        public string MEDICAL_INSURANCE_TYPE_CODE { get; set; }
        [Description("医疗保险类别名称")]
        public string MEDICAL_INSURANCE_TYPE_NAME { get; set; }
        [Description("医保卡号")]
        public string MEDICAL_INSURANCE_CARD_NO { get; set; }
        [Description("就诊卡号")]
        public string HOSPITAL_CARD_NO { get; set; }
        [Description("门诊病案号")]
        public string OP_MEDICAL_RECORD_NO { get; set; }
        [Description("姓名")]
        public string NAME { get; set; }
        /// <summary>
        /// 出生日期
        /// </summary>
        [Description("出生日期")]
        public string DOB { get; set; }

        public string GENDER_CODE { get; set; }
        [Description("性别")]
        public string GENDER_NAME { get; set; }

        public string ABO_BLOOD_TYPE_CODE { get; set; }
        [Description("ABO血型名称")]
        public string ABO_BLOOD_TYPE_NAME { get; set; }

        public string RH_BLOOD_TYPE_CODE { get; set; }
        [Description("RH血型名称")]
        public string RH_BLOOD_TYPE_NAME { get; set; }

        public string MARITAL_STATUS_CODE { get; set; }
        [Description("婚姻状况")]
        public string MARITAL_STATUS_NAME { get; set; }

        public string CITIZENSHIP_CODE { get; set; }
        [Description("国籍名称")]
        public string CITIZENSHIP_NAME { get; set; }

        public string ETHNIC_GROUP_CODE { get; set; }
        [Description("民族名称")]
        public string ETHNIC_GROUP_NAME { get; set; }

        public string OCCUPATION_CODE { get; set; }

        [Description("职业类别名称")]
        public string OCCUPATION_NAME { get; set; }

        public string EDUCATION_LEVEL_CODE { get; set; }

        [Description("文化程度名称")]
        public string EDUCATION_LEVEL_NAME { get; set; }
        [Description("出生地-省(自治区、直辖市)")]
        public string POB_PROVINCE { get; set; }
        [Description("出生地-市")]
        public string POB_CITY { get; set; }

        [Description("出生地-县")]
        public string POB_COUNTY { get; set; }

        [Description("籍贯-省")]
        public string NATIVE_PLACE_PROVINCE { get; set; }

        [Description("籍贯-市")]
        public string NATIVE_PLACE_CITY { get; set; }

        [Description("当前居住地-省")]
        public string PRESENT_ADDRESS_PROVINCE { get; set; }

        [Description("当前居住地-市")]
        public string PRESENT_ADDRESS_CITY { get; set; }

        [Description("当前居住地-县")]
        public string PRESENT_ADDRESS_COUNTY { get; set; }

        [Description("当前居住地-镇")]
        public string PRESENT_ADDRES_COUNTRY { get; set; }

        [Description("当前居住地-村")]
        public string PRESENT_ADDRES_VILLAGE { get; set; }

        [Description("当前居住地-门牌号")]
        public string PRESENT_ADDRES_HOUSE_NO { get; set; }

        [Description("当前居住地邮编")]
        public string PRESENT_ADDRES_POSTAL_CODE { get; set; }

        [Description("本人联系电话")]
        public string PHONE_NO { get; set; }

        [Description("工作单位名称")]
        public string EMPLOYER_NAME { get; set; }

        [Description("工作单位-省")]
        public string EMPLOYER_PROVINCE { get; set; }

        [Description("工作单位-市")]
        public string EMPLOYER_CITY { get; set; }

        [Description("工作单位-县")]
        public string EMPLOYER_COUNTY { get; set; }

        [Description("工作单位电话")]
        public string EMPLOYER_PHONE_NO { get; set; }

        [Description("工作单位邮编")]
        public string EMPLOYER_POSTAL_CODE { get; set; }

        [Description("联系人姓名")]
        public string CONTACT_NAME { get; set; }

        public string CONTACT_RELATIONSHIP_CODE { get; set; }

        [Description("联系人关系名称")]
        public string CONTACT_RELATIONSHIP_NAME { get; set; }

        [Description("联系人地址")]
        public string CONTACT_ADDRESS { get; set; }

        [Description("联系人电话")]
        public string CONTACT_PHONE_NO { get; set; }
        public string VIP_INDICATOR { get; set; }
        public string DEATH_INDICATOR { get; set; }
        /// <summary>
        /// 死亡日期
        /// </summary>
        public string DOD { get; set; }
        public string CREATE_BY { get; set; }
        public string CREATE_TIME { get; set; }
        public string LAST_UPDATE_BY { get; set; }
        /// <summary>
        /// 最后修改日期
        /// </summary>
        public string LAST_UPDATE_TIME { get; set; }
        public string STATUS { get; set; }

        public string PRESENT_ADDRESS { get; set; }
        public string IP_MEDICAL_RECORD_NO { get; set; }

        [Description("京医通卡号")]
        public string BEIJING_MEDICAL_CARD_NO { get; set; }
    }
}
