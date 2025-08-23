using FluentValidation;
using ReportCenter.Core.Reports.Queries;

namespace ReportCenter.Core.Reports.Validators;

public class DownloadReportValidator : AbstractValidator<DownloadReportQuery>
{
    public DownloadReportValidator()
    {
        RuleFor(prop => prop.Id)
            .NotEmpty();
    }
}
