using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using ReportCenter.AzureServiceBus.Dtos;
using ReportCenter.AzureServiceBus.Options;
using ReportCenter.Common.Providers.MessageQueues.Dtos;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;

namespace ReportCenter.AzureServiceBus.Services;

public class AzureServiceBusPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly ServiceBusSender _processesSender;
    private readonly ServiceBusSender _ProgressSender;

    public AzureServiceBusPublisher(
        ServiceBusClient client,
        IOptions<AzureServiceBusOptions> options)
    {
        _processesSender = client.CreateSender(options.Value.ProcessesTopicName);
        _ProgressSender = client.CreateSender(options.Value.ProgressTopicName);
    }

    public async Task PublishProcessesAsync(ReportMessageDto message, CancellationToken cancellationToken = default)
    {
        var messageObject = new MessageBodyDto(message.Id);
        var body = JsonSerializer.SerializeToUtf8Bytes(messageObject);

        var serviceBusMessage = new ServiceBusMessage(body)
        {
            ApplicationProperties =
            {
                [nameof(ReportMessageDto.Domain)] = message.Domain,
                [nameof(ReportMessageDto.Application)] = message.Application,
                [nameof(ReportMessageDto.ReportType)] = message.ReportType.GetHashCode(),
                [nameof(ReportMessageDto.DocumentName)] = message.DocumentName,
                [nameof(ReportMessageDto.DocumentKey)] = message.DocumentKey,
                [nameof(ReportMessageDto.Version)] = message.Version,
                ["TransactionId"] = Activity.Current?.Id
            }
        };

        await _processesSender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }

    public async Task PublishProgressAsync(
        ReportMessageProgressDto message,
        CancellationToken cancellationToken = default)
    {
        var messageObject = new MessageProgressBodyDto(
            message.Id,
            message.ProcessTimer,
            message.ProcessMessage,
            message.Requeue);

        var body = JsonSerializer.SerializeToUtf8Bytes(messageObject);

        var serviceBusMessage = new ServiceBusMessage(body)
        {
            ApplicationProperties =
            {
                [nameof(ReportMessageProgressDto.Domain)] = message.Domain,
                [nameof(ReportMessageProgressDto.Application)] = message.Application,
                [nameof(ReportMessageProgressDto.ReportType)] = message.ReportType.GetHashCode(),
                [nameof(ReportMessageProgressDto.DocumentName)] = message.DocumentName,
                [nameof(ReportMessageProgressDto.DocumentKey)] = message.DocumentKey,
                [nameof(ReportMessageProgressDto.Version)] = message.Version,
                [nameof(ReportMessageProgressDto.ProcessState)] = (short)message.ProcessState,
                ["TransactionId"] = Activity.Current?.Id
            }
        };

        await _ProgressSender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _processesSender.DisposeAsync();
        await _ProgressSender.DisposeAsync();
    }
}
