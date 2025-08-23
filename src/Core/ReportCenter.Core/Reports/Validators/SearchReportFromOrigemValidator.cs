using FluentValidation;
using ReportCenter.Core.Reports.Queries;

namespace ReportCenter.Core.Reports.Validators;

public class SearchReportFromOrigemValidator : AbstractValidator<SearchReportFromOrigemQuery>
{
    public SearchReportFromOrigemValidator()
    {
        RuleFor(prop => prop.Domain)
            .NotEmpty();

        RuleFor(prop => prop.Application)
            .NotEmpty();

        RuleFor(prop => (int)prop.version)
            .GreaterThan(0);

        RuleFor(prop => prop.DocumentName)
            .NotEmpty();

        RuleFor(prop => prop.Take)
            .GreaterThan(0)
            .LessThan(100);
    }
}
