using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SwizSales.Core.Model
{
    public class PurchaseSearchCondition
    {
        public string BillNo { get; set; }
        
        public Guid SupplierId { get; set; }

        public string SupplierName { get; set; }

        public Guid EmployeeId { get; set; }

        public string EmployeeName { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public double MinAmount { get; set; }

        public double MaxAmount { get; set; }

        public PaymentType PaymentType { get; set; }

        public int PageNo { get; set; }

        public int PageSize { get; set; }

        public PurchaseSearchCondition()
        {
            this.PageNo = 1;
            this.PageSize = 100;

            //this.FromDate = DateTime.Today.AddMonths(-1);
            //this.ToDate = DateTime.Today;
        }
    }
}
