namespace ReportCenter.RabbitMQ.Options;

public class RabbitMQOptions
{
    public static readonly string Position = "RabbitMQ";

    public string QueueName { get; set; } = string.Empty;
}
