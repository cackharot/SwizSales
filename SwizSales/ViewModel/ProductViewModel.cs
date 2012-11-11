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

        public ProductViewModel() { }

        public ProductViewModel(IProductService serviceAgent)
        {
            this.serviceAgent = serviceAgent;
            Load(new ProductSearchCondition() { });
        }

        #endregion

        #region Notifications

        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;

        public event EventHandler<NotificationEventArgs<Product, bool>> DeleteProductNotice;

        public event EventHandler<NotificationEventArgs<Product>> ProductDetailsNotice;

        #endregion

        #region Properties

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

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                NotifyPropertyChanged(m => m.SearchText);
                SearchCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Methods

        public void Load(ProductSearchCondition condition)
        {
            try
            {
                var lst = serviceAgent.Search(condition);

                if (lst != null)
                {
                    this.ProductCollection = new ObservableCollection<Product>(lst);
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message, ex);
            }
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
                    Load(new ProductSearchCondition()
                    {
                        Barcode = SearchText,
                        Name = SearchText
                    });
                }, () =>
                {
                    return true;
                }));
            }
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

        // Helper method to notify View of an error
        private void NotifyError(string message, Exception error)
        {
            // Notify view of an error
            SendMessage(MessageTokens.GlobalNotification, new NotificationEventArgs(message));
            Notify(ErrorNotice, new NotificationEventArgs<Exception>(message, error));
            LogService.Error(message, error);
        }

        #endregion
    }
}
