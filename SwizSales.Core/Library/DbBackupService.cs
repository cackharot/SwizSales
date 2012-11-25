using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Data.SqlClient;
using System.Windows.Shapes;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Collections.Specialized;

namespace SwizSales.Core.Library
{
    public class DbBackupService
    {
        string ServerName { get; set; }
        string UserName { get; set; }
        string Password { get; set; }
        string DatabaseName { get; set; }
        
        public DbBackupService(string serverName, string userName, string password,string databaseName)
        {
            Init(serverName, userName, password,databaseName);
        }

        public DbBackupService(string connectionString)
        {
            var connInfo = new SqlConnectionStringBuilder(connectionString);
            Init(connInfo.DataSource, connInfo.UserID, connInfo.Password,connInfo.InitialCatalog);
        }

        private void Init(string serverName, string userName, string password,string databaseName)
        {
            this.ServerName = serverName;
            this.UserName = userName;
            this.Password = password;
            this.DatabaseName = databaseName;
        }

        private Server GetDbServer()
        {
            ServerConnection conn = new ServerConnection(this.ServerName, this.UserName, this.Password);
            var server = new Server(conn);
            return server;
        }

        public void BackupDataBase(string destinationPath, Action<int> percentCompleteCallback, Action completeCallback)
        {
            var server = GetDbServer(); 
            
            var backup = new Backup();
            backup.Action = BackupActionType.Database;
            backup.Database = this.DatabaseName;
            backup.Devices.Add(new BackupDeviceItem(GetFileName(destinationPath, this.DatabaseName, ".bak"), DeviceType.File));
            backup.Initialize = true;
            backup.Checksum = true;
            backup.ContinueAfterError = true;
            backup.Incremental = false;
            backup.PercentCompleteNotification = 1;
            backup.LogTruncation = BackupTruncateLogType.Truncate;

            backup.Complete += (s, e) =>
            {
                if (completeCallback != null)
                {
                    completeCallback();
                }
            };

            backup.PercentComplete += (s, e) =>
            {
                if (percentCompleteCallback != null)
                {
                    percentCompleteCallback(e.Percent);
                }
            };

            backup.SqlBackupAsync(server);
        }

        public void GenerateScriptFile(string destinationPath, Action<int> percentCompleteCallback, Action<Exception> errorCallback)
        {
            var server = GetDbServer();
            var db = server.Databases[this.DatabaseName];

            var scripter = new Scripter(server);
            SetScriptOptions(destinationPath, scripter);

            var smoObjects = new List<Urn>();

            foreach (Table tb in db.Tables)
            {
                if (!tb.IsSystemObject)
                {
                    smoObjects.Add(tb.Urn);
                }
            }

            scripter.ScriptingError += new ScriptingErrorEventHandler((s, e) =>
            {
                if (errorCallback != null)
                {
                    errorCallback(e.InnerException);
                }
            });

            scripter.ScriptingProgress += new ProgressReportEventHandler((s, e) =>
            {
                int percent = Convert.ToInt32(((double)e.TotalCount / (double)e.Total) * 100.0);

                if (percentCompleteCallback != null)
                {
                    percentCompleteCallback(percent);
                }
            });

            //var sc = scripter.Script(smoObjects.ToArray());

            foreach (var sc in scripter.EnumScript(smoObjects.ToArray()))
            {

            }
        }

        private void SetScriptOptions(string destinationPath, Scripter scripter)
        {
            scripter.Options.AppendToFile = false;
            scripter.Options.ContinueScriptingOnError = true;
            scripter.Options.NoCommandTerminator = true;
            scripter.Options.DriAll = true;
            scripter.Options.DriDefaults = true;
            scripter.Options.FileName = GetFileName(destinationPath, this.DatabaseName, ".sql");
            scripter.Options.IncludeDatabaseContext = false;
            scripter.Options.IncludeIfNotExists = false;
            scripter.Options.ScriptData = true;
            scripter.Options.ScriptDrops = false;
            scripter.Options.ScriptSchema = true;
            scripter.Options.TimestampToBinary = false;
            scripter.Options.ToFileOnly = true;
            scripter.Options.WithDependencies = true;
        }

        private static int count = 1;
        private string GetFileName(string destinationPath, string databaseName, string extn)
        {
            var filePath = System.IO.Path.Combine(destinationPath, databaseName + extn);

            if (System.IO.File.Exists(filePath))
            {
                filePath = GetFileName(destinationPath, databaseName + count++, extn);
            }

            return filePath;
        }
    }
}
