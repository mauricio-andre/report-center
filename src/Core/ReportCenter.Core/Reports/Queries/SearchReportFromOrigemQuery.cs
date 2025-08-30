using MediatR;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Common.Queries;
using ReportCenter.Common.Responses;
using ReportCenter.Core.Reports.Responses;

namespace ReportCenter.Core.Reports.Queries;

public record SearchReportFromOrigemQuery(
    string? Domain,
    string? Application,
    short? version,
    string? DocumentName,
    ReportType? ReportType,
    string? DocumentKeyComposition,
    bool IncludeExpiredFiles,
    string? SortBy,
    int? Skip,
    int? Take = 50
) : IRequest<CollectionResponse<ReportResponse>>, IPageableQuery, ISortableQuery;
