using ReportCenter.Common.Providers.MessageQueues.Enums;

namespace ReportCenter.Common.Options;

public class ReportWorkerOptions
{
    public static readonly string Position = "ReportWorker";

    public string Domain { get; set; } = string.Empty;
    public string Application { get; set; } = string.Empty;
    public ReportType? ReportType { get; set; }
    public string? DocumentName { get; set; } = string.Empty;
}
