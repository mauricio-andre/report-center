using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReportCenter.Common.Providers.MessageQueues.Dtos;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;
using ReportCenter.RabbitMQ.Dtos;
using ReportCenter.RabbitMQ.Options;

namespace ReportCenter.RabbitMQ.Services;

public class RabbitMQConsumer : IMessageConsumer
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMQConsumer> _logger;
    private readonly RabbitMQOptions _rabbitMqOptions;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMQConsumer(
        IConnectionFactory connectionFactory,
        ILogger<RabbitMQConsumer> logger,
        IOptions<RabbitMQOptions> rabbitMqOptions)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _rabbitMqOptions = rabbitMqOptions.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);
    }

    public async Task RegistryConsumer(Func<object, CancellationToken, Task> ReceivedAsync, CancellationToken cancellationToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel!);
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            await ReceivedAsync(eventArgs, cancellationToken);
        };

        await _channel!.BasicConsumeAsync(_rabbitMqOptions.QueueName, autoAck: false, consumer, cancellationToken);
        _logger.LogInformation("Waiting for messages...");
    }

    public ReportMessageDto DeserializeMessage(object args)
    {
        var converted = args as BasicDeliverEventArgs;
        var headers = converted!.BasicProperties.Headers;
        var body = converted.Body.ToArray();

        return new ReportMessageDto(
            Id: JsonSerializer.Deserialize<MessageBodyDto>(Encoding.UTF8.GetString(body))!.Id,
            Domain: Encoding.UTF8.GetString((byte[])headers![nameof(ReportMessageDto.Domain)]!),
            Application: Encoding.UTF8.GetString((byte[])headers![nameof(ReportMessageDto.Application)]!),
            ReportType: (ReportType)short.Parse(headers![nameof(ReportMessageDto.ReportType)]!.ToString()!),
            DocumentName: Encoding.UTF8.GetString((byte[])headers![nameof(ReportMessageDto.DocumentName)]!),
            DocumentKey: Encoding.UTF8.GetString((byte[])headers![nameof(ReportMessageDto.DocumentKey)]!),
            Version: short.Parse(headers![nameof(ReportMessageDto.Version)]!.ToString()!)
        );
    }

    public ValueTask AbortProcessingAsync(object args, Exception ex, CancellationToken cancellationToken = default)
    {
        var converted = args as BasicDeliverEventArgs;
        return _channel!.BasicNackAsync(converted!.DeliveryTag, false, false, cancellationToken);
    }

    public ValueTask CompleteMessage(object args, CancellationToken cancellationToken = default)
    {
        var converted = args as BasicDeliverEventArgs;
        return _channel!.BasicAckAsync(converted!.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
    }

    public ValueTask<string?> GetParentTransactionId(object args)
    {
        var converted = args as BasicDeliverEventArgs;
        string? transactionId = null;
        if (converted!.BasicProperties.Headers!.TryGetValue("TransactionId", out var headerTransactionId))
            transactionId = Encoding.UTF8.GetString((byte[])headerTransactionId!);

        return ValueTask.FromResult(transactionId);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _channel!.CloseAsync(cancellationToken: cancellationToken);
        await _connection!.CloseAsync(cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        if (_channel != null && _channel.IsOpen)
            _channel.CloseAsync().Wait();

        if (_connection != null && _connection.IsOpen)
            _connection.CloseAsync().Wait();

        _channel?.Dispose();
        _connection?.Dispose();
    }
}

