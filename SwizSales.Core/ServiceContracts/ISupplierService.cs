using System;
using SwizSales.Core.Model;
using System.Collections.Generic;

namespace SwizSales.Core.ServiceContracts
{
    public interface ISupplierService
    {
        Guid Add(Supplier entity);

        void Delete(Guid id);

        Supplier GetSupplierById(Guid id);
        
        ICollection<Supplier> Search(SupplierSearchCondition condition);
        
        void Update(Supplier entity);
    }
}
