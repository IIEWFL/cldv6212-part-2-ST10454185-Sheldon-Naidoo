using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using RetailManagementAppFunctions.Models;
using RetailManagementAppFunctions.Services;
using Azure;
using Azure.Data.Tables;

namespace RetailManagementAppFunctions.Functions
{
    public class DeleteOrderFunction
    {
        // Code Attribution
        // This method was adapted from Microsoft Learn
        // https://learn.microsoft.com/en-us/aspnet/mvc/overview/getting-started/getting-started-with-ef-using-mvc/creating-a-more-complex-data-model-for-an-asp-net-mvc-application
        // Microsoft Learn

        // Code Attribution
        // This method was adapted from W3Schools
        // https://www.w3schools.com/cs/cs_constructors.php
        // W3Schools

        private readonly OrderTableService _orderTableService;
        private readonly ProductImageBlobStorageService _productStorageService;
        private readonly TransactionQueueService _transactionQueueService;

        // Unified constructor for Dependency Injection
        public DeleteOrderFunction(
            OrderTableService orderTableService,
            ProductImageBlobStorageService productStorageService,
            TransactionQueueService transactionQueueService)
        {
            _orderTableService = orderTableService;
            _productStorageService = productStorageService;
            _transactionQueueService = transactionQueueService;
        }

        [FunctionName("DeleteOrder")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "orders/{partitionKey}/{rowKey}")] HttpRequest req,
            string partitionKey,
            string rowKey,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request to delete order: {partitionKey}/{rowKey}");

            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return new BadRequestObjectResult("PartitionKey and RowKey must be provided in the route.");
            }

            try
            {
                // Retrieves the order to get the Photo (if it exists)
                var order = await _orderTableService.GetOrderAsync(partitionKey, rowKey);

                if (order == null)
                {
                    log.LogWarning($"Order not found for deletion: PartitionKey: {partitionKey}, RowKey: {rowKey}");
                    return new NotFoundResult();
                }

                // Deletes the associated image from Blob Storage
                if (!string.IsNullOrEmpty(order.ProductPhotoBlobName))
                {
                    // Extracts the blob name from the full URL
                    var blobName = order.ProductPhotoBlobName;
                    await _productStorageService.DeletePhotoAsync(blobName);
                    log.LogInformation($"Deleted photo: {blobName}");
                }

                // Deletes the entity from Table Storage
                await _orderTableService.DeleteOrderAsync(partitionKey, rowKey);
                log.LogInformation($"Deleted order: {partitionKey}/{rowKey}");

                // Sends message to the queue for auditing/downstream processes
                var queueMessage = new QueueMessageDto
                {
                    OperationType = "ORDER_DELETED",
                    PartitionKey = partitionKey,
                    RowKey = rowKey,
                    CustomerName = order.CustomerName,
                    TotalAmount = order.TotalAmount,
                    CreationTimeUtc = DateTime.UtcNow
                };

                await _transactionQueueService.SendTransactionMessageAsync(queueMessage);

                return new OkResult();
            }
            // Catchs specific Table Storage exception for Not Found errors
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                log.LogWarning($"Delete failed (404 Not Found): PartitionKey: {partitionKey}, RowKey: {rowKey}");
                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Critical error during order deletion: {partitionKey}/{rowKey}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }

}