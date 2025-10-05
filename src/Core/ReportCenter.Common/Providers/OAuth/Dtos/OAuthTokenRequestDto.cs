namespace ReportCenter.Common.Providers.OAuth.Dtos;

public record OAuthTokenRequestDto(
    string ClientId,
    string ClientSecret);
