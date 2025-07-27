using MediatR;
using ReportCenter.Core.Reports.Responses;

namespace ReportCenter.Core.Reports.Commands;

public record CreateReportExportCommand(
    string Domain,
    string Application,
    string DocumentName,
    string DocumentKey,
    short Version,
    DateTimeOffset ExpirationDate,
    Dictionary<string, object>? Filters,
    Dictionary<string, object>? ExtraProperties
) : IRequest<ReportResponse>;
