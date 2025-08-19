using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReportCenter.AzureServiceBus.Options;

namespace ReportCenter.AzureServiceBus.Extensions;

public static class AzureServiceBusServiceCollectionExtension
{
    public static IServiceCollection AddAzureServiceBusConsumer(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        services.AddSingleton<ServiceBusClient>(_ => new ServiceBusClient(connectionString));
        services.Configure<AzureServiceBusOptions>(configuration.GetSection(AzureServiceBusOptions.Position));

        return services;
    }
}
