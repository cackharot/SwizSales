using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SwizSales.Core.Model
{
    public enum PaymentType
    {
        Cash,
        Credit,
        Card,
        Cheque,
        Online
    }

    public class OrderSearchCondition
    {
        public int OrderNo { get; set; }

        public Guid CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string CustomerMobile { get; set; }

        public string CustomerNo { get; set; }

        public Guid EmployeeId { get; set; }

        public string EmployeeName { get; set; }

        public DateTime FromOrderDate { get; set; }

        public DateTime ToOrderDate { get; set; }

        public double MinAmount { get; set; }

        public double MaxAmount { get; set; }

        public PaymentType PaymentType { get; set; }

        public int PageNo { get; set; }

        public int PageSize { get; set; }

        public OrderSearchCondition()
        {
            this.PageNo = 1;
            this.PageSize = 100;

            this.FromOrderDate = DateTime.Today.AddDays(-1);
            this.ToOrderDate = DateTime.Today;
        }
    }
}
