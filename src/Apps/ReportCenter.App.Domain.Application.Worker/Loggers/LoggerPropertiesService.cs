using ReportCenter.Common.Loggers;
using ReportCenter.CustomConsoleFormatter.Interfaces;

namespace ReportCenter.App.Domain.Application.Worker.Loggers;

public class LoggerPropertiesService : ILoggerPropertiesService
{
    public LoggerPropertiesService()
    { }

    public string GetAppUser() => "Worker";

    public KeyValuePair<string, object?>[] DefaultPropertyList() =>
        new ReportBaseKeysLoggerRecord().Concat(
        new ReportDocumentKeysLoggerRecord()).ToArray();

    public KeyValuePair<string, object?>[] ScopeObjectStructuring(object value)
    {
        if (value is ReportBaseKeysLoggerRecord reportBaseKeysLoggerRecord)
            return reportBaseKeysLoggerRecord.ToArray();

        if (value is ReportDocumentKeysLoggerRecord reportDocumentKeysLoggerRecord)
            return reportDocumentKeysLoggerRecord.ToArray();

        return [];
    }
}
