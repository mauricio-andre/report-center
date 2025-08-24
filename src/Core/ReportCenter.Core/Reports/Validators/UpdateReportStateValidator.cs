using FluentValidation;
using ReportCenter.Core.Reports.Commands;

namespace ReportCenter.Core.Reports.Validators;

public class UpdateReportStateValidator : AbstractValidator<UpdateReportStateCommand>
{
    public UpdateReportStateValidator()
    {
        RuleFor(prop => prop.Id)
            .NotNull();

        RuleFor(prop => prop.ProcessState)
            .NotNull()
            .IsInEnum();
    }
}
