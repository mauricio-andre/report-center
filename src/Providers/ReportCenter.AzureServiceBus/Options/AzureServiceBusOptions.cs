namespace ReportCenter.AzureServiceBus.Options;

public class AzureServiceBusOptions
{
    public static readonly string Position = "AzureServiceBus";

    public string TopicName { get; set; } = string.Empty;
    public string SubscriptionName { get; set; } = string.Empty;
}
