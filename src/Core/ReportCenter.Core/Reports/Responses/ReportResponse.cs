using ReportCenter.Common.Providers.MessageQueues.Enums;

namespace ReportCenter.Core.Reports.Responses;

public record ReportResponse(
    Guid Id,
    string Domain,
    string Application,
    ReportType ReportType,
    string DocumentName,
    string DocumentKey,
    short Version,
    string UserIdentifier,
    DateTimeOffset CreationDate,
    DateTimeOffset? ExpirationDate,
    ProcessState ProcessState,
    TimeSpan? ProcessTimer,
    bool ExternalProcess,
    string? ProcessMessage
);
