using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs; 
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using RetailManagementAppFunctions.Models; 
using RetailManagementAppFunctions.Services;
using System;
using System.Threading.Tasks;

namespace RetailManagementAppFunctions.Functions
{
    public class CreateOrderFunction
    {
        // Code Attribution
        // This method was adapted from Microsoft Learn
        // https://learn.microsoft.com/en-us/aspnet/mvc/overview/getting-started/getting-started-with-ef-using-mvc/creating-a-more-complex-data-model-for-an-asp-net-mvc-application
        // Microsoft Learn

        // Code Attribution
        // This method was adapted from W3Schools
        // https://www.w3schools.com/cs/cs_constructors.php
        // W3Schools

        // Use interfaces for dependency injection, even if the constructor uses concrete types
        private readonly OrderTableService _orderTableService;
        private readonly ProductImageBlobStorageService _blobStorageService;
        private readonly TransactionQueueService _queueStorageService;

        // Constructor used by Azure Functions Dependency Injection to inject required services.
        public CreateOrderFunction(
            OrderTableService orderTableService,
            ProductImageBlobStorageService blobStorageService,
            TransactionQueueService queueStorageService)
        {
            _orderTableService = orderTableService;
            _blobStorageService = blobStorageService;
            _queueStorageService = queueStorageService;
        }

        [FunctionName("CreateOrder")] 
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request to create a new order.");

            // Reads and parses the incoming form data
            var form = await req.ReadFormAsync();

            // Defines PartitionKey and create a new RowKey
            var partitionKey = "Order"; 
            var rowKey = Guid.NewGuid().ToString();

            // Parses TotalAmount
            if (!decimal.TryParse(form["TotalAmount"], out decimal totalAmountValue))
            {
                return new BadRequestObjectResult("Invalid or missing TotalAmount.");
            }

            var order = new Order
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                CustomerName = form["CustomerName"],
                ProductName = form["ProductName"],
                TotalAmount = (double)totalAmountValue,
            };

            log.LogInformation($"New Order PK: {order.PartitionKey}, RK: {order.RowKey}, Amount: {order.TotalAmount}");

            // Validates the required fields
            if (string.IsNullOrEmpty(order.CustomerName) || string.IsNullOrEmpty(order.ProductName))
            {
                return new BadRequestObjectResult("Customer Name and Product Name are required fields.");
            }

            try
            {
                // Stores information into Azure tables
                await _orderTableService.AddOrderAsync(order);

                // Sends message to the queue for asynchronous processing
                // Use a standard model for the queue message payload
                var queueMessage = new
                {
                    Action = "OrderCreated",
                    OrderId = order.RowKey,
                    Customer = order.CustomerName,
                    Amount = order.TotalAmount,
                    Timestamp = DateTime.UtcNow
                };
                await _queueStorageService.SendTransactionMessageAsync(queueMessage);

                // 5. Success response
                return new OkObjectResult(order);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error creating order {order.RowKey}: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
