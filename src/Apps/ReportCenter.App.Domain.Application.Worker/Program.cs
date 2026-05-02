using DocumentFormat.OpenXml.Spreadsheet;
using FluentValidation;
using MediatR;
using ReportCenter.App.Domain.Application.Worker.Reports;
using ReportCenter.App.GrpcServer.Methods.V1.Examples;
using ReportCenter.AzureBlobStorages.Extensions;
using ReportCenter.AzureBlobStorages.Services;
using ReportCenter.AzureServiceBus.Extensions;
using ReportCenter.AzureServiceBus.Services;
using ReportCenter.Common.Diagnostics;
using ReportCenter.Common.Options;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;
using ReportCenter.Common.Providers.OAuth.Interfaces;
using ReportCenter.Common.Providers.Storage.Interfaces;
using ReportCenter.Core.Data;
using ReportCenter.Core.Identity.Interfaces;
using ReportCenter.Core.Identity.Services;
using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Interfaces;
using ReportCenter.Core.Reports.Services;
using ReportCenter.Core.Templates.BackgroundServices;
using ReportCenter.CustomStringLocalizer.Extensions;
using ReportCenter.LocalStorages.Services;
using ReportCenter.Mongo.Extensions;
using ReportCenter.MongoDB.Repositories;
using ReportCenter.OAuth.Extensions;
using ReportCenter.OAuth.Options;
using ReportCenter.OpenTelemetry.Extensions;

var builder = Host.CreateApplicationBuilder(args);

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
    .AddSingleton(_ => new ReportCenterActivitySource(builder.Configuration.GetValue<string>("ServiceName")!))
    .AddScoped<ICurrentIdentity, CurrentIdentity>()
    .AddScoped<IReportRepository, ExportRepository>()
    .AddSingleton<IReportServiceFactory, ReportServiceFactory>()
    .AddSingleton<IStorageService, LocalStorage>()
    // .AddSingleton<IStorageService, AzureBlobStorage>()
    .AddSingleton<IMessagePublisher, AzureServiceBusPublisher>()
    .AddSingleton<IMessageConsumer, AzureServiceBusConsumer>()
    // .AddScoped<IMessagePublisher, RabbitMQPublisher>()
    // .AddScoped<IMessageConsumer, RabbitMQConsumer>()
    .AddSingleton<IOAuthTokenService, OAuthTokenService>()
    .AddSingleton<IBiggestReportExport, BiggestReportExport>()
    .AddSingleton<ReportCenter.App.Domain.Application.Worker.Reports.V1.Example.ExportExampleService>()
    .AddSingleton<ReportCenter.App.Domain.Application.Worker.Reports.V2.Example.ExportExampleService>();

// Configure GrpcClients
var grpcAddres = new Uri(builder.Configuration.GetConnectionString("GrpcServer")!);
builder.Services
    .AddGrpcClient<ExamplesService.ExamplesServiceClient>(options => options.Address = grpcAddres);

// Configure providers
builder.Services.AddCustomStringLocalizerProvider();
builder.Services.AddAzureBlobStorageProvider(builder.Configuration, builder.Configuration.GetConnectionString("BlobStorage")!);
// builder.Services.AddRabbitMQProvider(builder.Configuration, builder.Configuration.GetConnectionString("RabbitMQ")!);
builder.Services.AddAzureServiceBusProvider(builder.Configuration, builder.Configuration.GetConnectionString("ServiceBus")!);
builder.Services.AddOAuthProvider(builder.Configuration);
builder.AddOpenTelemetryProvider();

// Configure options
builder.Services.Configure<ReportWorkerOptions>(builder.Configuration.GetSection(ReportWorkerOptions.Position));

builder.Services.AddHostedService<MessageConsumerTemplate>();

var host = builder.Build();

var _biggestReportExport = host.Services.GetRequiredService<IBiggestReportExport>();

await using (var stream = _biggestReportExport.OpenWriteStream(
    Path.Combine("..", "tmp", "file.xlsx"),
    "example",
    DateTimeOffset.Now,
    2))
{

    stream.SetHeader([
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue("Texto")
        },
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue("Inteiro")
        },
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue("Decimal")
        }
    ]);

    await stream.WriteRowAsync([
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue("teste")
        },
        new Cell
        {
            DataType = CellValues.Number,
            CellValue = new CellValue(20)
        },
        new Cell
        {
            DataType = CellValues.Number,
            CellValue = new CellValue(1)
        }
    ]);

    await stream.WriteRowAsync([
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue("teste")
        },
        new Cell
        {
            DataType = CellValues.Number,
            CellValue = new CellValue(20)
        },
        new Cell
        {
            DataType = CellValues.Number,
            CellValue = new CellValue(2)
        }
    ]);

    stream.EnsureSheet("teste");

    stream.SetHeader([
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue("Texto")
        },
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue("Inteiro")
        },
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue("Decimal")
        }
    ]);

    await stream.WriteRowAsync([
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue("teste")
        },
        new Cell
        {
            DataType = CellValues.Number,
            CellValue = new CellValue(20)
        },
        new Cell
        {
            DataType = CellValues.Number,
            CellValue = new CellValue(1)
        }
    ]);

    await stream.WriteRowAsync([
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue("teste")
        },
        new Cell
        {
            DataType = CellValues.Number,
            CellValue = new CellValue(20)
        },
        new Cell
        {
            DataType = CellValues.Number,
            CellValue = new CellValue(2)
        }
    ]);

    stream.EnsureSheet("example");

    await stream.WriteRowAsync([
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue("teste")
        },
        new Cell
        {
            DataType = CellValues.Number,
            CellValue = new CellValue(20)
        },
        new Cell
        {
            DataType = CellValues.Number,
            CellValue = new CellValue(3)
        }
    ]);

    await stream.WriteRowAsync([
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue("teste")
        },
        new Cell
        {
            DataType = CellValues.Number,
            CellValue = new CellValue(20)
        },
        new Cell
        {
            DataType = CellValues.Number,
            CellValue = new CellValue(4)
        }
    ]);

    stream.EnsureSheet("vazio");

    await stream.SaveAsync();
}

// await host.RunAsync();
