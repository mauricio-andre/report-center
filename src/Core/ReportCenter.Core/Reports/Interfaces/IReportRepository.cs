using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Core.Reports.Entities;

namespace ReportCenter.Core.Reports.Interfaces;

public interface IReportRepository
{
    /// <summary>
    /// Retorna um tipo IQueryable de Roport
    /// Não suporta operações assíncronas sob seus retornos
    /// </summary>
    /// <returns>IQueryable<Report></returns>
    public IQueryable<Report> AsQueryable();
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
