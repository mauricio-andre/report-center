using System.Diagnostics;

namespace ReportCenter.Common.Diagnostics;

public class ReportCenterActivitySource
{
    public readonly ActivitySource ActivitySourceDefault;
    public ReportCenterActivitySource(string serviceNameDefault)
    {
        ActivitySourceDefault = new ActivitySource(serviceNameDefault);
    }
}
