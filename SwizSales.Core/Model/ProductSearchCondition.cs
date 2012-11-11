using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SwizSales.Core.Model
{
    public class ProductSearchCondition
    {
        public string Barcode { get; set; }

        public string Name { get; set; }

        public decimal MinMRP { get; set; }

        public decimal MaxMRP { get; set; }

        public decimal MinSellPrice { get; set; }

        public decimal MaxSellPrice { get; set; }

        public Guid SupplierId { get; set; }

        public string SupplierName { get; set; }

        public Guid CategoryId { get; set; }

        public string CategoryName { get; set; }

        public Guid TaxCategoryId { get; set; }

        public string TaxCategoryName { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public int PageNo { get; set; }

        public int PageSize { get; set; }

        public ProductSearchCondition()
        {
            this.PageNo = 1;
            this.PageSize = 25;
        }
    }
}
