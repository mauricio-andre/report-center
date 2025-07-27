using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ReportCenter.Common.Localization;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Core.Data;
using ReportCenter.Core.Reports.Events;
using ReportCenter.Core.Reports.Exceptions;

namespace ReportCenter.Core.Reports.Rules;

public class ShallNotAllowDuplicateReportRule : INotificationHandler<CreateReportEvent>
{
    private readonly CoreDbContext _coreDbContext;
    private readonly IStringLocalizer<ReportCenterResource> _stringLocalizer;

    public ShallNotAllowDuplicateReportRule(
        IDbContextFactory<CoreDbContext> dbContextFactory,
        IStringLocalizer<ReportCenterResource> stringLocalizer)
    {
        _coreDbContext = dbContextFactory.CreateDbContext();
        _stringLocalizer = stringLocalizer;
    }

    public async Task Handle(
        CreateReportEvent notification,
        CancellationToken cancellationToken)
    {
        ProcessState[] noEndStateArray = [ProcessState.Waiting, ProcessState.Processing];

        var hasDuplicate = await _coreDbContext.Reports.AnyAsync(export =>
            export.Domain == notification.Domain
            && export.Application == notification.Application
            && export.ReportType == notification.ReportType
            && export.DocumentName == notification.DocumentName
            && export.DocumentKey == notification.DocumentKey
            && noEndStateArray.Contains(export.ProcessState));

        if (hasDuplicate)
            throw new DuplicatedReportException(
                _stringLocalizer,
                new Dictionary<string, string[]>
                {
                    {
                        "ReportAlreadyProcessing",
                        [
                            _stringLocalizer[
                                "message:validation:combinationValuesAlreadyUse",
                                string.Concat(
                                    "[",
                                    notification.Application,
                                    "], [",
                                    notification.Domain,
                                    "], [",
                                    notification.ReportType.ToString(),
                                    "], [",
                                    notification.DocumentName,
                                    "], [",
                                    notification.DocumentKey,
                                    "]")
                            ]
                        ]
                    }
                });
    }
}
