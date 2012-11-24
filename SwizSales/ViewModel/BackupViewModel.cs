using System;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Collections.ObjectModel;
using SimpleMvvmToolkit;
using SimpleMvvmToolkit.ModelExtensions;
using SwizSales.Core.Library;
using System.Globalization;
using System.Windows.Threading;

namespace SwizSales.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvmprop</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// </summary>
    public class BackupViewModel : ViewModelBase<BackupViewModel>
    {
        #region Initialization and Cleanup

        private DbBackupService backupService;

        public BackupViewModel()
        {
            if (!IsInDesignMode)
            {
                Init();
            }
        }

        private void Init()
        {
            using (var ctx = new SwizSales.Core.Model.OpenPOSDbEntities())
            {
                var efConn = new System.Data.EntityClient.EntityConnection(ctx.Connection.ConnectionString);
                this.backupService = new DbBackupService(efConn.StoreConnection.ConnectionString);
                this.backupService.Backup.PercentComplete += new Microsoft.SqlServer.Management.Smo.PercentCompleteEventHandler(Backup_PercentComplete);
                this.backupService.Backup.Complete += new Microsoft.SqlServer.Management.Common.ServerMessageEventHandler(Backup_Complete);
            }
        }

        void Backup_Complete(object sender, Microsoft.SqlServer.Management.Common.ServerMessageEventArgs e)
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.Invoke(DispatcherPriority.Normal, new DispatcherOperationCallback((percent) =>
            {
                //this.Progress = 0;
                this.Status = "Backup completed!";
                this.IsBusy = false;
                return null;
            }), null);
        }

        void Backup_PercentComplete(object sender, Microsoft.SqlServer.Management.Smo.PercentCompleteEventArgs e)
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.Invoke(DispatcherPriority.SystemIdle, new DispatcherOperationCallback((percent) =>
            {
                this.Progress = (int)percent;
                this.Status = string.Format(CultureInfo.CurrentCulture, "{0}% completed.", (int)percent);
                return null;
            }), e.Percent);
        }

        #endregion

        #region Notifications

        // TODO: Add events to notify the view or obtain data from the view
        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;

        #endregion

        #region Properties

        private string _backupPath;
        public string BackupPath
        {
            get { return _backupPath; }
            set
            {
                _backupPath = value;
                NotifyPropertyChanged(m => m.BackupPath);
                BackupCommand.RaiseCanExecuteChanged();
            }
        }

        private string _status;
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyPropertyChanged(m => m.Status);
            }
        }

        private int _progress;
        public int Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                NotifyPropertyChanged(m => m.Progress);
            }
        }

        private bool isBusy;
        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                NotifyPropertyChanged(m => m.IsBusy);
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new DispatcherOperationCallback((arg) =>
                {
                    this.BackupCommand.RaiseCanExecuteChanged();
                    return null;
                }), null);
            }
        }

        #endregion

        #region Methods

        private void Backup()
        {
            this.Status = string.Empty;
            this.Progress = 0;

            if (!string.IsNullOrEmpty(this.BackupPath))
            {
                try
                {
                    IsBusy = true;
                    Init();
                    this.backupService.BackupDataBase(this.BackupPath);
                }
                catch (Exception ex)
                {
                    LogService.Error("Error while backup.", ex);
                    IsBusy = false;
                }
            }
            else
            {
                this.Status = "Please choose a valid backup folder!";
            }
        }

        #endregion

        #region Completion Callbacks

        private DelegateCommand backupCommand;
        public DelegateCommand BackupCommand
        {
            get
            {
                return backupCommand ?? (backupCommand = new DelegateCommand(Backup, () =>
                {
                    return !string.IsNullOrEmpty(this.BackupPath) && !IsBusy;
                }));
            }
            private set { backupCommand = value; }
        }

        #endregion

        #region Helpers

        // Helper method to notify View of an error
        private void NotifyError(string message, Exception error)
        {
            // Notify view of an error
            Notify(ErrorNotice, new NotificationEventArgs<Exception>(message, error));
        }

        #endregion
    }
}