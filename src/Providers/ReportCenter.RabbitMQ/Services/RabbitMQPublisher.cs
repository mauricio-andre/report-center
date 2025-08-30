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

public class RabbitMQPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly RabbitMQOptions _rabbitMqOptions;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMQPublisher(
        IConnectionFactory connectionFactory,
        IOptions<RabbitMQOptions> rabbitMqOptions)
    {
        _connectionFactory = connectionFactory;
        _rabbitMqOptions = rabbitMqOptions.Value;
    }

    private async Task CreateChannel(CancellationToken cancellationToken = default)
    {
        if (_connection == null)
            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        if (_channel == null)
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    public async Task PublishProcessesAsync(ReportMessageDto message, CancellationToken cancellationToken = default)
    {
        await CreateChannel();

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

        await _channel!.BasicPublishAsync(
            exchange: "",
            routingKey: _rabbitMqOptions.ProcessesQueueName,
            mandatory: true,
            basicProperties: props,
            body: body,
            cancellationToken);
    }

    public async Task PublishProgressAsync(
        ReportMessageProgressDto message,
        CancellationToken cancellationToken = default)
    {
        await CreateChannel();

        var messageObject = new MessageProgressBodyDto(
            message.Id,
            message.ProcessTimer,
            message.ProcessMessage,
            message.Requeue);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageObject));

        var props = new BasicProperties
        {
            Headers = new Dictionary<string, object?>
            {
                [nameof(ReportMessageProgressDto.Domain)] = Encoding.UTF8.GetBytes(message.Domain),
                [nameof(ReportMessageProgressDto.Application)] = Encoding.UTF8.GetBytes(message.Application),
                [nameof(ReportMessageProgressDto.ReportType)] = message.ReportType.GetHashCode(),
                [nameof(ReportMessageProgressDto.DocumentName)] = Encoding.UTF8.GetBytes(message.DocumentName),
                [nameof(ReportMessageProgressDto.DocumentKey)] = Encoding.UTF8.GetBytes(message.DocumentKey),
                [nameof(ReportMessageProgressDto.Version)] = message.Version,
                [nameof(ReportMessageProgressDto.ProcessState)] = (short)message.ProcessState,
                ["TransactionId"] = Activity.Current?.Id,
            }
        };

        await _channel!.BasicPublishAsync(
            exchange: "",
            routingKey: _rabbitMqOptions.ProcessesQueueName,
            mandatory: true,
            basicProperties: props,
            body: body,
            cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        if (_channel != null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
        }
    }
}
