using Microsoft.Extensions.Localization;

namespace ReportCenter.Common.Exceptions;

public class DuplicatedEntityException : BusinessException
{
    public DuplicatedEntityException(IStringLocalizer localizer, string entity) : base(localizer["message:validation:duplicatedEntity", entity])
    {
    }

    public DuplicatedEntityException(
        IStringLocalizer localizer,
        string entity,
        Dictionary<string, string[]> errors) : base(localizer["message:validation:duplicatedEntity", entity], errors)
    {
    }

    public DuplicatedEntityException(string message) : base(message)
    {
    }

    public DuplicatedEntityException(
        string message,
        Dictionary<string, string[]> errors) : base(message, errors)
    {
    }
}
