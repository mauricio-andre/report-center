using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportCenter.AzureServiceBus.Dtos;
using ReportCenter.AzureServiceBus.Options;
using ReportCenter.Common.Providers.MessageQueues.Dtos;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;

namespace ReportCenter.AzureServiceBus.Services;

public sealed class AzureServiceBusConsumer : IMessageConsumer
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<AzureServiceBusConsumer> _logger;
    private readonly AzureServiceBusOptions _options;

    private ServiceBusProcessor? _processor;

    public AzureServiceBusConsumer(
        ServiceBusClient client,
        ILogger<AzureServiceBusConsumer> logger,
        IOptions<AzureServiceBusOptions> options)
    {
        _client = client;
        _logger = logger;
        _options = options.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _processor = _client.CreateProcessor(
            _options.TopicName,
            _options.SubscriptionName,
            new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false
            });

        return Task.CompletedTask;
    }

    public async Task RegistryConsumer(Func<object, CancellationToken, Task> ReceivedAsync, CancellationToken cancellationToken = default)
    {
        _processor!.ProcessMessageAsync += async (ProcessMessageEventArgs args) =>
        {
            await ReceivedAsync(args, cancellationToken);
        };

        _processor!.ProcessErrorAsync += (ProcessErrorEventArgs args) =>
        {
            _logger.LogError(
                args.Exception,
                "Erro no processamento do Service Bus (Entity {EntityPath}, ErrorSource {ErrorSource})",
                args.EntityPath,
                args.ErrorSource);

            return Task.CompletedTask;
        };

        await _processor.StartProcessingAsync(cancellationToken);

        _logger.LogInformation("Waiting for messages on Topic {Topic}/{Subscription}...",
            _options.TopicName, _options.SubscriptionName);
    }

    public ReportMessageDto DeserializeMessage(object args)
    {
        var converted = args as ProcessMessageEventArgs;
        var body = converted!.Message.Body.ToArray();
        var props = converted.Message.ApplicationProperties;

        return new ReportMessageDto(
            Id: JsonSerializer.Deserialize<MessageBodyDto>(Encoding.UTF8.GetString(body))!.Id,
            Domain: props[nameof(ReportMessageDto.Domain)].ToString()!,
            Application: props[nameof(ReportMessageDto.Application)].ToString()!,
            ReportType: (ReportType)Convert.ToInt16(props[nameof(ReportMessageDto.ReportType)]),
            DocumentName: props[nameof(ReportMessageDto.DocumentName)].ToString()!,
            DocumentKey: props[nameof(ReportMessageDto.DocumentKey)].ToString()!,
            Version: Convert.ToInt16(props[nameof(ReportMessageDto.Version)])
        );
    }

    public ValueTask AbortProcessingAsync(object args, Exception ex, CancellationToken cancellationToken = default)
    {
        var converted = args as ProcessMessageEventArgs;
        return new ValueTask(converted!.DeadLetterMessageAsync(converted.Message, ex.Message, ex.ToString(), cancellationToken));
    }

    public ValueTask CompleteMessage(object args, CancellationToken cancellationToken = default)
    {
        var converted = args as ProcessMessageEventArgs;
        return new ValueTask(converted!.CompleteMessageAsync(converted.Message, cancellationToken));
    }

    public ValueTask<string?> GetParentTransactionId(object args)
    {
        var converted = args as ProcessMessageEventArgs;
        string? transactionId = null;
        if (converted!.Message.ApplicationProperties.TryGetValue("TransactionId", out var headerTransactionId))
            transactionId = headerTransactionId?.ToString();

        return ValueTask.FromResult(transactionId);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.CloseAsync();
            await _processor.DisposeAsync();
            _processor = null;
        }
    }

    public void Dispose()
    {
        if (_processor != null)
        {
            _processor.StopProcessingAsync().Wait();
            _processor.CloseAsync().Wait();
            _processor.DisposeAsync().AsTask().Wait();
        }

        _client.DisposeAsync().AsTask().Wait();
    }
}
