// using System.Diagnostics;
// using MediatR;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using ReportCenter.Common.Diagnostics;
// using ReportCenter.Common.Loggers;
// using ReportCenter.Common.Options;
// using ReportCenter.Common.Providers.MessageQueues.Dtos;
// using ReportCenter.Common.Providers.MessageQueues.Enums;
// using ReportCenter.Common.Providers.MessageQueues.Interfaces;
// using ReportCenter.Core.Reports.Commands;
// using ReportCenter.Core.Reports.Queries;
// using ReportCenter.Core.Reports.Responses;

// namespace ReportCenter.Core.Templates.BackgroundServices;

// public abstract class MessageConsumerTemplateCopy<T> : BackgroundService
// {
//     private readonly ILogger<MessageConsumerTemplateCopy<T>> _logger;
//     private readonly ReportCenterActivitySource _reportCenterActivitySource;
//     private readonly IMessagePublisher _messagePublisher;
//     private readonly ReportWorkerOptions _reportWorkerOptions;
//     private readonly IMediator _mediator;
//     protected virtual string ExportFileExtension => "xlsx";

//     public MessageConsumerTemplateCopy(
//         ILogger<MessageConsumerTemplateCopy<T>> logger,
//         ReportCenterActivitySource reportCenterActivitySource,
//         IMessagePublisher messagePublisher,
//         IOptions<ReportWorkerOptions> options,
//         IMediator mediator)
//     {
//         _logger = logger;
//         _reportCenterActivitySource = reportCenterActivitySource;
//         _messagePublisher = messagePublisher;
//         _reportWorkerOptions = options.Value;
//         _mediator = mediator;
//     }

//     protected async Task ReceivedAsync(T args, CancellationToken cancellationToken)
//     {
//         ReportMessageDto message;
//         string? transactionId;

//         try
//         {
//             message = DeserializeMessage(args);
//         }
//         catch (Exception ex)
//         {
//             await AbortProcessingAsync(args, ex, cancellationToken);
//             _logger.LogError(ex, "An error occurred while deserializing the message, incompatible message body or header.");
//             return;
//         }

//         try
//         {
//             await CompleteMessage(args, cancellationToken);

//             if (IsMessageWithinProcessingScope(message))
//             {
//                 _logger.LogInformation("Message does not match Report Worker options");
//                 return;
//             }

//             transactionId = await GetParentTransactionId(args);

//             using (_logger.BeginScope(new ReportDocumentKeysLoggerRecord(
//                 message.ReportType.ToString(),
//                 message.DocumentName,
//                 message.DocumentKey,
//                 message.Version)))
//             {
//                 var activity = string.IsNullOrEmpty(transactionId)
//                     ? _reportCenterActivitySource.ActivitySourceDefault.StartActivity(
//                         "StartMessageProcess",
//                         ActivityKind.Consumer)
//                     : _reportCenterActivitySource.ActivitySourceDefault.StartActivity(
//                         "StartMessageProcess",
//                         ActivityKind.Consumer,
//                         ActivityContext.Parse(transactionId!, null));

//                 activity?.AddTag(nameof(ReportMessageDto.Domain), message.Domain);
//                 activity?.AddTag(nameof(ReportMessageDto.Application), message.Application);
//                 activity?.AddTag(nameof(ReportMessageDto.ReportType), message.ReportType.ToString());
//                 activity?.AddTag(nameof(ReportMessageDto.DocumentName), message.DocumentName);
//                 activity?.AddTag(nameof(ReportMessageDto.DocumentKey), message.DocumentKey);
//                 activity?.AddTag(nameof(ReportMessageDto.Version), message.Version);

//                 using (activity)
//                 {
//                     await _mediator.Send(
//                         new UpdateStateReportCommand(message.Id, ProcessState.Processing),
//                         cancellationToken);

//                     var report = await _mediator.Send(new GetReportByIdQuery(message.Id));

//                     var stopwatch = new Stopwatch();
//                     stopwatch.Start();

//                     await HandleMessageAsync(report, cancellationToken);

//                     stopwatch.Stop();
//                     await _mediator.Send(
//                         new UpdateStateReportCommand(
//                             message.Id,
//                             ProcessState.Success,
//                             stopwatch.Elapsed,
//                             report.ReportType == ReportType.Import
//                                 ? report.FileName
//                                 : CreateExportFileName(report)),
//                         cancellationToken);
//                 }
//             }
//         }
//         catch (OperationCanceledException)
//         {
//             _logger.LogInformation("Processing stopped, rescheduling message");
//             await _messagePublisher.PublishAsync(message);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogCritical(ex, "An unhandled error occurred while processing the message");
//             await _mediator.Send(
//                 new UpdateStateReportCommand(message.Id, ProcessState.Error),
//                 cancellationToken);
//         }
//     }

//     private bool IsMessageWithinProcessingScope(ReportMessageDto message)
//         => _reportWorkerOptions.Domain.Equals(message.Domain, StringComparison.InvariantCultureIgnoreCase)
//             && _reportWorkerOptions.Application.Equals(message.Application, StringComparison.InvariantCultureIgnoreCase)
//             && (!_reportWorkerOptions.ReportType.HasValue
//                 || _reportWorkerOptions.ReportType == message.ReportType)
//             && (!string.IsNullOrEmpty(_reportWorkerOptions.DocumentName)
//                 || _reportWorkerOptions.DocumentName!.Equals(message.DocumentName, StringComparison.InvariantCultureIgnoreCase));

//     protected string CreateExportFileName(ReportCompleteResponse report)
//         => Path.Combine(report.Domain, report.Application, report.DocumentName, string.Concat(report.Id, ".", ExportFileExtension));

//     protected abstract ReportMessageDto DeserializeMessage(T args);

//     protected abstract ValueTask AbortProcessingAsync(T args, Exception ex, CancellationToken cancellationToken);

//     protected abstract ValueTask CompleteMessage(T args, CancellationToken cancellationToken);

//     protected abstract ValueTask<string?> GetParentTransactionId(T args);

//     protected abstract Task HandleMessageAsync(ReportCompleteResponse report, CancellationToken cancellationToken);
// }
