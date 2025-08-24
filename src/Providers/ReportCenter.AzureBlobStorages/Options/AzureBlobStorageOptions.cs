namespace ReportCenter.AzureBlobStorages.Options;

public class AzureBlobStorageOptions
{
    public static readonly string Position = "AzureBlobStorage";

    public string ContainerName { get; set; } = string.Empty;
}
