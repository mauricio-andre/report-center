using Microsoft.Extensions.Localization;

namespace ReportCenter.Common.Exceptions;

public class ReportIsNotExternalProcessException : BusinessException
{
    public ReportIsNotExternalProcessException(IStringLocalizer localizer)
        : base(localizer["message:validation:reportIsNotExternalProcess"])
    {
    }
}
