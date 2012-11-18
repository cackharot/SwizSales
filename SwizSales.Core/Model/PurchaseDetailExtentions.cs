using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using SwizSales.Core.Library;

namespace SwizSales.Core.Model
{
    public partial class PurchaseDetail : IDataErrorInfo
    {
        private double _sellPrice;
        public double SellPrice { get { return _sellPrice; } set { _sellPrice = value; OnPropertyChanged("SellPrice"); } }

        private double _discount;
        public double Discount { get { return _discount; } set { _discount = value; OnPropertyChanged("Discount"); } }

        private Guid _categoryId;
        public Guid CategoryId { get { return _categoryId; } set { _categoryId = value; OnPropertyChanged("CategoryId"); } }

        private Guid _taxCategoryId;
        public Guid TaxCategoryId { get { return _taxCategoryId; } set { _taxCategoryId = value; OnPropertyChanged("TaxCategoryId"); } }

        public double SubTotal
        {
            get { return this.Quantity * this.BuyPrice; }
        }

        public PurchaseDetail()
        {
            this.PropertyChanged += new PropertyChangedEventHandler(PurchaseDetail_PropertyChanged);
            //this.CategoryId = ApplicationSettings.DefaultCategoryId;
            //this.TaxCategoryId = ApplicationSettings.DefaultTaxCategoryId;
        }

        partial void OnMRPChanged()
        {
            this.SellPrice = this.MRP - (this.MRP * (this.Discount / 100.0));
            //this.BuyPrice = this.MRP - (this.MRP * (ApplicationSettings.PuchaseDiscount / 100.0));
        }

        public void OnDiscountChanged()
        {
            if (!dontChange)
            {
                this.SellPrice = this.MRP - (this.MRP * (this.Discount / 100.0));
            }
        }

        private bool dontChange;
        public void OnSellPriceChanged()
        {
            dontChange = true;
            this.Discount = ((this.MRP - this.SellPrice) * 100.00) / this.MRP;
            dontChange = false;
        }

        void PurchaseDetail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MRP" || e.PropertyName == "BuyPrice"
                || e.PropertyName == "Discount"
                || e.PropertyName == "Quantity")
            {
                OnPropertyChanged("SubTotal");
            }
        }

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
                    case "Barcode":
                        validationResult = ValidateBarcode();
                        break;
                    case "Name":
                        validationResult = ValidateName();
                        break;
                    case "MRP":
                        validationResult = ValidateMRP();
                        break;
                    case "BuyPrice":
                        validationResult = ValidateBuyPrice();
                        break;
                    case "SellPrice":
                        validationResult = ValidateSellPrice();
                        break;
                    case "Quantity":
                        validationResult = ValidateQuantity();
                        break;
                }
                return validationResult;
            }
        }

        private string ValidateQuantity()
        {
            if (this.Quantity == 0)
                return "Quantity cannot be zero!";

            return null;
        }

        private string ValidateSellPrice()
        {
            //if (this.SellPrice < 0)
            //    return "Sell price cannot be a negative value!";

            //if (this.SellPrice > this.MRP)
            //    return "Sell price cannot be a greater than MRP!";

            //if (this.SellPrice < this.BuyPrice)
            //    return "Sell price cannot be a lesser than buy price!";

            return null;
        }

        private string ValidateBuyPrice()
        {
            if (this.BuyPrice < 0)
                return "Buy price cannot be a negative value!";

            if (this.BuyPrice > this.MRP)
                return "Buy price cannot be a greater than MRP!";

            return null;
        }

        private string ValidateMRP()
        {
            if (this.MRP < 0)
                return "MRP cannot be a negative value!";
            return null;
        }

        private string ValidateName()
        {
            if (string.IsNullOrEmpty(this.Name))
                return "Enter a valid Name!";

            if (this.Name.Length < 3)
                return "Name should be at-least 3 characters long";

            return null;
        }

        private string ValidateBarcode()
        {
            if (string.IsNullOrEmpty(this.Barcode))
                return "Enter a valid barcode!";
            return null;
        }
    }
}
