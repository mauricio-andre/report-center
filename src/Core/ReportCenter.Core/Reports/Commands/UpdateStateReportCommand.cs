using MediatR;
using ReportCenter.Common.Providers.MessageQueues.Enums;

namespace ReportCenter.Core.Reports.Commands;

public record UpdateStateReportCommand(
    Guid Id,
    ProcessState ProcessState,
    TimeSpan? ProcessTimer = null
) : IRequest;
