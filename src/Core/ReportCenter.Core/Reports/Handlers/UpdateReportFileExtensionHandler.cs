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

public class UpdateReportFileExtensionHandler : IRequestHandler<UpdateReportFileExtensionCommand>
{
    private readonly CoreDbContext _coreDbContext;
    private readonly IStringLocalizer<ReportCenterResource> _stringLocalizer;
    private readonly IValidator<UpdateReportFileExtensionCommand> _validator;

    public UpdateReportFileExtensionHandler(
        CoreDbContext coreDbContext,
        IStringLocalizer<ReportCenterResource> stringLocalizer,
        IValidator<UpdateReportFileExtensionCommand> validator)
    {
        _coreDbContext = coreDbContext;
        _stringLocalizer = stringLocalizer;
        _validator = validator;
    }

    public async Task Handle(UpdateReportFileExtensionCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var entity = await _coreDbContext.Reports.FirstOrDefaultAsync(entity => entity.Id == request.Id);

        if (entity == null)
            throw new EntityNotFoundException(_stringLocalizer, nameof(Report), request.Id.ToString());

        entity.FileExtension = request.FileExtension;

        _coreDbContext.Update(entity);
        await _coreDbContext.SaveChangesAsync();
    }
}
