using CqrsProject.App.GrpcServer.Methods.V1.Examples;
using FluentValidation;
using MediatR;
using ReportCenter.App.GrpcServer.Authentication;
using ReportCenter.App.GrpcServer.GrpcMetadata;
using ReportCenter.App.GrpcServer.Interceptors;
using ReportCenter.App.GrpcServer.Loggers;
using ReportCenter.Common.Consts;
using ReportCenter.Common.Diagnostics;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;
using ReportCenter.Core.Data;
using ReportCenter.Core.Identity.Interfaces;
using ReportCenter.Core.Identity.Services;
using ReportCenter.Core.Reports.Interfaces;
using ReportCenter.CustomConsoleFormatter.Extensions;
using ReportCenter.CustomStringLocalizer.Extensions;
using ReportCenter.Mongo.Extensions;
using ReportCenter.MongoDB.Repositories;
using ReportCenter.OpenTelemetry.Extensions;
using ReportCenter.RabbitMQ.Extensions;
using ReportCenter.RabbitMQ.Services;
using ReportCenter.AzureServiceBus.Extensions;
using ReportCenter.AzureServiceBus.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMongoCoreDbContext(
        builder.Configuration.GetConnectionString("CoreDbContext")!,
        builder.Configuration.GetValue<string>("MongoDBName")!)
    // .AddRabbitMQConsumer(builder.Configuration, builder.Configuration.GetConnectionString("RabbitMQ")!)
    .AddAzureServiceBusConsumer(builder.Configuration, builder.Configuration.GetConnectionString("ServiceBus")!)
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
    .AddScoped<IReportRepository, ExportRepository>()
    .AddSingleton<IGrpcInterceptorAttributeMap, GrpcInterceptorAttributeMap>()
    .AddSingleton(_ => new ReportCenterActivitySource(builder.Configuration.GetValue<string>("ServiceName")!))
    .AddSingleton<IMessagePublisher, AzureServiceBusPublisher>();
    // .AddSingleton<IMessagePublisher, RabbitMQPublisher>();

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
builder.Services.AddCustomConsoleFormatterProvider<LoggerPropertiesService>();
builder.AddOpenTelemetryProvider();

// Add gRPC
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ExceptionInterceptor>();
    options.Interceptors.Add<IdentityInterceptor>();
    options.Interceptors.Add<AttributesInterceptor>();
});


builder.Services.AddGrpcReflection();

var app = builder.Build();

// app.UseAuthentication();
// app.UseAuthorization();

// Add gRPC Services
if (app.Environment.IsDevelopment())
    app.MapGrpcReflectionService();

app.MapGrpcService<ExamplesGrpcService>();

await app.RunAsync();
