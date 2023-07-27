// Decompiled with JetBrains decompiler
// Type: Sitecore.ExperienceContentManagement.Administration.PublishQueueStats
// Assembly: Sitecore.ExperienceContentManagement.Administration, Version=9.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FA007834-6198-418B-8E97-23CDD4805B4C
// Assembly location: C:\inetpub\wwwroot\sc103xyzsc.dev.local\bin\Sitecore.ExperienceContentManagement.Administration.dll

using Sitecore;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.Publish;
using Sitecore.sitecore.admin;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace CustomizeAdminPageAccess
{
    [AllowDependencyInjection]
    public class PublishQueueStats : AdminPage
    {
        private const string PublishingTargetPath = "/sitecore/system/publishing targets";
        protected HtmlHead Head1;
        protected HtmlForm Form1;
        protected Literal lt;
        protected Repeater PublishQueueStatsRepeater;
        protected Label CleanupResult;
        protected DropDownList Databases;
        protected TextBox IntervalToKeep;

        protected IList<Database> GetPublishingTargets(Database database)
        {
            Item obj = database.GetItem("/sitecore/system/publishing targets");
            return obj == null ? (IList<Database>)new List<Database>() : (IList<Database>)obj.Children.Select<Item, Database>((Func<Item, Database>)(x => Factory.GetDatabase(x["Target database"]))).ToList<Database>();
        }

        protected void OnClick(object sender, EventArgs e)
        {
            Database database = Factory.GetDatabase(this.Databases.SelectedValue);
            if (database == null)
            {
                this.CleanupResult.Text = "The selected database does not exist.";
            }
            else
            {
                TimeSpan timeSpan = DateUtil.ParseTimeSpan(this.IntervalToKeep.Text, TimeSpan.MaxValue);
                if (timeSpan == TimeSpan.MaxValue)
                {
                    this.CleanupResult.Text = "The specified interval cannot be parsed.";
                }
                else
                {
                    PublishManager.CleanupPublishQueue(DateTime.Now - timeSpan, database);
                    this.ReloadStatistics();
                }
            }
        }

        protected override void OnInit(EventArgs args)
        {
            Assert.ArgumentNotNull((object)args, "arguments");
            this.CheckSecurity("sitecore\\PublishQueueStatsPageAccess");
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!this.IsPostBack)
            {
                this.Databases.DataSource = (object)Factory.GetDatabases();
                this.Databases.DataBind();
                this.ShowRefreshStatus();
            }
            this.ReloadStatistics();
        }

        protected virtual void ReloadStatistics()
        {
            List<Database> databases = Factory.GetDatabases();
            List<PublishQueueStats.PublishQueueStatistics> publishQueueStatisticsList = new List<PublishQueueStats.PublishQueueStatistics>();
            foreach (Database database1 in databases)
            {
                Database database = database1;
                IList<Database> publishingTargets = this.GetPublishingTargets(database);
                DatabaseProperties databaseProperties = new DatabaseProperties(database);
                LanguageCollection languages = LanguageManager.GetLanguages(database);
                List<PublishQueueStats.PublishQueueStatistics.PublishingTargetStatistics> list = publishingTargets.SelectMany(db => languages.Select(language => new
                {
                    Database = db,
                    Language = language,
                    LastPublishingDate = databaseProperties.GetLastPublishDate(db, language)
                })).Select(x => new PublishQueueStats.PublishQueueStatistics.PublishingTargetStatistics()
                {
                    DatabaseName = x.Database.Name,
                    LanguageName = x.Language.Name,
                    LastPublishingDate = x.LastPublishingDate.Kind == DateTimeKind.Utc ? x.LastPublishingDate.ToLocalTime() : x.LastPublishingDate,
                    RecordsToBeProcessed = (long)PublishManager.GetPublishQueue(x.LastPublishingDate, DateTime.MaxValue, database).Count
                }).ToList<PublishQueueStats.PublishQueueStatistics.PublishingTargetStatistics>();
                publishQueueStatisticsList.Add(new PublishQueueStats.PublishQueueStatistics()
                {
                    DatabaseName = database.Name,
                    NumberOfRecords = (long)new PublishQueue(database).GetCount(),
                    PublishingTargetStats = (IList<PublishQueueStats.PublishQueueStatistics.PublishingTargetStatistics>)list
                });
            }
            this.PublishQueueStatsRepeater.DataSource = (object)publishQueueStatisticsList;
            this.PublishQueueStatsRepeater.DataBind();
        }

        protected virtual void ShowRefreshStatus()
        {
            StringBuilder stringBuilder = new StringBuilder();
            int result;
            int.TryParse(this.Request.QueryString["refresh"], out result);
            stringBuilder.Append("Last updated: " + DateTime.Now.ToString((IFormatProvider)CultureInfo.InvariantCulture) + ". ");
            int[] numArray = new int[7] { 1, 2, 5, 10, 20, 30, 60 };
            stringBuilder.Append("Refresh each <a href='PublishQueueStats.aspx' class='refresh-link " + (result == 0 ? "refresh-selected" : string.Empty) + "'>No Refresh</a>");
            foreach (int num in numArray)
            {
                string str1 = result == num ? "refresh-selected" : string.Empty;
                string str2 = string.Format(", <a href='PublishQueueStats.aspx?refresh={0}' class='refresh-link {1}'>{0} sec</a>", (object)num, (object)str1);
                stringBuilder.Append(str2);
            }
            stringBuilder.Append("<br /><br />");
            this.lt.Text = stringBuilder.ToString();
        }

        public class PublishQueueStatistics
        {
            public string DatabaseName { get; set; }

            public long NumberOfRecords { get; set; }

            public IList<PublishQueueStats.PublishQueueStatistics.PublishingTargetStatistics> PublishingTargetStats { get; set; }

            public class PublishingTargetStatistics
            {
                public string DatabaseName { get; set; }

                public string LanguageName { get; set; }

                public DateTime LastPublishingDate { get; set; }

                public long RecordsToBeProcessed { get; set; }
            }
        }
    }
}
