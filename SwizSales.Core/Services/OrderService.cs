using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwizSales.Core.Model;
using SwizSales.Core.ServiceContracts;
using System.Collections.ObjectModel;
using SwizSales.Core.Library;
using System.Data.Objects;
using System.Transactions;

namespace SwizSales.Core.Services
{
    public class OrderService : IOrderService
    {
        public Collection<Order> Search(OrderSearchCondition condition)
        {
            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.ContextOptions.LazyLoadingEnabled = false;
                    ctx.Orders.MergeOption = MergeOption.NoTracking;

                    var items = ctx.Orders.Include("Customer")
                        .Include("Customer.ContactDetail")
                        .Include("Payments")
                        .Include("Employee").Include("Employee.ContactDetail")
                        .Include("OrderDetails").Where(x => x.Status == true);

                    if (condition.OrderNo > 0)
                    {
                        items = items.Where(x => x.BillNo == condition.OrderNo);
                    }
                    else
                    {
                        if (condition.CustomerId != Guid.Empty)
                        {
                            items = items.Where(x => x.CustomerId == condition.CustomerId);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(condition.CustomerMobile))
                            {
                                items = items.Where(x => x.Customer.ContactDetail.Mobile.Contains(condition.CustomerMobile));
                            }

                            if (!string.IsNullOrEmpty(condition.CustomerName))
                            {
                                items = items.Where(x => x.Customer.ContactDetail.ContactName.Contains(condition.CustomerName));
                            }

                            if (!string.IsNullOrEmpty(condition.CustomerNo))
                            {
                                items = items.Where(x => x.Customer.SSN.Contains(condition.CustomerNo));
                            }
                        }

                        if (condition.EmployeeId != Guid.Empty)
                        {
                            items = items.Where(x => x.EmployeeId == condition.EmployeeId);
                        }
                        
                        if (condition.MinAmount >= 0 && condition.MaxAmount >= 0)
                        {
                            items.Where(x => x.BillAmount >= condition.MinAmount && x.BillAmount <= condition.MaxAmount);
                        }
                        else if (condition.MinAmount <= 0 && condition.MaxAmount > 0)
                        {
                            items.Where(x => x.BillAmount <= condition.MaxAmount);
                        }
                        else if (condition.MinAmount > 0 && condition.MaxAmount <= 0)
                        {
                            items.Where(x => x.BillAmount >= condition.MinAmount);
                        }
                        
                        if ((condition.FromOrderDate != DateTime.MinValue && condition.ToOrderDate != DateTime.MinValue)
                            && (condition.FromOrderDate == condition.ToOrderDate))
                        {
                            items.Where(x => x.OrderDate == condition.FromOrderDate);
                        }
                        else if (condition.FromOrderDate != DateTime.MinValue && condition.ToOrderDate != DateTime.MinValue)
                        {
                            items.Where(x => x.OrderDate >= condition.FromOrderDate && x.OrderDate <= condition.ToOrderDate);
                        }
                        else if (condition.FromOrderDate != DateTime.MinValue && condition.ToOrderDate >= DateTime.MinValue)
                        {
                            items.Where(x => x.OrderDate <= condition.ToOrderDate);
                        }
                        else if (condition.FromOrderDate >= DateTime.MinValue && condition.ToOrderDate != DateTime.MinValue)
                        {
                            items.Where(x => x.OrderDate >= condition.FromOrderDate);
                        }
                    }

                    items = items.OrderByDescending(x => x.BillNo).OrderByDescending(x => x.OrderDate);

                    if (condition.PageNo > 0 && condition.PageSize > 0)
                    {
                        items = items.Skip((condition.PageNo - 1) * condition.PageSize).Take(condition.PageSize);
                    }
                    else
                    {
                        items.Take(1000);
                    }

