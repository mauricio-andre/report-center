using MediatR;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Common.Queries;
using ReportCenter.Common.Responses;
using ReportCenter.Core.Reports.Responses;

namespace ReportCenter.Core.Reports.Queries;

public record SearchReportFromOrigemQuery(
    string Domain,
    string Application,
    ReportType ReportType,
    string DocumentName,
    string? DocumentKeyComposition,
    string? SortBy,
    int? Skip,
    int? Take = 50,
    bool IncludeExpiredFiles = false
) : IRequest<CollectionResponse<ReportResponse>>, IPageableQuery, ISortableQuery;
