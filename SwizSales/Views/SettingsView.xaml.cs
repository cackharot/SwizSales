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
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();

            var vm = this.DataContext as SettingsViewModel;
            vm.ErrorNotice += new EventHandler<SimpleMvvmToolkit.NotificationEventArgs<Exception>>(vm_ErrorNotice);
        }

        void vm_ErrorNotice(object sender, SimpleMvvmToolkit.NotificationEventArgs<Exception> e)
        {
            MessageBox.Show(e.Message, "ERROR", MessageBoxButton.OK);
        }
    }
}
