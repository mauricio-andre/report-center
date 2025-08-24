using FluentValidation;
using ReportCenter.Core.Reports.Queries;

namespace ReportCenter.Core.Reports.Validators;

public class GetReportByIdValidator : AbstractValidator<GetReportByIdQuery>
{
    public GetReportByIdValidator()
    {
        RuleFor(prop => prop.Id)
            .NotNull();
    }
}
