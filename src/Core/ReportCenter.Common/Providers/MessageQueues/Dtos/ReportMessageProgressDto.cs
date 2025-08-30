using ReportCenter.Common.Providers.MessageQueues.Enums;

namespace ReportCenter.Common.Providers.MessageQueues.Dtos;

public record ReportMessageProgressDto(
    Guid Id,
    string Domain,
    string Application,
    short Version,
    string DocumentName,
    ReportType ReportType,
    string DocumentKey,
    ProcessState ProcessState,
    TimeSpan? ProcessTimer,
    string? ProcessMessage,
    bool Requeue
);
