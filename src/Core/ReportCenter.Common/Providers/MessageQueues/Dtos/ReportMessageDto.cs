using ReportCenter.Common.Providers.MessageQueues.Enums;

namespace ReportCenter.Common.Providers.MessageQueues.Dtos;

public record ReportMessageDto(
    Guid Id,
    string Domain,
    string Application,
    ReportType ReportType,
    string DocumentName,
    string DocumentKey,
    short Version
);
