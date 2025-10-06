using Microsoft.AspNetCore.Http;
using RetailManagementApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RetailManagementApp.Services
{

    // Defines the contract for interacting with the Product Management Azure Functions API.

    public interface IProductFunctionService
    {
        // CRUD Operations
        Task<List<Product>> GetProductsAsync();
        Task<Product> GetProductAsync(string partitionKey, string rowKey);

        // Uses multipart form data to send Product model and IFormFile (image) to the function.
        Task<bool> AddProductAsync(Product product, IFormFile image);

        // The function will handle the update logic, including optional image replacement.
        Task<bool> UpdateProductAsync(Product product, IFormFile? image);

        // Deletes the product entity and its associated blob.
        Task<bool> DeleteProductAsync(string partitionKey, string rowKey, string productPhotoBlobName);
    }
}