using System;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Collections.ObjectModel;

// Toolkit namespace
using SimpleMvvmToolkit;

// Toolkit extension methods
using SimpleMvvmToolkit.ModelExtensions;
using System.Printing;
using System.Globalization;
using SwizSales.Core.Library;
using SwizSales.Library;
using SwizSales.Core.ServiceContracts;
using SwizSales.Core.Model;
using System.ComponentModel;
using System.Xml;
using System.IO;
using System.Text;
using System.Windows.Markup;
using SwizSales.Core.Services;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace SwizSales.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvmprop</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// </summary>
    public class SettingsViewModel : ViewModelBase<SettingsViewModel>
    {
        #region Initialization and Cleanup

        private ISettingsService settingService;

        public SettingsViewModel(ISettingsService serviceAgent)
        {
            this.settingService = serviceAgent;
            LoadTemplates();
            LoadPrinters();

            LoadMockOrder();
        }

        private void LoadMockOrder()
        {
            this.Order = new OrderService().Search(new OrderSearchCondition { PageNo = 1, PageSize = 1 }).FirstOrDefault();
        }

        private void LoadTemplates()
        {
            this.Templates = new ObservableCollection<Setting>(this.settingService.GetSettingsByCategory(Constants.Category.Templates));

            var cusPointSetting = this.settingService.GetSettingById(Constants.CustomerPointsStartDateId);

            if (cusPointSetting != null && !string.IsNullOrEmpty(cusPointSetting.Value))
            {
                this.CustomerPointStartDate = DateTime.Parse(cusPointSetting.Value);
            }
            else
            {
                this.CustomerPointStartDate = DateTime.Parse("01/01/" + DateTime.Now.Year);
            }
        }

        private void LoadPrinters()
        {
            this.Printers = new ObservableCollection<string>();
            this.Languages = new ObservableCollection<CultureInfo>();

            LocalPrintServer printServer = new LocalPrintServer();
            PrintQueueCollection printQueuesOnLocalServer = printServer.GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections });
            foreach (PrintQueue printer in printQueuesOnLocalServer)
            {
                this.Printers.Add(printer.Name);
            }

            foreach (var cinfo in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                this.Languages.Add(cinfo);

                if (cinfo.IetfLanguageTag == Properties.Settings.Default.Culture)
                {
                    _selectedLang = cinfo;
                    NotifyPropertyChanged(x => SelectedLanguage);
                }
            }
        }

        #endregion

        #region Notifications

        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;
        public event EventHandler<NotificationEventArgs<bool, Setting>> NewTemplateNotice;

        #endregion

        #region Properties

        private ObservableCollection<Setting> _templates;
        public ObservableCollection<Setting> Templates
        {
            get { return _templates; }
            set
            {
                _templates = value;
                NotifyPropertyChanged(m => m.Templates);
            }
        }

        private ObservableCollection<string> _printers;
        public ObservableCollection<string> Printers
        {
            get { return _printers; }
            set
            {
                _printers = value;
                NotifyPropertyChanged(m => m.Printers);
            }
        }

        private ObservableCollection<CultureInfo> _langs;
        public ObservableCollection<CultureInfo> Languages
        {
            get { return _langs; }
            set
            {
                _langs = value;
                NotifyPropertyChanged(m => m.Languages);
            }
        }

        private CultureInfo _selectedLang;
        public CultureInfo SelectedLanguage
        {
            get { return _selectedLang; }
            set
            {
                _selectedLang = value;

                Properties.Settings.Default.Culture = _selectedLang.IetfLanguageTag;

                NotifyPropertyChanged(m => m.SelectedLanguage);
            }
        }

        private Order _order;
        public Order Order
        {
            get { return _order; }
            set
            {
                _order = value;
                NotifyPropertyChanged(m => m.Order);
            }
        }

        private Setting selectedTemplate;
        public Setting SelectedTemplate
        {
            get { return selectedTemplate; }
            set
            {
                selectedTemplate = value;
                NotifyPropertyChanged(m => m.SelectedTemplate);
                PreviewTemplate();
            }
        }

        private object _previewTemplateContent;
        public object PreviewTemplateContent
        {
            get
            {
                return _previewTemplateContent;
            }
            set
            {
                _previewTemplateContent = value;
                NotifyPropertyChanged(x => x.PreviewTemplateContent);
            }
        }

        public string CurrencySymbol
        {
            get { return Properties.Settings.Default.CurrencySymbol; }
            set
            {
                Properties.Settings.Default.CurrencySymbol = value;
                NotifyPropertyChanged(m => m.CurrencySymbol);
            }
        }

        public bool AutoPrint
        {
            get { return Properties.Settings.Default.AutoPrint; }
            set
            {
                Properties.Settings.Default.AutoPrint = value;
                NotifyPropertyChanged(m => m.AutoPrint);
            }
        }

        public double CustomerPointsAmount
        {
            get { return Properties.Settings.Default.CustomerPointsAmount; }
            set
            {
                Properties.Settings.Default.CustomerPointsAmount = value;
                NotifyPropertyChanged(m => m.CustomerPointsAmount);
            }
        }

        public double PurchaseDiscount
        {
            get { return Properties.Settings.Default.PurchaseDiscount; }
            set
            {
                Properties.Settings.Default.PurchaseDiscount = value;
                NotifyPropertyChanged(m => m.PurchaseDiscount);
            }
        }

        public int LineHeight
        {
            get { return Properties.Settings.Default.LineHeight; }
            set
            {
                Properties.Settings.Default.LineHeight = value;
                NotifyPropertyChanged(m => m.LineHeight);
            }
        }

        public int ExtraHeight
        {
            get { return Properties.Settings.Default.ExtraHeight; }
            set
            {
                Properties.Settings.Default.ExtraHeight = value;
                NotifyPropertyChanged(m => m.ExtraHeight);
            }
        }

        public double TicketWidth
        {
            get { return Properties.Settings.Default.TicketWidth; }
            set
            {
                Properties.Settings.Default.TicketWidth = value;
                NotifyPropertyChanged(m => m.TicketWidth);
            }
        }

        public double TicketHeight
        {
            get { return Properties.Settings.Default.TicketHeight; }
            set
            {
                Properties.Settings.Default.TicketWidth = value;
                NotifyPropertyChanged(m => m.TicketHeight);
            }
        }

        public double ReportWidth
        {
            get { return Properties.Settings.Default.ReportWidth; }
            set
            {
                Properties.Settings.Default.ReportWidth = value;
                NotifyPropertyChanged(m => m.ReportWidth);
            }
        }

        public double ReportHeight
        {
            get { return Properties.Settings.Default.ReportHeight; }
            set
            {
                Properties.Settings.Default.ReportHeight = value;
                NotifyPropertyChanged(m => m.ReportHeight);
            }
        }

        public string TicketPrinter
        {
            get { return Properties.Settings.Default.TicketPrinter; }
            set
            {
                Properties.Settings.Default.TicketPrinter = value;
                NotifyPropertyChanged(m => m.TicketPrinter);
            }
        }

        public string ReportPrinter
        {
            get { return Properties.Settings.Default.ReportPrinter; }
            set
            {
                Properties.Settings.Default.ReportPrinter = value;
                NotifyPropertyChanged(m => m.ReportPrinter);
            }
        }

        public string BarcodePrinter
        {
            get { return Properties.Settings.Default.BarcodePrinter; }
            set
            {
                Properties.Settings.Default.BarcodePrinter = value;
                NotifyPropertyChanged(m => m.BarcodePrinter);
            }
        }

        private DateTime customerPointDate;
        public DateTime CustomerPointStartDate
        {
            get { return customerPointDate; }
            set
            {
                customerPointDate = value;
                NotifyPropertyChanged(m => m.CustomerPointStartDate);
            }
        }

        #endregion

        #region Methods

        private DelegateCommand saveCommand;
        public DelegateCommand SaveCommand
        {
            get
            {
                return saveCommand ?? (saveCommand = new DelegateCommand(() =>
                {
                    try
                    {
                        Properties.Settings.Default.Save();

                        this.settingService.Update(new Setting
                        {
                            Id = Constants.CustomerPointsStartDateId,
                            Name = "CustomerPointsStartDate",
                            Value = this.CustomerPointStartDate.ToString(),
                            Category = Constants.Category.Application,
                            UpdatedOn = DateTime.Now,
                            Status = true
                        });
                    }
                    catch (Exception error)
                    {
                        NotifyError("Error while saving settings", error);
                    }
                }));
            }
            private set { saveCommand = value; }
        }

        private DelegateCommand resetCommand;
        public DelegateCommand ResetCommand
        {
            get
            {
                return resetCommand ?? (resetCommand = new DelegateCommand(() =>
                {
                    Properties.Settings.Default.Reset();

                    NotifyPropertyChanged(x => CurrencySymbol);
                    NotifyPropertyChanged(x => AutoPrint);
                    NotifyPropertyChanged(x => SelectedLanguage);
                    NotifyPropertyChanged(x => CustomerPointsAmount);
                    NotifyPropertyChanged(x => TicketHeight);
                    NotifyPropertyChanged(x => TicketWidth);
                    NotifyPropertyChanged(x => ReportHeight);
                    NotifyPropertyChanged(x => ReportWidth);
                    NotifyPropertyChanged(x => ReportPrinter);
                    NotifyPropertyChanged(x => TicketPrinter);
                    NotifyPropertyChanged(x => BarcodePrinter);
                }));
            }
            private set { resetCommand = value; }
        }

        private DelegateCommand saveTemplateCommand;
        public DelegateCommand SaveTemplateCommand
        {
            get
            {
                return saveTemplateCommand ?? (saveTemplateCommand = new DelegateCommand(() =>
                {
                    if (this.SelectedTemplate != null)
                    {
                        this.settingService.Update(this.SelectedTemplate);
                    }
                }));
            }
            private set { saveTemplateCommand = value; }
        }

        private DelegateCommand newTemplateCommand;
        public DelegateCommand NewTemplateCommand
        {
            get
            {
                return newTemplateCommand ?? (newTemplateCommand = new DelegateCommand(() =>
                {
                    if (this.NewTemplateNotice != null)
                    {
                        this.NewTemplateNotice(this, new NotificationEventArgs<bool, Setting>("NEWTEMPLATE", true, (s) =>
                        {
                            if (s != null && !string.IsNullOrEmpty(s.Name))
                            {
                                s.Category = "Templates";
                                this.settingService.Add(s);
                                this.Templates.Add(s);
                            }
                        }));
                    }
                }));
            }
            private set { newTemplateCommand = value; }
        }

        private DelegateCommand deleteTemplateCommand;
        public DelegateCommand DeleteTemplateCommand
        {
            get
            {
                return deleteTemplateCommand ?? (deleteTemplateCommand = new DelegateCommand(() =>
                {
                    if (this.SelectedTemplate != null && this.Templates.Contains(this.SelectedTemplate))
                    {
                        this.Templates.Remove(this.SelectedTemplate);
                        this.settingService.Delete(this.SelectedTemplate.Id);
                    }
                }));
            }
            private set { deleteTemplateCommand = value; }
        }

        private DelegateCommand previewTemplateCommand;
        public DelegateCommand PreviewTemplateCommand
        {
            get
            {
                return previewTemplateCommand ?? (previewTemplateCommand = new DelegateCommand(() =>
                {
                    PreviewTemplate();
                }));
            }
            private set { previewTemplateCommand = value; }
        }

        private void PreviewTemplate()
        {
            if (string.IsNullOrEmpty(SelectedTemplate.Value))
                return;

            try
            {
                var xml = this.SelectedTemplate.Value;

                if (this.Order != null)
                {
                    this.PreviewTemplateContent = PrintHelper.GetPrintDocument(xml, this.Order);
                }
            }
            catch (Exception ex)
            {
                NotifyError("Error while preview", ex);
            }
        }

        #endregion

        #region Helpers

        private void NotifyError(string message, Exception error)
        {
            SendMessage(MessageTokens.GlobalNotification, new NotificationEventArgs(message));
            LogService.Error(message, error);
            Notify(ErrorNotice, new NotificationEventArgs<Exception>(message, error));
        }

        private static object DeserializeXaml(string xaml)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(xaml);
                stream.Write(bytes, 0, bytes.Length);
                stream.Position = 0;
                return XamlReader.Load(stream);
            }
        }

        #endregion
    }
}