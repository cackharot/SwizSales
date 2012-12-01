using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwizSales.Core.Model;

namespace SwizSales.Core.ServiceContracts
{
    public interface IReportService
    {
        double GetCusomerTotalAmount(Guid customerId);

        Dictionary<Guid, double> GetCusomerTotalAmount(IEnumerable<Guid> customerIds);

        Dictionary<DateTime, double> GetSalesReport(OrderSearchCondition orderSearchCondition);
    }
}
