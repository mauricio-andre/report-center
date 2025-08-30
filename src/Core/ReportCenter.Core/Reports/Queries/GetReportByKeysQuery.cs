using MediatR;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Core.Reports.Responses;

namespace ReportCenter.Core.Reports.Queries;

public record GetReportByKeysQuery(
    string Domain,
    string Application,
    short Version,
    string DocumentName,
    ReportType ReportType,
    string DocumentKey
) : IRequest<ReportCompleteResponse>;
