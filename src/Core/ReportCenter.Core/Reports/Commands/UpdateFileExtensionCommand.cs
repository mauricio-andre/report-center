using MediatR;

namespace ReportCenter.Core.Reports.Commands;

public record UpdateFileExtensionCommand(
    Guid Id,
    string FileExtension
) : IRequest;
