using FluentValidation;
using MediatR;
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

public class CreateReportExportHandler : IRequestHandler<CreateReportExportCommand, ReportResponse>
{
    private readonly IReportRepository _exportRepository;
    private readonly IMediator _mediator;
    private readonly IValidator<CreateReportExportCommand> _validator;
    private readonly ICurrentIdentity _currentIdentity;
    private readonly IMessagePublisher _messagePublisher;

    public CreateReportExportHandler(
        IReportRepository exportRepository,
        IMediator mediator,
        IValidator<CreateReportExportCommand> validator,
        ICurrentIdentity currentIdentity,
        IMessagePublisher messagePublisher)
    {
        _exportRepository = exportRepository;
        _mediator = mediator;
        _validator = validator;
        _currentIdentity = currentIdentity;
        _messagePublisher = messagePublisher;
    }

    public async Task<ReportResponse> Handle(CreateReportExportCommand request, CancellationToken cancellationToken)
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
        await _messagePublisher.PublishAsync(new ReportMessageDto(
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
        Domain = request.Domain,
        Application = request.Application,
        ReportType = ReportType.Export,
        DocumentName = request.DocumentName,
        DocumentKey = request.DocumentKey,
        Version = request.Version,
        UserIdentifier = _currentIdentity.GetNameIdentifier()!,
        CreationDate = DateTimeOffset.Now,
        ExpirationDate = request.ExpirationDate,
        Filters = new FlexibleObject(request.Filters ?? new()),
        ExtraProperties = new FlexibleObject(request.ExtraProperties ?? new()),
        ExternalProcess = request.ExternalProcess
    };

    private static ReportResponse MapToResponse(Report entity)
    {
        return new ReportResponse(
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
            ProcessState.Waiting,
            null,
            entity.ExternalProcess,
            entity.ProcessMessage
        );
    }
}
