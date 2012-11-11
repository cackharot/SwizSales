using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwizSales.Core.Model;

namespace SwizSales.Core.ServiceContracts
{
    public interface IStockService
    {
        ICollection<Purchase> Search(PurchaseSearchCondition condition);

        Purchase GetPurchaseById(Guid id);

        Guid Add(Purchase entity);

        void Update(Purchase entity);

        void Delete(Guid id);
    }
}
