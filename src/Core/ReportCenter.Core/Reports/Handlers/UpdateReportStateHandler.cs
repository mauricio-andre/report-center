using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ReportCenter.Common.Exceptions;
using ReportCenter.Common.Localization;
using ReportCenter.Common.Providers.MessageQueues.Dtos;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;
using ReportCenter.Core.Data;
using ReportCenter.Core.Reports.Commands;
using ReportCenter.Core.Reports.Entities;
namespace ReportCenter.Core.Reports.Handlers;

public class UpdateReportStateHandler : IRequestHandler<UpdateReportStateCommand>
{
    private readonly CoreDbContext _coreDbContext;
    private readonly IStringLocalizer<ReportCenterResource> _stringLocalizer;
    private readonly IValidator<UpdateReportStateCommand> _validator;
    private readonly IMessagePublisher _messagePublisher;

    public UpdateReportStateHandler(
        IDbContextFactory<CoreDbContext> dbContextFactory,
        IStringLocalizer<ReportCenterResource> stringLocalizer,
        IValidator<UpdateReportStateCommand> validator,
        IMessagePublisher messagePublisher)
    {
        _coreDbContext = dbContextFactory.CreateDbContext();
        _stringLocalizer = stringLocalizer;
        _validator = validator;
        _messagePublisher = messagePublisher;
    }

    public async Task Handle(
        UpdateReportStateCommand request,
        CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var entity = await _coreDbContext.Reports.FirstOrDefaultAsync(
            entity => entity.Id == request.Id,
            cancellationToken);

        if (entity == null)
            throw new EntityNotFoundException(_stringLocalizer, nameof(Report), request.Id.ToString());

        entity.ProcessState = request.ProcessState;
        entity.ProcessTimer = request.ProcessTimer;
        entity.ProcessMessage = request.ProcessMessage;

        _coreDbContext.Update(entity);
        await _coreDbContext.SaveChangesAsync(cancellationToken);

        await _messagePublisher.PublishProgressAsync(
            new ReportMessageProgressDto(
                entity.Id,
                entity.Domain,
                entity.Application,
                entity.Version,
                entity.DocumentName,
                entity.ReportType,
                entity.DocumentKey,
                entity.ProcessState,
                entity.ProcessTimer,
                entity.ProcessMessage,
                entity.ProcessState == ProcessState.Waiting
            ),
            cancellationToken: cancellationToken
        );
    }
}
