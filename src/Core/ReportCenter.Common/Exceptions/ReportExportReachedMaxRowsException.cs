using Microsoft.Extensions.Localization;

namespace ReportCenter.Common.Exceptions;

public class ReportExportReachedMaxRowsException : BusinessException
{
    public ReportExportReachedMaxRowsException()
        : base("Sheet reached max rows")
    {
    }
}
