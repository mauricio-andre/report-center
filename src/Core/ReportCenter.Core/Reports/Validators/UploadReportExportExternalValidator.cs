using FluentValidation;
using ReportCenter.Core.Reports.Commands;

namespace ReportCenter.Core.Reports.Validators;

public class UploadReportExportExternalValidator : AbstractValidator<UploadReportExportExternalCommand>
{
    public UploadReportExportExternalValidator()
    {
        RuleFor(prop => prop.Id)
            .NotNull();

        RuleFor(prop => prop.Stream)
            .NotNull();
    }
}
