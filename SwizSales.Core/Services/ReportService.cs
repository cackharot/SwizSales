using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwizSales.Core.ServiceContracts;
using SwizSales.Core.Library;
using SwizSales.Core.Model;
using System.Data;
using System.Data.EntityClient;
using System.Data.SqlClient;

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

        public Dictionary<Guid, double> GetCusomerTotalAmount(IEnumerable<Guid> customerIds)
        {
            if (customerIds == null || customerIds.Count() == 0)
                throw new ArgumentNullException("customerIds");

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    var query = "SELECT o.CustomerId AS CustomerId, COALESCE(SUM(op.PaidAmount),0) AS TotalAmount FROM Payments op INNER JOIN Orders o on o.Id = op.OrderId AND o.CustomerId IN ('" + string.Join("','", customerIds) + "') GROUP BY o.CustomerId";
                    EntityConnection entityConn = (EntityConnection)ctx.Connection;
                    using (SqlConnection sqlConn = (SqlConnection)entityConn.StoreConnection)
                    {
                        SqlCommand cmd = new SqlCommand(query, sqlConn);
                        cmd.CommandType = CommandType.Text;

                        if (sqlConn.State != ConnectionState.Open)
                        {
                            sqlConn.Open();
                        }

                        Dictionary<Guid, double> results = new Dictionary<Guid, double>();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(reader.GetGuid(0), reader.GetDouble(1));
                            }
                        }

                        return results;
                    }
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
