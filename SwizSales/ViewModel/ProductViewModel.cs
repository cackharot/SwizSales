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
using System.Windows.Threading;
using System.ComponentModel;
using SwizSales.Core.Library;
using SwizSales.Helpers;
using SwizSales.Library;

namespace SwizSales.ViewModel
{
    public class ProductViewModel : ViewModelBase<ProductViewModel>
    {
        #region Initialization and Cleanup

        internal IProductService serviceAgent;
        private readonly BackgroundWorker worker = new BackgroundWorker();

        public ProductViewModel() { }

        public ProductViewModel(IProductService serviceAgent)
        {
            this.serviceAgent = serviceAgent;
            this.SearchCondition = new ProductSearchCondition();
            this.PageSizes = new ObservableCollection<int>(new int[] { 10, 25, 50, 100, 150, 250, 500, 1000, 0 });
            Init();
        }

        #endregion

        #region Notifications

        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;

        public event EventHandler<NotificationEventArgs<Product, bool>> DeleteProductNotice;

        public event EventHandler<NotificationEventArgs<Product>> ProductDetailsNotice;

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

        private ObservableCollection<Product> _productCollection;
        public ObservableCollection<Product> ProductCollection
        {
            get { return _productCollection; }
            set
            {
                _productCollection = value;
                NotifyPropertyChanged(m => m.ProductCollection);
            }
        }

        private Product _selectedProduct;
        public Product SelectedProduct
        {
            get { return _selectedProduct; }
            set
            {
                if (_selectedProduct != null)
                {
                    _selectedProduct.PropertyChanged -= new PropertyChangedEventHandler(_selectedProduct_PropertyChanged);
                    _selectedProduct.CancelEdit();
                }

                _selectedProduct = value;

                if (_selectedProduct != null)
                {
                    _selectedProduct.PropertyChanged += new PropertyChangedEventHandler(_selectedProduct_PropertyChanged);
                    _selectedProduct.BeginEdit();
                    RaiseCmds();
                }

                NotifyPropertyChanged(m => m.SelectedProduct);
            }
        }

        void _selectedProduct_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "HasChanges")
            {
                RaiseCmds();
            }
        }

        private void RaiseCmds()
        {
            this.SaveCommand.RaiseCanExecuteChanged();
            this.ResetCommand.RaiseCanExecuteChanged();
        }

        private ProductSearchCondition _searchCondition;
        public ProductSearchCondition SearchCondition
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
                    NotifyError("Error while loading Products", e.Error);
                    return;
                }

                var res = e.Result as Collection<Product>;

                if (res != null)
                {
                    this.ProductCollection = new ObservableCollection<Product>(res);
                }
            };

            DoSearch();
        }

        void Search(object sender, DoWorkEventArgs e)
        {
            var condition = e.Argument as ProductSearchCondition;

            if (condition != null)
            {
                var cuslst = this.serviceAgent.Search(condition);
                e.Result = cuslst;
                Thread.Sleep(500);
            }
        }

        void DoSearch()
        {
            this.SelectedProduct = null;
            this.ProductCollection = new ObservableCollection<Product>();

            if (this.worker.IsBusy)
            {
                return;
            }

            IsBusy = true;

            this.worker.RunWorkerAsync(this.SearchCondition);
        }

        public void Delete(Product entity)
        {
            try
            {
                this.ProductCollection.Remove(entity);
                this.serviceAgent.Delete(entity.Id);
            }
            catch (Exception ex)
            {
                NotifyError("Error while deleting product", ex);
            }
        }

        public void Save(Product entity)
        {
            try
            {
                if (entity.Id == Guid.Empty)
                {
                    entity.Id = this.serviceAgent.Add(entity);
                }
                else
                {
                    this.serviceAgent.Update(entity);
                }
            }
            catch (Exception ex)
            {
                NotifyError("Error while saving product", ex);
            }
        }

        #endregion

        #region Completion Callbacks

        public DelegateCommand<Product> _editCommand;
        public DelegateCommand<Product> EditCommand
        {
            get
            {
                if (_editCommand == null)
                {
                    _editCommand = new DelegateCommand<Product>((entity) =>
                    {
                        if (entity == null)
                            return;
                        Notify(ProductDetailsNotice, new NotificationEventArgs<Product>("Edit", entity));
                    });
                }

                return _editCommand;
            }
        }

        public DelegateCommand<Product> _deleteCommand;
        public DelegateCommand<Product> DeleteCommand
        {
            get
            {
                if (_deleteCommand == null)
                {
                    _deleteCommand = new DelegateCommand<Product>((entity) =>
                    {
                        if (entity == null)
                            return;
                        Notify(DeleteProductNotice, new NotificationEventArgs<Product, bool>("Delete", entity, (canDelete) =>
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

        private DelegateCommand _resetSearchCommand;
        public DelegateCommand ResetSearchCommand
        {
            get
            {
                return _resetSearchCommand ?? (_resetSearchCommand = new DelegateCommand(() =>
                {
                    this.SearchCondition = new ProductSearchCondition();
                    DoSearch();
                }));
            }
            private set { _resetSearchCommand = value; }
        }

        private DelegateCommand _addCommand;
        public DelegateCommand AddCommand
        {
            get
            {
                return _addCommand ?? (_addCommand = new DelegateCommand(() =>
                {
                    var entity = new Product();
                    Notify(ProductDetailsNotice, new NotificationEventArgs<Product>("Add", entity));
                }, () =>
                {
                    return true;
                }));
            }
        }

        public DelegateCommand _resetCommand;
        public DelegateCommand ResetCommand
        {
            get
            {
                if (_resetCommand == null)
                {
                    _resetCommand = new DelegateCommand(() =>
                    {
                        var entity = this.SelectedProduct;
                        if (entity == null)
                            return;
                        entity.Reset();
                    }, () =>
                    {
                        var entity = this.SelectedProduct;
                        return entity != null && entity.HasChanges;
                    });
                }

                return _resetCommand;
            }
        }

        public DelegateCommand _saveCommand;
        public DelegateCommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new DelegateCommand(() =>
                    {
                        var entity = this.SelectedProduct;
                        if (entity == null)
                            return;
                        entity.EndEdit();
                        Save(entity);

                    }, () =>
                    {
                        var entity = this.SelectedProduct;
                        return entity != null && entity.HasChanges && ValidationHelper.Validate(entity) == null;
                    });
                }

                return _saveCommand;
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
