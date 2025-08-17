using ReportCenter.Core.Reports.Entities;

namespace ReportCenter.Core.Reports.Interfaces;

public interface IReportService
{
    Task HandleAsync(Report report, CancellationToken cancellationToken = default);
}
