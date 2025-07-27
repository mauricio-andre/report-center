using ReportCenter.Common.Providers.Storage.Interfaces;

namespace ReportCenter.LocalStorage.Services;

public class LocalStorage : IStorageService
{
    public LocalStorage()
    {
    }

    public Task SaveAsync(string fullFileName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fullFileName);
        using var fileStream = File.Create(filePath);
        return content.CopyToAsync(fileStream, cancellationToken);
    }
}
