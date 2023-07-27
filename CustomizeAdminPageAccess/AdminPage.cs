using Sitecore.Sites;
using System.Web.UI;
using System.Web;
using Sitecore.Security.Accounts;

namespace CustomizeAdminPageAccess
{
    public class AdminPage : Page
    {
        protected void CheckSecurity(string rolename)
        {
            if (Sitecore.Context.User.IsAdministrator || (!string.IsNullOrWhiteSpace(rolename) && Sitecore.Context.User.IsInRole(Role.FromName(rolename))))//should be handled for case sensitivity
                return;
            SiteContext site = Sitecore.Context.Site;
            if (site == null)
                return;
            this.Response.Redirect(site.LoginPage + "?returnUrl=" + HttpUtility.UrlEncode(this.Request.Url.PathAndQuery));
        }
    }
}
