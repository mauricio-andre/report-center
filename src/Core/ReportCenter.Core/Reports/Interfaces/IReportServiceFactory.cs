using Microsoft.Extensions.DependencyInjection;
using ReportCenter.Core.Reports.Entities;

namespace ReportCenter.Core.Reports.Interfaces;

public interface IReportServiceFactory
{
    IReportService CreateInstance(Report report, IServiceScope scope);
}
