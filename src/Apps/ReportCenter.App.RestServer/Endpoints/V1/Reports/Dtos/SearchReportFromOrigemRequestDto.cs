using System.ComponentModel;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Common.Queries;

namespace ReportCenter.App.RestServer.Endpoints.V1.Reports.Dtos;

public record SearchReportFromOrigemRequestDto(
    string? Domain,
    string? Application,
    string? DocumentName,
    short? Version,
    [property:Description("Allows searching by partial value when using the % character at the beginning or end of the text")]
    string? DocumentKeyComposition,
    ReportType? ReportType,
    string? SortBy,
    int? Skip,
    int? Take = 50,
    bool IncludeExpiredFiles = false
) : IPageableQuery, ISortableQuery;
