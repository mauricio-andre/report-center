using FluentValidation;
using ReportCenter.Core.Reports.Queries;

namespace ReportCenter.Core.Reports.Validators;

public class GetReportByKeysValidator : AbstractValidator<GetReportByKeysQuery>
{
    public GetReportByKeysValidator()
    {
        RuleFor(prop => prop.Domain)
            .NotEmpty();

        RuleFor(prop => prop.Application)
            .NotEmpty();

        RuleFor(prop => (int)prop.Version)
            .GreaterThan(0);

        RuleFor(prop => prop.DocumentName)
            .NotEmpty();

        RuleFor(prop => prop.ReportType)
            .IsInEnum();

        RuleFor(prop => prop.DocumentKey)
            .NotEmpty();
    }
}
