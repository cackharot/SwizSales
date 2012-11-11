using System;
using SwizSales.Core.Model;
using System.Collections.Generic;

namespace SwizSales.Core.ServiceContracts
{
    public interface IEmployeeService
    {
        Guid Add(Employee entity);

        void Delete(Guid id);

        Employee GetEmployeeById(Guid id);

        ICollection<Employee> Search(EmployeeSearchCondition condition);

        void Update(Employee entity);
    }
}
