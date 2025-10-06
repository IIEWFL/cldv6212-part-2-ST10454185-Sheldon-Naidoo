using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs; 
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RetailManagementAppFunctions.Models; 
using RetailManagementAppFunctions.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RetailManagementAppFunctions.Functions
{
    public class UploadLogFileFunction
    {
        // Code Attribution
        // This method was adapted from Microsoft Learn
        // https://learn.microsoft.com/en-us/aspnet/mvc/overview/getting-started/getting-started-with-ef-using-mvc/creating-a-more-complex-data-model-for-an-asp-net-mvc-application
        // Microsoft Learn

        // Code Attribution
        // This method was adapted from Microsoft Learn
        // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to
        // Microsoft Learn

        // Code Attribution
        // This method was adapted from W3Schools
        // https://www.w3schools.com/cs/cs_constructors.php
        // W3Schools


        private readonly DocumentFileShareService _fileShareStorageService;
        private readonly TransactionQueueService _queueStorageService;


        // Constructor used by Azure Functions Dependency Injection.
        // Services are injected based on the registration in Startup.cs.

        public UploadLogFileFunction(DocumentFileShareService fileShareStorageService, TransactionQueueService queueStorageService)
        {
            _fileShareStorageService = fileShareStorageService;
            _queueStorageService = queueStorageService;
        }

        [FunctionName("UploadLogFile")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orderlogs")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function received a request to generate and upload a log file.");

            // Reads request body to get the file name/identifier
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Using dynamic for simple JSON parsing is fine here
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string name = data?.name;

            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult("Please provide a 'name' field in the request body to name the log file.");
            }

            try
            {
                // Retrieves all messages from the transaction queue
                log.LogInformation("Retrieving messages from transaction queue...");
                List<QueueLogViewModel> logMessages = await _queueStorageService.GetMessagesAsync();

                if (logMessages == null || logMessages.Count == 0)
                {
                    log.LogInformation("No messages found in the queue. Skipping file generation.");
                    // Return success but indicate no data was processed
                    return new OkObjectResult($"Log file generation skipped: No pending transactions found in the queue.");
                }

                // Creates CSV content from queue messages
                var content = new StringBuilder();
                content.AppendLine("MessageId,InsertionTime,MessageText");

                foreach (var logMessage in logMessages)
                {
                    // Escapes double quotes within the message text for proper CSV formatting
                    var messageText = logMessage.MessageText?.Replace("\"", "\"\"");
                    var insertionTime = logMessage.InsertionTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;

                    content.AppendLine($"\"{logMessage.MessageId}\",\"{insertionTime}\",\"{messageText}\"");
                }

                // Uploads the log file to Azure Fileshare
                string fileName = $"{name}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

                log.LogInformation($"Uploading log file '{fileName}' to file share...");

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content.ToString())))
                {
                    await _fileShareStorageService.UploadFileAsync(fileName, stream);
                }

                //  Clears the queue after successful archival
                log.LogInformation("Successfully uploaded file. Clearing transaction queue...");
                await _queueStorageService.ClearQueueAsync();

                // Success response
                return new OkObjectResult($"Log file '{fileName}' uploaded successfully to file share and transaction queue was cleared.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error uploading log file: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
