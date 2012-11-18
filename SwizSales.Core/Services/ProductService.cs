using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwizSales.Core.ServiceContracts;
using SwizSales.Core.Model;
using System.Collections.ObjectModel;
using System.Data.Objects;
using SwizSales.Core.Library;

namespace SwizSales.Core.Services
{
    public class ProductService : IProductService
    {
        public Collection<Product> Search(ProductSearchCondition condition)
        {
            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.ContextOptions.LazyLoadingEnabled = false;
                    ctx.Customers.MergeOption = MergeOption.NoTracking;

                    var items = ctx.Products.Include("Supplier").Include("Category").Include("TaxCategory").Where(x => x.Status == true);

                    if (!string.IsNullOrEmpty(condition.Name) && !string.IsNullOrEmpty(condition.Barcode))
                    {
                        items = items.Where(x => x.Name.Contains(condition.Name)
                            || x.Barcode.Contains(condition.Barcode));
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(condition.Barcode))
                        {
                            items = items.Where(x => x.Barcode.Contains(condition.Barcode));
                        }

                        if (!string.IsNullOrEmpty(condition.Name))
                        {
                            items = items.Where(x => x.Name.Contains(condition.Name));
                        }

                        if (condition.SupplierId != Guid.Empty)
                        {
                            items = items.Where(x => x.SupplierId == condition.SupplierId);
                        }
                        else if (!string.IsNullOrEmpty(condition.SupplierName))
                        {
                            items = items.Where(x => x.Supplier.Name.Contains(condition.SupplierName));
                        }

                        if (condition.MinMRP > 0 && condition.MaxMRP > 0)
                        {
                            items = items.Where(x => x.MRP >= condition.MinMRP && x.MRP <= condition.MaxMRP);
                        }
                        else if (condition.MinMRP > 0 && condition.MaxMRP <= 0)
                        {
                            items = items.Where(x => x.MRP >= condition.MinMRP);
                        }
                        else if (condition.MinMRP <= 0 && condition.MaxMRP > 0)
                        {
                            items = items.Where(x => x.MRP <= condition.MaxMRP);
                        }

                        if (condition.MinSellPrice > 0 && condition.MaxSellPrice > 0)
                        {
                            items = items.Where(x => x.SellPrice >= condition.MinSellPrice && x.SellPrice <= condition.MaxSellPrice);
                        }
                        else if (condition.MinSellPrice > 0 && condition.MaxSellPrice <= 0)
                        {
                            items = items.Where(x => x.SellPrice >= condition.MinSellPrice);
                        }
                        else if (condition.MinSellPrice <= 0 && condition.MaxSellPrice > 0)
                        {
                            items = items.Where(x => x.SellPrice <= condition.MaxSellPrice);
                        }
                    }

                    items = items.OrderBy(x => x.Name);

                    if (condition.PageNo > 0 && condition.PageSize > 0)
                    {
                        items = items.Skip((condition.PageNo - 1) * condition.PageSize).Take(condition.PageSize);
                    }

                    return new Collection<Product>(items.ToList());
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while searching products", ex);
                throw ex;
            }
        }

        public Product GetProductById(Guid id)
        {

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.Products.MergeOption = MergeOption.NoTracking;
                    return ctx.Products.SingleOrDefault(x => x.Id.Equals(id));
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while fetching products", ex);
                throw ex;
            }
        }

        public Product GetProductByName(string name)
        {
            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.Products.MergeOption = MergeOption.NoTracking;
                    return ctx.Products.Where(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && x.Status == true).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while fetching products", ex);
                throw ex;
            }
        }

        public Guid Add(Product entity)
        {
            if (string.IsNullOrEmpty(entity.Barcode))
                throw new ArgumentException("Product barcode cannot be empty!");

            if (string.IsNullOrEmpty(entity.Name))
                throw new ArgumentException("Product name cannot be empty!");

            using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
            {
                try
                {
                    entity.Status = true;
                    ctx.Products.AddObject(entity);
                    ctx.SaveChanges();
                    return entity.Id;
                }
                catch (Exception ex)
                {
                    LogService.Error("Error while adding product", ex);
                    throw new ArgumentException("Error while adding new product!");
                }
            }
        }

        public void Update(Product entity)
        {
            if (entity.Id.Equals(Guid.Empty))
                throw new ArgumentException("Product Id cannot be empty!");

            if (string.IsNullOrEmpty(entity.Barcode))
                throw new ArgumentException("Product barcode cannot be empty!");

            if (string.IsNullOrEmpty(entity.Name))
                throw new ArgumentException("Product name cannot be empty!");

            using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
            {
                try
                {

                    ctx.Products.Attach(entity);
                    ctx.ObjectStateManager.ChangeObjectState(entity, System.Data.EntityState.Modified);
                    ctx.Products.ApplyCurrentValues(entity);

                    ctx.SaveChanges();
                }
                catch (Exception ex)
                {
                    LogService.Error("Error while updating product", ex);
                    throw ex;
                }
            }
        }

        public void Delete(Guid id)
        {
            if (id.Equals(Guid.Empty))
                throw new ArgumentException("Product id cannot be empty!");

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    var entity = GetProductById(id);

                    if (entity != null)
                    {
                        ctx.Products.Attach(entity);
                        ctx.Products.DeleteObject(entity);
                        ctx.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while deleting product", ex);
                throw ex;
            }
        }

        public List<Product> GetProductByBarcode(string barcode)
        {
            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    ctx.Products.MergeOption = MergeOption.NoTracking;
                    return ctx.Products.Where(x => x.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase) && x.Status == true).ToList();
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while fetching products", ex);
                throw ex;
            }
        }
    }
}
