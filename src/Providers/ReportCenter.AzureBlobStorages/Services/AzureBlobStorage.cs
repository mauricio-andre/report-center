using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using ReportCenter.AzureBlobStorages.Options;
using ReportCenter.Common.Providers.Storage.Interfaces;

namespace ReportCenter.AzureBlobStorages.Services;

public class AzureBlobStorage : IStorageService
{
    private readonly BlobContainerClient _blobContainerClient;

    public AzureBlobStorage(
        BlobServiceClient blobServiceClient,
        IOptions<AzureBlobStorageOptions> options)
    {
        _blobContainerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
    }

    public async Task<Stream> OpenWriteAsync(
        string fullFileName,
        DateTimeOffset expirationDate,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        await _blobContainerClient.CreateIfNotExistsAsync();
        return await _blobContainerClient
            .GetBlobClient(fullFileName)
            .OpenWriteAsync(
                true,
                new BlobOpenWriteOptions()
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType ?? "application/octet-stream" },
                    Tags = new Dictionary<string, string>()
                    {
                        {
                            "expirationDate", expirationDate.ToString()
                        }
                    }
                },
                cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(string fullFileName, CancellationToken cancellationToken = default)
    {
        await _blobContainerClient.CreateIfNotExistsAsync();

        if (await _blobContainerClient.GetBlobClient(fullFileName).ExistsAsync())
            return await _blobContainerClient.GetBlobClient(fullFileName).OpenReadAsync(null, cancellationToken);

        return null;
    }

    public async Task SaveAsync(
        string fullFileName,
        Stream content,
        DateTimeOffset expirationDate,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        await _blobContainerClient.CreateIfNotExistsAsync();
        await _blobContainerClient
            .GetBlobClient(fullFileName)
            .UploadAsync(
                content,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType ?? "application/octet-stream" },
                    Tags = new Dictionary<string, string>()
                    {
                        {
                            "expirationDate", expirationDate.ToString()
                        }
                    }
                },
                cancellationToken);
    }
}
