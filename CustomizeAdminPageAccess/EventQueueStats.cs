// Decompiled with JetBrains decompiler
// Type: Sitecore.ExperienceContentManagement.Administration.EventQueueStats
// Assembly: Sitecore.ExperienceContentManagement.Administration, Version=9.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FA007834-6198-418B-8E97-23CDD4805B4C
// Assembly location: C:\inetpub\wwwroot\sc103xyzsc.dev.local\bin\Sitecore.ExperienceContentManagement.Administration.dll

using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Eventing;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace CustomizeAdminPageAccess
{
    [AllowDependencyInjection]
    public class EventQueueStats : AdminPage
    {
        private static readonly MethodInfo getTimestampForLastProcessingMI = typeof(Sitecore.Eventing.EventQueue).GetMethod("GetTimestampForLastProcessing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        protected HtmlHead Head1;
        protected HtmlForm Form1;
        protected Literal lt;
        protected Repeater EQStatsRepeater;
        protected Label CleanupResult;
        protected DropDownList Databases;
        protected TextBox IntervalToKeep;

        protected override void OnInit(EventArgs args)
        {
            Assert.ArgumentNotNull((object)args, "arguments");
            this.CheckSecurity("sitecore\\EventQueueStatsPageAccess");
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!this.IsPostBack)
            {
                this.ShowRefreshStatus();
                this.Databases.DataSource = (object)Factory.GetDatabases();
                this.Databases.DataBind();
            }
            this.ReloadStatistics();
        }

        protected virtual void ShowRefreshStatus()
        {
            StringBuilder stringBuilder = new StringBuilder();
            int result;
            int.TryParse(this.Request.QueryString["refresh"], out result);
            stringBuilder.Append("Last updated: " + DateTime.Now.ToString((IFormatProvider)CultureInfo.InvariantCulture) + ". ");
            int[] numArray = new int[7] { 1, 2, 5, 10, 20, 30, 60 };
            stringBuilder.Append("Refresh each <a href='EventQueueStats.aspx' class='refresh-link " + (result == 0 ? "refresh-selected" : string.Empty) + "'>No Refresh</a>");
            foreach (int num in numArray)
            {
                string str1 = result == num ? "refresh-selected" : string.Empty;
                string str2 = string.Format(", <a href='EventQueueStats.aspx?refresh={0}' class='refresh-link {1}'>{0} sec</a>", (object)num, (object)str1);
                stringBuilder.Append(str2);
            }
            stringBuilder.Append("<br /><br />");
            this.lt.Text = stringBuilder.ToString();
        }

        protected virtual void ReloadStatistics()
        {
            List<Database> databases = Factory.GetDatabases();
            List<EventQueueStats.EQStats> eqStatsList = new List<EventQueueStats.EQStats>();
            foreach (Database database in databases)
            {
                EventQueueStats.EQStats eqStats = new EventQueueStats.EQStats();
                IEventQueue eventQueue = database.RemoteEvents.EventQueue;
                eqStats.DatabaseName = database.Name;
                eqStats.NumberOfRecords = eventQueue.GetQueuedEventCount();
                if (eventQueue is Sitecore.Eventing.EventQueue)
                {
                    Assert.IsNotNull((object)EventQueueStats.getTimestampForLastProcessingMI, "getTimestampForLastProcessingMI is null");
                    object obj = EventQueueStats.getTimestampForLastProcessingMI.Invoke((object)eventQueue, (object[])null);
                    Assert.IsNotNull(obj, "timestampObject is null");
                    PropertyInfo property1 = obj.GetType().GetProperty("Sequence", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    Assert.IsNotNull((object)property1, "sequencePI is null");
                    eqStats.LastProcessedTimestamp = (long)property1.GetValue(obj);
                    PropertyInfo property2 = obj.GetType().GetProperty("Date", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    Assert.IsNotNull((object)property1, "datePI is null");
                    eqStats.RecordsToBeProcessed = this.GetNumberOfRecordsToProcess(database.ConnectionStringName, eqStats.LastProcessedTimestamp, (DateTime)property2.GetValue(obj));
                }
                eqStats.LastTimestamp = this.GetLastEventTimestamp(eventQueue, database.ConnectionStringName);
                eqStatsList.Add(eqStats);
            }
            this.EQStatsRepeater.DataSource = (object)eqStatsList;
            this.EQStatsRepeater.DataBind();
        }

        protected virtual long GetNumberOfRecordsToProcess(
          string connectionStringName,
          long lastProcessedTimestamp,
          DateTime fromDate)
        {
            if (ConfigurationManager.ConnectionStrings[connectionStringName] == null)
                return 0;
            string cmdText = "SELECT COUNT(*) FROM EventQueue WITH (NOLOCK) WHERE (InstanceName <> @p1 AND RaiseGlobally=1 OR InstanceName = @p1 AND RaiseLocally=1) AND Stamp>=@p2" + (fromDate != DateTime.MinValue ? " AND Created>=@p3" : string.Empty);
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString))
            {
                connection.Open();
                using (SqlCommand sqlCommand = new SqlCommand(cmdText, connection))
                {
                    sqlCommand.Parameters.AddWithValue("@p1", (object)Settings.InstanceName);
                    sqlCommand.Parameters.AddWithValue("@p2", (object)(lastProcessedTimestamp + 1L));
                    if (fromDate != DateTime.MinValue)
                        sqlCommand.Parameters.AddWithValue("@p3", (object)fromDate);
                    using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        return sqlDataReader.Read() ? (long)sqlDataReader.GetInt32(0) : 0L;
                }
            }
        }

        protected virtual long GetLastEventTimestamp(IEventQueue queue, string databaseName)
        {
            QueuedEvent lastEvent = queue.GetLastEvent();
            return lastEvent == null ? 0L : lastEvent.Timestamp;
        }

        [Obsolete("Use GetLastEventTimestamp instead.")]
        protected virtual long GetLastEventTimestampDirectly(string databaseName)
        {
            if (ConfigurationManager.ConnectionStrings[databaseName] == null)
                return 0;
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings[databaseName].ConnectionString))
            {
                connection.Open();
                using (SqlCommand sqlCommand = new SqlCommand("SELECT TOP(1) Stamp FROM EventQueue ORDER BY Stamp DESC", connection))
                {
                    using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        return sqlDataReader.Read() ? this.GetLong((IDataReader)sqlDataReader, 0) : 0L;
                }
            }
        }

        protected virtual long GetLong(IDataReader dataReader, int columnIndex)
        {
            Assert.ArgumentNotNull((object)dataReader, "reader");
            object obj = dataReader.GetValue(columnIndex);
            if (!(obj is byte[] source))
                return MainUtil.GetLong(obj, 0L);
            return ((IEnumerable<byte>)source).Aggregate<byte, long>(0L, (Func<long, byte, long>)((s, b) => (s << 8) + (long)b));
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
                    this.CleanupEventQueue(database.RemoteEvents.EventQueue, timeSpan);
                    this.ReloadStatistics();
                }
            }
        }

        protected virtual void CleanupEventQueue(IEventQueue queue, TimeSpan intervalToKeep)
        {
            if (queue is NullEventQueue)
                this.CleanupResult.Text = "Cleanup is not possible for this database.";
            else
                queue.Cleanup(intervalToKeep);
        }

        public class EQStats
        {
            public string DatabaseName { get; set; }

            public long NumberOfRecords { get; set; }

            public long LastProcessedTimestamp { get; set; }

            public long LastTimestamp { get; set; }

            public long RecordsToBeProcessed { get; set; }
        }
    }
}

