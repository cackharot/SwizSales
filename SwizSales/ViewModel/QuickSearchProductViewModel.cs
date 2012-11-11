using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleMvvmToolkit;
using SimpleMvvmToolkit.ModelExtensions;
using System.Globalization;
using SwizSales.Core.Library;
using System.Collections.ObjectModel;
using SwizSales.Core.Model;
using SwizSales.Core.ServiceContracts;
using SwizSales.Core.Services;
using System.ComponentModel;
using System.Windows;
using System.Threading;

namespace SwizSales.ViewModel
{
    public class QuickSearchProductViewModel : ViewModelBase<QuickSearchProductViewModel>
    {
        private readonly BackgroundWorker worker = new BackgroundWorker();
        private IProductService productService = new ProductService();

        public QuickSearchProductViewModel()
            : this(new List<Product>())
        {
        }

        public QuickSearchProductViewModel(IEnumerable<Product> lstProducts)
        {
            this.Products = new ObservableCollection<Product>(lstProducts ?? new List<Product>());
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
                    //NotifyError("Error while loading orders", e.Error);
                    return;
                }

                var res = e.Result as Collection<Product>;

                if (res != null)
                    this.Products = new ObservableCollection<Product>(res);
            };
        }

        public void Search(object sender, DoWorkEventArgs e)
        {
            var searchValue = e.Argument as string;

            if (!string.IsNullOrEmpty(searchValue))
            {
                e.Result = this.productService.Search(new ProductSearchCondition
                {
                    Barcode = searchValue,
                    Name = searchValue,
                    PageNo = 1,
                    PageSize = 10
                });

                Thread.Sleep(500);
            }
        }

        public void DoSearch()
        {
            this.SelectedProduct = null;

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
                this.Products = new ObservableCollection<Product>();
            }
        }

        public event EventHandler<NotificationEventArgs> CloseNotice;
        
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
            get { return IsBusy ? Visibility.Hidden: Visibility.Visible; }
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

        private ObservableCollection<Product> _products;
        public ObservableCollection<Product> Products
        {
            get { return _products; }
            set
            {
                _products = value;
                NotifyPropertyChanged(m => m.Products);
            }
        }

        private Product _selectedProduct;
        public Product SelectedProduct
        {
            get { return _selectedProduct; }
            set
            {
                _selectedProduct = value;
                NotifyPropertyChanged(m => m.SelectedProduct);
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

        private DelegateCommand searchCommand;
        public DelegateCommand SearchCommand
        {
            get { return searchCommand ?? (searchCommand = new DelegateCommand(() => { DoSearch(); })); }
            private set { searchCommand = value; }
        }
    }
}
