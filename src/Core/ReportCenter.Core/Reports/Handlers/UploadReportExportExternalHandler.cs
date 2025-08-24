using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ReportCenter.Common.Exceptions;
using ReportCenter.Common.Localization;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Common.Providers.Storage.Interfaces;
using ReportCenter.Core.Data;
using ReportCenter.Core.Reports.Commands;
using ReportCenter.Core.Reports.Entities;

namespace ReportCenter.Core.Reports.Handlers;

public class UploadReportExportExternalHandler : IRequestHandler<UploadReportExportExternalCommand>
{
    private readonly IStorageService _storageService;
    private readonly CoreDbContext _coreDbContext;
    private readonly IValidator<UploadReportExportExternalCommand> _validator;
    private readonly IStringLocalizer<ReportCenterResource> _stringLocalizer;

    public UploadReportExportExternalHandler(
        IStorageService storageService,
        IDbContextFactory<CoreDbContext> dbContextFactory,
        IValidator<UploadReportExportExternalCommand> validator,
        IStringLocalizer<ReportCenterResource> stringLocalizer)
    {
        _coreDbContext = dbContextFactory.CreateDbContext();
        _validator = validator;
        _storageService = storageService;
        _stringLocalizer = stringLocalizer;
    }

    public async Task Handle(UploadReportExportExternalCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var report = await _coreDbContext.Reports
            .Where(report => report.Id == request.Id)
            .FirstOrDefaultAsync();

        if (report == null)
            throw new EntityNotFoundException(_stringLocalizer, nameof(Report), request.Id.ToString());

        if (report.ProcessState == ProcessState.Success)
            throw new DuplicatedEntityException(_stringLocalizer, nameof(Report));

        if (!report.ExternalProcess)
            throw new ReportIsNotExternalProcessException(_stringLocalizer);

        report.FileExtension = request.FileExtension ?? report.FileExtension;
        report.ProcessTimer = request.ProcessTimer;
        report.ProcessState = ProcessState.Success;

        await _storageService.SaveAsync(report.FullFileName, request.Stream, cancellationToken: cancellationToken);
        _coreDbContext.Reports.Update(report);
        await _coreDbContext.SaveChangesAsync();
    }
}
