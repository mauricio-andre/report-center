namespace ReportCenter.Common.Providers.Storage.Interfaces;

public interface IStorageService
{
    Task<Stream> OpenWriteAsync(string fullFileName, CancellationToken cancellationToken = default);
    Task SaveAsync(string fullFileName, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fullFileName, CancellationToken cancellationToken = default);
}
