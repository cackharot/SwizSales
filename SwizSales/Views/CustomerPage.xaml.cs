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
using SwizSales.Core.Model;

namespace SwizSales.Views
{
    /// <summary>
    /// Interaction logic for CustomerPage.xaml
    /// </summary>
    public partial class CustomerPage : UserControl
    {
        CustomerViewModel Model { get; set; }

        public CustomerPage()
        {
            InitializeComponent();

            Model = (CustomerViewModel)this.DataContext;

            Model.ErrorNotice += (s, e) =>
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK);
            };

            Model.DeleteCustomerNotice += (s, e) =>
            {
                if (e.Data != null)
                {
                    if (MessageBox.Show(string.Format("Are you sure to delete '{0}'?", e.Data.ContactDetail.ContactName), "Delete Customer", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        e.Completed(true);
                    }
                }
            
                e.Completed(false);
            };

            Model.CustomerDetailsNotice += (s, e) =>
            {
                var title = e.Message;
                var entity = e.Data;
                var vm = new CustomerDetailViewModel(Model.serviceAgent, entity);

                var dw = new CustomerDetailWindow();
                dw.Owner = App.Current.MainWindow;
                dw.ShowInTaskbar = false;
                dw.DataContext = vm;

                vm.CloseNotice += (ss, ee) =>
                {
                    dw.Close();
                };

                vm.ErrorNotice += (ss, ee) =>
                {
                    MessageBox.Show(ee.Data == null ? ee.Message : ee.Data.Message, "Error");
                };

                dw.ShowDialog();
            };
        }
    }
}
