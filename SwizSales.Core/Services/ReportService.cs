using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwizSales.Core.ServiceContracts;
using SwizSales.Core.Library;
using SwizSales.Core.Model;

namespace SwizSales.Core.Services
{
    public class ReportService : IReportService
    {
        public double GetCusomerTotalAmount(Guid customerId)
        {
            if (customerId == Guid.Empty)
                throw new ArgumentNullException("customerId");

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    var query = ctx.ExecuteStoreQuery<double>("SELECT COALESCE(SUM(op.PaidAmount),0) FROM Payments op INNER JOIN Orders o on o.Id = op.OrderId AND o.CustomerId = {0}", customerId);
                    return query.First();
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while calculating customer order amount", ex);
                throw new ArgumentException("Error while calculating customer order amount", ex);
            }
        }
    }
}
