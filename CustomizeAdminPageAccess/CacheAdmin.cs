// MVID: C7059C67-2306-48C0-9640-BA8B104F7B8E
// Assembly location: C:\inetpub\wwwroot\sc103lkgsc.dev.local\bin\Sitecore.Client.dll
// XML documentation location: C:\inetpub\wwwroot\sc103lkgsc.dev.local\bin\Sitecore.Client.xml

using Sitecore;
using Sitecore.Caching;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Reflection;
using Sitecore.Web;
using System;
using System.Collections;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace CustomizeAdminPageAccess
{
    [AllowDependencyInjection]
    /// <summary>The cache page.</summary>
    public class CacheAdmin : AdminPage
    {
        /// <summary>The c_caches.</summary>
        protected PlaceHolder c_caches;
        /// <summary>The c_cache title.</summary>
        protected Label c_cacheTitle;
        /// <summary>The c_clear all.</summary>
        protected Button c_clearAll;
        /// <summary>The c_refresh.</summary>
        protected Button c_refresh;
        /// <summary>The c_totals.</summary>
        protected Label c_totals;
        /// <summary>The caches.</summary>
        protected Label Caches;
        /// <summary>The label 1.</summary>
        protected Label Label1;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event to initialize the page.
        /// </summary>
        /// <param name="arguments">
        /// An <see cref="T:System.EventArgs" /> that contains the event data.
        /// </param>
        protected override void OnInit(EventArgs arguments)
        {
            Assert.ArgumentNotNull((object)arguments, nameof(arguments));
            this.CheckSecurity();
            this.InitializeComponent();
            base.OnInit(arguments);
        }

        /// <summary>Handles the Click event of the 'Clear All' button.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="arguments">
        /// The <see cref="T:System.EventArgs" /> instance containing the event data.
        /// </param>
        private void ClearAllButtonClick(object sender, EventArgs arguments)
        {
            Assert.ArgumentNotNull(sender, nameof(sender));
            Assert.ArgumentNotNull((object)arguments, nameof(arguments));
            foreach (ICacheInfo allCach in CacheManager.GetAllCaches())
                allCach.Clear();
            TypeUtil.ClearSizeCache();
            this.ResetCacheList();
        }

        /// <summary>
        /// Initializes the component.
        /// Required method for Designer support -
        /// do not modify the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.c_refresh.Click += new EventHandler(this.RefreshButtonClick);
            this.c_clearAll.Click += new EventHandler(this.ClearAllButtonClick);
        }

        /// <summary>Handles the Click event of the 'Refresh' button.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="arguments">
        /// The <see cref="T:System.EventArgs" /> instance containing the event data.
        /// </param>
        private void RefreshButtonClick(object sender, EventArgs arguments)
        {
            Assert.ArgumentNotNull(sender, nameof(sender));
            Assert.ArgumentNotNull((object)arguments, nameof(arguments));
            this.UpdateTotals();
            this.ResetCacheList();
        }

        /// <summary>Resets the cache list.</summary>
        private void ResetCacheList()
        {
            ICacheInfo[] allCaches = CacheManager.GetAllCaches();
            Array.Sort((Array)allCaches, (IComparer)new CacheComparer());
            HtmlTable table = HtmlUtil.CreateTable(0, 0);
            table.Border = 1;
            table.CellPadding = 4;
            HtmlUtil.AddRow(table, new string[6]
            {
        string.Empty,
        "Name",
        "Count",
        "Size",
        "Delta",
        "MaxSize"
            });
            foreach (ICacheInfo cacheInfo in allCaches)
            {
                string name = "size_" + (object)cacheInfo.Id.ToShortID();
                long num = MainUtil.GetLong((object)this.Request.Form[name], 0L);
                long count = (long)cacheInfo.Count;
                long size1 = cacheInfo.Size;
                long maxSize = cacheInfo.MaxSize;
                long size2 = size1 - num;
                HtmlTableRow htmlTableRow = HtmlUtil.AddRow(table, new string[6]
                {
          string.Empty,
          cacheInfo.Name,
          count.ToString(),
          MainUtil.FormatSize(size1, false),
          MainUtil.FormatSize(size2, false),
          MainUtil.FormatSize(maxSize, false)
                });
                for (int index = 2; index < htmlTableRow.Cells.Count; ++index)
                    htmlTableRow.Cells[index].Align = "right";
                htmlTableRow.Cells[htmlTableRow.Cells.Count - 2].Style["color"] = "red";
                htmlTableRow.Cells[htmlTableRow.Cells.Count - 1].Style["color"] = "lightgrey";
                HtmlInputHidden htmlInputHidden = new HtmlInputHidden();
                htmlInputHidden.ID = name;
                htmlInputHidden.Value = size1.ToString();
                HtmlInputHidden child = htmlInputHidden;
                htmlTableRow.Cells[0].Controls.Add((Control)child);
            }
            if (this.c_caches.Controls.Count > 0)
                this.c_caches.Controls.RemoveAt(0);
            this.c_caches.Controls.Add((Control)table);
            this.c_cacheTitle.Text = string.Format("Caches ({0})", (object)allCaches.Length);
        }

        /// <summary>Updates the totals.</summary>
        private void UpdateTotals()
        {
            CacheStatistics statistics = CacheManager.GetStatistics();
            this.c_totals.Text = string.Format("Entries: {0}, Size: {1}", (object)statistics.TotalCount, (object)statistics.TotalSize);
        }
    }
}