                    return new Collection<Order>(items.ToList());
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while searching orders", ex);
                throw new ArgumentException("Error while searching orders", ex);
            }
        }

        public Order GetOrderById(Guid id)
        {

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.Orders.MergeOption = MergeOption.NoTracking;
                    var entity = ctx.Orders.Include("Customer").Include("Customer.ContactDetail")
                        .Include("OrderDetails")
                        .Include("Payments")
                        .Include("Employee").Include("Employee.ContactDetail").SingleOrDefault(x => x.Id.Equals(id));
                    return entity;
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while fetching order", ex);
                throw new ArgumentException("Error while fetching order", ex);
            }
        }

        public Order GetOrderByOrderNo(int orderNo)
        {
            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.Orders.MergeOption = MergeOption.NoTracking;
                    return ctx.Orders.Include("Customer").Include("Customer.ContactDetail")
                        .Include("OrderDetails")
                        .Include("Payments")
                        .Include("Employee").Include("Employee.ContactDetail").Where(x => x.BillNo == orderNo && x.Status == true).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while fetching order by bill no", ex);
                throw new ArgumentException("Error while fetching order by bill no", ex);
            }
        }

        public Order NewOrder(Guid customerId, Guid employeeId)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required))
            {
                var entity = new Order();
                entity.OrderDate = DateTime.Now;
                entity.SystemId = Environment.MachineName;
                entity.Status = true;

                using (var ctx = new OpenPOSDbEntities())
                {
                    entity.BillNo = ctx.Orders.Max(x => x.BillNo) + 1;
                }

                entity.CustomerId = customerId;
                entity.EmployeeId = employeeId;

                Add(entity);

                entity = GetOrderById(entity.Id);

                scope.Complete();

                return entity;
            }
        }

        public Guid Add(Order entity)
        {
            if (entity.BillNo <= 0)
                throw new ArgumentException("Order No cannot be empty!");

            if (entity.CustomerId == Guid.Empty)
                throw new ArgumentException("Order Customer cannot be empty!");

            if (entity.EmployeeId == Guid.Empty)
                throw new ArgumentException("Order Employee cannot be empty!");

            if (CheckDuplicateBillNo(entity.Id, entity.BillNo))
                throw new ArgumentException("Duplicate Order No found!");

            using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
            {
                try
                {
                    ctx.Orders.MergeOption = MergeOption.NoTracking;
                    ctx.Orders.AddObject(entity);
                    ctx.SaveChanges();
                    var id = entity.Id;
                    ctx.Detach(entity);
                    return id;
                }
                catch (Exception ex)
                {
                    LogService.Error("Error while adding order", ex);
                    throw new ArgumentException("Error while adding new order!", ex);
                }
            }
        }

        private bool CheckDuplicateBillNo(Guid orderId, int billNo)
        {
            using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
            {
                var query = ctx.Orders.Where(x => x.BillNo == billNo);
                if (orderId != Guid.Empty)
                {
                    query = query.Where(x => x.Id != orderId);
                }

                return query.Count() > 0;
            }
        }

        public void Update(Order entity)
        {
            if (entity.Id.Equals(Guid.Empty))
                throw new ArgumentException("Order Id cannot be empty!");

            if (entity.BillNo <= 0)
                throw new ArgumentException("Order No cannot be empty!");

            if (entity.CustomerId == Guid.Empty)
                throw new ArgumentException("Order Customer cannot be empty!");

            if (entity.EmployeeId == Guid.Empty)
                throw new ArgumentException("Order Employee cannot be empty!");

            if (CheckDuplicateBillNo(entity.Id, entity.BillNo))
                throw new ArgumentException("Duplicate Order No found!");

            using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
            {
                try
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Required))
                    {
                        ctx.ExecuteStoreCommand("DELETE FROM OrderDetails WHERE OrderId = {0};", entity.Id);
                        ctx.ExecuteStoreCommand("DELETE FROM Payments WHERE OrderId = {0};", entity.Id);

                        foreach (var od in entity.OrderDetails)
                        {
                            ctx.AttachTo("OrderDetails", od);
                            ctx.ObjectStateManager.ChangeObjectState(od, System.Data.EntityState.Added);
                        }

                        foreach (var payment in entity.Payments)
                        {
                            ctx.AttachTo("Payments", payment);
                            ctx.ObjectStateManager.ChangeObjectState(payment, System.Data.EntityState.Added);
                        }

                        ctx.AttachTo("Orders", entity);
                        ctx.ObjectStateManager.ChangeObjectState(entity, System.Data.EntityState.Modified);

                        ctx.SaveChanges();

                        scope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    LogService.Error("Error while updating order", ex);
                    throw new ArgumentException("Error while updating order!", ex);
                }
            }
        }

        public void UpdateOrderCustomer(Order entity)
        {
            if (entity.Id.Equals(Guid.Empty))
                throw new ArgumentException("Order Id cannot be empty!");

            if (entity.BillNo <= 0)
                throw new ArgumentException("Order No cannot be empty!");

            if (entity.CustomerId == Guid.Empty || entity.Customer == null)
                throw new ArgumentException("Order Customer cannot be empty!");

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.ExecuteStoreCommand("UPDATE Orders SET CustomerId = {0} WHERE Id = {1};", entity.CustomerId, entity.Id);
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while updating order customer", ex);
                throw new ArgumentException("Error while updating order customer!", ex);
            }
        }

        public void Delete(Guid id)
        {
            if (id.Equals(Guid.Empty))
                throw new ArgumentException("Order id cannot be empty!");

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    var entity = GetOrderById(id);

                    if (entity != null)
                    {
                        ctx.Orders.Attach(entity);
                        ctx.Orders.DeleteObject(entity);
                        ctx.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while deleting order", ex);
                throw new ArgumentException("Error while deleting order!", ex);
            }
        }
    }
}

