using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ReportCenter.Common.Exceptions;
using ReportCenter.Common.Localization;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Common.Providers.Storage.Interfaces;
using ReportCenter.Core.Data;
using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Queries;
using ReportCenter.Core.Reports.Responses;

namespace ReportCenter.Core.Reports.Handlers;

public class DownloadReportHandler : IRequestHandler<DownloadReportQuery, DownloadReportResponse?>
{
    private readonly IStorageService _storageService;
    private readonly CoreDbContext _coreDbContext;
    private readonly IValidator<DownloadReportQuery> _validator;
    private readonly IStringLocalizer<ReportCenterResource> _stringLocalizer;

    public DownloadReportHandler(
        IStorageService storageService,
        IDbContextFactory<CoreDbContext> dbContextFactory,
        IValidator<DownloadReportQuery> validator,
        IStringLocalizer<ReportCenterResource> stringLocalizer)
    {
        _coreDbContext = dbContextFactory.CreateDbContext();
        _validator = validator;
        _storageService = storageService;
        _stringLocalizer = stringLocalizer;
    }

    public async Task<DownloadReportResponse?> Handle(DownloadReportQuery request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var report = await _coreDbContext.Reports
            .Where(report => report.Id == request.Id)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (report == null)
            throw new EntityNotFoundException(_stringLocalizer, nameof(Report), request.Id.ToString());

        if (report.ProcessState != ProcessState.Success)
            throw new ReportIsNotReadyToDownloadException(_stringLocalizer);

        var stream = await _storageService.OpenReadAsync(report.FullFileName, cancellationToken);
        if (stream == null)
            return null;

        return new DownloadReportResponse(stream, string.Concat(report.Id, report.FileExtension));
    }
}
