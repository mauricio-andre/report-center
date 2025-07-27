using System.ComponentModel;

namespace ReportCenter.Common.Providers.MessageQueues.Enums;

public enum ReportType
{
    [Description("Import")]
    Import = 1,
    [Description("Export")]
    Export = 2
}
