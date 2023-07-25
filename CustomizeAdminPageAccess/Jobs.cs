using Microsoft.Extensions.DependencyInjection;
using Sitecore;
using Sitecore.Abstractions;
using Sitecore.Collections;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Reflection.Emit;
using Sitecore.sitecore.admin;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;


namespace CustomizeAdminPageAccess
{
    [AllowDependencyInjection]
    public class Jobs : AdminPage
    {
        private readonly DateTime now = DateTime.Now;
        private static readonly FieldInfo runningJobsFieldInfo = typeof(DefaultJobManager).GetField("runningJobs", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo queuedJobsFieldInfo = typeof(DefaultJobManager).GetField("queuedJobs", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo finishedJobsFieldInfo = typeof(DefaultJobManager).GetField("finishedJobs", BindingFlags.Instance | BindingFlags.NonPublic);
        protected HtmlForm Form1;
        protected Literal lt;

        protected override void OnInit(EventArgs args)
        {
            Assert.ArgumentNotNull((object)args, "arguments");
            this.CheckSecurity();
            base.OnInit(args);
        }

        protected override void OnLoad(EventArgs e)
        {
            JobManager.GetJobs();
            StringBuilder stringBuilder = new StringBuilder();
            BaseJobManager requiredService = ServiceLocator.ServiceProvider.GetRequiredService<BaseJobManager>();
            if (requiredService == null || !(requiredService is DefaultJobManager defaultJobManager))
            {
                this.lt.Text = "Job Viewer page does not work with non-default BaseJobManager implementation.";
            }
            else
            {
                this.ShowRefreshStatus(stringBuilder);
                if (CustomizeAdminPageAccess.Jobs.runningJobsFieldInfo != (FieldInfo)null)
                    this.ShowJobs(stringBuilder, "Running jobs", (ICollection<BaseJob>)((SafeDictionary<Handle, BaseJob>)CustomizeAdminPageAccess.Jobs.runningJobsFieldInfo.GetValue((object)defaultJobManager)).Values.ToArray<BaseJob>());
                if (CustomizeAdminPageAccess.Jobs.queuedJobsFieldInfo != (FieldInfo)null)
                    this.ShowJobs(stringBuilder, "Queued jobs", (ICollection<BaseJob>)((IEnumerable<BaseJob>)CustomizeAdminPageAccess.Jobs.queuedJobsFieldInfo.GetValue((object)defaultJobManager)).ToArray<BaseJob>());
                if (CustomizeAdminPageAccess.Jobs.finishedJobsFieldInfo != (FieldInfo)null)
                    this.ShowJobs(stringBuilder, "Finished jobs", (ICollection<BaseJob>)((IEnumerable<BaseJob>)CustomizeAdminPageAccess.Jobs.finishedJobsFieldInfo.GetValue((object)defaultJobManager)).Reverse<BaseJob>().ToArray<BaseJob>());
                this.lt.Text = stringBuilder.ToString();
            }
        }

        protected virtual void ShowJobs(
          StringBuilder stringBuilder,
          string name,
          ICollection<BaseJob> enumerable)
        {
            stringBuilder.AppendLine("<h1>" + name + ":</h1><br />");
            if (enumerable.Count > 0)
            {
                stringBuilder.AppendLine("<table class='jobs-table'>");
                stringBuilder.AppendLine("<thead><tr><td class='counter'>No</td><td class='add-time'>Added</td><td class='title'>Title</td><td class='progress'>Progress</td><td class='priority'>Priority</td></tr></thead>");
                int num = 1;
                foreach (BaseJob baseJob in (IEnumerable<BaseJob>)enumerable)
                {
                    long total = baseJob.Status.Total;
                    TimeSpan timeSpan = this.now - baseJob.QueueTime.ToLocalTime();
                    string str1 = timeSpan.Hours == 0 ? string.Empty : timeSpan.Hours.ToString() + "h ";
                    string str2 = timeSpan.Minutes == 0 ? string.Empty : timeSpan.Minutes.ToString() + "m ";
                    stringBuilder.AppendLine("<tr>");
                    stringBuilder.AppendLine("<td class='counter'>" + (object)num + "</td>");
                    StringBuilder stringBuilder1 = stringBuilder;
                    object[] objArray = new object[7];
                    objArray[0] = (object)"<td class='add-time'>";
                    DateTime dateTime = baseJob.QueueTime;
                    dateTime = dateTime.ToLocalTime();
                    objArray[1] = (object)dateTime.ToLongTimeString();
                    objArray[2] = (object)" (";
                    objArray[3] = (object)str1;
                    objArray[4] = (object)str2;
                    objArray[5] = (object)timeSpan.Seconds;
                    objArray[6] = (object)"s ago)</td>";
                    string str3 = string.Concat(objArray);
                    stringBuilder1.AppendLine(str3);
                    stringBuilder.AppendLine("<td class='title'>" + baseJob.Name + "</td>");
                    stringBuilder.AppendLine("<td class='progress'>" + (object)baseJob.Status.Processed + (total > 0L ? (object)(" of " + (object)total) : (object)string.Empty) + "</td>");
                    stringBuilder.AppendLine("<td class='priority'>" + (object)baseJob.Options.Priority + "</td>");
                    stringBuilder.AppendLine("</tr>");
                    ++num;
                }
                stringBuilder.AppendLine("</table>");
            }
            else
                stringBuilder.AppendLine("<b>No jobs</b><br />");
            stringBuilder.AppendLine("<br /><hr />");
        }

        protected virtual void ShowRefreshStatus(StringBuilder stringBuilder)
        {
            int result;
            int.TryParse(this.Request.QueryString["refresh"], out result);
            stringBuilder.Append("Last updated: " + DateTime.Now.ToString((IFormatProvider)CultureInfo.InvariantCulture) + ". ");
            int[] numArray = new int[7] { 1, 2, 5, 10, 20, 30, 60 };
            stringBuilder.Append("Refresh each <a href='jobs.aspx' class='refresh-link " + (result == 0 ? "refresh-selected" : string.Empty) + "'>No Refresh</a>");
            foreach (int num in numArray)
            {
                string str1 = result == num ? "refresh-selected" : string.Empty;
                string str2 = string.Format(", <a href='jobs.aspx?refresh={0}' class='refresh-link {1}'>{0} sec</a>", (object)num, (object)str1);
                stringBuilder.Append(str2);
            }
            stringBuilder.Append("<br /><br />");
        }

    }
}