using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReportCenter.AzureBlobStorages.Options;

namespace ReportCenter.AzureBlobStorages.Extensions;

public static class AzureBlobStorageServiceCollectionExtension
{
    public static IServiceCollection AddAzureBlobStorageProvider(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        services.AddSingleton<BlobServiceClient>(_ => new BlobServiceClient(connectionString));
        services.Configure<AzureBlobStorageOptions>(configuration.GetSection(AzureBlobStorageOptions.Position));

        return services;
    }
}
