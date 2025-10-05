using ReportCenter.Core.Reports.Services;

namespace ReportCenter.Core.Reports.Interfaces;

public interface IBiggestReportExport
{
    public BiggestReportExportStream OpenWriteStream(
        string fullFileName,
        string sheetBaseName,
        DateTimeOffset expirationDate,
        int maxRowsPerSheet = 1_000_000,
        CancellationToken cancellationToken = default);
}
