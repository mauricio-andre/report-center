namespace ReportCenter.RabbitMQ.Options;

public class RabbitMQOptions
{
    public static readonly string Position = "RabbitMQ";

    public string ProgressQueueName { get; set; } = string.Empty;
    public string ProcessesQueueName { get; set; } = string.Empty;
}
