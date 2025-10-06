using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailManagementAppFunctions.Services
{
    // Interface for Dependency Injection
    public interface IProductImageBlobStorageService
    {
        // Standardized to 'Photo' to match the function signatures
        Task<string> UploadPhotoAsync(string blobName, Stream fileStream);
        Task DeletePhotoAsync(string blobName);
        Task<List<BlobItem>> GetImagesAsync();
    }

    public class ProductImageBlobStorageService : IProductImageBlobStorageService
    {
        private readonly BlobContainerClient _blobContainerClient;

        public ProductImageBlobStorageService(string storageConnectionString, string containerName)
        {
            var serviceClient = new BlobServiceClient(storageConnectionString);
            _blobContainerClient = serviceClient.GetBlobContainerClient(containerName);
            // Ensure the container is created if it doesn't exist
            _blobContainerClient.CreateIfNotExists();
        }

        public async Task<List<BlobItem>> GetImagesAsync()
        {
            var blobs = new List<BlobItem>();
            await foreach (var blobItem in _blobContainerClient.GetBlobsAsync())
            {
                blobs.Add(blobItem);
            }
            return blobs;
        }


        // Uploads a photo stream and returns the secure SAS URI.

        public async Task<string> UploadPhotoAsync(string blobName, Stream stream)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobName);
            // Overwrite existing files with the same name
            await blobClient.UploadAsync(stream, true);
            // Return the secure URI
            return GetBlobUriWithSas(blobClient);
        }

        // Deletes a photo blob if it exists.

        public async Task DeletePhotoAsync(string blobName)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobName);
            // Use DeleteIfExistsAsync to avoid throwing an exception if the blob is already gone
            await blobClient.DeleteIfExistsAsync();
        }


        // Generates a time-limited Shared Access Signature (SAS) URI for read access to the blob.
        private string GetBlobUriWithSas(BlobClient blobClient)
        {
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b", // 'b' for single blob access
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1), // SAS valid for 1 hour
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                return sasUri.ToString();
            }
            else
            {
                throw new InvalidOperationException("Blob client does not support generating SAS URIs. Ensure a valid storage account connection string is used.");
            }
        }
    }

}