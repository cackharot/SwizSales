using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace SwizSales.Core.Model
{
    public partial class Order
    {
        private double _currentPaidAmount;
        public double CurrentPaidAmount
        {
            get
            {
                return _currentPaidAmount;
            }
            set
            {
                _currentPaidAmount = value;
                RaisePropertyChanged("CurrentPaidAmount");
            }
        }

        public double _TotalAmount
        {
            get
            {
                return this.OrderDetails.Sum(x => x.LineTotal);
            }
        }

        public double TotalAmount
        {
            get
            {
                return Math.Round(this._TotalAmount);
            }
        }

        public double RoundOff
        {
            get
            {
                return (this.TotalAmount - this._TotalAmount);
            }
        }

        public double MRPTotalAmount
        {
            get
            {
                return Math.Floor(this.OrderDetails.Sum(x => x.MRPLineTotal));
            }
        }

        public double SavingsAmount
        {
            get
            {
                return this.MRPTotalAmount - this.TotalAmount;
            }
        }

        public double _PaidAmount
        {
            get
            {
                if (this.Payments == null || this.Payments.Count == 0)
                    return 0.0;

                return this.Payments.Sum(x => x.PaidAmount);
            }
        }

        public double PaidAmount
        {
            get { return Math.Round(_PaidAmount); }
        }

        public double BalanceAmount
        {
            get
            {
                return this.TotalAmount - this.PaidAmount;
            }
        }

        public double TotalQuantity
        {
            get
            {
                return this.OrderDetails.Sum(x => x.Quantity);
            }
        }

        public double TotalItems
        {
            get
            {
                return this.OrderDetails.Count;
            }
        }

        public bool IsPaid
        {
            get
            {
                return this.PaidAmount > 0;
            }
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        public void NotifyChanges()
        {
            this.RaisePropertyChanged("Customer");
            this.RaisePropertyChanged("TotalAmount"); this.RaisePropertyChanged("_TotalAmount");
            this.RaisePropertyChanged("RoundOff");
            this.RaisePropertyChanged("PaidAmount"); this.RaisePropertyChanged("_PaidAmount");
            this.RaisePropertyChanged("BalanceAmount");
            this.RaisePropertyChanged("SavingsAmount");
            this.RaisePropertyChanged("TotalQuantity");
            this.RaisePropertyChanged("TotalItems");

            this.RaisePropertyChanged("IsPaid");
        }
    }

    public partial class OrderDetail
    {
        public double LineTotal
        {
            get
            {
                return this.Quantity * this.Price;
            }
        }

        public double MRPLineTotal
        {
            get
            {
                return this.Quantity * this.MRP;
            }
        }

        public OrderDetail()
        {
            this.PropertyChanged += new PropertyChangedEventHandler(OrderDetail_PropertyChanged);
        }

        partial void OnMRPChanged()
        {
            this.Price = this.MRP - (this.MRP * (this.Discount / 100.0));
        }

        partial void OnDiscountChanged()
        {
            if (!dontChange)
            {
                var ap = this.MRP - (this.MRP * (this.Discount / 100.0));
                if (ap != this.Price)
                {
                    //System.Diagnostics.Trace.WriteLine(string.Format("Price diff: Calculated: {0:C}, Actual: {1:C}", ap, this.Price));
                }
                this.Price = ap;
            }
        }

        private bool dontChange;
        partial void OnPriceChanged()
        {
            dontChange = true;
            this.Discount = ((this.MRP - this.Price) * 100.00) / this.MRP;
            dontChange = false;
        }

        void OrderDetail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MRP" || e.PropertyName == "Price"
                || e.PropertyName == "Discount"
                || e.PropertyName == "Quantity")
            {
                OnPropertyChanged("LineTotal");
            }
        }
    }
}
