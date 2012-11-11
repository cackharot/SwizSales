using System;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Collections.ObjectModel;

// Toolkit namespace
using SimpleMvvmToolkit;

// Toolkit extension methods
using SimpleMvvmToolkit.ModelExtensions;
using SwizSales.Library;
using SwizSales.Core.Model;
using SwizSales.Core.ServiceContracts;
using SwizSales.Core.Services;
using System.Xml;
using System.IO;
using System.Windows.Documents;
using System.Windows.Markup;
using SwizSales.Core.Library;
using System.Windows.Threading;

namespace SwizSales.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvmprop</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// </summary>
    public class TicketPrinterViewModel : ViewModelBase<TicketPrinterViewModel>
    {
        #region Initialization and Cleanup

        private ObservableCollection<Order> _printedOrders;
        public ObservableCollection<Order> PrintedOrders
        {
            get { return _printedOrders; }
            private set
            {
                _printedOrders = value;
                NotifyPropertyChanged(x => x.PrintedOrders);
            }
        }

        private Order _selectedOrder;
        public Order SelectedOrder
        {
            get { return _selectedOrder; }
            set { _selectedOrder = value; NotifyPropertyChanged(x => x.SelectedOrder); }
        }

        public FixedDocumentSequence _flowDoc;
        public FixedDocumentSequence Document
        {
            get { return _flowDoc; }
            set { _flowDoc = value; NotifyPropertyChanged(x => x.Document); }
        }

        IOrderService orderService = new OrderService();

        public TicketPrinterViewModel()
        {
            this.PrintedOrders = new ObservableCollection<Order>();

            RegisterToReceiveMessages(MessageTokens.PrintOrder, (object s, NotificationEventArgs<Guid> e) =>
            {
                PrintOrder(GetOrder(e.Data), false);
            });

            RegisterToReceiveMessages(MessageTokens.PreviewOrder, (object s, NotificationEventArgs<Guid> e) =>
            {
                PrintOrder(GetOrder(e.Data), true);
            });
        }

        private Order GetOrder(Guid orderId)
        {
            if (orderId == Guid.Empty)
                return null;

            try
            {
                var prevOrders = this.PrintedOrders.Where(x => x.Id == orderId);
                if (prevOrders != null && prevOrders.Count() > 0)
                {
                    foreach (var item in prevOrders)
                        this.PrintedOrders.Remove(item);
                }

                return orderService.GetOrderById(orderId);
            }
            catch (Exception ex)
            {
                NotifyError("Error fetching order", ex);
            }

            return null;
        }

        private void PrintOrder(Order order, bool preview)
        {
            if (order == null)
                return;

            this.SelectedOrder = order;

            if (!this.PrintedOrders.Contains(order))
                this.PrintedOrders.Add(order);

            try
            {
                XmlDocument template = PrintHelper.GetPrintTicketTemplate(order);

                var flowDocument = (FlowDocument)XamlReader.Load(new XmlTextReader(new StringReader(template.OuterXml)));

                flowDocument.DataContext = order;
                flowDocument.PageHeight = order.OrderDetails.Count * ApplicationSettings.LineHeight + ApplicationSettings.ExtraHeight;
                flowDocument.PageWidth = ApplicationSettings.PageWidth;

                // we need to give the binding infrastructure a push as we
                // are operating outside of the intended use of WPF
                var dispatcher = Dispatcher.CurrentDispatcher;
                dispatcher.Invoke(DispatcherPriority.SystemIdle, new DispatcherOperationCallback(delegate { return null; }), null);

                var xps = PrintHelper.GetXpsDocument(flowDocument);
                this.Document = xps.GetFixedDocumentSequence();

                if (!preview)
                    PrintHelper.PrintXpsToPrinter(xps, Properties.Settings.Default.TicketPrinter);
            }
            catch (Exception ex)
            {
                NotifyError("Error printing order", ex);
            }
        }

        #endregion

        #region Notifications

        // TODO: Add events to notify the view or obtain data from the view
        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;

        #endregion

        #region Properties

        // TODO: Add properties using the mvvmprop code snippet

        #endregion

        #region Methods

        // TODO: Add methods that will be called by the view

        #endregion

        #region Completion Callbacks

        private DelegateCommand refreshCommand;
        public DelegateCommand RefreshCommand
        {
            get
            {
                if (refreshCommand == null)
                {
                    refreshCommand = new DelegateCommand(() =>
                    {
                    });
                }

                return refreshCommand;
            }
        }

        private DelegateCommand editCommand;
        public DelegateCommand EditCommand
        {
            get
            {
                return editCommand ?? (editCommand = new DelegateCommand(() =>
                {
                    if (this.SelectedOrder != null)
                    {
                        PrintOrder(this.SelectedOrder, true);
                    }
                }));
            }
        }

        private DelegateCommand printCommand;
        public DelegateCommand PrintCommand
        {
            get
            {
                return printCommand ?? (printCommand = new DelegateCommand(() =>
                {
                    PrintOrder(this.SelectedOrder, false);
                }, () =>
                {
                    return this.SelectedOrder != null && this.SelectedOrder.OrderDetails.Count > 0;
                }));
            }
        }

        #endregion

        #region Helpers

        private void NotifyError(string message, Exception error)
        {
            SendMessage(MessageTokens.GlobalNotification, new NotificationEventArgs(message));
            Notify(ErrorNotice, new NotificationEventArgs<Exception>(message, error));
            LogService.Error("Print error", error);
        }

        #endregion
    }
}