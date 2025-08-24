using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using ReportCenter.RabbitMQ.Options;

namespace ReportCenter.RabbitMQ.Extensions;

public static class RabbitMQServiceCollectionExtension
{
    public static IServiceCollection AddRabbitMQProvider(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
        {
            Uri = new Uri(connectionString)
        });

        services.Configure<RabbitMQOptions>(configuration.GetSection(RabbitMQOptions.Position));

        return services;
    }
}
