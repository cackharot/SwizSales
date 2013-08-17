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
using SwizSales.Core.Library;

namespace SwizSales.Views
{
    /// <summary>
    /// Interaction logic for OrdersView.xaml
    /// </summary>
    public partial class OrdersView : UserControl
    {
        public OrdersView()
        {
            InitializeComponent(); 
            
            var vm = this.DataContext as OrdersViewModel;

            if (vm != null)
            {
                vm.ErrorNotice += new EventHandler<SimpleMvvmToolkit.NotificationEventArgs<Exception>>(vm_ErrorNotice);
                vm.PreviewNotice += new EventHandler<SimpleMvvmToolkit.NotificationEventArgs<Core.Model.Order>>(vm_PreviewNotice);
            }
        }

        void vm_ErrorNotice(object sender, SimpleMvvmToolkit.NotificationEventArgs<Exception> e)
        {
            LogService.Error("Error in orders page", e.Data);
            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK);
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
    }
}
