using ReportCenter.Common.Providers.OAuth.Dtos;

namespace ReportCenter.Common.Providers.OAuth.Interfaces;

public interface IOAuthTokenService
{
    public Task<string> GetOAuthTokenAsync(OAuthTokenRequestDto request);
}
