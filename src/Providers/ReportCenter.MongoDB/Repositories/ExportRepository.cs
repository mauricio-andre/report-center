using MongoDB.Driver;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Core.Data;
using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Interfaces;
using ReportCenter.MongoDB.Configurations.Reports;

namespace ReportCenter.MongoDB.Repositories;

public class ExportRepository : IReportRepository
{
    private readonly IMongoCollection<Report> _collection;

    public ExportRepository(CoreDbContext mongoCoreDbContext, IMongoDatabase mongoDatabase)
    {
        _collection = mongoDatabase.GetCollection<Report>(ReportEfConfiguration.CollectionName);
    }

    public async Task InsertAsync(Report request, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(r => r.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Report?> GetByKeysAsync(
        string domain,
        string application,
        short version,
        string documentName,
        ReportType reportType,
        string documentKey,
        CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(entity => entity.Domain == domain
                && entity.Application == application
                && entity.Version == version
                && entity.DocumentName == documentName
                && entity.ReportType == reportType
                && entity.DocumentKey == documentKey)
            .SortByDescending(entity => entity.CreationDate)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
