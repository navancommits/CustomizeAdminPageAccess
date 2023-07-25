// Decompiled with JetBrains decompiler
// Type: Sitecore.sitecore.admin.stats
// Assembly: Sitecore.Client, Version=18.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C7059C67-2306-48C0-9640-BA8B104F7B8E
// Assembly location: C:\inetpub\wwwroot\sc103lkgsc.dev.local\bin\Sitecore.Client.dll
// XML documentation location: C:\inetpub\wwwroot\sc103lkgsc.dev.local\bin\Sitecore.Client.xml

using Sitecore;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Web;
using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace CustomizeAdminPageAccess
{
    [AllowDependencyInjection]
    /// <summary>The statistics page.</summary>
    public class stats : AdminPage
    {
        /// <summary>form1 control.</summary>
        /// <remarks>
        /// Auto-generated field.
        /// To modify move field declaration from designer file to code-behind file.
        /// </remarks>
        protected HtmlForm form1;
        /// <summary>siteSelector control.</summary>
        /// <remarks>
        /// Auto-generated field.
        /// To modify move field declaration from designer file to code-behind file.
        /// </remarks>
        protected HtmlGenericControl siteSelector;
        /// <summary>renderings control.</summary>
        /// <remarks>
        /// Auto-generated field.
        /// To modify move field declaration from designer file to code-behind file.
        /// </remarks>
        protected PlaceHolder renderings;
        /// <summary>c_reset control.</summary>
        /// <remarks>
        /// Auto-generated field.
        /// To modify move field declaration from designer file to code-behind file.
        /// </remarks>
        protected Button c_reset;

        /// <summary>Handles the Click event of the 'Reset' button.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="arguments">
        /// The <see cref="T:System.EventArgs" /> instance containing the event data.
        /// </param>
        protected void c_reset_Click(object sender, EventArgs arguments)
        {
            Assert.ArgumentNotNull(sender, nameof(sender));
            Assert.ArgumentNotNull((object)arguments, nameof(arguments));
            Statistics.Clear();
            this.renderings.Controls.Clear();
        }

        /// <summary>Handles the Load event of the Page control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="arguments">
        /// The <see cref="T:System.EventArgs" /> instance containing the event data.
        /// </param>
        protected void Page_Load(object sender, EventArgs arguments)
        {
            Assert.ArgumentNotNull(sender, nameof(sender));
            Assert.ArgumentNotNull((object)arguments, nameof(arguments));
            this.CheckSecurity();
            this.ShowSiteSelector();
            this.ShowRenderingStats(this.Request.QueryString["site"]);
        }

        /// <summary>Gets the site names.</summary>
        /// <returns>The site names.</returns>
        private static string[] GetSiteNames()
        {
            List<string> stringList = new List<string>();
            foreach (Statistics.RenderingData renderingStatistic in Statistics.RenderingStatistics)
            {
                if (!stringList.Contains(renderingStatistic.SiteName))
                    stringList.Add(renderingStatistic.SiteName);
            }
            return stringList.ToArray();
        }

        /// <summary>Shows the rendering stats.</summary>
        /// <param name="siteName">Name of the site.</param>
        private void ShowRenderingStats(string siteName)
        {
            HtmlTable htmlTable = new HtmlTable()
            {
                Border = 1,
                CellPadding = 2
            };
            HtmlUtil.AddRow(htmlTable, new string[11]
            {
        "Rendering",
        "Site",
        "Count",
        "From cache",
        "Avg. time (ms)",
        "Avg. items",
        "Max. time",
        "Max. items",
        "Total time",
        "Total items",
        "Last run"
            });
            SortedList<string, Statistics.RenderingData> sortedList = new SortedList<string, Statistics.RenderingData>();
            foreach (Statistics.RenderingData renderingStatistic in Statistics.RenderingStatistics)
            {
                if (siteName == null || renderingStatistic.SiteName.Equals(siteName, StringComparison.OrdinalIgnoreCase))
                    sortedList.Add(renderingStatistic.SiteName + (object)(int)byte.MaxValue + renderingStatistic.TraceName, renderingStatistic);
            }
            foreach (Statistics.RenderingData renderingData in (IEnumerable<Statistics.RenderingData>)sortedList.Values)
            {
                HtmlTableRow htmlTableRow = HtmlUtil.AddRow(htmlTable, (object)renderingData.TraceName, (object)renderingData.SiteName, (object)renderingData.RenderCount, (object)renderingData.UsedCache, (object)renderingData.AverageTime.TotalMilliseconds, (object)renderingData.AverageItemsAccessed, (object)renderingData.MaxTime.TotalMilliseconds, (object)renderingData.MaxItemsAccessed, (object)renderingData.TotalTime, (object)renderingData.TotalItemsAccessed, (object)DateUtil.ToServerTime(renderingData.LastRendered));
                for (int index = 2; index < htmlTableRow.Cells.Count; ++index)
                    htmlTableRow.Cells[index].Align = "right";
            }
            this.renderings.Controls.Add((Control)htmlTable);
        }

        /// <summary>Shows the site selector.</summary>
        private void ShowSiteSelector()
        {
            string[] siteNames = stats.GetSiteNames();
            Array.Sort<string>(siteNames);
            HtmlTable table = HtmlUtil.CreateTable(1, siteNames.Length + 1);
            table.Border = 0;
            table.CellPadding = 5;
            table.Rows[0].Cells[0].InnerHtml = "<a href=\"?\">All sites</a>";
            int index = 1;
            foreach (string str in siteNames)
            {
                table.Rows[0].Cells[index].InnerHtml = string.Format("<a href=\"?site={0}\">{0}</a>", (object)str);
                ++index;
            }
            this.siteSelector.Controls.Add((Control)table);
        }
    }
}
