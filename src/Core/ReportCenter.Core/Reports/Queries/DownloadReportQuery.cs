using MediatR;

namespace ReportCenter.Core.Reports.Queries;

public record DownloadReportQuery(
    Guid Id
) : IRequest<Stream?>;
