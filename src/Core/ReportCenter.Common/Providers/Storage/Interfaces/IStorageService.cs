namespace ReportCenter.Common.Providers.Storage.Interfaces;

public interface IStorageService
{
    Task<Stream> OpenWriteAsync(
        string fullFileName,
        DateTimeOffset expirationDate,
        string? contentType = null,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string fullFileName, CancellationToken cancellationToken = default);

    Task SaveAsync(
        string fullFileName,
        Stream content,
        DateTimeOffset expirationDate,
        string? contentType = null,
        CancellationToken cancellationToken = default);
}
