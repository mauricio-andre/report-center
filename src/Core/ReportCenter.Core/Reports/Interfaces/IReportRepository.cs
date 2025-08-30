using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Core.Reports.Entities;

namespace ReportCenter.Core.Reports.Interfaces;

public interface IReportRepository
{
    public Task InsertAsync(Report request, CancellationToken cancellationToken = default);
    public Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    public Task<Report?> GetByKeysAsync(
        string domain,
        string application,
        short version,
        string documentName,
        ReportType reportType,
        string documentKey,
        CancellationToken cancellationToken = default);
}
