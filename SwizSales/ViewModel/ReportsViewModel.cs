using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Collections.ObjectModel;

// Toolkit namespace
using SimpleMvvmToolkit;

// Toolkit extension methods
using SimpleMvvmToolkit.ModelExtensions;
using SwizSales.Core.ServiceContracts;
using System.ComponentModel;
using SwizSales.Core.Model;
using System.Globalization;
using SwizSales.Properties;

namespace SwizSales.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvmprop</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// </summary>
    public class ReportsViewModel : ViewModelBase<ReportsViewModel>
    {
        class ReportOutput
        {
            public List<KeyValuePair<string, double>> SalesChartValues { get; set; }
            public int TotalOrders { get; set; }
            public ICollection<Product> TopProducts { get; set; }
            public ICollection<Customer> TopCustomers { get; set; }
        }

        #region Initialization and Cleanup

        private IReportService reportService;
        private IOrderService orderService;
        private readonly BackgroundWorker worker = new BackgroundWorker();

        public ReportsViewModel(IReportService reportService, IOrderService orderService)
        {
            this.orderService = orderService;
            this.reportService = reportService;
            this.SalesChartTypes = new ObservableCollection<string>(Enum.GetNames(typeof(ReportType)));
            this.SelectedSalesChartType = ReportType.Daily.ToString();
            this.FromSearchDate = DateTime.Today.AddDays(-1);
            this.ToSearchDate = DateTime.Today;
            Init();
        }

        private void Init()
        {
            worker.DoWork += new DoWorkEventHandler(LoadSalesReport);
            worker.RunWorkerCompleted += (s, e) =>
            {
                IsBusy = false;

                if (e.Cancelled)
                    return;

                if (e.Error != null)
                {
                    NotifyError("Error while loading sales report data", e.Error);
                    return;
                }

                var res = e.Result as ReportOutput;

                if (res != null)
                {
                    this.SalesChartValues = res.SalesChartValues;
                    this.TotalAmount = res.SalesChartValues.Sum(x => x.Value);
                    this.TotalOrders = res.TotalOrders;

                    if (res.TopCustomers != null && res.TopCustomers.Count > 0)
                    {
                        foreach (var c in res.TopCustomers)
                        {
                            c.Points = Convert.ToInt32(c.TotalAmount / Settings.Default.CustomerPointsAmount);
                        }

                        this.TopCustomers = new ObservableCollection<Customer>(res.TopCustomers.OrderByDescending(x => x.Points));
                    }
                    else
                    {
                        this.TopCustomers = new ObservableCollection<Customer>();
                    }

                    if (res.TopProducts != null && res.TopProducts.Count > 0)
                    {
                        this.TopProducts = new ObservableCollection<Product>(res.TopProducts.OrderByDescending(x => x.Sold));
                    }
                    else
                    {
                        this.TopProducts = new ObservableCollection<Product>();
                    }
                }
            };
        }

        void LoadSalesReport(object sender, DoWorkEventArgs e)
        {
            var results = new ReportOutput();
            var dateAmtList = new Dictionary<DateTime, double>();
            string formatString = "dd/MM/yyyy";

            results.SalesChartValues = new List<KeyValuePair<string, double>>();
            results.TotalOrders = this.reportService.GetTotalOrders(this.FromSearchDate, this.ToSearchDate);
            results.TopCustomers = this.reportService.GetTopCustomers(10, this.FromSearchDate, this.ToSearchDate);
            results .TopProducts = this.reportService.GetTopProducts(10, this.FromSearchDate, this.ToSearchDate);

            ReportType repType = (ReportType)Enum.Parse(typeof(ReportType), e.Argument as string);

            switch (repType)
            {
                case ReportType.Daily:
                    this.Title = string.Format(CultureInfo.CurrentUICulture, "Today Sales Report");
                    var orders = this.orderService.Search(new OrderSearchCondition
                    {
                        FromOrderDate = DateTime.Today,
                        ToOrderDate = DateTime.Today
                    });

                    if (orders != null && orders.Count > 0)
                    {
                        var hourly = orders.OrderBy(x => x.OrderDate).GroupBy(x => x.OrderDate.Hour);

                        var hcnt = DateTime.Today;
                        foreach (var item in hourly)
                        {
                            var key = item.First().OrderDate;
                            var val = item.Sum(x => x.BillAmount);

                            while (hcnt.Hour < item.Key)
                            {
                                dateAmtList.Add(hcnt, 0.0);
                                hcnt = hcnt.AddHours(1);
                            }

                            dateAmtList.Add(hcnt, val);
                            hcnt = hcnt.AddHours(1);
                        }

                        while (hcnt < DateTime.Today.AddHours(24))
                        {
                            dateAmtList.Add(hcnt, 0.0);
                            hcnt = hcnt.AddHours(1);
                        }
                    }

                    formatString = "hh:mm tt";
                    break;
                case ReportType.Weekly:
                    this.Title = string.Format(CultureInfo.CurrentUICulture, "This Week Sales Report. {0} to {1}", DateTime.Today.AddDays(-7).ToString("dd/MM/yyyy"), DateTime.Today.ToString("dd/MM/yyyy"));
                    var weeklst = this.reportService.GetSalesReport(new OrderSearchCondition
                    {
                        FromOrderDate = DateTime.Today.AddDays(-7),
                        ToOrderDate = DateTime.Today
                    });

                    if (weeklst != null && weeklst.Count > 0)
                    {
                        var dcnt = DateTime.Today.AddDays(-7);
                        int cnt = 1;
                        foreach (var item in weeklst)
                        {
                            if (dcnt != item.Key)
                            {
                                while (dcnt < item.Key)
                                {
                                    dateAmtList.Add(dcnt, 0.0);
                                    dcnt = dcnt.AddDays(1);
                                }
                            }

                            dateAmtList.Add(item.Key, item.Value);
                            dcnt = dcnt.AddDays(1);
                            cnt++;
                        }

                        while (dcnt < DateTime.Today)
                        {
                            dateAmtList.Add(dcnt, 0.0);
                            dcnt = dcnt.AddDays(1);
                        }
                    }

                    formatString = "dd/MM/yyyy";
                    break;
                case ReportType.Monthly:
                    var frm = DateTime.Parse(DateTime.Today.Month + "/01/" + DateTime.Today.Year);
                    var to = DateTime.Parse(DateTime.Today.Month + "/" + DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month) + "/" + DateTime.Today.Year);

                    this.Title = string.Format(CultureInfo.CurrentUICulture, "This Month Sales Report. {0} to {1}", frm, to.ToString("dd/MM/yyyy"));
                    var daysAmt = this.reportService.GetSalesReport(new OrderSearchCondition
                    {
                        FromOrderDate = frm,
                        ToOrderDate = to
                    });

                    if (daysAmt != null && daysAmt.Count > 0)
                    {
                        var dcnt = 1;
                        foreach (var item in daysAmt)
                        {
                            var key = item.Key;
                            var value = item.Value;

                            if (dcnt != key.Day)
                            {
                                while (dcnt < key.Day)
                                {
                                    dateAmtList.Add(frm.AddDays(dcnt), 0.0);
                                    dcnt++;
                                }
                            }

                            dateAmtList.Add(key, value);
                            dcnt++;
                        }

                        while (dcnt < DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month))
                        {
                            dateAmtList.Add(frm.AddDays(dcnt), 0.0);
                            dcnt++;
                        }
                    }

                    formatString = "dd/MM";
                    break;
                case ReportType.Yearly:
                    this.Title = string.Format(CultureInfo.CurrentUICulture, "This Year Sales Report. {0} to {1}", DateTime.Parse("01/01/" + DateTime.Today.Year).ToString("dd/MM/yyyy"), DateTime.Today.ToString("dd/MM/yyyy"));
                    var lst = this.reportService.GetSalesReport(new OrderSearchCondition
                    {
                        FromOrderDate = DateTime.Parse("01/01/" + DateTime.Today.Year),
                        ToOrderDate = DateTime.Today
                    });

                    if (lst != null && lst.Count > 0)
                    {
                        var monthly = lst.GroupBy(x => x.Key.Month);

                        int mcnt = 1;
                        foreach (var m in monthly)
                        {
                            var key = m.First().Key;
                            var val = m.Sum(x => x.Value);

                            if (mcnt != key.Month)
                            {
                                while (mcnt < key.Month)
                                {
                                    dateAmtList.Add(DateTime.Parse(mcnt + "/01/" + key.Year), 0.0);
                                    mcnt++;
                                }
                            }

                            dateAmtList.Add(key, val);
                            mcnt++;
                        }

                        while (mcnt <= 12)
                        {
                            dateAmtList.Add(DateTime.Today.AddMonths(mcnt), 0.0);
                            mcnt++;
                        }
                    }

                    formatString = "MMMM";
                    break;
                case ReportType.SpecificPeriod:
                    this.Title = string.Format(CultureInfo.CurrentUICulture, "Sales Report from {0} to {1}", this.FromSearchDate.ToString("dd/MM/yyyy"), this.ToSearchDate.ToString("dd/MM/yyyy"));
                    lst = this.reportService.GetSalesReport(new OrderSearchCondition
                    {
                        FromOrderDate = this.FromSearchDate,
                        ToOrderDate = this.ToSearchDate
                    });

                    if ((this.ToSearchDate.Year - this.FromSearchDate.Year) > 1)
                    {
                        var monthly = lst.GroupBy(x => x.Key.Month);

                        foreach (var m in monthly)
                        {
                            var key = m.First().Key;
                            var val = m.Sum(x => x.Value);

                            dateAmtList.Add(key, val);
                        }

                        formatString = "yyyy";
                    }
                    else if ((this.ToSearchDate.Month - this.FromSearchDate.Month) > 1)
                    {
                        var monthly = lst.GroupBy(x => x.Key.Month);

                        foreach (var m in monthly)
                        {
                            var key = m.First().Key;
                            var val = m.Sum(x => x.Value);

                            dateAmtList.Add(key, val);
                        }

                        formatString = "MMMM";
                    }
                    else if ((this.ToSearchDate.Day - this.FromSearchDate.Day) == 0
                        && (this.ToSearchDate.Month == this.FromSearchDate.Month)
                        && (this.ToSearchDate.Year == this.FromSearchDate.Year))
                    {
                        orders = this.orderService.Search(new OrderSearchCondition
                        {
                            FromOrderDate = DateTime.Today,
                            ToOrderDate = DateTime.Today
                        });

                        if (orders != null && orders.Count > 0)
                        {
                            foreach (var item in orders)
                            {
                                dateAmtList.Add(item.OrderDate, item.BillAmount);
                            }
                        }

                        formatString = "hh:mm tt";
                    }
                    else
                    {
                        dateAmtList = lst;
                        formatString = "dd/MM/yyyy";
                    }

                    break;
            }

            if (dateAmtList != null && dateAmtList.Count > 0)
            {
                foreach (var item in dateAmtList)
                {
                    var key = item.Key.ToString(formatString);
                    results.SalesChartValues.Add(new KeyValuePair<string, double>(key, item.Value));
                }
            }

            e.Result = results;
        }

        void DoLoad()
        {
            if (string.IsNullOrEmpty(this.SelectedSalesChartType))
            {
                return;
            }

            this.SalesChartValues = new List<KeyValuePair<string, double>>();
            this.TotalAmount = 0;

            if (this.worker.IsBusy)
            {
                return;
            }

            IsBusy = true;

            this.worker.RunWorkerAsync(this.SelectedSalesChartType);
        }

        void SetSearchDates(ReportType repType)
        {
            switch (repType)
            {
                case ReportType.Daily:
                    this.FromSearchDate = DateTime.Today;
                    this.ToSearchDate = DateTime.Today;
                    break;
                case ReportType.Weekly:
                    this.FromSearchDate = DateTime.Today.AddDays(-7);
                    this.ToSearchDate = DateTime.Now;
                    break;
                case ReportType.Monthly:
                    this.FromSearchDate = DateTime.Parse("01/" + DateTime.Today.Month + "/" + DateTime.Today.Year);
                    this.ToSearchDate = DateTime.Parse(DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month) + "/" + DateTime.Today.Month + "/" + DateTime.Today.Year);
                    break;
                case ReportType.Yearly:
                    this.FromSearchDate = DateTime.Parse("01/01/" + DateTime.Today.Year);
                    this.ToSearchDate = DateTime.Today;
                    break;
                case ReportType.SpecificPeriod:
                    break;
            }
        }

        #endregion

        #region Notifications

        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;

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
            }
        }

        private string title;
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                NotifyPropertyChanged(m => m.Title);
            }
        }

        private DateTime fromSearchDate;
        public DateTime FromSearchDate
        {
            get { return fromSearchDate; }
            set
            {
                fromSearchDate = value;
                NotifyPropertyChanged(m => m.FromSearchDate);
            }
        }

        private DateTime toSearchDate;
        public DateTime ToSearchDate
        {
            get { return toSearchDate; }
            set
            {
                toSearchDate = value;
                NotifyPropertyChanged(m => m.ToSearchDate);
            }
        }

        private string _selectedSalesChartType;
        public string SelectedSalesChartType
        {
            get { return _selectedSalesChartType; }
            set
            {
                _selectedSalesChartType = value;
                NotifyPropertyChanged(m => m.SelectedSalesChartType);
                SetSearchDates((ReportType)Enum.Parse(typeof(ReportType), value));
            }
        }

        private ObservableCollection<string> _salesChartTypes;
        public ObservableCollection<string> SalesChartTypes
        {
            get { return _salesChartTypes; }
            set
            {
                _salesChartTypes = value;
                NotifyPropertyChanged(m => m.SalesChartTypes);
            }
        }

        private List<KeyValuePair<string, double>> _salesChartValues;
        public List<KeyValuePair<string, double>> SalesChartValues
        {
            get { return _salesChartValues; }
            set
            {
                _salesChartValues = value;
                NotifyPropertyChanged(m => m.SalesChartValues);
            }
        }

        private double _grandTotal;
        public double TotalAmount
        {
            get { return _grandTotal; }
            set
            {
                _grandTotal = value;
                NotifyPropertyChanged(m => m.TotalAmount);
            }
        }

        private int totalOrders;
        public int TotalOrders
        {
            get { return totalOrders; }
            set
            {
                totalOrders = value;
                NotifyPropertyChanged(m => m.TotalOrders);
            }
        }

        private ObservableCollection<Product> topProducts;
        public ObservableCollection<Product> TopProducts
        {
            get { return topProducts; }
            set
            {
                topProducts = value;
                NotifyPropertyChanged(m => m.TopProducts);
            }
        }

        private ObservableCollection<Customer> topCustomers;
        public ObservableCollection<Customer> TopCustomers
        {
            get { return topCustomers; }
            set
            {
                topCustomers = value;
                NotifyPropertyChanged(m => m.TopCustomers);
            }
        }

        #endregion

        #region Methods


        #endregion

        #region Completion Callbacks

        private DelegateCommand loadSalesRepCommand;
        public DelegateCommand LoadSalesReportCommand
        {
            get { return loadSalesRepCommand ?? (loadSalesRepCommand = new DelegateCommand(DoLoad)); }
            private set { loadSalesRepCommand = value; }
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