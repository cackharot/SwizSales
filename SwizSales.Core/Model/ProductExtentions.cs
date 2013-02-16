using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
namespace SwizSales.Core.Model
{
    public partial class Product : IDataErrorInfo, IEditableObject
    {
        #region Validate

        public string Error
        {
            get { return null; }
        }

        public string this[string columnName]
        {
            get
            {
                return Validate(columnName);
            }
        }

        private string Validate(string columnName)
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
                case "SellPrice":
                    validationResult = ValidateSellPrice();
                    break;
                case "Discount":
                    validationResult = ValidateDiscount();
                    break;
            }

            return validationResult;
        }

        private string ValidateDiscount()
        {
            return null;
        }

        private string ValidateSellPrice()
        {
            return null;
        }

        private string ValidateMRP()
        {
            return null;
        }

        private string ValidateName()
        {
            if (string.IsNullOrEmpty(this.Name))
                return "Name cannot be empty!";

            if (this.Name.Length < 3)
                return "Name length should be at-least 2 characters!";

            if (this.Name.Length > 50)
                return "Name length should not exceed 50 characters!";

            return null;
        }

        private string ValidateBarcode()
        {
            if (string.IsNullOrEmpty(this.Barcode))
                return "Barcode cannot be empty!";

            if (this.Barcode.Length > 50)
                return "Barcode length should not exceed 50 characters!";

            return null;
        }

        #endregion

        #region Editable

        Product Backup;
        bool _isEditMode;

        public Product()
        {
            this.PropertyChanging += new PropertyChangingEventHandler(Product_PropertyChanging);
        }

        private bool _hasChanges;
        public bool HasChanges
        {
            get { return _hasChanges; }
            private set
            {
                _hasChanges = value;
                OnPropertyChanged("HasChanges");
            }
        }
        
        public void BeginEdit()
        {
            Backup = new Product();
            CopyValuesTo(this, Backup);
            HasChanges = false;
            _isEditMode = true;
        }

        void Product_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (_isEditMode)
                HasChanges = true;
        }

        public void CancelEdit()
        {
            if (_isEditMode)
            {
                HasChanges = false;
                _isEditMode = false;

                // revert the changes
                CopyValuesTo(Backup, this);
            }
        }

        public void Reset()
        {
            if (_isEditMode)
            {
                HasChanges = false;
                _isEditMode = false;

                // revert the changes
                CopyValuesTo(Backup, this);

                _isEditMode = true;
            }
        }

        public void EndEdit()
        {
            if (_isEditMode)
            {
                HasChanges = false;
                Backup = null;
            }
        }

        private void CopyValuesTo(Product src, Product dest)
        {
            if (src == null || dest == null) return;

            dest.Name = src.Name;
            dest.Barcode = src.Barcode;
            dest.MRP = src.MRP;
            dest.SellPrice = src.SellPrice;
            dest.BuyPrice = src.BuyPrice;
            dest.Discount = src.Discount;
            dest.SupplierId = src.SupplierId;
            dest.CategoryId = src.CategoryId;
            dest.TaxCategoryId = src.TaxCategoryId;
            dest.Stock = src.Stock;
            dest.MinStock = src.MinStock;
            dest.Status = src.Status;
            dest.Sold = src.Sold;
        }

        private double _sold;
        public double Sold
        {
            get { return _sold; }
            set
            {
                _sold = value;
                OnPropertyChanged("Sold");
            }
        }

        private double _subTotal;
        public double SubTotal
        {
            get
            {
                return _subTotal;
            }
            set
            {
                _subTotal = value;
                OnPropertyChanged("SubTotal");
            }
        }

        #endregion


    }
}
