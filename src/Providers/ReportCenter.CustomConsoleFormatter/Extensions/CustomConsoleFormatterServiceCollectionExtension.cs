using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportCenter.CustomConsoleFormatter.Formatters;
using ReportCenter.CustomConsoleFormatter.Interfaces;
using ReportCenter.CustomConsoleFormatter.Options;
using ReportCenter.CustomConsoleFormatter.Services;

namespace ReportCenter.CustomConsoleFormatter.Extensions;

public static class CustomConsoleFormatterServiceCollectionExtension
{
    public static IServiceCollection AddCustomConsoleFormatterProvider(this IServiceCollection services)
    {
        services.AddSingleton<ILoggerPropertiesService, LoggerDefaultPropertiesService>();
        services.AddLogging(builder => builder.AddCustomFormatters());
        return services;
    }

    public static IServiceCollection AddCustomConsoleFormatterProvider<TLoggerProperties>(this IServiceCollection services)
    where TLoggerProperties : class, ILoggerPropertiesService
    {
        services.AddSingleton<ILoggerPropertiesService, TLoggerProperties>();
        services.AddLogging(builder => builder.AddCustomFormatters());
        return services;
    }

    public static ILoggingBuilder AddCustomFormatters(this ILoggingBuilder builder) =>
        builder
            .AddConsole()
            .AddConsoleFormatter<JsonCustomConsoleFormatter, JsonCustomConsoleFormatterOptions>();
}
