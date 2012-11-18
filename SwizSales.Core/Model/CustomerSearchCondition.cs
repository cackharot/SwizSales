using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SwizSales.Core.Model
{
    public class CustomerSearchCondition
    {
        public string Number { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
        public string Landline { get; set; }
        public string Email { get; set; }

        public int PageSize { get; set; }
        public int PageNo { get; set; }

        public bool Status { get; set; }

        public CustomerSearchCondition()
        {
            this.PageSize = 25;
            this.PageNo = 1;
        }
    }
}
