using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ReportCenter.Common.Exceptions;
using ReportCenter.Common.Localization;
using ReportCenter.Core.Data;
using ReportCenter.Core.Reports.Commands;
using ReportCenter.Core.Reports.Entities;
namespace ReportCenter.Core.Reports.Handlers;

public class UpdateReportExternalProcessStateHandler : IRequestHandler<UpdateReportExternalProcessStateCommand>
{
    private readonly CoreDbContext _coreDbContext;
    private readonly IStringLocalizer<ReportCenterResource> _stringLocalizer;
    private readonly IValidator<UpdateReportExternalProcessStateCommand> _validator;

    public UpdateReportExternalProcessStateHandler(
        IDbContextFactory<CoreDbContext> dbContextFactory,
        IStringLocalizer<ReportCenterResource> stringLocalizer,
        IValidator<UpdateReportExternalProcessStateCommand> validator)
    {
        _coreDbContext = dbContextFactory.CreateDbContext();
        _stringLocalizer = stringLocalizer;
        _validator = validator;
    }

    public async Task Handle(
        UpdateReportExternalProcessStateCommand request,
        CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var entity = await _coreDbContext.Reports.FirstOrDefaultAsync(entity => entity.Id == request.Id);

        if (entity == null)
            throw new EntityNotFoundException(_stringLocalizer, nameof(Report), request.Id.ToString());

        if (!entity.ExternalProcess)
            throw new ReportIsNotExternalProcessException(_stringLocalizer);

        entity.ProcessState = request.ProcessState;
        entity.ProcessTimer = request.ProcessTimer;
        entity.ProcessMessage = request.ProcessMessage;

        _coreDbContext.Update(entity);
        await _coreDbContext.SaveChangesAsync();
    }
}
