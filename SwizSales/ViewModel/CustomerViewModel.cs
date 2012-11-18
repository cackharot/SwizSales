using System;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Collections.ObjectModel;

// Toolkit namespace
using SimpleMvvmToolkit;

// Toolkit extension methods
using SimpleMvvmToolkit.ModelExtensions;
using SwizSales.Core.ServiceContracts;
using SwizSales.Core.Model;
using System.Windows.Threading;
using System.ComponentModel;
using SwizSales.Core.Library;
using SwizSales.Library;
using SwizSales.Core.Services;
using SwizSales.Properties;

namespace SwizSales.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvmprop</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// </summary>
    public class CustomerViewModel : ViewModelBase<CustomerViewModel>
    {
        #region Initialization and Cleanup

        internal ICustomerService serviceAgent;
        internal IReportService reportService;
        private readonly BackgroundWorker worker = new BackgroundWorker();

        public CustomerViewModel(ICustomerService serviceAgent, IReportService reportService)
        {
            this.serviceAgent = serviceAgent;
            this.reportService = reportService;
            this.SearchCondition = new CustomerSearchCondition();
            this.PageSizes = new ObservableCollection<int>(new int[] { 10, 25, 50, 100, 150, 250, 500, 1000, 0 });
            Init();
        }

        #endregion

        #region Notifications

        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;

        public event EventHandler<NotificationEventArgs<Customer, bool>> DeleteCustomerNotice;

        public event EventHandler<NotificationEventArgs<Customer>> CustomerDetailsNotice;

        #endregion

        #region Properties

        private ObservableCollection<int> pageSizes;
        public ObservableCollection<int> PageSizes
        {
            get { return pageSizes; }
            set
            {
                pageSizes = value;
                NotifyPropertyChanged(m => m.PageSizes);
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                NotifyPropertyChanged(m => m.IsBusy);
            }
        }

        private ObservableCollection<Customer> _customerCollection;
        public ObservableCollection<Customer> CustomerCollection
        {
            get { return _customerCollection; }
            set
            {
                _customerCollection = value;
                NotifyPropertyChanged(m => m.CustomerCollection);
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

        private CustomerSearchCondition _searchCondition;
        public CustomerSearchCondition SearchCondition
        {
            get { return _searchCondition; }
            set
            {
                _searchCondition = value;
                NotifyPropertyChanged(m => m.SearchCondition);
            }
        }

        #endregion

        #region Methods

        private void Init()
        {
            worker.DoWork += new DoWorkEventHandler(Search);
            worker.RunWorkerCompleted += (s, e) =>
            {
                IsBusy = false;

                if (e.Cancelled)
                    return;

                if (e.Error != null)
                {
                    NotifyError("Error while loading Customers", e.Error);
                    return;
                }

                var res = e.Result as Collection<Customer>;

                if (res != null)
                {
                    this.CustomerCollection = new ObservableCollection<Customer>(res);
                }
            };

            DoSearch();
        }

        void Search(object sender, DoWorkEventArgs e)
        {
            var condition = e.Argument as CustomerSearchCondition;

            if (condition != null)
            {
                var cuslst = this.serviceAgent.Search(condition);

                if (cuslst != null && cuslst.Count > 0)
                {
                    var customerIds = cuslst.Where(x => x.Id != Settings.Default.DefaultCustomerId).Select(x => x.Id).Distinct();

                    var cusTotalAmounts = this.reportService.GetCusomerTotalAmount(customerIds);

                    if (cusTotalAmounts != null)
                    {
                        foreach (var cus in cuslst)
                        {
                            cus.Points = Convert.ToInt32(cusTotalAmounts.ContainsKey(cus.Id) ? (cusTotalAmounts[cus.Id] / Properties.Settings.Default.CustomerPointsAmount) : 0);
                        }
                    }
                }

                e.Result = cuslst;
                Thread.Sleep(500);
            }
        }

        void DoSearch()
        {
            this.SelectedCustomer = null;
            this.CustomerCollection = new ObservableCollection<Customer>();

            if (this.worker.IsBusy)
            {
                return;
            }

            IsBusy = true;

            this.worker.RunWorkerAsync(this.SearchCondition);
        }

        public void Load(CustomerSearchCondition condition)
        {
            try
            {
                var lst = serviceAgent.Search(condition);

                if (lst != null && lst.Count > 0)
                {
                    this.CustomerCollection = new ObservableCollection<Customer>(lst);
                }
                else
                {
                    this.CustomerCollection.Clear();
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message, ex);
            }
        }

        public void Delete(Customer entity)
        {
            this.CustomerCollection.Remove(entity);
            this.serviceAgent.Delete(entity.Id);
        }

        #endregion

        #region Completion Callbacks

        public DelegateCommand<Customer> _editCommand;
        public DelegateCommand<Customer> EditCommand
        {
            get
            {
                if (_editCommand == null)
                {
                    _editCommand = new DelegateCommand<Customer>((entity) =>
                    {
                        if (entity == null)
                            return;
                        Notify(CustomerDetailsNotice, new NotificationEventArgs<Customer>("Edit", entity));
                    });
                }

                return _editCommand;
            }
        }

        public DelegateCommand<Customer> _deleteCommand;
        public DelegateCommand<Customer> DeleteCommand
        {
            get
            {
                if (_deleteCommand == null)
                {
                    _deleteCommand = new DelegateCommand<Customer>((entity) =>
                    {
                        if (entity == null)
                            return;
                        Notify(DeleteCustomerNotice, new NotificationEventArgs<Customer, bool>("DeleteCustomer", entity, (canDelete) =>
                        {
                            if (canDelete)
                                Delete(entity);
                        }));
                    });
                }

                return _deleteCommand;
            }
        }

        private DelegateCommand _searchCommand;
        public DelegateCommand SearchCommand
        {
            get
            {
                return _searchCommand ?? (_searchCommand = new DelegateCommand(() =>
                {
                    DoSearch();
                }, () =>
                {
                    return true;
                }));
            }
        }

        private DelegateCommand _resetCommand;
        public DelegateCommand ResetCommand
        {
            get
            {
                return _resetCommand ?? (_resetCommand = new DelegateCommand(() =>
                {
                    this.SearchCondition = new CustomerSearchCondition();
                    DoSearch();
                }));
            }
            private set { _resetCommand = value; }
        }

        private DelegateCommand _addCommand;
        public DelegateCommand AddCommand
        {
            get
            {
                return _addCommand ?? (_addCommand = new DelegateCommand(() =>
                {
                    var entity = new Customer
                    {
                        SSN = string.Empty,
                        ContactDetail = new ContactDetail
                        {
                            ContactName = string.Empty,
                            City = "Puducherry",
                            Country = "India",
                            Pincode = "605001"
                        }
                    };
                    Notify(CustomerDetailsNotice, new NotificationEventArgs<Customer>("Add", entity));
                }, () =>
                {
                    return true;
                }));
            }
        }

        #endregion

        #region Helpers

        private void NotifyError(string message, Exception error)
        {
            SendMessage(MessageTokens.GlobalNotification, new NotificationEventArgs(message));
            Notify(ErrorNotice, new NotificationEventArgs<Exception>(message, error));
            LogService.Error(message, error);
        }

        #endregion
    }
}