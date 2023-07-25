// Decompiled with JetBrains decompiler
// Type: Sitecore.ExperienceContentManagement.Administration.RawSearch
// Assembly: Sitecore.ExperienceContentManagement.Administration, Version=9.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FA007834-6198-418B-8E97-23CDD4805B4C
// Assembly location: C:\inetpub\wwwroot\sc103xyzsc.dev.local\bin\Sitecore.ExperienceContentManagement.Administration.dll

using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.sitecore.admin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace CustomizeAdminPageAccess
{
    [AllowDependencyInjection]
    public class RawSearch : AdminPage
    {
        private const string ItemPathFormat = "<a href='/sitecore/admin/dbbrowser.aspx?db={0}&id={1}{3}'>[{0}] {{{1}}} - {2}</a>";
        private const string FileLink = "<a href='/sitecore/shell/Applications/Layouts/IDE.aspx?fi=%2f{1}'>{0}</a>";
        protected HtmlHead Head1;
        protected HtmlForm form1;
        protected Label Label1;
        protected TextBox Query;
        protected Label WarningLabel;
        protected Button Button1;
        protected CheckBox SearchItemNames;
        protected CheckBox SearchFields;
        protected CheckBox SearchFiles;
        protected CheckBox IgnoreCase;
        protected TextBox NeibourSymbolsAroundFoundOccurance;
        protected TextBox MaxCapturesAmount;
        protected TextBox ExcludeFileExtensions;
        protected PlaceHolder DatabaseSearchPlaceholder;
        protected PlaceHolder ItemNamesSearchPlaceholder;
        protected Literal ItemNamesResults;
        protected PlaceHolder FieldsSearchPlaceholder;
        protected Literal SharedFieldResults;
        protected Literal UnVersionedFieldResults;
        protected Literal VersionedFieldResults;
        protected PlaceHolder FileSystemSearch;
        protected Literal FileNamesResults;
        protected Literal FolderNamesResults;
        protected Literal FileContentsResults;

        protected static Dictionary<Pair<string, Guid>, string> ItemNamesCache => Sitecore.Context.Items["Sitecore.Support.Search-ItemNamesCache"] is Dictionary<Pair<string, Guid>, string> dictionary ? dictionary : (Sitecore.Context.Items["Sitecore.Support.Search-ItemNamesCache"] = (object)new Dictionary<Pair<string, Guid>, string>()) as Dictionary<Pair<string, Guid>, string>;

        protected static Dictionary<Pair<string, Guid>, string> ItemPathsCache => Sitecore.Context.Items["Sitecore.Support.Search-ItemPathsCache"] is Dictionary<Pair<string, Guid>, string> dictionary ? dictionary : (Sitecore.Context.Items["Sitecore.Support.Search-ItemPathsCache"] = (object)new Dictionary<Pair<string, Guid>, string>()) as Dictionary<Pair<string, Guid>, string>;

        protected override void OnInit(EventArgs arguments)
        {
            Assert.ArgumentNotNull((object)arguments, nameof(arguments));
            this.CheckSecurity();
            this.WarningLabel.Visible = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!this.IsPostBack)
            {
                this.SearchItemNames.Checked = false;
                this.SearchFields.Checked = false;
            }
            base.OnLoad(e);
        }

        protected virtual void DoSearch(object o, EventArgs a)
        {
            this.ItemNamesResults.Text = string.Empty;
            this.SharedFieldResults.Text = string.Empty;
            this.VersionedFieldResults.Text = string.Empty;
            this.UnVersionedFieldResults.Text = string.Empty;
            int result1;
            int capsLimit = int.TryParse(this.MaxCapturesAmount.Text, out result1) ? result1 : 3;
            int result2 = int.TryParse(this.NeibourSymbolsAroundFoundOccurance.Text, out result2) ? result2 : 40;
            string text = this.Query.Text.ToLower().Replace('*', '%');
            if (string.IsNullOrEmpty(text.Trim('%')))
            {
                this.WarningLabel.Visible = true;
            }
            else
            {
                foreach (Database database in Factory.GetDatabases())
                {
                    string connectionString = this.GetConnectionString(database);
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            connection.Open();
                            if (this.SearchItemNames.Checked)
                                this.DoSearchNames(database, connection, text);
                            if (this.SearchFields.Checked)
                                this.DoSearchFields(database, connection, text, capsLimit, result2);
                        }
                    }
                }
                this.CheckNoEntries(this.SharedFieldResults);
                this.CheckNoEntries(this.UnVersionedFieldResults);
                this.CheckNoEntries(this.VersionedFieldResults);
                if (!this.SearchFiles.Checked)
                    return;
                this.DoSearchFS(text, capsLimit, result2, this.ExcludeFileExtensions.Text.Split(','));
            }
        }

        protected virtual void DoSearchNames(Database database, SqlConnection connection, string text)
        {
            this.DatabaseSearchPlaceholder.Visible = true;
            string name = database.Name;
            List<Guid> guidList = new List<Guid>();
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = "select [ID], [Name] from [Items] WITH (NOLOCK) where [Name] like @0";
                command.Parameters.AddWithValue("@0", (object)text);
                using (SqlDataReader sqlDataReader = command.ExecuteReader())
                {
                    while (sqlDataReader.Read())
                    {
                        Guid guid = (Guid)sqlDataReader["ID"];
                        guidList.Add(guid);
                    }
                }
            }
            StringBuilder stringBuilder = new StringBuilder();
            foreach (Guid id in guidList)
            {
                string path = this.GetPath(connection, id);
                string str = string.Format("<a href='/sitecore/admin/dbbrowser.aspx?db={0}&id={1}{3}'>[{0}] {{{1}}} - {2}</a>", (object)name, (object)id, (object)path, (object)string.Empty);
                stringBuilder.Append("<li>");
                stringBuilder.Append(str);
                stringBuilder.Append("</li>");
            }
            this.ItemNamesResults.Text += stringBuilder.ToString();
            this.ItemNamesSearchPlaceholder.Visible = true;
        }

        protected virtual void DoSearchFields(
          Database database,
          SqlConnection connection,
          string text,
          int capsLimit,
          int neighbourCharactersLimit)
        {
            this.DoSearchSharedFields(database, connection, text, capsLimit, neighbourCharactersLimit);
            this.DoSearchUnversionedFields(database, connection, text, capsLimit, neighbourCharactersLimit);
            this.DoSearchVersionedFields(database, connection, text, capsLimit, neighbourCharactersLimit);
            this.FieldsSearchPlaceholder.Visible = true;
            this.DatabaseSearchPlaceholder.Visible = true;
        }

        protected virtual void DoSearchSharedFields(
          Database database,
          SqlConnection connection,
          string text,
          int capsLimit,
          int neighbourCharactersLimit)
        {
            List<RawSearch.DatabaseEntry> databaseEntryList = new List<RawSearch.DatabaseEntry>();
            string name1 = database.Name;
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = "select [ItemID], [FieldID], [Value] from [SharedFields] WITH (NOLOCK) where [Value] like @0";
                command.Parameters.AddWithValue("@0", (object)text);
                using (SqlDataReader sqlDataReader = command.ExecuteReader())
                {
                    while (sqlDataReader.Read())
                    {
                        Guid guid1 = (Guid)sqlDataReader["ItemID"];
                        string str = (string)sqlDataReader["Value"];
                        Guid guid2 = (Guid)sqlDataReader["FieldID"];
                        databaseEntryList.Add(new RawSearch.DatabaseEntry()
                        {
                            ItemId = guid1,
                            FieldId = guid2,
                            FieldValue = str
                        });
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            foreach (RawSearch.DatabaseEntry databaseEntry in databaseEntryList)
            {
                string path = this.GetPath(connection, databaseEntry.ItemId);
                string name2 = this.GetName(connection, databaseEntry.FieldId);
                string entryPath = string.Format("<a href='/sitecore/admin/dbbrowser.aspx?db={0}&id={1}{3}'>[{0}] {{{1}}} - {2}</a> [field: {4}]", (object)name1, (object)databaseEntry.ItemId, (object)path, (object)string.Empty, (object)name2);
                this.SearchAndOutputCaptures(text, databaseEntry.FieldValue, entryPath, capsLimit, neighbourCharactersLimit, sb);
            }
            this.SharedFieldResults.Text += sb.ToString();
        }

        protected virtual void DoSearchUnversionedFields(
          Database database,
          SqlConnection connection,
          string text,
          int capsLimit,
          int neighbourCharactersLimit)
        {
            List<RawSearch.DatabaseEntry> databaseEntryList = new List<RawSearch.DatabaseEntry>();
            string name1 = database.Name;
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = "select [ItemID], [FieldID], [Language], [Value] from [UnversionedFields] WITH (NOLOCK) where [Value] like @0";
                command.Parameters.AddWithValue("@0", (object)text);
                using (SqlDataReader sqlDataReader = command.ExecuteReader())
                {
                    while (sqlDataReader.Read())
                    {
                        Guid guid1 = (Guid)sqlDataReader["ItemID"];
                        string str1 = (string)sqlDataReader["Language"];
                        string str2 = (string)sqlDataReader["Value"];
                        Guid guid2 = (Guid)sqlDataReader["FieldID"];
                        databaseEntryList.Add(new RawSearch.DatabaseEntry()
                        {
                            ItemId = guid1,
                            FieldId = guid2,
                            FieldValue = str2,
                            Language = str1
                        });
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            foreach (RawSearch.DatabaseEntry databaseEntry in databaseEntryList)
            {
                string name2 = this.GetName(connection, databaseEntry.FieldId);
                string path = this.GetPath(connection, databaseEntry.ItemId);
                string entryPath = string.Format("<a href='/sitecore/admin/dbbrowser.aspx?db={0}&id={1}{3}'>[{0}] {{{1}}} - {2}</a> [field: {4}, language: {5}]", (object)name1, (object)databaseEntry.ItemId, (object)path, (object)("&lang=" + databaseEntry.Language), (object)name2, (object)databaseEntry.Language);
                this.SearchAndOutputCaptures(text, databaseEntry.FieldValue, entryPath, capsLimit, neighbourCharactersLimit, sb);
            }
            this.UnVersionedFieldResults.Text += sb.ToString();
        }

        protected virtual void DoSearchVersionedFields(
          Database database,
          SqlConnection connection,
          string text,
          int capsLimit,
          int neighbourCharactersLimit)
        {
            List<RawSearch.DatabaseEntry> databaseEntryList = new List<RawSearch.DatabaseEntry>();
            string name1 = database.Name;
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = "select [ItemID], [FieldID], [Language], [Version], [Value] from [VersionedFields] WITH (NOLOCK) where [Value] like @0";
                command.Parameters.AddWithValue("@0", (object)text);
                using (SqlDataReader sqlDataReader = command.ExecuteReader())
                {
                    while (sqlDataReader.Read())
                    {
                        Guid guid1 = (Guid)sqlDataReader["ItemID"];
                        string str1 = (string)sqlDataReader["Language"];
                        string str2 = (string)sqlDataReader["Value"];
                        int num = (int)sqlDataReader["Version"];
                        Guid guid2 = (Guid)sqlDataReader["FieldID"];
                        databaseEntryList.Add(new RawSearch.DatabaseEntry()
                        {
                            ItemId = guid1,
                            FieldId = guid2,
                            FieldValue = str2,
                            Language = str1,
                            Version = num
                        });
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            foreach (RawSearch.DatabaseEntry databaseEntry in databaseEntryList)
            {
                string name2 = this.GetName(connection, databaseEntry.FieldId);
                string path = this.GetPath(connection, databaseEntry.ItemId);
                string entryPath = string.Format("<a href='/sitecore/admin/dbbrowser.aspx?db={0}&id={1}{3}'>[{0}] {{{1}}} - {2}</a> [field: {4}, language: {5}, version: {6}]", (object)name1, (object)databaseEntry.ItemId, (object)path, (object)("&lang=" + databaseEntry.Language + "&ver=" + (object)databaseEntry.Version), (object)name2, (object)databaseEntry.Language, (object)databaseEntry.Version);
                this.SearchAndOutputCaptures(text, databaseEntry.FieldValue, entryPath, capsLimit, neighbourCharactersLimit, sb);
            }
            this.VersionedFieldResults.Text += sb.ToString();
        }

        protected virtual void DoSearchFS(
          string text,
          int capsLimit,
          int neighbourCharactersLimit,
          string[] ignoreFileExtensions)
        {
            string path = this.Server.MapPath("/").TrimEnd('\\');
            StringBuilder sbcontent = new StringBuilder();
            StringBuilder sbfolders = new StringBuilder();
            StringBuilder sbfiles = new StringBuilder();
            this.DoSearchFS(text, path, sbfolders, sbfiles, sbcontent, capsLimit, neighbourCharactersLimit, ignoreFileExtensions);
            this.FileContentsResults.Text = sbcontent.ToString();
            this.FileNamesResults.Text = sbfiles.ToString();
            this.FolderNamesResults.Text = sbfolders.ToString();
            this.FileSystemSearch.Visible = true;
        }

        protected virtual void DoSearchFS(
          string text,
          string path,
          StringBuilder sbfolders,
          StringBuilder sbfiles,
          StringBuilder sbcontent,
          int capsLimit,
          int neighbourCharactersLimit,
          string[] ignoreFileExtensions)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                try
                {
                    string name = Path.GetFileName(file);
                    string withoutExtension = Path.GetFileNameWithoutExtension(file);
                    if (RawSearch.Capture.IsMatch(name, text) || RawSearch.Capture.IsMatch(withoutExtension, text))
                    {
                        sbfiles.Append("<li>");
                        string virtualPath = this.GetVirtualPath(file);
                        sbfiles.Append(string.Format("<a href='/sitecore/shell/Applications/Layouts/IDE.aspx?fi=%2f{1}'>{0}</a>", (object)file, (object)this.Server.UrlEncode(virtualPath)));
                        sbfiles.Append("</li>");
                    }
                    if (((IEnumerable<string>)ignoreFileExtensions).All<string>((Func<string, bool>)(w => !name.EndsWith(w))))
                    {
                        string source = File.ReadAllText(file);
                        string virtualPath = this.GetVirtualPath(file);
                        this.SearchAndOutputCaptures(text, source, string.Format("<a href='/sitecore/shell/Applications/Layouts/IDE.aspx?fi=%2f{1}'>{0}</a>", (object)file, (object)this.Server.UrlEncode(virtualPath)), capsLimit, neighbourCharactersLimit, sbcontent);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("An error occured when trying to search in the file " + file, ex, (object)this);
                }
            }
            foreach (string directory in Directory.GetDirectories(path))
            {
                if (RawSearch.Capture.IsMatch(Path.GetFileName(directory), text))
                {
                    sbfolders.Append("<li>");
                    sbfolders.Append(directory);
                    sbfolders.Append("</li>");
                }
                this.DoSearchFS(text, directory, sbfolders, sbfiles, sbcontent, capsLimit, neighbourCharactersLimit, ignoreFileExtensions);
            }
        }

        protected void CheckNoEntries(Literal obj)
        {
            if (!string.IsNullOrEmpty(obj.Text))
                return;
            obj.Text = "<li>No entries were found</li>";
        }

        protected void SearchAndOutputCaptures(
          string text,
          string source,
          string entryPath,
          int capsLimit,
          int neighbourCharactersLimit,
          StringBuilder sb)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(source) || string.IsNullOrEmpty(entryPath) || capsLimit <= 0 || neighbourCharactersLimit <= 0)
                return;
            this.OutputFoundCaps(RawSearch.Capture.Search(source, text, capsLimit), entryPath, source, neighbourCharactersLimit, sb);
        }

        protected void OutputFoundCaps(
          RawSearch.Capture[] caps,
          string mainEntry,
          string source,
          int neighbourCharactersLimit,
          StringBuilder sb)
        {
            if (caps.Length == 0)
                return;
            int num1 = -1;
            sb.Append("<li>");
            sb.Append(mainEntry);
            sb.Append("<ul>");
            foreach (RawSearch.Capture cap in caps)
            {
                int num2 = num1;
                num1 = cap.Index;
                int startIndex = Math.Max(num2 + 1, num1 - neighbourCharactersLimit);
                string s1 = source.Substring(startIndex, num1 - startIndex);
                string s2 = cap.Value;
                int num3 = num1 + cap.Value.Length;
                int length = Math.Min(neighbourCharactersLimit, source.Length - num3);
                string s3 = source.Substring(num1 + cap.Length, length);
                sb.Append("<li>");
                sb.Append(HttpUtility.HtmlEncode(s1));
                sb.Append("<b>");
                sb.Append(HttpUtility.HtmlEncode(s2));
                sb.Append("</b>");
                sb.Append(HttpUtility.HtmlEncode(s3));
                sb.Append("</li>");
            }
            sb.Append("</ul>");
            sb.Append("</li>");
        }

        protected string GetPath(SqlConnection connection, Guid id)
        {
            Pair<string, Guid> key = new Pair<string, Guid>(connection.ConnectionString, id);
            if (RawSearch.ItemPathsCache.ContainsKey(key))
                return RawSearch.ItemPathsCache[key];
            string str = string.Empty;
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT TOP 1 [ParentID] from [Items] Where [ID] = @idparam";
                SqlParameter sqlParameter = new SqlParameter("idparam", (object)id);
                command.Parameters.Add(sqlParameter);
                object obj = command.ExecuteScalar();
                if (obj != null)
                {
                    Guid id1 = (Guid)obj;
                    if (id1 != Guid.Empty)
                        str = this.GetPath(connection, id1);
                }
            }
            string path = str + "/" + this.GetName(connection, id);
            RawSearch.ItemPathsCache.Add(key, path);
            return path;
        }

        protected string GetName(SqlConnection connection, Guid id)
        {
            Pair<string, Guid> key = new Pair<string, Guid>(connection.ConnectionString, id);
            if (RawSearch.ItemNamesCache.ContainsKey(key))
                return RawSearch.ItemNamesCache[key];
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT [Name] From [Items] where [ID] = @idparam";
                SqlParameter sqlParameter = new SqlParameter("idparam", (object)id);
                command.Parameters.Add(sqlParameter);
                using (SqlDataReader sqlDataReader = command.ExecuteReader())
                {
                    if (sqlDataReader.Read())
                    {
                        string name = (string)sqlDataReader["Name"];
                        RawSearch.ItemNamesCache.Add(key, name);
                        return name;
                    }
                }
            }
            return (string)null;
        }

        protected string GetConnectionString(Database database)
        {
            string connectionStringName = database.ConnectionStringName;
            if (string.IsNullOrEmpty(connectionStringName))
                return (string)null;
            ConnectionStringSettings connectionString1 = ConfigurationManager.ConnectionStrings[connectionStringName];
            if (connectionString1 == null)
            {
                this.Response.Write("Database " + connectionStringName + " not found <br />");
                return (string)null;
            }
            string connectionString2 = connectionString1.ConnectionString;
            if (connectionString2 != null)
                return connectionString2;
            this.Response.Write("Database " + connectionStringName + " not found <br />");
            return (string)null;
        }

        protected string GetVirtualPath(string file)
        {
            string virtualPath = file;
            string lower = this.Server.MapPath("/").ToLower();
            if (file.ToLower().Contains(lower))
                virtualPath = file.Substring(lower.Length);
            return virtualPath;
        }

        public class DatabaseEntry
        {
            public string FieldValue { get; set; }

            public Guid ItemId { get; set; }

            public Guid FieldId { get; set; }

            public string Language { get; set; }

            public int Version { get; set; }
        }

        public class Capture
        {
            public readonly int Index;
            public readonly int Length;
            public readonly string Value;

            public Capture(int index, int length, string value)
            {
                this.Index = index;
                this.Length = length;
                this.Value = value;
            }

            public static RawSearch.Capture[] Search(string source, string targetPattern, int capsLimit)
            {
                string source1 = targetPattern;
                targetPattern = targetPattern.Trim('%');
                string[] source2 = targetPattern.Split('%');
                List<RawSearch.Capture> captureList = new List<RawSearch.Capture>();
                if (source1.First<char>() != '%')
                {
                    if (!source.StartsWith(((IEnumerable<string>)source2).First<string>()))
                        return captureList.ToArray();
                    capsLimit = 1;
                }
                if (source1.Last<char>() != '%')
                {
                    if (!source.EndsWith(((IEnumerable<string>)source2).Last<string>()))
                        return captureList.ToArray();
                    capsLimit = 1;
                }
                if (source1.First<char>() != '%' && source1.Last<char>() != '%' && source2.Length > 1 && source.IndexOf(((IEnumerable<string>)source2).First<string>(), StringComparison.Ordinal) + ((IEnumerable<string>)source2).First<string>().Length > source.LastIndexOf(((IEnumerable<string>)source2).Last<string>(), StringComparison.Ordinal))
                    return captureList.ToArray();
                int[] source3 = new int[source2.Length];
                int num1 = -1;
                while (captureList.Count != capsLimit)
                {
                    int startIndex = num1 + 1;
                    int num2;
                    for (int index1 = 0; index1 < source2.Length; index1 = num2 + 1)
                    {
                        string str = source2[index1];
                        int num3 = source.IndexOf(str, startIndex, StringComparison.OrdinalIgnoreCase);
                        int[] numArray = source3;
                        int index2 = index1;
                        num2 = index2 + 1;
                        int num4 = num3;
                        numArray[index2] = num4;
                        if (num3 < 0)
                            return captureList.ToArray();
                        startIndex = num3 + str.Length;
                    }
                    num1 = ((IEnumerable<int>)source3).First<int>();
                    string str1 = source.Substring(num1, startIndex - num1);
                    captureList.Add(new RawSearch.Capture(num1, str1.Length, str1));
                }
                return captureList.ToArray();
            }

            public static bool IsMatch(string text, string pattern)
            {
                if (pattern == text.ToLower() || pattern.All<char>((Func<char, bool>)(ch => ch == '%')))
                    return true;
                if (pattern[0] != '%')
                {
                    int length = pattern.IndexOf('%', 0);
                    if (length >= 0)
                    {
                        if (!text.StartsWith(pattern.Substring(0, length), StringComparison.OrdinalIgnoreCase))
                            return false;
                    }
                    else if (pattern != text)
                        return false;
                }
                string[] strArray = pattern.Split('%');
                int startIndex = 0;
                foreach (string str in strArray)
                {
                    int num = text.IndexOf(str, startIndex, StringComparison.OrdinalIgnoreCase);
                    if (num < 0)
                        return false;
                    startIndex = num + str.Length;
                }
                if (pattern[pattern.Length - 1] != '%')
                {
                    int num = pattern.LastIndexOf("%", pattern.Length - 1, StringComparison.OrdinalIgnoreCase);
                    if (num >= 0 && !text.EndsWith(pattern.Substring(num + 1)))
                        return false;
                }
                return true;
            }
        }
    }
}
