using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using SwizSales.Core.Model;

namespace SwizSales.Core.ServiceContracts
{
    public interface IProductService
    {
        Collection<Product> Search(ProductSearchCondition condition);

        Product GetProductById(Guid id);

        Product GetProductByName(string name);

        Guid Add(Product entity);

        void Update(Product entity);

        void Delete(Guid id);

        List<Product> GetProductByBarcode(string barcode);
    }
}
