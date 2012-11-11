using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SwizSales.Core.ServiceContracts
{
    public interface IReportService
    {
        double GetCusomerTotalAmount(Guid customerId);
    }
}
