using System;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Collections.ObjectModel;

// Toolkit namespace
using SimpleMvvmToolkit;

// Toolkit extension methods
using SimpleMvvmToolkit.ModelExtensions;
using SwizSales.Core.Library;
using SwizSales.Core.Model;
using SwizSales.Core.ServiceContracts;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Xml;
using System.Windows.Documents;
using System.Windows.Markup;
using System.IO;
using System.Windows.Threading;
using System.Windows.Xps.Packaging;
using SwizSales.Library;
using SwizSales.Core.Services;
using SwizSales.Properties;
using System.Collections.Generic;

namespace SwizSales.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvmprop</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// </summary>
    public class SalesViewModel : ViewModelBase<SalesViewModel>
    {
        #region Initialization and Cleanup

        private IOrderService serviceAgent;
        private IProductService productService;
        private IReportService reportService;
        private ISettingsService settingsService;

        public SalesViewModel() { }

        public SalesViewModel(IOrderService serviceAgent, IProductService productService)
        {
            this.serviceAgent = serviceAgent;
            this.productService = productService;
            this.reportService = new ReportService();
            this.settingsService = new SettingsService();

            Init();
        }

        private readonly BackgroundWorker worker = new BackgroundWorker();
        private void Init()
        {
            worker.DoWork += new DoWorkEventHandler(LoadOrders);
            worker.RunWorkerCompleted += (s, e) =>
            {
                IsBusy = false;

                if (e.Cancelled)
                    return;

                if (e.Error != null)
                {
                    NotifyError("Error while loading orders", e.Error);
                    return;
                }

                var res = e.Result as Collection<Order>;

                if (res != null)
                    this.TodayOrders = new ObservableCollection<Order>(res);
            };

            LoadTodayOrders();
            LoadTemplates();
        }

        void LoadOrders(object sender, DoWorkEventArgs e)
        {
            var condition = e.Argument as OrderSearchCondition;

            if (condition != null)
            {
                var lstOrders = this.serviceAgent.Search(condition);
                e.Result = lstOrders;
                Thread.Sleep(500);
            }
        }

        private void LoadTodayOrders()
        {
            this.Model = null;
            this.SelectedOrder = null;
            this.TodayOrders = new ObservableCollection<Order>();

            if (this.worker.IsBusy)
                this.worker.CancelAsync();

            this.worker.RunWorkerAsync(new OrderSearchCondition
            {
                FromOrderDate = DateTime.Now.Date,
                ToOrderDate = DateTime.Now.Date,
                PageSize = 100
            });

            IsBusy = true;
        }

        #endregion

        #region Notifications

        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;
        public event EventHandler<NotificationEventArgs<Order, Customer>> ChangeCustomerNotice;
        public event EventHandler<NotificationEventArgs<Order, bool>> CheckoutNotice;
        public event EventHandler<NotificationEventArgs<Order>> PreviewNotice;

        public event EventHandler<NotificationEventArgs<List<Product>, Product>> SearchProductNotice;

        public event EventHandler SelectBarcode;

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

        private Order _model;
        public Order Model
        {
            get { return _model; }
            set
            {
                _model = value;

                if (_model != null)
                {
                    this.LineItems = new ObservableCollection<OrderDetail>(_model.OrderDetails);

                    if (_model.Customer.Id != Settings.Default.DefaultCustomerId)
                    {
                        var totalAmount = this.reportService.GetCusomerTotalAmount(_model.Customer.Id);
                        _model.Customer.Points = Convert.ToInt32(totalAmount / Settings.Default.CustomerPointsAmount);
                    }
                }

                NotifyPropertyChanged(m => m.Model);
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
            }
        }

        private OrderDetail _selectedLineItem;
        public OrderDetail SelectedLineItem
        {
            get { return _selectedLineItem; }
            set
            {
                if (_selectedLineItem != null)
                {
                    _selectedLineItem.PropertyChanged -= _selectedLineItem_PropertyChanged;
                }

                _selectedLineItem = value;

                if (_selectedLineItem != null)
                {
                    _selectedLineItem.PropertyChanged += new PropertyChangedEventHandler(_selectedLineItem_PropertyChanged);
                }

                NotifyPropertyChanged(m => m.SelectedLineItem);
            }
        }

        void _selectedLineItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.Model.NotifyChanges();
        }

        private ObservableCollection<Order> _todayOrders;
        public ObservableCollection<Order> TodayOrders
        {
            get { return _todayOrders; }
            set
            {
                _todayOrders = value;
                NotifyPropertyChanged(m => m.TodayOrders);
            }
        }

        private ObservableCollection<OrderDetail> _lineItems;
        public ObservableCollection<OrderDetail> LineItems
        {
            get { return _lineItems; }
            set
            {
                if (_lineItems != null)
                {
                    _lineItems.CollectionChanged -= _lineItems_CollectionChanged;
                }

                _lineItems = value;

                if (_lineItems != null)
                {
                    _lineItems.CollectionChanged += new NotifyCollectionChangedEventHandler(_lineItems_CollectionChanged);
                }

                NotifyPropertyChanged(m => m.LineItems);
            }
        }

        void _lineItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (OrderDetail item in e.NewItems)
                    this.Model.OrderDetails.Add(item);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (OrderDetail item in e.OldItems)
                    this.Model.OrderDetails.Remove(item);

                this.SelectedLineItem = this.LineItems.LastOrDefault();
            }

            this.Model.NotifyChanges();

            NotifyCommands();
        }

        private string _barcode;
        public string Barcode
        {
            get { return _barcode; }
            set
            {
                _barcode = value;
                NotifyPropertyChanged(m => m.Barcode);
                AddLineItemCommand.RaiseCanExecuteChanged();
            }
        }

        private string _itemName;
        public string ItemName
        {
            get { return _itemName; }
            set
            {
                _itemName = value;
                NotifyPropertyChanged(m => m.ItemName);
                AddLineItemCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Methods

        public void Load(OrderSearchCondition condition)
        {
            try
            {
                var lstOrders = this.serviceAgent.Search(condition);

                if (lstOrders != null && lstOrders.Count > 0)
                {
                    this.TodayOrders = new ObservableCollection<Order>(lstOrders);
                }
            }
            catch (Exception ex)
            {
                NotifyError("Error while loading orders", ex);
            }
        }

        private void Delete(Order entity)
        {
            try
            {
                this.TodayOrders.Remove(entity);
                this.serviceAgent.Delete(entity.Id);
            }
            catch (Exception ex)
            {
                NotifyError("Error while deleting order", ex);
            }
        }

        private void Save()
        {
            try
            {
                this.Model.OrderDate = DateTime.Now;
                this.serviceAgent.Update(this.Model);

                var newModel = this.serviceAgent.GetOrderById(this.Model.Id);

                var idx = this.TodayOrders.IndexOf(this.Model);
                if (idx > -1)
                {
                    this.Model = this.TodayOrders[idx] = newModel;
                }
            }
            catch (Exception ex)
            {
                NotifyError("Error while saving order", ex);
            }
        }

        private void AddLineItem()
        {
            if (string.IsNullOrEmpty(this.Barcode))
                return;

            string value = this.Barcode;
            double mrp;

            var prods = this.productService.GetProductByBarcode(value);

            if (prods != null && prods.Count > 0)
            {
                Product item = prods[0]; // select the first item

                if (prods.Count > 1 && SearchProductNotice != null)
                {
                    // show product select dialog box and get the selected item
                    this.SearchProductNotice(this, new NotificationEventArgs<List<Product>, Product>("SelectProduct", prods, (x) =>
                    {
                        if (x != null)
                        {
                            item = x;
                        }
                        else
                        {
                            item = null;
                        }
                    }));
                }

                if (item != null)
                {
                    AddItem(new OrderDetail
                    {
                        Id = Guid.NewGuid(),
                        OrderId = this.Model.Id,
                        Barcode = item.Barcode,
                        ProductId = item.Id,
                        ItemName = item.Name,
                        MRP = item.MRP,
                        Discount = item.Discount,
                        Price = item.SellPrice,
                        Quantity = 1
                    });
                }
            }
            else if (Double.TryParse(value, out mrp) && mrp <= 1000.00)
            {
                AddItem(new OrderDetail
                {
                    Id = Guid.NewGuid(),
                    OrderId = this.Model.Id,
                    ProductId = null,
                    Barcode = mrp.ToString(),
                    MRP = mrp,
                    Price = mrp,
                    Discount = 0,
                    Quantity = 1
                });
            }
        }

        private void AddItem(OrderDetail orderDetail)
        {
            OrderDetail found = null;

            if (orderDetail.ProductId == null)
            {
                found = this.LineItems.Where(x => x.ProductId == null && string.IsNullOrEmpty(x.ItemName) && x.MRP == orderDetail.MRP).FirstOrDefault();
            }
            else
            {
                found = this.LineItems.Where(x => x.ProductId == orderDetail.ProductId).FirstOrDefault();
            }

            if (found != null)
            {
                found.Quantity++;
                this.Model.NotifyChanges();
            }
            else
            {
                this.LineItems.Insert(0, orderDetail);
            }
        }

        private void NewOrder()
        {
            try
            {
                this.Model = this.serviceAgent.NewOrder(Settings.Default.DefaultCustomerId, Settings.Default.DefaultEmployeeId);
                this.TodayOrders.Insert(0, this.Model);
                this.SelectedOrder = this.Model;
            }
            catch (Exception ex)
            {
                NotifyError("Error while creating new order", ex);
            }
        }

        private void CancelOrder()
        {
            if (this.Model != null)
            {
                this.LineItems.Clear();
                this.Model.OrderDetails.Clear();
                this.Model.Payments.Clear();
                this.Model.NotifyChanges();
            }
        }

        private void CheckoutOrder()
        {
            if (this.Model == null)
                return;

            this.Model.BillAmount = this.Model.TotalAmount;

            if (this.CheckoutNotice != null)
            {
                this.Model.CurrentPaidAmount = this.Model.BalanceAmount;
                this.CheckoutNotice(this, new NotificationEventArgs<Order, bool>("CheckOut", this.Model, (x) =>
                {
                    if (x)
                    {
                        if (this.Model.CurrentPaidAmount != 0)
                        {
                            this.Model.Payments.Add(new Payment
                            {
                                OrderId = this.Model.Id,
                                PaidAmount = this.Model.CurrentPaidAmount,
                                PaymentDate = DateTime.Now,
                                EmployeeId = Settings.Default.DefaultEmployeeId,
                                SystemId = this.Model.SystemId,
                                Type = (short)PaymentType.Cash
                            });
                        }

                        this.Model.NotifyChanges();
                        Save();
                    }
                    else
                    {
                        LogService.Info("Payment cancelled! [{0}]", this.Model.BillNo);
                    }
                }));
            }
        }

        private void PrintOrder(bool printToPrinter)
        {
            if (this.Model == null || this.Model.OrderDetails.Count == 0)
            {
                return;
            }

            //if (this.Model.Customer.Id != ApplicationSettings.DefaultCustomerId)
            //{
            //    var totalAmount = this.reportService.GetCusomerTotalAmount(this.Model.Customer.Id);
            //    this.Model.Customer.Points = (int)(totalAmount / Settings.Default.CustomerPointsAmount);
            //}

            if (printToPrinter)
            {
                //SendMessage<Guid>(MessageTokens.PrintOrder, new NotificationEventArgs<Guid>("Print Order", this.Model.Id));
                PrintOrderToPrinter(this.Model);
            }
            else
            {
                if (PreviewNotice != null)
                {
                    PreviewNotice(this, new NotificationEventArgs<Order>(this.SelectedTemplate.Value, this.Model));
                }
            }
        }

        private void PrintOrderToPrinter(Order order)
        {
            if (this.SelectedTemplate == null)
            {
                NotifyError("Print template is not selected!", null);
            }

            if (string.IsNullOrEmpty(this.SelectedTemplate.Value))
            {
                NotifyError("Print template does not have valid text!", null);
            }

            try
            {
                var flowDocument = PrintHelper.GetPrintDocument(this.SelectedTemplate.Value, this.Model);
                var xps = PrintHelper.GetXpsDocument(flowDocument);
                PrintHelper.PrintXpsToPrinter(xps, Properties.Settings.Default.TicketPrinter);
            }
            catch (Exception ex)
            {
                NotifyError("Error printing order", ex);
            }
        }

        #endregion

        #region Commamds

        private DelegateCommand refreshCommand;
        public DelegateCommand RefreshCommand
        {
            get
            {
                if (refreshCommand == null)
                {
                    refreshCommand = new DelegateCommand(() =>
                    {
                        LoadTodayOrders();
                        LoadTemplates();
                        this.Model = null;
                        this.SelectedOrder = null;
                        this.LineItems = null;
                        NotifySelectBarcode();
                    });
                }

                return refreshCommand;
            }
        }

        private DelegateCommand newCommand;
        public DelegateCommand NewCommand
        {
            get
            {
                if (newCommand == null)
                {
                    newCommand = new DelegateCommand(() =>
                    {
                        NewOrder();
                        NotifySelectBarcode();
                    }, () =>
                    {
                        return true;
                    });
                }

                return newCommand;
            }
        }

        private DelegateCommand cancelCommand;
        public DelegateCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new DelegateCommand(() =>
                    {
                        CancelOrder();
                        NotifySelectBarcode();
                    }, () =>
                    {
                        return this.Model != null && this.Model.OrderDetails.Count > 0;
                    });
                }

                return cancelCommand;
            }
        }

        private DelegateCommand checkoutCommand;
        public DelegateCommand CheckoutCommand
        {
            get
            {
                if (checkoutCommand == null)
                {
                    checkoutCommand = new DelegateCommand(() =>
                    {
                        CheckoutOrder();
                        NotifySelectBarcode();
                    }, () =>
                    {
                        return this.Model != null && this.Model.OrderDetails.Count > 0;
                    });
                }

                return checkoutCommand;
            }
        }

        private DelegateCommand printCommand;
        public DelegateCommand PrintCommand
        {
            get
            {
                return printCommand ?? (printCommand = new DelegateCommand(() =>
                {
                    PrintOrder(true);
                    NotifySelectBarcode();
                }, () =>
                {
                    return this.Model != null && this.Model.OrderDetails.Count > 0;
                }));
            }
        }

        private DelegateCommand previewCommand;
        public DelegateCommand PreviewCommand
        {
            get
            {
                return previewCommand ?? (previewCommand = new DelegateCommand(() =>
                    {
                        PrintOrder(false);
                        NotifySelectBarcode();
                    }, () =>
                    {
                        return this.Model != null && this.Model.OrderDetails.Count > 0;
                    }));
            }
        }

        private DelegateCommand changeCustomerCommand;
        public DelegateCommand ChangeCustomerCommand
        {
            get
            {
                return changeCustomerCommand ?? (changeCustomerCommand = new DelegateCommand(() =>
                {
                    if (this.Model != null && this.ChangeCustomerNotice != null)
                    {
                        this.ChangeCustomerNotice(this, new NotificationEventArgs<Order, Customer>("ChangeCustomer", this.Model, (cus) =>
                        {
                            if (cus != null)
                            {
                                this.Model.Customer = cus;

                                if (cus.Id != Settings.Default.DefaultCustomerId && cus.Points == 0)
                                {
                                    var totalAmount = this.reportService.GetCusomerTotalAmount(cus.Id);
                                    this.Model.Customer.Points = Convert.ToInt32(totalAmount / Settings.Default.CustomerPointsAmount);
                                }

                                this.Model.NotifyChanges();
                                this.serviceAgent.UpdateOrderCustomer(this.Model);
                            }

                            NotifySelectBarcode();
                        }));
                    }
                }));
            }
        }

        private DelegateCommand addLineItemCommand;
        public DelegateCommand AddLineItemCommand
        {
            get
            {
                return addLineItemCommand ?? (addLineItemCommand = new DelegateCommand(() =>
                {
                    AddLineItem();
                    NotifySelectBarcode();
                }, () =>
                {
                    return this.Model != null && !string.IsNullOrEmpty(this.Barcode);
                }));
            }
        }

        private DelegateCommand editCommand;
        public DelegateCommand EditCommand
        {
            get
            {
                return editCommand ?? (editCommand = new DelegateCommand(() =>
                {
                    if (this.SelectedOrder != null)
                    {
                        //this.Model = this.serviceAgent.GetOrderById(this.SelectedOrder.Id);
                        this.Model = this.SelectedOrder;
                        NotifyCommands();
                    }
                    NotifySelectBarcode();
                }));
            }
        }

        private DelegateCommand deleteLineItemCommand;
        public DelegateCommand DeleteLineItemCommand
        {
            get
            {
                return deleteLineItemCommand ?? (deleteLineItemCommand = new DelegateCommand(() =>
                {
                    if (this.Model != null && this.SelectedLineItem != null
                        && this.LineItems.Contains(this.SelectedLineItem))
                    {
                        this.LineItems.Remove(this.SelectedLineItem);
                    }
                    NotifySelectBarcode();
                }));
            }
        }

        private DelegateCommand selectBarcodeCommand;
        public DelegateCommand SelectBarcodeCommand
        {
            get
            {
                return selectBarcodeCommand ?? (selectBarcodeCommand = new DelegateCommand(() =>
                {
                    NotifySelectBarcode();
                }));
            }
            private set { selectBarcodeCommand = value; }
        }

        private DelegateCommand searchProductCommand;
        public DelegateCommand SearchProductCommand
        {
            get
            {
                return searchProductCommand ?? (searchProductCommand = new DelegateCommand(() =>
                {
                    if (this.SearchProductNotice != null)
                    {
                        this.SearchProductNotice(this, new NotificationEventArgs<List<Product>, Product>("Search", null, (item) =>
                        {
                            if (item != null)
                            {
                                AddItem(new OrderDetail
                                {
                                    Id = Guid.NewGuid(),
                                    OrderId = this.Model.Id,
                                    Barcode = item.Barcode,
                                    ProductId = item.Id,
                                    ItemName = item.Name,
                                    MRP = item.MRP,
                                    Discount = item.Discount,
                                    Price = item.SellPrice,
                                    Quantity = 1
                                });
                            }
                        }));
                    }
                }, () =>
                {
                    return this.Model != null;
                }));
            }
            private set { searchProductCommand = value; }
        }

        #endregion

        #region Helpers

        private void NotifyError(string message, Exception error)
        {
            SendMessage(MessageTokens.GlobalNotification, new NotificationEventArgs(message));
            LogService.Error(message, error);
            Notify(ErrorNotice, new NotificationEventArgs<Exception>(message, error));
        }

        public void NotifySelectBarcode()
        {
            if (this.SelectBarcode != null)
                this.SelectBarcode(this, new EventArgs());
        }

        private void NotifyCommands()
        {
            CancelCommand.RaiseCanExecuteChanged();
            CheckoutCommand.RaiseCanExecuteChanged();
            PrintCommand.RaiseCanExecuteChanged();
            PreviewCommand.RaiseCanExecuteChanged();
            SearchProductCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Print Templates Settings

        private ObservableCollection<Setting> _templates;
        public ObservableCollection<Setting> Templates
        {
            get { return _templates; }
            set
            {
                _templates = value;
                NotifyPropertyChanged(m => m.Templates);
            }
        }

        private Setting selectedTemplate;
        public Setting SelectedTemplate
        {
            get { return selectedTemplate; }
            set
            {
                selectedTemplate = value;
                NotifyPropertyChanged(m => m.SelectedTemplate);
            }
        }

        private void LoadTemplates()
        {
            this.Templates = new ObservableCollection<Setting>(this.settingsService.GetSettingsByCategory("Templates"));

            if (this.Templates != null && this.Templates.Count > 0)
            {
                foreach (var item in this.Templates)
                {
                    if (item.Name.EndsWith(Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName))
                    {
                        this.SelectedTemplate = item;
                        break;
                    }
                }
            }
        }

        #endregion
    }
}