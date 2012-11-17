using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SwizSales.ViewModel;
using System.Xml;
using System.IO;
using System.Windows.Markup;
using SwizSales.Core.Library;
using System.Windows.Threading;
using SwizSales.Properties;

namespace SwizSales.Views
{
    /// <summary>
    /// Interaction logic for SalesPage.xaml
    /// </summary>
    public partial class SalesPage : UserControl
    {
        public SalesPage()
        {
            InitializeComponent();

            var vm = this.DataContext as SalesViewModel;

            vm.ErrorNotice += new EventHandler<SimpleMvvmToolkit.NotificationEventArgs<Exception>>(vm_ErrorNotice);

            vm.SelectBarcode += new EventHandler(vm_SelectBarcode);

            vm.PreviewNotice += new EventHandler<SimpleMvvmToolkit.NotificationEventArgs<Core.Model.Order>>(vm_PreviewNotice);

            vm.CheckoutNotice += new EventHandler<SimpleMvvmToolkit.NotificationEventArgs<Core.Model.Order, bool>>(vm_CheckoutNotice);

            vm.ChangeCustomerNotice += new EventHandler<SimpleMvvmToolkit.NotificationEventArgs<Core.Model.Order, Core.Model.Customer>>(vm_ChangeCustomerNotice);

            vm.SearchProductNotice += new EventHandler<SimpleMvvmToolkit.NotificationEventArgs<List<Core.Model.Product>, Core.Model.Product>>(vm_SearchProductNotice);

            this.Loaded += new RoutedEventHandler(SalesPage_Loaded);
        }

        void vm_SearchProductNotice(object sender, SimpleMvvmToolkit.NotificationEventArgs<List<Core.Model.Product>, Core.Model.Product> e)
        {
            bool selected = false;
            var vm = new QuickSearchProductViewModel(e.Data);

            var win = new SelectProductWindow();
            win.Owner = Application.Current.MainWindow;
            win.DataContext = vm;

            vm.CloseNotice += (ss, ee) =>
            {
                if (vm.SelectedProduct != null)
                {
                    selected = true;
                    e.Completed(vm.SelectedProduct);
                }

                win.Close();
            };

            win.txtSearch.Focus();
            win.ShowDialog();

            if (!selected)
                e.Completed(null);
        }

        void vm_ErrorNotice(object sender, SimpleMvvmToolkit.NotificationEventArgs<Exception> e)
        {
            MessageBox.Show(e.Message, "ERROR", MessageBoxButton.OK);
        }

        void vm_ChangeCustomerNotice(object sender, SimpleMvvmToolkit.NotificationEventArgs<Core.Model.Order, Core.Model.Customer> e)
        {
            bool selected = false;
            var vm = new CustomerSearchViewModel();

            var win = new CustomerSearchView();
            win.Owner = Application.Current.MainWindow;
            win.DataContext = vm;

            vm.CloseNotice += (ss, ee) =>
            {
                if (vm.SelectedCustomer != null)
                {
                    selected = true;
                    e.Completed(vm.SelectedCustomer);
                }

                win.Close();
            };

            win.ShowDialog();

            if (!selected)
                e.Completed(null);
        }

        void vm_CheckoutNotice(object sender, SimpleMvvmToolkit.NotificationEventArgs<Core.Model.Order, bool> e)
        {
            bool tmp = false;
            var payWin = new InvoicePaymentDialog();
            payWin.Owner = Application.Current.MainWindow;

            if (Settings.Default.AutoPrint)
            {
                payWin.paymentView.chkPrint.IsChecked = true;
            }
            else
            {
                payWin.paymentView.chkPrint.IsChecked = false;
            }

            payWin.DataContext = e.Data;
            payWin.paymentView.btnPay.Click += (s, ee) =>
            {
                tmp = true;
                bool print = payWin.paymentView.chkPrint.IsChecked.HasValue && payWin.paymentView.chkPrint.IsChecked.Value;
                payWin.Close();
                e.Completed(true);

                if (print)
                {
                    this.btnPrint.Command.Execute(null);
                }
            };
            payWin.paymentView.txtPaidAmount.Focus();
            payWin.ShowDialog();

            if (!tmp)
            {
                e.Completed(false);
            }
        }

        void vm_PreviewNotice(object sender, SimpleMvvmToolkit.NotificationEventArgs<Core.Model.Order> e)
        {
            try
            {
                var flowDocument = PrintHelper.GetPrintDocument(e.Message, e.Data);
                var xps = PrintHelper.GetXpsDocument(flowDocument);
                var document = xps.GetFixedDocumentSequence();

                var previewWindow = new PreviewTicket();
                previewWindow.Owner = Application.Current.MainWindow;
                previewWindow.docViewer.Document = document;
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LogService.Error("Error while preview ticket", ex);
                MessageBox.Show(ex.Message);
            }
        }

        void SalesPage_Loaded(object sender, RoutedEventArgs e)
        {
            SelectBarcodeTextBox();
        }

        void vm_SelectBarcode(object sender, EventArgs e)
        {
            SelectBarcodeTextBox();
        }

        private void SelectBarcodeTextBox()
        {
            txtBarcode.Focus();
            txtBarcode.SelectAll();
        }
    }
}
