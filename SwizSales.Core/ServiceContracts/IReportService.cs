using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwizSales.Core.Model;

namespace SwizSales.Core.ServiceContracts
{
    public interface IReportService
    {
        double GetCusomerTotalAmount(Guid customerId, DateTime fromDate);

        int GetTotalOrders(DateTime fromDate, DateTime toDate);

        Dictionary<Guid, double> GetCusomerTotalAmount(IEnumerable<Guid> customerIds, DateTime fromDate);

        Dictionary<DateTime, double> GetSalesReport(OrderSearchCondition orderSearchCondition);

        ICollection<Product> GetTopProducts(int count, DateTime fromDate, DateTime toDate);

        ICollection<Customer> GetTopCustomers(int count, DateTime fromDate, DateTime toDate);
    }
}
