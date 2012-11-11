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
using SwizSales.Library;

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

        // Default ctor
        public CustomerViewModel() { }

        // TODO: ctor that accepts IXxxServiceAgent
        public CustomerViewModel(ICustomerService serviceAgent)
        {
            this.serviceAgent = serviceAgent;
            Load(new CustomerSearchCondition() { });
        }

        #endregion

        #region Notifications

        // TODO: Add events to notify the view or obtain data from the view
        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;

        public event EventHandler<NotificationEventArgs<Customer, bool>> DeleteCustomerNotice;

        public event EventHandler<NotificationEventArgs<Customer>> CustomerDetailsNotice;

        #endregion

        #region Properties

        // TODO: Add properties using the mvvmprop code snippet
        private ObservableCollection<Customer> _customerCollection;
        public ObservableCollection<Customer> CustomerCollection
        {
            get { return _customerCollection; }
            set
            {
                _customerCollection = value;
                NotifyPropertyChanged(m => m.CustomerCollection);
                //NotifyPropertyChanged(m => m.HasCustomers);
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

        //public Visibility HasCustomers
        //{
        //    get { return this.CustomerCollection != null && this.CustomerCollection.Count > 0 ? Visibility.Visible : Visibility.Collapsed; }
        //}

        #endregion

        #region Methods

        public void Load(CustomerSearchCondition condition)
        {
            try
            {
                var lst = serviceAgent.Search(condition);

                if (lst != null)
                {
                    this.CustomerCollection = new ObservableCollection<Customer>(lst);
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
                    Load(new CustomerSearchCondition()
                    {
                        Number = SearchText,
                        Name = SearchText,
                        Mobile = SearchText,
                        Email = SearchText
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