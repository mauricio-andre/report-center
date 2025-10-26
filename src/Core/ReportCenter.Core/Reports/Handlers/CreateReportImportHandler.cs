using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;
using ReportCenter.Common.Localization;
using ReportCenter.Common.Providers.MessageQueues.Dtos;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;
using ReportCenter.Common.Providers.Storage.Interfaces;
using ReportCenter.Core.Identity.Interfaces;
using ReportCenter.Core.Reports.Commands;
using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Events;
using ReportCenter.Core.Reports.Interfaces;
using ReportCenter.Core.Reports.ObjectValues;
using ReportCenter.Core.Reports.Responses;

namespace ReportCenter.Core.Reports.Handlers;

public class CreateReportImportHandler : IRequestHandler<CreateReportImportCommand, ReportCompleteResponse>
{
    private readonly IReportRepository _reportRepository;
    private readonly IMediator _mediator;
    private readonly IValidator<CreateReportImportCommand> _validator;
    private readonly ICurrentIdentity _currentIdentity;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IStringLocalizer<ReportCenterResource> _stringLocalizer;
    private readonly IStorageService _storageService;

    public CreateReportImportHandler(
        IReportRepository reportRepository,
        IMediator mediator,
        IValidator<CreateReportImportCommand> validator,
        ICurrentIdentity currentIdentity,
        IMessagePublisher messagePublisher,
        IStringLocalizer<ReportCenterResource> stringLocalizer,
        IStorageService storageService)
    {
        _reportRepository = reportRepository;
        _mediator = mediator;
        _validator = validator;
        _currentIdentity = currentIdentity;
        _messagePublisher = messagePublisher;
        _stringLocalizer = stringLocalizer;
        _storageService = storageService;
    }

    public async Task<ReportCompleteResponse> Handle(CreateReportImportCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        await _mediator.Publish(new CreateReportEvent(
            request.Domain,
            request.Application,
            ReportType.Import,
            request.DocumentName,
            request.DocumentKey
        ), cancellationToken);

        var entity = MapToEntity(request);

        await _storageService.SaveAsync(
            entity.FullFileName,
            request.Stream,
            expirationDate: entity.ExpirationDate,
            cancellationToken: cancellationToken);

        await _reportRepository.InsertAsync(entity);

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

    private Report MapToEntity(CreateReportImportCommand request) => new Report()
    {
        Id = Guid.NewGuid(),
        Domain = request.Domain.ToUpper(),
        Application = request.Application.ToUpper(),
        ReportType = ReportType.Import,
        DocumentName = request.DocumentName.ToUpper(),
        DocumentKey = request.DocumentKey,
        Version = request.Version,
        UserIdentifier = _currentIdentity.GetNameIdentifier()!,
        CreationDate = DateTimeOffset.Now,
        ExpirationDate = request.ExpirationDate,
        Filters = new FlexibleObject(request.Filters ?? new()),
        ExtraProperties = new FlexibleObject(request.ExtraProperties ?? new()),
        ExternalProcess = request.ExternalProcess,
        FileExtension = request.FileExtension
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
