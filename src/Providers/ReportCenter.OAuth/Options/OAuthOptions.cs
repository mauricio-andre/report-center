namespace ReportCenter.OAuth.Services;

public class OAuthOptions
{
    public static readonly string Position = "OAuth";

    public string UrlToken { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
}
