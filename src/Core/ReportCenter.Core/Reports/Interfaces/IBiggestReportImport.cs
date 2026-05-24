using ReportCenter.Core.Reports.Services;

namespace ReportCenter.Core.Reports.Interfaces;

public interface IBiggestReportImport
{
    public Task<BiggestReportImportStream> OpenReadStreamAsync(
        string fullFileName,
        CancellationToken cancellationToken = default);
}
