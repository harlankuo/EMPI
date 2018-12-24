using System.Web.Mvc;

namespace EMPIMS.Web.Areas.IndexManage
{
    public class IndexManageAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "IndexManage";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                this.AreaName + "_default",
                this.AreaName + "/{controller}/{action}/{id}",
                //new { action = "Index", id = UrlParameter.Optional }
                new { area = this.AreaName, controller = "Home", action = "Index", id = UrlParameter.Optional },
                new string[] { "EMPIMS.Web.Areas." + this.AreaName + ".Controllers" }
            );
        }
    }
}
