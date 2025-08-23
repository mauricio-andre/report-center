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
            .Where(entity => entity.Domain.ToLower() == request.Domain.ToLower())
            .Where(entity => entity.Application.ToLower() == request.Application.ToLower())
            .Where(entity => entity.DocumentName.ToLower() == request.DocumentName.ToLower())
            .Where(entity => entity.ReportType == request.ReportType)
            .Where(entity => entity.Version == request.version)
            .WhereIf(
                !request.IncludeExpiredFiles,
                entity => entity.ExpirationDate >= DateTimeOffset.Now)
            .WhereIf(
                !string.IsNullOrEmpty(request.DocumentKeyComposition)
                    && request.DocumentKeyComposition.StartsWith('%'),
                entity => entity.DocumentKey.ToLower().StartsWith(request.DocumentKeyComposition!.ToLower().Replace("%", "")))
            .WhereIf(
                !string.IsNullOrEmpty(request.DocumentKeyComposition)
                    && request.DocumentKeyComposition.EndsWith('%'),
                entity => entity.DocumentKey.ToLower().EndsWith(request.DocumentKeyComposition!.ToLower().Replace("%", "")));
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
