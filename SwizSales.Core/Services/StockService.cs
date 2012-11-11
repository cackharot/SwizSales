using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwizSales.Core.ServiceContracts;
using SwizSales.Core.Model;
using SwizSales.Core.Library;
using System.Data.Objects;
using System.Collections.ObjectModel;
using System.Transactions;

namespace SwizSales.Core.Services
{
    public class StockService : IStockService
    {
        public ICollection<Purchase> Search(PurchaseSearchCondition condition)
        {
            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.ContextOptions.LazyLoadingEnabled = false;
                    ctx.Purchases.MergeOption = MergeOption.NoTracking;

                    var items = ctx.Purchases.Include("Supplier")
                        .Include("Supplier.ContactDetail")
                        .Include("PurchasePayments")
                        .Include("Employee").Include("Employee.ContactDetail")
                        .Include("PurchaseDetails").Where(x => x.Status == true);

                    if (!string.IsNullOrEmpty(condition.BillNo))
                    {
                        items = items.Where(x => x.BillNo == condition.BillNo);
                    }
                    else if (!string.IsNullOrEmpty(condition.SupplierName))
                    {
                        items = items.Where(x => x.Supplier.Name.Contains(condition.SupplierName));
                    }
                    else if (condition.SupplierId != Guid.Empty)
                    {
                        items = items.Where(x => x.SupplierId == condition.SupplierId);
                    }
                    else if (condition.EmployeeId != Guid.Empty)
                    {
                        items = items.Where(x => x.EmployeeId == condition.EmployeeId);
                    }
                    else if (condition.MinAmount >= 0 && condition.MaxAmount >= 0)
                    {
                        items.Where(x => x.TotalAmount >= condition.MinAmount && x.TotalAmount <= condition.MaxAmount);
                    }
                    else if (condition.MinAmount < 0 && condition.MaxAmount >= 0)
                    {
                        items.Where(x => x.TotalAmount <= condition.MaxAmount);
                    }
                    else if (condition.MinAmount >= 0 && condition.MaxAmount < 0)
                    {
                        items.Where(x => x.TotalAmount >= condition.MinAmount);
                    }
                    else if ((condition.FromDate != DateTime.MinValue && condition.ToDate != DateTime.MinValue)
                        && (condition.FromDate == condition.ToDate))
                    {
                        items.Where(x => x.PurchaseDate == condition.FromDate);
                    }
                    else if (condition.FromDate != DateTime.MinValue && condition.ToDate != DateTime.MinValue)
                    {
                        items.Where(x => x.PurchaseDate >= condition.FromDate && x.PurchaseDate <= condition.ToDate);
                    }
                    else if (condition.FromDate != DateTime.MinValue && condition.ToDate >= DateTime.MinValue)
                    {
                        items.Where(x => x.PurchaseDate <= condition.ToDate);
                    }
                    else if (condition.FromDate >= DateTime.MinValue && condition.ToDate != DateTime.MinValue)
                    {
                        items.Where(x => x.PurchaseDate >= condition.FromDate);
                    }


                    items = items.OrderByDescending(x => x.BillNo).OrderByDescending(x => x.PurchaseDate);

                    items = items.Skip((condition.PageNo - 1) * condition.PageSize).Take(condition.PageSize);

                    return new Collection<Purchase>(items.ToList());
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while searching Purchase", ex);
                throw new ArgumentException("Error while searching Purchase", ex);
            }
        }

        public Purchase GetPurchaseById(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentNullException("id");

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.Orders.MergeOption = MergeOption.NoTracking;
                    var entity = ctx.Purchases.Include("Supplier").Include("Supplier.ContactDetail")
                        .Include("PurchaseDetails")
                        .Include("PurchasePayments")
                        .Include("Employee").Include("Employee.ContactDetail").SingleOrDefault(x => x.Id.Equals(id));
                    return entity;
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while fetching Purchase", ex);
                throw new ArgumentException("Error while fetching Purchase", ex);
            }
        }

        private bool CheckDuplicateBillNo(Guid purchaseId, string billNo)
        {
            using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
            {
                var query = ctx.Purchases.Where(x => x.BillNo == billNo);
                if (purchaseId != Guid.Empty)
                {
                    query = query.Where(x => x.Id != purchaseId);
                }

                return query.Count() > 0;
            }
        }

        public Guid Add(Purchase entity)
        {
            if (string.IsNullOrEmpty(entity.BillNo))
                throw new ArgumentException("Purchase No cannot be empty!");

            if (entity.SupplierId == Guid.Empty)
                throw new ArgumentException("Purchase supplier cannot be empty!");

            if (entity.EmployeeId == Guid.Empty)
                throw new ArgumentException("Purchase Employee cannot be empty!");

            if (CheckDuplicateBillNo(entity.Id, entity.BillNo))
                throw new ArgumentException("Duplicate Purchase No found!");

            using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
            {
                try
                {
                    ctx.Purchases.MergeOption = MergeOption.NoTracking;
                    ctx.Purchases.AddObject(entity);
                    ctx.SaveChanges();
                    var id = entity.Id;
                    ctx.Detach(entity);
                    return id;
                }
                catch (Exception ex)
                {
                    LogService.Error("Error while adding Purchase", ex);
                    throw new ArgumentException("Error while adding new Purchase!", ex);
                }
            }
        }

        public void Update(Purchase entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            if (string.IsNullOrEmpty(entity.BillNo))
                throw new ArgumentException("Purchase No cannot be empty!");

            if (entity.SupplierId == Guid.Empty)
                throw new ArgumentException("Purchase supplier cannot be empty!");

            if (entity.EmployeeId == Guid.Empty)
                throw new ArgumentException("Purchase Employee cannot be empty!");

            if (CheckDuplicateBillNo(entity.Id, entity.BillNo))
                throw new ArgumentException("Duplicate Purchase No found!");

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Required))
                    {
                        ctx.ExecuteStoreCommand("DELETE FROM PurchaseDetails WHERE PurchaseId = {0};", entity.Id);
                        ctx.ExecuteStoreCommand("DELETE FROM PurchasePayments WHERE PurchaseId = {0};", entity.Id);

                        foreach (var od in entity.PurchaseDetails)
                        {
                            ctx.AttachTo("PurchaseDetails", od);
                            ctx.ObjectStateManager.ChangeObjectState(od, System.Data.EntityState.Added);
                        }

                        foreach (var payment in entity.PurchasePayments)
                        {
                            ctx.AttachTo("PurchasePayments", payment);
                            ctx.ObjectStateManager.ChangeObjectState(payment, System.Data.EntityState.Added);
                        }

                        ctx.AttachTo("Purchases", entity);
                        ctx.ObjectStateManager.ChangeObjectState(entity, System.Data.EntityState.Modified);

                        ctx.SaveChanges();

                        scope.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while updating Purchase", ex);
                throw new ArgumentException("Error while updating Purchase", ex);
            }
        }

        public void Delete(Guid id)
        {
            if (id.Equals(Guid.Empty))
                throw new ArgumentException("Purchase id cannot be empty!");

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    var entity = GetPurchaseById(id);

                    if (entity != null)
                    {
                        ctx.Purchases.Attach(entity);
                        ctx.Purchases.DeleteObject(entity);
                        ctx.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while deleting Purchase", ex);
                throw new ArgumentException("Error while deleting Purchase!", ex);
            }
        }
    }
}
