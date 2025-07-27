using FluentValidation;
using ReportCenter.Core.Reports.Commands;

namespace ReportCenter.Core.Reports.Validators;

public class CreateReportExportValidator : AbstractValidator<CreateReportExportCommand>
{
    public CreateReportExportValidator()
    {
        RuleFor(prop => prop.ExpirationDate)
            .GreaterThan(DateTimeOffset.Now.AddDays(1));
    }
}
