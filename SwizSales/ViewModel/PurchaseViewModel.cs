using System;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Collections.ObjectModel;
using SimpleMvvmToolkit;
using SimpleMvvmToolkit.ModelExtensions;
using SwizSales.Core.ServiceContracts;
using System.ComponentModel;
using SwizSales.Core.Model;
using SwizSales.Core.Library;
using SwizSales.Core.Services;
using SwizSales.Library;

namespace SwizSales.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvmprop</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// </summary>
    public class PurchaseViewModel : ViewModelBase<PurchaseViewModel>
    {
        #region Initialization and Cleanup

        private readonly BackgroundWorker worker = new BackgroundWorker();
        private IStockService serviceAgent;
        private ISupplierService supplierService = new SupplierService();
        private IEmployeeService employeeService = new EmployeeService();

        public PurchaseViewModel(IStockService serviceAgent)
        {
            this.serviceAgent = serviceAgent;
            this.SearchCondition = new PurchaseSearchCondition();
            this.Purchases = new ObservableCollection<Purchase>();
            this.Suppliers = new ObservableCollection<Supplier>(supplierService.Search(new SupplierSearchCondition()));
            this.Employees = new ObservableCollection<Employee>(employeeService.Search(new EmployeeSearchCondition()));
            Init();
        }

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
                    NotifyError("Error while loading Purchases", e.Error);
                    return;
                }

                var res = e.Result as Collection<Purchase>;

                if (res != null)
                    this.Purchases = new ObservableCollection<Purchase>(res);
            };
        }

        void Search(object sender, DoWorkEventArgs e)
        {
            var condition = e.Argument as PurchaseSearchCondition;

            if (condition != null)
            {
                e.Result = this.serviceAgent.Search(condition);
                Thread.Sleep(500);
            }
        }

        void DoSearch()
        {
            this.SelectedPurchase = null;
            this.Purchases = new ObservableCollection<Purchase>();

            if (this.worker.IsBusy)
            {
                return;
            }

            IsBusy = true;

            this.worker.RunWorkerAsync(this.SearchCondition);
        }

        #endregion

        #region Notifications

        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;

        public event EventHandler<NotificationEventArgs<Purchase>> ManagePurchaseNotice;

        public event EventHandler<NotificationEventArgs<Purchase>> PreviewNotice;

        public event EventHandler<NotificationEventArgs<Purchase>> PaymentsNotice;

        #endregion

        #region Properties

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                NotifyPropertyChanged(m => m.IsBusy);
                NotifyPropertyChanged(x => TodayOrdersVisible);
            }
        }

        public Visibility TodayOrdersVisible
        {
            get { return IsBusy ? Visibility.Hidden : Visibility.Visible; }
        }

        private ObservableCollection<Purchase> _lstPurchase;
        public ObservableCollection<Purchase> Purchases
        {
            get { return _lstPurchase; }
            set
            {
                _lstPurchase = value;
                NotifyPropertyChanged(m => m.Purchases);
                NotifyPropertyChanged(m => m.SearchTotal);
                NotifyPropertyChanged(m => m.SearchTotalPaid);
                NotifyPropertyChanged(m => m.SearchTotalBalance);
            }
        }

        private Purchase _selectedPurchase;
        public Purchase SelectedPurchase
        {
            get { return _selectedPurchase; }
            set
            {
                _selectedPurchase = value;
                NotifyPropertyChanged(m => m.SelectedPurchase);
                NotifyCommands();
            }
        }

        private PurchaseSearchCondition _searchCondition;
        public PurchaseSearchCondition SearchCondition
        {
            get { return _searchCondition; }
            set
            {
                _searchCondition = value;
                NotifyPropertyChanged(m => m.SearchCondition);
            }
        }

        private ObservableCollection<Supplier> _lstSupplier;
        public ObservableCollection<Supplier> Suppliers
        {
            get { return _lstSupplier; }
            set
            {
                _lstSupplier = value;
                NotifyPropertyChanged(m => m.Suppliers);
            }
        }

        private ObservableCollection<Employee> _lstEmployee;
        public ObservableCollection<Employee> Employees
        {
            get { return _lstEmployee; }
            set
            {
                _lstEmployee = value;
                NotifyPropertyChanged(m => m.Employees);
            }
        }

        public double SearchTotal
        {
            get { return this.Purchases == null ? 0 : this.Purchases.Sum(x => x.ActualTotalAmount); }
        }

        public double SearchTotalPaid
        {
            get { return this.Purchases == null ? 0 : this.Purchases.Sum(x => x.PaidAmount); }
        }

        public double SearchTotalBalance
        {
            get { return this.Purchases == null ? 0 : this.Purchases.Sum(x => x.BalanceAmount); }
        }

        #endregion

        #region Methods

        private DelegateCommand _searchCommand;
        public DelegateCommand SearchCommand
        {
            get
            {
                return _searchCommand ?? (_searchCommand = new DelegateCommand(() =>
                {
                    DoSearch();
                }));
            }
            private set { _searchCommand = value; }
        }

        private DelegateCommand _resetCommand;
        public DelegateCommand ResetCommand
        {
            get
            {
                return _resetCommand ?? (_resetCommand = new DelegateCommand(() =>
                {
                    this.SearchCondition = new PurchaseSearchCondition();
                    DoSearch();
                }));
            }
            private set { _resetCommand = value; }
        }

        private DelegateCommand editCommand;
        public DelegateCommand EditCommand
        {
            get
            {
                return editCommand ?? (editCommand = new DelegateCommand(() =>
                {
                    if (this.SelectedPurchase != null && this.ManagePurchaseNotice != null)
                    {
                        this.ManagePurchaseNotice(this, new NotificationEventArgs<Purchase>("Manage", this.SelectedPurchase));
                    }

                }, () => { return this.SelectedPurchase != null; }));
            }
            private set { editCommand = value; }
        }

        private DelegateCommand deleteCommand;
        public DelegateCommand DeleteCommand
        {
            get
            {
                return deleteCommand ?? (deleteCommand = new DelegateCommand(() =>
                {
                }, () => { return this.SelectedPurchase != null; }));
            }
            private set { deleteCommand = value; }
        }

        private DelegateCommand newCommand;
        public DelegateCommand NewCommand
        {
            get
            {
                return newCommand ?? (newCommand = new DelegateCommand(() =>
                {
                    if (this.ManagePurchaseNotice != null)
                    {
                        this.ManagePurchaseNotice(this, new NotificationEventArgs<Purchase>("Manage", null));
                    }
                }));
            }
            private set { newCommand = value; }
        }

        private DelegateCommand paymentsCommand;
        public DelegateCommand PaymentsCommand
        {
            get { return paymentsCommand ?? (paymentsCommand = new DelegateCommand(() => { }, () => { return this.SelectedPurchase != null; })); }
            private set { paymentsCommand = value; }
        }

        private DelegateCommand printCommand;
        public DelegateCommand PrintCommand
        {
            get { return printCommand ?? (printCommand = new DelegateCommand(() => { }, () => { return this.SelectedPurchase != null; })); }
            private set { printCommand = value; }
        }

        private DelegateCommand previewCommand;
        public DelegateCommand PreviewCommand
        {
            get { return previewCommand ?? (previewCommand = new DelegateCommand(() => { }, () => { return this.SelectedPurchase != null; })); }
            private set { previewCommand = value; }
        }

        private DelegateCommand refreshCommand;
        public DelegateCommand RefreshCommand
        {
            get { return refreshCommand ?? (refreshCommand = new DelegateCommand(() => { DoSearch(); })); }
            private set { refreshCommand = value; }
        }

        #endregion

        #region Helpers

        private void NotifyError(string message, Exception error)
        {
            SendMessage(MessageTokens.GlobalNotification, new NotificationEventArgs(message));
            LogService.Error(message, error);
            Notify(ErrorNotice, new NotificationEventArgs<Exception>(message, error));
        }

        private void NotifyCommands()
        {
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            PrintCommand.RaiseCanExecuteChanged();
            PreviewCommand.RaiseCanExecuteChanged();
            PaymentsCommand.RaiseCanExecuteChanged();
        }

        #endregion
    }
}