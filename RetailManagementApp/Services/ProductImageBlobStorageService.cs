using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailManagementApp.Services
{
    public class ProductImageBlobStorageService
    {
        private readonly BlobContainerClient _blobContainerClient;

        public ProductImageBlobStorageService(string storageConnectionString, string containerName)
        {
            var serviceClient = new BlobServiceClient(storageConnectionString);
            _blobContainerClient = serviceClient.GetBlobContainerClient(containerName);
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

        public async Task<string> UploadImageAsync(string blobName, Stream stream)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(stream, true);
            return GetBlobUriWithSas(blobClient);
        }

        public async Task DeleteImageAsync(string blobName)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        private string GetBlobUriWithSas(BlobClient blobClient)
        {
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                return sasUri.ToString();
            }
            else
            {
                throw new InvalidOperationException("Blob client does not support generating SAS URIs. " +
                                                    "Ensure a valid shared key credential is used.");
            }
        }
    }
}
