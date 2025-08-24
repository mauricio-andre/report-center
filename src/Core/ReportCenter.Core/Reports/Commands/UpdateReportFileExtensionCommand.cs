using MediatR;

namespace ReportCenter.Core.Reports.Commands;

public record UpdateReportFileExtensionCommand(
    Guid Id,
    string FileExtension
) : IRequest;
