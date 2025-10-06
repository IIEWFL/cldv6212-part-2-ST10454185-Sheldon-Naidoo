using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration; 
using System;
using Microsoft.AspNetCore.Http; 
using System.Text;
using RetailManagementApp.Models;

namespace RetailManagementApp.Services
{
    // Interface for Dependency Injection
    public interface IFunctionService
    {
        Task<List<Order>> GetAllOrdersAsync();
        Task<Order> GetOrderAsync(string partitionKey, string rowKey);
        Task<bool> AddOrderAsync(Order order, IFormFile? photo);
        Task<bool> DeleteOrderAsync(string partitionKey, string rowKey, string blobName);
        Task<bool> UpdateOrderAsync(Order order, IFormFile? photo);
        Task<List<QueueLogViewModel>> GetMessagesAsync();
        Task<string> ExportLog(string name);
    }

    public class FunctionService : IFunctionService
    {
        // Code Attribution
        // This method was adapted from Microsoft Learn
        // https://learn.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
        // Microsoft Learn

        private readonly HttpClient _httpClient;

        // Configuration and HttpClient injection
        public FunctionService(HttpClient httpClient, IConfiguration configuration)
        {
            // Code Attribution
            // This method was adapted from Microsoft Learn
            // https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient
            // Microsoft Learn

            _httpClient = httpClient;
        }

        // --- Order CRUD Operations ---

        // Get All Orders
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<List<Order>>("GetAllOrders");
            return response ?? new List<Order>();
        }

        // Get Order Details (by PK/RK)
        public async Task<Order> GetOrderAsync(string partitionKey, string rowKey)
        {
            var requestUrl = $"GetOrder?partitionKey={partitionKey}&rowKey={rowKey}";

            var response = await _httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Order>() ?? throw new InvalidOperationException("Received empty response for order.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Handle not found
                return null;
            }

            response.EnsureSuccessStatusCode();
            return null;
        }

        // Create Order (with photo upload)
        public async Task<bool> AddOrderAsync(Order order, IFormFile? photo)
        {
            // Code Attribution
            // This method was adapted from Microsoft Learn
            // https://learn.microsoft.com/en-us/dotnet/api/system.guid.newguid?view=net-9.0
            // Microsoft Learn

            order.RowKey ??= Guid.NewGuid().ToString();
            order.PartitionKey ??= "DEFAULT";

            using var content = new MultipartFormDataContent();

            // Add all required fields as StringContent
            content.Add(new StringContent(order.PartitionKey), "PartitionKey");
            content.Add(new StringContent(order.RowKey), "RowKey");
            content.Add(new StringContent(order.CustomerName ?? string.Empty), "CustomerName");
            content.Add(new StringContent(order.ProductName ?? string.Empty), "ProductName");
            content.Add(new StringContent(order.TotalAmount.ToString()), "TotalAmount");

            if (photo != null)
            {
                var streamContent = new StreamContent(photo.OpenReadStream());
                content.Add(streamContent, "ProductPhoto", photo.FileName);
            }

            var response = await _httpClient.PostAsync("CreateOrder", content);
            return response.IsSuccessStatusCode;
        }

        // Update Order 
        public async Task<bool> UpdateOrderAsync(Order order, IFormFile? photo)
        {
            var response = await _httpClient.PutAsJsonAsync("UpdateOrder", order);
            return response.IsSuccessStatusCode;
        }

        // Delete Order
        public async Task<bool> DeleteOrderAsync(string partitionKey, string rowKey, string blobName)
        {
            var requestUrl = $"DeleteOrder?partitionKey={partitionKey}&rowKey={rowKey}&blobName={blobName}";
            var response = await _httpClient.DeleteAsync(requestUrl); // Using DELETE verb
            return response.IsSuccessStatusCode;
        }

        // --- Queue and File Share Operations ---

        // Get all messages from the queue (for monitoring)
        public async Task<List<QueueLogViewModel>> GetMessagesAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<List<QueueLogViewModel>>("GetMessages");
            return response ?? new List<QueueLogViewModel>();
        }

        // Upload log file to file share (Export Invoice/Report)
        public async Task<string> ExportLog(string name)
        {
            var response = await _httpClient.PostAsJsonAsync("UploadLogFile", new { LogName = name });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
