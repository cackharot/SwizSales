using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwizSales.Core.Model;
using System.Collections.ObjectModel;

namespace SwizSales.Core.ServiceContracts
{
    public interface IOrderService
    {
        Collection<Order> Search(OrderSearchCondition condition);

        Order GetOrderById(Guid id);

        Order GetOrderByOrderNo(int orderNo);

        Order NewOrder(Guid customerId, Guid employeeId);

        Guid Add(Order entity);

        void Update(Order entity);

        void Delete(Guid id);

        void UpdateOrderCustomer(Order order);
    }
}
