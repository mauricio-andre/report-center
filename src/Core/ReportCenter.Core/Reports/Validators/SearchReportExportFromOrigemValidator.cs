using FluentValidation;
using ReportCenter.Core.Reports.Queries;

namespace ReportCenter.Core.Reports.Validators;

public class SearchReportExportFromOrigemValidator : AbstractValidator<SearchReportFromOrigemQuery>
{
    public SearchReportExportFromOrigemValidator()
    {
        RuleFor(prop => prop.Domain)
            .NotEmpty();

        RuleFor(prop => prop.Application)
            .NotEmpty();

        RuleFor(prop => prop.DocumentName)
            .NotEmpty();

        RuleFor(prop => prop.Take)
            .GreaterThan(0)
            .LessThan(100);
    }
}
