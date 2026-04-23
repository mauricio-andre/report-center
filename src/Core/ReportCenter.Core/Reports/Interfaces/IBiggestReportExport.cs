using ReportCenter.Core.Reports.Services;

namespace ReportCenter.Core.Reports.Interfaces;

public interface IBiggestReportExport
{
    public BiggestReportExportStream OpenWriteStream(
        string fullFileName,
        string sheetBaseName,
        DateTimeOffset expirationDate,
        int? maxRows = null,
        CancellationToken cancellationToken = default);
}
