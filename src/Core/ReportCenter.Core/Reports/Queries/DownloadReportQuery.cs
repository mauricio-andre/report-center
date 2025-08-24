using MediatR;
using ReportCenter.Core.Reports.Responses;

namespace ReportCenter.Core.Reports.Queries;

public record DownloadReportQuery(
    Guid Id
) : IRequest<DownloadReportResponse?>;
