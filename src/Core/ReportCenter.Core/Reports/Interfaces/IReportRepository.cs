using ReportCenter.Core.Reports.Entities;

namespace ReportCenter.Core.Reports.Interfaces;

public interface IReportRepository
{
    public Task InsertAsync(Report request, CancellationToken cancellationToken = default);
    public Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
