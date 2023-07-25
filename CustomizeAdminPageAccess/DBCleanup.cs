// Decompiled with JetBrains decompiler
// Type: Sitecore.ExperienceContentManagement.Administration.DbCleanup
// Assembly: Sitecore.ExperienceContentManagement.Administration, Version=9.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FA007834-6198-418B-8E97-23CDD4805B4C
// Assembly location: C:\inetpub\wwwroot\sc103xyzsc.dev.local\bin\Sitecore.ExperienceContentManagement.Administration.dll

using Sitecore.Configuration;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.ExperienceContentManagement.Administration.Helpers;
using Sitecore.ExperienceContentManagement.Administration.Helpers.DbCleanup;
using Sitecore.sitecore.admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace CustomizeAdminPageAccess
{
    [AllowDependencyInjection]
    public class DbCleanup : AdminPage
    {
        protected readonly CleanupTaskRunner CleanupTaskRunner;
        protected HtmlForm form1;
        protected CheckBoxList databaseList;
        protected CheckBoxList taskList;
        protected Button run;
        protected UpdatePanel updatePanel;
        protected TextBox log;
        protected Timer logRefreshTimer;

        public DbCleanup() => this.CleanupTaskRunner = new CleanupTaskRunner();

        protected override void OnInit(EventArgs args)
        {
            Assert.ArgumentNotNull((object)args, "arguments");
            this.CheckSecurity();
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!this.IsPostBack)
            {
                this.BindDatabaseList();
                this.BindTaskList();
            }
            this.BindLog();
        }

        protected virtual void RunOnClick(object sender, EventArgs e)
        {
            List<string> list = this.databaseList.Items.Cast<ListItem>().Where<ListItem>((Func<ListItem, bool>)(databaseItem => databaseItem.Selected)).Select<ListItem, string>((Func<ListItem, string>)(databaseItem => databaseItem.Value)).ToList<string>();
            List<string> selectedTasks = this.taskList.Items.Cast<ListItem>().Where<ListItem>((Func<ListItem, bool>)(taskItem => taskItem.Selected)).Select<ListItem, string>((Func<ListItem, string>)(taskItem => taskItem.Value)).ToList<string>();
            this.CleanupTaskRunner.RunCleanUp(list.SelectMany<string, TaskToRun>((Func<string, IEnumerable<TaskToRun>>)(database => selectedTasks.Select<string, TaskToRun>((Func<string, TaskToRun>)(task => new TaskToRun()
            {
                Database = database,
                Task = (CleanupTasks)Enum.Parse(typeof(CleanupTasks), task)
            })))));
            this.BindLog();
        }

        protected void BindDatabaseList()
        {
            this.databaseList.DataSource = (object)((IEnumerable<string>)Factory.GetDatabaseNames()).Where<string>((Func<string, bool>)(name => !Factory.GetDatabase(name).ReadOnly));
            this.databaseList.DataBind();
        }

        protected void BindLog()
        {
            bool running;
            this.log.Text = this.CleanupTaskRunner.GetLog(out running);
            this.logRefreshTimer.Enabled = running;
        }

        protected void BindTaskList()
        {
            this.taskList.Items.Clear();
            this.taskList.Items.AddRange(((IEnumerable<CleanupTasks>)Enum.GetValues(typeof(CleanupTasks))).Select<CleanupTasks, ListItem>((Func<CleanupTasks, ListItem>)(task => new ListItem(task.GetDescription<CleanupTasks>(), task.ToString()))).ToArray<ListItem>());
        }
    }
}
