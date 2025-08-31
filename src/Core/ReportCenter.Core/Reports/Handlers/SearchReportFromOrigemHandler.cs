using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ReportCenter.Common.Extensions;
using ReportCenter.Common.Responses;
using ReportCenter.Core.Data;
using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Queries;
using ReportCenter.Core.Reports.Responses;

namespace ReportCenter.Core.Reports.Handlers;

public class SearchReportFromOrigemHandler : IRequestHandler<SearchReportFromOrigemQuery, CollectionResponse<ReportResponse>>
{
    private readonly CoreDbContext _coreDbContext;
    private readonly IValidator<SearchReportFromOrigemQuery> _validator;

    public SearchReportFromOrigemHandler(
        IDbContextFactory<CoreDbContext> dbContextFactory,
        IValidator<SearchReportFromOrigemQuery> validator)
    {
        _coreDbContext = dbContextFactory.CreateDbContext();
        _validator = validator;
    }

    public async Task<CollectionResponse<ReportResponse>> Handle(
        SearchReportFromOrigemQuery request,
        CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var query = CreateSearchQuery(request).AsNoTracking();
        var totalCount = await query.CountAsync();

        query = query
            .ApplySorting(request, query => query.OrderByDescending(report => report.CreationDate))
            .ApplyPagination(request);

        var items = MapToResponse(query).AsAsyncEnumerable();
        return new CollectionResponse<ReportResponse>(items, totalCount);
    }

    private IQueryable<Report> CreateSearchQuery(SearchReportFromOrigemQuery request)
    {
        return _coreDbContext.Reports
            .WhereIf(
                !string.IsNullOrEmpty(request.Domain),
                entity => entity.Domain.ToUpper() == request.Domain!.ToUpper())
            .WhereIf(
                !string.IsNullOrEmpty(request.Application),
                entity => entity.Application.ToUpper() == request.Application!.ToUpper())
            .WhereIf(
                !string.IsNullOrEmpty(request.DocumentName),
                entity => entity.DocumentName.ToUpper() == request.DocumentName!.ToUpper())
            .WhereIf(
                request.ReportType.HasValue,
                entity => entity.ReportType == request.ReportType)
            .WhereIf(
                request.version.HasValue,
                entity => entity.Version == request.version)
            .WhereIf(
                request.IncludeExpiredFiles,
                entity => entity.ExpirationDate >= DateTimeOffset.Now)
            .WhereIf(
                !string.IsNullOrEmpty(request.DocumentKeyComposition)
                    && request.DocumentKeyComposition.StartsWith('%'),
                entity => entity.DocumentKey.ToUpper().StartsWith(request.DocumentKeyComposition!.ToUpper().Replace("%", "")))
            .WhereIf(
                !string.IsNullOrEmpty(request.DocumentKeyComposition)
                    && request.DocumentKeyComposition.EndsWith('%'),
                entity => entity.DocumentKey.ToUpper().EndsWith(request.DocumentKeyComposition!.ToUpper().Replace("%", "")))
            .WhereIf(
                !string.IsNullOrEmpty(request.DocumentKeyComposition)
                    && !request.DocumentKeyComposition.Contains('%'),
                entity => entity.DocumentKey.ToUpper() == request.DocumentKeyComposition!.ToUpper());
    }

    private static IQueryable<ReportResponse> MapToResponse(IQueryable<Report> query)
        => query.Select(entity => new ReportResponse(
            entity.Id,
            entity.Domain,
            entity.Application,
            entity.ReportType,
            entity.DocumentName,
            entity.DocumentKey,
            entity.Version,
            entity.UserIdentifier,
            entity.CreationDate,
            entity.ExpirationDate,
            entity.ProcessState,
            entity.ProcessTimer,
            entity.ExternalProcess,
            entity.ProcessMessage
        ));
}
