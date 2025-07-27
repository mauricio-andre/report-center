using ReportCenter.App.Domain.Application.Worker.Reports.V1.Example;
using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Interfaces;

namespace ReportCenter.App.Domain.Application.Worker.Reports;

public class ReportServiceFactory : IReportServiceFactory
{
    public IReportService CreateInstance(Report report, IServiceScope scope)
    {
        var key = string.Concat(
            report.Domain,
            ":",
            report.Application,
            ":",
            report.ReportType.ToString(),
            ":",
            report.Version,
            ":",
            report.DocumentName).ToUpper();

        return key switch
        {
            "DOMAIN:APPLICATION:EXPORT:1:DOCUMENTNAME" => scope.ServiceProvider.GetRequiredService<ExportExampleService>(),
            _ => throw new NotImplementedException(),
        };
    }
}
