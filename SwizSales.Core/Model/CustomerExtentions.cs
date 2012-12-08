using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace SwizSales.Core.Model
{
    public partial class Customer : IDataErrorInfo
    {
        public Customer()
        {
            this.CreatedBy = SwizSales.Constants.UserId;
            this.CreatedOn = DateTime.Now;
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
                    case "SSN":
                        validationResult = ValidateSSN();
                        break;
                }
                return validationResult;
            }
        }

        private string ValidateSSN()
        {
            if (string.IsNullOrEmpty(this.SSN))
                return "Enter a valid customer number!";

            if (this.SSN.Length < 3)
                return "Customer number should be atleast 3 characters!";

            return null;
        }

        private int _points;
        public int Points
        {
            get { return _points; }
            set { _points = value; OnPropertyChanged("Points"); }
        }

        private double _totalAmt;
        public double TotalAmount
        {
            get { return _totalAmt; }
            set { _totalAmt = value; OnPropertyChanged("TotalAmount"); }
        }
    }

    public partial class ContactDetail : IDataErrorInfo
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
                    case "ContactName":
                        validationResult = ValidateName();
                        break;
                    case "Mobile":
                        validationResult = ValidateMobile();
                        break;
                    case "Landline":
                        validationResult = ValidateLandLine();
                        break;
                    case "Email":
                        validationResult = ValidateEmail();
                        break;
                    case "Pincode":
                        validationResult = ValidatePincode();
                        break;
                }
                return validationResult;
            }
        }

        private string ValidateName()
        {
            if (string.IsNullOrEmpty(this.ContactName))
                return "Enter a valid name!";

            if (this.ContactName.Length < 4)
                return "Name should be atleast 4 characters!";

            return null;
        }

        private string ValidatePincode()
        {
            return null;
        }

        private string ValidateLandLine()
        {
            return null;
        }

        private string ValidateEmail()
        {
            return null;
        }

        private string ValidateMobile()
        {
            return null;
        }
    }
}
