using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Sas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailManagementAppFunctions.Services
{
    public class DocumentFileShareService
    {
        private readonly ShareClient _shareClient;

        public DocumentFileShareService(string storageConnectionString, string shareName)
        {
            var serviceClient = new ShareServiceClient(storageConnectionString);
            _shareClient = serviceClient.GetShareClient(shareName);
            _shareClient.CreateIfNotExists();
        }

        public async Task<string> GetFileAsync(string fileName)
        {
            var directoryClient = _shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            if (await fileClient.ExistsAsync())
            {
                return GetFileUriWithSas(fileClient);
            }
            return null;
        }

        public async Task UploadFileAsync(string fileName, Stream fileStream)
        {
            // Code Attribution
            // This method was adapted from Microsoft Learn
            // https://learn.microsoft.com/en-us/dotnet/api/system.io.binaryreader.readbytes?view=net-9.0
            // Microsoft Learn

            var directoryClient = _shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            await fileClient.CreateAsync(fileStream.Length);

            long position = 0;
            const int bufferSize = 4 * 1024 * 1024;
            byte[] buffer = new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, bufferSize)) > 0)
            {
                using var memoryStream = new MemoryStream(buffer, 0, bytesRead);
                await fileClient.UploadRangeAsync(
                    ShareFileRangeWriteType.Update,
                    new HttpRange(position, bytesRead),
                    memoryStream);
                position += bytesRead;
            }
        }
        public async Task DeleteFileAsync(string fileName)
        {
            var directoryClient = _shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);
            await fileClient.DeleteIfExistsAsync();
        }

        private string GetFileUriWithSas(ShareFileClient fileClient)
        {
            if (fileClient.CanGenerateSasUri)
            {
                var sasBuilder = new ShareSasBuilder
                {
                    ShareName = fileClient.ShareName,
                    FilePath = fileClient.Path,
                    Resource = "f",
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                };

                sasBuilder.SetPermissions(ShareFileSasPermissions.Read);

                var sasUri = fileClient.GenerateSasUri(sasBuilder);
                return sasUri.ToString();
            }
            else
            {
                throw new InvalidOperationException("File client does not support generating SAS URIs. " +
                                                    "Ensure a valid shared key credential is used.");
            }
        }
    }
}
