using MediatR;

namespace ReportCenter.Core.Reports.Commands;

public record UploadReportExportExternalCommand(
    Guid Id,
    Stream Stream,
    string? FileExtension,
    TimeSpan? ProcessTimer
) : IRequest;
