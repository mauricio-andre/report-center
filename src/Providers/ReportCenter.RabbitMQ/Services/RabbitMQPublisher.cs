using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ReportCenter.Common.Providers.MessageQueues.Dtos;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;
using ReportCenter.RabbitMQ.Dtos;
using ReportCenter.RabbitMQ.Options;

namespace ReportCenter.RabbitMQ.Services;

public class RabbitMQPublisher : IMessagePublisher
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly RabbitMQOptions _rabbitMqOptions;

    public RabbitMQPublisher(
        IConnectionFactory connectionFactory,
        IOptions<RabbitMQOptions> rabbitMqOptions)
    {
        _connectionFactory = connectionFactory;
        _rabbitMqOptions = rabbitMqOptions.Value;
    }

    public async Task PublishAsync(ReportMessageDto message, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var messageObject = new MessageBodyDto(message.Id);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageObject));

        var props = new BasicProperties
        {
            Headers = new Dictionary<string, object?>
            {
                [nameof(ReportMessageDto.Domain)] = Encoding.UTF8.GetBytes(message.Domain),
                [nameof(ReportMessageDto.Application)] = Encoding.UTF8.GetBytes(message.Application),
                [nameof(ReportMessageDto.ReportType)] = message.ReportType.GetHashCode(),
                [nameof(ReportMessageDto.DocumentName)] = Encoding.UTF8.GetBytes(message.DocumentName),
                [nameof(ReportMessageDto.DocumentKey)] = Encoding.UTF8.GetBytes(message.DocumentKey),
                [nameof(ReportMessageDto.Version)] = message.Version,
                ["TransactionId"] = Activity.Current?.Id,
            }
        };

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: _rabbitMqOptions.QueueName,
            mandatory: true,
            basicProperties: props,
            body: body,
            cancellationToken);
    }
}
