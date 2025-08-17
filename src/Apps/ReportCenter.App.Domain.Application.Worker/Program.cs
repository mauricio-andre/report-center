using DocumentFormat.OpenXml.Spreadsheet;
using FluentValidation;
using MediatR;
using ReportCenter.App.Domain.Application.Worker;
using ReportCenter.App.Domain.Application.Worker.Loggers;
using ReportCenter.App.Domain.Application.Worker.Reports;
using ReportCenter.Common.Diagnostics;
using ReportCenter.Common.Options;
using ReportCenter.Common.Providers.MessageQueues.Interfaces;
using ReportCenter.Common.Providers.Storage.Interfaces;
using ReportCenter.Core.Data;
using ReportCenter.Core.Identity.Interfaces;
using ReportCenter.Core.Identity.Services;
using ReportCenter.Core.Reports.Interfaces;
using ReportCenter.Core.Reports.Services;
using ReportCenter.CustomConsoleFormatter.Extensions;
using ReportCenter.CustomStringLocalizer.Extensions;
using ReportCenter.LocalStorage.Services;
using ReportCenter.Mongo.Extensions;
using ReportCenter.MongoDB.Repositories;
using ReportCenter.OpenTelemetry.Extensions;
using ReportCenter.RabbitMQ.Extensions;
using ReportCenter.RabbitMQ.Options;
using ReportCenter.RabbitMQ.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMongoCoreDbContext(
        builder.Configuration.GetConnectionString("CoreDbContext")!,
        builder.Configuration.GetValue<string>("MongoDBName")!)
    .AddRabbitMQConsumer(builder.Configuration.GetConnectionString("RabbitMQ")!)
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
    .AddSingleton<IMessagePublisher, RabbitMQPublisher>()
    .AddSingleton<IMessageConsumer, RabbitMQConsumer>()
    .AddSingleton<IBiggestReportExport, BiggestReportExport>();
    // .AddSingleton<ReportCenter.App.Domain.Application.Worker.Reports.V1.Example.ExportExampleService>()
    // .AddSingleton<ReportCenter.App.Domain.Application.Worker.Reports.V2.Example.ExportExampleService>();

// Configure GrpcClients
// var grpcAddres = new Uri(builder.Configuration.GetConnectionString("GrpcServer")!);
// builder.Services
//     .AddGrpcClient<ExamplesService.ExamplesServiceClient>(options => options.Address = grpcAddres);

// Configure providers
builder.Services.AddCustomStringLocalizerProvider();
builder.Services.AddCustomConsoleFormatterProvider<LoggerPropertiesService>();
builder.AddOpenTelemetryProvider();

// Configure options
builder.Services.Configure<ReportWorkerOptions>(builder.Configuration.GetSection(ReportWorkerOptions.Position));
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection(RabbitMQOptions.Position));

// builder.Services.AddHostedService<MessageConsumerTemplate>();

var host = builder.Build();

// var storage = new LocalStorage();
// var teste = new BiggestReportExportTest(storage);

// teste.Temp();

var temp = host.Services.GetRequiredService<IBiggestReportExport>();

await using (var biggest = temp.OpenWriteStream("relatório.xlsx", "MinhaAba", 100))
{
    var headerCells = new Cell[] {
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue($"Titulo Coluna 1")
        },
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue($"Titulo Coluna 2")
        },
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue($"Titulo Coluna 3")
        },
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue($"Titulo Coluna 4")
        },
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue($"Titulo Coluna 5")
        },
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue($"Titulo Coluna 6")
        },
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue($"Titulo Coluna 7")
        },
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue($"Titulo Coluna 8")
        },
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue($"Titulo Coluna 9")
        },
        new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue($"Titulo Coluna 10")
        }
    };

    biggest.SetHeader(headerCells);

    for (uint r = 1; r <= 1000; r++)
    {
        var listCells = new Cell[10];
        for (int c = 0; c < 10; c++)
        {
            listCells[c] = new Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue($"R{r}C{c}")
            };
        }

        await biggest.WriteRowAsync(listCells);
    }
}

await host.RunAsync();
