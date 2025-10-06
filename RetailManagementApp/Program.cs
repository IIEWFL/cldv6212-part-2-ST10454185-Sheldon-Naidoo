using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RetailManagementApp.Services;
using System;

namespace RetailManagementApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var storageConnectionString = builder.Configuration.GetConnectionString("StorageConnectionString")
                ?? throw new InvalidOperationException("Storage connection string is missing");

            builder.Services.AddSingleton(new CustomerTableService(storageConnectionString, "Customers"));
            builder.Services.AddSingleton(new ProductTableService(storageConnectionString, "Products"));
            builder.Services.AddSingleton(new ProductImageBlobStorageService(storageConnectionString, "product-images"));
            builder.Services.AddSingleton(new TransactionQueueService(storageConnectionString, "transaction-queue"));
            builder.Services.AddSingleton(new DocumentFileShareService(storageConnectionString, "document-share"));

            // --- API CLIENTS (Connect to Function App) ---

            // The base URL for the Function App is now configured using AddHttpClient
            var functionBaseUrl = builder.Configuration["AzureFunctionBaseURL"]
                ?? throw new InvalidOperationException("Azure Functions Base URL (AzureFunctionBaseURL) is missing in configuration.");

            var functionBaseUri = new Uri(functionBaseUrl);

            builder.Services.AddHttpClient<IFunctionService, FunctionService>(client =>
            {
                // Set the base address using the configuration value
                client.BaseAddress = functionBaseUri;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
