using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using ReportCenter.Common.Localization;
using ReportCenter.CustomStringLocalizer.Services;

namespace ReportCenter.CustomStringLocalizer.Extensions;

public static class CustomStringLocalizerServiceCollectionExtension
{
    public static IServiceCollection AddCustomStringLocalizerProvider(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IStringLocalizer<ReportCenterResource>>(serviceProvider =>
        {
            var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
            return new StringLocalizerService(memoryCache);

        });

        return services;
    }
}
