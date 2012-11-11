using System;
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

        public SettingsViewModel()
        {
            LoadPrinters();
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

        // TODO: Add events to notify the view or obtain data from the view
        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;

        #endregion

        #region Properties

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

        #endregion
        
        #region Helpers

        private void NotifyError(string message, Exception error)
        {
            SendMessage(MessageTokens.GlobalNotification, new NotificationEventArgs(message));
            LogService.Error(message, error);
            Notify(ErrorNotice, new NotificationEventArgs<Exception>(message, error));
        }

        #endregion
    }
}