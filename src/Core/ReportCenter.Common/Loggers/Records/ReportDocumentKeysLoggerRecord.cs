using System.Collections;

namespace ReportCenter.Common.Loggers;

public record ReportDocumentKeysLoggerRecord : IEnumerable<KeyValuePair<string, object?>>
{
    private string? ReportType { get; }
    private string? DocumentName { get; }
    private string? DocumentKey { get; }
    private short? Version { get; }

    public ReportDocumentKeysLoggerRecord()
    {
    }

    public ReportDocumentKeysLoggerRecord(
        string reportType,
        string documentName,
        string documentKey,
        short version)
    {
        ReportType = reportType;
        DocumentName = documentName;
        DocumentKey = documentKey;
        Version = version;
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        yield return new KeyValuePair<string, object?>("reportType", ReportType ?? "");
        yield return new KeyValuePair<string, object?>("documentName", DocumentName ?? "");
        yield return new KeyValuePair<string, object?>("documentKey", DocumentKey ?? "");
        yield return new KeyValuePair<string, object?>("version", Version ?? 0);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
