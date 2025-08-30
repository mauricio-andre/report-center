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
        string? transactionId = null;

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
            var activity = !string.IsNullOrEmpty(transactionId) && ActivityContext.TryParse(transactionId, null, out var activityContext)
                ? _reportCenterActivitySource.ActivitySourceDefault.StartActivity(
                    "StartMessageProcess",
                    ActivityKind.Consumer,
                    activityContext)
                : _reportCenterActivitySource.ActivitySourceDefault.StartActivity(
                    "StartMessageProcess",
                    ActivityKind.Consumer);

            activity?.AddTag(nameof(ReportMessageDto.Domain), message.Domain);
            activity?.AddTag(nameof(ReportMessageDto.Application), message.Application);
            activity?.AddTag(nameof(ReportMessageDto.ReportType), message.ReportType.ToString());
            activity?.AddTag(nameof(ReportMessageDto.DocumentName), message.DocumentName);
            activity?.AddTag(nameof(ReportMessageDto.DocumentKey), message.DocumentKey);
            activity?.AddTag(nameof(ReportMessageDto.Version), message.Version);

            using (activity)
            {
                try
                {
                    await StateProcessingAsync(message, cancellationToken);

                    var report = await _reportRepository!.GetByIdAsync(message.Id);

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    var service = _reportServiceFactory!.CreateInstance(report!, _serviceScope!);

                    await service.HandleAsync(report!, cancellationToken);

                    stopwatch.Stop();

                    await StateSuccessAsync(message, stopwatch, cancellationToken);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogInformation(ex, "Processing stopped, rescheduling message.");
                    await StateErrorAsync(message, "message:report:cancellationtokenTriggered", true);
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "An error occurred while connecting to the gRPC server.");

                    if (ex.Status.StatusCode == StatusCode.Unavailable
                        || ex.Status.StatusCode == StatusCode.DeadlineExceeded)
                    {
                        _logger.LogInformation(ex, "Putting message back into the processing queue");
                        await StateErrorAsync(message, "message:report:connectionGrpcServerFail", true, cancellationToken);
                        return;
                    }

                    await StateErrorAsync(message, "message:report:connectionGrpcServerCriticalFail", false, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "An unhandled error occurred while processing the message.");
                    await StateErrorAsync(message, ex.Message, false, cancellationToken);
                }
            }
        }
    }

    private async Task StateProcessingAsync(ReportMessageDto message, CancellationToken cancellationToken)
    {
        await _mediator!.Send(
            new UpdateReportStateCommand(message.Id, ProcessState.Processing),
            cancellationToken);

        await _messagePublisher.PublishProgressAsync(
            new ReportMessageProgressDto(
                message.Id,
                message.Domain,
                message.Application,
                message.Version,
                message.DocumentName,
                message.ReportType,
                message.DocumentKey,
                ProcessState.Processing,
                null,
                null,
                false
            ),
            cancellationToken: cancellationToken
        );
    }

    private async Task StateSuccessAsync(ReportMessageDto message, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        await _mediator!.Send(
            new UpdateReportStateCommand(
                message.Id,
                ProcessState.Success,
                stopwatch.Elapsed),
            cancellationToken);

        await _messagePublisher.PublishProgressAsync(
            new ReportMessageProgressDto(
                message.Id,
                message.Domain,
                message.Application,
                message.Version,
                message.DocumentName,
                message.ReportType,
                message.DocumentKey,
                ProcessState.Success,
                stopwatch.Elapsed,
                null,
                false
            ),
            cancellationToken: cancellationToken
        );
    }

    private async Task StateErrorAsync(
        ReportMessageDto message,
        string messageException,
        bool requeue,
        CancellationToken cancellationToken = default)
    {
        if (requeue)
            await _messagePublisher.PublishProcessesAsync(message, cancellationToken);

        await _mediator!.Send(
            new UpdateReportStateCommand(
                message.Id,
                ProcessState.Waiting,
                ProcessMessage: messageException),
            cancellationToken);

        await _messagePublisher.PublishProgressAsync(
            new ReportMessageProgressDto(
                message.Id,
                message.Domain,
                message.Application,
                message.Version,
                message.DocumentName,
                message.ReportType,
                message.DocumentKey,
                ProcessState.Error,
                null,
                messageException,
                requeue
            ),
            cancellationToken
        );
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
