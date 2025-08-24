namespace ReportCenter.Core.Reports.Responses;

public record DownloadReportResponse(
    Stream Stream,
    string FileName
);
