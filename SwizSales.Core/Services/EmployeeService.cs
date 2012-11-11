using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwizSales.Core.Model;
using System.Collections.ObjectModel;
using SwizSales.Core.Library;
using System.Data.Objects;
using SwizSales.Core.ServiceContracts;

namespace SwizSales.Core.Services
{
    public class EmployeeService : IEmployeeService
    {
        public ICollection<Employee> Search(EmployeeSearchCondition condition)
        {
            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.ContextOptions.LazyLoadingEnabled = false;
                    ctx.Employees.MergeOption = MergeOption.NoTracking;

                    var employees = ctx.Employees.Include("ContactDetail").Where(x => x.Status == true);

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

                    return new Collection<Employee>(employees.ToList());
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while searching employees", ex);
                throw ex;
            }
        }

        public Employee GetEmployeeById(Guid id)
        {

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.Employees.MergeOption = MergeOption.NoTracking;
                    return ctx.Employees.Include("ContactDetail").SingleOrDefault(x => x.Id.Equals(id));
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while fetching customer", ex);
                throw ex;
            }
        }

        public Guid Add(Employee entity)
        {
            using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
            {
                try
                {
                    ctx.Employees.AddObject(entity);
                    ctx.SaveChanges();
                    return entity.Id;
                }
                catch (Exception ex)
                {
                    LogService.Error("Error while adding Employee", ex);
                    throw new ArgumentException("Error while adding new Employee!");
                }
            }
        }
        
        public void Update(Employee entity)
        {
            if (entity.Id.Equals(Guid.Empty))
                throw new ArgumentException("Employee Id cannot be empty!");

            using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
            {
               try
                {

                    ctx.Employees.Attach(entity);
                   
                    ctx.ObjectStateManager.ChangeObjectState(entity, System.Data.EntityState.Modified);
                   
                    ctx.Employees.ApplyCurrentValues(entity);
                   
                    ctx.SaveChanges();

                }
                catch (Exception ex)
                {
                    LogService.Error("Error while updating Employee", ex);
                    throw ex;
                }
            }
        }

        public void Delete(Guid id)
        {
            if (id.Equals(Guid.Empty))
                throw new ArgumentException("Employee id cannot be empty!");

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    var entity = GetEmployeeById(id);

                    if (entity != null)
                    {
                        ctx.Employees.Attach(entity);
                        ctx.Employees.DeleteObject(entity);
                        ctx.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while deleting Employee", ex);
                throw ex;
            }
        }
    }
}
