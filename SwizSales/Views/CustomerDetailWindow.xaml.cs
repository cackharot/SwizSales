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
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.Windows.Automation.Peers;

namespace SwizSales.Views
{
    /// <summary>
    /// Interaction logic for CustomerDetailWindow.xaml
    /// </summary>
    public partial class CustomerDetailWindow : MetroWindow
    {
        public CustomerDetailWindow()
        {
            InitializeComponent();
        }

        private void OnCloseCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }
    }
}
