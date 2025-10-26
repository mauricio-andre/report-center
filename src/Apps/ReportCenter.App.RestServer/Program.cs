using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using ReportCenter.App.RestServer.Authentication;
using ReportCenter.App.RestServer.Extensions;
using ReportCenter.App.RestServer.Filters;
using ReportCenter.App.RestServer.Middlewares;
using ReportCenter.App.RestServer.Transformers;
using ReportCenter.Common.Consts;
using ReportCenter.Common.Diagnostics;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;
using ReportCenter.Core.Data;
using ReportCenter.Core.Identity.Interfaces;
using ReportCenter.Core.Identity.Services;
using ReportCenter.Core.Reports.Interfaces;
using ReportCenter.CustomStringLocalizer.Extensions;
using ReportCenter.Mongo.Extensions;
using ReportCenter.MongoDB.Repositories;
using ReportCenter.OpenTelemetry.Extensions;
using ReportCenter.RabbitMQ.Extensions;
using ReportCenter.RabbitMQ.Services;
using ReportCenter.Scalar.Extensions;
using ReportCenter.Swagger.Extensions;
using Scalar.AspNetCore;
using ReportCenter.AzureServiceBus.Extensions;
using ReportCenter.AzureServiceBus.Services;
using ReportCenter.Common.Providers.Storage.Interfaces;
using ReportCenter.LocalStorages.Services;
using ReportCenter.AzureBlobStorages.Services;
using ReportCenter.AzureBlobStorages.Extensions;

namespace ReportCenter.App.RestServer;

public class Program
{
    protected Program()
    {
    }

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddMongoCoreDbContext(
                builder.Configuration.GetConnectionString("CoreDbContext")!,
                builder.Configuration.GetValue<string>("MongoDBName")!)
            .AddMediatR(config => config.RegisterServicesFromAssemblyContaining<CoreDbContext>())
            .Scan(scan => scan.FromAssembliesOf(typeof(CoreDbContext))
                .AddClasses(classes => classes.AssignableTo(typeof(AbstractValidator<>)))
                    .AsImplementedInterfaces()
                    .WithSingletonLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<>)))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime())
            .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .AddScoped<ICurrentIdentity, CurrentIdentity>()
            .AddSingleton(_ => new ReportCenterActivitySource(builder.Configuration.GetValue<string>("ServiceName")!))
            .AddScoped<IReportRepository, ExportRepository>()
            .AddSingleton<IMessagePublisher, AzureServiceBusPublisher>()
            .AddSingleton<IMessageConsumer, AzureServiceBusConsumer>()
            // .AddSingleton<IStorageService, AzureBlobStorage>();
            .AddSingleton<IStorageService, LocalStorage>();
            // .AddSingleton<IMessagePublisher, RabbitMQPublisher>();
            // .AddSingleton<IMessageConsumer, RabbitMQConsumer>();

        // Configuration string location
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportCultures = builder
                .Configuration
                .GetValue<string>("SupportedCultures")!
                .Split(",")
                .Select(culture => culture.Trim())
                .ToArray();

            options
                .SetDefaultCulture(supportCultures[0])
                .AddSupportedCultures(supportCultures)
                .AddSupportedUICultures(supportCultures);
        });

        // configuration controllers
        builder.Services
            .AddControllers(options =>
            {
                options.Conventions.Add(
                    new RouteTokenTransformerConvention(
                        new KebabCaseParameterTransformer()));

                options.Filters.Add<ExceptionFilter>();
            });

        // configuration API Explorer
        builder.Services
            .AddEndpointsApiExplorer()
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            })
            .AddOpenApiVersions(builder.Services);

        // configuration cors
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var origins = builder
                    .Configuration
                    .GetValue<string>("Cors:AllowedOrigins")!
                    .Split(",")
                    .Select(origin => origin.Trim())
                    .ToArray();

                policy
                    .WithOrigins(origins)
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowCredentials()
                    .AllowAnyMethod()
                    .WithHeaders("Tenant-Id")
                    .WithExposedHeaders("Content-Range");
            });
        });

        // configuration authentication
        builder.Services
            .AddAuthentication(schemes =>
            {
                schemes.DefaultAuthenticateScheme = AuthenticationDefaults.AuthenticationScheme;
                schemes.DefaultChallengeScheme = AuthenticationDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = builder.Configuration.GetValue<string>("Authentication:Bearer:Authority");
                options.Audience = builder.Configuration.GetValue<string>("Authentication:Bearer:Audience");
                options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
            })
            .AddScheme<AuthenticationOptions, AuthenticationHandler>(
                AuthenticationDefaults.AuthenticationScheme,
                AuthenticationDefaults.DisplayName,
                null);

        // Configure providers
        builder.Services.AddCustomStringLocalizerProvider();
        // builder.Services.AddAzureBlobStorageProvider(builder.Configuration, builder.Configuration.GetConnectionString("BlobStorage")!);
        // builder.Services.AddRabbitMQProvider(builder.Configuration, builder.Configuration.GetConnectionString("RabbitMQ")!);
        builder.Services.AddAzureServiceBusProvider(builder.Configuration, builder.Configuration.GetConnectionString("ServiceBus")!);
        builder.Services.AddSwaggerProvider(builder.Configuration);
        builder.AddOpenTelemetryProvider();

        var app = builder.Build();

        // configuration app
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCors();
        app.MapControllers();
        app.UseStaticFiles();
        app.UseRequestLocalization();
        app.MapOpenApi();

        // configuration swagger app
        app.UseSwaggerProvider();

        // configure Scalar
        app.UseScalarProvider(options =>
        {
            var clientId = app.Environment.IsDevelopment()
                ? app.Configuration.GetValue<string>("OpenApi:ClientId")
                : string.Empty;

            options
                .WithPreferredScheme(SecuritySchemeType.OAuth2.GetDisplayName())
                .WithOAuth2Authentication(oauth =>
                {
                    oauth.ClientId = clientId;
                    oauth.Scopes = app.Configuration.GetValue<string>("OpenApi:Scopes")!.Split(" ");
                })
                .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch);
        });

        app.UseMiddleware<IdentityMiddleware>();

        await app.RunAsync();
    }
}
