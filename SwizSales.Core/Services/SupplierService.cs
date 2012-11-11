using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwizSales.Core.Model;
using System.Collections.ObjectModel;
using System.Data.Objects;
using SwizSales.Core.Library;
using SwizSales.Core.ServiceContracts;

namespace SwizSales.Core.Services
{
    public class SupplierService : ISupplierService
    {
        public ICollection<Supplier> Search(SupplierSearchCondition condition)
        {
            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.ContextOptions.LazyLoadingEnabled = false;
                    ctx.Employees.MergeOption = MergeOption.NoTracking;

                    var employees = ctx.Suppliers.Include("ContactDetail").Where(x => x.Status == true);

                    if (!string.IsNullOrEmpty(condition.Mobile)
                        && !string.IsNullOrEmpty(condition.Email)
                        && !string.IsNullOrEmpty(condition.Email))
                    {
                        employees = employees.Where(x => x.ContactDetail.ContactName.Contains(condition.Name)
                            || x.ContactDetail.Mobile.Contains(condition.Mobile)
                            || x.ContactDetail.Email.Contains(condition.Email));
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(condition.Name))
                        {
                            employees = employees.Where(x => x.ContactDetail.ContactName.Contains(condition.Name));
                        }
                        else if (!string.IsNullOrEmpty(condition.Mobile))
                        {
                            employees = employees.Where(x => x.ContactDetail.Mobile.Contains(condition.Mobile));
                        }
                        else if (!string.IsNullOrEmpty(condition.Email))
                        {
                            employees = employees.Where(x => x.ContactDetail.Email.Contains(condition.Email));
                        }
                    }

                    employees = employees.OrderBy(x => x.ContactDetail.ContactName);

                    employees = employees.Skip((condition.PageNo - 1) * condition.PageSize).Take(condition.PageSize);

                    return new Collection<Supplier>(employees.ToList());
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while searching suppliers", ex);
                throw ex;
            }
        }

        public Supplier GetSupplierById(Guid id)
        {

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.Employees.MergeOption = MergeOption.NoTracking;
                    return ctx.Suppliers.Include("ContactDetail").SingleOrDefault(x => x.Id.Equals(id));
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while fetching Supplier", ex);
                throw ex;
            }
        }

        public Guid Add(Supplier entity)
        {
            using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
            {
                try
                {
                    ctx.Suppliers.AddObject(entity);
                    ctx.SaveChanges();
                    return entity.Id;
                }
                catch (Exception ex)
                {
                    LogService.Error("Error while adding Supplier", ex);
                    throw new ArgumentException("Error while adding new Supplier!");
                }
            }
        }

        public void Update(Supplier entity)
        {
            if (entity.Id.Equals(Guid.Empty))
                throw new ArgumentException("Supplier Id cannot be empty!");

            using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
            {
                try
                {

                    ctx.Suppliers.Attach(entity);

                    ctx.ObjectStateManager.ChangeObjectState(entity, System.Data.EntityState.Modified);

                    ctx.Suppliers.ApplyCurrentValues(entity);

                    ctx.SaveChanges();

                }
                catch (Exception ex)
                {
                    LogService.Error("Error while updating Supplier", ex);
                    throw ex;
                }
            }
        }

        public void Delete(Guid id)
        {
            if (id.Equals(Guid.Empty))
                throw new ArgumentException("Supplier id cannot be empty!");

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    var entity = GetSupplierById(id);

                    if (entity != null)
                    {
                        ctx.Suppliers.Attach(entity);
                        ctx.Suppliers.DeleteObject(entity);
                        ctx.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while deleting Supplier", ex);
                throw ex;
            }
        }
    }
}
