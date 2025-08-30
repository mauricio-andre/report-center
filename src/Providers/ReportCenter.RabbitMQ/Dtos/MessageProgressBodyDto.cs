namespace ReportCenter.RabbitMQ.Dtos;

public record MessageProgressBodyDto(
    Guid Id,
    TimeSpan? ProcessTimer,
    string? ProcessMessage,
    bool Requeue);
