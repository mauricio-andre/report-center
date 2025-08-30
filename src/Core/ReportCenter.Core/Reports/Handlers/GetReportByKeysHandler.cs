using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;
using ReportCenter.Common.Exceptions;
using ReportCenter.Common.Localization;
using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Interfaces;
using ReportCenter.Core.Reports.Queries;
using ReportCenter.Core.Reports.Responses;

namespace ReportCenter.Core.Reports.Handlers;

public class GetReportByKeysHandler : IRequestHandler<GetReportByKeysQuery, ReportCompleteResponse>
{
    private readonly IReportRepository _exportRepository;
    private readonly IStringLocalizer<ReportCenterResource> _stringLocalizer;
    private readonly IValidator<GetReportByKeysQuery> _validator;

    public GetReportByKeysHandler(
        IReportRepository exportRepository,
        IStringLocalizer<ReportCenterResource> stringLocalizer,
        IValidator<GetReportByKeysQuery> validator)
    {
        _exportRepository = exportRepository;
        _stringLocalizer = stringLocalizer;
        _validator = validator;
    }

    public async Task<ReportCompleteResponse> Handle(
        GetReportByKeysQuery request,
        CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var entity = await _exportRepository.GetByKeysAsync(
            request.Domain,
            request.Application,
            request.Version,
            request.DocumentName,
            request.ReportType,
            request.DocumentKey);

        if (entity == null)
            throw new EntityNotFoundException(
                _stringLocalizer,
                nameof(Report),
                string.Concat(
                    "[",
                    request.Application,
                    "], [",
                    request.Domain,
                    "], [",
                    request.Version,
                    "], [",
                    request.DocumentName,
                    "], [",
                    request.ReportType.ToString(),
                    "], [",
                    request.DocumentKey,
                    "]"));

        return MapToResponse(entity);
    }

    private ReportCompleteResponse MapToResponse(Report entity) =>
        new ReportCompleteResponse(
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
            string.IsNullOrEmpty(entity.ProcessMessage)
                ? entity.ProcessMessage
                : _stringLocalizer[entity.ProcessMessage]
        );
}
