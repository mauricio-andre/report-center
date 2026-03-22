using FluentValidation;
using ReportCenter.Core.Reports.Queries;

namespace ReportCenter.Core.Reports.Validators;

public class SearchReportValidator : AbstractValidator<SearchReportQuery>
{
    public SearchReportValidator()
    {
        RuleFor(prop => prop.Skip)
            .GreaterThan(0);

        RuleFor(prop => prop.Take)
            .GreaterThan(0)
            .LessThan(100);
    }
}
