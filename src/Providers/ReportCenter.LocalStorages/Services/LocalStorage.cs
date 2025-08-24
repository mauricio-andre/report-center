using ReportCenter.Common.Providers.Storage.Interfaces;

namespace ReportCenter.LocalStorages.Services;

public class LocalStorage : IStorageService
{
    private readonly string _basePath = Path.Combine("..", "..", "..", "tmp");

    public LocalStorage()
    {
    }

    public Task<Stream> OpenWriteAsync(string fullFileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, fullFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        Stream stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true);

        return Task.FromResult(stream);
    }

    public Task<Stream?> OpenReadAsync(string fullFileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, fullFileName);
        if (!File.Exists(filePath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true
        );

        return Task.FromResult<Stream?>(stream);
    }

    public Task SaveAsync(string fullFileName, Stream content, string? contentType = null, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, fullFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        using var fileStream = File.Create(filePath);
        return content.CopyToAsync(fileStream, cancellationToken);
    }
}
