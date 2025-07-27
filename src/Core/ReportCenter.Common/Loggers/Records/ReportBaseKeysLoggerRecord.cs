using System.Collections;

namespace ReportCenter.Common.Loggers;

public record ReportBaseKeysLoggerRecord : IEnumerable<KeyValuePair<string, object?>>
{
    private string? Domain { get; }
    private string? Application { get; }
    public ReportBaseKeysLoggerRecord()
    {
    }

    public ReportBaseKeysLoggerRecord(
        string domain,
        string application)
    {
        Domain = domain;
        Application = application;
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        yield return new KeyValuePair<string, object?>("domain", Domain ?? "");
        yield return new KeyValuePair<string, object?>("application", Application ?? "");
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
