using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RetailManagementAppFunctions.Models;

namespace RetailManagementAppFunctions.Services
{
    public class ProductTableService
    {
        // Code Attribution
        // This method was adapted from W3Schools
        // https://www.w3schools.com/cs/cs_constructors.php
        // W3Schools

        private readonly TableClient _tableClient;

        public ProductTableService(string storageConnectionString, string tableName)
        {
            var serviceClient = new TableServiceClient(storageConnectionString);
            _tableClient = serviceClient.GetTableClient(tableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            var products = new List<Product>();
            await foreach (var product in _tableClient.QueryAsync<Product>())
            {
                products.Add(product);
            }
            return products;
        }

        public async Task<Product> GetProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<Product>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task AddProductAsync(Product product)
        {
            // Code Attribution
            // This method was adapted from Microsoft Learn
            // https://learn.microsoft.com/en-us/dotnet/api/system.guid.newguid?view=net-9.0
            // Microsoft Learn

            product.PartitionKey = "electronics";
            product.RowKey = Guid.NewGuid().ToString();

            await _tableClient.AddEntityAsync(product);
        }

        public async Task UpdateProductAsync(Product product)
        {
            await _tableClient.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}
