using Microsoft.Extensions.Localization;
using ReportCenter.Common.Exceptions;

namespace ReportCenter.Core.Reports.Exceptions;

public class DuplicatedReportException : DuplicatedEntityException
{
    public DuplicatedReportException(IStringLocalizer localizer) : base(localizer["message:validation:duplicatedReport"])
    {
    }

    public DuplicatedReportException(
        IStringLocalizer localizer,
        Dictionary<string, string[]> errors) : base(localizer["message:validation:duplicatedReport"], errors)
    {
    }
}
