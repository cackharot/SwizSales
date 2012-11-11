using System;
using System.Windows;
using System.Threading;
using System.Collections.ObjectModel;

// Toolkit namespace
using SimpleMvvmToolkit;

// Toolkit extension methods
using SimpleMvvmToolkit.ModelExtensions;
using SwizSales.Core.ServiceContracts;
using SwizSales.Library;

namespace SwizSales.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvmprop</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase<MainViewModel>
    {
        #region Initialization and Cleanup

        //private ICustomerService serviceAgent;

        // Default ctor
        public MainViewModel() { }

        /*public MainViewModel(ICustomerService serviceAgent)
        {
            this.serviceAgent = serviceAgent;
        }*/

        #endregion

        #region Notifications

        // TODO: Add events to notify the view or obtain data from the view
        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;

        #endregion

        #region Properties

        // TODO: Add properties using the mvvmprop code snippet

        #endregion

        #region Methods

        // TODO: Add methods that will be called by the view

        #endregion

        #region Completion Callbacks

        // TODO: Optionally add callback methods for async calls to the service agent
        
        #endregion

        #region Helpers

        // Helper method to notify View of an error
        private void NotifyError(string message, Exception error)
        {
            // Notify view of an error
            SendMessage(MessageTokens.GlobalNotification, new NotificationEventArgs(message));
            Notify(ErrorNotice, new NotificationEventArgs<Exception>(message, error));
        }

        #endregion
    }
}