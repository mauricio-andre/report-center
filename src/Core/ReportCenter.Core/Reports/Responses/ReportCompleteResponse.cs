using ReportCenter.Common.Providers.MessageQueues.Enums;

namespace ReportCenter.Core.Reports.Responses;

public record ReportCompleteResponse(
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
    Dictionary<string, object> Filters,
    Dictionary<string, object> ExtraProperties,
    string? FileExtension,
    TimeSpan? ProcessTimer,
    bool ExternalProcess,
    string? ProcessMessage
);
