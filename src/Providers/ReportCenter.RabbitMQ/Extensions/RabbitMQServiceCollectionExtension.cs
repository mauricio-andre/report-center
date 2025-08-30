using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;
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

        // Garante a assinatura da classe como Scoped
        var publisher = services.Last(s => s.ServiceType == typeof(IMessagePublisher));
        services.Remove(publisher);
        services.AddScoped(publisher.ServiceType, publisher.ImplementationType!);

        var consumer = services.Last(s => s.ServiceType == typeof(IMessageConsumer));
        services.Remove(consumer);
        services.AddScoped(consumer.ServiceType, consumer.ImplementationType!);

        return services;
    }
}
