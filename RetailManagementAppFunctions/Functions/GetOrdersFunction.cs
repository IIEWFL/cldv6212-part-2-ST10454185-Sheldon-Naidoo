using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using RetailManagementAppFunctions.Models;
using RetailManagementAppFunctions.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RetailManagementAppFunctions.Functions
{
    public class GetOrdersFunction
    {
        // Code Attribution
        // This method was adapted from Microsoft Learn
        // https://learn.microsoft.com/en-us/aspnet/mvc/overview/getting-started/getting-started-with-ef-using-mvc/creating-a-more-complex-data-model-for-an-asp-net-mvc-application
        // Microsoft Learn

        private readonly OrderTableService _orderStorageService;

        // Unified constructor for Dependency Injection
        public GetOrdersFunction(OrderTableService orderTableService)
        {
            _orderStorageService = orderTableService;
        }

        [FunctionName("GetOrders")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request to get all orders.");

            try
            {
                // Retrieve the orders from the table storage
                var orders = await _orderStorageService.GetAllOrdersAsync();

                // Convert list of Order entities to list of Order Data Transfer Objects (DTOs)
                var orderDtos = orders.Select(o => new OrderDto
                {
                    PartitionKey = o.PartitionKey,
                    RowKey = o.RowKey,
                    Timestamp = o.Timestamp,
                    ETag = o.ETag.ToString(), // Use null-conditional for safety
                    CustomerName = o.CustomerName,
                    ProductName = o.ProductName,
                    TotalAmount = o.TotalAmount,
                }).ToList();

                // Return the list of orders as an API response
                return new OkObjectResult(orderDtos);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error fetching all orders from table storage.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }

}
