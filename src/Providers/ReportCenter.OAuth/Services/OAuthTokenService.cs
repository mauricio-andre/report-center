using System.Net.Http.Json;
using System.Security.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportCenter.Common.Providers.OAuth.Dtos;
using ReportCenter.Common.Providers.OAuth.Interfaces;
using ReportCenter.OAuth.Dtos;
using ReportCenter.OAuth.Services;

namespace ReportCenter.OAuth.Options;

public class OAuthTokenService : IOAuthTokenService
{
    private readonly OAuthOptions _oAuthOptions;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<OAuthTokenService> _logger;

    public OAuthTokenService(
        IOptions<OAuthOptions> oAuthOptions,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        ILogger<OAuthTokenService> logger)
    {
        _oAuthOptions = oAuthOptions.Value;
        _httpClient = httpClientFactory.CreateClient();
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<string> GetOAuthTokenAsync(OAuthTokenRequestDto request)
    {
        return _memoryCache.GetOrCreateAsync(
            $"authenticationClient:accessToken:",
            async cacheEntry =>
            {
                var url = new Uri(_oAuthOptions.UrlToken);
                var data = new List<KeyValuePair<string, string>>()
                {
                    new ("grant_type", "client_credentials"),
                    new ("client_id", request.ClientId),
                    new ("client_secret", request.ClientSecret)
                };

                if (!string.IsNullOrEmpty(_oAuthOptions.Audience))
                    data.Add(new KeyValuePair<string, string>("audience", _oAuthOptions.Audience));

                if (!string.IsNullOrEmpty(_oAuthOptions.Scopes))
                    data.Add(new KeyValuePair<string, string>("scope", _oAuthOptions.Scopes));

                var form = new FormUrlEncodedContent(data);

                var response = await _httpClient.PostAsync(url, form);
                if (!response.IsSuccessStatusCode)
                {
                    var resultError = await response.Content.ReadAsStringAsync();
                    _logger.LogError(resultError);
                    throw new AuthenticationException(resultError);
                }

                var result = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
                if (result == null)
                {
                    _logger.LogError("Failed to serialize authentication service response");
                    throw new AuthenticationException("Failed to serialize authentication service response");
                }

                _logger.LogTrace($"Client {request.ClientId} has requested an access token successfully");

                cacheEntry.SlidingExpiration = TimeSpan.FromSeconds(result.ExpisesIn - 30);
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(result.ExpisesIn - 10);

                return result.AccessToken;
            })!;
    }
}
