namespace ReportCenter.AzureServiceBus.Dtos;

public record MessageProgressBodyDto(
    Guid Id,
    TimeSpan? ProcessTimer,
    string? ProcessMessage,
    bool Requeue);
