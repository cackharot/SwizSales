using System;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Collections.ObjectModel;
using SimpleMvvmToolkit;
using SimpleMvvmToolkit.ModelExtensions;
using SwizSales.Core.ServiceContracts;
using SwizSales.Core.Services;
using SwizSales.Core.Model;
using SwizSales.Core.Library;
using System.ComponentModel;
using SwizSales.Library;
using System.Collections.Specialized;

namespace SwizSales.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvmprop</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// </summary>
    public class ManagePurchaseViewModel : ViewModelBase<ManagePurchaseViewModel>
    {
        #region Initialization and Cleanup

        private IStockService serviceAgent = new StockService();
        private ISupplierService supplierService = new SupplierService();
        private IProductService productService = new ProductService();
        private IMasterDataService<Category> categoryService = new MasterDataService<Category>();
        private IMasterDataService<TaxCategory> taxCategoryService = new MasterDataService<TaxCategory>();

        public ManagePurchaseViewModel(Purchase model)
        {
            if (model == null)
            {
                model = new Purchase();
            }

            this.Model = model;
            this.CurrentLineItem = new PurchaseDetail();
            this.Suppliers = new ObservableCollection<Supplier>(supplierService.Search(new SupplierSearchCondition()));
            this.Categories = new ObservableCollection<Category>(this.categoryService.Search());
            this.TaxCategories = new ObservableCollection<TaxCategory>(this.taxCategoryService.Search());
        }

        #endregion

        #region Notifications

        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;

        #endregion

        #region Properties

        private Purchase model;
        public Purchase Model
        {
            get { return model; }
            set
            {
                model = value;

                if (model != null)
                {
                    this.LineItems = new ObservableCollection<PurchaseDetail>(model.PurchaseDetails);
                }

                NotifyPropertyChanged(m => m.Model);
            }
        }

        private ObservableCollection<PurchaseDetail> lineItems;
        public ObservableCollection<PurchaseDetail> LineItems
        {
            get { return lineItems; }
            set
            {
                if (this.lineItems != null)
                {
                    this.lineItems.CollectionChanged -= lineItems_CollectionChanged;
                }

                lineItems = value;

                if (this.lineItems != null)
                {
                    this.lineItems.CollectionChanged += new NotifyCollectionChangedEventHandler(lineItems_CollectionChanged);
                }

                NotifyPropertyChanged(m => m.LineItems);
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

        private ObservableCollection<Category> _lstCategory;
        public ObservableCollection<Category> Categories
        {
            get { return _lstCategory; }
            set
            {
                _lstCategory = value;
                NotifyPropertyChanged(m => m.Categories);
            }
        }

        private ObservableCollection<TaxCategory> _lstTaxCategories;
        public ObservableCollection<TaxCategory> TaxCategories
        {
            get { return _lstTaxCategories; }
            set
            {
                _lstTaxCategories = value;
                NotifyPropertyChanged(m => m.TaxCategories);
            }
        }

        private PurchaseDetail currentLineItem;
        public PurchaseDetail CurrentLineItem
        {
            get { return currentLineItem; }
            set
            {
                if (currentLineItem != null)
                {
                    this.currentLineItem.PropertyChanged -= currentLineItem_PropertyChanged;
                }

                currentLineItem = value;

                if (currentLineItem != null)
                {
                    this.currentLineItem.PropertyChanged += new PropertyChangedEventHandler(currentLineItem_PropertyChanged);
                }

                NotifyPropertyChanged(m => m.CurrentLineItem);
            }
        }

        private PurchaseDetail selectedLineItem;
        public PurchaseDetail SelectedLineItem
        {
            get { return selectedLineItem; }
            set
            {
                selectedLineItem = value;
                NotifyPropertyChanged(m => m.SelectedLineItem);
                NotifyCommands();
            }
        }

        #endregion

        #region Methods

        void lineItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (PurchaseDetail item in e.NewItems)
                    this.Model.PurchaseDetails.Add(item);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (PurchaseDetail item in e.OldItems)
                {
                    this.Model.PurchaseDetails.Remove(item);

                    if (item.ProductId.HasValue)
                    {
                        ReduceStock(item.ProductId.Value, item.Quantity);
                    }
                }

                this.SelectedLineItem = this.LineItems.LastOrDefault();
            }

            this.Model.NotifyChanges();
        }

        void currentLineItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Quantity")
            {
                NotifyCommands();
            }
            else if (e.PropertyName == "Barcode")
            {
                LoadProduct(this.CurrentLineItem.Barcode);
            }
        }

        public void AddLineItem()
        {
            var item = this.CurrentLineItem;
            item.PurchaseId = this.Model.Id;

            var found = this.LineItems.Where(x => x.Barcode == item.Barcode && x.MRP == item.MRP && x.SellPrice == item.SellPrice).FirstOrDefault();

            if (found != null)
            {
                found.Quantity += item.Quantity;
            }
            else
            {
                item.Id = Guid.NewGuid();
                this.LineItems.Insert(0, item);
            }

            this.Model.NotifyChanges();

            UpdateProduct(item);

            this.CurrentLineItem = new PurchaseDetail();
        }

        private void UpdateProduct(PurchaseDetail item)
        {
            try
            {
                Product entity = GetProduct(item);

                if (entity.Id == Guid.Empty)
                    this.productService.Add(entity);
                else
                    this.productService.Update(entity);
            }
            catch (Exception ex)
            {
                NotifyError(string.Format("Error while updating product with barcode {0}", item.Barcode), ex);
            }
        }

        private Product GetProduct(PurchaseDetail item)
        {
            var prod = new Product();

            if (item.ProductId.HasValue)
            {
                var oldProduct = this.productService.GetProductById(item.ProductId.Value);

                if (oldProduct.MRP == item.MRP || oldProduct.SellPrice == item.SellPrice)
                {
                    prod.Id = item.ProductId.Value; // if mrp or sellprice is not changed then update the old product
                }
            }

            prod.Barcode = item.Barcode;
            prod.Name = item.Name;
            prod.MRP = item.MRP;
            prod.SellPrice = item.SellPrice;
            prod.BuyPrice = item.BuyPrice;
            prod.Discount = item.Discount;
            prod.Status = true;
            prod.Stock += item.Quantity;

            prod.CategoryId = item.CategoryId;
            prod.TaxCategoryId = item.TaxCategoryId;
            prod.SupplierId = this.Model.SupplierId;

            return prod;
        }

        private void ReduceStock(Guid productId, double quantity)
        {
            try
            {
                var entity = this.productService.GetProductById(productId);

                if (entity != null)
                {
                    entity.Stock -= quantity;
                    this.productService.Update(entity);
                }
            }
            catch (Exception ex)
            {
                NotifyError("Error while reducing stock", ex);
            }
        }

        private void LoadProduct(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
                return;

            try
            {
                var lstProducts = this.productService.GetProductByBarcode(barcode);

                if (lstProducts != null && lstProducts.Count > 0)
                {
                    var prod = lstProducts[0];

                    this.CurrentLineItem.Name = prod.Name;
                    this.CurrentLineItem.ProductId = prod.Id;
                    this.CurrentLineItem.MRP = prod.MRP;
                    this.CurrentLineItem.BuyPrice = prod.BuyPrice;
                    this.CurrentLineItem.SellPrice = prod.SellPrice;
                    this.CurrentLineItem.Discount = prod.Discount;
                    this.CurrentLineItem.PurchaseId = this.Model.Id;
                }
                else
                {
                    this.CurrentLineItem.Name = string.Empty;
                    this.CurrentLineItem.ProductId = null;
                    this.CurrentLineItem.MRP = 0;
                    this.CurrentLineItem.BuyPrice = 0;
                    this.CurrentLineItem.SellPrice = 0;
                    this.CurrentLineItem.Discount = 0;
                    this.CurrentLineItem.PurchaseId = Guid.Empty;
                }
            }
            catch (Exception ex)
            {
                NotifyError(string.Format("Error while searching for product with barcode {0}", barcode), ex);
            }
        }

        private void Save()
        {
            try
            {
                if (this.Model != null)
                {
                    this.Model.PurchaseDate = DateTime.Now;
                    this.Model.EmployeeId = ApplicationSettings.DefaultEmployeeId;
                    this.Model.SystemId = Environment.MachineName;
                    this.Model.Status = true;

                    if (this.Model.Id == Guid.Empty)
                    {
                        this.serviceAgent.Add(this.Model);
                    }
                    else
                    {
                        this.serviceAgent.Update(this.Model);
                    }

                    SendMessage(MessageTokens.GlobalNotification, new NotificationEventArgs(string.Format("Purchase #{0} Saved successfully!", this.Model.BillNo)));
                }
            }
            catch (Exception ex)
            {
                NotifyError("Error while saving purchase", ex);
            }
        }

        #endregion

        #region Completion Callbacks

        private DelegateCommand addCommand;
        public DelegateCommand AddCommand
        {
            get
            {
                return addCommand ?? (addCommand = new DelegateCommand(() =>
                {
                    AddLineItem();
                }, () => { return this.CurrentLineItem != null && this.CurrentLineItem.Quantity != 0; }));
            }
            private set { addCommand = value; }
        }

        private DelegateCommand resetCommand;
        public DelegateCommand ResetCommand
        {
            get
            {
                return resetCommand ?? (resetCommand = new DelegateCommand(() =>
                {
                    this.CurrentLineItem = new PurchaseDetail();
                }));
            }
            private set { resetCommand = value; }
        }

        private DelegateCommand deleteCommand;
        public DelegateCommand DeleteCommand
        {
            get
            {
                return deleteCommand ?? (deleteCommand = new DelegateCommand(() =>
                {
                    if (this.Model != null && this.SelectedLineItem != null && this.LineItems.Contains(this.SelectedLineItem))
                    {
                        this.LineItems.Remove(this.SelectedLineItem);
                    }
                }));
            }
            private set { deleteCommand = value; }
        }

        private DelegateCommand saveCommand;
        public DelegateCommand SaveCommand
        {
            get
            {
                return saveCommand ?? (saveCommand = new DelegateCommand(() =>
                {
                    Save();
                }));
            }
            private set { saveCommand = value; }
        }

        #endregion

        #region Helpers

        private void NotifyError(string message, Exception error)
        {
            LogService.Error(message, error);
            SendMessage(MessageTokens.GlobalNotification, new NotificationEventArgs(message));
            Notify(ErrorNotice, new NotificationEventArgs<Exception>(message, error));
        }

        private void NotifyCommands()
        {
            AddCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
        }

        #endregion
    }
}