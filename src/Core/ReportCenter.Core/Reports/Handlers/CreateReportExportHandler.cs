using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;
using ReportCenter.Common.Localization;
using ReportCenter.Common.Providers.MessageQueues.Dtos;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;
using ReportCenter.Core.Identity.Interfaces;
using ReportCenter.Core.Reports.Commands;
using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Events;
using ReportCenter.Core.Reports.Interfaces;
using ReportCenter.Core.Reports.ObjectValues;
using ReportCenter.Core.Reports.Responses;

namespace ReportCenter.Core.Reports.Handlers;

public class CreateReportExportHandler : IRequestHandler<CreateReportExportCommand, ReportCompleteResponse>
{
    private readonly IReportRepository _exportRepository;
    private readonly IMediator _mediator;
    private readonly IValidator<CreateReportExportCommand> _validator;
    private readonly ICurrentIdentity _currentIdentity;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IStringLocalizer<ReportCenterResource> _stringLocalizer;

    public CreateReportExportHandler(
        IReportRepository exportRepository,
        IMediator mediator,
        IValidator<CreateReportExportCommand> validator,
        ICurrentIdentity currentIdentity,
        IMessagePublisher messagePublisher,
        IStringLocalizer<ReportCenterResource> stringLocalizer)
    {
        _exportRepository = exportRepository;
        _mediator = mediator;
        _validator = validator;
        _currentIdentity = currentIdentity;
        _messagePublisher = messagePublisher;
        _stringLocalizer = stringLocalizer;
    }

    public async Task<ReportCompleteResponse> Handle(CreateReportExportCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        await _mediator.Publish(new CreateReportEvent(
            request.Domain,
            request.Application,
            ReportType.Export,
            request.DocumentName,
            request.DocumentKey
        ), cancellationToken);

        var entity = MapToEntity(request);

        await _exportRepository.InsertAsync(entity);
        await _messagePublisher.PublishProcessesAsync(new ReportMessageDto(
            Id: entity.Id,
            Domain: entity.Domain,
            Application: entity.Application,
            ReportType: entity.ReportType,
            DocumentName: entity.DocumentName,
            DocumentKey: entity.DocumentKey,
            Version: entity.Version
        ));

        return MapToResponse(entity);
    }

    private Report MapToEntity(CreateReportExportCommand request) => new Report()
    {
        Id = Guid.NewGuid(),
        Domain = request.Domain.ToUpper(),
        Application = request.Application.ToUpper(),
        ReportType = ReportType.Export,
        DocumentName = request.DocumentName.ToUpper(),
        DocumentKey = request.DocumentKey,
        Version = request.Version,
        UserIdentifier = _currentIdentity.GetNameIdentifier()!,
        CreationDate = DateTimeOffset.Now,
        ExpirationDate = request.ExpirationDate,
        Filters = new FlexibleObject(request.Filters ?? new()),
        ExtraProperties = new FlexibleObject(request.ExtraProperties ?? new()),
        ExternalProcess = request.ExternalProcess
    };

    private ReportCompleteResponse MapToResponse(Report entity)
    {
        return new ReportCompleteResponse(
            Id: entity.Id,
            Domain: entity.Domain,
            Application: entity.Application,
            ReportType: entity.ReportType,
            DocumentName: entity.DocumentName,
            DocumentKey: entity.DocumentKey,
            Version: entity.Version,
            UserIdentifier: entity.UserIdentifier,
            CreationDate: entity.CreationDate,
            ExpirationDate: entity.ExpirationDate,
            ProcessState: entity.ProcessState,
            Filters: entity.Filters.Data,
            ExtraProperties: entity.ExtraProperties.Data,
            FileExtension: entity.FileExtension,
            ProcessTimer: entity.ProcessTimer,
            ExternalProcess: entity.ExternalProcess,
            ProcessMessage: string.IsNullOrEmpty(entity.ProcessMessage)
                ? entity.ProcessMessage
                : _stringLocalizer[entity.ProcessMessage]
        );
    }
}
