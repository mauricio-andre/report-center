using Microsoft.Extensions.Localization;

namespace ReportCenter.Common.Exceptions;

public class BadFormattedJsonException : BusinessException
{
    public BadFormattedJsonException(IStringLocalizer localizer) : base(localizer["message:validation:badFormattedJson"])
    {
    }

    public BadFormattedJsonException(
        IStringLocalizer localizer,
        Dictionary<string, string[]> errors) : base(localizer["message:validation:badFormattedJson"], errors)
    {
    }
}
