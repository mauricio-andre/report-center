using System.ComponentModel;
using ReportCenter.Common.Queries;

namespace ReportCenter.App.RestServer.Endpoints.V1.Reports.Dtos;

public record SearchReportFromOrigemRequestDto(
    [property:Description("Allows searching by partial value when using the % character at the beginning or end of the text")]
    string? DocumentKeyComposition,
    string? SortBy,
    int? Skip,
    int? Take = 50,
    bool IncludeExpiredFiles = false
) : IPageableQuery, ISortableQuery;
