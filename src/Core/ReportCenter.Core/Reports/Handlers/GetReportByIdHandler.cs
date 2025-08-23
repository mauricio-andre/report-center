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

public class GetReportByIdHandler : IRequestHandler<GetReportByIdQuery, ReportCompleteResponse>
{
    private readonly IReportRepository _exportRepository;
    private readonly IStringLocalizer<ReportCenterResource> _stringLocalizer;
    private readonly IValidator<GetReportByIdQuery> _validator;

    public GetReportByIdHandler(
        IReportRepository exportRepository,
        IStringLocalizer<ReportCenterResource> stringLocalizer,
        IValidator<GetReportByIdQuery> validator)
    {
        _exportRepository = exportRepository;
        _stringLocalizer = stringLocalizer;
        _validator = validator;
    }

    public async Task<ReportCompleteResponse> Handle(
        GetReportByIdQuery request,
        CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var entity = await _exportRepository.GetByIdAsync(request.Id);

        if (entity == null)
            throw new EntityNotFoundException(_stringLocalizer, nameof(Report), request.Id.ToString());

        return MapToResponse(entity);
    }

    private static ReportCompleteResponse MapToResponse(Report entity) =>
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
            entity.ProcessMessage
        );
}
