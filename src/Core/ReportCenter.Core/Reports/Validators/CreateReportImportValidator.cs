using FluentValidation;
using ReportCenter.Core.Reports.Commands;

namespace ReportCenter.Core.Reports.Validators;

public class CreateReportImportValidator : AbstractValidator<CreateReportImportCommand>
{
    private static readonly string[] AcceptExtensions = { ".csv", ".xlsx" };

    public CreateReportImportValidator()
    {
        RuleFor(prop => prop.ExpirationDate)
            .GreaterThan(DateTimeOffset.Now.AddDays(1));

        RuleFor(prop => prop.Stream)
            .NotNull();

        RuleFor(prop => prop.FileExtension)
            .NotNull()
            .NotEmpty()
            .Must(fileExtension => AcceptExtensions.Contains(fileExtension));
    }
}
