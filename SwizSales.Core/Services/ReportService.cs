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

        public Dictionary<DateTime, double> GetSalesReport(OrderSearchCondition orderSearchCondition)
        {
            Dictionary<DateTime, double> results = new Dictionary<DateTime, double>();

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    EntityConnection entityConn = (EntityConnection)ctx.Connection;
                    using (SqlConnection sqlConn = (SqlConnection)entityConn.StoreConnection)
                    {
                        SqlCommand cmd = new SqlCommand();
                        cmd.Connection = sqlConn;

                        var query = new StringBuilder("SELECT DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) as odate,");
                        query.Append(" COALESCE(SUM(o.BillAmount),0) AS TotalAmount FROM Orders AS o WHERE 1=1");

                        if (orderSearchCondition.ToOrderDate == orderSearchCondition.FromOrderDate)
                        {
                            query.Append(" AND DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) = @date");
                            cmd.Parameters.AddWithValue("@date", orderSearchCondition.FromOrderDate);
                        }
                        else
                        {
                            query.Append(" AND DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) >= @fromdate");
                            query.Append(" AND DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) <= @todate");
                            cmd.Parameters.AddWithValue("@fromdate", orderSearchCondition.FromOrderDate);
                            cmd.Parameters.AddWithValue("@todate", orderSearchCondition.ToOrderDate);
                        }

                        query.Append(" GROUP BY DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) ORDER BY odate ASC");

                        cmd.CommandText = query.ToString();
                        cmd.CommandType = CommandType.Text;

                        if (sqlConn.State != ConnectionState.Open)
                        {
                            sqlConn.Open();
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(reader.GetDateTime(0), reader.GetDouble(1));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while calculating sales report", ex);
                throw new ArgumentException("Error while calculating sales report", ex);
            }

            return results;
        }
    }
}
