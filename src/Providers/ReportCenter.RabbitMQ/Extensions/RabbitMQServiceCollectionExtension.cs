using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace ReportCenter.RabbitMQ.Extensions;

public static class RabbitMQServiceCollectionExtension
{
    public static IServiceCollection AddRabbitMQConsumer(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
        {
            Uri = new Uri(connectionString)
        });

        return services;
    }
}
