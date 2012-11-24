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
using SwizSales.Library;
using SwizSales.Core.Library;
using System.ComponentModel;
using SwizSales.Core.Model;

namespace SwizSales.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvmprop</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// </summary>
    public class OrdersViewModel : ViewModelBase<OrdersViewModel>
    {
        #region Initialization and Cleanup

        private IOrderService orderService;
        private readonly BackgroundWorker worker = new BackgroundWorker();

        public OrdersViewModel(IOrderService orderService)
        {
            this.orderService = orderService;
            this.SearchCondition = new OrderSearchCondition { FromOrderDate = DateTime.Today.AddDays(-7), ToOrderDate = DateTime.Today };
            this.PageSizes = new ObservableCollection<int>(new int[] { 10, 25, 50, 100, 150, 250, 500, 1000, 0 });
            Init();
        }

        #endregion

        #region Notifications

        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;
        public event EventHandler<NotificationEventArgs<Order>> ManageOrderNotice;

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

        private ObservableCollection<Order> _lstOrders;
        public ObservableCollection<Order> Orders
        {
            get { return _lstOrders; }
            set
            {
                _lstOrders = value;
                NotifyPropertyChanged(m => m.Orders);
                NotifyPropertyChanged(m => m.SearchTotal);
                NotifyPropertyChanged(m => m.SearchTotalPaid);
                NotifyPropertyChanged(m => m.SearchTotalBalance);
            }
        }

        private Order _selectedOrder;
        public Order SelectedOrder
        {
            get { return _selectedOrder; }
            set
            {
                _selectedOrder = value;
                NotifyPropertyChanged(m => m.SelectedOrder);
                NotifyCommands();
            }
        }

        private OrderSearchCondition _searchCondition;
        public OrderSearchCondition SearchCondition
        {
            get { return _searchCondition; }
            set
            {
                _searchCondition = value;
                NotifyPropertyChanged(m => m.SearchCondition);
            }
        }

        public double SearchTotal
        {
            get { return this.Orders == null ? 0 : this.Orders.Sum(x => x._TotalAmount); }
        }

        public double SearchTotalPaid
        {
            get { return this.Orders == null ? 0 : this.Orders.Sum(x => x._PaidAmount); }
        }

        public double SearchTotalBalance
        {
            get { return this.Orders == null ? 0 : this.Orders.Sum(x => x.BalanceAmount); }
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
                    NotifyError("Error while loading Orders", e.Error);
                    return;
                }

                var res = e.Result as Collection<Order>;

                if (res != null)
                    this.Orders = new ObservableCollection<Order>(res);
            };

            DoSearch();
        }

        void Search(object sender, DoWorkEventArgs e)
        {
            var condition = e.Argument as OrderSearchCondition;

            if (condition != null)
            {
                e.Result = this.orderService.Search(condition);
                Thread.Sleep(500);
            }
        }

        void DoSearch()
        {
            this.SelectedOrder = null;
            this.Orders = new ObservableCollection<Order>();

            if (this.worker.IsBusy)
            {
                return;
            }

            IsBusy = true;

            this.worker.RunWorkerAsync(this.SearchCondition);
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
                    this.SearchCondition = new OrderSearchCondition { FromOrderDate = DateTime.Today.AddDays(-7), ToOrderDate = DateTime.Today };
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
                    if (this.SelectedOrder != null && this.ManageOrderNotice != null)
                    {
                        this.ManageOrderNotice(this, new NotificationEventArgs<Order>("Manage", this.SelectedOrder));
                    }

                }, () => { return this.SelectedOrder != null; }));
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
                }, () => { return this.SelectedOrder != null; }));
            }
            private set { deleteCommand = value; }
        }

        private DelegateCommand paymentsCommand;
        public DelegateCommand PaymentsCommand
        {
            get { return paymentsCommand ?? (paymentsCommand = new DelegateCommand(() => { }, () => { return this.SelectedOrder != null; })); }
            private set { paymentsCommand = value; }
        }

        private DelegateCommand printCommand;
        public DelegateCommand PrintCommand
        {
            get
            {
                return printCommand ?? (printCommand = new DelegateCommand(() =>
                {

                }, () => { return this.SelectedOrder != null; }));
            }
            private set { printCommand = value; }
        }

        private DelegateCommand previewCommand;
        public DelegateCommand PreviewCommand
        {
            get
            {
                return previewCommand ?? (previewCommand = new DelegateCommand(() =>
                {

                }, () => { return this.SelectedOrder != null; }));
            }
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