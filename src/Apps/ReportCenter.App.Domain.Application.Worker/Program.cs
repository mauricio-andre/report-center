using FluentValidation;
using MediatR;
using ReportCenter.App.Domain.Application.Worker.Loggers;
using ReportCenter.App.Domain.Application.Worker.Reports;
using ReportCenter.App.GrpcServer.Methods.V1.Examples;
using ReportCenter.Common.Diagnostics;
using ReportCenter.Common.Options;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;
using ReportCenter.Common.Providers.Storage.Interfaces;
using ReportCenter.Core.Data;
using ReportCenter.Core.Identity.Interfaces;
using ReportCenter.Core.Identity.Services;
using ReportCenter.Core.Reports.Interfaces;
using ReportCenter.Core.Reports.Services;
using ReportCenter.Core.Templates.BackgroundServices;
using ReportCenter.CustomConsoleFormatter.Extensions;
using ReportCenter.CustomStringLocalizer.Extensions;
using ReportCenter.LocalStorage.Services;
using ReportCenter.Mongo.Extensions;
using ReportCenter.MongoDB.Repositories;
using ReportCenter.OpenTelemetry.Extensions;
using ReportCenter.AzureServiceBus.Extensions;
using ReportCenter.AzureServiceBus.Services;

var builder = Host.CreateApplicationBuilder(args);

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
    .AddSingleton(_ => new ReportCenterActivitySource(builder.Configuration.GetValue<string>("ServiceName")!))
    .AddScoped<ICurrentIdentity, CurrentIdentity>()
    .AddScoped<IReportRepository, ExportRepository>()
    .AddSingleton<IReportServiceFactory, ReportServiceFactory>()
    .AddSingleton<IStorageService, LocalStorage>()
    .AddSingleton<IMessagePublisher, AzureServiceBusPublisher>()
    .AddSingleton<IMessageConsumer, AzureServiceBusConsumer>()
    // .AddSingleton<IMessagePublisher, RabbitMQPublisher>()
    // .AddSingleton<IMessageConsumer, RabbitMQConsumer>()
    .AddSingleton<IBiggestReportExport, BiggestReportExport>()
    .AddSingleton<ReportCenter.App.Domain.Application.Worker.Reports.V1.Example.ExportExampleService>()
    .AddSingleton<ReportCenter.App.Domain.Application.Worker.Reports.V2.Example.ExportExampleService>();

// Configure GrpcClients
var grpcAddres = new Uri(builder.Configuration.GetConnectionString("GrpcServer")!);
builder.Services
    .AddGrpcClient<ExamplesService.ExamplesServiceClient>(options => options.Address = grpcAddres);

// Configure providers
builder.Services.AddCustomStringLocalizerProvider();
builder.Services.AddCustomConsoleFormatterProvider<LoggerPropertiesService>();
builder.AddOpenTelemetryProvider();

// Configure options
builder.Services.Configure<ReportWorkerOptions>(builder.Configuration.GetSection(ReportWorkerOptions.Position));

builder.Services.AddHostedService<MessageConsumerTemplate>();

var host = builder.Build();

await host.RunAsync();
