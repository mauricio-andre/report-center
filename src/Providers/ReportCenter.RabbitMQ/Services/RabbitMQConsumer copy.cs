// using System.Text;
// using System.Text.Json;
// using MediatR;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using RabbitMQ.Client;
// using RabbitMQ.Client.Events;
// using ReportCenter.Common.Diagnostics;
// using ReportCenter.Common.Options;
// using ReportCenter.Common.Providers.MessageQueues.Dtos;
// using ReportCenter.Common.Providers.MessageQueues.Enums;
// using ReportCenter.Common.Providers.MessageQueues.Interfaces;
// using ReportCenter.Core.Templates.BackgroundServices;
// using ReportCenter.RabbitMQ.Dtos;
// using ReportCenter.RabbitMQ.Options;

// namespace ReportCenter.RabbitMQ.Services;

// public abstract class RabbitMQConsumerCopy : MessageConsumerTemplateCopy<BasicDeliverEventArgs>
// {
//     private readonly IConnectionFactory _connectionFactory;
//     private readonly ILogger<RabbitMQConsumerCopy> _logger;
//     private readonly RabbitMQOptions _rabbitMqOptions;
//     private IConnection? _connection;
//     private IChannel? _channel;

//     public RabbitMQConsumerCopy(
//         IConnectionFactory connectionFactory,
//         ILogger<RabbitMQConsumerCopy> logger,
//         IOptions<RabbitMQOptions> rabbitMqOptions,
//         ReportCenterActivitySource reportCenterActivitySource,
//         IMessagePublisher messagePublisher,
//         IOptions<ReportWorkerOptions> options,
//         IMediator mediator) : base(logger, reportCenterActivitySource, messagePublisher, options, mediator)
//     {
//         _connectionFactory = connectionFactory;
//         _logger = logger;
//         _rabbitMqOptions = rabbitMqOptions.Value;
//     }

//     public override async Task StartAsync(CancellationToken cancellationToken)
//     {
//         _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//         _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
//         await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);
//         await base.StartAsync(cancellationToken);
//     }

//     protected override async Task ExecuteAsync(CancellationToken cancellationToken)
//     {
//         var consumer = new AsyncEventingBasicConsumer(_channel!);
//         consumer.ReceivedAsync += async (sender, eventArgs) =>
//         {
//             await ReceivedAsync(eventArgs, cancellationToken);
//         };

//         await _channel!.BasicConsumeAsync(_rabbitMqOptions.QueueName, autoAck: false, consumer, cancellationToken);
//         _logger.LogInformation("Waiting for messages...");
//     }

//     protected override ReportMessageDto DeserializeMessage(BasicDeliverEventArgs args)
//     {
//         var headers = args.BasicProperties.Headers;
//         var body = args.Body.ToArray();

//         return new ReportMessageDto(
//             Id: JsonSerializer.Deserialize<MessageBodyDto>(Encoding.UTF8.GetString(body))!.Id,
//             Domain: Encoding.UTF8.GetString((byte[])headers![nameof(ReportMessageDto.Domain)]!),
//             Application: Encoding.UTF8.GetString((byte[])headers![nameof(ReportMessageDto.Application)]!),
//             ReportType: (ReportType)short.Parse(headers![nameof(ReportMessageDto.ReportType)]!.ToString()!),
//             DocumentName: Encoding.UTF8.GetString((byte[])headers![nameof(ReportMessageDto.DocumentName)]!),
//             DocumentKey: Encoding.UTF8.GetString((byte[])headers![nameof(ReportMessageDto.DocumentKey)]!),
//             Version: short.Parse(headers![nameof(ReportMessageDto.Version)]!.ToString()!)
//         );
//     }

//     protected override ValueTask AbortProcessingAsync(BasicDeliverEventArgs args, Exception ex, CancellationToken cancellationToken)
//     {
//         return _channel!.BasicNackAsync(args.DeliveryTag, false, false, cancellationToken);
//     }

//     protected override ValueTask CompleteMessage(BasicDeliverEventArgs args, CancellationToken cancellationToken)
//     {
//         return _channel!.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
//     }

//     protected override ValueTask<string?> GetParentTransactionId(BasicDeliverEventArgs args)
//     {
//         string? transactionId = null;
//         if (args.BasicProperties.Headers!.TryGetValue("TransactionId", out var headerTransactionId))
//             transactionId = Encoding.UTF8.GetString((byte[])headerTransactionId!);

//         return ValueTask.FromResult(transactionId);
//     }

//     public override async Task StopAsync(CancellationToken cancellationToken)
//     {
//         await _channel!.CloseAsync(cancellationToken: cancellationToken);
//         await _connection!.CloseAsync(cancellationToken: cancellationToken);
//         await base.StopAsync(cancellationToken);
//     }

//     public override void Dispose()
//     {
//         if (_channel != null && _channel.IsOpen)
//             _channel.CloseAsync().Wait();

//         if (_connection != null && _connection.IsOpen)
//             _connection.CloseAsync().Wait();

//         _channel?.Dispose();
//         _connection?.Dispose();

//         base.Dispose();
//     }
// }

