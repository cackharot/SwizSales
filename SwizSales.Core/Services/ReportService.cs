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
using System.Collections.ObjectModel;
using System.Data.Objects;
using System.Data.Objects.SqlClient;

namespace SwizSales.Core.Services
{
    public class ReportService : IReportService
    {
        public double GetCusomerTotalAmount(Guid customerId, DateTime fromDate)
        {
            if (customerId == Guid.Empty)
                throw new ArgumentNullException("customerId");

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    var queryString = new StringBuilder("SELECT COALESCE(SUM(op.PaidAmount),0) FROM Payments op");
                    queryString.Append(" INNER JOIN Orders o on o.Id = op.OrderId AND o.CustomerId = {0}")
                               .Append(" AND (DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) >= {1} AND DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) <= DATEADD(dd, DATEDIFF(dd, 0, GETDATE()), 0))");
                    var query = ctx.ExecuteStoreQuery<double>(queryString.ToString(), customerId, fromDate);
                    return query.First();
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while calculating customer order amount", ex);
                throw new ArgumentException("Error while calculating customer order amount", ex);
            }
        }

        public Dictionary<Guid, double> GetCusomerTotalAmount(IEnumerable<Guid> customerIds, DateTime fromDate)
        {
            if (customerIds == null || customerIds.Count() == 0)
                throw new ArgumentNullException("customerIds");

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    var query = "SELECT o.CustomerId AS CustomerId, COALESCE(SUM(op.PaidAmount),0) AS TotalAmount FROM Payments op";
                    query += " INNER JOIN Orders o on o.Id = op.OrderId AND o.CustomerId IN ('" + string.Join("','", customerIds) + "') ";
                    query += " AND (DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) >= @startDate AND DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) <= DATEADD(dd, DATEDIFF(dd, 0, GETDATE()), 0))";
                    query += " GROUP BY o.CustomerId";
                    EntityConnection entityConn = (EntityConnection)ctx.Connection;
                    using (SqlConnection sqlConn = (SqlConnection)entityConn.StoreConnection)
                    {
                        SqlCommand cmd = new SqlCommand(query, sqlConn);
                        cmd.CommandType = CommandType.Text;

                        cmd.Parameters.AddWithValue("@startDate", fromDate);

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

        public ICollection<Product> GetTopProducts(int count, DateTime fromDate, DateTime toDate)
        {
            var lstProducts = new List<Product>();
            var pids = new Dictionary<Guid, Tuple<double, double>>();

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    EntityConnection entityConn = (EntityConnection)ctx.Connection;
                    using (SqlConnection sqlConn = (SqlConnection)entityConn.StoreConnection)
                    {
                        SqlCommand cmd = new SqlCommand();
                        cmd.Connection = sqlConn;

                        var queryString = new StringBuilder("SELECT TOP " + count + " ProductId, SUM(Quantity) AS TQ, SUM(Quantity*Price) AS TA FROM OrderDetails od");
                        queryString.Append(" INNER JOIN Orders o ON o.Id = od.OrderId");
                        queryString.Append(" AND DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) >= @fromdate");
                        queryString.Append(" AND DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) <= @todate");
                        queryString.Append(" WHERE ProductId IS NOT NULL AND ProductId NOT IN ('56C9FA56-F363-E011-AF1E-001E90ED96B8','989DA90C-1431-E111-B051-001E90ED96B8')");
                        queryString.Append(" GROUP BY ProductId ORDER BY TQ DESC");

                        cmd.Parameters.AddWithValue("@fromdate", fromDate);
                        cmd.Parameters.AddWithValue("@todate", toDate);

                        cmd.CommandText = queryString.ToString();
                        cmd.CommandType = CommandType.Text;

                        if (sqlConn.State != ConnectionState.Open)
                        {
                            sqlConn.Open();
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                pids.Add(reader.GetGuid(0), Tuple.Create<double, double>(reader.GetDouble(1), reader.GetDouble(2)));
                            }
                        }
                    }
                }

                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.ContextOptions.LazyLoadingEnabled = false;
                    ctx.Products.MergeOption = MergeOption.NoTracking;
                    var ids = pids.Keys.ToList();
                    lstProducts = ctx.Products.Include("Supplier").Include("Category").Include("TaxCategory").Where(x => ids.Contains(x.Id)).ToList();

                    foreach (var p in lstProducts)
                    {
                        p.Sold = pids[p.Id].Item1;
                        p.SubTotal = pids[p.Id].Item2;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while searching top products", ex);
                throw new ArgumentException("Error while searching top products", ex);
            }

            return new Collection<Product>(lstProducts);
        }

        public ICollection<Customer> GetTopCustomers(int count, DateTime fromDate, DateTime toDate)
        {
            var lstCustomers = new List<Customer>();
            var cids = new Dictionary<Guid, double>();

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    EntityConnection entityConn = (EntityConnection)ctx.Connection;
                    using (SqlConnection sqlConn = (SqlConnection)entityConn.StoreConnection)
                    {
                        SqlCommand cmd = new SqlCommand();
                        cmd.Connection = sqlConn;

                        var queryString = new StringBuilder("SELECT TOP " + count + " CustomerId, SUM(od.Price*od.Quantity) AS TQ FROM Orders o");
                        queryString.Append(" INNER JOIN OrderDetails od ON od.OrderId = o.Id");
                        queryString.Append(" WHERE DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) >= @fromdate");
                        queryString.Append(" AND DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) <= @todate");
                        queryString.Append(" AND CustomerId NOT IN ('b004dfc3-e53f-4e6b-952f-67ceae170da6')");
                        queryString.Append(" GROUP BY CustomerId ORDER BY TQ DESC");

                        cmd.Parameters.AddWithValue("@fromdate", fromDate);
                        cmd.Parameters.AddWithValue("@todate", toDate);

                        cmd.CommandText = queryString.ToString();
                        cmd.CommandType = CommandType.Text;

                        if (sqlConn.State != ConnectionState.Open)
                        {
                            sqlConn.Open();
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                cids.Add(reader.GetGuid(0), reader.GetDouble(1));
                            }
                        }
                    }
                }

                if (cids != null && cids.Count > 0)
                {
                    using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                    {
                        ctx.ContextOptions.LazyLoadingEnabled = false;
                        ctx.Customers.MergeOption = MergeOption.NoTracking;
                        var ids = cids.Keys.ToList();
                        lstCustomers = ctx.Customers.Include("ContactDetail").Where(x => ids.Contains(x.Id) && x.Status == true).ToList();

                        foreach (var p in lstCustomers)
                        {
                            p.TotalAmount = cids[p.Id];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while searching top customers", ex);
                throw new ArgumentException("Error while searching top customers", ex);
            }

            return new Collection<Customer>(lstCustomers);
        }

        public int GetTotalOrders(DateTime fromDate, DateTime toDate)
        {
            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    var queryString = new StringBuilder("SELECT COUNT(Id) FROM Orders o");
                    queryString.Append(" WHERE (DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) >= {0} AND DATEADD(dd, DATEDIFF(dd, 0, o.OrderDate), 0) <= {1})");
                    var query = ctx.ExecuteStoreQuery<int>(queryString.ToString(), fromDate, toDate);
                    return query.First();
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while calculating total orders", ex);
                throw new ArgumentException("Error while calculating total orders", ex);
            }
        }
    }
}
