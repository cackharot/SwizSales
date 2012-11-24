using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Data.SqlClient;
using System.Windows.Shapes;

namespace SwizSales.Core.Library
{
    public class DbBackupService
    {
        Server DbServer { get; set; }
        string DatabaseName { get; set; }
        public Backup Backup { get; set; }

        public DbBackupService(string serverName, string userName, string password)
        {
            InitDbServer(serverName, userName, password);
        }

        public DbBackupService(string connectionString)
        {
            var connInfo = new SqlConnectionStringBuilder(connectionString);
            this.DatabaseName = connInfo.InitialCatalog;
            InitDbServer(connInfo.DataSource, connInfo.UserID, connInfo.Password);
        }

        ~DbBackupService()
        {
            if (this.DbServer != null && this.DbServer.ConnectionContext.IsOpen)
            {
                this.DbServer.ConnectionContext.Cancel();
            }
        }
        
        private void InitDbServer(string serverName, string userName, string password)
        {
            ServerConnection conn = new ServerConnection(serverName, userName, password);
            this.DbServer = new Server(conn);
            this.Backup = new Backup();
        }

        public void BackupDataBase(string destinationPath)
        {
            Backup.Action = BackupActionType.Database;
            Backup.Database = this.DatabaseName;
            destinationPath = System.IO.Path.Combine(destinationPath, this.DatabaseName + ".bak");
            Backup.Devices.Add(new BackupDeviceItem(destinationPath, DeviceType.File));
            Backup.Initialize = true;
            Backup.Checksum = true;
            Backup.ContinueAfterError = true;
            Backup.Incremental = false;
            Backup.PercentCompleteNotification = 1;
            Backup.LogTruncation = BackupTruncateLogType.Truncate;
            Backup.PercentComplete += new PercentCompleteEventHandler(backup_PercentComplete);
            Backup.Complete += new ServerMessageEventHandler(backup_Complete);
            Backup.SqlBackupAsync(this.DbServer);
            string script = Backup.Script(this.DbServer);
        }

        static void backup_Complete(object sender, ServerMessageEventArgs e)
        {
        }

        static void backup_PercentComplete(object sender, PercentCompleteEventArgs e)
        {
        }
    }
}
