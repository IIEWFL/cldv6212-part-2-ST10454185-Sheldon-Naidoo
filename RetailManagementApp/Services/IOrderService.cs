using RetailManagementApp.Models;

namespace RetailManagementApp.Services
{
    // Contract for interacting with the Order API (hosted by Azure Functions).

    public interface IOrderService
    {
        Task<List<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(string partitionKey, string rowKey);
        Task<Order?> CreateOrderAsync(Order order);
        Task<bool> DeleteOrderAsync(string partitionKey, string rowKey);
        Task<Order?> UpdateOrderAsync(string partitionKey, string rowKey, MultipartFormDataContent content);
    }
}
