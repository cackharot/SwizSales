using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace SwizSales.Core.Library
{
    public static class ApplicationSettings
    {
        static ApplicationSettings()
        {
             PuchaseDiscount = Convert.ToDouble(ConfigurationManager.AppSettings["PurchaseDiscount"]);
        }

        public static double PuchaseDiscount { get; set; }
    }
}
