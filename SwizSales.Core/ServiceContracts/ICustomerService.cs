using System;

namespace SwizSales.Core.ServiceContracts
{
    public interface ICustomerService
    {
        Guid Add(SwizSales.Core.Model.Customer entity);
        void Delete(Guid id);
        SwizSales.Core.Model.Customer GetCustomerById(Guid Id);
        SwizSales.Core.Model.Customer GetCustomerBySSN(string ssn);
        System.Collections.ObjectModel.Collection<SwizSales.Core.Model.Customer> Search(SwizSales.Core.Model.CustomerSearchCondition condition);
        void Update(SwizSales.Core.Model.Customer entity);
    }
}
