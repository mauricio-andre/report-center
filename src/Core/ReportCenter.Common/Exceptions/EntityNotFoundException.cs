using Microsoft.Extensions.Localization;

namespace ReportCenter.Common.Exceptions;

public class EntityNotFoundException : BusinessException
{
    public EntityNotFoundException(IStringLocalizer localizer, string entity, string key)
        : base(localizer["message:validation:entityNotFound", entity, key])
    {
    }
}
