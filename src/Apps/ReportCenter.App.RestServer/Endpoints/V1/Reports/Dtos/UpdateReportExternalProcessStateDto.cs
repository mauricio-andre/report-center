using ReportCenter.Common.Providers.MessageQueues.Enums;

namespace ReportCenter.App.RestServer.Endpoints.V1.Reports.Dtos;

public record UpdateReportExternalProcessStateDto(
    ProcessState ProcessState,
    TimeSpan? ProcessTimer = null,
    string? ProcessMessage = null
);
