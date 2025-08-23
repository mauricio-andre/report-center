using Microsoft.Extensions.Localization;

namespace ReportCenter.Common.Exceptions;

public class ReportIsNotReadyToDownloadException : BusinessException
{
    public ReportIsNotReadyToDownloadException(IStringLocalizer localizer)
        : base(localizer["message:validation:reportIsNotReadyToDownload"])
    {
    }
}
