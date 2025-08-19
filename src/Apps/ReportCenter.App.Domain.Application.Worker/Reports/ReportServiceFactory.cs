using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Interfaces;

namespace ReportCenter.App.Domain.Application.Worker.Reports;

public class ReportServiceFactory : IReportServiceFactory
{
    public const string CusteioPegasusExportV1Resumo = "CUSTEIO:PEGASUS:EXPORT:1:RESUMO";
    public const string CusteioPegasusExportV2Resumo = "CUSTEIO:PEGASUS:EXPORT:2:RESUMO";

    public IReportService CreateInstance(Report report, IServiceScope scope)
    {
        return report.ComposeDocKey.ToUpper() switch
        {
            CusteioPegasusExportV1Resumo => scope.ServiceProvider.GetRequiredService<V1.Example.ExportExampleService>(),
            CusteioPegasusExportV2Resumo => scope.ServiceProvider.GetRequiredService<V2.Example.ExportExampleService>(),
            _ => throw new NotImplementedException(),
        };
    }
}
