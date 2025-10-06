using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RetailManagementAppFunctions.Services;
using RetailManagementAppFunctions.Models;

namespace RetailManagementAppFunctions.Functions
{
    public class GetOrderFunction
    {
        // Code Attribution
        // This method was adapted from Microsoft Learn
        // https://learn.microsoft.com/en-us/aspnet/mvc/overview/getting-started/getting-started-with-ef-using-mvc/creating-a-more-complex-data-model-for-an-asp-net-mvc-application
        // Microsoft Learn

        private readonly OrderTableService _orderStorageService;

        public GetOrderFunction(OrderTableService orderTableService)
        {
            _orderStorageService = orderTableService;
        }

        [FunctionName("GetOrder")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{partitionKey}/{rowKey}")] HttpRequest req,
            string partitionKey,
            string rowKey,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request to get order details for PartitionKey: {partitionKey} and RowKey: {rowKey}");

            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return new BadRequestObjectResult("PartitionKey and RowKey must be provided in the route.");
            }

            try
            {
                // Retrieves order from the table storage
                var order = await _orderStorageService.GetOrderAsync(partitionKey, rowKey);

                if (order == null)
                {
                    log.LogWarning($"Order not found on PartitionKey: {partitionKey} and RowKey: {rowKey}");
                    return new NotFoundResult();
                }

                // Converts to Order to OrderDto for client consumption
                var orderDto = new OrderDto
                {
                    PartitionKey = order.PartitionKey,
                    RowKey = order.RowKey,
                    Timestamp = order.Timestamp,
                    ETag = order.ETag.ToString(),
                    CustomerName = order.CustomerName,
                    ProductName = order.ProductName,
                    TotalAmount = order.TotalAmount
                };

                return new OkObjectResult(orderDto);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error fetching order for {partitionKey}/{rowKey}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}