using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwizSales.Core.Library;
using SwizSales.Core.Model;
using System.Collections.ObjectModel;
using SwizSales.Core.ServiceContracts;

namespace SwizSales.Core.Services
{
    public class MasterDataService<T> : IMasterDataService<T> where T : class
    {
        public ICollection<T> Search()
        {
            try
            {
                using (var ctx = new OpenPOSDbEntities())
                {
                    //var query = ctx.CreateQuery<T>("SELECT * FROM " + ctx.GetTableName<T>());
                    var query = ctx.CreateObjectSet<T>();
                    return new Collection<T>(query.ToList());
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while search masterdata", ex);
                throw ex;
            }
        }
    }
}
