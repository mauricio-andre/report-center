using MediatR;
using ReportCenter.Core.Reports.Responses;

namespace ReportCenter.Core.Reports.Commands;

public record CreateReportImportCommand(
    Stream Stream,
    string FileExtension,
    string Domain,
    string Application,
    string DocumentName,
    string DocumentKey,
    short Version,
    DateTimeOffset ExpirationDate,
    Dictionary<string, object>? Filters,
    Dictionary<string, object>? ExtraProperties,
    bool ExternalProcess = false
) : IRequest<ReportCompleteResponse>;
