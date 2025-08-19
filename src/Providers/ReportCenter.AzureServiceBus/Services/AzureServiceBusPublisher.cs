using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using ReportCenter.AzureServiceBus.Dtos;
using ReportCenter.AzureServiceBus.Options;
using ReportCenter.Common.Providers.MessageQueues.Dtos;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;

namespace ReportCenter.AzureServiceBus.Services;

public class AzureServiceBusPublisher : IMessagePublisher
{
    private readonly ServiceBusClient _client;
    private readonly AzureServiceBusOptions _options;

    public AzureServiceBusPublisher(
        ServiceBusClient client,
        IOptions<AzureServiceBusOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task PublishAsync(ReportMessageDto message, CancellationToken cancellationToken = default)
    {
        ServiceBusSender sender = _client.CreateSender(_options.TopicName);

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

        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }
}
