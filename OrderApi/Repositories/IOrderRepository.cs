using OrderModels.Models;

namespace OrderApi.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<IEnumerable<Order>> GetAllOrdersAsync();
    Task UpdateOrderAsync(Order order);
}
