using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleMvvmToolkit;
using SwizSales.Library;
using SwizSales.Core.Library;

namespace SwizSales.ViewModel
{
    public class NotificationViewModel : ViewModelBase<NotificationViewModel>
    {
        public NotificationViewModel()
        {
            this.RegisterToReceiveMessages(MessageTokens.GlobalNotification, (s, e) =>
            {
                var message = e.Message;
                LogService.Warn(message);
                this.Status = message;
            });
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
    }
}
