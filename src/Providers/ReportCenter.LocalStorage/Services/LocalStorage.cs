using ReportCenter.Common.Providers.Storage.Interfaces;

namespace ReportCenter.LocalStorage.Services;

public class LocalStorage : IStorageService
{
    private readonly string _basePath = ".";//Path.GetTempPath();

    public LocalStorage()
    {
    }

    public Task<Stream> OpenWriteAsync(string fullFileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, fullFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        Stream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        return Task.FromResult(stream);
    }

    public Task SaveAsync(string fullFileName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, fullFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        using var fileStream = File.Create(filePath);
        return content.CopyToAsync(fileStream, cancellationToken);
    }

    public Task DeleteAsync(string fullFileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, fullFileName);
        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }
}
