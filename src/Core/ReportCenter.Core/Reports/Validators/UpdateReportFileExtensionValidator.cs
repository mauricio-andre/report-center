using FluentValidation;
using ReportCenter.Core.Reports.Commands;

namespace ReportCenter.Core.Reports.Validators;

public class UpdateReportFileExtensionValidator : AbstractValidator<UpdateReportFileExtensionCommand>
{
    public UpdateReportFileExtensionValidator()
    {
        RuleFor(prop => prop.Id)
            .NotNull();

        RuleFor(prop => prop.FileExtension)
            .NotNull()
            .IsInEnum();
    }
}
