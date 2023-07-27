// Decompiled with JetBrains decompiler
// Type: Sitecore.ExperienceContentManagement.Administration.Logs
// Assembly: Sitecore.ExperienceContentManagement.Administration, Version=9.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FA007834-6198-418B-8E97-23CDD4805B4C
// Assembly location: C:\inetpub\wwwroot\sc103xyzsc.dev.local\bin\Sitecore.ExperienceContentManagement.Administration.dll

using Sitecore;
using Sitecore.Configuration;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security.AntiXss;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace CustomizeAdminPageAccess
{
    [AllowDependencyInjection]
    public class Logs : AdminPage
    {
        private static readonly Regex LogGroupRegex = new Regex("(.+)(\\.\\d\\d\\d\\d\\d\\d\\d\\d)(\\.\\d\\d\\d\\d\\d\\d)?\\.txt", RegexOptions.Compiled);
        private static readonly Regex LogFileRegex = new Regex("^[a-zA-Z\\.\\d]+\\.txt$", RegexOptions.Compiled);
        private static readonly Regex LogTypeRegex = new Regex("^[a-zA-Z\\.\\d]+$", RegexOptions.Compiled);
        protected HtmlForm form1;
        protected Panel ListTypes;
        protected BulletedList LogTypes;
        protected Panel LogTypeInfo;
        protected Literal LogTypeInfoTypeName;
        protected Literal CurrentLogFileName;
        protected HyperLink CurrentLogFileView;
        protected HyperLink CurrentLogFileTailView;
        protected HyperLink CurrentLogFileDownload;
        protected BulletedList DownloadFilesPlaceHolder;
        protected Panel LogFileViewer;
        protected Label LogFileText;

        private static string LogFolder
        {
            get
            {
                string path = MainUtil.MapPath(Settings.LogFolder);
                Assert.IsNotNullOrEmpty(path, "logFolder must not be null or empty");
                Assert.IsTrue(Directory.Exists(path), "logFolder must exist: " + path);
                return path;
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            this.CheckSecurity("sitecore\\LogsPageAccess");
            string str1 = this.Request.QueryString["file"];
            if (!string.IsNullOrEmpty(str1))
            {
                Assert.IsTrue(Logs.LogFileRegex.IsMatch(str1), "invalid file param");
                string lastBytes = this.Request.QueryString["lastBytes"];
                if (!string.IsNullOrEmpty(lastBytes))
                    this.ShowLogFile(str1, lastBytes);
                else
                    this.DownloadLogFile(str1);
            }
            else
            {
                string str2 = this.Request.QueryString["type"];
                if (!string.IsNullOrEmpty(str2))
                {
                    Assert.IsTrue(Logs.LogTypeRegex.IsMatch(str2), "invalid type param");
                    this.ShowLogTypeInfo(str2);
                }
                else
                    this.ShowLogTypes();
            }
        }

        private void ShowLogFile(string fileName, string lastBytes)
        {
            Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));
            Assert.ArgumentNotNullOrEmpty(lastBytes, nameof(lastBytes));
            this.LogFileViewer.Visible = true;
            FileInfo file = FileSystem.GetFile(Logs.LogFolder, fileName);
            int num = int.Parse(lastBytes);
            long offset = num <= 0 ? 0L : Math.Max(0L, file.Length - (long)num);
            using (FileStream fileStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
                StreamReader streamReader = new StreamReader((Stream)fileStream);
                string str1 = num > 0 ? "Displayed last " + lastBytes + " bytes of contents" : "Displayed entire file contents";
                DateTime utcNow = DateTime.UtcNow;
                string newLine = Environment.NewLine;
                string str2 = "File Name: " + fileName + newLine + str1 + newLine + "Collected on " + utcNow.ToLongDateString() + " at " + utcNow.ToLongTimeString() + " UTC" + newLine + newLine;
                if (offset > 0L)
                {
                    streamReader.ReadLine();
                    str2 += string.Format("({0} bytes are skipped){1}...{2}", (object)offset, (object)newLine, (object)newLine);
                }
                this.LogFileText.Text = HttpUtility.HtmlEncode(str2 + streamReader.ReadToEnd()).Replace(Environment.NewLine, "<br/>");
            }
        }

        private void DownloadLogFile(string fileName)
        {
            Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));
            FileInfo file = FileSystem.GetFile(Logs.LogFolder, fileName);
            Assert.IsTrue(file.Exists, "The file doesn't exit");
            this.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
            Stream outputStream = this.Response.OutputStream;
            Assert.IsNotNull((object)outputStream, "outputStream must not be null");
            StreamWriter streamWriter = new StreamWriter(outputStream);
            using (FileStream fileStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                StreamReader streamReader = new StreamReader((Stream)fileStream);
                while (streamReader.Peek() >= 0)
                    streamWriter.WriteLine(streamReader.ReadLine());
            }
            this.Response.End();
        }

        private void ShowLogTypeInfo(string type)
        {
            Assert.ArgumentNotNullOrEmpty(type, nameof(type));
            this.LogTypeInfo.Visible = true;
            this.LogTypeInfoTypeName.Text = AntiXssEncoder.HtmlEncode(type, false);
            string[] logFiles = this.GetLogFiles(type);
            string fileName1 = Path.GetFileName(Logs.GetCurrentLogFile(logFiles));
            this.CurrentLogFileName.Text = AntiXssEncoder.HtmlEncode(fileName1, false);
            string downloadUrl1 = this.GetDownloadUrl(fileName1);
            string baseViewUrl = downloadUrl1 + "&lastBytes=";
            string viewUrl1 = Logs.GetViewUrl(baseViewUrl);
            string viewUrl2 = Logs.GetViewUrl(baseViewUrl, 10240);
            this.CurrentLogFileView.NavigateUrl = viewUrl1;
            this.CurrentLogFileTailView.NavigateUrl = viewUrl2;
            this.CurrentLogFileDownload.NavigateUrl = downloadUrl1;
            foreach (string path in logFiles)
            {
                string fileName2 = Path.GetFileName(path);
                string downloadUrl2 = this.GetDownloadUrl(fileName2);
                this.DownloadFilesPlaceHolder.Items.Add(new ListItem()
                {
                    Text = fileName2,
                    Value = downloadUrl2
                });
            }
        }

        private string GetDownloadUrl(string currentLogFile) => this.Request.Url.LocalPath + "?file=" + currentLogFile;

        private static string GetViewUrl(string baseViewUrl, int bytes = 0) => baseViewUrl + (object)bytes;

        private void ShowLogTypes()
        {
            this.ListTypes.Visible = true;
            foreach (string logGroupName in Logs.GetLogGroupNames(Directory.GetFiles(MainUtil.MapPath(Settings.LogFolder))))
            {
                string str = this.Request.Url.LocalPath + "?type=" + logGroupName;
                this.LogTypes.Items.Add(new ListItem()
                {
                    Text = logGroupName,
                    Value = str
                });
            }
        }

        private static string GetCurrentLogFile(string[] files)
        {
            Assert.ArgumentNotNull((object)files, nameof(files));
            return ((IEnumerable<string>)files).OrderByDescending<string, DateTime>(new Func<string, DateTime>(File.GetCreationTimeUtc)).FirstOrDefault<string>();
        }

        private string[] GetLogFiles(string type)
        {
            Assert.ArgumentNotNullOrEmpty(type, nameof(type));
            string[] files = Directory.GetFiles(Logs.LogFolder, type + "*.txt");
            int firstFileLength = "log.yyyymmdd.txt".Length;
            int extensionLength = ".txt".Length;
            return ((IEnumerable<string>)files).OrderBy<string, string>((Func<string, string>)(x => Path.GetFileName(x).Length != firstFileLength ? x : x.Substring(0, x.Length - extensionLength) + ".000000.txt")).ToArray<string>();
        }

        public static string[] GetLogGroupNames(string[] files)
        {
            Assert.ArgumentNotNull((object)files, nameof(files));
            return ((IEnumerable<string>)files).Where<string>((Func<string, bool>)(x => !string.IsNullOrEmpty(x))).Select(x => new
            {
                FilePath = x,
                Position = x.LastIndexOf('\\')
            }).Select(x => x.Position >= 0 ? x.FilePath.Substring(x.Position + 1) : x.FilePath).Select<string, Match>((Func<string, Match>)(x => Logs.LogGroupRegex.Match(x))).Where<Match>((Func<Match, bool>)(x => x.Success)).Select<Match, string>((Func<Match, string>)(x => x.Groups[1].Value)).Distinct<string>().ToArray<string>();
        }
    }

    internal static class FileSystem
    {
        public static DirectoryInfo GetSubdirectory(DirectoryInfo directory, string name)
        {
            Assert.ArgumentNotNull((object)directory, nameof(directory));
            Assert.ArgumentNotNullOrEmpty(name.Trim(), nameof(name));
            return FileSystem.GetSubdirectory(directory.FullName, name);
        }

        public static DirectoryInfo GetSubdirectory(string directoryPath, string name)
        {
            Assert.ArgumentNotNull((object)directoryPath, nameof(directoryPath));
            Assert.ArgumentNotNullOrEmpty(name.Trim(), nameof(name));
            return new DirectoryInfo(FileSystem.CombinePathSafe(directoryPath, (string)null, name));
        }

        public static FileInfo GetFile(DirectoryInfo directory, string name)
        {
            Assert.ArgumentNotNull((object)directory, nameof(directory));
            Assert.ArgumentNotNullOrEmpty(name.Trim(), nameof(name));
            return FileSystem.GetFile(directory.FullName, name);
        }

        public static FileInfo GetFile(string directoryPath1, string directoryPath2, string name)
        {
            Assert.ArgumentNotNull((object)directoryPath1, nameof(directoryPath1));
            Assert.ArgumentNotNull((object)directoryPath2, nameof(directoryPath2));
            Assert.ArgumentNotNullOrEmpty(name.Trim(), nameof(name));
            return new FileInfo(FileSystem.CombinePathSafe(directoryPath1, directoryPath2, name));
        }

        private static string CombinePathSafe(
          string directoryPath1,
          string directoryPath2,
          string name)
        {
            string str = directoryPath2 == null ? directoryPath1 : Path.Combine(directoryPath1, directoryPath2);
            string path = Path.Combine(str, name);
            Assert.AreEqual(Path.GetFullPath(Path.GetDirectoryName(path)), Path.GetFullPath(str), "The name contains invalid file name characters: " + name);
            return path;
        }

        public static FileInfo GetFile(string directoryPath, string name)
        {
            Assert.ArgumentNotNull((object)directoryPath, nameof(directoryPath));
            Assert.ArgumentNotNullOrEmpty(name.Trim(), nameof(name));
            string str = Path.Combine(directoryPath, name);
            Assert.AreEqual(Path.GetFullPath(Path.GetDirectoryName(str)), Path.GetFullPath(directoryPath), "The file name is invalid: " + name);
            return new FileInfo(str);
        }

        public static bool TryDelete(this FileInfo file)
        {
            Assert.ArgumentNotNull((object)file, nameof(file));
            try
            {
                file.Delete();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryDelete(this DirectoryInfo directory, bool recursive)
        {
            Assert.ArgumentNotNull((object)directory, nameof(directory));
            try
            {
                directory.Delete(recursive);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string ReadAllText(this FileInfo file)
        {
            Assert.ArgumentNotNull((object)file, nameof(file));
            using (StreamReader streamReader = new StreamReader((Stream)file.OpenRead()))
                return streamReader.ReadToEnd();
        }

        public static void WriteAllText(this FileInfo file, string text)
        {
            Assert.ArgumentNotNull((object)file, nameof(file));
            Assert.ArgumentNotNull((object)text, nameof(text));
            using (StreamWriter streamWriter = new StreamWriter((Stream)file.OpenWrite()))
                streamWriter.Write(text);
        }

        public static DirectoryInfo GetUniqueTempFolder()
        {
            string tempFileName = Path.GetTempFileName();
            new FileInfo(tempFileName).TryDelete();
            DirectoryInfo uniqueTempFolder = new DirectoryInfo(tempFileName + ".dir");
            uniqueTempFolder.Create();
            return uniqueTempFolder;
        }

        public static string GetNameWithoutExtension(FileInfo file) => FileSystem.GetNameWithoutExtension(file.Name);

        public static string GetNameWithoutExtension(string name) => Path.GetFileNameWithoutExtension(name);
    }
}
