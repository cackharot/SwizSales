using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace SwizSales.Core.Model
{
    public partial class Purchase : IDataErrorInfo
    {
        public string Error
        {
            get { return string.Empty; }
        }

        public string this[string columnName]
        {
            get
            {
                string validationResult = null;
                switch (columnName)
                {
                    case "BillNo":
                        if (string.IsNullOrEmpty(this.BillNo))
                        {
                            validationResult = "Bill number cannot be empty!";
                        }
                        break;
                    case "SupplierId":
                        if (this.SupplierId == Guid.Empty)
                        {
                            validationResult = "Choose a valid supplier!";
                        }
                        break;
                }
                return validationResult;
            }
        }

        public double PaidAmount
        {
            get
            {
                return this.PurchasePayments != null ? this.PurchasePayments.Sum(x => x.PaidAmount) : 0.0;
            }
        }

        public double ActualTotalAmount
        {
            get
            {
                return this.PurchaseDetails.Sum(x => x.Quantity * x.BuyPrice);
            }
        }

        public double BalanceAmount
        {
            get { return this.ActualTotalAmount - this.PaidAmount; }
        }

        public int TotalItems
        {
            get
            {
                return this.PurchaseDetails.Count;
            }
        }

        public double TotalQuantity
        {
            get
            {
                return this.PurchaseDetails.Sum(x => x.Quantity);
            }
        }

        public void NotifyChanges()
        {
            this.RaisePropertyChanged("Supplier");
            this.RaisePropertyChanged("ActualTotalAmount");
            this.RaisePropertyChanged("PaidAmount");
            this.RaisePropertyChanged("BalanceAmount");
            this.RaisePropertyChanged("TotalQuantity");
            this.RaisePropertyChanged("TotalItems");
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
