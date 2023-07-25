using Sitecore.Diagnostics;
using System;
using System.IO;
using System.Web;

namespace CustomizeAdminPageAccess
{
    public class NonSecurePage : AdminPage
    {
        private const string DisabledFilePath = "~/sitecore/admin/disabled";
        private const string EnabledFilePath = "~/sitecore/admin/enabled";
        private const string SettingFilePrefix = "~/sitecore/admin/";
        private const string ErrorPageUrl = "/sitecore/admin/NonSecurePageDisabled.aspx";

        protected virtual bool IsEnabled
        {
            get
            {
                string path = this.Server.MapPath("~/sitecore/admin/enabled");
                return !File.Exists(this.Server.MapPath("~/sitecore/admin/disabled")) & File.Exists(path);
            }
        }

        protected virtual void CheckEnabled()
        {
            if (this.IsEnabled)
                return;
            this.HandleDisabled();
        }

        protected virtual void HandleDisabled() => this.Response.Redirect("/sitecore/admin/NonSecurePageDisabled.aspx?returnUrl=" + HttpUtility.UrlEncode(this.Request.Url.PathAndQuery));

        protected override void OnInit(EventArgs args)
        {
            Assert.ArgumentNotNull((object)args, "arguments");
            this.CheckEnabled();
        }
    }
}
