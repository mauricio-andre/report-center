using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ReportCenter.Common.Exceptions;
using ReportCenter.Common.Localization;
using ReportCenter.Core.Data;
using ReportCenter.Core.Reports.Commands;
using ReportCenter.Core.Reports.Entities;
namespace ReportCenter.Core.Reports.Handlers;

public class UpdateStateReportHandler : IRequestHandler<UpdateStateReportCommand>
{
    private readonly CoreDbContext _coreDbContext;
    private readonly IStringLocalizer<ReportCenterResource> _stringLocalizer;

    public UpdateStateReportHandler(
        IDbContextFactory<CoreDbContext> dbContextFactory,
        IStringLocalizer<ReportCenterResource> stringLocalizer)
    {
        _coreDbContext = dbContextFactory.CreateDbContext();
        _stringLocalizer = stringLocalizer;
    }

    public async Task Handle(
        UpdateStateReportCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _coreDbContext.Reports.FirstOrDefaultAsync(entity => entity.Id == request.Id);

        if (entity == null)
            throw new EntityNotFoundException(_stringLocalizer, nameof(Report), request.Id.ToString());

        entity.ProcessState = request.ProcessState;
        entity.ProcessTimer = request.ProcessTimer;

        if (!string.IsNullOrEmpty(request.FileExtension))
            entity.FileExtension = request.FileExtension;

        _coreDbContext.Update(entity);
        await _coreDbContext.SaveChangesAsync();
    }
}
