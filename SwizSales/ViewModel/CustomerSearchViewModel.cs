using System;
using System.Windows;
using System.Threading;
using System.Collections.ObjectModel;

// Toolkit namespace
using SimpleMvvmToolkit;

// Toolkit extension methods
using SimpleMvvmToolkit.ModelExtensions;
using SwizSales.Core.ServiceContracts;
using SwizSales.Core.Model;
using SwizSales.Core.Library;
using SwizSales.Core.Services;
using System.ComponentModel;
using SwizSales.Library;

namespace SwizSales.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvmprop</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// </summary>
    public class CustomerSearchViewModel : ViewModelBase<CustomerSearchViewModel>
    {
        #region Initialization and Cleanup

        private readonly BackgroundWorker worker = new BackgroundWorker();
        internal ICustomerService serviceAgent;

        public CustomerSearchViewModel() : this(new CustomerService()) { }

        public CustomerSearchViewModel(ICustomerService serviceAgent)
        {
            this.serviceAgent = serviceAgent;
            Init();
        }

        public void Init()
        {
            worker.DoWork += new DoWorkEventHandler(Search);
            worker.RunWorkerCompleted += (s, e) =>
            {
                IsBusy = false;

                if (e.Cancelled)
                    return;

                if (e.Error != null)
                {
                    NotifyError("Error while searching customers", e.Error);
                    return;
                }

                var lst = e.Result as Collection<Customer>;

                if (lst != null)
                    this.CustomerList = new ObservableCollection<Customer>(lst);
            };
        }

        #endregion

        #region Notifications

        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice; 
        public event EventHandler<NotificationEventArgs> CloseNotice;

        #endregion

        #region Properties

        private bool isBusy;
        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                NotifyPropertyChanged(m => m.IsBusy);
                NotifyPropertyChanged(m => m.ListVisibility);
            }
        }

        public Visibility ListVisibility
        {
            get { return IsBusy ? Visibility.Hidden : Visibility.Visible; }
        }

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                NotifyPropertyChanged(m => m.SearchText);
                DoSearch();
            }
        }

        private ObservableCollection<Customer> _customerList;
        public ObservableCollection<Customer> CustomerList
        {
            get { return _customerList; }
            set
            {
                _customerList = value;
                NotifyPropertyChanged(m => m.CustomerList);
            }
        }

        private Customer _selectedCustomer;
        public Customer SelectedCustomer
        {
            get { return _selectedCustomer; }
            set
            {
                _selectedCustomer = value;
                NotifyPropertyChanged(m => m.SelectedCustomer);
            }
        }

        private DelegateCommand _selectCommand;
        public DelegateCommand SelectCommand
        {
            get
            {
                return _selectCommand ?? (_selectCommand = new DelegateCommand(() =>
                {
                    if (this.CloseNotice != null)
                    {
                        this.CloseNotice(this, new NotificationEventArgs());
                    }
                }));
            }
            private set { _selectCommand = value; }
        }

        #endregion

        #region Methods

        private void Search(object sender, DoWorkEventArgs e)
        {
            try
            {
                var searchValue = e.Argument as string;

                if (!string.IsNullOrEmpty(searchValue))
                {
                    var lst = this.serviceAgent.Search(new CustomerSearchCondition
                    {
                        Email = searchValue,
                        Name = searchValue,
                        Mobile = searchValue,
                        Number = searchValue,
                        PageNo = 1,
                        PageSize = 10,
                        Status = true
                    });

                    e.Result = lst;
                    Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                NotifyError("Error while searching customer", ex);
            }
        }

        public void DoSearch()
        {
            this.SelectedCustomer = null;

            if (this.worker.IsBusy)
            {
                return;
                //this.worker.CancelAsync();
            }

            if (!string.IsNullOrWhiteSpace(this.SearchText))
            {
                IsBusy = true;
                this.worker.RunWorkerAsync(this.SearchText);
            }
            else
            {
                this.CustomerList = new ObservableCollection<Customer>();
            }
        }

        #endregion
        
        #region Helpers

        // Helper method to notify View of an error
        private void NotifyError(string message, Exception error)
        {
            // Notify view of an error
            SendMessage(MessageTokens.GlobalNotification, new NotificationEventArgs(message));
            LogService.Error(message, error);
            Notify(ErrorNotice, new NotificationEventArgs<Exception>(message, error));
        }

        #endregion
    }
}