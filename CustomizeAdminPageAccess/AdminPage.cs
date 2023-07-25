using Sitecore.Sites;
using System.Web.UI;
using System.Web;

namespace CustomizeAdminPageAccess
{
    public class AdminPage : Page
    {
        protected void CheckSecurity()
        {
            if (Sitecore.Context.User.IsAdministrator)
                return;
            SiteContext site = Sitecore.Context.Site;
            if (site == null)
                return;
            this.Response.Redirect(site.LoginPage + "?returnUrl=" + HttpUtility.UrlEncode(this.Request.Url.PathAndQuery));
        }
    }
}
