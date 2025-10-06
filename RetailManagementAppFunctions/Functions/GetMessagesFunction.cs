using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using RetailManagementAppFunctions.Services;
using System.Threading.Tasks;

namespace RetailManagementAppFunctions.Functions
{
    public class GetMessagesFunction
    {
        // Code Attribution
        // This method was adapted from Microsoft Learn
        // https://learn.microsoft.com/en-us/aspnet/mvc/overview/getting-started/getting-started-with-ef-using-mvc/creating-a-more-complex-data-model-for-an-asp-net-mvc-application
        // Microsoft Learn

        private readonly TransactionQueueService _queueStorageService;

        // Constructor used by Azure Functions Dependency Injection to inject the queue service.
        public GetMessagesFunction(TransactionQueueService queueStorageService)
        {
            _queueStorageService = queueStorageService;
        }

        [FunctionName("GetMessages")] 
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "messages")] HttpRequest req, // Changed route to "messages" for clarity
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request to get all transaction messages from the queue.");

            try
            {
                // Retrieves all messages from the transaction queue
                var messages = await _queueStorageService.GetMessagesAsync();

                // Returns the list of messages
                return new OkObjectResult(messages);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error retrieving messages from the queue: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
