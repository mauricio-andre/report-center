using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Interfaces;

namespace ReportCenter.App.Domain.Application.Worker.Reports;

public class ReportServiceFactory : IReportServiceFactory
{
    public const string CusteioPegasusExportV1Resumo = "CUSTEIO:PEGASUS:V1:RESUMO:EXPORT";
    public const string CusteioPegasusExportV2Resumo = "CUSTEIO:PEGASUS:V2:RESUMO:EXPORT";

    public IReportService CreateInstance(Report report, IServiceScope scope)
    {
        return report.ComposeWorkerKey.ToUpper() switch
        {
            CusteioPegasusExportV1Resumo => scope.ServiceProvider.GetRequiredService<V1.Example.ExportExampleService>(),
            CusteioPegasusExportV2Resumo => scope.ServiceProvider.GetRequiredService<V2.Example.ExportExampleService>(),
            _ => throw new NotImplementedException(),
        };
    }
}
