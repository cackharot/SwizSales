using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleMvvmToolkit;
using SwizSales.Core.Model;
using System.Collections.ObjectModel;
using SwizSales.Properties;
using SwizSales.Library;
using SwizSales.Core.Library;

namespace SwizSales.ViewModel
{
    public class OrderPaymentViewModel : ViewModelBase<OrderPaymentViewModel>
    {
        private Order _order;
        public Order Order
        {
            get { return _order; }
            set
            {
                _order = value;
                NotifyPropertyChanged(m => m.Order);
            }
        }

        private Payment _model;
        public Payment Model
        {
            get { return _model; }
            set
            {
                _model = value;
                NotifyPropertyChanged(m => m.Model);
            }
        }

        private ObservableCollection<string> _paymentModes;
        public ObservableCollection<string> PaymentModes
        {
            get { return _paymentModes; }
            set
            {
                _paymentModes = value;
                NotifyPropertyChanged(m => m.PaymentModes);
            }
        }

        private string selectedPaymentMode;
        public string SelectedPaymentMode
        {
            get { return selectedPaymentMode; }
            set
            {
                selectedPaymentMode = value;

                PaymentType pmode;
                if (Enum.TryParse<PaymentType>(value, out pmode))
                {
                    this.Model.Type = (short)pmode;

                    if (pmode == PaymentType.Card && Settings.Default.CreditCardPercent > 0)
                    {
                        this.CardAmount = (this.Order.TotalAmount * Settings.Default.CreditCardPercent);
                    }
                    else
                    {
                        this.CardAmount = 0;
                    }
                }

                NotifyPropertyChanged(m => m.SelectedPaymentMode);
                NotifyPropertyChanged(m => m.TotalAmount);
                NotifyPropertyChanged(m => m.CardAmount);
            }
        }

        private double _cardAmount;
        public double CardAmount
        {
            get { return _cardAmount; }
            set
            {
                _cardAmount = value;
                this.Model.PaidAmount = Math.Ceiling(this.Order.BalanceAmount > 0 ? this.Order.BalanceAmount + value : 0);
                NotifyPropertyChanged(m => m.CardAmount);
            }
        }

        public double TotalAmount
        {
            get { return Math.Ceiling(this.Order.TotalAmount + this.CardAmount); }
        }

        private bool _enablePrint;
        public bool EnablePrint
        {
            get { return _enablePrint; }
            set
            {
                _enablePrint = value;
                NotifyPropertyChanged(m => m.EnablePrint);
            }
        }

        public OrderPaymentViewModel(Order order)
        {
            this.Order = order;

            this.Model = new Payment
            {
                OrderId = this.Order.Id,
                PaidAmount = this.Order.BalanceAmount > 0 ? this.Order.BalanceAmount : 0,
                PaymentDate = DateTime.Now,
                EmployeeId = Settings.Default.DefaultEmployeeId,
                SystemId = this.Order.SystemId,
                Type = (short)PaymentType.Cash
            };

            this.EnablePrint = Settings.Default.AutoPrint;

            this.PaymentModes = new ObservableCollection<string>();
            this.PaymentModes.Add(PaymentType.Cash.ToString());
            this.PaymentModes.Add(PaymentType.Credit.ToString());
            this.PaymentModes.Add(PaymentType.Card.ToString());
            //this.PaymentModes.Add(PaymentType.Cheque.ToString());

            this.SelectedPaymentMode = PaymentType.Cash.ToString();
        }

        #region Notifications

        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;
        public event EventHandler<NotificationEventArgs> CloseNotice;

        #endregion

        private DelegateCommand _payCommand;
        public DelegateCommand PayCommand
        {
            get
            {
                return _payCommand ?? (_payCommand = new DelegateCommand(() =>
                {
                    this.Model.PaymentDate = DateTime.Now;

                    switch ((PaymentType)this.Model.Type)
                    {
                        case PaymentType.Card:
                            if (this.Model.PaidAmount < this.Order.BalanceAmount + this.CardAmount)
                            {
                                NotifyError(string.Format("Paid Amount should be greater than {0:C}", this.Order.BalanceAmount + this.CardAmount), null);
                                return;
                            }

                            this.Order.OrderDetails.Add(new OrderDetail
                            {
                                Barcode = ".000123",
                                ItemName = "Credit/Debit Card Charge",
                                MRP = this.Order.TotalAmount,
                                Price = this.Order.TotalAmount,
                                Quantity = Settings.Default.CreditCardPercent
                            });

                            break;
                        case PaymentType.Cash:
                            if (this.Model.PaidAmount < this.Order.BalanceAmount)
                            {
                                NotifyError(string.Format("Paid Amount should be greater than {0:C}", this.Order.BalanceAmount), null);
                                return;
                            }
                            break;
                        case PaymentType.Credit:
                            if (this.Model.PaidAmount >= this.Order.BalanceAmount)
                            {
                                this.Model.Type = (short)PaymentType.Cash;
                            }
                            break;
                        case PaymentType.Cheque:
                            break;
                        case PaymentType.Online:
                            break;
                    }

                    this.Order.Payments.Add(this.Model);

                    NotifyClose("Close payment!");
                }));
            }
            private set { _payCommand = value; }
        }

        private void NotifyError(string message, Exception error)
        {
            SendMessage(MessageTokens.GlobalNotification, new NotificationEventArgs(message));
            LogService.Error(message, error);
            Notify(ErrorNotice, new NotificationEventArgs<Exception>(message, error));
        }

        private void NotifyClose(string message = "")
        {
            Notify(CloseNotice, new NotificationEventArgs(message));
        }
    }
}
