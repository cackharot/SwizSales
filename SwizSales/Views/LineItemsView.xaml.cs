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

namespace SwizSales.Views
{
    /// <summary>
    /// Interaction logic for LineItemsView.xaml
    /// </summary>
    public partial class LineItemsView : UserControl
    {
        public LineItemsView()
        {
            InitializeComponent();
        }

        protected void Item_GotFocus(object sender, RoutedEventArgs e)
        {
            var item = sender as ListViewItem;

            if (item != null)
            {
                lstOrders.SelectedItem = item.DataContext;
            }
        }
    }
}
