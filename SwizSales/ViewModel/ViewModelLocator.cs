/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:SwizSales.ViewModel"
                                   x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"
*/

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// Toolkit namespace
using SimpleMvvmToolkit;

using SwizSales.Core.ServiceContracts;
using SwizSales.Core.Services;

namespace SwizSales.ViewModel
{
    /// <summary>
    /// This class creates ViewModels on demand for Views, supplying a
    /// ServiceAgent to the ViewModel if required.
    /// <para>
    /// Place the ViewModelLocator in the App.xaml resources:
    /// </para>
    /// <code>
    /// &lt;Application.Resources&gt;
    ///     &lt;vm:ViewModelLocator xmlns:vm="clr-namespace:SwizSales.ViewModel"
    ///                                  x:Key="Locator" /&gt;
    /// &lt;/Application.Resources&gt;
    /// </code>
    /// <para>
    /// Then use:
    /// </para>
    /// <code>
    /// DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"
    /// </code>
    /// <para>
    /// Use the <strong>mvvmlocator</strong> or <strong>mvvmlocatornosa</strong>
    /// code snippets to add ViewModels to this locator.
    /// </para>
    /// </summary>
    public class ViewModelLocator
    {
        // TODO: Use mvvmlocator or mvvmlocatornosa code snippets
        // to add ViewModels to the locator.
        // Create MainViewModel on demand
        public MainViewModel MainViewModel
        {
            get
            {
                //ICustomerService serviceAgent = new CustomerService();
                //return new MainViewModel(serviceAgent);
                return new MainViewModel();
            }
        }

        // Create CustomerViewModel on demand
        public CustomerViewModel CustomerViewModel
        {
            get
            {
                ICustomerService serviceAgent = new CustomerService();
                return new CustomerViewModel(serviceAgent);
            }
        }

        public ProductViewModel ProductViewModel
        {
            get
            {
                IProductService serviceAgent = new ProductService();
                return new ProductViewModel(serviceAgent);
            }
        }

        // Create SalesViewModel on demand
        public SalesViewModel SalesViewModel
        {
            get
            {
                IOrderService serviceAgent = new OrderService();
                IProductService productService = new ProductService();
                return new SalesViewModel(serviceAgent, productService);
            }
        }

        public TicketPrinterViewModel TicketPrinterViewModel
        {
            get
            {
                return new TicketPrinterViewModel();
            }
        }

        public SettingsViewModel SettingsViewModel
        {
            get
            {
                return new SettingsViewModel();
            }
        }

        public PurchaseViewModel PurchaseViewModel
        {
            get
            {
                return new PurchaseViewModel(new StockService());
            }
        }

        public NotificationViewModel NotificationViewModel
        {
            get
            {
                return new NotificationViewModel();
            }
        }
    }
}