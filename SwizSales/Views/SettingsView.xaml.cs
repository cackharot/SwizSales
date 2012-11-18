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
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();

            var vm = this.DataContext as SettingsViewModel;

            if (vm != null)
            {
                vm.ErrorNotice += new EventHandler<SimpleMvvmToolkit.NotificationEventArgs<Exception>>(vm_ErrorNotice);
                vm.NewTemplateNotice += new EventHandler<SimpleMvvmToolkit.NotificationEventArgs<bool, Core.Model.Setting>>(vm_NewTemplateNotice);
            }
        }

        void vm_NewTemplateNotice(object sender, SimpleMvvmToolkit.NotificationEventArgs<bool, Core.Model.Setting> e)
        {
            bool isDone = false;
            var vm = new Setting();
            var win = new NewSettingsWindow();
            win.DataContext = vm;
            win.btnSave.Click += (ss, ee) =>
            {
                isDone = true;
                win.Close();
                e.Completed(vm);
            };

            win.ShowDialog();

            if (!isDone)
            {
                e.Completed(null);
            }
        }

        void vm_ErrorNotice(object sender, SimpleMvvmToolkit.NotificationEventArgs<Exception> e)
        {
            MessageBox.Show(e.Message, "ERROR", MessageBoxButton.OK);
        }
    }
}
