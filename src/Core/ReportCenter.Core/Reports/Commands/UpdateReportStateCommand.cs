using MediatR;
using ReportCenter.Common.Providers.MessageQueues.Enums;

namespace ReportCenter.Core.Reports.Commands;

public record UpdateReportStateCommand(
    Guid Id,
    ProcessState ProcessState,
    TimeSpan? ProcessTimer = null,
    string? ProcessMessage = null
) : IRequest;
