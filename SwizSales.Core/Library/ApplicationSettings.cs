using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SwizSales.Core.Library
{
    public static class ApplicationSettings
    {
        public static string XpsTicketPath { get; set; }

        public static double PageHeight { get; set; }

        public static double PageWidth { get; set; }

        public static int LineHeight { get; set; }

        public static int ExtraHeight { get; set; }

        public static bool PrintItemNameUpperCase { get; set; }
        
        public static string PrintTicketTemplate { get; set; }

        static ApplicationSettings()
        {
            PrintTicketTemplate = "ReportLayouts\\PrintTicketTemplate.xml";
            XpsTicketPath = "currentTicket.xps";
            PageHeight = 720;
            PageWidth = 300;
            LineHeight = 25;
            ExtraHeight = 300;
            PrintItemNameUpperCase = false;            
            PuchaseDiscount = 10.0;
            DefaultEmployeeId = Guid.Parse("9DCD414D-156C-4465-89ED-095E5BB3D88A"); 
            DefaultCustomerId = Guid.Parse("B004DFC3-E53F-4E6B-952F-67CEAE170DA6"); 
            DefaultCategoryId = Guid.Parse("25125713-62BE-4927-8623-B784C49CB01A");
            DefaultTaxCategoryId = Guid.Parse("8CD9785E-F59C-4013-8C9C-FDDE4B144738");
        }

        public static double PuchaseDiscount { get; set; }

        public static Guid DefaultCustomerId { get; set; }

        public static Guid DefaultEmployeeId { get; set; }

        public static Guid DefaultCategoryId { get; set; }

        public static Guid DefaultTaxCategoryId { get; set; }
    }
}
