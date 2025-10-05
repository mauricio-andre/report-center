using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReportCenter.OAuth.Services;

namespace ReportCenter.OAuth.Extensions;

public static class OAuthServiceCollectionExtension
{
    public static IServiceCollection AddOAuthProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OAuthOptions>(configuration.GetSection(OAuthOptions.Position));

        return services;
    }
}
