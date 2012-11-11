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

namespace SwizSales.Views
{
    /// <summary>
    /// Interaction logic for PurchaseView.xaml
    /// </summary>
    public partial class PurchaseView : UserControl
    {
        public PurchaseView()
        {
            InitializeComponent();

            var vm = this.DataContext as PurchaseViewModel;

            if (vm == null) return;

            vm.ErrorNotice += (ss, ee) =>
            {
                MessageBox.Show(ee.Message, "ERROR", MessageBoxButton.OK);
            };

            vm.ManagePurchaseNotice += new EventHandler<SimpleMvvmToolkit.NotificationEventArgs<Core.Model.Purchase>>(vm_ManagePurchaseNotice);

            vm.PreviewNotice += new EventHandler<SimpleMvvmToolkit.NotificationEventArgs<Core.Model.Purchase>>(vm_PreviewNotice);

            vm.PaymentsNotice += new EventHandler<SimpleMvvmToolkit.NotificationEventArgs<Core.Model.Purchase>>(vm_PaymentsNotice);
        }

        void vm_PaymentsNotice(object sender, SimpleMvvmToolkit.NotificationEventArgs<Core.Model.Purchase> e)
        {
            
        }

        void vm_PreviewNotice(object sender, SimpleMvvmToolkit.NotificationEventArgs<Core.Model.Purchase> e)
        {
            
        }

        void vm_ManagePurchaseNotice(object sender, SimpleMvvmToolkit.NotificationEventArgs<Core.Model.Purchase> e)
        {
            var vm = new ManagePurchaseViewModel(e.Data);

            vm.ErrorNotice += (ss, ee) =>
            {
                MessageBox.Show(ee.Message, "ERROR", MessageBoxButton.OK);
            };

            var win = new ManagePurchaseWindow();
            win.DataContext = vm;
            win.Owner = Application.Current.MainWindow;

            win.Show(); // show non-blocking
        }
    }
}
