using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using MiniCommerce.InventoryService.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniCommerce.InventoryService.Infrastructure.Storage
{
    public class AzureBlobStorageService : IFileStorage
    {
        private readonly BlobContainerClient _container;

        public AzureBlobStorageService(IConfiguration config)
        {
            var conn = config["AzureStorage:ConnectionString"]!;
            var containerName = config["AzureStorage:ContainerName"]!;

            var blobService = new BlobServiceClient(conn);
            _container = blobService.GetBlobContainerClient(containerName);

            _container.CreateIfNotExists(PublicAccessType.Blob);
        }

        public async Task<string> UploadAsync(Stream fileStream, string contentType, string fileName)
        {
            var blobName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = _container.GetBlobClient(blobName);

            await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });

            return blobClient.Uri.ToString();
        }
    }
}
