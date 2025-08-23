using FluentValidation;
using ReportCenter.Core.Reports.Commands;

namespace ReportCenter.Core.Reports.Validators;

public class UpdateFileExtensionValidator : AbstractValidator<UpdateFileExtensionCommand>
{
    public UpdateFileExtensionValidator()
    {
        RuleFor(prop => prop.Id)
            .NotEmpty();

        RuleFor(prop => prop.FileExtension)
            .NotEmpty();
    }
}
