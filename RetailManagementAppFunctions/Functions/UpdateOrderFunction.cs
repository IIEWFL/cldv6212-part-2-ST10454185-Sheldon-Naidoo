using System;
using System.IO;
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
    public class UpdateOrderFunction
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
        public UpdateOrderFunction(
            OrderTableService orderTableService,
            ProductImageBlobStorageService productStorageService,
            TransactionQueueService transactionQueueService)
        {
            _orderTableService = orderTableService;
            _productStorageService = productStorageService;
            _transactionQueueService = transactionQueueService;
        }

        [FunctionName("UpdateOrder")]
        public async Task<IActionResult> Run(
            // Routes updated to match standard REST pattern for update operations
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "orders/{partitionKey}/{rowKey}")] HttpRequest req, // Changed "post" to "put" for semantic REST compliance
            string partitionKey,
            string rowKey,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request to update Order: {partitionKey}/{rowKey}.");

            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return new BadRequestObjectResult("PartitionKey and RowKey must be provided in the route for update.");
            }

            // Retrieves the existing order entity
            var existingOrderEntity = await _orderTableService.GetOrderAsync(partitionKey, rowKey);

            if (existingOrderEntity == null)
            {
                log.LogWarning($"Order not found for Partition Key: {partitionKey}, Row Key: {rowKey}");
                return new NotFoundResult();
            }

            try
            {
                // Reads form data 
                var formData = await req.ReadFormAsync();

                // NOTE: TotalAmount is parsed as it comes as a string from form data.
                var customerName = formData["CustomerName"];
                var productName = formData["ProductName"];
                var totalAmountString = formData["TotalAmount"];

                // Updates the entity with new values if provided
                if (!string.IsNullOrEmpty(customerName))
                {
                    existingOrderEntity.CustomerName = customerName;
                }

                if (!string.IsNullOrEmpty(productName))
                {
                    existingOrderEntity.ProductName = productName;
                }

                if (!string.IsNullOrEmpty(totalAmountString) && decimal.TryParse(totalAmountString, out decimal newTotalAmount))
                {
                    existingOrderEntity.TotalAmount = (double)newTotalAmount;
                }

                // Handles file upload
                if (formData.Files.Count > 0)
                {
                    var photo = formData.Files[0];
                    using var stream = photo.OpenReadStream();

                    // Cleanup existing photo if one exists
                    if (!string.IsNullOrEmpty(existingOrderEntity.ProductPhotoBlobName))
                    {
                        await _productStorageService.DeletePhotoAsync(existingOrderEntity.ProductPhotoBlobName);
                    }

                    // Uploads new photo and store the URL and blob name
                    var blobFileName = $"{existingOrderEntity.RowKey}-{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";

                    existingOrderEntity.ProductPhotoUrl = await _productStorageService.UploadPhotoAsync(blobFileName, stream);
                    existingOrderEntity.ProductPhotoBlobName = blobFileName;
                }

                // Saves the updated order entity 
                await _orderTableService.UpdateOrderAsync(existingOrderEntity);

                // Converts to DTO and send queue message using the standard payload
                var queueMessage = new QueueMessageDto
                {
                    OperationType = "ORDER_UPDATED",
                    PartitionKey = existingOrderEntity.PartitionKey,
                    RowKey = existingOrderEntity.RowKey,
                    CustomerName = existingOrderEntity.CustomerName,
                    TotalAmount = existingOrderEntity.TotalAmount,
                    CreationTimeUtc = DateTime.UtcNow
                };

                await _transactionQueueService.SendTransactionMessageAsync(queueMessage);

                // Returns the updated entity
                return new OkObjectResult(existingOrderEntity);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Critical error during order update: {partitionKey}/{rowKey}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}