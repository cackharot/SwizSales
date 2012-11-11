using System;
using System.Windows;
using System.Threading;
using System.Collections.ObjectModel;

// Toolkit namespace
using SimpleMvvmToolkit;

// Toolkit extension methods
using SimpleMvvmToolkit.ModelExtensions;
using SwizSales.Core.Model;
using SwizSales.Core.ServiceContracts;
using System.ComponentModel;
using SwizSales.Library;

namespace SwizSales.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvmprop</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// </summary>
    public class CustomerDetailViewModel : ViewModelBase<CustomerDetailViewModel>
    {
        #region Initialization and Cleanup

        private ICustomerService serviceAgent;
        Customer OriginalModel { get; set; }

        // Default ctor
        public CustomerDetailViewModel(ICustomerService serviceAgent, Customer entity)
        {
            this.serviceAgent = serviceAgent;
            this.OriginalModel = entity;

            this.Model = new Customer();
            this.Model.ContactDetail = new ContactDetail();
            CopyModel(this.Model, this.OriginalModel);

            this.Title = GetTitle(entity);
        }

        private static string GetTitle(Customer entity)
        {
            if (entity.Id.Equals(Guid.Empty))
            {
                return "Add New Customer";
            }
            else
            {
                return string.Format("Edit '{0}' Details", entity.ContactDetail.ContactName);
            }
        }

        #endregion

        #region Notifications

        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;
        public event EventHandler<NotificationEventArgs> CloseNotice;

        #endregion

        #region Properties

        private Customer _model;
        public Customer Model
        {
            get { return _model; }
            set
            {
                _model = value;
                NotifyPropertyChanged(m => m.Model);
            }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                NotifyPropertyChanged(m => m.Title);
            }
        }

        private bool allPropertiesValid = false;
        public bool AllPropertiesValid
        {
            get { return allPropertiesValid; }
            set
            {
                if (allPropertiesValid != value)
                {
                    allPropertiesValid = value;
                    NotifyPropertyChanged(x => x.AllPropertiesValid);
                }
            }
        }

        #endregion

        #region Methods

        public bool Save()
        {
            if (this.Validate(this.Model))
            {
                try
                {
                    var entity = this.Model;

                    if (entity.Id.Equals(Guid.Empty))
                    {
                        entity.CreatedBy = SwizSales.Constants.UserId;
                        entity.CreatedOn = DateTime.Now;
                        entity.Status = true;

                        this.serviceAgent.Add(entity);
                    }
                    else
                    {
                        CopyModel(this.OriginalModel, this.Model);

                        entity = this.OriginalModel;

                        entity.UpdatedBy = SwizSales.Constants.UserId;
                        entity.UpdatedOn = DateTime.Now;
                        entity.Status = true;
                        this.serviceAgent.Update(entity);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    NotifyError(ex.Message, ex);
                }
            }

            return false;
        }

        private static void CopyModel(Customer orginalModel, Customer modifiedModel)
        {
            orginalModel.Id = modifiedModel.Id;
            orginalModel.SSN = modifiedModel.SSN;
            orginalModel.CreatedBy = modifiedModel.CreatedBy;
            orginalModel.CreatedOn = modifiedModel.CreatedOn;
            orginalModel.UpdatedBy = modifiedModel.UpdatedBy;
            orginalModel.UpdatedOn = modifiedModel.UpdatedOn;
            orginalModel.Status = modifiedModel.Status;

            orginalModel.ContactDetail.Id = modifiedModel.ContactDetail.Id;
            orginalModel.ContactDetail.ContactName = modifiedModel.ContactDetail.ContactName;
            orginalModel.ContactDetail.City = modifiedModel.ContactDetail.City;
            orginalModel.ContactDetail.Country = modifiedModel.ContactDetail.Country;
            orginalModel.ContactDetail.DateOfBirth = modifiedModel.ContactDetail.DateOfBirth;
            orginalModel.ContactDetail.Email = modifiedModel.ContactDetail.Email;
            orginalModel.ContactDetail.Landline = modifiedModel.ContactDetail.Landline;
            orginalModel.ContactDetail.Mobile = modifiedModel.ContactDetail.Mobile;
            orginalModel.ContactDetail.Pincode = modifiedModel.ContactDetail.Pincode;
            orginalModel.ContactDetail.Street = modifiedModel.ContactDetail.Street;
        }

        private bool Validate(Customer entity)
        {
            return string.IsNullOrEmpty((entity as IDataErrorInfo).Error) && string.IsNullOrEmpty((entity.ContactDetail as IDataErrorInfo).Error);
        }

        private DelegateCommand _saveCommand;
        public DelegateCommand SaveCommand
        {
            get
            {
                return _saveCommand ?? (_saveCommand = new DelegateCommand(() =>
                {
                    if (Save())
                    {
                        Notify(CloseNotice, new NotificationEventArgs());
                    }
                }, () =>
                {
                    return true;
                }));
            }
            private set { _saveCommand = value; }
        }

        #endregion

        #region Completion Callbacks



        #endregion

        #region Helpers

        private void NotifyError(string message, Exception error)
        {
            SendMessage(MessageTokens.GlobalNotification, new NotificationEventArgs(message));
            Notify(ErrorNotice, new NotificationEventArgs<Exception>(message, error));
        }

        #endregion
    }
}