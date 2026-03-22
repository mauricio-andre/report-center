using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ReportCenter.Common.Extensions;
using ReportCenter.Common.Responses;
using ReportCenter.Core.Data;
using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Interfaces;
using ReportCenter.Core.Reports.Queries;
using ReportCenter.Core.Reports.Responses;

namespace ReportCenter.Core.Reports.Handlers;

public class SearchReportHandler : IRequestHandler<SearchReportQuery, CollectionResponse<ReportResponse>>
{
    private readonly IReportRepository _reportRepository;
    private readonly IValidator<SearchReportQuery> _validator;

    public SearchReportHandler(
        IReportRepository reportRepository,
        IValidator<SearchReportQuery> validator)
    {
        _reportRepository = reportRepository;
        _validator = validator;
    }

    public async Task<CollectionResponse<ReportResponse>> Handle(
        SearchReportQuery request,
        CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var query = CreateSearchQuery(request).AsNoTracking();

#pragma warning disable S6966
        var totalCount = query.Count();
#pragma warning restore S6966

        query = query
            .ApplySorting(request, query => query.OrderByDescending(report => report.CreationDate))
            .ApplyPagination(request);

        var items = MapToResponse(query);
        return new CollectionResponse<ReportResponse>(items, totalCount);
    }

    private IQueryable<Report> CreateSearchQuery(SearchReportQuery request)
    {
        return _reportRepository.AsQueryable()
            .WhereIf(
                !string.IsNullOrEmpty(request.Domain),
                entity => entity.Domain.ToUpper() == request.Domain!.ToUpper())
            .WhereIf(
                !string.IsNullOrEmpty(request.Application),
                entity => entity.Application.ToUpper() == request.Application!.ToUpper())
            .WhereIf(
                request.version.HasValue,
                entity => entity.Version == request.version)
            .WhereIf(
                !string.IsNullOrEmpty(request.DocumentName),
                entity => entity.DocumentName.ToUpper() == request.DocumentName!.ToUpper())
            .WhereIf(
                request.ReportType.HasValue,
                entity => entity.ReportType == request.ReportType)
            .WhereIf(
                !request.IncludeExpiredFiles,
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

    private static async IAsyncEnumerable<ReportResponse> MapToResponse(IQueryable<Report> query)
    {
#pragma warning disable S6966
        foreach (var entity in query.ToList())
        {
            yield return new ReportResponse(
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
                entity.Filters.Data,
                entity.ExtraProperties.Data,
                entity.FileExtension,
                entity.ProcessTimer,
                entity.ExternalProcess,
                entity.ProcessMessage
            );
        }
#pragma warning restore S6966
    }
}
