using System;
using System.Collections.Generic;
namespace SwizSales.Core.ServiceContracts
{
    public interface IMasterDataService<T>
     where T : class
    {
        ICollection<T> Search();
    }
}
