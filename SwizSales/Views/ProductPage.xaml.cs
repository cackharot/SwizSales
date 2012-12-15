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
    /// Interaction logic for ProductPage.xaml
    /// </summary>
    public partial class ProductPage : UserControl
    {
        public ProductPage()
        {
            InitializeComponent();

            var vm = (ProductViewModel)this.DataContext;

            vm.ErrorNotice += (s, e) =>
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK);
            };

            vm.DeleteProductNotice += (s, e) =>
            {
                if (e.Data != null)
                {
                    if (MessageBox.Show(string.Format("Are you sure to delete '{0}'?", e.Data.Name), "Delete Product", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        e.Completed(true);
                    }
                }

                e.Completed(false);
            };
        }
    }
}
