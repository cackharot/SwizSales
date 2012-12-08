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
using SwizSales.Core.Library;
using SwizSales.Core.Services;
using System.ComponentModel;
using SwizSales.Library;
using SwizSales.Properties;

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
        private ICustomerService serviceAgent;
        private IReportService reportService;
        private ISettingsService settingsService;

        public CustomerSearchViewModel() : this(new CustomerService(), new ReportService()) { }

        public CustomerSearchViewModel(ICustomerService serviceAgent, IReportService reportService)
        {
            this.serviceAgent = serviceAgent;
            this.reportService = reportService;
            this.settingsService = new SettingsService();
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

            var cusPointSetting = this.settingsService.GetSettingById(Constants.CustomerPointsStartDateId);

            if (cusPointSetting != null && !string.IsNullOrEmpty(cusPointSetting.Value))
            {
                this.CustomerPointStartDate = DateTime.Parse(cusPointSetting.Value);
            }
            else
            {
                this.CustomerPointStartDate = DateTime.Parse("01/01/" + DateTime.Now.Year);
            }
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

        private DateTime customerPointDate;
        public DateTime CustomerPointStartDate
        {
            get { return customerPointDate; }
            set
            {
                customerPointDate = value;
                NotifyPropertyChanged(m => m.CustomerPointStartDate);
            }
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
                    var cuslst = this.serviceAgent.Search(new CustomerSearchCondition
                    {
                        Email = searchValue,
                        Name = searchValue,
                        Mobile = searchValue,
                        Number = searchValue,
                        PageNo = 1,
                        PageSize = 10,
                        Status = true
                    });

                    if (cuslst != null && cuslst.Count > 0)
                    {
                        var customerIds = cuslst.Where(x => x.Id != Settings.Default.DefaultCustomerId).Select(x => x.Id).Distinct();

                        var cusTotalAmounts = this.reportService.GetCusomerTotalAmount(customerIds, this.CustomerPointStartDate);

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

        private DelegateCommand searchCommand;
        public DelegateCommand SearchCommand
        {
            get { return searchCommand ?? (searchCommand = new DelegateCommand(() => { DoSearch(); })); }
            private set { searchCommand = value; }
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