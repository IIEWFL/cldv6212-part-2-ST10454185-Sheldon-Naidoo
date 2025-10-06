using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using RetailManagementAppFunctions.Models;

namespace RetailManagementAppFunctions.Services
{
    // Defined Interface: Essential for clean Dependency Injection (DI) and Unit Testing.
    public interface IOrderTableService
    {
        Task<Order> GetOrderAsync(string partitionKey, string rowKey);
        Task<List<Order>> GetAllOrdersAsync();
        Task AddOrderAsync(Order order);
        Task UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(string partitionKey, string rowKey);
    }

    public class OrderTableService : IOrderTableService
    {
        // Code Attribution
        // This method was adapted from W3Schools
        // https://www.w3schools.com/cs/cs_constructors.php
        // W3Schools

        private readonly TableClient _tableClient;

        // Constructor uses TableServiceClient to initialize TableClient
        public OrderTableService(string storageConnectionString, string tableName)
        {
            var serviceClient = new TableServiceClient(storageConnectionString);
            _tableClient = serviceClient.GetTableClient(tableName);
            _tableClient.CreateIfNotExists();
        }

        // Retrieves a single Order entity. Returns null if not found.
        public async Task<Order> GetOrderAsync(string partitionKey, string rowKey)
        {
            try
            {
                Response<Order> response = await _tableClient.GetEntityAsync<Order>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }


        // Retrieves all Order entities from the table.
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            List<Order> orders = new List<Order>();
            // Query all entities from the table
            await foreach (var order in _tableClient.QueryAsync<Order>())
            {
                orders.Add(order);
            }
            return orders;
        }

        // Adds a new Order entity to the table.
        public async Task AddOrderAsync(Order order)
        {
            // The function must ensure PartitionKey and RowKey are set before calling this.
            if (string.IsNullOrEmpty(order.PartitionKey) || string.IsNullOrEmpty(order.RowKey))
            {
                throw new ArgumentNullException(nameof(order), "PartitionKey and RowKey must be set before adding the entity.");
            }

            await _tableClient.AddEntityAsync(order);
        }

        public async Task UpdateOrderAsync(Order order)
        {
            // UpdateEntity handles both Insert and Replace.
            await _tableClient.UpdateEntityAsync(order, order.ETag, TableUpdateMode.Replace);
        }

        // Deletes an Order entity.
        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            // Uses ETag.All to ensure the item is deleted regardless of its ETag state.
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey, ETag.All);
        }
    }

}