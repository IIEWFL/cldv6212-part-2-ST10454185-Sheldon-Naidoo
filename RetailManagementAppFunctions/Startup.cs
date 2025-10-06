using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RetailManagementAppFunctions.Services;
using System;
using System.IO;
using System.Reflection;

// The assembly attribute tells the Azure Functions runtime which class to execute upon startup.
[assembly: FunctionsStartup(typeof(RetailManagementAppFunctions.Startup))]

namespace RetailManagementAppFunctions
{
    // Configures Dependency Injection for the Azure Function App.
    // All storage services are initialized here using a single connection string and registered as singletons.

    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Code Attribution
            // This method was adapted from C-Sharpcorner
            // https://www.c-sharpcorner.com/UploadFile/8911c4/singleton-design-pattern-in-C-Sharp/
            // Mahesh Alle
            // https://www.c-sharpcorner.com/members/mahesh-alle

            // Register IOrderTableService mapped to OrderTableService
            builder.Services.AddSingleton<IOrderTableService>(sp =>
                CreateStorageService<OrderTableService>(sp, "ordertable", "table"));

            // Register IProductImageBlobStorageService mapped to ProductImageBlobStorageService
            builder.Services.AddSingleton<IProductImageBlobStorageService>(sp =>
                CreateStorageService<ProductImageBlobStorageService>(sp, "product-pictures", "blob"));

            // Register ITransactionQueueService mapped to TransactionQueueService
            builder.Services.AddSingleton<ITransactionQueueService>(sp =>
                CreateStorageService<TransactionQueueService>(sp, "retail-logs", "queue"));

            // Register tableStorage services
            builder.Services.AddSingleton(sp =>
                CreateStorageService<CustomerTableService>(sp, "customertable", "table"));

            builder.Services.AddSingleton(sp =>
                CreateStorageService<OrderTableService>(sp, "ordertable", "table"));

            builder.Services.AddSingleton(sp =>
                CreateStorageService<ProductTableService>(sp, "producttable", "table"));

            // Register blobStorage service
            builder.Services.AddSingleton(sp =>
                // The serviceIdentifier is the container name: product-pictures
                CreateStorageService<ProductImageBlobStorageService>(sp, "product-pictures", "blob"));

            // Register fileShare service
            builder.Services.AddSingleton(sp =>
                // The serviceIdentifier is the file share name: retail-log-files
                CreateStorageService<DocumentFileShareService>(sp, "retail-log-files", "fileshare"));

            // Register Queue service
            builder.Services.AddSingleton(sp =>
                // The serviceIdentifier is the queue name: retail-logs
                CreateStorageService<TransactionQueueService>(sp, "retail-logs", "queue"));
        }

        // A generic helper method to centralize service creation, configuration validation, and logging for different storage service types.
       
        private T CreateStorageService<T>(IServiceProvider sp, string serviceIdentifier, string serviceTypeKey) where T : class
        {
            // Code Attribution
            // This method was adapted from Microsoft Learn
            // https://learn.microsoft.com/en-us/dotnet/api/system.iserviceprovider?view=net-9.0
            // Microsoft Learn

            var logger = sp.GetRequiredService<ILogger<Startup>>();
            // Retrieves the connection string from environment variables/local.settings.json
            var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            // Logs for clarity, checking the specific identifier (table/queue/container name)
            logger.LogInformation($"Attempting to initialize {typeof(T).Name} using key '{serviceTypeKey}' and identifier '{serviceIdentifier}'.");


            if (string.IsNullOrEmpty(storageConnectionString))
            {
                logger.LogError("Storage Connection String or service identifier is not set properly.");
                throw new InvalidOperationException("Configuration is invalid: StorageConnectionString is missing.");
            }

            if (string.IsNullOrEmpty(serviceIdentifier))
            {
                logger.LogError("Service Identifier (e.g., table/queue name) is not set.");
                throw new InvalidOperationException("Configuration is invalid: Service Identifier is missing.");
            }

            // Centralized instantiation logic based on the serviceTypeKey
            T serviceInstance = serviceTypeKey switch
            {
                // Table Services: Note the use of the actual service name in the switch condition for mapping
                "table" when typeof(T) == typeof(CustomerTableService) => new CustomerTableService(storageConnectionString, serviceIdentifier) as T,
                "table" when typeof(T) == typeof(OrderTableService) => new OrderTableService(storageConnectionString, serviceIdentifier) as T,
                "table" when typeof(T) == typeof(ProductTableService) => new ProductTableService(storageConnectionString, serviceIdentifier) as T,

                // Blob Service
                "blob" => new ProductImageBlobStorageService(storageConnectionString, serviceIdentifier) as T,

                // File Share Service
                "fileshare" => new DocumentFileShareService(storageConnectionString, serviceIdentifier) as T,

                // Queue Service
                "queue" => new TransactionQueueService(storageConnectionString, serviceIdentifier) as T,

                _ => throw new NotImplementedException($"Service type or configuration not supported for key: {serviceTypeKey}")
            };

            logger.LogInformation($"Successfully configured {typeof(T).Name} with identifier: {serviceIdentifier}");
            return serviceInstance;
        }
    }
}
