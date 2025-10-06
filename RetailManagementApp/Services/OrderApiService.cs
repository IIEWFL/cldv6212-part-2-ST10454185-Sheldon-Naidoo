using RetailManagementApp.Models;
using System.Text.Json;

namespace RetailManagementApp.Services
{
    public class OrderApiService : IOrderService
    {
        // Code Attribution
        // This method was adapted from Microsoft Learn
        // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to
        // Microsoft Learn

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public OrderApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Configure JSON deserialization to be case-insensitive for reliable API consumption
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<Order>> GetOrdersAsync()
        {
            // Code Attribution
            // This method was adapted from Microsoft Learn
            // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to
            // Microsoft Learn

            var response = await _httpClient.GetAsync("orders");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Order>>(content, _jsonOptions) ?? new List<Order>();
        }

        public async Task<Order?> GetOrderAsync(string partitionKey, string rowKey)
        {
            // Code Attribution
            // This method was adapted from Microsoft Learn
            // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to
            // Microsoft Learn

            var response = await _httpClient.GetAsync($"orders/{partitionKey}/{rowKey}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Order>(content, _jsonOptions);
        }

        public async Task<Order?> CreateOrderAsync(Order order)
        {
            // Code Attribution
            // This method was adapted from Microsoft Learn
            // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to
            // Microsoft Learn

            // The Function App expects a simple JSON body for creation
            var payload = new
            {
                order.CustomerName,
                order.ProductName,
                order.TotalAmount
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("orders", jsonContent);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Order>(content, _jsonOptions);
        }

        public async Task<bool> DeleteOrderAsync(string partitionKey, string rowKey)
        {
            var response = await _httpClient.DeleteAsync($"orders/{partitionKey}/{rowKey}");
            return response.IsSuccessStatusCode;
        }

        public async Task<Order?> UpdateOrderAsync(string partitionKey, string rowKey, MultipartFormDataContent content)
        {
            // Code Attribution
            // This method was adapted from Microsoft Learn
            // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to
            // Microsoft Learn

            // The Function App expects an HTTP POST with multipart/form-data for updates
            var response = await _httpClient.PostAsync($"orders/{partitionKey}/{rowKey}", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Order>(responseContent, _jsonOptions);
        }
    }
}