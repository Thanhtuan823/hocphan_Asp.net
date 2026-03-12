using lab2.Models;
using System.Collections.Generic;

namespace lab2.Repositories
{
    public interface IOrderRepository
    {
        // Lưu đơn hàng mới
        void SaveOrder(Order order);

        // Lấy đơn hàng theo Id
        Order GetOrderById(int orderId);


        // (Tuỳ chọn) Lấy tất cả đơn hàng
        List<Order> GetAllOrders();
    }
}