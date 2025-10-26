namespace ReportCenter.Common.Providers.OAuth.Interfaces;

public interface IOAuthTokenService
{
    public Task<string> GetOAuthTokenAsync();
}
