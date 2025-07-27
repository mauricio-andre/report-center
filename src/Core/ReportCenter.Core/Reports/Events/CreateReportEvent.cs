using MediatR;
using ReportCenter.Common.Providers.MessageQueues.Enums;

namespace ReportCenter.Core.Reports.Events;

public record CreateReportEvent(
    string Domain,
    string Application,
    ReportType ReportType,
    string DocumentName,
    string DocumentKey
) : INotification;
