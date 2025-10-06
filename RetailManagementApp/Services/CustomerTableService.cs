using Azure;
using Azure.Data.Tables;
using RetailManagementApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RetailManagementApp.Services
{
    public class CustomerTableService
    {
        // Code Attribution
        // This method was adapted from W3Schools
        // https://www.w3schools.com/cs/cs_constructors.php
        // W3Schools

        private readonly TableClient _tableClient;

        public CustomerTableService(string storageConnectionString, string tableName)
        {
            var serviceClient = new TableServiceClient(storageConnectionString);
            _tableClient = serviceClient.GetTableClient(tableName);
            _tableClient.CreateIfNotExists();
        }
        public async Task<List<Customer>> GetCustomerAsync()
        {
            var customers = new List<Customer>();
            await foreach (var customer in _tableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }
            return customers;
        }

        public async Task<Customer?> GetCustomerAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            // Code Attribution
            // This method was adapted from Microsoft Learn
            // https://learn.microsoft.com/en-us/dotnet/api/system.guid.newguid?view=net-9.0
            // Microsoft Learn

            customer.PartitionKey = "customers";
            customer.RowKey = Guid.NewGuid().ToString();

            await _tableClient.AddEntityAsync(customer);
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            await _tableClient.UpdateEntityAsync(customer, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}
