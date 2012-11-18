using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwizSales.Core.Model;
using System.Collections.ObjectModel;
using SwizSales.Core.Library;
using System.Data.Objects;

namespace SwizSales.Core.Services
{
    public class CustomerService : SwizSales.Core.ServiceContracts.ICustomerService
    {
        public Collection<Customer> Search(CustomerSearchCondition condition)
        {
            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.ContextOptions.LazyLoadingEnabled = false;
                    ctx.Customers.MergeOption = MergeOption.NoTracking;
                    ctx.ContactDetails.MergeOption = MergeOption.NoTracking;

                    var items = ctx.Customers.Include("ContactDetail").Where(x => x.Status == true);

                    if (!string.IsNullOrEmpty(condition.Number) && !string.IsNullOrEmpty(condition.Mobile)
                        && !string.IsNullOrEmpty(condition.Email)
                        && !string.IsNullOrEmpty(condition.Email))
                    {
                        items = items.Where(x => x.SSN.Contains(condition.Number)
                            || x.ContactDetail.ContactName.Contains(condition.Name)
                            || x.ContactDetail.Mobile.Contains(condition.Mobile)
                            || x.ContactDetail.Email.Contains(condition.Email));
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(condition.Number))
                        {
                            items = items.Where(x => x.SSN.Contains(condition.Number));
                        }
                        
                        if (!string.IsNullOrEmpty(condition.Name))
                        {
                            items = items.Where(x => x.ContactDetail.ContactName.Contains(condition.Name));
                        }
                        
                        if (!string.IsNullOrEmpty(condition.Mobile))
                        {
                            items = items.Where(x => x.ContactDetail.Mobile.Contains(condition.Mobile));
                        }
                        
                        if (!string.IsNullOrEmpty(condition.Email))
                        {
                            items = items.Where(x => x.ContactDetail.Email.Contains(condition.Email));
                        }
                    }

                    items = items.OrderBy(x => x.ContactDetail.ContactName);

                    if (condition.PageNo > 0 && condition.PageSize > 0)
                    {
                        items = items.Skip((condition.PageNo - 1) * condition.PageSize).Take(condition.PageSize);
                    }

                    return new Collection<Customer>(items.ToList());
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while searching customer", ex);
            }

            return null;
        }

        public Customer GetCustomerById(Guid id)
        {

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.Customers.MergeOption = MergeOption.NoTracking;
                    ctx.ContactDetails.MergeOption = MergeOption.NoTracking;
                    return ctx.Customers.Include("ContactDetail").SingleOrDefault(x => x.Id.Equals(id));
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while fetching customer", ex);
                return null;
            }
        }

        public Customer GetCustomerBySSN(string ssn)
        {
            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.Customers.MergeOption = MergeOption.NoTracking;
                    ctx.ContactDetails.MergeOption = MergeOption.NoTracking;
                    return ctx.Customers.Include("ContactDetail").SingleOrDefault(x => x.SSN.Equals(ssn, StringComparison.OrdinalIgnoreCase) && x.Status == true);
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while fetching customer", ex);
                return null;
            }
        }

        public Guid Add(Customer entity)
        {
            if (string.IsNullOrEmpty(entity.SSN))
                throw new ArgumentException("Customer number cannot be empty!");

            if (string.IsNullOrEmpty(entity.ContactDetail.ContactName))
                throw new ArgumentException("Customer name cannot be empty!");

            using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
            {
                if (CheckDuplicate(entity.Id, entity.SSN, ctx))
                {
                    throw new ArgumentException("Duplicate Customer number found!");
                }

                try
                {
                    ctx.Customers.AddObject(entity);
                    ctx.SaveChanges();
                    return entity.Id;
                }
                catch (Exception ex)
                {
                    LogService.Error("Error while adding customer", ex);
                    throw new ArgumentException("Error while adding new customer!");
                }
            }
        }

        private bool CheckDuplicate(Guid id, string number, OpenPOSDbEntities ctx)
        {
            var query = ctx.Customers.Where(x => x.SSN.Equals(number, StringComparison.InvariantCultureIgnoreCase));

            if (!id.Equals(Guid.Empty))
                query = query.Where(x => x.Id != id);

            return query.Count() != 0;
        }

        public void Update(Customer entity)
        {
            if (entity.Id.Equals(Guid.Empty))
                throw new ArgumentException("Customer Id cannot be empty!");

            if (string.IsNullOrEmpty(entity.SSN))
                throw new ArgumentException("Customer number cannot be empty!");

            if (string.IsNullOrEmpty(entity.ContactDetail.ContactName))
                throw new ArgumentException("Customer name cannot be empty!");

            using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
            {
                if (CheckDuplicate(entity.Id, entity.SSN, ctx))
                {
                    throw new ArgumentException("Duplicate Customer number found!");
                }

                try
                {

                    ctx.Customers.Attach(entity);
                    ctx.ContactDetails.Attach(entity.ContactDetail);

                    ctx.ObjectStateManager.ChangeObjectState(entity, System.Data.EntityState.Modified);
                    ctx.ObjectStateManager.ChangeObjectState(entity.ContactDetail, System.Data.EntityState.Modified);

                    ctx.Customers.ApplyCurrentValues(entity);
                    ctx.ContactDetails.ApplyCurrentValues(entity.ContactDetail);

                    ctx.SaveChanges();

                }
                catch (Exception ex)
                {
                    LogService.Error("Error while updating customer", ex);
                }
            }
        }

        public void Delete(Guid id)
        {
            if (id.Equals(Guid.Empty))
                throw new ArgumentException("Customer id cannot be empty!");

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    var entity = GetCustomerById(id);

                    if (entity != null)
                    {
                        ctx.Customers.Attach(entity);
                        ctx.Customers.DeleteObject(entity);
                        ctx.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while deleting customer", ex);
            }
        }
    }
}
