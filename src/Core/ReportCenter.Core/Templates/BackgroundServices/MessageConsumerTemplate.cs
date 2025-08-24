using System.Diagnostics;
using Grpc.Core;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportCenter.Common.Diagnostics;
using ReportCenter.Common.Loggers;
using ReportCenter.Common.Options;
using ReportCenter.Common.Providers.MessageQueues.Dtos;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;
using ReportCenter.Core.Reports.Commands;
using ReportCenter.Core.Reports.Interfaces;

namespace ReportCenter.Core.Templates.BackgroundServices;

public class MessageConsumerTemplate : BackgroundService
{
    private readonly ILogger<MessageConsumerTemplate> _logger;
    private readonly ReportCenterActivitySource _reportCenterActivitySource;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IMessageConsumer _messageConsumer;
    private readonly ReportWorkerOptions _reportWorkerOptions;
    private readonly IServiceProvider _serviceProvider;
    protected IServiceScope? _serviceScope;
    private IDisposable? _globalLoggerScope;
    private IMediator? _mediator;
    private IReportRepository? _reportRepository;
    private IReportServiceFactory? _reportServiceFactory;

    public MessageConsumerTemplate(
        ILogger<MessageConsumerTemplate> logger,
        ReportCenterActivitySource reportCenterActivitySource,
        IMessagePublisher messagePublisher,
        IMessageConsumer messageConsumer,
        IOptions<ReportWorkerOptions> options,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _reportCenterActivitySource = reportCenterActivitySource;
        _messagePublisher = messagePublisher;
        _messageConsumer = messageConsumer;
        _reportWorkerOptions = options.Value;
        _serviceProvider = serviceProvider;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _serviceScope = _serviceProvider.CreateScope();
        _mediator = _serviceScope.ServiceProvider.GetRequiredService<IMediator>();
        _reportRepository = _serviceScope.ServiceProvider.GetRequiredService<IReportRepository>();
        _reportServiceFactory = _serviceScope.ServiceProvider.GetRequiredService<IReportServiceFactory>();

        _globalLoggerScope = _logger.BeginScope(new ReportBaseKeysLoggerRecord(
            _reportWorkerOptions.Domain,
            _reportWorkerOptions.Application));

        await _messageConsumer.StartAsync();
        await base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _messageConsumer.RegistryConsumer(ReceivedAsync, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _messageConsumer.StopAsync();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _messageConsumer.Dispose();
        _globalLoggerScope?.Dispose();
        _serviceScope?.Dispose();
        base.Dispose();
    }

    protected async Task ReceivedAsync(dynamic args, CancellationToken cancellationToken)
    {
        ReportMessageDto message;
        string? transactionId;

        try
        {
            message = _messageConsumer.DeserializeMessage(args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deserializing the message, incompatible message body or header.");
            await _messageConsumer.AbortProcessingAsync(args, ex, cancellationToken);
            return;
        }

        try
        {
            await _messageConsumer.CompleteMessage(args, cancellationToken);

            if (!IsMessageWithinProcessingScope(message))
            {
                _logger.LogInformation("Message does not match Report Worker options.");
                return;
            }

            transactionId = await _messageConsumer.GetParentTransactionId(args);

            using (_logger.BeginScope(new ReportDocumentKeysLoggerRecord(
                message.ReportType.ToString(),
                message.DocumentName,
                message.DocumentKey,
                message.Version)))
            {
                var activity = string.IsNullOrEmpty(transactionId)
                    ? _reportCenterActivitySource.ActivitySourceDefault.StartActivity(
                        "StartMessageProcess",
                        ActivityKind.Consumer)
                    : _reportCenterActivitySource.ActivitySourceDefault.StartActivity(
                        "StartMessageProcess",
                        ActivityKind.Consumer,
                        ActivityContext.Parse(transactionId!, null));

                activity?.AddTag(nameof(ReportMessageDto.Domain), message.Domain);
                activity?.AddTag(nameof(ReportMessageDto.Application), message.Application);
                activity?.AddTag(nameof(ReportMessageDto.ReportType), message.ReportType.ToString());
                activity?.AddTag(nameof(ReportMessageDto.DocumentName), message.DocumentName);
                activity?.AddTag(nameof(ReportMessageDto.DocumentKey), message.DocumentKey);
                activity?.AddTag(nameof(ReportMessageDto.Version), message.Version);

                using (activity)
                {
                    await _mediator!.Send(
                        new UpdateReportStateCommand(message.Id, ProcessState.Processing),
                        cancellationToken);

                    var report = await _reportRepository!.GetByIdAsync(message.Id);

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    var service = _reportServiceFactory!.CreateInstance(report!, _serviceScope!);

                    await service.HandleAsync(report!, cancellationToken);

                    stopwatch.Stop();
                    await _mediator.Send(
                        new UpdateReportStateCommand(
                            message.Id,
                            ProcessState.Success,
                            stopwatch.Elapsed),
                        cancellationToken);
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "Processing stopped, rescheduling message.");
            await _messagePublisher.PublishAsync(message);
            await _mediator!.Send(
                new UpdateReportStateCommand(message.Id, ProcessState.Waiting, ProcessMessage: "message:report:cancellationtokenTriggered"));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "An error occurred while connecting to the gRPC server.");

            if (ex.Status.StatusCode == StatusCode.Unavailable
                || ex.Status.StatusCode == StatusCode.DeadlineExceeded)
            {
                _logger.LogInformation(ex, "Putting message back into the processing queue");

                await _messagePublisher.PublishAsync(message, cancellationToken);
                await _mediator!.Send(
                    new UpdateReportStateCommand(message.Id, ProcessState.Waiting, ProcessMessage: "message:report:connectionGrpcServerFail"),
                    cancellationToken);

                return;
            }

            await _mediator!.Send(
                new UpdateReportStateCommand(message.Id, ProcessState.Error, ProcessMessage: "message:report:connectionGrpcServerCriticalFail"),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An unhandled error occurred while processing the message.");
            await _mediator!.Send(
                new UpdateReportStateCommand(message.Id, ProcessState.Error, ProcessMessage: ex.Message),
                cancellationToken);
        }
    }

    private bool IsMessageWithinProcessingScope(ReportMessageDto message)
        => _reportWorkerOptions.Domain.Equals(message.Domain, StringComparison.InvariantCultureIgnoreCase)
            && _reportWorkerOptions.Application.Equals(message.Application, StringComparison.InvariantCultureIgnoreCase)
            && (!_reportWorkerOptions.ReportType.HasValue
                || _reportWorkerOptions.ReportType == 0
                || _reportWorkerOptions.ReportType == message.ReportType)
            && (string.IsNullOrEmpty(_reportWorkerOptions.DocumentName)
                || _reportWorkerOptions.DocumentName!.Equals(message.DocumentName, StringComparison.InvariantCultureIgnoreCase));
}
